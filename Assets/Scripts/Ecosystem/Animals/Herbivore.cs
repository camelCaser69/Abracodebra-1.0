using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    public class Herbivore : Animal
    {
        [Header("Herbivore Settings")]
        public float fleeDistance = 5f;             // How far it runs when threatened
        public bool preferYoungLeaves = true;       // Whether to target young growth
        public bool canEatFruit = true;             // Whether it can consume fruit
        
        [Header("Feeding Preferences")]
        [Range(0f, 1f)] public float leafPreference = 0.8f;
        [Range(0f, 1f)] public float fruitPreference = 0.6f;
        [Range(0f, 1f)] public float flowerPreference = 0.4f;
        [Range(0f, 1f)] public float stemPreference = 0.3f;
        
        // Private state variables
        private GameObject nearestPredator;
        private GameObject nearestPlant;
        private float moveTimer = 0f;
        private Vector3 randomMoveTarget;
        
        protected override void Awake()
        {
            base.Awake();
            animalType = AnimalType.Herbivore;
        }
        
        protected override void Start()
        {
            base.Start();
            // Start with a random idle state
            ChangeState(BehaviorState.Idle);
        }
        
        protected override void ProcessCurrentState()
        {
            switch (currentState)
            {
                case BehaviorState.Idle:
                    ProcessIdleState();
                    break;
                    
                case BehaviorState.Seeking:
                    ProcessSeekingState();
                    break;
                    
                case BehaviorState.Feeding:
                    ProcessFeedingState();
                    break;
                    
                case BehaviorState.Fleeing:
                    ProcessFleeingState();
                    break;
                    
                case BehaviorState.Resting:
                    ProcessRestingState();
                    break;
            }
        }
        
        protected override void CheckStateTransitions()
        {
            // Check for predators first - highest priority
            CheckForPredators();
            
            // Check current state and potential transitions
            switch (currentState)
            {
                case BehaviorState.Idle:
                    // If hungry, transition to seeking
                    if (IsHungry())
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    
                    // Random exploration based on curiosity
                    if (Random.value < curiosity * 0.01f)
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    
                    // If been idle too long, maybe move randomly
                    if (stateTime > 5f && Random.value < 0.3f)
                    {
                        randomMoveTarget = transform.position + new Vector3(
                            Random.Range(-5f, 5f),
                            Random.Range(-5f, 5f),
                            0f
                        );
                        moveTimer = 0f;
                        
                        // Small chance to rest instead of moving randomly
                        if (Random.value < 0.2f)
                        {
                            ChangeState(BehaviorState.Resting);
                        }
                    }
                    break;
                    
                case BehaviorState.Seeking:
                    // Found food
                    nearestPlant = FindNearestPlantFood();
                    if (nearestPlant != null && 
                        Vector3.Distance(transform.position, nearestPlant.transform.position) < 1f)
                    {
                        currentTarget = nearestPlant;
                        ChangeState(BehaviorState.Feeding);
                        return;
                    }
                    
                    // Give up seeking after a while if not finding anything
                    float seekTimeout = 15f * persistence; // Persistence affects how long they'll search
                    if (stateTime > seekTimeout && nearestPlant == null)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                
                case BehaviorState.Feeding:
                    // If no longer hungry or no plant to eat
                    if (!IsHungry() || currentTarget == null)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    
                    // If moved too far from food
                    if (currentTarget != null && 
                        Vector3.Distance(transform.position, currentTarget.transform.position) > 1.5f)
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    break;
                    
                case BehaviorState.Fleeing:
                    // Done fleeing when predator is far enough or time passed
                    if (nearestPredator == null || 
                        Vector3.Distance(transform.position, nearestPredator.transform.position) > fleeDistance ||
                        stateTime > 5f)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                    
                case BehaviorState.Resting:
                    // Done resting after a while
                    if (stateTime > 8f || IsStarving())
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
            }
        }
        
        private void CheckForPredators()
        {
            // Find predators using colliders rather than tags
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, senseRadius);
            
            float closestDistance = senseRadius;
            nearestPredator = null;
            
            foreach (Collider2D collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;
                    
                Animal potentialPredator = collider.GetComponent<Animal>();
                if (potentialPredator != null && 
                    (potentialPredator.animalType == AnimalType.Carnivore || 
                     potentialPredator.animalType == AnimalType.Omnivore))
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        nearestPredator = collider.gameObject;
                        closestDistance = distance;
                    }
                }
            }
            
            // If predator detected and currently not fleeing, start fleeing
            if (nearestPredator != null && closestDistance < senseRadius * 0.7f && 
                currentState != BehaviorState.Fleeing)
            {
                // Chance to flee based on fearfulness
                if (Random.value < Fearfulness)
                {
                    ChangeState(BehaviorState.Fleeing);
                    AddMemory($"Fled from predator {nearestPredator.name}");
                }
            }
        }
        
        private void ProcessIdleState()
        {
            // In idle state, occasionally look around or make small movements
            moveTimer += Time.deltaTime;
            
            if (moveTimer > 3f)
            {
                // Move to a random nearby position
                if (Vector3.Distance(transform.position, randomMoveTarget) > 0.5f)
                {
                    MoveToward(randomMoveTarget, moveSpeed * 0.5f);
                }
                else
                {
                    moveTimer = 0f;
                    randomMoveTarget = transform.position + new Vector3(
                        Random.Range(-3f, 3f),
                        Random.Range(-3f, 3f),
                        0f
                    );
                }
            }
        }
        
        private void ProcessSeekingState()
        {
            // Look for nearest food source
            nearestPlant = FindNearestPlantFood();
            
            if (nearestPlant != null)
            {
                // Move toward the plant
                MoveToward(nearestPlant.transform.position, moveSpeed);
                
                // Set as current target when close enough
                if (Vector3.Distance(transform.position, nearestPlant.transform.position) < 1.2f)
                {
                    currentTarget = nearestPlant;
                }
            }
            else
            {
                // No plant visible, move randomly to explore
                moveTimer += Time.deltaTime;
                
                if (moveTimer > 1f || Vector3.Distance(transform.position, randomMoveTarget) < 0.5f)
                {
                    moveTimer = 0f;
                    randomMoveTarget = transform.position + new Vector3(
                        Random.Range(-5f, 5f), 
                        Random.Range(-5f, 5f),
                        0f
                    );
                }
                
                MoveToward(randomMoveTarget, moveSpeed * 0.7f);
            }
        }
        
        private void ProcessFeedingState()
        {
            if (currentTarget != null)
            {
                // Get plant part
                PlantPart plantPart = currentTarget.GetComponent<PlantPart>();
                if (plantPart != null)
                {
                    // Start/continue eating process
                    plantPart.StartEating(this);
                    
                    // Add memory about what's being eaten
                    if (stateTime < 0.5f) // Only once at the start
                    {
                        AddMemory($"Started eating a {plantPart.partType}");
                    }
                }
                else
                {
                    // Not a valid plant part anymore
                    ChangeState(BehaviorState.Seeking);
                }
            }
            else
            {
                // Target disappeared
                ChangeState(BehaviorState.Seeking);
            }
        }
        
        private void ProcessFleeingState()
        {
            if (nearestPredator != null)
            {
                // Calculate direction away from predator
                Vector3 awayDirection = transform.position - nearestPredator.transform.position;
                awayDirection.Normalize();
                
                // Move in that direction but faster than normal
                Vector3 targetPosition = transform.position + awayDirection * 5f;
                MoveToward(targetPosition, moveSpeed * 1.5f);
            }
            else
            {
                // No predator, stop fleeing
                ChangeState(BehaviorState.Idle);
            }
        }
        
        private void ProcessRestingState()
        {
            // When resting, stay still and recover
            // Could implement resource regeneration here
        }
        
        // Helper method to move toward a target position
        private void MoveToward(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            
            // Flip sprite based on movement direction if needed
            if (spriteRenderer != null)
            {
                if (direction.x < 0)
                    spriteRenderer.flipX = true;
                else if (direction.x > 0)
                    spriteRenderer.flipX = false;
            }
            
            // If we have an animator, set movement parameters
            if (animator != null)
            {
                animator.SetFloat("Speed", direction.magnitude * speed);
                animator.SetFloat("Horizontal", direction.x);
                animator.SetFloat("Vertical", direction.y);
            }
        }
        
        // Override to specify plant part preferences
        protected override bool CanEatPlantPart(PlantPartType partType)
        {
            switch (partType)
            {
                case PlantPartType.Leaf:
                    return Random.value < leafPreference;
                    
                case PlantPartType.Fruit:
                    return canEatFruit && Random.value < fruitPreference;
                    
                case PlantPartType.Flower:
                    return Random.value < flowerPreference;
                    
                case PlantPartType.Stem:
                    return Random.value < stemPreference;
                    
                case PlantPartType.Seed:
                    return Random.value < 0.2f; // Rarely eat seeds directly
                    
                case PlantPartType.Root:
                    return IsStarving() && Random.value < 0.1f; // Very rarely eat roots
                    
                default:
                    return false;
            }
        }
        
        // Override for herbivore-specific archetypes
        public override void DetermineArchetype()
        {
            if (curiosity > 0.7f && fearfulness > 0.6f)
            {
                archetype = "The Nibbler";
            }
            else if (aggression > 0.7f && persistence > 0.7f)
            {
                archetype = "The Devastator";
            }
            else if (intelligence > 0.7f && adaptability > 0.6f)
            {
                archetype = "The Seasonal Visitor";
            }
            else if (fearfulness > 0.8f && adaptability > 0.7f)
            {
                archetype = "The Opportunist";
            }
            else if (intelligence > 0.6f && persistence > 0.6f)
            {
                archetype = "The Hoarder";
            }
            else
            {
                archetype = "Common Herbivore";
            }
        }
    }
}
