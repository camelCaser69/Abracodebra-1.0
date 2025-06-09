using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlantCellManager {
    readonly PlantGrowth plant;
    readonly GameObject seedCellPrefab;
    readonly GameObject stemCellPrefab;
    readonly GameObject leafCellPrefab;
    readonly GameObject berryCellPrefab;
    readonly float cellSpacing;
    
    Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    List<GameObject> activeCellGameObjects = new List<GameObject>();
    public GameObject RootCellInstance { get; set; }
    public List<LeafData> LeafDataList { get; } = new List<LeafData>();
    
    bool? offsetRightForPattern1 = null;
    
    public PlantCellManager(PlantGrowth plant, GameObject seedPrefab, GameObject stemPrefab, GameObject leafPrefab, GameObject berryPrefab, float spacing) {
        this.plant = plant;
        seedCellPrefab = seedPrefab;
        stemCellPrefab = stemPrefab;
        leafCellPrefab = leafPrefab;
        berryCellPrefab = berryPrefab;
        cellSpacing = spacing;
    }
    
    public void ReportCellDestroyed(Vector2Int coord) {
        if (cells.ContainsKey(coord)) {
            PlantCellType cellType = cells[coord];
            
            if (cellType == PlantCellType.Leaf) {
                for (int i = 0; i < LeafDataList.Count; i++) {
                    if (LeafDataList[i].GridCoord == coord) {
                        LeafData updatedData = LeafDataList[i];
                        updatedData.IsActive = false;
                        LeafDataList[i] = updatedData;
                        break;
                    }
                }
            }
            
            cells.Remove(coord);
            activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));
            
            if (plant.IsOutlineEnabled() && plant.VisualManager.OutlineController != null) {
                plant.VisualManager.OutlineController.OnPlantCellRemoved(coord);
            }
        }
    }
    
    public void ClearAllVisuals() {
        List<GameObject> cellsToClear = new List<GameObject>(activeCellGameObjects);
        foreach (GameObject cellGO in cellsToClear) {
            if (cellGO != null) {
                Object.Destroy(cellGO);
            }
        }
        activeCellGameObjects.Clear();
        cells.Clear();
        RootCellInstance = null;
    }
    
    public GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null) {
        if (cells.ContainsKey(coords)) {
            Debug.LogWarning($"[{plant.gameObject.name}] Trying to spawn {cellType} at occupied coord {coords}.");
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
            Debug.LogError($"[{plant.gameObject.name}] Prefab for PlantCellType.{cellType} is null!");
            return null;
        }
        
        Vector2 worldPos = (Vector2)plant.transform.position + ((Vector2)coords * cellSpacing);
        GameObject instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, plant.transform);
        instance.name = $"{plant.gameObject.name}_{cellType}_{coords.x}_{coords.y}";
        
        PlantCell cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>();
        cellComp.ParentPlantGrowth = plant;
        cellComp.GridCoord = coords;
        cellComp.CellType = cellType;
        
        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);
        
        SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();
        sorter.SetUseParentYCoordinate(cellType != PlantCellType.Seed);
        
        if (cellType == PlantCellType.Fruit) {
            plant.ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }
        
        plant.VisualManager.RegisterShadowForCell(instance, cellType.ToString());
        if (plant.IsOutlineEnabled() && plant.VisualManager.OutlineController != null) {
            plant.VisualManager.OutlineController.OnPlantCellAdded(coords, instance);
        }
        
        return instance;
    }
    
    public List<Vector2Int> CalculateLeafPositions(Vector2Int stemPos, int stageCounter, int leafPattern) {
        List<Vector2Int> leafPositions = new List<Vector2Int>();
        Vector2Int leftBase = stemPos + Vector2Int.left;
        Vector2Int rightBase = stemPos + Vector2Int.right;
        
        switch (leafPattern) {
            case 0:
                leafPositions.Add(leftBase);
                leafPositions.Add(rightBase);
                break;
            case 1:
                if (offsetRightForPattern1 == null) offsetRightForPattern1 = Random.value < 0.5f;
                if (offsetRightForPattern1.Value) {
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else {
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                break;
            case 2:
                switch (stageCounter % 4) {
                    case 0:
                    case 2:
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase);
                        break;
                    case 1:
                        leafPositions.Add(leftBase + Vector2Int.up);
                        leafPositions.Add(rightBase);
                        break;
                    case 3:
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase + Vector2Int.up);
                        break;
                }
                break;
            case 3:
                int spiralDir = (stageCounter % 2 == 0) ? 1 : -1;
                if (spiralDir > 0) {
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else {
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                break;
            case 4:
                leafPositions.Add(leftBase);
                leafPositions.Add(leftBase + Vector2Int.up);
                leafPositions.Add(rightBase);
                leafPositions.Add(rightBase + Vector2Int.up);
                break;
            default:
                leafPositions.Add(leftBase);
                leafPositions.Add(rightBase);
                break;
        }
        return leafPositions;
    }
    
    public bool TryRegrowLeaf() {
        int missingLeafIndex = -1;
        for (int i = 0; i < LeafDataList.Count; i++) {
            if (!LeafDataList[i].IsActive) {
                missingLeafIndex = i;
                break;
            }
        }
        if (missingLeafIndex == -1) return false;
        
        Vector2Int leafCoord = LeafDataList[missingLeafIndex].GridCoord;
        if (cells.ContainsKey(leafCoord)) return false;
        
        GameObject newLeaf = SpawnCellVisual(PlantCellType.Leaf, leafCoord);
        if (newLeaf != null) {
            LeafDataList[missingLeafIndex] = new LeafData(leafCoord, true);
            return true;
        }
        return false;
    }
    
    public bool DoesCellExistAt(Vector2Int coord) => cells.ContainsKey(coord);
    
    public GameObject GetCellGameObjectAt(Vector2Int coord) {
        return activeCellGameObjects.FirstOrDefault(go => go != null && go.GetComponent<PlantCell>()?.GridCoord == coord);
    }
    
    public Dictionary<Vector2Int, PlantCellType> GetCells() => cells;
}