using UnityEngine;

[CreateAssetMenu(fileName = "ToolDefinition", menuName = "Tiles/Tool Definition")]
public class ToolDefinition : ScriptableObject
{
    [Tooltip("Which tool type this represents (e.g. Hoe, WateringCan).")]
    public ToolType toolType;

    [Tooltip("Human-readable name (for debugging/UI).")]
    public string displayName;

    [Tooltip("Icon sprite for the tool.")]
    public Sprite icon;

    [Tooltip("Tint color to apply to the icon sprite.")]
    public Color iconTint = Color.white;
}