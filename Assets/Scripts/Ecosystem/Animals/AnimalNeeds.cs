using UnityEngine;
using WegoSystem;
using Abracodabra.Genes.Components;

public class AnimalNeeds : MonoBehaviour
{
    private AnimalController controller;
    private AnimalDefinition definition;
    private AnimalDiet diet;
    private SpriteRenderer spriteRenderer;

    private float currentHealth;
    private float currentHunger;

    private int hungerTick = 0;
    private int starvationTick = 0;

    private float flashRemainingTime = 0f;
    private float flashDurationSeconds = 0.2f;
    private bool isFlashing = false;
    private Color originalColor;

    public float CurrentHealth => currentHealth;
    public float CurrentHunger => currentHunger;
    public bool IsHungry => currentHunger >= diet.hungerThreshold;
    public bool IsStarving => currentHunger >= diet.maxHunger;

    public void Initialize(AnimalController controller, AnimalDefinition definition)
    {
        this.controller = controller;
        this.definition = definition;
        this.diet = definition.diet;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (TickManager.Instance?.Config != null)
        {
            flashDurationSeconds = definition.damageFlashTicks / TickManager.Instance.Config.ticksPerRealSecond;
        }

        currentHealth = definition.maxHealth;
        currentHunger = 0f;
    }

    public void OnTickUpdate(int currentTick)
    {
        UpdateHunger();
        UpdateStarvation();
    }

    private void UpdateHunger()
    {
        if (TickManager.Instance?.Config == null || diet == null) return;

        hungerTick++;
        if (hungerTick >= TickManager.Instance.Config.animalHungerTickInterval)
        {
            hungerTick = 0;

            currentHunger += diet.hungerIncreaseRate;
            currentHunger = Mathf.Min(currentHunger, diet.maxHunger);

            controller.UpdateUI();
        }
    }

    private void UpdateStarvation()
    {
        if (!IsStarving)
        {
            starvationTick = 0;
            return;
        }

        starvationTick++;
        if (starvationTick >= definition.starvationDamageTickInterval)
        {
            starvationTick = 0;
            ApplyStarvationDamage();
        }
    }

    void Update()
    {
        if (!isFlashing || spriteRenderer == null) return;

        flashRemainingTime -= Time.deltaTime;

        if (flashRemainingTime <= 0)
        {
            spriteRenderer.color = originalColor;
            isFlashing = false;
        }
        else
        {
            float t = (flashRemainingTime / flashDurationSeconds);
            spriteRenderer.color = Color.Lerp(originalColor, definition.damageFlashColor, t);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, definition.maxHealth);

        StartDamageFlash();
        controller.UpdateUI();

        if (currentHealth <= definition.maxHealth * 0.3f && controller.CanShowThought())
        {
            controller.ShowThought(ThoughtTrigger.HealthLow);
        }
    }

    private void ApplyStarvationDamage()
    {
        currentHealth -= definition.damagePerStarvationTick;
        currentHealth = Mathf.Clamp(currentHealth, 0f, definition.maxHealth);

        StartDamageFlash();
        controller.UpdateUI();

        Debug.Log($"[AnimalNeeds] {controller.SpeciesName} taking starvation damage. Health: {currentHealth}");
    }

    public void Eat(FoodItem foodItem)
    {
        if (foodItem == null || foodItem.foodType == null || diet == null) return;

        float satiationGain = 0f;

        // NEW: Prioritize NutritionComponent for gene-based foods
        NutritionComponent nutrition = foodItem.GetComponent<NutritionComponent>();
        if (nutrition != null)
        {
            satiationGain = nutrition.nutritionValue;
        }
        else
        {
            // Fallback for standard plant parts
            satiationGain = diet.GetSatiationValue(foodItem.foodType);
        }
        
        currentHunger -= satiationGain;
        currentHunger = Mathf.Max(0f, currentHunger);

        controller.UpdateUI();

        Debug.Log($"[AnimalNeeds] {controller.SpeciesName} ate {foodItem.foodType.foodName} for {satiationGain} satiation. Hunger: {currentHunger}/{diet.maxHunger}");
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, definition.maxHealth);
        controller.UpdateUI();
    }

    public void ModifyHunger(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0f, diet.maxHunger);
        controller.UpdateUI();
    }

    private void StartDamageFlash()
    {
        if (spriteRenderer == null) return;

        isFlashing = true;
        flashRemainingTime = flashDurationSeconds;
    }
}