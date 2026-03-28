// FILE: Assets/Scripts/Genes/Implementations/Payload/ExplosivePayload.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "ExplosivePayload", menuName = "Abracodabra/Genes/Payload/Explosive")]
    public class ExplosivePayload : PayloadGene
    {
        [Header("Explosive Configuration")]
        [Tooltip("AoE damage dealt to all creatures in blast radius.")]
        public float baseAoeDamage = 15f;

        [Tooltip("Blast radius in tiles.")]
        public float blastRadius = 1.5f;

        [Tooltip("Number of leaves destroyed on the source plant per detonation (self-damage).")]
        public int selfDamageLeaves = 1;

        [Tooltip("Optional: prefab for the explosion visual effect.")]
        public GameObject explosionVfxPrefab;

        public ExplosivePayload()
        {
            payloadType = PayloadType.Special;
            geneColor = new Color(1f, 0.4f, 0.1f); // Orange-red — explosive
        }

        // Track the last tick + source plant combo to prevent multiple self-damages
        // from the same delivery event (e.g., Cloud hitting 5 creatures in one tick)
        static int _lastDetonationTick = -1;
        static int _lastDetonationPlantId = -1;

        public override void ApplyPayload(PayloadContext context)
        {
            if (context.target == null) return;

            float potency = GetFinalPotency(context.payloadInstance);
            float finalDamage = baseAoeDamage * potency * context.effectMultiplier;

            // ── Direct hit damage ──
            var directTarget = context.target.GetComponent<AnimalController>();
            if (directTarget != null && !directTarget.IsDying)
            {
                directTarget.TakeDamage(finalDamage);
                Debug.Log($"[ExplosivePayload] Direct hit on '{directTarget.SpeciesName}' for {finalDamage:F1} damage");
            }

            // ── AoE splash to nearby creatures (excludes direct target) ──
            Vector3 detonationPos = context.target.transform.position;
            var creaturesInBlast = TargetFinder.FindCreaturesInRadius(detonationPos, blastRadius);

            int splashCount = 0;
            foreach (var creature in creaturesInBlast)
            {
                if (creature == null || creature.IsDying) continue;
                if (creature.gameObject == context.target) continue; // Skip direct target (already damaged)

                // Splash deals 50% of full damage
                creature.TakeDamage(finalDamage * 0.5f);
                splashCount++;
            }

            if (splashCount > 0)
            {
                Debug.Log($"[ExplosivePayload] Splash hit {splashCount} creature(s) in {blastRadius:F1} tile radius for {finalDamage * 0.5f:F1} each");
            }

            // ── Spawn explosion VFX ──
            if (explosionVfxPrefab != null)
            {
                var vfx = Object.Instantiate(explosionVfxPrefab, detonationPos, Quaternion.identity);
                vfx.transform.localScale = Vector3.one * blastRadius * 2f;
                Object.Destroy(vfx, 1f); // Auto-cleanup after 1 second
            }

            // ── Self-damage: destroy leaves on source plant ──
            // Only once per tick per plant to prevent Cloud/Aura multi-hit cascading
            if (context.source != null)
            {
                int currentTick = Time.frameCount; // Use frameCount as tick proxy for dedup
                int plantId = context.source.GetInstanceID();

                if (_lastDetonationTick != currentTick || _lastDetonationPlantId != plantId)
                {
                    _lastDetonationTick = currentTick;
                    _lastDetonationPlantId = plantId;

                    for (int i = 0; i < selfDamageLeaves; i++)
                    {
                        bool destroyed = context.source.DestroyRandomLeaf("Explosive");
                        if (!destroyed)
                        {
                            Debug.Log($"[ExplosivePayload] Source plant '{context.source.name}' has no more leaves to destroy!");
                            break;
                        }
                    }

                    Debug.Log($"[ExplosivePayload] Self-damage: destroyed {selfDamageLeaves} leaf/leaves on '{context.source.name}'. Remaining: {context.source.ActiveLeafCount}");
                }
            }

            // Floating combat text at detonation point
            FloatingCombatText.Spawn(
                detonationPos + Vector3.up * 0.2f,
                $"BOOM -{finalDamage:F0}",
                geneColor
            );
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance)
        {
            fruit.AddVisualEffect(geneColor);

            if (fruit.DynamicProperties == null)
                fruit.DynamicProperties = new System.Collections.Generic.Dictionary<string, float>();

            fruit.DynamicProperties["is_explosive"] = 1f;
            fruit.DynamicProperties["explosion_damage"] = baseAoeDamage;
            fruit.DynamicProperties["blast_radius"] = blastRadius;
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance)
        {
            // Used when explosive fruit is eaten — detonate at the eater's position
            if (target == null) return;

            Vector3 detonationPos = target.transform.position;
            float potency = GetFinalPotency(instance);
            float finalDamage = baseAoeDamage * potency;

            // AoE at the eater's position
            var creaturesInBlast = TargetFinder.FindCreaturesInRadius(detonationPos, blastRadius);
            foreach (var creature in creaturesInBlast)
            {
                if (creature == null || creature.IsDying) continue;
                creature.TakeDamage(finalDamage);
            }

            // VFX
            if (explosionVfxPrefab != null)
            {
                var vfx = Object.Instantiate(explosionVfxPrefab, detonationPos, Quaternion.identity);
                vfx.transform.localScale = Vector3.one * blastRadius * 2f;
                Object.Destroy(vfx, 1f);
            }

            FloatingCombatText.Spawn(
                detonationPos + Vector3.up * 0.2f,
                $"BOOM -{finalDamage:F0}",
                geneColor
            );

            Debug.Log($"[ExplosivePayload] Fruit detonated at {detonationPos}! Hit {creaturesInBlast.Count} creature(s) for {finalDamage:F1}");
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float potency = 1f;
            if (context?.instance != null)
            {
                potency = GetFinalPotency(context.instance);
            }

            return $"{description}\n\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(geneColor)}><b>Effect: Explosive</b></color>\n" +
                $"Direct hit: <b>{baseAoeDamage * potency:F0}</b> damage.\n" +
                $"Splash: <b>{baseAoeDamage * potency * 0.5f:F0}</b> damage in <b>{blastRadius:F1}</b> tile radius.\n" +
                $"<color=#FF6644>Self-damage: destroys <b>{selfDamageLeaves}</b> leaf per detonation.</color>\n" +
                $"Pairs with Thick Bark + Regrowth for self-sustaining builds.";
        }
    }
}