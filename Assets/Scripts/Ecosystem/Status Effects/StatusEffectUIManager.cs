// Assets/Scripts/Ecosystem/StatusEffects/StatusEffectUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusEffectUIManager : MonoBehaviour
{
    [Header("UI Configuration")]
    [SerializeField] private Transform effectIconContainer; // Set this in prefab
    [SerializeField] private GameObject effectIconPrefab; // Simple UI icon prefab
    [SerializeField] private float iconSpacing = 35f;

    private StatusEffectManager statusManager;
    private Dictionary<string, StatusEffectIconUI> activeIcons = new Dictionary<string, StatusEffectIconUI>();

    public void Initialize(StatusEffectManager manager)
    {
        statusManager = manager;

        if (effectIconPrefab == null)
        {
            CreateDefaultIconPrefab();
        }
    }

    void Update()
    {
        if (statusManager == null || effectIconContainer == null) return;

        UpdateStatusIcons();
    }

    private void UpdateStatusIcons()
    {
        // Get current active effects
        var currentEffects = statusManager.GetActiveEffects();

        // Remove icons for expired effects
        var keysToRemove = new List<string>();
        foreach (var kvp in activeIcons)
        {
            if (!currentEffects.Exists(e => e.effect.effectID == kvp.Key))
            {
                Destroy(kvp.Value.gameObject);
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            activeIcons.Remove(key);
        }

        // Add/update icons for active effects
        int index = 0;
        foreach (var instance in currentEffects)
        {
            if (!activeIcons.ContainsKey(instance.effect.effectID))
            {
                CreateEffectIcon(instance);
            }

            // Update position
            if (activeIcons.TryGetValue(instance.effect.effectID, out var iconUI))
            {
                iconUI.transform.localPosition = new Vector3(index * iconSpacing, 0, 0);
                index++;
            }
        }
    }

    private void CreateEffectIcon(StatusEffectInstance instance)
    {
        GameObject iconObj = Instantiate(effectIconPrefab, effectIconContainer);
        iconObj.SetActive(true); // Ensure it's active when instantiated
        StatusEffectIconUI iconUI = iconObj.GetComponent<StatusEffectIconUI>();

        if (iconUI == null)
        {
            iconUI = iconObj.AddComponent<StatusEffectIconUI>();
        }

        iconUI.Initialize(instance);
        activeIcons[instance.effect.effectID] = iconUI;
    }

    private void CreateDefaultIconPrefab()
    {
        // Create a simple default icon prefab programmatically
        GameObject prefab = new GameObject("StatusEffectIcon");
        prefab.AddComponent<RectTransform>().sizeDelta = new Vector2(32, 32);

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(prefab.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        bg.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 32);

        // Icon (for sprite) container
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(prefab.transform, false);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.enabled = false; // Disabled by default
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
        
        // Unicode text (child of icon)
        GameObject unicodeTextGO = new GameObject("UnicodeText");
        unicodeTextGO.transform.SetParent(icon.transform, false);
        TextMeshProUGUI tmpText = unicodeTextGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = "?";
        tmpText.fontSize = 20;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enabled = false; // Disabled by default
        RectTransform textRect = tmpText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        effectIconPrefab = prefab;
        effectIconPrefab.SetActive(false); // Keep the source prefab inactive
    }
}