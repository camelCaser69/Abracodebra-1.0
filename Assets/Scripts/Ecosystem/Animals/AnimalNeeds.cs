using UnityEngine;
using WegoSystem;

public class AnimalNeeds : MonoBehaviour
{
    // References
    private AnimalController controller;
    private AnimalDefinition definition;
    private AnimalDiet diet;
    private SpriteRenderer spriteRenderer;
    
    // Stats
    private float currentHealth;
    private float currentHunger;
    
    // Tick counters
    private int hungerTick = 0;
    private int starvationTick = 0;
    
    // Flash effect
    private float flashRemainingTime = 0f;
    private float flashDurationSeconds = 0.2f;
    private bool isFlashing = false;
    private Color originalColor;
    
    // Properties
    public float CurrentHealth => currentHealth;
    public float CurrentHunger => currentHunger;
    public bool IsHungry => currentHunger >= diet.hungerThreshold;
    public bool IsStarving => currentHunger >= diet.maxHunger;
    
    public void Initialize(AnimalController controller, AnimalDefinition definition)
    {
        this.controller = controller;
        this.definition = definition;
        this.diet = definition.diet;
        
        // Get sprite renderer
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Calculate flash duration
        if (TickManager.Instance?.Config != null)
        {
            flashDurationSeconds = definition.damageFlashTicks / TickManager.Instance.Config.ticksPerRealSecond;
        }
        
        // Initialize stats
        currentHealth = definition.maxHealth;
        currentHunger = 0f;
    }
    
    public void OnTickUpdate(int currentTick)
    {
        UpdateHunger();
        UpdateStarvation();
        UpdateFlashEffect();
    }
    
    private void UpdateHunger()
    {
        if (TickManager.Instance?.Config == null || diet == null) return;
        
        hungerTick++;
        if (hungerTick >= TickManager.Instance.Config.animalHungerTickInterval)
        {
            hungerTick = 0;
            
            // Increase hunger
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
    
    private void UpdateFlashEffect()
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
            // Pulse effect
            float t = Mathf.PingPong(flashRemainingTime * 10f, 1f);
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
        TakeDamage(definition.damagePerStarvationTick);
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
    
    private void StartDamageFlash()
    {
        if (spriteRenderer == null) return;
        
        isFlashing = true;
        flashRemainingTime = flashDurationSeconds;
    }
}