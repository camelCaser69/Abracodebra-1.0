// FILE: Assets/Scripts/UI/Tooltips/UniversalTooltipManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections;
using UnityEngine.EventSystems;

public class UniversalTooltipManager : MonoBehaviour
{
    public static UniversalTooltipManager Instance { get; set; }

    [SerializeField] GameObject tooltipPanel;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI detailsText;
    [SerializeField] Image backgroundImage;
    [SerializeField] CanvasGroup canvasGroup;

    [SerializeField] VerticalLayoutGroup layoutGroup;

    [SerializeField] float fadeDuration = 0.15f;
    [SerializeField] bool moveTooltipWithMouse = true;
    [SerializeField] Vector2 mouseFollowOffset = new Vector2(15f, -15f);

    [Header("Styling")]
    [SerializeField] Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] Color titleColor = Color.white;
    [SerializeField] Color descriptionColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] Color detailsColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] int titleFontSize = 18;
    [SerializeField] int descriptionFontSize = 14;
    [SerializeField] int detailsFontSize = 12;
    
    [Header("Content")]
    [SerializeField] bool showEffectsInDetails = true;
    [SerializeField] bool showSeedSequenceInfo = true;
    [SerializeField] string effectPrefix = "• ";
    [SerializeField] Color passiveEffectColor = new Color(0.6f, 0.8f, 1f, 1f);
    [SerializeField] Color activeEffectColor = new Color(1f, 0.8f, 0.6f, 1f);

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

    void Update()
    {
        // If the tooltip is visible but its target object has been destroyed or set to null,
        // hide the tooltip. This uses a helper method to avoid the CS0252 warning.
        if (isVisible && IsTargetNullOrDestroyed(currentTarget))
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

    public void ShowNodeTooltip(NodeData nodeData, NodeDefinition nodeDef, Transform anchor = null)
    {
        if (nodeData == null || nodeDef == null) return;

        // explicit reference check – removes CS0252
        if (isVisible && ReferenceEquals(currentTarget, nodeData)) return;

        currentTarget = nodeData;

        string title       = nodeDef.displayName ?? nodeData.nodeDisplayName ?? "Unknown Node";
        string description = nodeDef.description ?? string.Empty;
        string details     = BuildNodeDetails(nodeData, nodeDef);

        ShowTooltipInternal(title, description, details, anchor);
    }

    public void ShowToolTooltip(ToolDefinition toolDef, Transform anchor = null)
    {
        if (toolDef == null) return;

        if (isVisible && ReferenceEquals(currentTarget, toolDef)) return;

        currentTarget = toolDef;

        string title       = toolDef.displayName ?? "Unknown Tool";
        string description = $"Tool Type: {toolDef.toolType}";
        string details     = BuildToolDetails(toolDef);

        ShowTooltipInternal(title, description, details, anchor);
    }

    public void ShowInventoryItemTooltip(InventoryBarItem item, Transform anchor = null)
    {
        if (item == null || !item.IsValid()) return;

        object underlying = item.Type == InventoryBarItem.ItemType.Node
            ? (object)item.NodeData
            : item.ToolDefinition;

        if (isVisible && ReferenceEquals(currentTarget, underlying)) return;

        if (item.Type == InventoryBarItem.ItemType.Node)
            ShowNodeTooltip(item.NodeData, item.NodeDefinition, anchor);
        else
            ShowToolTooltip(item.ToolDefinition, anchor);
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
    
    // --- Private and Helper Methods ---

    /// <summary>
    /// Safely checks if the target object is null, correctly handling both standard C# objects
    /// and UnityEngine.Objects that may have been destroyed. This is the definitive fix for
    /// the CS0252 "unintended reference comparison" warning.
    /// </summary>
    private bool IsTargetNullOrDestroyed(object target)
    {
        // First, explicitly check if the object is a UnityEngine.Object.
        if (target is UnityEngine.Object unityObject)
        {
            // If it is, use Unity's custom '==' operator. This is the only
            // way to correctly check if a Unity object has been destroyed.
            return unityObject == null;
        }
        
        // If the object is NOT a UnityEngine.Object, it's a standard C# type.
        // For these types, a direct reference comparison is what we want.
        // Using object.ReferenceEquals is the most explicit way to do this
        // and guarantees we don't accidentally trigger Unity's overloaded operator.
        // This completely satisfies the compiler and removes the warning.
        return System.Object.ReferenceEquals(target, null);
    }

    private void ShowTooltipInternal(string title, string description, string details, Transform itemAnchor)
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
                        tooltipRect.localPosition = localPos;
                    }
                }
            }
        }

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

    private bool ValidateReferences()
    {
        if (tooltipPanel == null) { Debug.LogError("[UniversalTooltipManager] Tooltip Panel not assigned!"); return false; }
        
        if (canvasGroup == null) canvasGroup = tooltipPanel.GetComponent<CanvasGroup>() ?? tooltipPanel.AddComponent<CanvasGroup>();
        if (layoutGroup == null) layoutGroup = tooltipPanel.GetComponent<VerticalLayoutGroup>();
        if (backgroundImage == null) backgroundImage = tooltipPanel.GetComponent<Image>();
        if (titleText == null) titleText = FindTextComponent("Title");
        if (descriptionText == null) descriptionText = FindTextComponent("Description");
        if (detailsText == null) detailsText = FindTextComponent("Details");

        if (titleText == null) { Debug.LogError("[UniversalTooltipManager] Missing required title text component!"); return false; }

        return true;
    }
    
    private TextMeshProUGUI FindTextComponent(string nameContains)
    {
        if (tooltipPanel == null) return null;
        foreach (TextMeshProUGUI text in tooltipPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.name.ToLower().Contains(nameContains.ToLower()))
                return text;
        }
        return null;
    }
    
    private void SetupTooltipPanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (backgroundImage != null) backgroundImage.color = backgroundColor;
        ApplyTextStyles();
    }
    
    private void ApplyTextStyles()
    {
        if (titleText != null) { titleText.color = titleColor; titleText.fontSize = titleFontSize; titleText.fontStyle = FontStyles.Bold; }
        if (descriptionText != null) { descriptionText.color = descriptionColor; descriptionText.fontSize = descriptionFontSize; }
        if (detailsText != null) { detailsText.color = detailsColor; detailsText.fontSize = detailsFontSize; }
    }
    
    private string BuildNodeDetails(NodeData nodeData, NodeDefinition nodeDef)
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
            if (nodeData.storedSequence != null)
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append("\n");
                sb.Append("<b>Seed Sequence:</b> ");
                sb.Append((nodeData.storedSequence.nodes != null && nodeData.storedSequence.nodes.Count > 0) ?
                    $"{nodeData.storedSequence.nodes.Count} nodes" : "Empty");
            }
            else
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append("\n");
                sb.Append("<b>Seed Sequence:</b> Not Initialized");
            }
        }
        return sb.ToString().TrimEnd();
    }

    private string BuildToolDetails(ToolDefinition toolDef)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(toolDef.limitedUses ? $"<b>Uses:</b> {toolDef.initialUses}\n" : "<b>Uses:</b> Unlimited\n");
        return sb.ToString().TrimEnd();
    }
    
    private string FormatEffectValue(NodeEffectData effect)
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
    
    private IEnumerator FadeTooltip(bool fadeIn)
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
}