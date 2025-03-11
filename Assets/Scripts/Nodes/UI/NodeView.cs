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
    [SerializeField] private VerticalLayoutGroup inputPinsContainer;
    [SerializeField] private VerticalLayoutGroup outputPinsContainer;

    private NodeData nodeData;

    // We add a 'displayName' parameter to make it simpler
    public void Initialize(NodeData data, Color color, string displayName)
    {
        nodeData = data;
        if (nodeTitleText) nodeTitleText.text = displayName;
        if (backgroundImage) backgroundImage.color = color;
    }

    public void GeneratePins(List<NodePort> inputs, List<NodePort> outputs)
    {
        // Clear existing pins if needed
        foreach (Transform child in inputPinsContainer.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in outputPinsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Generate input pins
        foreach (var inputPort in inputs)
        {
            CreatePin(inputPinsContainer.transform, inputPort, true);
        }

        // Generate output pins
        foreach (var outputPort in outputs)
        {
            CreatePin(outputPinsContainer.transform, outputPort, false);
        }
    }

    private void CreatePin(Transform parent, NodePort port, bool isInput)
    {
        // For demonstration, let's just create a small button or image
        GameObject pinObj = new GameObject(isInput ? "InputPin" : "OutputPin", typeof(RectTransform));
        pinObj.transform.SetParent(parent);
        var rect = pinObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 20);

        // Add an Image to make it visible
        var img = pinObj.AddComponent<Image>();
        img.color = isInput ? Color.blue : Color.green;
        
        // (Optional) store references somewhere so you can connect them with lines
        // e.g. pinObj.AddComponent<PinView>().Initialize(port);

        // If you want to show the portName, you can add a tooltip or small text next to it
    }

    public NodeData GetNodeData()
    {
        return nodeData;
    }
}
