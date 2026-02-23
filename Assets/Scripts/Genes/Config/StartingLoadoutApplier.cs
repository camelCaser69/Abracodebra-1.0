// File: Assets/Scripts/Genes/Config/StartingLoadoutApplier.cs
using UnityEngine;
using Abracodabra.Genes.Config;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;

namespace Abracodabra.Genes.Config
{
    /// <summary>
    /// Applies the starting gene/seed loadout to the player's inventory on game start.
    /// Attach to a GameObject in the scene (e.g., GameManager or a dedicated initializer).
    /// Runs after GameUIManager has set up the inventory.
    /// </summary>
    public class StartingLoadoutApplier : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StartingLoadoutConfig loadoutConfig;

        [Header("Timing")]
        [Tooltip("Delay before applying loadout to ensure inventory is initialized.")]
        [SerializeField] private float applyDelay = 0.5f;

        private bool hasApplied;

        private void Start()
        {
            if (loadoutConfig == null)
            {
                Debug.LogWarning("[StartingLoadoutApplier] No StartingLoadoutConfig assigned!");
                return;
            }

            Invoke(nameof(ApplyLoadout), applyDelay);
        }

        void ApplyLoadout() {
            if (hasApplied) return;
            hasApplied = true;

            if (!InventoryService.IsInitialized) {
                Debug.LogError("[StartingLoadoutApplier] InventoryService not ready! Cannot apply loadout.");
                return;
            }

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
