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
        
        [Header("Plant Properties")]
        [Range(0f, 1f)] public float toxicity = 0f;     // Can harm herbivores if high
        [Range(0f, 1f)] public float toughness = 0f;    // Increases eating time
        [Range(0f, 1f)] public float visibility = 0.5f; // How easily spotted by herbivores
        
        [Header("Nutritional Properties")]
        [Range(0f, 1f)] public float nutritionalValue = 0.5f;  // Base nutritional content
        public float calories = 15f;  // How much hunger is reduced when fully eaten
        
        [Header("Eating Mechanics")]
        public float eatingTime = 3f;  // Base time it takes to fully eat this part
        private float currentEatingTime = 0f;
        private bool isBeingEaten = false;
        private Animal eatingAnimal = null;
        
        [Header("Visual Feedback")]
        public SpriteRenderer spriteRenderer;
        public Gradient eatingGradient;  // Color changes as plant is eaten
        public bool shrinkWhileEating = true;
        
        [Header("Events")]
        public UnityEvent<float> OnDamaged;             // Triggered when damaged
        public UnityEvent OnDestroyed;                  // Triggered when destroyed
        
        // References
        private SortableEntity sortableEntity;
        private Vector3 originalScale;
        private Color originalColor;
        
        private void Awake()
        {
            // Find references
            sortableEntity = GetComponent<SortableEntity>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Store original properties
            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;
            
            originalScale = transform.localScale;
            
            // Adjust eating time based on toughness
            eatingTime *= (1f + toughness * 2f); // Tougher parts take longer to eat
        }
        
        private void Update()
        {
            if (isBeingEaten)
            {
                // Check if animal is still nearby and in feeding state
                if (eatingAnimal == null || 
                    Vector3.Distance(transform.position, eatingAnimal.transform.position) > 1.5f ||
                    eatingAnimal.CurrentState != BehaviorState.Feeding)
                {
                    // Animal moved away, was destroyed, or stopped feeding
                    StopEating();
                    return;
                }
                
                // Progress eating time
                currentEatingTime += Time.deltaTime;
                
                // Visual feedback
                UpdateVisualFeedback();
                
                // Provide nutrition to animal as it eats
                float nutritionPerSecond = (nutritionalValue * calories) / eatingTime;
                eatingAnimal.Eat(nutritionPerSecond * Time.deltaTime);
                
                // Apply toxicity damage if applicable
                if (toxicity > 0.3f)
                {
                    float toxicityDamage = toxicity * 2f * Time.deltaTime;
                    eatingAnimal.TakeDamage(toxicityDamage);
                }
                
                // Check if fully eaten
                if (currentEatingTime >= eatingTime)
                {
                    // Part is fully eaten
                    OnDestroyed?.Invoke();
                    Destroy(gameObject);
                }
            }
        }
        
        // Start being eaten by an animal
        public void StartEating(Animal animal)
        {
            if (!isBeingEaten)
            {
                isBeingEaten = true;
                eatingAnimal = animal;
                currentEatingTime = 0f;
                
                // Trigger damage event
                OnDamaged?.Invoke(1f);
            }
        }
        
        // Stop being eaten
        public void StopEating()
        {
            if (isBeingEaten)
            {
                isBeingEaten = false;
                eatingAnimal = null;
                
                // Partially restore appearance
                if (shrinkWhileEating)
                {
                    float recoveryFactor = 0.5f; // How much to recover when eating stops
                    float eatingProgress = currentEatingTime / eatingTime;
                    float recovery = eatingProgress * recoveryFactor;
                    
                    transform.localScale = Vector3.Lerp(
                        Vector3.one * 0.3f,  // Smallest size
                        originalScale,       // Original size
                        recovery             // Recovery amount
                    );
                }
                
                if (spriteRenderer != null && eatingGradient != null)
                {
                    spriteRenderer.color = originalColor;
                }
            }
        }
        
        // Update visual appearance based on eating progress
        private void UpdateVisualFeedback()
        {
            float eatProgress = currentEatingTime / eatingTime;
            
            // Scale down
            if (shrinkWhileEating)
            {
                transform.localScale = Vector3.Lerp(
                    originalScale,         // Start size
                    originalScale * 0.3f,  // End size (30% of original)
                    eatProgress
                );
            }
            
            // Change color
            if (spriteRenderer != null && eatingGradient != null)
            {
                spriteRenderer.color = eatingGradient.Evaluate(eatProgress);
            }
        }
        
        // Legacy TakeDamage method for compatibility
        public void TakeDamage(float damage)
        {
            // Ignore damage amount and use eating system instead
            if (!isBeingEaten)
            {
                Animal nearbyAnimal = FindNearbyAnimal();
                if (nearbyAnimal != null)
                {
                    StartEating(nearbyAnimal);
                }
                else
                {
                    // No animal nearby, just trigger damage event
                    OnDamaged?.Invoke(damage);
                }
            }
        }
        
        // Find the nearest animal that could be eating this plant
        private Animal FindNearbyAnimal()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.5f);
            foreach (Collider2D collider in colliders)
            {
                Animal animal = collider.GetComponent<Animal>();
                if (animal != null && animal.CurrentState == BehaviorState.Feeding)
                {
                    return animal;
                }
            }
            return null;
        }
        
        // Get total nutritional value
        public float GetTotalNutrition()
        {
            return nutritionalValue * calories;
        }
        
        // Get modified eating time based on animal's attributes
        public float GetModifiedEatingTime(Animal animal)
        {
            // Base time, adjusted for toughness and modified by animal traits
            if (animal != null)
            {
                // Aggressive animals eat faster
                float aggressionModifier = 1f - (animal.aggression * 0.3f);
                // Persistent animals take their time more
                float persistenceModifier = 1f + (animal.persistence * 0.2f);
                
                return eatingTime * aggressionModifier * persistenceModifier;
            }
            
            return eatingTime;
        }
    }
}