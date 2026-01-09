using UnityEngine;
using System.Collections.Generic;

namespace WegoSystem {
 public enum PivotMode {
  None,
  Automatic,
  Manual
 }

 [CreateAssetMenu(fileName = "New Footprint", menuName = "WegoSystem/MultiTile Footprint")]
 public class MultiTileFootprint : ScriptableObject {
  [Tooltip("Size of the footprint in tiles. (2,2) = 2x2 grid.")]
  [SerializeField] Vector2Int size = new Vector2Int(2, 2);

  [Tooltip("Determines how the visual position is calculated relative to the anchor (bottom-left) tile.")]
  [SerializeField] PivotMode pivotMode = PivotMode.Automatic;

  [Tooltip("If Pivot Mode is Manual: The offset in tiles from the bottom-left anchor. (0.5, 0.5) is the center of the first tile.")]
  [SerializeField] Vector2 manualPivot = new Vector2(0.5f, 0.5f);

  [Tooltip("Interaction priority. Higher values take precedence over tiles and other entities.")]
  [SerializeField] int interactionPriority = 200;

  [Tooltip("If true, this entity blocks movement/pathfinding on all occupied tiles.")]
  [SerializeField] bool blocksTiles = true;

  public Vector2Int Size => size;
  public PivotMode CurrentPivotMode => pivotMode;
  public int InteractionPriority => interactionPriority;
  public bool BlocksTiles => blocksTiles;

  public List<Vector2Int> GetLocalOffsets() {
   var offsets = new List<Vector2Int>();
   for (int x = 0; x < size.x; x++) {
    for (int y = 0; y < size.y; y++) {
     offsets.Add(new Vector2Int(x, y));
    }
   }
   return offsets;
  }

  public Vector2 GetCenterOffset(float cellSize = 1f) {
   switch (pivotMode) {
    case PivotMode.Automatic:
     // Legacy behavior: Centers based on the (Size - 1) * 0.5 logic
     return new Vector2(
      (size.x - 1) * cellSize * 0.5f,
      (size.y - 1) * cellSize * 0.5f
     );

    case PivotMode.Manual:
     // Manual behavior: Uses defined units directly scaled by cell size
     return manualPivot * cellSize;

    case PivotMode.None:
    default:
     return Vector2.zero;
   }
  }

  public Vector3 GetCenteredWorldPosition(Vector3 anchorWorldPosition, float cellSize = 1f) {
   Vector2 offset = GetCenterOffset(cellSize);
   return anchorWorldPosition + new Vector3(offset.x, offset.y, 0f);
  }

  void OnValidate() {
   size.x = Mathf.Max(1, size.x);
   size.y = Mathf.Max(1, size.y);
  }
 }
}