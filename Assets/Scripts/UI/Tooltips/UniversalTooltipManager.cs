using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// A common interface for any object that can provide data to the tooltip system.
/// </summary>
public interface ITooltipDataProvider
{
    string GetTooltipTitle();
    string GetTooltipDescription();
    string GetTooltipDetails(object source = null);
}

public class UniversalTooltipManager : MonoBehaviour
{
    public static UniversalTooltipManager Instance { get; private set; }

    #region Serialized Fields
    [Header("UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private VerticalLayoutGroup layoutGroup;

    [Header("Behavior")]
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private bool moveTooltipWithMouse = true;
    [SerializeField] private Vector2 mouseFollowOffset = new Vector2(15f, -15f);

    [Header("Styling")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color descriptionColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color detailsColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private int titleFontSize = 18;
    [SerializeField] private int descriptionFontSize = 14;
    [SerializeField] private int detailsFontSize = 12;
    #endregion

    private Coroutine _fadeCoroutine;
    private object _currentTarget;
    private bool _isVisible = false;

    private void Awake()
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
        _isVisible = false;
    }
    
    private void Update()
    {
        if (_isVisible && IsTargetNullOrDestroyed(_currentTarget))
        {
            // If the target is gone, hide the tooltip immediately.
            HideTooltip();
        }
    }
    
    /// <summary>
    /// Shows the tooltip for any object that provides tooltip data.
    /// </summary>
    /// <param name="provider">The object providing the data (e.g., a NodeDefinition or ToolDefinition).</param>
    /// <param name="anchor">The transform to anchor the tooltip to (optional).</param>
    /// <param name="source">Additional context data, like a NodeData instance (optional).</param>
    public void ShowTooltip(ITooltipDataProvider provider, Transform anchor = null, object source = null)
    {
        if (provider == null) return;
        
        // Don't re-show for the same object
        if (_isVisible && ReferenceEquals(_currentTarget, provider)) return;

        _currentTarget = provider;

        string title = provider.GetTooltipTitle();
        string description = provider.GetTooltipDescription();
        string details = provider.GetTooltipDetails(source);

        ShowTooltipInternal(title, description, details, anchor);
    }

    public void HideTooltip()
    {
        _currentTarget = null;
        if (tooltipPanel == null || canvasGroup == null) return;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        if (fadeDuration > 0f && gameObject.activeInHierarchy)
        {
            _fadeCoroutine = StartCoroutine(FadeTooltip(false));
        }
        else
        {
            canvasGroup.alpha = 0f;
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
            _isVisible = false;
        }
    }

    private void ShowTooltipInternal(string title, string description, string details, Transform itemAnchor)
    {
        if (tooltipPanel == null || canvasGroup == null) return;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

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
        // Force layout rebuild to get correct size before positioning
        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }
        
        if (moveTooltipWithMouse)
        {
            PositionTooltipWithMouse();
        }

        if (fadeDuration > 0f)
        {
            _fadeCoroutine = StartCoroutine(FadeTooltip(true));
        }
        else
        {
            canvasGroup.alpha = 1f;
            _isVisible = true;
        }
    }

    private void PositionTooltipWithMouse()
    {
        var tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        var rootCanvas = tooltipPanel.GetComponentInParent<Canvas>()?.rootCanvas;

        if (tooltipRect == null || rootCanvas == null || !Input.mousePresent) return;
        
        Vector2 targetScreenPos = Input.mousePosition;
        targetScreenPos += mouseFollowOffset; // Apply user offset
        
        // Clamp to screen boundaries
        var panelRect = tooltipRect.rect;
        targetScreenPos.x = Mathf.Clamp(targetScreenPos.x, 0, Screen.width - panelRect.width);
        targetScreenPos.y = Mathf.Clamp(targetScreenPos.y, 0, Screen.height - panelRect.height);
        
        var parentRect = tooltipRect.parent as RectTransform;
        if (parentRect == null) return;

        Camera renderCamera = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPos, renderCamera, out var localPos))
        {
            tooltipRect.localPosition = localPos;
        }
    }

    private IEnumerator FadeTooltip(bool fadeIn)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;

        if (fadeIn) _isVisible = true;

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

        if (!fadeIn)
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
            _isVisible = false;
        }
    }

    #region Setup and Validation
    
    private bool IsTargetNullOrDestroyed(object target)
    {
        if (target is UnityEngine.Object unityObject)
        {
            return unityObject == null;
        }
        return System.Object.ReferenceEquals(target, null);
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
        foreach (var text in tooltipPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.name.ToLower().Contains(nameContains.ToLower()))
                return text;
        }
        return null;
    }
    
    #endregion
}