// File: Assets/Scripts/Genes/PlantSequenceExecutor.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

namespace Abracodabra.Genes
{
    public class PlantSequenceExecutor : MonoBehaviour
    {
        public PlantGrowth plantGrowth;
        public PlantGeneRuntimeState runtimeState;

        public float tickInterval = 1f;
        public bool isPaused = false;

        private Coroutine executionCoroutine;
        private IGeneEventBus eventBus;
        private IDeterministicRandom random;

        private void Awake()
        {
            eventBus = GeneServices.Get<IGeneEventBus>();
            random = GeneServices.Get<IDeterministicRandom>();

            if (eventBus == null || random == null)
            {
                Debug.LogError($"[{nameof(PlantSequenceExecutor)}] on {gameObject.name} could not retrieve required gene services! This indicates a critical initialization order problem. The component will be disabled.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            if (plantGrowth == null)
                plantGrowth = GetComponent<PlantGrowth>();
        }

        public void InitializeWithTemplate(SeedTemplate template)
        {
            runtimeState = template.CreateRuntimeState();
            // The call to ApplyPassiveGenes() has been removed from here.
            // PlantGrowth now handles this in the correct order.
            StartExecution();
        }

        public void StartExecution()
        {
            if (executionCoroutine != null)
                StopCoroutine(executionCoroutine);

            executionCoroutine = StartCoroutine(ExecutionLoop());
        }

        IEnumerator ExecutionLoop()
        {
            yield return null;

            while (true)
            {
                yield return new WaitForSeconds(tickInterval);

                if (isPaused || runtimeState == null || runtimeState.template == null || plantGrowth == null)
                    continue;

                // Use the plant's actual energy system
                var energySystem = plantGrowth.EnergySystem;
                if (energySystem == null)
                    continue;

                // Energy regeneration is handled by PlantEnergySystem.OnTickUpdate()
                // We just need to check if we're in recharge
                if (runtimeState.rechargeTicksRemaining > 0)
                {
                    runtimeState.rechargeTicksRemaining--;
                    continue;
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
        }

        // In file: Assets/Scripts/Genes/PlantSequenceExecutor.cs

        private bool TryExecuteCurrentSlot()
        {
            if (runtimeState.currentPosition >= runtimeState.activeSequence.Count)
                return false;

            var slot = runtimeState.activeSequence[runtimeState.currentPosition];
            if (!slot.HasContent)
            {
                // Slot is empty, so we successfully "executed" it by skipping.
                return true; 
            }

            var activeGene = slot.activeInstance?.GetGene<ActiveGene>();
            if (activeGene == null)
            {
                // The gene asset might be missing or failed to load. Log an error and skip the slot.
                Debug.LogError($"Active gene instance at sequence position {runtimeState.currentPosition} has a null or invalid gene reference! Skipping slot.", this);
                return true;
}

            float energyCost = slot.GetEnergyCost();
            var energySystem = plantGrowth.EnergySystem;

            if (!energySystem.HasEnergy(energyCost))
            {
                eventBus?.Publish(new GeneValidationFailedEvent
                {
                    GeneId = activeGene.GUID,
                    Reason = $"Insufficient energy. Has {energySystem.CurrentEnergy}, needs {energyCost}."
                });
                return false; // Not enough energy, try again next tick
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

            // Pre-Execution step for modifiers
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

            // Execute the main gene
            slot.isExecuting = true;
            activeGene.Execute(context);
            energySystem.SpendEnergy(energyCost);

            // Post-Execution step for modifiers
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
                SequencePosition = runtimeState.currentPosition,
                Success = true,
                EnergyCost = energyCost
            });

            StartCoroutine(ClearExecutionFlag(slot));

            return true;
        }

        IEnumerator ClearExecutionFlag(RuntimeSequenceSlot slot)
        {
            yield return new WaitForSeconds(0.5f);
            slot.isExecuting = false;
        }

        void OnSequenceComplete()
        {
            var energySystem = plantGrowth.EnergySystem;
            
            eventBus?.Publish(new SequenceCompletedEvent
            {
                TotalSlotsExecuted = runtimeState.activeSequence.Count,
                TotalEnergyUsed = energySystem.MaxEnergy - energySystem.CurrentEnergy
            });

            runtimeState.currentPosition = 0;
            runtimeState.rechargeTicksRemaining = runtimeState.template.baseRechargeTime;
        }

        public void PauseExecution() => isPaused = true;
        public void ResumeExecution() => isPaused = false;

        void OnDestroy()
        {
            if (executionCoroutine != null)
                StopCoroutine(executionCoroutine);
        }
    }
}