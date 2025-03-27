using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Ecosystem
{
    /// <summary>
    /// This extension class adds ecosystem-specific functionality to the existing PlantGrowth class.
    /// In a future refactoring, these features would be merged directly into PlantGrowth.
    /// </summary>
    [RequireComponent(typeof(PlantGrowth))]
    public class PlantGrowthExtension : MonoBehaviour
    {
        [Header("Plant Genetics")]
        [Range(0f, 1f)] public float toxicityGene = 0f;     // How toxic the plant is to herbivores
        [Range(0f, 1f)] public float toughnessGene = 0f;    // Physical resistance to damage
        [Range(0f, 1f)] public float nutritionGene = 0.5f;  // Base nutritional value
        [Range(0f, 1f)] public float growthSpeedGene = 0.5f;// How quickly the plant grows
        [Range(0f, 1f)] public float rootDepthGene = 0.5f;  // How deep/extensive the root system is
        
        [Header("Environmental Responses")]
        public float droughtResistance = 0.5f;              // Ability to survive low water
        public float coldResistance = 0.5f;                 // Ability to survive low temperatures
        
        [Header("Interaction Settings")]
        public float plantVisibility = 0.5f;                // How easily spotted by herbivores
        public float attractiveness = 0.5f;                 // For flowers, attracts pollinators
        
        [Header("UI and Visualization")]
        public GameObject thoughtBubblePrefab;              // For showing plant "thoughts"
        public Transform thoughtBubbleSpawnPoint;           // Where to spawn the thought bubble
        
        // Reference to the main plant growth component
        private PlantGrowth plantGrowth;
        
        // List of all plant parts
        private List<PlantPart> allPlantParts = new List<PlantPart>();
        
        // UI elements
        private GameObject activeBubble;
        private TMP_Text bubbleText;
        private float bubbleTimer = 0f;
        
        // State tracking
        private float lastDamageTime = -10f;
        private float totalDamageReceived = 0f;
        private float lastSunlightValue = 1f;
        private bool isUnderDrought = false;
        
        private void Awake()
        {
            plantGrowth = GetComponent<PlantGrowth>();
        }
        
        private void Start()
        {
            // Register for stem growth events to apply genetics to new parts
            // For now, we'll just periodically check for new parts in Update
        }
        
        private void Update()
        {
            // Check for environmental conditions
            CheckEnvironmentalConditions();
            
            // Find any new plant parts that were added
            FindAndInitializeNewPlantParts();
            
            // Update any active UI elements
            UpdateUI();
            
            // Update genetics impact on growth parameters
            UpdateGrowthParametersFromGenes();
        }
        
        private void CheckEnvironmentalConditions()
        {
            // Get sunlight from WeatherManager
            float currentSunlight = 1f;
            if (WeatherManager.Instance != null)
            {
                currentSunlight = WeatherManager.Instance.sunIntensity;
            }
            
            // Check for drought (if we had a water system)
            // For now, we'll consider low sunlight as our example environmental stressor
            bool isDroughtNow = currentSunlight < 0.3f;
            
            // Detect changes in conditions
            if (currentSunlight < lastSunlightValue * 0.7f)
            {
                ShowThoughtBubble("Less sunlight... need to conserve energy", 3f);
            }
            else if (currentSunlight > lastSunlightValue * 1.5f && lastSunlightValue < 0.5f)
            {
                ShowThoughtBubble("Sunlight increasing! Time to grow", 3f);
            }
            
            // Drought began
            if (isDroughtNow && !isUnderDrought)
            {
                isUnderDrought = true;
                ShowThoughtBubble("Drought conditions! Conserving water", 5f);
                
                // Adjust growth based on drought resistance
                if (droughtResistance < 0.3f)
                {
                    // Low resistance, significantly slow growth
                    plantGrowth.growthSpeed *= 2f; // Slow down growth
                }
            }
            // Drought ended
            else if (!isDroughtNow && isUnderDrought)
            {
                isUnderDrought = false;
                ShowThoughtBubble("Water available again! Resuming normal growth", 4f);
                
                // Reset growth speed (this is simplified, in a full implementation
                // we would store and restore the original growth speed)
                UpdateGrowthParametersFromGenes();
            }
            
            lastSunlightValue = currentSunlight;
        }
        
        private void FindAndInitializeNewPlantParts()
        {
            // Find all plant parts that are children of this plant
            PlantPart[] parts = GetComponentsInChildren<PlantPart>();
            foreach (PlantPart part in parts)
            {
                if (!allPlantParts.Contains(part))
                {
                    // New part found, initialize its properties based on genes
                    InitializePlantPart(part);
                    allPlantParts.Add(part);
                    
                    // Subscribe to events
                    part.OnDamaged.AddListener(OnPartDamaged);
                    part.OnDestroyed.AddListener(() => OnPartDestroyed(part));
                }
            }
            
            // Clean up list of parts that no longer exist
            allPlantParts.RemoveAll(part => part == null);
        }
        
        private void InitializePlantPart(PlantPart part)
        {
            // Set properties based on genes and part type
            part.toxicity = toxicityGene;
            part.toughness = toughnessGene;
            part.nutritionalValue = nutritionGene;
            part.visibility = plantVisibility;
            
            // Adjust based on part type
            switch (part.partType)
            {
                case PlantPartType.Leaf:
                    // Leaves are more visible
                    part.visibility += 0.2f;
                    break;
                    
                case PlantPartType.Stem:
                    // Stems are tougher
                    part.toughness += 0.3f;
                    part.toxicity *= 0.7f; // Less toxic than leaves
                    break;
                    
                case PlantPartType.Flower:
                    // Flowers are attractive to pollinators and herbivores
                    part.visibility = Mathf.Min(1f, plantVisibility + attractiveness * 0.5f);
                    break;
                    
                case PlantPartType.Fruit:
                    // Fruit is nutritious and visible
                    part.nutritionalValue += 0.3f;
                    part.visibility += 0.3f;
                    part.toxicity *= 0.2f; // Much less toxic
                    break;
                    
                case PlantPartType.Root:
                    // Roots are protected underground
                    part.visibility *= 0.3f;
                    break;
            }
        }
        
        private void UpdateGrowthParametersFromGenes()
        {
            // Update growth speed from genes
            plantGrowth.growthSpeed = Mathf.Lerp(2f, 0.5f, growthSpeedGene);
            
            // Other growth parameters could be updated here
        }
        
        private void UpdateUI()
        {
            // Update thought bubble timer
            if (activeBubble != null && bubbleTimer > 0)
            {
                bubbleTimer -= Time.deltaTime;
                if (bubbleTimer <= 0)
                {
                    HideThoughtBubble();
                }
            }
        }
        
        private void OnPartDamaged(float damage)
        {
            totalDamageReceived += damage;
            lastDamageTime = Time.time;
            
            // React based on damage amount
            if (damage > 10f)
            {
                ShowThoughtBubble("Ouch! I'm being eaten!", 2f);
            }
            
            // Check if we should increase toxicity as a defense mechanism
            if (totalDamageReceived > 50f && toxicityGene < 0.8f)
            {
                // Gradually increase toxicity in response to repeated damage
                toxicityGene += 0.05f;
                toxicityGene = Mathf.Min(toxicityGene, 1f);
                
                ShowThoughtBubble("Developing toxins to defend myself", 3f);
                
                // Apply increased toxicity to all parts
                foreach (var part in allPlantParts)
                {
                    if (part != null)
                    {
                        part.toxicity = toxicityGene;
                    }
                }
            }
        }
        
        private void OnPartDestroyed(PlantPart part)
        {
            // React to losing a part
            switch (part.partType)
            {
                case PlantPartType.Leaf:
                    ShowThoughtBubble("Lost a leaf! Photosynthesis reduced", 3f);
                    break;
                    
                case PlantPartType.Stem:
                    ShowThoughtBubble("Stem damaged! Structural integrity compromised", 3f);
                    break;
                    
                case PlantPartType.Flower:
                    ShowThoughtBubble("Flower destroyed! Reproduction impaired", 3f);
                    break;
                    
                case PlantPartType.Fruit:
                    ShowThoughtBubble("Fruit taken! Seeds might be dispersed", 3f);
                    break;
            }
            
            // Remove this part from our list
            allPlantParts.Remove(part);
        }
        
        // UI Methods
        
        public void ShowThoughtBubble(string text, float duration)
        {
            if (activeBubble == null && thoughtBubblePrefab != null)
            {
                // Create bubble
                Vector3 spawnPos = (thoughtBubbleSpawnPoint != null) ? 
                    thoughtBubbleSpawnPoint.position : transform.position + Vector3.up * 2f;
                    
                activeBubble = Instantiate(thoughtBubblePrefab, spawnPos, Quaternion.identity, transform);
                
                // Set text
                bubbleText = activeBubble.GetComponentInChildren<TMP_Text>();
                if (bubbleText != null)
                {
                    bubbleText.text = text;
                }
                
                // Set timer
                bubbleTimer = duration;
            }
            else if (activeBubble != null && bubbleText != null)
            {
                // Update existing bubble
                bubbleText.text = text;
                bubbleTimer = duration;
            }
        }
        
        private void HideThoughtBubble()
        {
            if (activeBubble != null)
            {
                Destroy(activeBubble);
                activeBubble = null;
                bubbleText = null;
            }
        }
        
        // Public interface for other systems to interact with
        
        public float GetToxicityLevel()
        {
            return toxicityGene;
        }
        
        public void SetAttractivenessLevel(float value)
        {
            attractiveness = Mathf.Clamp01(value);
            
            // Update flower parts
            foreach (var part in allPlantParts)
            {
                if (part != null && part.partType == PlantPartType.Flower)
                {
                    part.visibility = Mathf.Min(1f, plantVisibility + attractiveness * 0.5f);
                }
            }
        }
    }
}