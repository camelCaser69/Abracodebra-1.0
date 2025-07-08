// Assets/Scripts/Ecosystem/StatusEffects/StatusEffectIconUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusEffectIconUI : MonoBehaviour
{
    private Image iconImage;
    private TextMeshProUGUI iconText;
    private StatusEffect currentEffect;

    public void Initialize(StatusEffectInstance instance)
    {
        currentEffect = instance.effect;

        // Find UI components by name
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<Image>();
            iconText = iconTransform.GetComponentInChildren<TextMeshProUGUI>();

            // Use sprite if available, otherwise use unicode
            if (currentEffect.icon != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = currentEffect.icon;
                    iconImage.color = currentEffect.effectColor;
                    iconImage.enabled = true;
                }
                if (iconText != null) iconText.enabled = false;
            }
            else
            {
                if (iconImage != null) iconImage.enabled = false;
                if (iconText != null)
                {
                    iconText.text = currentEffect.unicodeSymbol;
                    iconText.color = currentEffect.effectColor;
                    iconText.enabled = true;
                }
            }
        }
    }
}