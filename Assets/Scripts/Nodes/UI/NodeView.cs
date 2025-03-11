// Assets/Scripts/Nodes/UI/NodeView.cs
// A MonoBehaviour that visually represents a single NodeData in the editor.

using UnityEngine;
using UnityEngine.UI;
using TMPro;  // If using TextMeshPro

public class NodeView : MonoBehaviour
{
    [SerializeField] private TMP_Text nodeTitleText;
    [SerializeField] private Image backgroundImage;

    private NodeData nodeData;

    public void Initialize(NodeData data, Color color)
    {
        nodeData = data;
        if (nodeTitleText) nodeTitleText.text = data.nodeType;
        if (backgroundImage) backgroundImage.color = color;
    }

    // Optional convenience accessor
    public NodeData GetNodeData()
    {
        return nodeData;
    }
}