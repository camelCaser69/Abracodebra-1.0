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
    [SerializeField] private TMP_Text manaStorageText;
    [SerializeField] private TMP_Text effectsText;
    [SerializeField] private TMP_Text descriptionText; // Separate text field for description

    private NodeData nodeData;

    private void Update()
    {
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
        if (nodeTitleText) 
            nodeTitleText.text = displayName;
        if (backgroundImage) 
            backgroundImage.color = color;

        // Build effects text.
        if (effectsText)
        {
            if (nodeData.effects.Count == 0)
                effectsText.text = "No Effects";
            else
            {
                string str = "Effects:\n";
                foreach (var eff in nodeData.effects)
                    str += $"- {eff.effectType} ({eff.effectValue})\n";
                effectsText.text = str;
            }
        }

        // Display description in cursive using <i> tags.
        if (descriptionText)
            descriptionText.text = $"{nodeData.description}";

        // If node has a ManaStorage effect, update manaStorageText.
        var manaEff = nodeData.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
        if (manaEff != null && manaStorageText != null)
            manaStorageText.text = $"Mana: {manaEff.secondaryValue}/{manaEff.effectValue}";
    }

    public void GeneratePins(List<NodePort> inputs, List<NodePort> outputs)
    {
        foreach (Transform child in inputPinsContainer)
            Destroy(child.gameObject);
        foreach (Transform child in outputPinsContainer)
            Destroy(child.gameObject);

        foreach (var input in inputs)
            CreatePin(inputPinsContainer, input, true);
        foreach (var output in outputs)
            CreatePin(outputPinsContainer, output, false);
    }

    private void CreatePin(Transform parent, NodePort port, bool isInput)
    {
        GameObject pinObj = new GameObject(isInput ? "InputPin" : "OutputPin", typeof(RectTransform));
        pinObj.transform.SetParent(parent, false);
        RectTransform rt = pinObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(20, 20);
        Image img = pinObj.AddComponent<Image>();
        switch (port.portType)
        {
            case PortType.Mana:
                img.color = Color.cyan;
                break;
            case PortType.Condition:
                img.color = new Color(1f, 0.65f, 0f);
                break;
            default:
                img.color = Color.blue;
                break;
        }
        PinView pinView = pinObj.AddComponent<PinView>();
        NodeEditorController controller = UnityEngine.Object.FindFirstObjectByType<NodeEditorController>();
        pinView.Initialize(port, isInput, controller);
    }

    public NodeData GetNodeData() => nodeData;
}
