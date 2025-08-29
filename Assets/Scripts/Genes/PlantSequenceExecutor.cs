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
        public PlantGrowth plantGrowth;
        public PlantGeneRuntimeState runtimeState;

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
            
            // This is the core execution logic, now running once per tick
            if (TryExecuteCurrentSlot())
            {
                runtimeState.currentPosition++;

                if (runtimeState.currentPosition >= runtimeState.activeSequence.Count)
                {
                    OnSequenceComplete();
                }
            }
        }

        private bool TryExecuteCurrentSlot()
        {
            if (runtimeState.currentPosition >= runtimeState.activeSequence.Count)
            {
                return false;
            }

            var slot = runtimeState.activeSequence[runtimeState.currentPosition];
            
            // Handle empty slots
            if (!slot.HasContent)
            {
                return true; // Skip empty slot, advance sequence
            }
            
            // Handle execution delay
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
            
            // If we have reached this point, we will execute the gene.
            energySystem.SpendEnergy(energyCost);
            slot.isExecuting = true;
            
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
            
            // Check for delay and schedule it, otherwise execute immediately
            if (activeGene.executionDelayTicks > 0)
            {
                slot.delayTicksRemaining = activeGene.executionDelayTicks;
                // We still return true here because the action has *started* and energy has been spent.
                // The delay check at the top of the method will handle the waiting period.
            }
            else
            {
                PerformExecutionLogic(activeGene, context, slot);
            }

            return true; // Execution successful, advance sequence
        }

        private void PerformExecutionLogic(ActiveGene activeGene, ActiveGeneContext context, RuntimeSequenceSlot slot)
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

        // This small coroutine can remain as it's for a minor visual effect and doesn't affect game logic timing.
        private IEnumerator ClearExecutionFlag(RuntimeSequenceSlot slot)
        {
            yield return new WaitForSeconds(0.5f);
            if(slot != null)
            {
                slot.isExecuting = false;
            }
        }

        private void OnSequenceComplete()
        {
            var energySystem = plantGrowth.EnergySystem;

            eventBus?.Publish(new SequenceCompletedEvent
            {
                TotalSlotsExecuted = runtimeState.activeSequence.Count,
                TotalEnergyUsed = (energySystem != null) ? (energySystem.MaxEnergy - energySystem.CurrentEnergy) : 0f
            });

            runtimeState.currentPosition = 0;
            runtimeState.rechargeTicksRemaining = runtimeState.template.baseRechargeTime;
        }

        public void PauseExecution() { /* Deprecated */ }
        public void ResumeExecution() { /* Deprecated */ }
    }
}