using UnityEngine;

[CreateAssetMenu(fileName = "ToolDefinition", menuName = "Tiles/Tool Definition")]
public class ToolDefinition : ScriptableObject
{
    public ToolType toolType;
    public string displayName;
    public Sprite icon;
}