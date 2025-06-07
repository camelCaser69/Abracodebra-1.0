using UnityEngine;
using System.Text;

// NOTE: The ITooltipDataProvider interface is defined in UniversalTooltipManager.cs
// It is included here in comments for clarity, but not as a live definition.

[CreateAssetMenu(fileName = "Tool_", menuName = "Gameplay/New Tool Definition")]
public class ToolDefinition : ScriptableObject, ITooltipDataProvider
{
    #region Fields
    
    public ToolType toolType;
    public string displayName;

    [Header("Visuals")]
    public Sprite icon;
    public Color iconTint = Color.white;

    [Header("Usage")]
    public bool limitedUses = false;
    public int initialUses = 10;

    [Header("Auto-Add to Inventory")]
    public bool autoAddToInventory = true;

    #endregion

    #region ITooltipDataProvider Implementation

    public string GetTooltipTitle()
    {
        return displayName ?? "Unknown Tool";
    }

    public string GetTooltipDescription()
    {
        return $"Tool Type: {toolType}";
    }

    public string GetTooltipDetails(object source = null)
    {
        var sb = new StringBuilder();
        sb.Append(limitedUses ? $"<b>Uses:</b> {initialUses}" : "<b>Uses:</b> Unlimited");
        return sb.ToString().TrimEnd();
    }

    #endregion
}