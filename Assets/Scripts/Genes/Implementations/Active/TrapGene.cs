// FILE: Assets/Scripts/Genes/Implementations/Active/TrapGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;
using WegoSystem;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "TrapGene", menuName = "Abracodabra/Genes/Active/Trap")]
    public class TrapGene : ActiveGene
    {
        [Header("Trap Configuration")]
        [Tooltip("Radius from plant in tiles where the trap can deploy.")]
        public float armRadius = 1.5f;

        [Tooltip("How close a creature must be to trigger the trap (tiles).")]
        public float triggerRadius = 0.3f;

        [Tooltip("Ticks the creature is immobilized (rooted) after triggering.")]
        public int immobilizeDurationTicks = 3;

        [Tooltip("Prefab for the trap marker (subtle ground visual). Needs SpriteRenderer.")]
        public GameObject trapMarkerPrefab;

        [Tooltip("Prefab for the trigger VFX (root burst). Auto-destroyed after 1s.")]
        public GameObject triggerVfxPrefab;

        public TrapGene()
        {
            baseEnergyCost = 0f;        // Trap is free
            canExecuteEmpty = false;     // Needs at least one payload to be useful
            requiresTarget = false;      // Always arms regardless of enemies
        }

        public override void Execute(ActiveGeneContext context)
        {
            // Check if this plant already has an active trap — only 1 at a time
            var existingTrap = context.plant.GetComponentInChildren<TrapWorldEffect>();
            if (existingTrap != null)
            {
                Debug.Log($"[TrapGene] '{geneName}' on '{context.plant.name}' — trap already armed. Skipping.");
                return;
            }

            // Find a random empty tile within arm radius
            Vector3 plantPos = context.plant.transform.position;
            Vector3? trapPosition = FindTrapPosition(plantPos, context.random);

            if (trapPosition == null)
            {
                Debug.Log($"[TrapGene] '{geneName}' on '{context.plant.name}' — no valid tile for trap. Skipping.");
                return;
            }

            // Spawn the trap
            GameObject trapObj;
            if (trapMarkerPrefab != null)
            {
                trapObj = Object.Instantiate(trapMarkerPrefab, trapPosition.Value, Quaternion.identity);
            }
            else
            {
                trapObj = new GameObject($"Trap_{geneName}_{context.plant.name}");
                trapObj.transform.position = trapPosition.Value;
            }

            trapObj.name = $"Trap_{geneName}_{context.plant.name}";
            trapObj.transform.SetParent(context.plant.transform);

            TrapWorldEffect trap = trapObj.GetComponent<TrapWorldEffect>();
            if (trap == null)
            {
                trap = trapObj.AddComponent<TrapWorldEffect>();
            }

            float multiplier = context.activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;
            var payloadsCopy = new List<RuntimeGeneInstance>(context.payloads);

            trap.Initialize(
                context.plant,
                payloadsCopy,
                triggerRadius,
                immobilizeDurationTicks,
                multiplier,
                triggerVfxPrefab
            );

            Debug.Log($"[TrapGene] '{geneName}' armed trap at {trapPosition.Value} for '{context.plant.name}' | TriggerRadius: {triggerRadius:F1} | Payloads: {payloadsCopy.Count}");
        }

        Vector3? FindTrapPosition(Vector3 plantWorldPos, Services.IDeterministicRandom random)
        {
            // Try several random positions within arm radius
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float angle = (random != null ? random.Range(0f, 1f) : Random.value) * Mathf.PI * 2f;
                float dist = (random != null ? random.Range(0.5f, armRadius) : Random.Range(0.5f, armRadius));

                Vector3 candidate = plantWorldPos + new Vector3(
                    Mathf.Cos(angle) * dist,
                    Mathf.Sin(angle) * dist,
                    0f
                );

                // Verify it's a valid position on the grid
                if (GridPositionManager.Instance != null)
                {
                    GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(candidate);
                    if (GridPositionManager.Instance.IsPositionValid(gridPos) &&
                        !GridPositionManager.Instance.IsMovementBlockedAt(gridPos))
                    {
                        // Snap to grid center
                        return GridPositionManager.Instance.GridToWorld(gridPos);
                    }
                }
                else
                {
                    // No grid manager — just use the candidate position
                    return candidate;
                }
            }

            return null;
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return payloads.Count > 0; // Trap needs at least one payload
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                $"Arms a hidden trap within <b>{armRadius:F1}</b> tiles of the plant.\n" +
                $"Energy Cost: <b>0 E</b> (free).\n" +
                $"When triggered: immobilizes creature for <b>{immobilizeDurationTicks}</b> ticks + applies payloads.\n" +
                $"Re-arms on next strand cycle. One trap per plant.";
        }
    }
}