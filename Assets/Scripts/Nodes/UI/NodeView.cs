// Assets/Scripts/Nodes/UI/NodeView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NodeView : MonoBehaviour
{
    [SerializeField] private TMP_Text nodeTitleText;
    [SerializeField] private Image backgroundImage;

    [Header("Pin Containers")]
    [SerializeField] private Transform inputPinsContainer;   // e.g., a Panel with VerticalLayoutGroup (left side)
    [SerializeField] private Transform outputPinsContainer;  // (right side)

    private NodeData nodeData;

    // Initialize the node view with data, color, and display name.
    public void Initialize(NodeData data, Color color, string displayName)
    {
        nodeData = data;
        if (nodeTitleText) nodeTitleText.text = displayName;
        if (backgroundImage) backgroundImage.color = color;
    }

    // Generate pin UI elements based on provided port lists.
    // Assets/Scripts/Nodes/UI/NodeView.cs
    public void GeneratePins(List<NodePort> inputs, List<NodePort> outputs)
    {
        // Find the NodeEditorController in the scene using the new recommended method.
        NodeEditorController controller = FindFirstObjectByType<NodeEditorController>();  

        foreach (var input in inputs)
        {
            CreatePin(inputPinsContainer, input, true, controller);
        }

        foreach (var output in outputs)
        {
            CreatePin(outputPinsContainer, output, false, controller);
        }
    }

    private void CreatePin(Transform parent, NodePort port, bool isInput, NodeEditorController controller)
    {
        GameObject pinObj = new GameObject(isInput ? "InputPin" : "OutputPin", typeof(RectTransform));
        pinObj.transform.SetParent(parent);
        RectTransform rt = pinObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(20, 20);

        Image img = pinObj.AddComponent<Image>();

        switch (port.portType)
        {
            case PortType.Mana:
                img.color = Color.cyan;
                break;
            case PortType.Condition:
                img.color = new Color(1f, 0.65f, 0f); // Orange
                break;
            default:
                img.color = Color.blue;
                break;
        }

        PinView pinView = pinObj.AddComponent<PinView>();
        pinView.Initialize(port, isInput, controller);
    }


    public NodeData GetNodeData()
    {
        return nodeData;
    }
}

