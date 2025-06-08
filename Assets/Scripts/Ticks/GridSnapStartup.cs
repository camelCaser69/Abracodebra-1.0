// Assets\Scripts\Core\GridSnapStartup.cs

using UnityEngine;
using WegoSystem;

public class GridSnapStartup : MonoBehaviour {
    [SerializeField] bool snapAllAnimals = true;
    [SerializeField] bool snapAllPlants = true;
    [SerializeField] bool snapPlayer = true;
    [SerializeField] bool debugLog = true;

    void Awake() {
        // Ensure this runs after GridPositionManager
        if (GridPositionManager.Instance == null) {
            Debug.LogError("[GridSnapStartup] GridPositionManager not found! Cannot snap entities.");
            return;
        }
    }

    void Start() {
        PerformGridSnapping();
    }

    void PerformGridSnapping() {
        int snappedCount = 0;

        if (snapPlayer) {
            GardenerController[] gardeners = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);
            foreach (var gardener in gardeners) {
                GridPositionManager.Instance.SnapEntityToGrid(gardener.gameObject);
                snappedCount++;
            }
        }

        if (snapAllAnimals) {
            AnimalController[] animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
            foreach (var animal in animals) {
                GridPositionManager.Instance.SnapEntityToGrid(animal.gameObject);
                snappedCount++;
            }
        }

        if (snapAllPlants) {
            PlantGrowth[] plants = FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
            foreach (var plant in plants) {
                GridPositionManager.Instance.SnapEntityToGrid(plant.gameObject);
                snappedCount++;
            }
        }

        if (debugLog) {
            Debug.Log($"[GridSnapStartup] Snapped {snappedCount} entities to grid on startup");
        }
    }

    // Editor helper to snap entities in edit mode
    [ContextMenu("Snap All Entities Now")]
    void SnapAllEntitiesNow() {
        if (GridPositionManager.Instance == null) {
            Debug.LogError("GridPositionManager not found in scene!");
            return;
        }

        PerformGridSnapping();
    }
}