using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class NodeView : MonoBehaviour
{
    [Header("Text Components")]
    [SerializeField] private TMP_Text nodeTitleText;
    [SerializeField] private Image backgroundImage;

    [Header("Node Info Display")]
    [SerializeField] private TMP_Text manaStorageText;
    [SerializeField] private TMP_Text effectsText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Pins Settings")]
    [SerializeField] private float pinRadius = 60f; // Distance from node center to pin (auto-updated from HexGridManager if available)
    [SerializeField] private Transform pinContainer; // Parent container for spawned pins

    [Header("Pin Customization")]
    public Sprite manaPinSprite;
    public Color manaPinColor = Color.cyan;
    public Sprite conditionPinSprite;
    public Color conditionPinColor = new Color(1f, 0.65f, 0f);
    public Sprite generalPinSprite;
    public Color generalPinColor = Color.blue;
    [Tooltip("Uniform additional rotation (in degrees) applied to port sprites.")]
    public float portSpriteRotationOffset = 0f;

    [Header("Pin Scale Settings")]
    [Tooltip("General multiplier applied to all port sprites.")]
    public float portSpriteScaleMultiplier = 1.0f;
    [Tooltip("Multiplier for input port sprites.")]
    public float inputPortScaleMultiplier = 1.0f;
    [Tooltip("Multiplier for output port sprites.")]
    public float outputPortScaleMultiplier = 1.0f;

    private NodeData nodeData;

    private void Awake()
    {
        if (pinContainer == null)
            pinContainer = this.transform;
        if (HexGridManager.Instance != null)
            pinRadius = HexGridManager.Instance.hexSize * HexGridManager.Instance.pinRadiusMultiplier;
    }

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

        if (effectsText != null)
        {
            if (nodeData.effects.Count == 0)
                effectsText.text = "No Effects";
            else
            {
                string str = "";              //"Effects:\n";
                foreach (var eff in nodeData.effects)
                    str += $"- {eff.effectType} ({eff.effectValue})\n";
                effectsText.text = str;
            }
        }

        if (descriptionText != null)
            descriptionText.text = nodeData.description;

        var manaEff = nodeData.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
        if (manaEff != null && manaStorageText != null)
            manaStorageText.text = $"Mana: {manaEff.secondaryValue}/{manaEff.effectValue}";
    }

    // Generate pins for each port in the node.
    public void GeneratePins(List<NodePort> ports)
    {
        foreach (Transform child in pinContainer)
            Destroy(child.gameObject);
        foreach (var port in ports)
            CreatePin(port);
    }

    private void CreatePin(NodePort port)
    {
        GameObject pinObj = new GameObject(port.isInput ? "InputPin" : "OutputPin", typeof(RectTransform));
        pinObj.transform.SetParent(pinContainer, false);
        RectTransform rt = pinObj.GetComponent<RectTransform>();

        // Calculate scale multiplier based on port type.
        float scaleMultiplier = portSpriteScaleMultiplier * (port.isInput ? inputPortScaleMultiplier : outputPortScaleMultiplier);
        rt.sizeDelta = new Vector2(20, 20) * scaleMultiplier;

        // For flat-top hexagons, pins should be centered on each side.
        // Mapping: Top = 90°, One = 30°, Two = -30°, Three = -90°, Four = -150°, Five = -210°.
        int sideIndex = (int)port.side; // Top=0, One=1, ..., Five=5.
        float posAngle = 90f - sideIndex * 60f; // This yields: Top:90, One:30, Two:-30, Three:-90, Four:-150, Five:-210.
        float posRad = posAngle * Mathf.Deg2Rad;
        float x = pinRadius * Mathf.Cos(posRad);
        float y = pinRadius * Mathf.Sin(posRad);
        rt.anchoredPosition = new Vector2(x, y);

        // Set sprite rotation: for outputs, arrow points out; for inputs, arrow points in (add 180°).
        float spriteAngle = posAngle;
        if (port.isInput)
            spriteAngle += 180f;
        spriteAngle += portSpriteRotationOffset;
        rt.localRotation = Quaternion.Euler(0, 0, spriteAngle);

        Image img = pinObj.AddComponent<Image>();
        switch (port.portType)
        {
            case PortType.Mana:
                img.sprite = manaPinSprite;
                img.color = manaPinColor;
                break;
            case PortType.Condition:
                img.sprite = conditionPinSprite;
                img.color = conditionPinColor;
                break;
            default:
                img.sprite = generalPinSprite;
                img.color = generalPinColor;
                break;
        }
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
    }

    public NodeData GetNodeData() => nodeData;
}
