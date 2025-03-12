// Assets/Scripts/Nodes/UI/NodeView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;


public class NodeView : MonoBehaviour
{
    [SerializeField] private TMP_Text nodeTitleText;
    [SerializeField] private Image backgroundImage;

    [Header("Pin Containers")]
    [SerializeField] private Transform inputPinsContainer;
    [SerializeField] private Transform outputPinsContainer;

    [Header("Node Info Display")]
    [SerializeField] private TMP_Text manaStorageText; // For real-time mana display
    [SerializeField] private TMP_Text effectsText;  // Where we list the node's effects

    private NodeData nodeData;

    private void Update()
    {
        // If the node has a ManaStorage effect, display
        var manaEff = nodeData.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
        if (manaEff != null && manaStorageText != null)
        {
            float cap = Mathf.Floor(manaEff.effectValue);
            float cur = Mathf.Floor(manaEff.secondaryValue);
            manaStorageText.text = $"Mana: {cur}/{cap}";
        }
    }


    
    public void Initialize(NodeData data, Color color, string displayName)
    {
        nodeData = data;
        if (nodeTitleText) nodeTitleText.text = displayName;
        if (backgroundImage) backgroundImage.color = color;

        // Display effects (as before)
        if (effectsText)
        {
            if (nodeData.effects.Count == 0)
            {
                effectsText.text = "No Effects";
            }
            else
            {
                string str = "Effects:\n";
                foreach (var eff in nodeData.effects)
                {
                    str += $"- {eff.effectType} ({eff.effectValue})\n";
                }
                effectsText.text = str;
            }
        }

        // If node has a ManaStorage effect, display it
        var manaEff = nodeData.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
        if (manaEff != null && manaStorageText != null)
        {
            manaStorageText.text = $"Mana: {manaEff.secondaryValue}/{manaEff.effectValue}";
        }
    }

    public void GeneratePins(List<NodePort> inputs, List<NodePort> outputs)
    {
        // Clear existing pins
        foreach (Transform child in inputPinsContainer)
            Destroy(child.gameObject);
        foreach (Transform child in outputPinsContainer)
            Destroy(child.gameObject);

        // Create input pins
        foreach (var input in inputs)
        {
            CreatePin(inputPinsContainer, input, true);
        }
        // Create output pins
        foreach (var output in outputs)
        {
            CreatePin(outputPinsContainer, output, false);
        }
    }

    private void CreatePin(Transform parent, NodePort port, bool isInput)
    {
        // Create a new UI GameObject for the pin.
        GameObject pinObj = new GameObject(isInput ? "InputPin" : "OutputPin", typeof(RectTransform));
        pinObj.transform.SetParent(parent, false);  // false to keep local scaling

        // Set a fixed size for the pin.
        RectTransform rt = pinObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(20, 20);

        // Add an Image component to make it visible.
        Image img = pinObj.AddComponent<Image>();
        // Color mapping based on port type.
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

        // Add the PinView component to enable connection behavior.
        PinView pinView = pinObj.AddComponent<PinView>();
        // Use FindFirstObjectByType if available, otherwise FindObjectOfType.
        NodeEditorController controller = UnityEngine.Object.FindFirstObjectByType<NodeEditorController>();
        pinView.Initialize(port, isInput, controller);
    }


    public NodeData GetNodeData() => nodeData;
}
