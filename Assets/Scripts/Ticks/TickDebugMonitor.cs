﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

public class TickDebugMonitor : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] GameObject monitorPanel;
    [SerializeField] TextMeshProUGUI tickCounterText;
    [SerializeField] TextMeshProUGUI tickRateText;
    [SerializeField] TextMeshProUGUI dayProgressText;
    [SerializeField] TextMeshProUGUI phaseText;
    
    [Header("Entity Counters")]
    [SerializeField] TextMeshProUGUI animalCountText;
    [SerializeField] TextMeshProUGUI plantCountText;
    [SerializeField] TextMeshProUGUI fireflyCountText;
    
    [Header("Performance")]
    [SerializeField] TextMeshProUGUI tickDurationText;
    [SerializeField] Slider tickDurationBar;
    [SerializeField] float maxTickDuration = 50f; // milliseconds
    
    [Header("Effect Tracking")]
    [SerializeField] Transform effectListContainer;
    [SerializeField] GameObject effectEntryPrefab;
    
    [Header("Settings")]
    [SerializeField] KeyCode toggleKey = KeyCode.F3;
    [SerializeField] bool showOnStart = false;
    
    // Performance tracking
    float lastTickStartTime;
    float lastTickDuration;
    Queue<float> tickDurationHistory = new Queue<float>();
    const int HISTORY_SIZE = 60;
    
    // Effect tracking
    Dictionary<string, TickEffectEntry> activeEffects = new Dictionary<string, TickEffectEntry>();
    List<GameObject> effectUIEntries = new List<GameObject>();
    
    class TickEffectEntry {
        public string name;
        public string source;
        public int remainingTicks;
        public Color color;
    }
    
    void Start() {
        monitorPanel.SetActive(showOnStart);
        
        if (TickManager.Instance != null) {
            TickManager.Instance.OnTickStarted += OnTickStarted;
            TickManager.Instance.OnTickCompleted += OnTickCompleted;
        }
    }
    
    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.OnTickStarted -= OnTickStarted;
            TickManager.Instance.OnTickCompleted -= OnTickCompleted;
        }
    }
    
    void Update() {
        if (Input.GetKeyDown(toggleKey)) {
            monitorPanel.SetActive(!monitorPanel.activeSelf);
        }
        
        if (monitorPanel.activeSelf) {
            UpdateDisplay();
        }
    }
    
    void OnTickStarted(int tick) {
        lastTickStartTime = Time.realtimeSinceStartup;
    }
    
    void OnTickCompleted(int tick) {
        lastTickDuration = (Time.realtimeSinceStartup - lastTickStartTime) * 1000f; // Convert to ms
        
        tickDurationHistory.Enqueue(lastTickDuration);
        if (tickDurationHistory.Count > HISTORY_SIZE) {
            tickDurationHistory.Dequeue();
        }
    }
    
    // In TickDebugMonitor.cs, replace the UpdateDisplay method with this corrected version:

    void UpdateDisplay() {
        if (TickManager.Instance == null) return;
    
        tickCounterText.text = $"Tick: {TickManager.Instance.CurrentTick}";
        tickRateText.text = $"Rate: {TickManager.Instance.Config.ticksPerRealSecond:F1} ticks/sec";
    
        if (TickManager.Instance.Config != null) {
            int dayProgress = TickManager.Instance.Config.GetDayProgress(TickManager.Instance.CurrentTick);
            float dayPercent = TickManager.Instance.Config.GetDayProgressNormalized(TickManager.Instance.CurrentTick) * 100f;
            dayProgressText.text = $"Day: {dayProgress}/{TickManager.Instance.Config.ticksPerDay} ({dayPercent:F0}%)";
        }
    
        // Updated to use RunManager instead of TurnPhaseManager
        if (RunManager.Instance != null) {
            phaseText.text = $"Phase: {RunManager.Instance.CurrentPhase} (Tick {RunManager.Instance.CurrentPhaseTicks})";
        }
    
        animalCountText.text = $"Animals: {CountEntities<AnimalController>()}";
        plantCountText.text = $"Plants: {PlantGrowth.AllActivePlants.Count}";
        fireflyCountText.text = $"Fireflies: {CountEntities<FireflyController>()}";
    
        // Update tick duration display
        float avgTickDuration = tickDurationHistory.Count > 0 ? 
            tickDurationHistory.Average() : 0f;
        tickDurationText.text = $"Tick Time: {avgTickDuration:F1}ms (Last: {lastTickDuration:F1}ms)";
        tickDurationBar.value = avgTickDuration / maxTickDuration;
    
        // Update tick duration bar color
        if (avgTickDuration < maxTickDuration * 0.5f) {
            tickDurationBar.fillRect.GetComponent<Image>().color = Color.green;
        } else if (avgTickDuration < maxTickDuration * 0.8f) {
            tickDurationBar.fillRect.GetComponent<Image>().color = Color.yellow;
        } else {
            tickDurationBar.fillRect.GetComponent<Image>().color = Color.red;
        }
    
        // Update active effects
        UpdateActiveEffects();
    }
    
    int CountEntities<T>() where T : Component {
        return FindObjectsByType<T>(FindObjectsSortMode.None).Length;
    }
    
    // Replace the UpdateActiveEffects method in TickDebugMonitor.cs with this fixed version:

    void UpdateActiveEffects()
    {
        activeEffects.Clear();
    
        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        foreach (var animal in animals)
        {
            // Access health through the Needs component
            if (animal.Needs != null && animal.Needs.CurrentHealth < animal.Definition.maxHealth * 0.3f)
            {
                AddEffect($"{animal.SpeciesName}_low_health", "Low Health", animal.SpeciesName, -1, Color.red);
            }
        }
    
        foreach (var plant in PlantGrowth.AllActivePlants)
        {
            if (plant.CurrentState == PlantState.Growing)
            {
                var logic = plant.GrowthLogic;
                int remainingTicks = logic.GrowthTicksPerStage - Mathf.FloorToInt(logic.GetGrowthProgressNormalized() * logic.GrowthTicksPerStage);
                AddEffect($"plant_growing_{plant.GetInstanceID()}", "Growing", plant.name, remainingTicks, Color.green);
            }
        }
    
        RefreshEffectUI();
    }
    
    void AddEffect(string id, string name, string source, int remainingTicks, Color color) {
        activeEffects[id] = new TickEffectEntry {
            name = name,
            source = source,
            remainingTicks = remainingTicks,
            color = color
        };
    }
    
    void RefreshEffectUI() {
        // Clear old UI
        foreach (var entry in effectUIEntries) {
            Destroy(entry);
        }
        effectUIEntries.Clear();
        
        // Create new UI entries
        foreach (var effect in activeEffects.Values.OrderBy(e => e.source)) {
            GameObject entry = Instantiate(effectEntryPrefab, effectListContainer);
            effectUIEntries.Add(entry);
            
            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) {
                string tickText = effect.remainingTicks >= 0 ? $"{effect.remainingTicks}t" : "∞";
                text.text = $"{effect.source}: {effect.name} [{tickText}]";
                text.color = effect.color;
            }
        }
    }
}