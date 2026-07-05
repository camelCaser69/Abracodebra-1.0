// FILE: Assets\Scripts\Genes\WorldEffects\WorldEffect.cs
using System.Collections;
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using UnityEngine;
using WegoSystem;

namespace Abracodabra.Genes.WorldEffects
{
    public abstract class WorldEffect : MonoBehaviour, ITickUpdateable
    {
        [Header("World Effect Settings")]
        public PlantGrowth sourcePlant;
        public List<RuntimeGeneInstance> payloadInstances = new List<RuntimeGeneInstance>();
        public float radius;
        public int durationTicks;
        public float effectMultiplier = 1f;

        protected int currentTick;
        protected bool isActive;
        protected SpriteRenderer spriteRenderer;

        // --- Tile overlay state ---
        bool tileOverlayActive;

        public bool IsActive => isActive;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        protected virtual void OnEnable()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }

            isActive = true;
            currentTick = 0;
        }

        protected virtual void OnDisable()
        {
            var tickManager = TickManager.Instance;
            if (tickManager != null)
            {
                tickManager.UnregisterTickUpdateable(this);
            }

            ClearTileOverlay();
        }

        protected virtual void OnDestroy()
        {
            var tickManager = TickManager.Instance;
            if (tickManager != null)
            {
                tickManager.UnregisterTickUpdateable(this);
            }

            ClearTileOverlay();
        }

        public void OnTickUpdate(int tick)
        {
            if (RunManager.HasInstance && RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            {
                return;
            }

            if (!isActive) return;

            currentTick++;
            OnEffectTick(currentTick);

            if (currentTick >= durationTicks)
            {
                isActive = false;
                OnEffectExpire();
            }
        }

        protected abstract void OnEffectTick(int tick);

        protected virtual void OnEffectExpire()
        {
            ClearTileOverlay();
            StartCoroutine(FadeAndDestroy());
        }

        IEnumerator FadeAndDestroy()
        {
            if (spriteRenderer != null)
            {
                float fadeDuration = 0.3f;
                float elapsed = 0f;
                Color startColor = spriteRenderer.color;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
                    spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }
            }

            Destroy(gameObject);
        }

        protected void ApplyPayloadsToTarget(GameObject target)
        {
            if (target == null || payloadInstances == null) return;

            foreach (var instance in payloadInstances)
            {
                if (instance == null) continue;

                var payloadGene = instance.GetGene<PayloadGene>();
                if (payloadGene == null) continue;

                var context = new PayloadContext
                {
                    target = target,
                    source = sourcePlant,
                    payloadInstance = instance,
                    effectMultiplier = effectMultiplier,
                    parentGene = null
                };

                try
                {
                    payloadGene.ApplyPayload(context);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[WorldEffect] Error applying payload {payloadGene.geneName} to {target.name}: {e.Message}");
                }
            }
        }

        public virtual void Initialize(PlantGrowth source, List<RuntimeGeneInstance> payloads, float effectRadius, int duration, float multiplier = 1f)
        {
            sourcePlant = source;
            payloadInstances = payloads != null ? new List<RuntimeGeneInstance>(payloads) : new List<RuntimeGeneInstance>();
            radius = effectRadius;
            durationTicks = duration;
            effectMultiplier = multiplier;

            // Tint any sprite if present (used by projectile, trap marker, etc.)
            if (spriteRenderer != null && payloadInstances.Count > 0)
            {
                var primaryPayload = payloadInstances[0]?.GetGene<PayloadGene>();
                if (primaryPayload != null)
                {
                    Color tint = primaryPayload.geneColor;
                    tint.a = 0.5f;
                    spriteRenderer.color = tint;
                }
            }

            Debug.Log($"[WorldEffect] {GetType().Name} initialized at {transform.position} | Radius: {radius} | Duration: {durationTicks} ticks");
        }

        // =====================================================================
        // TILE OVERLAY HELPERS
        // Subclasses call these to show/update/clear tile-based AOE overlays
        // via GridDebugVisualizer. Multiple effects naturally overlap because
        // each gets its own semi-transparent tile set keyed by 'this'.
        // =====================================================================

        /// <summary>
        /// Resolves the overlay color from payloads. Returns the primary payload's
        /// geneColor with the specified alpha, or a white fallback.
        /// </summary>
        protected Color GetOverlayColor(float alpha = 0.3f)
        {
            if (payloadInstances != null && payloadInstances.Count > 0)
            {
                var primaryPayload = payloadInstances[0]?.GetGene<PayloadGene>();
                if (primaryPayload != null)
                {
                    Color c = primaryPayload.geneColor;
                    c.a = alpha;
                    return c;
                }
            }

            return new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>
        /// Shows or updates a persistent tile overlay at the effect's current position.
        /// Call this after Initialize / InitializeAura / Refresh, and whenever
        /// the radius or color changes.
        /// </summary>
        protected void UpdateTileOverlay()
        {
            UpdateTileOverlay(GetOverlayColor());
        }

        /// <summary>
        /// Shows or updates a persistent tile overlay with an explicit color.
        /// </summary>
        protected void UpdateTileOverlay(Color color)
        {
            if (GridDebugVisualizer.Instance == null || GridPositionManager.Instance == null) return;

            GridPosition center = GridPositionManager.Instance.WorldToGrid(transform.position);
            int radiusTiles = Mathf.Max(0, Mathf.RoundToInt(radius));

            GridDebugVisualizer.Instance.ShowContinuousColoredRadius(this, center, radiusTiles, color);
            tileOverlayActive = true;
        }

        /// <summary>
        /// Removes the tile overlay for this effect.
        /// </summary>
        protected void ClearTileOverlay()
        {
            if (!tileOverlayActive) return;

            if (GridDebugVisualizer.Instance != null)
            {
                GridDebugVisualizer.Instance.HideContinuousRadius(this);
            }
            tileOverlayActive = false;
        }

        /// <summary>
        /// Shows a momentary colored burst on tiles (for explosions, reactive bursts, etc.)
        /// This is fire-and-forget — tiles auto-destroy after duration.
        /// </summary>
        protected void ShowTileBurst(float burstRadius, Color color, float duration = 0.4f)
        {
            if (GridDebugVisualizer.Instance == null || GridPositionManager.Instance == null) return;

            GridPosition center = GridPositionManager.Instance.WorldToGrid(transform.position);
            int radiusTiles = Mathf.Max(0, Mathf.RoundToInt(burstRadius));

            GridDebugVisualizer.Instance.ShowColoredRadiusBurst(this, center, radiusTiles, color, duration);
        }
    }
}