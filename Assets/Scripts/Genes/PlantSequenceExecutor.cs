// File: Assets/Scripts/Genes/PlantSequenceExecutor.cs
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

        Coroutine executionCoroutine;
        IGeneEventBus eventBus;
        IDeterministicRandom random;

        void Awake()
        {
            eventBus = GeneServices.Get<IGeneEventBus>();
            random = GeneServices.Get<IDeterministicRandom>();
        }

        void Start()
        {
            if (plantGrowth == null)
                plantGrowth = GetComponent<PlantGrowth>();
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

            foreach (var modInstance in slot.modifierInstances)
            {
                modInstance.GetGene<ModifierGene>()?.PreExecution(context);
            }

            slot.isExecuting = true;
            activeGene.Execute(context);
            energySystem.SpendEnergy(energyCost);

            foreach (var modInstance in slot.modifierInstances)
            {
                modInstance.GetGene<ModifierGene>()?.PostExecution(context);
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