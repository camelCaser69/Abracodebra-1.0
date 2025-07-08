// Assets/Scripts/Ecosystem/Animals/AnimalNeeds.cs
using UnityEngine;
using WegoSystem;

public class AnimalNeeds : MonoBehaviour
{
    AnimalController controller;
    AnimalDefinition definition;
    AnimalDiet diet;
    SpriteRenderer spriteRenderer;

    float currentHealth;
    float currentHunger;

    int hungerTick = 0;
    int starvationTick = 0;

    float flashRemainingTime = 0f;
    float flashDurationSeconds = 0.2f;
    bool isFlashing = false;
    Color originalColor;

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

    void UpdateHunger()
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

    void UpdateStarvation()
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
        float finalDamage = amount;
        if (controller.StatusEffects != null)
        {
            finalDamage *= controller.StatusEffects.DamageResistanceMultiplier;
        }
        
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, definition.maxHealth);

        StartDamageFlash();
        controller.UpdateUI();

        if (currentHealth <= definition.maxHealth * 0.3f && controller.CanShowThought())
        {
            controller.ShowThought(ThoughtTrigger.HealthLow);
        }
    }

    void ApplyStarvationDamage()
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

        float satiationGain = diet.GetSatiationValue(foodItem.foodType);
        currentHunger -= satiationGain;
        currentHunger = Mathf.Max(0f, currentHunger);

        controller.UpdateUI();

        Debug.Log($"[AnimalNeeds] {controller.SpeciesName} ate {foodItem.foodType.foodName}. Hunger: {currentHunger}/{diet.maxHunger}");
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

    void StartDamageFlash()
    {
        if (spriteRenderer == null) return;

        isFlashing = true;
        flashRemainingTime = flashDurationSeconds;
    }
}