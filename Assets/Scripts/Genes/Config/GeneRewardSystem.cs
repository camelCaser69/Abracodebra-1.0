// File: Assets/Scripts/Genes/Config/GeneRewardSystem.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;
using WegoSystem;

namespace Abracodabra.Genes.Config
{
    /// <summary>
    /// Gives random gene rewards to the player after each round.
    /// Subscribes to RunManager.OnRoundChanged to trigger rewards.
    /// Gene tier is gated by round number.
    /// </summary>
    public class GeneRewardSystem : MonoBehaviour
    {
        [Header("Reward Settings")]
        [Tooltip("Minimum number of genes awarded per round.")]
        [SerializeField] private int minGenesPerRound = 2;

        [Tooltip("Maximum number of genes awarded per round.")]
        [SerializeField] private int maxGenesPerRound = 4;

        [Header("References")]
        [SerializeField] private GeneLibrary geneLibrary;

        private void Start()
        {
            if (geneLibrary == null)
            {
                geneLibrary = GeneLibrary.Instance;
            }

            if (RunManager.HasInstance)
            {
                RunManager.Instance.OnRoundChanged += OnRoundChanged;
            }
            else
            {
                Debug.LogWarning("[GeneRewardSystem] RunManager not found at Start. Will attempt late binding.");
                Invoke(nameof(LateBindRunManager), 0.5f);
            }
        }

        private void LateBindRunManager()
        {
            if (RunManager.HasInstance)
            {
                RunManager.Instance.OnRoundChanged += OnRoundChanged;
                Debug.Log("[GeneRewardSystem] Late-bound to RunManager.");
            }
        }

        private void OnDestroy()
        {
            if (RunManager.HasInstance)
            {
                RunManager.Instance.OnRoundChanged -= OnRoundChanged;
            }
        }

        private void OnRoundChanged(int newRoundNumber)
        {
            // Rewards are given at the START of a new round (meaning the previous round was completed)
            int completedRound = newRoundNumber - 1;
            if (completedRound <= 0) return;

            GiveRoundReward(completedRound);
        }

        public void GiveRoundReward(int completedRoundNumber) {
            if (geneLibrary == null) {
                Debug.LogError("[GeneRewardSystem] GeneLibrary is null! Cannot give rewards.");
                return;
            }

            if (!InventoryService.IsInitialized) {
                Debug.LogError("[GeneRewardSystem] Inventory not available! Cannot give rewards.");
                return;
            }

            int geneCount = Random.Range(minGenesPerRound, maxGenesPerRound + 1);
            int maxTier = GetMaxTierForRound(completedRoundNumber);

            var eligibleGenes = GetEligibleGenes(maxTier);
            if (eligibleGenes.Count == 0) {
                Debug.LogWarning("[GeneRewardSystem] No eligible genes found in library!");
                return;
            }

            int added = 0;
            for (int i = 0; i < geneCount; i++) {
                if (!InventoryService.HasEmptySlot()) {
                    Debug.LogWarning("[GeneRewardSystem] Inventory full! Could not add all rewards.");
                    break;
                }

                GeneBase randomGene = eligibleGenes[Random.Range(0, eligibleGenes.Count)];
                var item = new UIInventoryItem(randomGene);
                int slot = InventoryService.AddItem(item);

                if (slot >= 0) {
                    added++;
                    Debug.Log($"[GeneRewardSystem] Rewarded: {randomGene.geneName} (T{randomGene.tier})");
                }
            }

            Debug.Log($"[GeneRewardSystem] Round {completedRoundNumber} complete! Awarded {added} gene(s). Max tier: T{maxTier}");
        }

        private int GetMaxTierForRound(int roundNumber)
        {
            if (roundNumber <= 2) return 1;
            if (roundNumber <= 4) return 2;
            return 3;
        }

        private List<GeneBase> GetEligibleGenes(int maxTier)
        {
            var all = new List<GeneBase>();

            foreach (var gene in geneLibrary.GetAllGenes())
            {
                if (gene == null) continue;
                if (gene is PlaceholderGene) continue;
                if (gene.tier > maxTier) continue;

                all.Add(gene);
            }

            return all;
        }
    }
}
