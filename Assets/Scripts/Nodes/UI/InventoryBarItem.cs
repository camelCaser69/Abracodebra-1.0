// FILE: Assets/Scripts/UI/InventoryBarItem.cs
using UnityEngine;

[System.Serializable]
public class InventoryBarItem
{
    public enum ItemType { Node, Tool }
    
    [SerializeField] private ItemType itemType;
    [SerializeField] private NodeData nodeData;
    [SerializeField] private NodeDefinition nodeDefinition;
    [SerializeField] private ToolDefinition toolDefinition;
    [SerializeField] private GameObject viewGameObject;
    
    public ItemType Type => itemType;
    public NodeData NodeData => nodeData;
    public NodeDefinition NodeDefinition => nodeDefinition;
    public ToolDefinition ToolDefinition => toolDefinition;
    public GameObject ViewGameObject => viewGameObject;
    
    // Factory methods
    public static InventoryBarItem FromNode(NodeData data, NodeDefinition def, GameObject viewObj = null)
    {
        return new InventoryBarItem
        {
            itemType = ItemType.Node,
            nodeData = data,
            nodeDefinition = def,
            toolDefinition = null,
            viewGameObject = viewObj
        };
    }
    
    public static InventoryBarItem FromTool(ToolDefinition tool, GameObject viewObj = null)
    {
        return new InventoryBarItem
        {
            itemType = ItemType.Tool,
            nodeData = null,
            nodeDefinition = null,
            toolDefinition = tool,
            viewGameObject = viewObj
        };
    }
    
    public string GetDisplayName()
    {
        return itemType == ItemType.Node ? 
            (nodeDefinition?.displayName ?? nodeData?.nodeDisplayName ?? "Unknown Node") : 
            (toolDefinition?.displayName ?? "Unknown Tool");
    }
    
    public Sprite GetIcon()
    {
        return itemType == ItemType.Node ? nodeDefinition?.thumbnail : toolDefinition?.icon;
    }
    
    public Color GetIconTint()
    {
        return itemType == ItemType.Node ? 
            (nodeDefinition?.thumbnailTintColor ?? Color.white) : 
            (toolDefinition?.iconTint ?? Color.white);
    }
    
    public bool IsValid()
    {
        return itemType == ItemType.Node ? 
            (nodeData != null && nodeDefinition != null) : 
            (toolDefinition != null);
    }
    
    public bool IsSeed()
    {
        return itemType == ItemType.Node && nodeData != null && nodeData.IsSeed();
    }
}