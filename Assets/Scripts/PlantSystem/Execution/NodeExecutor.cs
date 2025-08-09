// Reworked File: Assets/Scripts/PlantSystem/Execution/NodeExecutor.cs

using Abracodabra.Genes;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes.Templates;

/// <summary>
/// REPURPOSED: This class is no longer a node executor.
/// It is now the central spawner for creating new plants from SeedTemplates.
/// </summary>
public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private GameObject plantPrefab;

    public GameObject SpawnPlantFromTemplate(SeedTemplate seedTemplate, Vector3 plantingPosition, Transform parentTransform)
    {
        if (seedTemplate == null)
        {
            Debug.LogError("[NodeExecutor] Cannot spawn plant: Provided SeedTemplate is null!");
            return null;
        }

        if (plantPrefab == null)
        {
            Debug.LogError("[NodeExecutor] Plant prefab not assigned!");
            return null;
        }

        GameObject plantObj = Instantiate(plantPrefab, plantingPosition, Quaternion.identity, parentTransform);

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(plantObj);
        }

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            growthComponent.InitializeFromTemplate(seedTemplate);
            Debug.Log($"[NodeExecutor] Plant spawned from seed template '{seedTemplate.templateName}'");
            return plantObj;
        }
        else
        {
            Debug.LogError($"[NodeExecutor] Plant prefab is missing the required PlantGrowth component!");
            Destroy(plantObj);
            return null;
        }
    }
}