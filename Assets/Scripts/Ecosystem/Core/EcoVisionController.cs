using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Ecosystem
{
    /// <summary>
    /// Controller for the EcoVision™ Interface that shows ecosystem information
    /// from the plant's perspective.
    /// </summary>
    public class EcoVisionController : MonoBehaviour
    {
        public enum ViewMode
        {
            Normal,
            Threats,
            Opportunities,
            Resources
        }
        
        [Header("UI Elements")]
        public CanvasGroup interfaceCanvasGroup;
        public Image backgroundPanel;
        public TMP_Text statusText;
        public TMP_Text timeText;
        public Transform radarDisplay;
        public Image viewModeIndicator;
        
        [Header("Radar Settings")]
        public float radarRadius = 20f;
        public GameObject threatIndicatorPrefab;
        public GameObject opportunityIndicatorPrefab;
        public GameObject resourceIndicatorPrefab;
        
        [Header("View Mode Colors")]
        public Color normalModeColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        public Color threatModeColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);
        public Color opportunityModeColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        public Color resourceModeColor = new Color(0.2f, 0.2f, 0.8f, 0.3f);
        
        [Header("Controls")]
        public KeyCode toggleKey = KeyCode.V;
        public KeyCode cycleViewModeKey = KeyCode.C;
        public KeyCode threatViewKey = KeyCode.T;
        public KeyCode opportunityViewKey = KeyCode.O;
        public KeyCode resourceViewKey = KeyCode.R;
        
        // State
        private bool isVisible = false;
        private ViewMode currentViewMode = ViewMode.Normal;
        private Transform plantTransform;
        private PlantGrowthExtension plantExtension;
        private Dictionary<Transform, RectTransform> trackedEntities = new Dictionary<Transform, RectTransform>();
        private float updateTimer = 0f;
        
        private void Start()
        {
            // Find the player plant
            plantTransform = GameObject.FindObjectOfType<PlantGrowthExtension>()?.transform;
            if (plantTransform != null)
            {
                plantExtension = plantTransform.GetComponent<PlantGrowthExtension>();
            }
            
            // Initialize UI
            if (interfaceCanvasGroup != null)
            {
                interfaceCanvasGroup.alpha = 0f;
                interfaceCanvasGroup.blocksRaycasts = false;
            }
            
            // Set initial view mode
            UpdateViewMode(ViewMode.Normal);
        }
        
        private void Update()
        {
            // Toggle visibility
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleVisibility();
            }
            
            // Only process other inputs if visible
            if (isVisible)
            {
                // Cycle through view modes
                if (Input.GetKeyDown(cycleViewModeKey))
                {
                    CycleViewMode();
                }
                
                // Direct view mode selection
                if (Input.GetKeyDown(threatViewKey))
                {
                    UpdateViewMode(ViewMode.Threats);
                }
                else if (Input.GetKeyDown(opportunityViewKey))
                {
                    UpdateViewMode(ViewMode.Opportunities);
                }
                else if (Input.GetKeyDown(resourceViewKey))
                {
                    UpdateViewMode(ViewMode.Resources);
                }
                
                // Update radar at regular intervals
                updateTimer -= Time.deltaTime;
                if (updateTimer <= 0f)
                {
                    updateTimer = 0.5f; // Update twice per second
                    UpdateRadar();
                }
                
                // Update UI text
                UpdateUIText();
            }
        }
        
        private void ToggleVisibility()
        {
            isVisible = !isVisible;
            
            if (interfaceCanvasGroup != null)
            {
                interfaceCanvasGroup.alpha = isVisible ? 1f : 0f;
                interfaceCanvasGroup.blocksRaycasts = isVisible;
            }
        }
        
        private void CycleViewMode()
        {
            switch (currentViewMode)
            {
                case ViewMode.Normal:
                    UpdateViewMode(ViewMode.Threats);
                    break;
                case ViewMode.Threats:
                    UpdateViewMode(ViewMode.Opportunities);
                    break;
                case ViewMode.Opportunities:
                    UpdateViewMode(ViewMode.Resources);
                    break;
                case ViewMode.Resources:
                    UpdateViewMode(ViewMode.Normal);
                    break;
            }
        }
        
        private void UpdateViewMode(ViewMode newMode)
        {
            currentViewMode = newMode;
            
            // Update background color
            if (backgroundPanel != null)
            {
                switch (newMode)
                {
                    case ViewMode.Normal:
                        backgroundPanel.color = normalModeColor;
                        break;
                    case ViewMode.Threats:
                        backgroundPanel.color = threatModeColor;
                        break;
                    case ViewMode.Opportunities:
                        backgroundPanel.color = opportunityModeColor;
                        break;
                    case ViewMode.Resources:
                        backgroundPanel.color = resourceModeColor;
                        break;
                }
            }
            
            // Update view mode indicator
            if (viewModeIndicator != null)
            {
                switch (newMode)
                {
                    case ViewMode.Normal:
                        viewModeIndicator.color = normalModeColor;
                        break;
                    case ViewMode.Threats:
                        viewModeIndicator.color = threatModeColor;
                        break;
                    case ViewMode.Opportunities:
                        viewModeIndicator.color = opportunityModeColor;
                        break;
                    case ViewMode.Resources:
                        viewModeIndicator.color = resourceModeColor;
                        break;
                }
            }
            
            // Update radar for new mode
            ClearRadar();
            UpdateRadar();
        }
        
        private void UpdateUIText()
        {
            if (statusText != null && plantExtension != null)
            {
                // Update status based on view mode
                switch (currentViewMode)
                {
                    case ViewMode.Normal:
                        statusText.text = "EcoVision™ Normal Mode\nPress [C] to cycle views";
                        break;
                    case ViewMode.Threats:
                        statusText.text = $"EcoVision™ Threat Scanner\nDefenses: Toxicity {plantExtension.toxicityGene:F2}, Toughness {plantExtension.toughnessGene:F2}";
                        break;
                    case ViewMode.Opportunities:
                        statusText.text = $"EcoVision™ Opportunity Scanner\nAttractiveness: {plantExtension.attractiveness:F2}";
                        break;
                    case ViewMode.Resources:
                        statusText.text = $"EcoVision™ Resource Scanner\nGrowth Speed: {plantExtension.growthSpeedGene:F2}, Root Depth: {plantExtension.rootDepthGene:F2}";
                        break;
                }
            }
            
            if (timeText != null && EcosystemManager.Instance != null)
            {
                timeText.text = $"Day {EcosystemManager.Instance.currentDay}";
            }
        }
        
        private void UpdateRadar()
        {
            if (plantTransform == null || radarDisplay == null)
                return;
                
            // Remove indicators for entities that no longer exist
            List<Transform> entitiesToRemove = new List<Transform>();
            foreach (var kvp in trackedEntities)
            {
                if (kvp.Key == null)
                {
                    entitiesToRemove.Add(kvp.Key);
                }
            }
            
            foreach (Transform entity in entitiesToRemove)
            {
                if (trackedEntities[entity] != null)
                {
                    Destroy(trackedEntities[entity].gameObject);
                }
                trackedEntities.Remove(entity);
            }
            
            // Find and update entities based on view mode
            switch (currentViewMode)
            {
                case ViewMode.Threats:
                    UpdateThreatRadar();
                    break;
                case ViewMode.Opportunities:
                    UpdateOpportunityRadar();
                    break;
                case ViewMode.Resources:
                    UpdateResourceRadar();
                    break;
                case ViewMode.Normal:
                    // Normal mode shows a bit of everything
                    UpdateThreatRadar();
                    UpdateOpportunityRadar();
                    UpdateResourceRadar();
                    break;
            }
        }
        
        private void UpdateThreatRadar()
        {
            // Find herbivores (potential threats)
            Animal[] herbivores = GameObject.FindObjectsOfType<Herbivore>();
            
            foreach (Animal herbivore in herbivores)
            {
                if (herbivore == null)
                    continue;
                    
                // Calculate distance
                float distance = Vector3.Distance(plantTransform.position, herbivore.transform.position);
                
                // Only show if within radar radius
                if (distance <= radarRadius)
                {
                    // Add or update indicator
                    UpdateEntityIndicator(herbivore.transform, threatIndicatorPrefab, Color.red);
                    
                    // Scale size based on threat level
                    if (trackedEntities.TryGetValue(herbivore.transform, out RectTransform indicator))
                    {
                        // Calculate threat level based on herbivore properties
                        float threatLevel = herbivore.aggression * 0.7f + herbivore.persistence * 0.3f;
                        
                        // Reduce threat if plant is highly toxic
                        if (plantExtension != null && plantExtension.toxicityGene > 0.5f)
                        {
                            threatLevel *= (1f - plantExtension.toxicityGene * 0.5f);
                        }
                        
                        // Scale size based on threat level
                        float size = Mathf.Lerp(15f, 30f, threatLevel);
                        indicator.sizeDelta = new Vector2(size, size);
                    }
                }
                else if (trackedEntities.ContainsKey(herbivore.transform))
                {
                    // Remove if out of range
                    if (trackedEntities[herbivore.transform] != null)
                    {
                        Destroy(trackedEntities[herbivore.transform].gameObject);
                    }
                    trackedEntities.Remove(herbivore.transform);
                }
            }
        }
        
        private void UpdateOpportunityRadar()
        {
            // For now, let's assume opportunities are:
            // 1. Pollinators (for flowers)
            // 2. Seed dispersers (for fruits)
            // 3. Guardian predators (keeping herbivores away)
            
            // Find omnivores (potential pollinators/seed dispersers)
            Animal[] omnivores = GameObject.FindObjectsOfType<Omnivore>();
            
            foreach (Animal omnivore in omnivores)
            {
                if (omnivore == null)
                    continue;
                    
                // Calculate distance
                float distance = Vector3.Distance(plantTransform.position, omnivore.transform.position);
                
                // Only show if within radar radius
                if (distance <= radarRadius)
                {
                    // Add or update indicator
                    UpdateEntityIndicator(omnivore.transform, opportunityIndicatorPrefab, Color.green);
                }
                else if (trackedEntities.ContainsKey(omnivore.transform))
                {
                    // Remove if out of range
                    if (trackedEntities[omnivore.transform] != null)
                    {
                        Destroy(trackedEntities[omnivore.transform].gameObject);
                    }
                    trackedEntities.Remove(omnivore.transform);
                }
            }
            
            // Find carnivores (potential guardians)
            Animal[] carnivores = GameObject.FindObjectsOfType<Carnivore>();
            
            foreach (Animal carnivore in carnivores)
            {
                if (carnivore == null)
                    continue;
                    
                // Calculate distance
                float distance = Vector3.Distance(plantTransform.position, carnivore.transform.position);
                
                // Only show if within radar radius
                if (distance <= radarRadius)
                {
                    // Add or update indicator (use a different color for guardians)
                    UpdateEntityIndicator(carnivore.transform, opportunityIndicatorPrefab, new Color(0.5f, 0.8f, 0.5f));
                }
                else if (trackedEntities.ContainsKey(carnivore.transform))
                {
                    // Remove if out of range
                    if (trackedEntities[carnivore.transform] != null)
                    {
                        Destroy(trackedEntities[carnivore.transform].gameObject);
                    }
                    trackedEntities.Remove(carnivore.transform);
                }
            }
        }
        
        private void UpdateResourceRadar()
        {
            // For resources, we could show:
            // 1. Sunlight intensity in different areas
            // 2. Water/moisture levels
            // 3. Soil nutrients
            
            // For this simple version, we'll just simulate some resource spots
            
            // Get weather manager for sunlight info
            float globalSunlight = 1f;
            if (WeatherManager.Instance != null)
            {
                globalSunlight = WeatherManager.Instance.sunIntensity;
            }
            
            // Create some simulated resource spots based on world position
            // In a real implementation, these would be actual resource objects in the world
            CreateResourceIndicators(globalSunlight);
        }
        
        private void CreateResourceIndicators(float globalSunlight)
        {
            // This is a placeholder that creates simulated resource indicators
            // In a real implementation, these would be based on actual resources in the world
            
            // Get the world bounds
            Vector2 worldSize = new Vector2(100f, 100f);
            Vector3 worldCenter = Vector3.zero;
            
            if (EcosystemManager.Instance != null)
            {
                worldSize = EcosystemManager.Instance.worldSize;
                if (EcosystemManager.Instance.worldCenter != null)
                {
                    worldCenter = EcosystemManager.Instance.worldCenter.position;
                }
            }
            
            // Create resource spots in a grid pattern
            int gridSize = 5;
            float cellWidth = worldSize.x / gridSize;
            float cellHeight = worldSize.y / gridSize;
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 position = new Vector3(
                        worldCenter.x - worldSize.x/2 + x * cellWidth + cellWidth/2,
                        worldCenter.y - worldSize.y/2 + y * cellHeight + cellHeight/2,
                        0f
                    );
                    
                    // Calculate distance from plant
                    float distance = Vector3.Distance(plantTransform.position, position);
                    
                    // Only show if within radar radius
                    if (distance <= radarRadius)
                    {
                        // Create a unique key for this grid position
                        GameObject dummyObj = new GameObject($"Resource_{x}_{y}");
                        dummyObj.transform.position = position;
                        dummyObj.transform.SetParent(transform);
                        
                        // Resource value varies by position and global conditions
                        float resourceValue = 0.5f + 0.5f * Mathf.PerlinNoise(position.x * 0.1f, position.y * 0.1f);
                        resourceValue *= globalSunlight; // Affected by global sunlight
                        
                        // Color based on resource value
                        Color resourceColor = Color.Lerp(Color.cyan, Color.blue, resourceValue);
                        
                        // Add indicator
                        UpdateEntityIndicator(dummyObj.transform, resourceIndicatorPrefab, resourceColor);
                        
                        // Scale based on resource value
                        if (trackedEntities.TryGetValue(dummyObj.transform, out RectTransform indicator))
                        {
                            float size = Mathf.Lerp(10f, 25f, resourceValue);
                            indicator.sizeDelta = new Vector2(size, size);
                        }
                    }
                }
            }
        }
        
        private void UpdateEntityIndicator(Transform entity, GameObject prefab, Color color)
        {
            // If already tracking this entity, update its position
            if (trackedEntities.TryGetValue(entity, out RectTransform indicator))
            {
                UpdateIndicatorPosition(entity, indicator);
                
                // Update color
                Image image = indicator.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
            else
            {
                // Create new indicator
                if (prefab != null && radarDisplay != null)
                {
                    GameObject indicatorObj = Instantiate(prefab, radarDisplay);
                    RectTransform rect = indicatorObj.GetComponent<RectTransform>();
                    
                    UpdateIndicatorPosition(entity, rect);
                    
                    // Set color
                    Image image = indicatorObj.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = color;
                    }
                    
                    // Add to tracked entities
                    trackedEntities[entity] = rect;
                }
            }
        }
        
        private void UpdateIndicatorPosition(Transform entity, RectTransform indicator)
        {
            if (entity == null || indicator == null || plantTransform == null)
                return;
                
            // Calculate position relative to plant
            Vector3 relativePos = entity.position - plantTransform.position;
            
            // Scale to fit within radar display
            float radarScale = 100f; // Size of the radar display in UI
            float x = relativePos.x / radarRadius * radarScale;
            float y = relativePos.y / radarRadius * radarScale;
            
            // Clamp to radar bounds
            float maxDist = radarScale;
            float dist = Mathf.Sqrt(x*x + y*y);
            if (dist > maxDist)
            {
                float scale = maxDist / dist;
                x *= scale;
                y *= scale;
            }
            
            // Set position
            indicator.anchoredPosition = new Vector2(x, y);
            
            // Optional: fade based on distance
            Image image = indicator.GetComponent<Image>();
            if (image != null)
            {
                float normalizedDist = dist / maxDist;
                float alpha = Mathf.Lerp(1f, 0.3f, normalizedDist);
                Color color = image.color;
                color.a = alpha;
                image.color = color;
            }
        }
        
        private void ClearRadar()
        {
            foreach (var kvp in trackedEntities)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            
            trackedEntities.Clear();
        }
    }
}