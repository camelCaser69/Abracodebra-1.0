using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HungerUI : MonoBehaviour
{
    [SerializeField] private PlayerHungerSystem playerHungerSystem;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private TextMeshProUGUI hungerText;

    void Start()
    {
        if (playerHungerSystem == null)
        {
            GardenerController player = FindAnyObjectByType<GardenerController>();
            if (player != null)
            {
                playerHungerSystem = player.GetComponent<PlayerHungerSystem>();
            }
        }

        if (playerHungerSystem != null)
        {
            playerHungerSystem.OnHungerChanged += UpdateUI;
            UpdateUI(playerHungerSystem.CurrentHunger, playerHungerSystem.MaxHunger);
        }
        else
        {
            Debug.LogError("[HungerUI] PlayerHungerSystem reference not found! UI will not update.", this);
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (playerHungerSystem != null)
        {
            playerHungerSystem.OnHungerChanged -= UpdateUI;
        }
    }

    private void UpdateUI(float currentHunger, float maxHunger)
    {
        if (hungerSlider != null)
        {
            hungerSlider.maxValue = maxHunger;
            hungerSlider.value = currentHunger;
        }

        if (hungerText != null)
        {
            hungerText.text = $"{Mathf.CeilToInt(currentHunger)} / {Mathf.CeilToInt(maxHunger)}";
        }
    }
}