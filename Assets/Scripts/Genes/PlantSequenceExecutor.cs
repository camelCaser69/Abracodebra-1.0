// File: Assets/Scripts/Genes/PlantSequenceExecutor.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes
{
    public class PlantSequenceExecutor : MonoBehaviour
    {
        public PlantGrowth plantGrowth;
        public PlantGeneRuntimeState runtimeState;

        IGeneEventBus eventBus;
        IDeterministicRandom random;

        void Awake()
        {
            eventBus = GeneServices.Get<IGeneEventBus>();
            random = GeneServices.Get<IDeterministicRandom>();

            if (eventBus == null || random == null)
            {
                Debug.LogError($"[{nameof(PlantSequenceExecutor)}] on {gameObject.name} could not retrieve required gene services! This indicates a critical initialization order problem. The component will be disabled.", this);
                enabled = false;
            }
        }

        void Start()
        {
            if (plantGrowth == null)
            {
                plantGrowth = GetComponent<PlantGrowth>();
            }
        }

        public void InitializeWithTemplate(SeedTemplate template)
        {
            runtimeState = template.CreateRuntimeState();
        }

        public void InitializeWithTemplate(PlantGeneRuntimeState state)
        {
            this.runtimeState = state;
            if (runtimeState == null || runtimeState.activeSequence == null || runtimeState.activeSequence.Count == 0)
            {
                Debug.LogWarning($"Plant '{plantGrowth.name}' has no active gene sequence. Executor will remain idle.", this);
            }
        }

        public void OnTickUpdate(int currentTick)
        {
            if (runtimeState == null || runtimeState.template == null || plantGrowth == null)
            {
                return;
            }

            if (plantGrowth.CurrentState != PlantState.Mature)
            {
                return;
            }

            var energySystem = plantGrowth.EnergySystem;
            if (energySystem == null)
            {
                return;
            }

            if (runtimeState.rechargeTicksRemaining > 0)
            {
                runtimeState.rechargeTicksRemaining--;
                return;
            }

            if (TryExecuteCurrentSlot())
            {
                runtimeState.currentPosition++;

                if (runtimeState.currentPosition >= runtimeState.activeSequence.Count)
                {
                    OnSequenceComplete();
                }
            }
        }

        bool TryExecuteCurrentSlot()
        {
            if (runtimeState.currentPosition >= runtimeState.activeSequence.Count)
            {
                return false;
            }

            var slot = runtimeState.activeSequence[runtimeState.currentPosition];

            if (!slot.HasContent)
            {
                return true; // Skip empty slot, advance sequence
            }

            if (slot.delayTicksRemaining > 0)
            {
                slot.delayTicksRemaining--;
                return false; // Still waiting, do not advance sequence
            }

            var activeGene = slot.activeInstance?.GetGene<ActiveGene>();
            if (activeGene == null)
            {
                Debug.LogError($"Active gene instance at sequence position {runtimeState.currentPosition} has a null or invalid gene reference! Skipping slot.", this);
                return true; // Skip invalid slot, advance sequence
            }

            // === TASK 4/6: Pre-energy checks ===

            // Build context early so trigger modifiers can use it
            var context = new ActiveGeneContext
            {
                plant = plantGrowth,
                activeInstance = slot.activeInstance,
                modifiers = slot.modifierInstances,
                payloads = slot.payloadInstances,
                sequencePosition = runtimeState.currentPosition,
                executor = this,
                random = random
            };

            // Check requiresTarget BEFORE spending energy (Task 4)
            if (activeGene.requiresTarget)
            {
                if (!TargetFinder.HasCreatureInRange(plantGrowth.transform.position, activeGene.targetRange))
                {
                    // No target — skip slot, advance sequence, don't spend energy
                    return true;
                }
            }

            // Check trigger-type modifiers BEFORE spending energy (Task 6)
            foreach (var modInstance in slot.modifierInstances)
            {
                var modifierGene = modInstance?.GetGene<ModifierGene>();
                if (modifierGene != null && modifierGene.modifierType == ModifierType.Trigger)
                {
                    if (!modifierGene.CheckTriggerCondition(context))
                    {
                        // Trigger condition not met — skip, advance, save energy
                        return true;
                    }
                }
            }

            // === Energy check (original logic) ===

            float energyCost = slot.GetEnergyCost();
            var energySystem = plantGrowth.EnergySystem;

            if (!energySystem.HasEnergy(energyCost))
            {
                eventBus?.Publish(new GeneValidationFailedEvent
                {
                    GeneId = activeGene.GUID,
                    Reason = $"Insufficient energy. Has {energySystem.CurrentEnergy}, needs {energyCost}."
                });
                return false; // Not enough energy, do not advance sequence
            }

            energySystem.SpendEnergy(energyCost);
            slot.isExecuting = true;

            // Reset effect_multiplier before modifiers apply (Overcharge reads/writes this)
            if (slot.activeInstance != null)
            {
                slot.activeInstance.SetValue("effect_multiplier", 1f);
            }

            // Run modifier PreExecution (Overcharge sets effect_multiplier here)
            foreach (var modInstance in slot.modifierInstances)
            {
                var modifierGene = modInstance?.GetGene<ModifierGene>();
                if (modifierGene != null)
                {
                    modifierGene.PreExecution(context);
                }
                else
                {
                    Debug.LogWarning($"A modifier gene in the slot at position {runtimeState.currentPosition} is missing or invalid.", this);
                }
            }

            if (activeGene.executionDelayTicks > 0)
            {
                slot.delayTicksRemaining = activeGene.executionDelayTicks;
            }
            else
            {
                PerformExecutionLogic(activeGene, context, slot);
            }

            return true; // Execution successful, advance sequence
        }

        void PerformExecutionLogic(ActiveGene activeGene, ActiveGeneContext context, RuntimeSequenceSlot slot)
        {
            activeGene.Execute(context);

            foreach (var modInstance in slot.modifierInstances)
            {
                var modifierGene = modInstance?.GetGene<ModifierGene>();
                if (modifierGene != null)
                {
                    modifierGene.PostExecution(context);
                }
            }

            eventBus?.Publish(new GeneExecutedEvent
            {
                Gene = activeGene,
                SequencePosition = context.sequencePosition,
                Success = true,
                EnergyCost = slot.GetEnergyCost()
            });

            StartCoroutine(ClearExecutionFlag(slot));
        }

        IEnumerator ClearExecutionFlag(RuntimeSequenceSlot slot)
        {
            yield return new WaitForSeconds(0.5f);
            if (slot != null)
            {
                slot.isExecuting = false;
            }
        }

        void OnSequenceComplete() {
            var energySystem = plantGrowth.EnergySystem;

            eventBus?.Publish(new SequenceCompletedEvent {
                TotalSlotsExecuted = runtimeState.activeSequence.Count,
                TotalEnergyUsed = (energySystem != null) ? energySystem.EnergySpentThisCycle : 0f
            });

            // Reset the cycle tracker for the next sequence iteration
            if (energySystem != null) {
                energySystem.EnergySpentThisCycle = 0f;
            }

            runtimeState.currentPosition = 0;
            runtimeState.rechargeTicksRemaining = runtimeState.template.baseRechargeTime;
        }
    }
}
