// FILE: Assets/Scripts/Genes/WorldEffects/TrapWorldEffect.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.WorldEffects
{
    /// <summary>
    /// An armed trigger that waits for a creature to step on it.
    /// NOT a WorldEffect subclass — no tick-based duration. It's a proximity trigger.
    /// Parented to the source plant; destroyed when triggered or plant dies.
    /// </summary>
    public class TrapWorldEffect : MonoBehaviour
    {
        [Header("Runtime State")]
        public PlantGrowth sourcePlant;
        public List<RuntimeGeneInstance> payloadInstances = new List<RuntimeGeneInstance>();
        public float triggerRadius = 0.3f;
        public int immobilizeDurationTicks = 3;
        public float effectMultiplier = 1f;
        public GameObject triggerVfxPrefab;

        bool isArmed = false;
        bool hasTriggered = false;
        SpriteRenderer spriteRenderer;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public void Initialize(
            PlantGrowth source,
            List<RuntimeGeneInstance> payloads,
            float trigger,
            int immobilizeTicks,
            float multiplier,
            GameObject vfxPrefab)
        {
            sourcePlant = source;
            payloadInstances = payloads ?? new List<RuntimeGeneInstance>();
            triggerRadius = trigger;
            immobilizeDurationTicks = immobilizeTicks;
            effectMultiplier = multiplier;
            triggerVfxPrefab = vfxPrefab;
            isArmed = true;
            hasTriggered = false;

            // Subtle armed visual — semi-transparent, small
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 0.25f;
                spriteRenderer.color = c;
            }

            Debug.Log($"[TrapWorldEffect] Armed at {transform.position} | TriggerRadius: {triggerRadius:F1} | Payloads: {payloadInstances.Count}");
        }

        void Update()
        {
            if (!isArmed || hasTriggered) return;

            // Check if source plant is still alive
            if (sourcePlant == null || sourcePlant.CurrentState == Abracodabra.Genes.PlantState.Dead)
            {
                Destroy(gameObject);
                return;
            }

            // Check all creatures for proximity
            var creatures = TargetFinder.FindCreaturesInRadius(transform.position, triggerRadius);

            foreach (var creature in creatures)
            {
                if (creature == null || creature.IsDying) continue;

                // TRIGGERED!
                TriggerTrap(creature);
                return;
            }
        }

        void TriggerTrap(AnimalController creature)
        {
            hasTriggered = true;
            isArmed = false;

            Debug.Log($"[TrapWorldEffect] TRIGGERED on '{creature.SpeciesName}' at {transform.position}!");

            // 1. Immobilize the creature
            creature.ApplyImmobilize(immobilizeDurationTicks);

            // 2. Apply all payloads
            if (payloadInstances != null)
            {
                foreach (var instance in payloadInstances)
                {
                    if (instance == null) continue;

                    var payloadGene = instance.GetGene<PayloadGene>();
                    if (payloadGene == null) continue;

                    var context = new PayloadContext
                    {
                        target = creature.gameObject,
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
                        Debug.LogError($"[TrapWorldEffect] Error applying payload {payloadGene.geneName}: {e.Message}");
                    }
                }
            }

            // 3. Trigger VFX
            if (triggerVfxPrefab != null)
            {
                var vfx = Instantiate(triggerVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 1f);
            }

            // 4. Floating combat text
            FloatingCombatText.Spawn(
                transform.position + Vector3.up * 0.2f,
                "TRAPPED!",
                new Color(0.6f, 0.3f, 0.1f)
            );

            // 5. Destroy the trap marker
            Destroy(gameObject, 0.1f);
        }
    }
}