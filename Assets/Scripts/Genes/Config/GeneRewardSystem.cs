// FILE: Assets/Scripts/Genes/Config/GeneRewardSystem.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services; // A6: deterministic RNG
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;
using WegoSystem;

namespace Abracodabra.Genes.Config {
    public class GeneRewardSystem : MonoBehaviour {
        [Header("Reward Settings")]
        [Tooltip("Minimum number of genes awarded per round.")]
        [SerializeField] int minGenesPerRound = 2;

        [Tooltip("Maximum number of genes awarded per round.")]
        [SerializeField] int maxGenesPerRound = 4;

        [Header("References")]
        [SerializeField] GeneLibrary geneLibrary;

        void Start() {
            if (geneLibrary == null) {
                geneLibrary = GeneLibrary.Instance;
            }

            // A5: RunManager is a SingletonMonoBehaviour; its Instance is set in Awake,
            // so it is always available by Start. No timed late-binding needed.
            if (RunManager.Instance != null) {
                RunManager.Instance.OnRoundChanged += OnRoundChanged;
            }
            else {
                Debug.LogError("[GeneRewardSystem] RunManager not found at Start. Round rewards will not fire.");
            }
        }

        void OnDestroy() {
            if (RunManager.HasInstance) {
                RunManager.Instance.OnRoundChanged -= OnRoundChanged;
            }
        }

        void OnRoundChanged(int newRoundNumber) {
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

            // A6: deterministic gameplay randomness.
            var rng = GeneServices.Get<IDeterministicRandom>();

            int geneCount = (rng != null)
                ? rng.Range(minGenesPerRound, maxGenesPerRound + 1)
                : Random.Range(minGenesPerRound, maxGenesPerRound + 1);

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

                int pick = (rng != null)
                    ? rng.Range(0, eligibleGenes.Count)
                    : Random.Range(0, eligibleGenes.Count);

                GeneBase randomGene = eligibleGenes[pick];
                var item = new UIInventoryItem(randomGene);
                int slot = InventoryService.AddItem(item);

                if (slot >= 0) {
                    added++;
                    Debug.Log($"[GeneRewardSystem] Rewarded: {randomGene.geneName} (T{randomGene.tier})");
                }
            }

            Debug.Log($"[GeneRewardSystem] Round {completedRoundNumber} complete! Awarded {added} gene(s). Max tier: T{maxTier}");
        }

        int GetMaxTierForRound(int roundNumber) {
            if (roundNumber <= 2) return 1;
            if (roundNumber <= 4) return 2;
            return 3;
        }

        List<GeneBase> GetEligibleGenes(int maxTier) {
            var all = new List<GeneBase>();

            foreach (var gene in geneLibrary.GetAllGenes()) {
                if (gene == null) continue;
                if (gene is PlaceholderGene) continue;
                if (gene.tier > maxTier) continue;

                all.Add(gene);
            }

            return all;
        }
    }
}
