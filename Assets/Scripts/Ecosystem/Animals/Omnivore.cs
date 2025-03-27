using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    public class Omnivore : Animal
    {
        [Header("Omnivore Settings")]
        public float attackDamage = 10f;            // Damage dealt to smaller prey
        public float flightHeight = 2f;             // Normal flying height above ground
        public float divingSpeed = 7f;              // Speed when diving to catch prey
        public float soaringSpeed = 4f;             // Speed when flying normally
        public float landedSpeed = 2f;              // Speed when walking on ground
        public bool isFlying = true;                // Whether currently flying or on ground
        
        [Header("Diet Preferences")]
        [Range(0f, 1f)] public float meatPreference = 0.5f;     // Preference for meat over plants/seeds
        [Range(0f, 1f)] public float insectPreference = 0.7f;   // Preference for insects over other food
        public float seedNutritionValue = 10f;                  // Nutrition from eating seeds
        public float insectNutritionValue = 15f;                // Nutrition from eating insects
        public float preyNutritionValue = 25f;                  // Nutrition from eating small prey
        
        [Header("Targeting Preferences")]
        public string[] preyTags = { "Insect", "SmallPrey" };  
        public string[] seedTags = { "Seed", "Fruit" };
        public string[] predatorTags = { "LargePredator" };
        
        // Private state variables
        private GameObject nearestPrey;
        private GameObject nearestSeed;
        private GameObject nearestPredator;
        private Vector3 moveTarget;
        private float moveTimer = 0f;
        private Vector3 randomMoveTarget;
        private bool isPerched = false;
        private GameObject perchTarget;
        private bool hasNest = false;
        private Vector3 nestLocation;
        
        protected override void Awake()
        {
            base.Awake();
            animalType = AnimalType.Omnivore;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Start flying by default
            isFlying = true;
            
            // Remember if we should be sorting based on Y position
            if (sortable != null)
            {
                // Disable Y-sorting when flying, since we'll be above most objects
                // In a more comprehensive system, we'd handle this with proper Z-depth
            }
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
            
            // Look for food based on hunger and preferences
            if (IsHungry())
            {
                // Determine food preferences based on hunger level and innate preferences
                float meatUrgency = meatPreference * (currentHunger / maxHunger);
                float insectUrgency = insectPreference * (currentHunger / maxHunger);
                float seedUrgency = (1f - meatPreference) * (currentHunger / maxHunger);
                
                // Find potential food sources
                nearestPrey = FindNearestOfTags(preyTags);
                nearestSeed = FindNearestOfTags(seedTags);
                
                // Choose target based on preferences and availability
                if (nearestPrey != null && (meatUrgency > seedUrgency || nearestSeed == null))
                {
                    if (currentState != BehaviorState.Hunting)
                    {
                        currentTarget = nearestPrey;
                        ChangeState(BehaviorState.Hunting);
                        return;
                    }
                }
                else if (nearestSeed != null)
                {
                    if (currentState != BehaviorState.Seeking)
                    {
                        currentTarget = nearestSeed;
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                }
                else if (currentState != BehaviorState.Seeking && currentState != BehaviorState.Hunting)
                {
                    // No visible food, start seeking
                    ChangeState(BehaviorState.Seeking);
                    return;
                }
            }
            
            // State-specific transitions
            switch (currentState)
            {
                case BehaviorState.Idle:
                    // If been idle too long, maybe move randomly
                    if (stateTime > 5f && Random.value < 0.3f)
                    {
                        // Choose flight or perching based on state and energy
                        if (isFlying && Random.value < 0.4f && !IsHungry())
                        {
                            // Look for perch
                            FindPerchLocation();
                            if (perchTarget != null)
                            {
                                isPerched = true;
                                ChangeState(BehaviorState.Resting);
                                return;
                            }
                        }
                        else if (isPerched && (Random.value < 0.3f || IsHungry()))
                        {
                            // Take off
                            isPerched = false;
                            isFlying = true;
                            ChangeState(BehaviorState.Seeking);
                            return;
                        }
                        else
                        {
                            // Just move randomly
                            randomMoveTarget = GetRandomFlightPosition();
                            moveTimer = 0f;
                            ChangeState(BehaviorState.Seeking);
                            return;
                        }
                    }
                    break;
                    
                case BehaviorState.Seeking:
                    // Found seeds, transition to feeding
                    if (currentTarget != null && 
                        Vector3.Distance(transform.position, currentTarget.transform.position) < 1f)
                    {
                        ChangeState(BehaviorState.Feeding);
                        return;
                    }
                    
                    // Give up seeking after a while if not finding anything
                    float seekTimeout = 10f * persistence; // Persistence affects how long they'll search
                    if (stateTime > seekTimeout && currentTarget == null)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                    
                case BehaviorState.Hunting:
                    // Target disappeared
                    if (currentTarget == null)
                    {
                        ChangeState(BehaviorState.Seeking);
                        return;
                    }
                    
                    // Been hunting too long, give up based on persistence
                    float persistenceFactor = Mathf.Lerp(5f, 15f, persistence);
                    if (stateTime > persistenceFactor)
                    {
                        AddMemory("Failed hunt, prey escaped");
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                    
                case BehaviorState.Feeding:
                    // Finished feeding or no longer hungry
                    if (stateTime > 3f || !IsHungry() || currentTarget == null)
                    {
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                    
                case BehaviorState.Fleeing:
                    // Done fleeing when predator is far away or time passed
                    if (nearestPredator == null || 
                        Vector3.Distance(transform.position, nearestPredator.transform.position) > senseRadius * 1.5f ||
                        stateTime > 6f)
                    {
                        isFlying = true; // Always fly after fleeing
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                    break;
                    
                case BehaviorState.Resting:
                    // Done resting after a while or if hungry
                    if ((stateTime > 8f && Random.value < 0.3f) || IsStarving())
                    {
                        isPerched = false;
                        isFlying = true;
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
            
            if (isPerched)
            {
                // When perched, stay in place but maybe look around
                if (spriteRenderer != null && Random.value < 0.05f)
                {
                    // Randomly flip direction to simulate looking around
                    spriteRenderer.flipX = Random.value < 0.5f;
                }
            }
            else if (isFlying)
            {
                // When flying, make small movements
                if (moveTimer > 1f)
                {
                    moveTimer = 0f;
                    randomMoveTarget = GetRandomFlightPosition();
                }
                
                // Gentle flying motion
                MoveToward(randomMoveTarget, soaringSpeed * 0.6f);
            }
            else
            {
                // Walking on ground
                if (moveTimer > 2f)
                {
                    // Move to a random nearby position or stop
                    if (Random.value < 0.7f)
                    {
                        randomMoveTarget = transform.position + new Vector3(
                            Random.Range(-3f, 3f),
                            Random.Range(-3f, 3f),
                            0f
                        );
                        randomMoveTarget.y = 0f; // Keep on ground level
                    }
                    
                    moveTimer = 0f;
                }
                
                if (Vector3.Distance(transform.position, randomMoveTarget) > 0.5f)
                {
                    MoveToward(randomMoveTarget, landedSpeed);
                }
            }
        }
        
        private void ProcessSeekingState()
        {
            if (currentTarget != null)
            {
                // Target found, move toward it
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
                
                // Approach differently based on height
                if (isFlying && distanceToTarget < 3f)
                {
                    // Start descending toward target
                    Vector3 targetPos = currentTarget.transform.position;
                    targetPos.y += 0.5f; // Stay slightly above ground
                    MoveToward(targetPos, soaringSpeed);
                    
                    // Land when close enough
                    if (distanceToTarget < 1f)
                    {
                        isFlying = false;
                    }
                }
                else
                {
                    // Move toward target at appropriate speed
                    float speed = isFlying ? soaringSpeed : landedSpeed;
                    MoveToward(currentTarget.transform.position, speed);
                }
            }
            else
            {
                // No target, search randomly
                moveTimer += Time.deltaTime;
                
                if (moveTimer > 2f || Vector3.Distance(transform.position, randomMoveTarget) < 1f)
                {
                    moveTimer = 0f;
                    
                    if (isFlying)
                    {
                        randomMoveTarget = GetRandomFlightPosition();
                    }
                    else
                    {
                        randomMoveTarget = transform.position + new Vector3(
                            Random.Range(-5f, 5f),
                            0f,
                            Random.Range(-5f, 5f)
                        );
                    }
                }
                
                float speed = isFlying ? soaringSpeed : landedSpeed;
                MoveToward(randomMoveTarget, speed);
                
                // Look for food while moving
                nearestPrey = FindNearestOfTags(preyTags);
                nearestSeed = FindNearestOfTags(seedTags);
                
                if (nearestPrey != null && meatPreference > 0.5f)
                {
                    currentTarget = nearestPrey;
                    ChangeState(BehaviorState.Hunting);
                }
                else if (nearestSeed != null)
                {
                    currentTarget = nearestSeed;
                }
            }
        }
        
        private void ProcessHuntingState()
        {
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
                
                if (distanceToTarget < 1f)
                {
                    // Attack the prey
                    // Get prey animal component if it exists
                    Animal targetAnimal = currentTarget.GetComponent<Animal>();
                    if (targetAnimal != null && targetAnimal.animalType == AnimalType.Herbivore)
                    {
                        // Only attack if it's smaller (we'll use a simple check based on if it's a smaller herbivore)
                        targetAnimal.TakeDamage(attackDamage);
                        
                        // If prey is dead, start feeding
                        if (targetAnimal.currentHealth <= 0)
                        {
                            AddMemory($"Successfully caught prey: {targetAnimal.animalName}");
                            ChangeState(BehaviorState.Feeding);
                            return;
                        }
                    }
                    else if (currentTarget.CompareTag("Insect"))
                    {
                        // For insects, immediate consumption
                        Eat(insectNutritionValue);
                        AddMemory("Caught and ate an insect");
                        Destroy(currentTarget); // Remove the insect
                        currentTarget = null;
                        ChangeState(BehaviorState.Idle);
                        return;
                    }
                }
                else
                {
                    // Dive attack for flying birds
                    if (isFlying && distanceToTarget < 5f)
                    {
                        // Diving attack - move faster and directly at target
                        MoveToward(currentTarget.transform.position, divingSpeed);
                        
                        // Land when close to target
                        if (distanceToTarget < 1.5f)
                        {
                            isFlying = false;
                        }
                    }
                    else
                    {
                        // Normal pursuit
                        float speed = isFlying ? soaringSpeed : landedSpeed;
                        MoveToward(currentTarget.transform.position, speed);
                    }
                }
            }
            else
            {
                // Target lost, go back to seeking
                ChangeState(BehaviorState.Seeking);
            }
        }
        
        private void ProcessFeedingState()
        {
            if (currentTarget != null)
            {
                // Different feeding behavior based on food type
                if (currentTarget.CompareTag("Seed") || currentTarget.CompareTag("Fruit"))
                {
                    // Eat seeds/fruit
                    Eat(seedNutritionValue * Time.deltaTime);
                    
                    // Damage the plant part if applicable
                    PlantPart plantPart = currentTarget.GetComponent<PlantPart>();
                    if (plantPart != null)
                    {
                        plantPart.TakeDamage(5f * Time.deltaTime);
                        
                        // Potentially disperse seeds
                        if (plantPart.partType == PlantPartType.Fruit && Random.value < 0.1f)
                        {
                            AddMemory("Dispersed seeds from fruit");
                            // Seed dispersal logic would go here
                        }
                    }
                }
                else if (currentTarget.CompareTag("SmallPrey"))
                {
                    // Eating meat
                    Eat(preyNutritionValue * Time.deltaTime);
                    
                    // Destroy the prey gradually
                    Animal prey = currentTarget.GetComponent<Animal>();
                    if (prey != null)
                    {
                        prey.TakeDamage(10f * Time.deltaTime);
                    }
                }
                else if (currentTarget.CompareTag("Insect"))
                {
                    // Insects are eaten immediately in hunting state
                    // This is a fallback
                    Eat(insectNutritionValue);
                    Destroy(currentTarget);
                    currentTarget = null;
                    ChangeState(BehaviorState.Idle);
                }
            }
            else
            {
                // No target, end feeding
                ChangeState(BehaviorState.Idle);
            }
        }
        
        private void ProcessFleeingState()
        {
            // Always fly when fleeing
            isFlying = true;
            isPerched = false;
            
            if (nearestPredator != null)
            {
                // Calculate direction away from predator
                Vector3 awayDirection = transform.position - nearestPredator.transform.position;
                awayDirection.Normalize();
                
                // Move in that direction but faster and higher
                Vector3 targetPosition = transform.position + awayDirection * 10f;
                targetPosition.y = flightHeight * 1.5f; // Fly higher when fleeing
                MoveToward(targetPosition, divingSpeed); // Use diving speed for rapid escape
            }
            else
            {
                // No predator visible, gradually slow down and normalize height
                Vector3 targetPosition = transform.position + transform.forward * 5f;
                targetPosition.y = flightHeight;
                MoveToward(targetPosition, soaringSpeed);
                
                // Maybe end fleeing state
                if (Random.value < 0.1f)
                {
                    ChangeState(BehaviorState.Idle);
                }
            }
        }
        
        private void ProcessRestingState()
        {
            // If perched, stay in position
            if (isPerched && perchTarget != null)
            {
                // Stay at perch position
                transform.position = perchTarget.transform.position + new Vector3(0f, 1f, 0f);
                
                // Occasionally look around
                if (spriteRenderer != null && Random.value < 0.03f)
                {
                    spriteRenderer.flipX = !spriteRenderer.flipX;
                }
            }
            else
            {
                // Not properly perched, transition to idle
                isPerched = false;
                ChangeState(BehaviorState.Idle);
            }
        }
        
        // Helper method to move toward a target position
        private void MoveToward(Vector3 targetPosition, float speed)
        {
            // Calculate direction
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // Apply movement
            transform.position += direction * speed * Time.deltaTime;
            
            // Handle height based on flying state
            if (isFlying)
            {
                // Gradually adjust height toward flight height
                float currentHeight = transform.position.y;
                float targetHeight = flightHeight;
                transform.position = new Vector3(
                    transform.position.x,
                    Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * 2f),
                    transform.position.z
                );
            }
            else
            {
                // Stay on ground
                transform.position = new Vector3(
                    transform.position.x,
                    0f, // Ground level
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
        
        // Get a random position in the air
        private Vector3 GetRandomFlightPosition()
        {
            return transform.position + new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-1f, 1f),
                Random.Range(-10f, 10f)
            );
        }
        
        // Find a suitable perch location
        private void FindPerchLocation()
        {
            // In a more complex implementation, we'd look for actual perching spots
            // For now, we'll just simulate finding a perch
            
            // Simple version: look for tall objects like trees
            GameObject[] potentialPerches = GameObject.FindGameObjectsWithTag("Tree");
            
            float nearestDistance = 15f;
            GameObject nearestPerch = null;
            
            foreach (GameObject perch in potentialPerches)
            {
                float distance = Vector3.Distance(transform.position, perch.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPerch = perch;
                }
            }
            
            // If no trees, maybe just use other tall game objects
            if (nearestPerch == null)
            {
                // Fallback perch is just a random high position
                perchTarget = new GameObject("VirtualPerch");
                perchTarget.transform.position = transform.position + new Vector3(
                    Random.Range(-5f, 5f),
                    flightHeight,
                    Random.Range(-5f, 5f)
                );
            }
            else
            {
                perchTarget = nearestPerch;
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
        
        // Override for omnivore-specific archetypes
        public override void DetermineArchetype()
        {
            // Track food preferences for archetype determination
            int seedMeals = 0;
            int insectMeals = 0;
            int preyMeals = 0;
            
            foreach (string memory in memories)
            {
                if (memory.Contains("fruit") || memory.Contains("seed"))
                    seedMeals++;
                else if (memory.Contains("insect"))
                    insectMeals++;
                else if (memory.Contains("prey") || memory.Contains("caught"))
                    preyMeals++;
            }
            
            // Determine archetype based on behavior
            if (seedMeals > preyMeals && seedMeals > insectMeals)
            {
                archetype = "Seed Disperser";
            }
            else if (preyMeals > seedMeals && preyMeals > insectMeals)
            {
                archetype = "Hunter Bird";
            }
            else if (insectMeals > seedMeals && insectMeals > preyMeals)
            {
                archetype = "Insectivore";
            }
            else if (sociability > 0.7f)
            {
                archetype = "Flock Member";
            }
            else if (intelligence > 0.7f && persistence > 0.6f)
            {
                archetype = "Nest Builder";
            }
            else
            {
                archetype = "Common Bird";
            }
        }
    }
}