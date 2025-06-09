using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public partial class PlantGrowth : MonoBehaviour
{
    public void ReportCellDestroyed(Vector2Int coord) {
        if (cells.ContainsKey(coord)) {
            PlantCellType cellType = cells[coord];

            if (cellType == PlantCellType.Leaf) {
                for (int i = 0; i < leafDataList.Count; i++) {
                    if (leafDataList[i].GridCoord == coord) {
                        LeafData updatedData = leafDataList[i];
                        updatedData.IsActive = false;
                        leafDataList[i] = updatedData;
                        break;
                    }
                }
            }

            cells.Remove(coord);
            activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));

            if (enableOutline && outlineController != null) {
                outlineController.OnPlantCellRemoved(coord);
            }
        }
    }

    private void ClearAllVisuals() {
        List<GameObject> cellsToClear = new List<GameObject>(activeCellGameObjects);
        foreach (GameObject cellGO in cellsToClear) {
            if (cellGO != null) {
                Destroy(cellGO);
            }
        }
        activeCellGameObjects.Clear();
        cells.Clear();
        rootCellInstance = null;
    }

    private GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null) {
        if (cells.ContainsKey(coords)) {
            Debug.LogWarning($"[{gameObject.name}] Trying to spawn {cellType} at occupied coord {coords}.");
            return null;
        }

        GameObject prefab = null;
        switch (cellType) {
            case PlantCellType.Seed: prefab = seedCellPrefab; break;
            case PlantCellType.Stem: prefab = stemCellPrefab; break;
            case PlantCellType.Leaf: prefab = leafCellPrefab; break;
            case PlantCellType.Fruit: prefab = berryCellPrefab; break;
        }

        if (prefab == null) {
            Debug.LogError($"[{gameObject.name}] Prefab for PlantCellType.{cellType} is null!");
            return null;
        }

        Vector2 worldPos = (Vector2)transform.position + ((Vector2)coords * cellSpacing);
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        instance.name = $"{gameObject.name}_{cellType}_{coords.x}_{coords.y}";

        PlantCell cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>();
        cellComp.ParentPlantGrowth = this;
        cellComp.GridCoord = coords;
        cellComp.CellType = cellType;

        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);

        SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();
        sorter.SetUseParentYCoordinate(cellType != PlantCellType.Seed);

        if (cellType == PlantCellType.Fruit) {
            ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }

        RegisterShadowForCell(instance, cellType.ToString());
        if (enableOutline && outlineController != null) {
            outlineController.OnPlantCellAdded(coords, instance);
        }

        return instance;
    }

    private void RegisterShadowForCell(GameObject cellInstance, string cellTypeName) {
        if (shadowController == null || shadowPartPrefab == null || cellInstance == null) return;
        SpriteRenderer partRenderer = cellInstance.GetComponentInChildren<SpriteRenderer>();
        if (partRenderer != null) {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        } else {
            Debug.LogWarning($"Plant '{gameObject.name}': {cellTypeName} missing SpriteRenderer. No shadow.", cellInstance);
        }
    }
    
    private List<Vector2Int> CalculateLeafPositions(Vector2Int stemPos, int stageCounter) {
        List<Vector2Int> leafPositions = new List<Vector2Int>();
        Vector2Int leftBase = stemPos + Vector2Int.left;
        Vector2Int rightBase = stemPos + Vector2Int.right;

        switch (finalLeafPattern) {
            case 0: leafPositions.Add(leftBase); leafPositions.Add(rightBase); break;
            case 1:
                if (offsetRightForPattern1 == null) offsetRightForPattern1 = Random.value < 0.5f;
                if (offsetRightForPattern1.Value) { leafPositions.Add(leftBase); leafPositions.Add(rightBase + Vector2Int.up); } 
                else { leafPositions.Add(leftBase + Vector2Int.up); leafPositions.Add(rightBase); }
                break;
            case 2:
                switch (stageCounter % 4) {
                    case 0: case 2: leafPositions.Add(leftBase); leafPositions.Add(rightBase); break;
                    case 1: leafPositions.Add(leftBase + Vector2Int.up); leafPositions.Add(rightBase); break;
                    case 3: leafPositions.Add(leftBase); leafPositions.Add(rightBase + Vector2Int.up); break;
                }
                break;
            case 3:
                int spiralDir = (stageCounter % 2 == 0) ? 1 : -1;
                if (spiralDir > 0) { leafPositions.Add(leftBase); leafPositions.Add(rightBase + Vector2Int.up); } 
                else { leafPositions.Add(leftBase + Vector2Int.up); leafPositions.Add(rightBase); }
                break;
            case 4: leafPositions.Add(leftBase); leafPositions.Add(leftBase + Vector2Int.up); leafPositions.Add(rightBase); leafPositions.Add(rightBase + Vector2Int.up); break;
            default: leafPositions.Add(leftBase); leafPositions.Add(rightBase); break;
        }
        return leafPositions;
    }
}