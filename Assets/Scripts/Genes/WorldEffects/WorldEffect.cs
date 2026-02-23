// File: Assets/Scripts/Genes/WorldEffects/WorldEffect.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using WegoSystem;

namespace Abracodabra.Genes.WorldEffects
{
    /// <summary>
    /// Abstract base for any persistent world effect (clouds, auras, etc.).
    /// Lives on a spawned GameObject. Registers with TickManager for tick updates.
    /// Only ticks during GrowthAndThreat phase.
    /// </summary>
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
        }

        protected virtual void OnDestroy()
        {
            var tickManager = TickManager.Instance;
            if (tickManager != null)
            {
                tickManager.UnregisterTickUpdateable(this);
            }
        }

        public void OnTickUpdate(int tick)
        {
            // Only process during Growth & Threat phase
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

        /// <summary>
        /// Called each tick while the effect is active. Implement per-effect behavior here.
        /// </summary>
        protected abstract void OnEffectTick(int tick);

        /// <summary>
        /// Called when the effect's duration runs out. Default: destroy after fade.
        /// </summary>
        protected virtual void OnEffectExpire()
        {
            StartCoroutine(FadeAndDestroy());
        }

        private System.Collections.IEnumerator FadeAndDestroy()
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

        /// <summary>
        /// Applies all payload genes to the given target.
        /// </summary>
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

        /// <summary>
        /// Initialize the world effect. Call after instantiation.
        /// </summary>
        public virtual void Initialize(PlantGrowth source, List<RuntimeGeneInstance> payloads, float effectRadius, int duration, float multiplier = 1f)
        {
            sourcePlant = source;
            payloadInstances = payloads != null ? new List<RuntimeGeneInstance>(payloads) : new List<RuntimeGeneInstance>();
            radius = effectRadius;
            durationTicks = duration;
            effectMultiplier = multiplier;

            // Set visual scale based on radius
            transform.localScale = Vector3.one * radius * 2f;

            // Tint based on primary payload color
            if (spriteRenderer != null && payloadInstances.Count > 0)
            {
                var primaryPayload = payloadInstances[0]?.GetGene<PayloadGene>();
                if (primaryPayload != null)
                {
                    Color tint = primaryPayload.geneColor;
                    tint.a = 0.5f; // Semi-transparent
                    spriteRenderer.color = tint;
                }
            }

            Debug.Log($"[WorldEffect] {GetType().Name} initialized at {transform.position} | Radius: {radius} | Duration: {durationTicks} ticks");
        }
    }
}
