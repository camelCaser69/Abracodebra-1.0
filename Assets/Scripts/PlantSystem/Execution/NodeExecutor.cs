using UnityEngine;
using Abracodabra.Genes;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private GameObject plantPrefab;

    // MODIFIED: This method now accepts the fully-configured state.
    public GameObject SpawnPlantFromState(PlantGeneRuntimeState runtimeState, Vector3 plantingPosition, Transform parentTransform)
    {
        if (runtimeState == null)
        {
            Debug.LogError("[NodeExecutor] Cannot spawn plant: Provided PlantGeneRuntimeState is null!");
            return null;
        }

        if (plantPrefab == null)
        {
            Debug.LogError("[NodeExecutor] Plant prefab not assigned!");
            return null;
        }

        GameObject plantObj = Instantiate(plantPrefab, plantingPosition, Quaternion.identity, parentTransform);

        // Grid snapping should happen after instantiation.
        // The PlantPlacementManager will handle this now.

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            growthComponent.InitializeWithState(runtimeState);
            Debug.Log($"[NodeExecutor] Plant spawned from seed template '{runtimeState.template.templateName}'");
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