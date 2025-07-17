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

        // Find the parent 'Icon' GameObject first
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
        {
            // <<< THIS IS THE CRITICAL FIX
            // Ensure the parent 'Icon' GameObject itself is active.
            iconTransform.gameObject.SetActive(true);

            // Now, find the components within it
            iconImage = iconTransform.GetComponent<Image>();
            iconText = iconTransform.GetComponentInChildren<TextMeshProUGUI>();

            // The rest of the logic can now correctly enable/disable the components
            if (currentEffect.icon != null)
            {
                // Use sprite if available
                if (iconImage != null)
                {
                    iconImage.sprite = currentEffect.icon;
                    iconImage.color = currentEffect.effectColor;
                    iconImage.enabled = true;
                }
                if (iconText != null)
                {
                    iconText.enabled = false;
                }
            }
            else
            {
                // Otherwise, use unicode symbol
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }
                if (iconText != null)
                {
                    iconText.text = currentEffect.unicodeSymbol;
                    iconText.color = currentEffect.effectColor;
                    iconText.enabled = true;
                }
            }
        }
        else
        {
            Debug.LogError("Could not find child GameObject named 'Icon' in the StatusEffectIcon prefab!", this);
        }
    }
}