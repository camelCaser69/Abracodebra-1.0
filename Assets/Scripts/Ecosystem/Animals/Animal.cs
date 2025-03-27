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
        
        // Consume food to reduce hunger
        public virtual void Eat(float nutritionAmount)
        {
            currentHunger -= nutritionAmount;
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
            
            // Add a memory of this eating experience
            AddMemory($"Ate food with nutrition value {nutritionAmount}");
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
        
        // Simple AI to find the nearest object of a specific type
        protected GameObject FindNearestOfTag(string tag, float maxDistance = 100f)
        {
            GameObject nearest = null;
            float nearestDistance = maxDistance;
            
            // Get all objects with the specified tag
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            
            foreach (GameObject obj in objects)
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = obj;
                    nearestDistance = distance;
                }
            }
            
            return nearest;
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
    }
}