using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    public class Insect : Animal
    {
        [Header("Insect Settings")]
        public float flightSpeed = 3f;               // Speed when flying
        public float crawlSpeed = 1f;                // Speed when crawling
        public bool isFlying = true;                 // Whether currently flying
        public float hoverHeight = 0.5f;             // Height above ground when flying
        public float restingTime = 5f;               // How long to rest between flights
        
        [Header("Feeding Settings")]
        [Range(0f, 1f)] public float nectarPreference = 0.8f;   // Preference for flowers
        [Range(0f, 1f)] public float leafPreference = 0.3f;     // Preference for leaves
        
        [Header("Behavior Settings")]
        public float flightDuration = 10f;           // How long to fly before resting
        public float maxFlightDistance = 15f;        // Maximum distance to fly from spawn
        public bool isPollinator = false;            // Whether this insect pollinates flowers
        
        // Private state
        private Vector3 homePosition;                // Original spawn position
        private Vector3 randomMoveTarget;
        private float moveTimer = 0f;
        private float flightTimer = 0f;
        private GameObject nearestPlant;
        private GameObject nearestPredator;
        private GameObject nearestFlower;
        
        protected override void Awake()
        {
            base.Awake();
            animalType = AnimalType.Insect;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Remember spawn point as home position
            homePosition = transform.position;
            
            // Start with flying behavior for flying insects
            isFlying = (Random.value < 0.7f);
            
            // Smaller sized animals
            transform.localScale *= 0.5f;
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
            
            // Flying time management
            if (isFlying)
            {
                flightTimer += Time.deltaTime;
                if (flightTimer > flightDuration && currentState != BehaviorState.Fleeing)
                {
                    isFlying = false;
                    flightTimer = 0f;
                    ChangeState(BehaviorState.Resting);
                    return;
                }
            }
            
            // Check for food if hungry
            if (IsHungry() && currentState != BehaviorState.Feeding && currentState != BehaviorState.Fleeing)
            {
                if (isPollinator)
                {
                    nearestFlower = FindNearestFlower();
                    if (nearestFlower != null)
                    {
                        currentTarget = nearestFlower;
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                }
                
                nearestPlant = FindNearestPlantFood();
                if (nearestPlant != null)
                {
                    currentTarget = nearestPlant;
                    ChangeState(BehaviorState.Seeking);
                    return;
                }
            }
            
            // State-specific transitions
            switch (currentState)
            {
                case BehaviorState.Idle:
                    // If idle too long, start seeking
                    if (stateTime > 3f && Random.value < 0.4f)
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    
                    // If hungry, look for food
                    if (IsHungry())
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    break;
                    
                case BehaviorState.Seeking:
                    // Found target for feeding
                    if (currentTarget != null && 
                        Vector3.Distance(transform.position, currentTarget.transform.position) < 0.5f)
                    {
                        ChangeState(BehaviorState.Feeding);
                        return;
                    }
                    
                    // Give up seeking after a while
                    if (stateTime > 10f && currentTarget == null)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    
                    // If we flew too far from home, return
                    float distanceFromHome = Vector3.Distance(transform.position, homePosition);
                    if (distanceFromHome > maxFlightDistance)
                    {
                        currentTarget = null;
                        randomMoveTarget = homePosition;
                        return;
                    }
                    break;
                    
                case BehaviorState.Feeding:
                    // Done feeding or target gone
                    if (stateTime > 5f || !IsHungry() || currentTarget == null)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                    
                case BehaviorState.Fleeing:
                    // Done fleeing when predator is gone
                    if (nearestPredator == null || 
                        Vector3.Distance(transform.position, nearestPredator.transform.position) > senseRadius ||
                        stateTime > 5f)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                    
                case BehaviorState.Resting:
                    // Done resting after time passes
                    if (stateTime > restingTime || IsStarving())
                    {
                        isFlying = true;
                        flightTimer = 0f;
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
            }
        }
        
        private void CheckForPredators()
        {
            // Find potential predators
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
                // Always fly when fleeing
                isFlying = true;
                ChangeState(BehaviorState.Fleeing);
                AddMemory($"Fled from predator {nearestPredator.name}");
            }
        }
        
        private void ProcessIdleState()
        {
            // Occasionally move around randomly
            moveTimer += Time.deltaTime;
            
            if (moveTimer > 2f)
            {
                moveTimer = 0f;
                
                // Generate a random position to move to
                if (isFlying)
                {
                    randomMoveTarget = transform.position + new Vector3(
                        Random.Range(-3f, 3f),
                        Random.Range(-0.5f, 0.5f),
                        0f
                    );
                    
                    // Keep within flight range of home
                    Vector3 homeDirection = homePosition - transform.position;
                    float distanceFromHome = homeDirection.magnitude;
                    
                    if (distanceFromHome > maxFlightDistance * 0.8f)
                    {
                        // Head back toward home
                        randomMoveTarget = transform.position + homeDirection.normalized * 2f;
                    }
                    
                    // Set y position based on hover height
                    randomMoveTarget.y = hoverHeight;
                }
                else
                {
                    // Crawling - stay on ground
                    randomMoveTarget = transform.position + new Vector3(
                        Random.Range(-1f, 1f),
                        0f,
                        Random.Range(-1f, 1f)
                    );
                }
            }
            
            // Move toward random target
            if (Vector3.Distance(transform.position, randomMoveTarget) > 0.1f)
            {
                float speed = isFlying ? flightSpeed * 0.5f : crawlSpeed * 0.5f;
                MoveToward(randomMoveTarget, speed);
            }
        }
        
        private void ProcessSeekingState()
        {
            if (currentTarget != null)
            {
                // Move toward target
                float speed = isFlying ? flightSpeed : crawlSpeed;
                MoveToward(currentTarget.transform.position, speed);
            }
            else
            {
                // No target, search randomly
                moveTimer += Time.deltaTime;
                
                if (moveTimer > 1f)
                {
                    moveTimer = 0f;
                    
                    // Generate random search position
                    randomMoveTarget = transform.position + new Vector3(
                        Random.Range(-5f, 5f),
                        isFlying ? Random.Range(0f, hoverHeight) : 0f,
                        0f
                    );
                    
                    // Keep within flight range of home
                    Vector3 homeDirection = homePosition - transform.position;
                    float distanceFromHome = homeDirection.magnitude;
                    
                    if (distanceFromHome > maxFlightDistance * 0.8f)
                    {
                        // Head back toward home
                        randomMoveTarget = transform.position + homeDirection.normalized * 2f;
                    }
                }
                
                // Move toward random target
                float speed = isFlying ? flightSpeed * 0.7f : crawlSpeed * 0.7f;
                MoveToward(randomMoveTarget, speed);
                
                // Look for food while moving
                if (isPollinator)
                {
                    nearestFlower = FindNearestFlower();
                    if (nearestFlower != null)
                    {
                        currentTarget = nearestFlower;
                    }
                }
                
                if (currentTarget == null)
                {
                    nearestPlant = FindNearestPlantFood();
                    if (nearestPlant != null)
                    {
                        currentTarget = nearestPlant;
                    }
                }
            }
        }
        
        private void ProcessFeedingState()
        {
            if (currentTarget != null)
            {
                // Get the plant part
                PlantPart plantPart = currentTarget.GetComponent<PlantPart>();
                if (plantPart != null)
                {
                    // Start eating process
                    plantPart.StartEating(this);
                    
                    // Get nutrition
                    float nutritionPerSecond = plantPart.GetNutritionalValue() * 5f * Time.deltaTime;
                    Eat(nutritionPerSecond);
                    
                    // If pollinator and eating a flower, handle pollination
                    if (isPollinator && plantPart.partType == PlantPartType.Flower)
                    {
                        // Simulate pollination
                        if (Random.value < 0.1f * Time.deltaTime)
                        {
                            AddMemory("Pollinated a flower");
                            
                            // In a full implementation, we would track pollen and
                            // potentially transfer between plants
                        }
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
                // Target is gone
                ChangeState(BehaviorState.Seeking);
            }
        }
        
        private void ProcessFleeingState()
        {
            // Always fly when fleeing
            isFlying = true;
            
            if (nearestPredator != null)
            {
                // Calculate direction away from predator
                Vector3 awayDirection = transform.position - nearestPredator.transform.position;
                awayDirection.Normalize();
                
                // Move in that direction but faster
                Vector3 targetPosition = transform.position + awayDirection * 5f;
                
                // Adjust height based on predator type
                Animal predator = nearestPredator.GetComponent<Animal>();
                if (predator != null && predator.animalType == AnimalType.Omnivore)
                {
                    // If bird, flee higher
                    targetPosition.y = hoverHeight * 3f;
                }
                else
                {
                    // If ground predator, flee up
                    targetPosition.y = hoverHeight * 2f;
                }
                
                MoveToward(targetPosition, flightSpeed * 1.5f);
            }
            else
            {
                // No predator visible, head toward home
                Vector3 homeDirection = homePosition - transform.position;
                MoveToward(transform.position + homeDirection.normalized * 2f, flightSpeed);
            }
        }
        
        private void ProcessRestingState()
        {
            // Stay still while resting, sligihtly adjust position
            if (moveTimer > 3f && Random.value < 0.1f)
            {
                moveTimer = 0f;
                
                // Small adjustment
                Vector3 target = transform.position + new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    0f,
                    Random.Range(-0.2f, 0.2f)
                );
                
                MoveToward(target, crawlSpeed * 0.3f);
            }
            else
            {
                moveTimer += Time.deltaTime;
            }
        }
        
        // Helper method to move toward a target position
        private void MoveToward(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            
            // If flying, maintain proper height
            if (isFlying)
            {
                transform.position = new Vector3(
                    transform.position.x,
                    Mathf.Lerp(transform.position.y, hoverHeight, Time.deltaTime * 3f),
                    transform.position.z
                );
            }
            else
            {
                // Stay on ground
                transform.position = new Vector3(
                    transform.position.x,
                    0f,
                    transform.position.z
                );
            }
            
            // Flip sprite based on movement direction
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
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
                animator.SetBool("IsFlying", isFlying);
                animator.SetFloat("Horizontal", direction.x);
                animator.SetFloat("Vertical", direction.y);
            }
        }
        
        // Find the nearest flower
        private GameObject FindNearestFlower()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, senseRadius);
            
            float closestDistance = senseRadius;
            GameObject nearest = null;
            
            foreach (Collider2D collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;
                    
                PlantPart plantPart = collider.GetComponent<PlantPart>();
                if (plantPart != null && plantPart.partType == PlantPartType.Flower)
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
        
        // Override to specify plant part preferences
        protected override bool CanEatPlantPart(PlantPartType partType)
        {
            switch (partType)
            {
                case PlantPartType.Flower:
                    return isPollinator && Random.value < nectarPreference;
                    
                case PlantPartType.Leaf:
                    return Random.value < leafPreference;
                    
                case PlantPartType.Fruit:
                    return Random.value < 0.3f;
                    
                default:
                    return false;
            }
        }
        
        // Override for insect-specific archetypes
        public override void DetermineArchetype()
        {
            if (isPollinator && nectarPreference > 0.7f)
            {
                archetype = "The Pollinator Squad";
            }
            else if (leafPreference > 0.7f)
            {
                archetype = "The Decomposer Crew";
            }
            else if (intelligence > 0.7f && persistence > 0.6f)
            {
                archetype = "The Architects";
            }
            else if (sociability > 0.8f)
            {
                archetype = "The Swarm";
            }
            else if (adaptability > 0.7f)
            {
                archetype = "The Parasite";
            }
            else
            {
                archetype = "Common Insect";
            }
        }
    }
}