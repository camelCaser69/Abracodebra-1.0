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

        public float tickInterval = 1f;
        public bool isPaused = false;

        Coroutine executionCoroutine;
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
            StartExecution();
        }

        public void StartExecution()
        {
            if (runtimeState == null || runtimeState.activeSequence == null || runtimeState.activeSequence.Count == 0)
            {
                Debug.LogWarning($"Plant '{plantGrowth.name}' has no active gene sequence. Executor will remain idle.", this);
                if (executionCoroutine != null)
                {
                    StopCoroutine(executionCoroutine);
                    executionCoroutine = null;
                }
                return;
            }

            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
            }

            executionCoroutine = StartCoroutine(ExecutionLoop());
        }

        IEnumerator ExecutionLoop()
        {
            yield return null;

            while (true)
            {
                yield return new WaitForSeconds(tickInterval);

                if (isPaused || runtimeState == null || runtimeState.template == null || plantGrowth == null)
                {
                    continue;
                }
                
                // FIX: Only execute active genes if the plant is mature.
                if (plantGrowth.CurrentState != PlantState.Mature)
                {
                    continue;
                }

                var energySystem = plantGrowth.EnergySystem;
                if (energySystem == null)
                {
                    continue;
                }

                runtimeState.currentEnergy = energySystem.CurrentEnergy;

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
            {
                return false;
            }

            var slot = runtimeState.activeSequence[runtimeState.currentPosition];
            if (!slot.HasContent)
            {
                return true;
            }

            var activeGene = slot.activeInstance?.GetGene<ActiveGene>();
            if (activeGene == null)
            {
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

            energySystem.SpendEnergy(energyCost);
            runtimeState.currentEnergy = energySystem.CurrentEnergy;
            slot.isExecuting = true;

            if (activeGene.executionDelay > 0)
            {
                StartCoroutine(DelayedExecution(activeGene, context, slot, energyCost));
            }
            else
            {
                PerformExecutionLogic(activeGene, context, slot, energyCost);
            }

            return true;
        }

        void PerformExecutionLogic(ActiveGene activeGene, ActiveGeneContext context, RuntimeSequenceSlot slot, float energyCost)
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
                EnergyCost = energyCost
            });

            StartCoroutine(ClearExecutionFlag(slot));
        }

        IEnumerator DelayedExecution(ActiveGene activeGene, ActiveGeneContext context, RuntimeSequenceSlot slot, float energyCost)
        {
            yield return new WaitForSeconds(activeGene.executionDelay);

            if (plantGrowth != null && plantGrowth.gameObject != null)
            {
                PerformExecutionLogic(activeGene, context, slot, energyCost);
            }
        }

        IEnumerator ClearExecutionFlag(RuntimeSequenceSlot slot)
        {
            yield return new WaitForSeconds(0.5f);
            if(slot != null)
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
                TotalEnergyUsed = (energySystem != null) ? (energySystem.MaxEnergy - energySystem.CurrentEnergy) : 0f
            });

            runtimeState.currentPosition = 0;
            runtimeState.rechargeTicksRemaining = runtimeState.template.baseRechargeTime;
        }

        public void PauseExecution() => isPaused = true;
        public void ResumeExecution() => isPaused = false;

        void OnDestroy()
        {
            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
            }
        }
    }
}