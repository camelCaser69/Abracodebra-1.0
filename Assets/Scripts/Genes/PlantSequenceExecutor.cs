// File: Assets/Scripts/Genes/PlantSequenceExecutor.cs
using UnityEngine;
using System.Collections;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

namespace Abracodabra.Genes
{
    public class PlantSequenceExecutor : MonoBehaviour
    {
        [Header("References")]
        public PlantGrowth plantGrowth;
        public PlantGeneRuntimeState runtimeState;

        [Header("Execution Settings")]
        public float tickInterval = 1f;
        public bool isPaused = false;

        private Coroutine executionCoroutine;
        private IGeneEventBus eventBus;
        private IDeterministicRandom random;

        void Awake()
        {
            // Retrieve services. Ensure GeneServices is initialized at game start.
            eventBus = GeneServices.Get<IGeneEventBus>();
            random = GeneServices.Get<IDeterministicRandom>();
        }

        void Start()
        {
            if (plantGrowth == null)
                plantGrowth = GetComponent<PlantGrowth>();

            // The executor is initialized externally by PlantGrowth
            // by calling InitializeWithTemplate.
        }

        public void InitializeWithTemplate(SeedTemplate template)
        {
            runtimeState = template.CreateRuntimeState();
            ApplyPassiveGenes();
            StartExecution();
        }

        void ApplyPassiveGenes()
        {
            if (runtimeState == null) return;

            foreach (var instance in runtimeState.passiveInstances)
            {
                var passive = instance.GetGene<PassiveGene>();
                if (passive != null && passive.MeetsRequirements(plantGrowth))
                {
                    passive.ApplyToPlant(plantGrowth, instance);
                    Debug.Log($"Applied passive gene: {passive.geneName} to {plantGrowth.name}");
                }
            }
        }

        public void StartExecution()
        {
            if (executionCoroutine != null)
                StopCoroutine(executionCoroutine);

            executionCoroutine = StartCoroutine(ExecutionLoop());
        }

        IEnumerator ExecutionLoop()
        {
            // Wait one frame to ensure everything is initialized
            yield return null;

            while (true)
            {
                yield return new WaitForSeconds(tickInterval);

                if (isPaused || runtimeState == null || runtimeState.template == null)
                    continue;

                // 1. Regenerate energy
                runtimeState.currentEnergy = Mathf.Min(
                    runtimeState.currentEnergy + runtimeState.template.energyRegenRate * tickInterval,
                    runtimeState.maxEnergy
                );

                // 2. Handle recharge cooldown
                if (runtimeState.rechargeTicksRemaining > 0)
                {
                    runtimeState.rechargeTicksRemaining--;
                    continue;
                }

                // 3. Try to execute the current slot in the sequence
                if (TryExecuteCurrentSlot())
                {
                    // Advance to the next position in the sequence
                    runtimeState.currentPosition++;

                    // Check if the entire sequence has completed
                    if (runtimeState.currentPosition >= runtimeState.activeSequence.Count)
                    {
                        OnSequenceComplete();
                    }
                }
            }
        }

        bool TryExecuteCurrentSlot()
        {
            if (runtimeState.currentPosition >= runtimeState.activeSequence.Count)
                return false;

            var slot = runtimeState.activeSequence[runtimeState.currentPosition];
            if (!slot.HasContent) return true; // Skip empty slots and advance

            var activeGene = slot.activeInstance.GetGene<ActiveGene>();
            if (activeGene == null)
            {
                Debug.LogError("Active gene instance has a null gene reference!", this);
                return true; // Skip broken slots
            }

            // Check if there is enough energy
            float energyCost = slot.GetEnergyCost();
            if (runtimeState.currentEnergy < energyCost)
            {
                eventBus?.Publish(new GeneValidationFailedEvent
                {
                    GeneId = activeGene.GUID,
                    Reason = $"Insufficient energy. Has {runtimeState.currentEnergy}, needs {energyCost}."
                });
                return false; // Not enough energy, try again next tick
            }

            // Create context for the execution
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

            // Run Pre-Execution modifiers
            foreach (var modInstance in slot.modifierInstances)
            {
                modInstance.GetGene<ModifierGene>()?.PreExecution(context);
            }

            // Execute the gene
            slot.isExecuting = true;
            activeGene.Execute(context);
            runtimeState.currentEnergy -= energyCost;

            // Run Post-Execution modifiers
            foreach (var modInstance in slot.modifierInstances)
            {
                modInstance.GetGene<ModifierGene>()?.PostExecution(context);
            }

            // Publish success event
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
            eventBus?.Publish(new SequenceCompletedEvent
            {
                TotalSlotsExecuted = runtimeState.activeSequence.Count,
                TotalEnergyUsed = runtimeState.maxEnergy - runtimeState.currentEnergy // Assuming energy starts full
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