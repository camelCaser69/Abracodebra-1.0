using UnityEngine;
using UnityEngine.UI; // Required for Image, VerticalLayoutGroup, CanvasGroup
using TMPro; // Required for TextMeshProUGUI
using System.Text; // Required for StringBuilder
using System.Collections; // Required for IEnumerator
using UnityEngine.EventSystems; // Required for EventSystem

public class UniversalTooltipManager : MonoBehaviour
{
    public static UniversalTooltipManager Instance { get; set; }

    [Header("UI References")]
    [SerializeField] GameObject tooltipPanel;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI detailsText;
    [SerializeField] Image backgroundImage;
    [SerializeField] CanvasGroup canvasGroup; // Ensure this is assigned or it will be added

    [SerializeField] VerticalLayoutGroup layoutGroup; // Optional, if you use it for auto-sizing

    [Header("Appearance & Behavior")]
    [SerializeField] float fadeDuration = 0.15f;

    [Header("Positioning")]
    [Tooltip("If true, the tooltip panel will follow the mouse cursor. If false, it stays in its static editor position.")]
    [SerializeField] bool moveTooltipWithMouse = true;
    [Tooltip("The X/Y offset from the mouse cursor if 'Move Tooltip With Mouse' is enabled. Can be negative.")]
    [SerializeField] Vector2 mouseFollowOffset = new Vector2(15f, -15f);


    [Header("Default Styles")]
    [SerializeField] Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] Color titleColor = Color.white;
    [SerializeField] Color descriptionColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] Color detailsColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] int titleFontSize = 18;
    [SerializeField] int descriptionFontSize = 14;
    [SerializeField] int detailsFontSize = 12;

    [Header("Content Options")]
    [SerializeField] bool showEffectsInDetails = true;
    [SerializeField] bool showSeedSequenceInfo = true;
    [SerializeField] string effectPrefix = "• ";
    [SerializeField] Color passiveEffectColor = new Color(0.6f, 0.8f, 1f, 1f); // Light blue
    [SerializeField] Color activeEffectColor = new Color(1f, 0.8f, 0.6f, 1f); // Light orange

    Coroutine fadeCoroutine;
    object currentTarget;
    bool isVisible = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        SetupTooltipPanel();
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        isVisible = false;
    }

    bool ValidateReferences()
    {
        if (tooltipPanel == null) { Debug.LogError("[UniversalTooltipManager] Tooltip Panel not assigned!"); return false; }

        if (canvasGroup == null) canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) // If still null, add one
        {
            Debug.LogWarning("[UniversalTooltipManager] CanvasGroup not found on Tooltip Panel, adding one.", tooltipPanel);
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        }

        if (layoutGroup == null) layoutGroup = tooltipPanel.GetComponent<VerticalLayoutGroup>();
        if (backgroundImage == null) backgroundImage = tooltipPanel.GetComponent<Image>();


        if (titleText == null) titleText = FindTextComponent("Title");
        if (descriptionText == null) descriptionText = FindTextComponent("Description"); // Optional
        if (detailsText == null) detailsText = FindTextComponent("Details"); // Optional

        if (titleText == null) { Debug.LogError("[UniversalTooltipManager] Missing required title text component!"); return false; }

        return true;
    }

    TextMeshProUGUI FindTextComponent(string nameContains)
    {
        if (tooltipPanel == null) return null;
        foreach (TextMeshProUGUI text in tooltipPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.name.ToLower().Contains(nameContains.ToLower()))
                return text;
        }
        return null;
    }


    void SetupTooltipPanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (tooltipPanel != null)
        {
            ContentSizeFitter sizeFitter = tooltipPanel.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null && layoutGroup != null)
            {
                Destroy(sizeFitter);
            }
        }

        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        ApplyTextStyles();
    }

    void ApplyTextStyles()
    {
        if (titleText != null)
        {
            titleText.color = titleColor;
            titleText.fontSize = titleFontSize;
            titleText.fontStyle = FontStyles.Bold;
        }

        if (descriptionText != null)
        {
            descriptionText.color = descriptionColor;
            descriptionText.fontSize = descriptionFontSize;
        }

        if (detailsText != null)
        {
            detailsText.color = detailsColor;
            detailsText.fontSize = detailsFontSize;
        }
    }

    public void ShowNodeTooltip(NodeData nodeData, NodeDefinition nodeDef, Transform anchor = null)
    {
        if (nodeData == null || nodeDef == null) return;
        if (isVisible && currentTarget == nodeData) return;

        currentTarget = nodeData;
        string title = nodeDef.displayName ?? nodeData.nodeDisplayName ?? "Unknown Node";
        string description = nodeDef.description ?? "";
        string details = BuildNodeDetails(nodeData, nodeDef);
        ShowTooltipInternal(title, description, details, anchor);
    }

    public void ShowToolTooltip(ToolDefinition toolDef, Transform anchor = null)
    {
        if (toolDef == null) return;
        if (isVisible && currentTarget == toolDef) return;

        currentTarget = toolDef;
        string title = toolDef.displayName ?? "Unknown Tool";
        string description = $"Tool Type: {toolDef.toolType}";
        string details = BuildToolDetails(toolDef);
        ShowTooltipInternal(title, description, details, anchor);
    }

    public void ShowInventoryItemTooltip(InventoryBarItem item, Transform anchor = null)
    {
        if (item == null || !item.IsValid()) return;

        object itemUnderlyingData = item.Type == InventoryBarItem.ItemType.Node ? (object)item.NodeData : (object)item.ToolDefinition;
        if (isVisible && currentTarget == itemUnderlyingData) return;

        if (item.Type == InventoryBarItem.ItemType.Node)
        {
            ShowNodeTooltip(item.NodeData, item.NodeDefinition, anchor);
        }
        else if (item.Type == InventoryBarItem.ItemType.Tool)
        {
            ShowToolTooltip(item.ToolDefinition, anchor);
        }
    }

    void ShowTooltipInternal(string title, string description, string details, Transform itemAnchor)
    {
        if (tooltipPanel == null || canvasGroup == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (titleText != null)
        {
            titleText.text = title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }
        if (descriptionText != null)
        {
            descriptionText.text = description;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
        }
        if (detailsText != null)
        {
            detailsText.text = details;
            detailsText.gameObject.SetActive(!string.IsNullOrEmpty(details));
        }

        tooltipPanel.SetActive(true);
        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }

        // --- Positioning Logic ---
        if (moveTooltipWithMouse)
        {
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            Canvas rootCanvas = tooltipPanel.GetComponentInParent<Canvas>()?.rootCanvas;

            if (tooltipRect != null && rootCanvas != null && Input.mousePresent)
            {
                Vector2 targetScreenPos = Input.mousePosition;
                targetScreenPos += mouseFollowOffset; // Apply user offset

                RectTransform parentRect = tooltipRect.parent as RectTransform;
                if (parentRect != null)
                {
                    Vector2 localPos;
                    Camera renderCamera = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPos, renderCamera, out localPos))
                    {
                        // TODO: Implement screen boundary checks here if needed
                        tooltipRect.localPosition = localPos;
                    }
                }
            }
        }
        // If !moveTooltipWithMouse, the tooltip remains at its current (static) position.

        if (fadeDuration > 0f)
        {
            fadeCoroutine = StartCoroutine(FadeTooltip(true));
        }
        else
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            isVisible = true;
        }
    }

    public void HideTooltip()
    {
        currentTarget = null;
        if (tooltipPanel == null || canvasGroup == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (fadeDuration > 0f && gameObject.activeInHierarchy)
        {
            fadeCoroutine = StartCoroutine(FadeTooltip(false));
        }
        else
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
            isVisible = false;
        }
    }

    string BuildNodeDetails(NodeData nodeData, NodeDefinition nodeDef)
    {
        StringBuilder sb = new StringBuilder();
        if (showEffectsInDetails && nodeData.effects != null && nodeData.effects.Count > 0)
        {
            sb.Append("<b>Effects:</b>\n");
            bool hasAnyEffectText = false;
            foreach (var effect in nodeData.effects)
            {
                if (effect == null) continue;
                Color effectColor = effect.isPassive ? passiveEffectColor : activeEffectColor;
                string hexColor = ColorUtility.ToHtmlStringRGB(effectColor);
                sb.Append($"<color=#{hexColor}>{effectPrefix}{effect.effectType}: ");
                sb.Append(FormatEffectValue(effect));
                sb.Append("</color>\n");
                hasAnyEffectText = true;
            }
            if (!hasAnyEffectText) { sb.Length -= "<b>Effects:</b>\n".Length; }
        }

        if (showSeedSequenceInfo && nodeData.IsSeed())
        {
            if (nodeData.storedSequence != null) {
                if (sb.Length > 0 && sb[sb.Length -1] != '\n') sb.Append("\n");
                sb.Append("<b>Seed Sequence:</b> ");
                sb.Append((nodeData.storedSequence.nodes != null && nodeData.storedSequence.nodes.Count > 0) ?
                           $"{nodeData.storedSequence.nodes.Count} nodes" : "Empty");
            } else {
                 if (sb.Length > 0 && sb[sb.Length -1] != '\n') sb.Append("\n");
                 sb.Append("<b>Seed Sequence:</b> Not Initialized");
            }
        }
        return sb.ToString().TrimEnd();
    }

    string BuildToolDetails(ToolDefinition toolDef)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(toolDef.limitedUses ? $"<b>Uses:</b> {toolDef.initialUses}\n" : "<b>Uses:</b> Unlimited\n");
        return sb.ToString().TrimEnd();
    }

    string FormatEffectValue(NodeEffectData effect)
    {
        string result = effect.primaryValue.ToString("G3");
        bool hasSecondaryValue = false;
        switch (effect.effectType)
        {
            case NodeEffectType.ScentModifier:
            case NodeEffectType.PoopFertilizer:
                hasSecondaryValue = true;
                break;
            default:
                if (effect.secondaryValue != 0) hasSecondaryValue = true;
                break;
        }
        if (hasSecondaryValue)
        {
            result += $" / {effect.secondaryValue.ToString("G3")}";
        }
        if (effect.effectType == NodeEffectType.ScentModifier && effect.scentDefinitionReference != null)
        {
            result += $" ({effect.scentDefinitionReference.displayName})";
        }
        return result;
    }

    IEnumerator FadeTooltip(bool fadeIn)
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;

        if (fadeIn) isVisible = true;

        if (fadeIn && tooltipPanel != null && !tooltipPanel.activeSelf)
            tooltipPanel.SetActive(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = (fadeDuration > 0) ? elapsed / fadeDuration : 1f;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (!fadeIn)
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
            isVisible = false;
        }
    }

    public void RefreshStyles()
    {
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;
        ApplyTextStyles();
    }

    public void DebugLogState()
    {
        Debug.Log($"[UniversalTooltipManager] Panel Active: {tooltipPanel?.activeSelf}, Alpha: {canvasGroup?.alpha}, IsVisible: {isVisible}, CurrentTarget: {currentTarget?.ToString() ?? "null"}");
    }

    void Update()
    {
        if (isVisible && currentTarget == null)
        {
            if (canvasGroup != null && canvasGroup.alpha > 0.01f)
            {
                if (fadeCoroutine == null || canvasGroup.alpha == 1f)
                {
                    HideTooltip();
                }
            }
        }
    }
}