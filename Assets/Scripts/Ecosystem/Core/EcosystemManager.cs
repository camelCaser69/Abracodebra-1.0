using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Ecosystem
{
    /// <summary>
    /// Core manager for the ecosystem simulation. Handles spawning, time control,
    /// and global ecosystem properties.
    /// </summary>
    public class EcosystemManager : MonoBehaviour
    {
        // Singleton instance
        public static EcosystemManager Instance { get; private set; }
        
        [Header("Ecosystem Boundaries")]
        public Vector2 worldSize = new Vector2(100f, 100f);
        public Transform worldCenter;
        
        [Header("Time Settings")]
        [Range(0.1f, 10f)] public float timeScale = 1f;
        public bool pauseSimulation = false;
        public int currentDay = 1;
        public float dayDuration = 240f; // seconds in real-time for one day
        
        [Header("Population Settings")]
        public int maxHerbivores = 15;
        public int maxCarnivores = 5;
        public int maxPlants = 20;
        
        [Header("Spawner Settings")]
        public GameObject[] herbivorePrefabs;
        public GameObject[] carnivorePrefabs;
        public GameObject[] plantPrefabs;
        
        [Header("Spawning Probabilities")]
        [Range(0f, 1f)] public float herbivoreSpawnChance = 0.3f;
        [Range(0f, 1f)] public float carnivoreSpawnChance = 0.1f;
        [Range(0f, 1f)] public float plantSpawnChance = 0.2f;
        
        [Header("UI Elements")]
        public TMP_Text dayCounterText;
        public TMP_Text populationText;
        
        // Internal tracking
        private float dayTimer = 0f;
        private int herbivoreCount = 0;
        private int carnivoreCount = 0;
        private int plantCount = 0;
        private float nextSpawnCheckTime = 0f;
        
        // Lists to track all ecosystem entities
        private List<Animal> allAnimals = new List<Animal>();
        private List<PlantGrowth> allPlants = new List<PlantGrowth>();
        
        private void Awake()
        {
            // Set up singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // If no world center specified, use this transform
            if (worldCenter == null)
                worldCenter = transform;
        }
        
        private void Start()
        {
            // Initial ecosystem setup
            PerformInitialSpawning();
            
            // Count existing ecosystem entities
            CountExistingEntities();
            
            // Update UI
            UpdateUI();
        }
        
        private void Update()
        {
            if (pauseSimulation)
                return;
                
            // Apply time scale
            Time.timeScale = timeScale;
            
            // Update day timer
            dayTimer += Time.deltaTime;
            if (dayTimer >= dayDuration)
            {
                dayTimer = 0f;
                AdvanceDay();
            }
            
            // Periodically check spawning
            if (Time.time > nextSpawnCheckTime)
            {
                nextSpawnCheckTime = Time.time + 5f;
                CheckAutomaticSpawning();
            }
            
            // Update counts
            CountExistingEntities();
            
            // Update UI every few frames
            if (Time.frameCount % 30 == 0)
            {
                UpdateUI();
            }
        }
        
        private void PerformInitialSpawning()
        {
            // Spawn initial plants
            for (int i = 0; i < maxPlants / 2; i++)
            {
                SpawnRandomPlant();
            }
            
            // Spawn initial herbivores
            for (int i = 0; i < maxHerbivores / 3; i++)
            {
                SpawnRandomHerbivore();
            }
            
            // Spawn initial carnivores
            for (int i = 0; i < maxCarnivores / 3; i++)
            {
                SpawnRandomCarnivore();
            }
        }
        
        private void AdvanceDay()
        {
            currentDay++;
            Debug.Log($"Ecosystem: Day {currentDay} begins");
            
            // Age all animals
            foreach (var animal in allAnimals)
            {
                if (animal != null)
                {
                    animal.age++;
                    
                    // Animals might develop archetypes based on experiences
                    if (animal.age % 5 == 0)
                    {
                        animal.DetermineArchetype();
                    }
                }
            }
            
            // Update UI
            UpdateUI();
        }
        
        private void CheckAutomaticSpawning()
        {
            // Check if we need more plants
            if (plantCount < maxPlants && Random.value < plantSpawnChance)
            {
                SpawnRandomPlant();
            }
            
            // Check if we need more herbivores
            if (herbivoreCount < maxHerbivores && Random.value < herbivoreSpawnChance)
            {
                SpawnRandomHerbivore();
            }
            
            // Check if we need more carnivores (only if there are enough herbivores)
            if (carnivoreCount < maxCarnivores && herbivoreCount > maxCarnivores * 2 && Random.value < carnivoreSpawnChance)
            {
                SpawnRandomCarnivore();
            }
        }
        
        private void CountExistingEntities()
        {
            // Clean up null references
            allAnimals.RemoveAll(a => a == null);
            allPlants.RemoveAll(p => p == null);
            
            // Count by type
            herbivoreCount = 0;
            carnivoreCount = 0;
            
            foreach (var animal in allAnimals)
            {
                if (animal == null) continue;
                
                if (animal.animalType == AnimalType.Herbivore)
                    herbivoreCount++;
                else if (animal.animalType == AnimalType.Carnivore)
                    carnivoreCount++;
            }
            
            plantCount = allPlants.Count;
        }
        
        private void UpdateUI()
        {
            if (dayCounterText != null)
            {
                dayCounterText.text = $"Day: {currentDay}";
            }
            
            if (populationText != null)
            {
                populationText.text = $"Plants: {plantCount} | Herbivores: {herbivoreCount} | Carnivores: {carnivoreCount}";
            }
        }
        
        // Spawning methods
        
        public GameObject SpawnRandomHerbivore()
        {
            if (herbivorePrefabs == null || herbivorePrefabs.Length == 0)
            {
                Debug.LogWarning("No herbivore prefabs assigned to EcosystemManager");
                return null;
            }
            
            // Pick a random herbivore prefab
            GameObject prefab = herbivorePrefabs[Random.Range(0, herbivorePrefabs.Length)];
            
            // Spawn at random position within world bounds
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject herbivoreObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // Get animal component
            Animal animal = herbivoreObj.GetComponent<Animal>();
            if (animal != null)
            {
                allAnimals.Add(animal);
                
                // Randomize personality traits slightly
                RandomizeAnimalPersonality(animal);
            }
            
            return herbivoreObj;
        }
        
        public GameObject SpawnRandomCarnivore()
        {
            if (carnivorePrefabs == null || carnivorePrefabs.Length == 0)
            {
                Debug.LogWarning("No carnivore prefabs assigned to EcosystemManager");
                return null;
            }
            
            // Pick a random carnivore prefab
            GameObject prefab = carnivorePrefabs[Random.Range(0, carnivorePrefabs.Length)];
            
            // Spawn at random position within world bounds
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject carnivoreObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // Get animal component
            Animal animal = carnivoreObj.GetComponent<Animal>();
            if (animal != null)
            {
                allAnimals.Add(animal);
                
                // Randomize personality traits slightly
                RandomizeAnimalPersonality(animal);
            }
            
            return carnivoreObj;
        }
        
        public GameObject SpawnRandomPlant()
        {
            if (plantPrefabs == null || plantPrefabs.Length == 0)
            {
                Debug.LogWarning("No plant prefabs assigned to EcosystemManager");
                return null;
            }
            
            // Pick a random plant prefab
            GameObject prefab = plantPrefabs[Random.Range(0, plantPrefabs.Length)];
            
            // Spawn at random position within world bounds
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject plantObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // Get plant growth component
            PlantGrowth plant = plantObj.GetComponent<PlantGrowth>();
            if (plant != null)
            {
                allPlants.Add(plant);
                
                // Randomize plant genetics
                PlantGrowthExtension extension = plantObj.GetComponent<PlantGrowthExtension>();
                if (extension != null)
                {
                    RandomizePlantGenetics(extension);
                }
            }
            
            return plantObj;
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = worldCenter.position;
            float halfWidth = worldSize.x * 0.5f;
            float halfHeight = worldSize.y * 0.5f;
            
            return new Vector3(
                center.x + Random.Range(-halfWidth, halfWidth),
                center.y + Random.Range(-halfHeight, halfHeight),
                0f
            );
        }
        
        private void RandomizeAnimalPersonality(Animal animal)
        {
            // Generate a somewhat correlated set of personality traits
            // (some traits are likely to appear together)
            
            // Base randomization
            animal.aggression = Random.Range(0.2f, 0.8f);
            animal.curiosity = Random.Range(0.2f, 0.8f);
            animal.sociability = Random.Range(0.2f, 0.8f);
            animal.intelligence = Random.Range(0.3f, 0.7f);
            animal.persistence = Random.Range(0.3f, 0.7f);
            
            // Add some correlations between traits
            if (animal.aggression > 0.6f)
            {
                // Aggressive animals tend to be less sociable but more persistent
                animal.sociability *= 0.7f;
                animal.persistence = Mathf.Min(1f, animal.persistence * 1.3f);
            }
            
            if (animal.curiosity > 0.6f)
            {
                // Curious animals tend to be more intelligent
                animal.intelligence = Mathf.Min(1f, animal.intelligence * 1.3f);
            }
            
            if (animal.sociability > 0.7f)
            {
                // Social animals tend to be less aggressive
                animal.aggression *= 0.7f;
            }
            
            // Determine archetype based on personality
            animal.DetermineArchetype();
        }
        
        private void RandomizePlantGenetics(PlantGrowthExtension plant)
        {
            // Randomize genetic traits
            plant.toxicityGene = Random.Range(0.1f, 0.4f);
            plant.toughnessGene = Random.Range(0.2f, 0.6f);
            plant.nutritionGene = Random.Range(0.3f, 0.7f);
            plant.growthSpeedGene = Random.Range(0.3f, 0.8f);
            plant.rootDepthGene = Random.Range(0.3f, 0.7f);
            plant.attractiveness = Random.Range(0.4f, 0.8f);
            
            // Add some correlations between traits
            if (plant.toxicityGene > 0.3f)
            {
                // Toxic plants tend to grow slower but be tougher
                plant.growthSpeedGene *= 0.8f;
                plant.toughnessGene = Mathf.Min(1f, plant.toughnessGene * 1.2f);
            }
            
            if (plant.nutritionGene > 0.6f)
            {
                // Nutritious plants are more visible/attractive to herbivores
                plant.plantVisibility = Mathf.Min(1f, plant.plantVisibility * 1.2f);
            }
        }
        
        // Public interface methods
        
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Clamp(scale, 0.1f, 10f);
        }
        
        public void TogglePause()
        {
            pauseSimulation = !pauseSimulation;
            Time.timeScale = pauseSimulation ? 0f : timeScale;
        }
        
        public int GetPopulationOfType(AnimalType type)
        {
            int count = 0;
            foreach (var animal in allAnimals)
            {
                if (animal != null && animal.animalType == type)
                    count++;
            }
            return count;
        }
        
        public List<Animal> GetAllAnimalsOfType(AnimalType type)
        {
            return allAnimals.FindAll(a => a != null && a.animalType == type);
        }
        
        public void SetWorldBounds(Vector2 size)
        {
            worldSize = size;
        }
    }
}