using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    public enum AnimalType { Herbivore, Carnivore, Omnivore, Insect }
    
    public enum BehaviorState { Idle, Seeking, Feeding, Fleeing, Hunting, Resting }
    
    [RequireComponent(typeof(SortableEntity))]
    public abstract class Animal : MonoBehaviour
    {
        [Header("Animal Identity")]
        public string animalName;
        public AnimalType animalType;
        
        [Header("Base Stats")]
        public float moveSpeed = 3f;
        public float maxHealth = 100f;
        public float currentHealth;
        public float senseRadius = 10f;
        public float hungerRate = 5f;           // How quickly animal gets hungry (per minute)
        public float maxHunger = 100f;
        public float currentHunger = 0f;        // 0 = full, 100 = starving
        
        [Header("Personality Traits (0-1 scale)")]
        [Range(0f, 1f)] public float aggression = 0.5f;
        [Range(0f, 1f)] public float curiosity = 0.5f;
        [Range(0f, 1f)] public float sociability = 0.5f;
        [Range(0f, 1f)] public float intelligence = 0.5f;
        [Range(0f, 1f)] public float persistence = 0.5f;
        
        [Header("Derived Traits (Auto-Calculated)")]
        [SerializeField] public float fearfulness;
        [SerializeField] public float territoriality;
        [SerializeField] public float adaptability;

        [Header("Experience")]
        public int age = 0;                     // In days
        public List<string> memories = new List<string>();
        public string archetype = "Unspecified";
        
        // Current state and targets
        protected BehaviorState currentState = BehaviorState.Idle;
        protected GameObject currentTarget;
        protected Vector3 moveTarget;
        protected float stateTime = 0f;         // Time in current state
        
        // Internal references
        protected Rigidbody2D rb;
        protected SortableEntity sortable;
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        
        // Properties
        public BehaviorState CurrentState => currentState;
        public float Fearfulness => fearfulness;
        public float Territoriality => territoriality;
        public float Adaptability => adaptability;
        
        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sortable = GetComponent<SortableEntity>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Initialize health
            currentHealth = maxHealth;
            
            // Calculate derived traits
            UpdateDerivedTraits();
        }
        
        protected virtual void Start()
        {
            // If no name specified, generate one
            if (string.IsNullOrEmpty(animalName))
            {
                animalName = GenerateRandomName();
            }
        }
        
        protected virtual void Update()
        {
            // Update hunger
            currentHunger += hungerRate * Time.deltaTime / 60f;
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
            
            // Update state timer
            stateTime += Time.deltaTime;
            
            // Process current behavior state
            ProcessCurrentState();
            
            // Check if state should change
            CheckStateTransitions();
        }
        
        // Called in Update to handle current state behavior
        protected abstract void ProcessCurrentState();
        
        // Called in Update to check if state should transition
        protected abstract void CheckStateTransitions();
        
        // Change the current behavior state
        public virtual void ChangeState(BehaviorState newState)
        {
            currentState = newState;
            stateTime = 0f;
            
            // Trigger animation if available
            if (animator != null)
            {
                animator.SetInteger("State", (int)newState);
            }
            
            Debug.Log($"{animalName} ({animalType}) changed state to {newState}");
        }
        
        // Take damage and potentially die
        public virtual void TakeDamage(float amount)
        {
            currentHealth -= amount;
            
            if (currentHealth <= 0)
            {
                Die();
            }
            else if (currentState != BehaviorState.Fleeing)
            {
                // Chance to flee based on fearfulness
                if (Random.value < fearfulness)
                {
                    ChangeState(BehaviorState.Fleeing);
                }
            }
        }
        
        // Consume food to reduce hunger - calling directly with nutrition amount
        public virtual void Eat(float nutritionAmount)
        {
            currentHunger -= nutritionAmount;
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
            
            // Add a memory of this eating experience
            AddMemory($"Ate food with nutrition value {nutritionAmount}");
        }
        
        // Consume food to reduce hunger - from a food object
        public virtual void Eat(GameObject foodObject)
        {
            float nutritionValue = 10f; // Default fallback value
            string foodName = foodObject.name;
            
            // Try to get nutrition from food
            PlantPart plantPart = foodObject.GetComponent<PlantPart>();
            if (plantPart != null)
            {
                nutritionValue = plantPart.nutritionalValue * plantPart.calories;
                foodName = $"{plantPart.partType}";
            }
            
            // If food is another animal
            Animal preyAnimal = foodObject.GetComponent<Animal>();
            if (preyAnimal != null)
            {
                // Base nutrition on prey's size/health
                nutritionValue = preyAnimal.maxHealth * 0.3f;
                foodName = $"{preyAnimal.animalName} ({preyAnimal.animalType})";
            }
            
            // Reduce hunger
            currentHunger -= nutritionValue;
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
            
            // Add memory
            AddMemory($"Ate {foodName} (nutrition: {nutritionValue:F1})");
        }
        
        // Die and potentially drop resources
        protected virtual void Die()
        {
            Debug.Log($"{animalName} ({animalType}) has died.");
            // Override in derived classes for type-specific death behavior
            Destroy(gameObject);
        }
        
        // Add new experiences to memory
        public void AddMemory(string memoryText)
        {
            memories.Add($"Day {age}: {memoryText}");
            
            // Keep memory list from growing too large
            if (memories.Count > 20)
            {
                memories.RemoveAt(0);
            }
            
            // Personality can evolve based on experiences
            // This would be expanded in a more sophisticated implementation
            UpdatePersonalityFromMemory(memoryText);
        }
        
        // Update personality traits based on experiences
        protected virtual void UpdatePersonalityFromMemory(string memoryText)
        {
            // Simple demonstration - would be more sophisticated in full implementation
            if (memoryText.Contains("attacked") || memoryText.Contains("threat"))
            {
                aggression += 0.01f;
                fearfulness += 0.02f;
            }
            else if (memoryText.Contains("discovered") || memoryText.Contains("found new"))
            {
                curiosity += 0.01f;
                adaptability += 0.01f;
            }
            
            // Clamp all values
            aggression = Mathf.Clamp01(aggression);
            curiosity = Mathf.Clamp01(curiosity);
            sociability = Mathf.Clamp01(sociability);
            intelligence = Mathf.Clamp01(intelligence);
            persistence = Mathf.Clamp01(persistence);
            
            // Update derived traits after personality changes
            UpdateDerivedTraits();
        }
        
        // Calculate derived personality traits
        protected void UpdateDerivedTraits()
        {
            // Fearfulness is inversely related to aggression and positively related to intelligence
            fearfulness = Mathf.Clamp01((1f - aggression) * 0.7f + intelligence * 0.3f);
            
            // Territoriality is related to aggression and inversely to sociability
            territoriality = Mathf.Clamp01(aggression * 0.6f + (1f - sociability) * 0.4f);
            
            // Adaptability combines curiosity, intelligence and moderate persistence
            float normalizedPersistence = Mathf.Abs(persistence - 0.5f) * 2f; // 0.5 = optimal, too high or too low reduces adaptability
            adaptability = Mathf.Clamp01(curiosity * 0.4f + intelligence * 0.4f + (1f - normalizedPersistence) * 0.2f);
        }
        
        // Check if this animal is hungry enough to seek food
        protected bool IsHungry()
        {
            return currentHunger > maxHunger * 0.5f;
        }
        
        // Check if this animal is very hungry and desperate for food
        protected bool IsStarving()
        {
            return currentHunger > maxHunger * 0.8f;
        }
        
        // Generate a random animal name
        protected string GenerateRandomName()
        {
            string[] prefixes = { "Fluffy", "Speedy", "Sneaky", "Bouncy", "Crafty", "Swift", "Clever", "Mighty", "Brave", "Curious" };
            string[] suffixes = { "Paws", "Tail", "Whiskers", "Ears", "Snout", "Hunter", "Jumper", "Runner", "Digger", "Scout" };
            
            return $"{prefixes[Random.Range(0, prefixes.Length)]} {suffixes[Random.Range(0, suffixes.Length)]}";
        }
        
        // Utility method to determine the archetype based on personality traits
        public virtual void DetermineArchetype()
        {
            // Example logic - would be expanded in full implementation
            if (aggression > 0.7f && persistence > 0.6f)
            {
                archetype = "The Devastator";
            }
            else if (curiosity > 0.7f && sociability < 0.4f)
            {
                archetype = "The Nibbler";
            }
            else if (sociability > 0.7f && intelligence > 0.6f)
            {
                archetype = "The Pack Hunter";
            }
            else if (persistence > 0.8f && territoriality > 0.7f)
            {
                archetype = "The Guardian";
            }
            else if (adaptability > 0.8f && intelligence > 0.7f)
            {
                archetype = "The Opportunist";
            }
            else
            {
                archetype = "Unspecified";
            }
        }
        
        // Check if this animal can be a prey for the given predator type
        public virtual bool IsValidPreyFor(AnimalType predatorType)
        {
            switch (predatorType)
            {
                case AnimalType.Carnivore:
                    return this.animalType == AnimalType.Herbivore || 
                          (this.animalType == AnimalType.Omnivore && currentHealth < maxHealth * 0.5f) ||
                          (this.animalType == AnimalType.Insect); // Carnivores can eat insects too
                
                case AnimalType.Omnivore:
                    return this.animalType == AnimalType.Herbivore && this.maxHealth < 80 || // Small herbivores only
                           this.animalType == AnimalType.Insect; // Omnivores eat insects
                    
                default:
                    return false;
            }
        }
        
        // Find nearest food source based on animal type
        protected GameObject FindNearestFood()
        {
            switch (animalType)
            {
                case AnimalType.Herbivore:
                    return FindNearestPlantFood();
                
                case AnimalType.Carnivore:
                    return FindNearestPrey();
                
                case AnimalType.Omnivore:
                    // Try plant food first, then prey if starving
                    GameObject plantFood = FindNearestPlantFood();
                    
                    if (plantFood != null)
                        return plantFood;
                        
                    if (IsStarving())
                        return FindNearestPrey();
                        
                    return null;
                
                case AnimalType.Insect:
                    return FindNearestPlantFood();
                
                default:
                    return null;
            }
        }
        
        // Find nearest plant food for herbivores and omnivores
        protected virtual GameObject FindNearestPlantFood()
        {
            // Search for plant parts in order of preference
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, senseRadius);
            
            float closestDistance = senseRadius;
            GameObject nearest = null;
            
            foreach (Collider2D collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;
                
                // Check if it's a plant part
                PlantPart plantPart = collider.GetComponent<PlantPart>();
                if (plantPart != null)
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        // Check if this animal can eat this part type
                        if (CanEatPlantPart(plantPart.partType))
                        {
                            nearest = collider.gameObject;
                            closestDistance = distance;
                        }
                    }
                }
            }
            
            return nearest;
        }
        
        // Find nearest prey for carnivores and hungry omnivores
        protected virtual GameObject FindNearestPrey()
        {
            // Search for valid prey animals
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, senseRadius);
            
            float closestDistance = senseRadius;
            GameObject nearest = null;
            
            foreach (Collider2D collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;
                
                // Check if it's an animal
                Animal potentialPrey = collider.GetComponent<Animal>();
                if (potentialPrey != null && potentialPrey.IsValidPreyFor(this.animalType))
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        nearest = collider.gameObject;
                        closestDistance = distance;
                    }
                }
            }
            
            return nearest;
        }
        
        // Check if this animal can eat a specific plant part
        protected virtual bool CanEatPlantPart(PlantPartType partType)
        {
            // Base implementation - override in derived classes for specific preferences
            return true; // By default, can eat any plant part
        }
    }
}