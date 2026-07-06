// FILE: Assets/Scripts/Genes/Config/StartingLoadoutApplier.cs
using UnityEngine;
using Abracodabra.Genes.Config;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;

namespace Abracodabra.Genes.Config {
    public class StartingLoadoutApplier : MonoBehaviour {
        [Header("Configuration")]
        [SerializeField] StartingLoadoutConfig loadoutConfig;

        bool hasApplied;
        bool subscribed;

        void Start() {
            // A5: deterministic readiness instead of a fixed time delay.
            if (InventoryService.IsInitialized) {
                ApplyLoadout();
            }
            else {
                InventoryService.OnInventoryReady += HandleInventoryReady;
                subscribed = true;
            }
        }

        void OnDestroy() {
            if (subscribed) {
                InventoryService.OnInventoryReady -= HandleInventoryReady;
                subscribed = false;
            }
        }

        void HandleInventoryReady() {
            InventoryService.OnInventoryReady -= HandleInventoryReady;
            subscribed = false;
            ApplyLoadout();
        }

        void ApplyLoadout() {
            if (hasApplied) return;

            if (loadoutConfig == null) {
                Debug.LogWarning("[StartingLoadoutApplier] No StartingLoadoutConfig assigned!");
                return;
            }

            if (!InventoryService.IsInitialized) {
                Debug.LogError("[StartingLoadoutApplier] InventoryService not ready! Cannot apply loadout.");
                return;
            }

            hasApplied = true;

            int totalAdded = 0;

            foreach (var seedEntry in loadoutConfig.startingSeeds) {
                if (seedEntry.seed == null) continue;

                for (int i = 0; i < seedEntry.count; i++) {
                    var item = new UIInventoryItem(seedEntry.seed);
                    int slot = InventoryService.AddItem(item);
                    if (slot >= 0) {
                        totalAdded++;
                    }
                    else {
                        Debug.LogWarning($"[StartingLoadoutApplier] Inventory full! Could not add seed '{seedEntry.seed.templateName}'");
                        break;
                    }
                }
            }

            foreach (var geneEntry in loadoutConfig.startingGenes) {
                if (geneEntry.gene == null) continue;

                for (int i = 0; i < geneEntry.count; i++) {
                    var item = new UIInventoryItem(geneEntry.gene);
                    int slot = InventoryService.AddItem(item);
                    if (slot >= 0) {
                        totalAdded++;
                    }
                    else {
                        Debug.LogWarning($"[StartingLoadoutApplier] Inventory full! Could not add gene '{geneEntry.gene.geneName}'");
                        break;
                    }
                }
            }

            Debug.Log($"[StartingLoadoutApplier] Applied starting loadout: {totalAdded} items added to inventory.");
        }
    }
}
