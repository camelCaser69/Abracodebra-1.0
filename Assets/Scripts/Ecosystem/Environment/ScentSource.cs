using UnityEngine;
using WegoSystem;

public class ScentSource : MonoBehaviour {
    public ScentDefinition definition;
    public float radiusModifier = 0f;
    public float strengthModifier = 0f;
    
    GridEntity gridEntity;
    
    public float EffectiveRadius => Mathf.Max(0f, (definition != null ? definition.baseRadius : 0f) + radiusModifier);
    public float EffectiveStrength => Mathf.Max(0f, (definition != null ? definition.baseStrength : 0f) + strengthModifier);
    
    void Awake() {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null) {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
    }
    
    void OnDrawGizmosSelected() {
        if (definition == null || !Application.isPlaying) return;
        
        // Get grid position
        if (gridEntity == null) gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null) return;
        
        int effectiveRadiusTiles = Mathf.RoundToInt(EffectiveRadius);
        if (effectiveRadiusTiles <= 0) return;
        
        // Draw grid-based radius
        var affectedTiles = GridRadiusUtility.GetTilesInCircle(gridEntity.Position, effectiveRadiusTiles);
        
        // Set color based on definition
        Random.State prevState = Random.state;
        Random.InitState(definition.name.GetHashCode());
        Color gizmoColor = Random.ColorHSV(0f, 1f, 0.7f, 0.9f, 0.8f, 1f);
        gizmoColor.a = 0.3f;
        Random.state = prevState;
        
        Gizmos.color = gizmoColor;
        
        // Draw each affected tile
        foreach (var tile in affectedTiles) {
            Vector3 tileWorld = GridPositionManager.Instance.GridToWorld(tile);
            Gizmos.DrawCube(tileWorld, Vector3.one * 0.9f); // Slightly smaller than full tile
        }
        
        // Draw outline
        var outlineTiles = GridRadiusUtility.GetCircleOutline(gridEntity.Position, effectiveRadiusTiles);
        gizmoColor.a = 0.8f;
        Gizmos.color = gizmoColor;
        
        foreach (var tile in outlineTiles) {
            Vector3 tileWorld = GridPositionManager.Instance.GridToWorld(tile);
            Gizmos.DrawWireCube(tileWorld, Vector3.one);
        }
    }
}