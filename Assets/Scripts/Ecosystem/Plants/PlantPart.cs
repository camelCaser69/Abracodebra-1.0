using UnityEngine;
using UnityEngine.Events;

namespace Ecosystem
{
    public enum PlantPartType
    {
        Seed,
        Root,
        Stem,
        Leaf,
        Flower,
        Fruit
    }
    
    [RequireComponent(typeof(SortableEntity))]
    public class PlantPart : MonoBehaviour
    {
        [Header("Plant Part Settings")]
        public PlantPartType partType;
        public float maxHealth = 100f;
        public float currentHealth;
        
        [Header("Plant Properties")]
        [Range(0f, 1f)] public float toxicity = 0f;     // Reduces damage and can harm herbivores
        [Range(0f, 1f)] public float toughness = 0f;    // Physical resistance to damage
        [Range(0f, 1f)] public float nutrition = 0.5f;  // How nutritious this part is (more attracts herbivores)
        [Range(0f, 1f)] public float visibility = 0.5f; // How easily spotted by herbivores
        
        [Header("Visual Feedback")]
        public SpriteRenderer spriteRenderer;
        public Gradient healthGradient;                 // Color varies with health
        public float damageFeedbackDuration = 0.3f;     // How long damage flash lasts
        
        [Header("Events")]
        public UnityEvent<float> OnDamaged;             // Triggered when damaged
        public UnityEvent OnDestroyed;                  // Triggered when destroyed
        
        // References
        private PlantGrowth plantGrowth;
        private SortableEntity sortableEntity;
        
        // Internal state
        private float damageDisplayTimer = 0f;
        private Color originalColor;
        private bool isDying = false;
        
        private void Awake()
        {
            // Find references
            sortableEntity = GetComponent<SortableEntity>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Find parent plant
            plantGrowth = GetComponentInParent<PlantGrowth>();
            
            // Initialize health
            currentHealth = maxHealth;
            
            // Store original color
            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;
        }
        
        private void Start()
        {
            // You could add initialization based on plant gene expressions here
            if (plantGrowth != null)
            {
                // Example of how genes might influence this part's properties
                // This would be expanded in the gene expression system
                switch (partType)
                {
                    case PlantPartType.Leaf:
                        // Leaf properties might be affected by plant growth genes
                        break;
                    case PlantPartType.Stem:
                        // Stem properties might have different influences
                        toughness += 0.2f; // Stems are naturally tougher
                        break;
                    // Other part types...
                }
            }
            
            // Set the proper tag for this plant part for animal targeting
            gameObject.tag = partType.ToString();
        }
        
        private void Update()
        {
            // Visual feedback when damaged
            if (damageDisplayTimer > 0)
            {
                damageDisplayTimer -= Time.deltaTime;
                
                if (spriteRenderer != null)
                {
                    if (damageDisplayTimer > 0)
                    {
                        // Flash white when damaged
                        spriteRenderer.color = Color.Lerp(Color.white, originalColor, 
                            1f - (damageDisplayTimer / damageFeedbackDuration));
                    }
                    else
                    {
                        // Reset to health-based color
                        UpdateVisualHealth();
                    }
                }
            }
        }
        
        public void TakeDamage(float damage)
        {
            // Reduce damage based on toughness and toxicity
            float reducedDamage = damage * (1f - toughness * 0.5f);
            
            currentHealth -= reducedDamage;
            
            // Visual feedback
            damageDisplayTimer = damageFeedbackDuration;
            
            // Update appearance based on health
            UpdateVisualHealth();
            
            // Trigger damage event
            OnDamaged?.Invoke(reducedDamage);
            
            // Check if destroyed
            if (currentHealth <= 0 && !isDying)
            {
                isDying = true;
                OnDestroyed?.Invoke();
                DestroyPart();
            }
        }
        
        private void UpdateVisualHealth()
        {
            if (spriteRenderer != null && healthGradient != null)
            {
                float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
                spriteRenderer.color = healthGradient.Evaluate(healthPercent);
            }
        }
        
        private void DestroyPart()
        {
            // Different behavior based on part type
            switch (partType)
            {
                case PlantPartType.Leaf:
                    // Losing a leaf reduces energy production
                    if (plantGrowth != null)
                    {
                        // Reduce photosynthesis efficiency
                    }
                    break;
                
                case PlantPartType.Stem:
                    // Stem damage might cause parts above to die as well
                    if (plantGrowth != null)
                    {
                        // Find and damage dependent parts
                    }
                    break;
                
                case PlantPartType.Fruit:
                    // Fruit might drop seeds when destroyed
                    // Implement seed spawning logic
                    break;
            }
            
            // Destroy the game object
            Destroy(gameObject);
        }
        
        // Method for animals to check toxicity before eating
        public float GetToxicity()
        {
            return toxicity;
        }
        
        // Method for animals to check nutritional value
        public float GetNutritionalValue()
        {
            return nutrition * maxHealth * 0.01f; // Scale with size/health
        }
    }
}