using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    public class Carnivore : Animal
    {
        [Header("Carnivore Settings")]
        public float attackDamage = 20f;            // Damage dealt per attack
        public float attackCooldown = 2f;           // Seconds between attacks
        public float preyNutritionValue = 30f;      // Nutrition from successful hunt
        public float stalkSpeed = 2f;               // Reduced speed when stalking prey
        public float chaseSpeed = 5f;               // Increased speed when actively chasing
        public float territoryRadius = 20f;         // Area this carnivore considers its territory
        
        [Header("Targeting Preferences")]
        public string[] preyTags = { "Herbivore", "Prey" };
        public string[] competitorTags = { "Carnivore" };
        
        // Private state variables
        private GameObject nearestPrey;
        private GameObject nearestCompetitor;
        private Vector3 territoryCenter;
        private float lastAttackTime = -10f;
        private float moveTimer = 0f;
        private Vector3 randomMoveTarget;
        private float huntSuccessRate = 0.5f;      // Tracks successful hunts for archetype determination
        
        protected override void Awake()
        {
            base.Awake();
            animalType = AnimalType.Carnivore;
        }
        
        protected override void Start()
        {
            base.Start();
            // Set initial territory center to spawn position
            territoryCenter = transform.position;
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
                
                case BehaviorState.Hunting:
                    ProcessHuntingState();
                    break;
                
                case BehaviorState.Feeding:
                    ProcessFeedingState();
                    break;
                
                case BehaviorState.Resting:
                    ProcessRestingState();
                    break;
            }
        }
        
        protected override void CheckStateTransitions()
        {
            // First check for territory intrusion by competitors (if territorial)
            if (Territoriality > 0.7f)
            {
                nearestCompetitor = FindNearestOfTags(competitorTags);
                if (nearestCompetitor != null && 
                    Vector3.Distance(transform.position, nearestCompetitor.transform.position) < territoryRadius &&
                    nearestCompetitor != gameObject) // Don't count self
                {
                    if (currentState != BehaviorState.Hunting || stateTime > 5f)
                    {
                        AddMemory($"Defending territory from {nearestCompetitor.name}");
                        currentTarget = nearestCompetitor;
                        ChangeState(BehaviorState.Hunting);
                        return;
                    }
                }
            }
            
            // Check for prey if hungry
            if (IsHungry())
            {
                nearestPrey = FindNearestOfTags(preyTags);
                if (nearestPrey != null && 
                    Vector3.Distance(transform.position, nearestPrey.transform.position) < senseRadius)
                {
                    if (currentState != BehaviorState.Hunting)
                    {
                        AddMemory($"Spotted prey: {nearestPrey.name}");
                        currentTarget = nearestPrey;
                        ChangeState(BehaviorState.Hunting);
                        return;
                    }
                }
            }
            
            // State-specific transitions
            switch (currentState)
            {
                case BehaviorState.Idle:
                    // If hungry but no visible prey, start seeking
                    if (IsHungry() && nearestPrey == null)
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    
                    // Random chance to patrol territory based on territoriality
                    if (Random.value < Territoriality * 0.02f)
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    
                    // If been idle too long, rest or patrol
                    if (stateTime > 8f)
                    {
                        if (Random.value < 0.3f)
                        {
                            ChangeState(BehaviorState.Resting);
                        }
                        else
                        {
                            ChangeState(BehaviorState.Seeking);
                        }
                        return;
                    }
                    break;
                
                case BehaviorState.Seeking:
                    // Give up seeking if been seeking a while without finding prey
                    if (stateTime > 20f * persistence && nearestPrey == null)
                    {
                        // If very hungry, keep seeking, otherwise go idle
                        if (!IsStarving())
                        {
                            ChangeState(BehaviorState.Idle);
                            return;
                        }
                    }
                    break;
                
                case BehaviorState.Hunting:
                    // Target disappeared or died
                    if (currentTarget == null)
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    
                    // Been hunting too long without success, give up based on persistence
                    float persistenceFactor = Mathf.Lerp(10f, 30f, persistence);
                    if (stateTime > persistenceFactor)
                    {
                        AddMemory("Failed hunt, prey escaped");
                        huntSuccessRate = Mathf.Max(0.1f, huntSuccessRate - 0.05f);
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                
                case BehaviorState.Feeding:
                    // Finished feeding or no longer hungry
                    if (stateTime > 5f || !IsHungry())
                    {
                        ChangeState(BehaviorState.Resting);
                        return;
                    }
                    break;
                
                case BehaviorState.Resting:
                    // Done resting after a while, or if hungry
                    if (stateTime > 10f || IsStarving())
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
            
            if (moveTimer > 2f)
            {
                // Move to a random nearby position within territory
                if (Vector3.Distance(transform.position, randomMoveTarget) > 0.5f)
                {
                    MoveToward(randomMoveTarget, moveSpeed * 0.5f);
                }
                else
                {
                    moveTimer = 0f;
                    // Stay closer to territory center
                    randomMoveTarget = territoryCenter + new Vector3(
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f),
                        0f
                    );
                }
            }
        }
        
        private void ProcessSeekingState()
        {
            moveTimer += Time.deltaTime;
            
            // Look for prey or patrol territory
            nearestPrey = FindNearestOfTags(preyTags);
            
            if (nearestPrey != null)
            {
                // Move toward the prey, but more slowly (stalking)
                currentTarget = nearestPrey;
                ChangeState(BehaviorState.Hunting);
            }
            else
            {
                // No prey visible, patrol territory
                if (moveTimer > 3f || Vector3.Distance(transform.position, randomMoveTarget) < 1f)
                {
                    moveTimer = 0f;
                    // Generate point within territory
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = Random.Range(0f, territoryRadius * 0.8f);
                    randomMoveTarget = territoryCenter + new Vector3(
                        Mathf.Cos(angle) * distance,
                        Mathf.Sin(angle) * distance,
                        0f
                    );
                }
                
                MoveToward(randomMoveTarget, moveSpeed * 0.7f);
            }
        }
        
        private void ProcessHuntingState()
        {
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
                
                // If close enough to attack
                if (distanceToTarget < 1.5f && Time.time > lastAttackTime + attackCooldown)
                {
                    // Attack the prey
                    lastAttackTime = Time.time;
                    
                    // Get animal component if it exists
                    Animal targetAnimal = currentTarget.GetComponent<Animal>();
                    if (targetAnimal != null)
                    {
                        // Deal damage modified by aggression
                        float adjustedDamage = attackDamage * (0.8f + aggression * 0.4f);
                        targetAnimal.TakeDamage(adjustedDamage);
                        
                        // If prey is dead or very weak, start feeding
                        if (targetAnimal.currentHealth <= 0)
                        {
                            AddMemory($"Successfully hunted {targetAnimal.animalName}");
                            huntSuccessRate = Mathf.Min(1f, huntSuccessRate + 0.1f);
                            ChangeState(BehaviorState.Feeding);
                            return;
                        }
                    }
                    else
                    {
                        // For non-animal targets like competitors
                        // Simply stay in hunting state
                    }
                }
                else
                {
                    // Not close enough, stalk or chase based on distance
                    float speed = (distanceToTarget < 5f) ? stalkSpeed : chaseSpeed;
                    
                    // Get animal component if it exists
                    Animal targetAnimal = currentTarget.GetComponent<Animal>();
                    
                    // If smart enough and prey is fleeing, try to predict movement
                    if (intelligence > 0.7f && targetAnimal != null && targetAnimal.CurrentState == BehaviorState.Fleeing)
                    {
                        // Simple prediction - aim slightly ahead of current position
                        Vector3 targetVelocity = (targetAnimal.transform.position - transform.position).normalized;
                        Vector3 predictedPosition = targetAnimal.transform.position + targetVelocity * 2f;
                        MoveToward(predictedPosition, speed);
                    }
                    else
                    {
                        // Direct chase
                        MoveToward(currentTarget.transform.position, speed);
                    }
                }
            }
            else
            {
                // Target disappeared
                ChangeState(BehaviorState.Seeking);
            }
        }
        
        private void ProcessFeedingState()
        {
            // Feeding reduces hunger
            float feedAmount = preyNutritionValue * Time.deltaTime;
            Eat(feedAmount);
            
            // Stay in place while feeding
            // Could add feeding animation here
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
            
            // Flip sprite based on movement direction
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
        
        // Override for carnivore-specific archetypes
        public override void DetermineArchetype()
        {
            if (Territoriality > 0.7f && persistence > 0.6f)
            {
                archetype = "The Guardian";
            }
            else if (intelligence > 0.7f && huntSuccessRate < 0.4f)
            {
                archetype = "The Opportunist";
            }
            else if (aggression > 0.8f && huntSuccessRate > 0.7f)
            {
                archetype = "The Apex";
            }
            else if (sociability > 0.7f && intelligence > 0.6f)
            {
                archetype = "The Pack Hunter";
            }
            else if (persistence > 0.7f && intelligence > 0.8f)
            {
                archetype = "The Trainer";
            }
            else
            {
                archetype = "Common Predator";
            }
        }
    }
}