using UnityEngine;

[CreateAssetMenu(fileName = "TileHoverColorManager", menuName = "World Interaction/Tile Hover Color Manager")]
public class TileHoverColorManager : ScriptableObject {
    [Header("Hover Tile Colors")]
    [SerializeField, Tooltip("Color when player is within range to interact")]
    private Color withinRangeColor = new Color(1f, 1f, 1f, 0.8f);
    
    [SerializeField, Tooltip("Color when player is outside interaction range")]
    private Color outsideRangeColor = new Color(1f, 1f, 1f, 0.3f);

    public Color WithinRangeColor => withinRangeColor;
    public Color OutsideRangeColor => outsideRangeColor;

    public Color GetColorForRange(bool isWithinRange) {
        return isWithinRange ? withinRangeColor : outsideRangeColor;
    }

    // Editor validation
    private void OnValidate() {
        // Ensure alpha values make sense
        if (withinRangeColor.a < outsideRangeColor.a) {
            Debug.LogWarning("[TileHoverColorManager] Within range alpha should typically be higher than outside range alpha for better visibility.");
        }
    }
}