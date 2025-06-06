// FILE: Assets/Scripts/Tiles/Tools/ToolDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ToolDefinition", menuName = "Tiles/Tool Definition")]
public class ToolDefinition : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Which tool type this represents (e.g. Hoe, WateringCan).")]
    public ToolType toolType;
    [Tooltip("Human-readable name (for debugging/UI).")]
    public string displayName;

    [Header("Visuals")]
    [Tooltip("Icon sprite for the tool.")]
    public Sprite icon;
    [Tooltip("Tint color to apply to the icon sprite.")]
    public Color iconTint = Color.white;

    [Header("Usage Limits")]
    [Tooltip("If true, this tool has a limited number of uses.")]
    public bool limitedUses = false;
    [Tooltip("The number of uses the tool starts with (only relevant if Limited Uses is true).")]
    [Min(0)]
    public int initialUses = 10;
    
    [Header("Auto-Add to Inventory")] // NEW SECTION
    [Tooltip("If true, this tool will be automatically added to the inventory at the start of planning phase.")]
    public bool autoAddToInventory = true; // NEW FIELD
}