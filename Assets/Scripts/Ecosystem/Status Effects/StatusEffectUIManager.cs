// Assets/Scripts/Ecosystem/Status Effects/StatusEffectUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using WegoSystem;

public class StatusEffectUIManager : MonoBehaviour
{
    [SerializeField] Transform effectIconContainer;
    [SerializeField] GameObject effectIconPrefab;

    StatusEffectManager statusManager;
    Dictionary<string, StatusEffectIconUI> activeIcons = new Dictionary<string, StatusEffectIconUI>();

    // Re-introducing the public Initialize method.
    public void Initialize(StatusEffectManager manager)
    {
        statusManager = manager;

        if (effectIconPrefab == null)
        {
            CreateDefaultIconPrefab();
        }
    }

    // Removing the Awake and Start methods that caused the race condition.
    
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

        // Remove icons for effects that are no longer active
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

        // Add icons for new effects
        foreach (var instance in currentEffectInstances)
        {
            if (!activeIcons.ContainsKey(instance.effect.effectID))
            {
                CreateEffectIcon(instance);
            }
        }

        // Ensure icon order matches effect order
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