using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    /// <summary>
    /// Component that extends animals with visual communication capabilities.
    /// This includes thought bubbles, relationship tracking, and more.
    /// </summary>
    [RequireComponent(typeof(Animal))]
    public class AnimalCommunication : MonoBehaviour
    {
        [Header("Thought Bubble Settings")]
        public GameObject thoughtBubblePrefab;
        public Transform bubbleSpawnPoint;
        public float minTimeBetweenThoughts = 10f;
        [Range(0f, 1f)] public float thoughtFrequency = 0.5f;
        
        [Header("Relationships")]
        public bool trackRelationships = true;
        public float relationshipRange = 10f;
        [Range(0f, 1f)] public float relationshipMemory = 0.5f;
        
        // Internal state
        private Animal animal;
        private ThoughtBubble activeBubble;
        private float lastThoughtTime = -10f;
        private RelationshipIndicator relationshipIndicator;
        private List<Transform> knownEntities = new List<Transform>();
        
        private void Start()
        {
            animal = GetComponent<Animal>();
            
            // Find the relationship indicator in the scene
            relationshipIndicator = FindObjectOfType<RelationshipIndicator>();
            
            // If no spawn point specified, create one above the animal
            if (bubbleSpawnPoint == null)
            {
                GameObject spawnPoint = new GameObject("BubbleSpawnPoint");
                spawnPoint.transform.SetParent(transform);
                spawnPoint.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                bubbleSpawnPoint = spawnPoint.transform;
            }
        }
        
        private void Update()
        {
            // Randomly show thoughts based on current state and frequency
            if (Time.time > lastThoughtTime + minTimeBetweenThoughts && 
                Random.value < thoughtFrequency * Time.deltaTime)
            {
                ShowRandomThought();
            }
            
            // Track relationships with nearby entities
            if (trackRelationships && relationshipIndicator != null)
            {
                UpdateRelationships();
            }
        }
        
        private void ShowRandomThought()
        {
            if (animal == null || thoughtBubblePrefab == null)
                return;
                
            // Different thoughts based on animal state
            string thought = GetRandomThoughtForState();
            if (!string.IsNullOrEmpty(thought))
            {
                ShowThoughtBubble(thought);
                lastThoughtTime = Time.time;
            }
        }
        
        private string GetRandomThoughtForState()
        {
            // Get lists of possible thoughts based on state
            List<string> possibleThoughts = new List<string>();
            
            switch (animal.CurrentState)
            {
                case BehaviorState.Idle:
                    possibleThoughts.Add("Just relaxing...");
                    possibleThoughts.Add("Nice day today");
                    possibleThoughts.Add("*yawn*");
                    
                    // Add personality-influenced thoughts
                    if (animal.curiosity > 0.7f)
                        possibleThoughts.Add("I wonder what's over there?");
                    if (animal.sociability > 0.7f)
                        possibleThoughts.Add("Where is everyone?");
                    break;
                    
                case BehaviorState.Seeking:
                    possibleThoughts.Add("Need to find food");
                    possibleThoughts.Add("Something to eat around here?");
                    possibleThoughts.Add("Hungry...");
                    
                    // Add hunger level thoughts
                    if (animal.currentHunger > animal.maxHunger * 0.8f)
                        possibleThoughts.Add("So hungry!");
                    
                    // Add personality-influenced thoughts
                    if (animal.persistence > 0.7f)
                        possibleThoughts.Add("Won't give up looking");
                    break;
                    
                case BehaviorState.Hunting:
                    possibleThoughts.Add("Target acquired");
                    possibleThoughts.Add("Almost got it...");
                    possibleThoughts.Add("This one's mine!");
                    
                    // Add personality-influenced thoughts
                    if (animal.aggression > 0.7f)
                        possibleThoughts.Add("Time to attack!");
                    if (animal.intelligence > 0.7f)
                        possibleThoughts.Add("If I approach from here...");
                    break;
                    
                case BehaviorState.Feeding:
                    possibleThoughts.Add("Mmm, delicious");
                    possibleThoughts.Add("Tasty!");
                    possibleThoughts.Add("*munch munch*");
                    
                    // Add personality-influenced thoughts
                    if (animal.sociability < 0.3f)
                        possibleThoughts.Add("Mine! All mine!");
                    break;
                    
                case BehaviorState.Fleeing:
                    possibleThoughts.Add("Run away!!!");
                    possibleThoughts.Add("Danger!");
                    possibleThoughts.Add("Help!");
                    
                    // Add personality-influenced thoughts
                    if (animal.Fearfulness > 0.7f)
                        possibleThoughts.Add("So scary!!!");
                    break;
                    
                case BehaviorState.Resting:
                    possibleThoughts.Add("Zzz...");
                    possibleThoughts.Add("*peaceful snooze*");
                    possibleThoughts.Add("Rest time");
                    break;
            }
            
            // Add archetype-specific thoughts
            AddArchetypeThoughts(possibleThoughts);
            
            // Choose a random thought from the list
            if (possibleThoughts.Count > 0)
            {
                return possibleThoughts[Random.Range(0, possibleThoughts.Count)];
            }
            
            return "";
        }
        
        private void AddArchetypeThoughts(List<string> thoughts)
        {
            if (string.IsNullOrEmpty(animal.archetype))
                return;
                
            // Add thoughts based on animal archetype
            switch (animal.archetype)
            {
                case "The Nibbler":
                    thoughts.Add("Just a little taste...");
                    thoughts.Add("So many plants to try!");
                    break;
                    
                case "The Devastator":
                    thoughts.Add("Must. Eat. EVERYTHING.");
                    thoughts.Add("This whole plant is mine!");
                    break;
                    
                case "The Guardian":
                    thoughts.Add("Protecting my territory");
                    thoughts.Add("No herbivores allowed here");
                    break;
                    
                case "The Opportunist":
                    thoughts.Add("Waiting for the right moment...");
                    thoughts.Add("Someone else did the hard work");
                    break;
                    
                case "The Apex":
                    thoughts.Add("I rule this ecosystem");
                    thoughts.Add("All fear me");
                    break;
                    
                case "The Pack Hunter":
                    thoughts.Add("Where's my pack?");
                    thoughts.Add("Together we're stronger");
                    break;
                    
                case "Seed Disperser":
                    thoughts.Add("These seeds are delicious");
                    thoughts.Add("I'll plant these somewhere nice");
                    break;
            }
        }
        
        public void ShowThoughtBubble(string text, float duration = 3f)
        {
            if (thoughtBubblePrefab == null || bubbleSpawnPoint == null)
                return;
                
            // If there's already an active bubble, update it
            if (activeBubble != null)
            {
                activeBubble.SetText(text, duration);
                return;
            }
            
            // Create new bubble
            GameObject bubbleObj = Instantiate(thoughtBubblePrefab, bubbleSpawnPoint.position, Quaternion.identity);
            bubbleObj.transform.SetParent(transform);
            
            activeBubble = bubbleObj.GetComponent<ThoughtBubble>();
            if (activeBubble == null)
            {
                activeBubble = bubbleObj.AddComponent<ThoughtBubble>();
            }
            
            activeBubble.SetText(text, duration);
            activeBubble.SetTarget(bubbleSpawnPoint);
        }
        
        private void UpdateRelationships()
        {
            if (animal == null)
                return;
                
            // Find nearby animals
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, relationshipRange);
            
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject == gameObject)
                    continue;
                    
                Animal otherAnimal = collider.GetComponent<Animal>();
                if (otherAnimal != null)
                {
                    // Determine relationship type based on animal types and behavior
                    RelationshipIndicator.RelationshipType relType = DetermineRelationshipType(otherAnimal);
                    
                    // Calculate relationship strength based on distance and memory
                    float distance = Vector3.Distance(transform.position, otherAnimal.transform.position);
                    float strength = Mathf.Lerp(0.3f, 1f, 1f - (distance / relationshipRange));
                    
                    // Duration based on memory (more memory = longer relationships)
                    float duration = Mathf.Lerp(5f, 30f, relationshipMemory);
                    
                    // Add relationship in the relationship indicator
                    relationshipIndicator.AddRelationship(transform, otherAnimal.transform, relType, strength, duration);
                    
                    // Add to known entities if new
                    if (!knownEntities.Contains(otherAnimal.transform))
                    {
                        knownEntities.Add(otherAnimal.transform);
                        
                        // Add memory of this encounter
                        animal.AddMemory($"Met {otherAnimal.animalName} ({otherAnimal.animalType})");
                        
                        // Show thought bubble when encountering new entity
                        if (Random.value < 0.7f)
                        {
                            string encounterThought = GetEncounterThought(otherAnimal);
                            ShowThoughtBubble(encounterThought, 2f);
                        }
                    }
                }
            }
        }
        
        private RelationshipIndicator.RelationshipType DetermineRelationshipType(Animal otherAnimal)
        {
            // Predator-prey relationships
            if (animal.animalType == AnimalType.Carnivore && 
                (otherAnimal.animalType == AnimalType.Herbivore || otherAnimal.animalType == AnimalType.Omnivore))
            {
                return RelationshipIndicator.RelationshipType.PredatorPrey;
            }
            
            if (animal.animalType == AnimalType.Omnivore && otherAnimal.animalType == AnimalType.Herbivore)
            {
                return RelationshipIndicator.RelationshipType.PredatorPrey;
            }
            
            // Competitive relationships (same type)
            if (animal.animalType == otherAnimal.animalType)
            {
                // Social animals may form cooperative groups
                if (animal.sociability > 0.7f && otherAnimal.sociability > 0.7f)
                {
                    return RelationshipIndicator.RelationshipType.Symbiotic;
                }
                else
                {
                    return RelationshipIndicator.RelationshipType.Competitive;
                }
            }
            
            // Prey-predator relationships (reverse of above)
            if (animal.animalType == AnimalType.Herbivore && 
                (otherAnimal.animalType == AnimalType.Carnivore || otherAnimal.animalType == AnimalType.Omnivore))
            {
                // If prey is currently fleeing, definitely in a predator-prey relationship
                if (animal.CurrentState == BehaviorState.Fleeing)
                {
                    return RelationshipIndicator.RelationshipType.PredatorPrey;
                }
                // Otherwise, more nuanced - might be neutral until the predator shows interest
                else
                {
                    return RelationshipIndicator.RelationshipType.Neutral;
                }
            }
            
            // Default neutral relationship
            return RelationshipIndicator.RelationshipType.Neutral;
        }
        
        private string GetEncounterThought(Animal otherAnimal)
        {
            if (animal.animalType == AnimalType.Herbivore && otherAnimal.animalType == AnimalType.Carnivore)
            {
                // Herbivore sees predator
                if (animal.Fearfulness > 0.7f)
                    return "Eek! A predator!";
                else
                    return "Uh oh...";
            }
            else if (animal.animalType == AnimalType.Carnivore && otherAnimal.animalType == AnimalType.Herbivore)
            {
                // Carnivore sees prey
                if (animal.aggression > 0.7f)
                    return "Mmm, dinner!";
                else
                    return "Potential prey spotted";
            }
            else if (animal.animalType == otherAnimal.animalType)
            {
                // Same type
                if (animal.sociability > 0.7f)
                    return "Hello there!";
                else if (animal.Territoriality > 0.7f)
                    return "This is MY territory!";
                else
                    return "Another one like me";
            }
            else
            {
                // Default
                return "Who's that?";
            }
        }
    }
}