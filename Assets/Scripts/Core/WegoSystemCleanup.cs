// Assets\Scripts\Core\WegoSystemCleanup.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WegoSystemCleanup : MonoBehaviour {
    [Header("Cleanup Options")]
    [SerializeField] bool removeRigidbody2DFromEntities = true;
    [SerializeField] bool removeObsoleteScripts = true;
    [SerializeField] bool logActions = true;

    [Header("Obsolete Script Names")]
    [SerializeField] List<string> obsoleteScriptNames = new List<string> {
        "SpeedModifiable",
        "RunManager" // If we're simplifying the turn system
    };

    [ContextMenu("Perform Cleanup")]
    public void PerformCleanup() {
        int totalRemoved = 0;

        if (removeRigidbody2DFromEntities) {
            totalRemoved += RemoveRigidbodiesFromEntities();
        }

        if (removeObsoleteScripts) {
            totalRemoved += RemoveObsoleteComponents();
        }

        if (logActions) {
            Debug.Log($"[WegoSystemCleanup] Cleanup complete. Removed {totalRemoved} components.");
        }
    }

    int RemoveRigidbodiesFromEntities() {
        int removed = 0;

        // Remove from gardeners
        var gardeners = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);
        foreach (var gardener in gardeners) {
            var rb = gardener.GetComponent<Rigidbody2D>();
            if (rb != null) {
                if (logActions) Debug.Log($"[WegoSystemCleanup] Removing Rigidbody2D from {gardener.name}");
                DestroyImmediate(rb);
                removed++;
            }
        }

        // Remove from animals
        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        foreach (var animal in animals) {
            var rb = animal.GetComponent<Rigidbody2D>();
            if (rb != null) {
                if (logActions) Debug.Log($"[WegoSystemCleanup] Removing Rigidbody2D from {animal.name}");
                DestroyImmediate(rb);
                removed++;
            }
        }

        return removed;
    }

    int RemoveObsoleteComponents() {
        int removed = 0;

        // Find all GameObjects in scene
        var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (var obj in allObjects) {
            var components = obj.GetComponents<Component>();
            foreach (var component in components) {
                if (component == null) {
                    // Missing script
                    if (logActions) Debug.Log($"[WegoSystemCleanup] Found missing script on {obj.name}");
                    removed++;
                }
                else if (obsoleteScriptNames.Contains(component.GetType().Name)) {
                    if (logActions) Debug.Log($"[WegoSystemCleanup] Removing {component.GetType().Name} from {obj.name}");
                    DestroyImmediate(component);
                    removed++;
                }
            }
        }

        return removed;
    }

    [ContextMenu("List Components to Remove")]
    public void ListComponentsToRemove() {
        Debug.Log("[WegoSystemCleanup] === Components that will be removed ===");

        // List Rigidbody2D components
        var gardeners = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);
        foreach (var gardener in gardeners) {
            if (gardener.GetComponent<Rigidbody2D>() != null) {
                Debug.Log($"- Rigidbody2D on {gardener.name}");
            }
        }

        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        foreach (var animal in animals) {
            if (animal.GetComponent<Rigidbody2D>() != null) {
                Debug.Log($"- Rigidbody2D on {animal.name}");
            }
        }

        // List obsolete components
        var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects) {
            var components = obj.GetComponents<Component>();
            foreach (var component in components) {
                if (component != null && obsoleteScriptNames.Contains(component.GetType().Name)) {
                    Debug.Log($"- {component.GetType().Name} on {obj.name}");
                }
            }
        }
    }
}