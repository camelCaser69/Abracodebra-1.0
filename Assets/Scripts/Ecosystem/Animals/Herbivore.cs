using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    public class Herbivore : Animal
    {
        [Header("Herbivore Settings")]
        public float feedingRate = 10f;             // How quickly it consumes plants
        public float plantNutritionValue = 15f;     // How much hunger is reduced by eating
        public bool preferYoungLeaves = true;       // Whether to target young growth
        public bool canEatFruit = true;             // Whether it can consume fruit
        public float fleeDistance = 5f;             // How far it runs when threatened
        
        [Header("Targeting Preferences")]
        public LayerMask plantLayerMask;
        public string[] preferredPlantTags = { "Leaf", "Plant" };
        public string[] predatorTags = { "Predator", "Carnivore" };
        
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
            nearestPredator = FindNearestOfTags(predatorTags);
            if (nearestPredator != null && 
                Vector3.Distance(transform.position, nearestPredator.transform.position) < senseRadius)
            {
                if (currentState != BehaviorState.Fleeing)
                {
                    ChangeState(BehaviorState.Fleeing);
                    AddMemory($"Fled from predator {nearestPredator.name}");
                    return;
                }
            }
            
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
                    if (nearestPlant != null && 
                        Vector3.Distance(transform.position, nearestPlant.transform.position) < 1f)
                    {
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
                    if (!IsHungry() || nearestPlant == null)
                    {
                        ChangeState(BehaviorState.Idle);
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
            nearestPlant = FindNearestOfTags(preferredPlantTags);
            
            if (nearestPlant != null)
            {
                // Move toward the plant
                MoveToward(nearestPlant.transform.position, moveSpeed);
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
            if (nearestPlant != null)
            {
                // Damage the plant and feed
                PlantPart plantPart = nearestPlant.GetComponent<PlantPart>();
                if (plantPart != null)
                {
                    // Damage the plant part
                    plantPart.TakeDamage(feedingRate * Time.deltaTime);
                    
                    // Reduce hunger based on feeding rate
                    float nutritionGained = feedingRate * Time.deltaTime;
                    Eat(nutritionGained);
                    
                    // Special case for fruit which might have different effects
                    if (plantPart.partType == PlantPartType.Fruit)
                    {
                        // Fruits might provide additional benefits
                        AddMemory("Ate a delicious fruit");
                    }
                    else
                    {
                        AddMemory($"Ate plant part: {plantPart.partType}");
                    }
                }
            }
            else
            {
                // Plant disappeared somehow, go back to seeking
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
        
        // Utility to find nearest object from multiple tags
        private GameObject FindNearestOfTags(string[] tags)
        {
            GameObject nearest = null;
            float nearestDistance = senseRadius;
            
            foreach (string tag in tags)
            {
                GameObject obj = FindNearestOfTag(tag, senseRadius);
                if (obj != null)
                {
                    float distance = Vector3.Distance(transform.position, obj.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearest = obj;
                        nearestDistance = distance;
                    }
                }
            }
            
            return nearest;
        }
        
        // Override the archetype determination for herbivore-specific archetypes
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