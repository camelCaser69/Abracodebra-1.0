// Assets/Scripts/Ecosystem/StatusEffects/StatusEffectUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class StatusEffectUIManager : MonoBehaviour
{
    [Header("UI Configuration")]
    [SerializeField] private Transform effectIconContainer; // Set this in prefab
    [SerializeField] private GameObject effectIconPrefab; // Simple UI icon prefab

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

    void Start()
    {
        // This check ensures that the UIManager is properly initialized before it starts trying to update.
        if (statusManager == null)
        {
            Debug.LogError($"[{GetType().Name}] StatusManager reference is null. This component was not initialized correctly. Disabling.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (statusManager == null || effectIconContainer == null) return;

        UpdateStatusIcons();
    }

    private void UpdateStatusIcons()
    {
        var currentEffectInstances = statusManager.GetActiveEffects();
        var currentEffectIDs = currentEffectInstances.Select(e => e.effect.effectID).ToList();
        var displayedIconIDs = activeIcons.Keys.ToList();

        // 1. Remove icons for effects that are no longer active
        foreach (var id in displayedIconIDs)
        {
            if (!currentEffectIDs.Contains(id))
            {
                if (activeIcons.TryGetValue(id, out StatusEffectIconUI iconToDestroy))
                {
                    if (iconToDestroy != null) Destroy(iconToDestroy.gameObject);
                }
                activeIcons.Remove(id);
            }
        }

        // 2. Add icons for new effects that aren't displayed yet
        foreach (var instance in currentEffectInstances)
        {
            if (!activeIcons.ContainsKey(instance.effect.effectID))
            {
                CreateEffectIcon(instance);
            }
        }
        
        // 3. Ensure correct order by re-ordering the transforms in the hierarchy
        for (int i = 0; i < currentEffectInstances.Count; i++)
        {
            string effectID = currentEffectInstances[i].effect.effectID;
            if (activeIcons.TryGetValue(effectID, out StatusEffectIconUI iconUI))
            {
                iconUI.transform.SetSiblingIndex(i);
            }
        }
    }

    private void CreateEffectIcon(StatusEffectInstance instance)
    {
        if (effectIconPrefab == null)
        {
            Debug.LogError("Effect Icon Prefab is missing!", this);
            return;
        }

        GameObject iconObj = Instantiate(effectIconPrefab, effectIconContainer);
        iconObj.SetActive(true);
        StatusEffectIconUI iconUI = iconObj.GetComponent<StatusEffectIconUI>();

        if (iconUI == null) iconUI = iconObj.AddComponent<StatusEffectIconUI>();

        iconUI.Initialize(instance);
        activeIcons[instance.effect.effectID] = iconUI;
    }

    private void CreateDefaultIconPrefab()
    {
        // <<< SIZES ARE NOW MUCH SMALLER TO WORK IN WORLD SPACE
        float iconSize = 0.32f; // e.g., 0.32 world units
        float iconPadding = 0.04f;
        float fontSize = 0.2f;

        GameObject prefab = new GameObject("StatusEffectIcon");
        prefab.AddComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
        prefab.AddComponent<LayoutElement>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(prefab.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        bg.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(prefab.transform, false);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.enabled = false;
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize - iconPadding, iconSize - iconPadding);
        
        GameObject unicodeTextGO = new GameObject("UnicodeText");
        unicodeTextGO.transform.SetParent(icon.transform, false);
        TextMeshProUGUI tmpText = unicodeTextGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = "?";
        tmpText.fontSize = fontSize; // Use the smaller font size
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enabled = false;
        RectTransform textRect = tmpText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        effectIconPrefab = prefab;
        effectIconPrefab.SetActive(false);
    }
}