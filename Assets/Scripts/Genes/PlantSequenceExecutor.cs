// FILE: Assets/Scripts/Genes/PlantSequenceExecutor.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.WorldEffects;
using Abracodabra.Genes.Implementations;

namespace Abracodabra.Genes
{
    public class PlantSequenceExecutor : MonoBehaviour
    {
        public PlantGrowth plantGrowth;
        public PlantGeneRuntimeState runtimeState;

        IGeneEventBus eventBus;
        IDeterministicRandom random;

        // Tracks whether trigger-type genes have been initialized for this plant
        bool triggerGenesInitialized = false;

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
            InitializeTriggerGenes();
        }

        public void InitializeWithTemplate(PlantGeneRuntimeState state)
        {
            this.runtimeState = state;
            if (runtimeState == null || runtimeState.activeSequence == null || runtimeState.activeSequence.Count == 0)
            {
                Debug.LogWarning($"Plant '{plantGrowth.name}' has no active gene sequence. Executor will remain idle.", this);
            }
            InitializeTriggerGenes();
        }

        /// <summary>
        /// Scans the sequence for trigger-type active genes and sets up their event handlers.
        /// Called once during initialization.
        /// </summary>
        void InitializeTriggerGenes()
        {
            if (triggerGenesInitialized) return;
            if (runtimeState == null || runtimeState.activeSequence == null) return;

            foreach (var slot in runtimeState.activeSequence)
            {
                if (!slot.HasContent) continue;

                var activeGene = slot.activeInstance?.GetGene<ActiveGene>();
                if (activeGene == null || !activeGene.isTriggerType) continue;

                // Reactive Burst
                if (activeGene is ReactiveBurstGene burstGene)
                {
                    var handler = gameObject.GetComponent<ReactiveBurstHandler>();
                    if (handler == null)
                    {
                        handler = gameObject.AddComponent<ReactiveBurstHandler>();
                    }

                    handler.Initialize(
                        plantGrowth,
                        burstGene,
                        slot.payloadInstances,
                        slot.modifierInstances,
                        slot.activeInstance
                    );

                    Debug.Log($"[PlantSequenceExecutor] Initialized ReactiveBurstHandler for '{burstGene.geneName}' on '{plantGrowth.name}'");
                }

                // Future trigger types can be added here
            }

            triggerGenesInitialized = true;
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

            // ── TRIGGER-TYPE: skip immediately without spending energy or a tick ──
            if (activeGene.isTriggerType)
            {
                return true; // Cursor passes over trigger slots freely
            }

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

            if (activeGene.requiresTarget)
            {
                if (!TargetFinder.HasCreatureInRange(plantGrowth.transform.position, activeGene.targetRange))
                {
                    return true;
                }
            }

            foreach (var modInstance in slot.modifierInstances)
            {
                var modifierGene = modInstance?.GetGene<ModifierGene>();
                if (modifierGene != null && modifierGene.modifierType == ModifierType.Trigger)
                {
                    if (!modifierGene.CheckTriggerCondition(context))
                    {
                        return true;
                    }
                }
            }

            float energyCost = slot.GetEnergyCost();
            var energySystemRef = plantGrowth.EnergySystem;

            if (!energySystemRef.HasEnergy(energyCost))
            {
                eventBus?.Publish(new GeneValidationFailedEvent
                {
                    GeneId = activeGene.GUID,
                    Reason = $"Insufficient energy. Has {energySystemRef.CurrentEnergy}, needs {energyCost}."
                });
                return false; // Not enough energy, do not advance sequence
            }

            energySystemRef.SpendEnergy(energyCost);
            slot.isExecuting = true;

            if (slot.activeInstance != null)
            {
                slot.activeInstance.SetValue("effect_multiplier", 1f);
            }

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

        void OnSequenceComplete()
        {
            var energySystem = plantGrowth.EnergySystem;

            eventBus?.Publish(new SequenceCompletedEvent
            {
                TotalSlotsExecuted = runtimeState.activeSequence.Count,
                TotalEnergyUsed = (energySystem != null) ? energySystem.EnergySpentThisCycle : 0f
            });

            if (energySystem != null)
            {
                energySystem.EnergySpentThisCycle = 0f;
            }

            runtimeState.currentPosition = 0;
            runtimeState.rechargeTicksRemaining = runtimeState.template.baseRechargeTime;
        }
    }
}