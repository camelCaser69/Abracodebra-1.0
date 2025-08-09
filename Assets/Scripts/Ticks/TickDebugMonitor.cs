// REWORKED FILE: Assets/Scripts/Ticks/TickDebugMonitor.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WegoSystem;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes;

public class TickDebugMonitor : MonoBehaviour
{
    // ... (Fields are mostly the same)
    [SerializeField] private GameObject monitorPanel;
    [SerializeField] private TextMeshProUGUI tickCounterText;
    [SerializeField] private TextMeshProUGUI animalCountText;
    [SerializeField] private TextMeshProUGUI plantCountText;
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            monitorPanel.SetActive(!monitorPanel.activeSelf);
        }

        if (monitorPanel.activeSelf)
        {
            UpdateDisplay();
        }
    }

    void UpdateDisplay()
    {
        if (TickManager.Instance == null) return;

        tickCounterText.text = $"Tick: {TickManager.Instance.CurrentTick}";
        animalCountText.text = $"Animals: {FindObjectsByType<AnimalController>(FindObjectsSortMode.None).Length}";
        
        // FIX: Use the new static list for an accurate plant count
        plantCountText.text = $"Plants: {PlantGrowth.AllActivePlants.Count}";
        
        // The rest of the display logic can be simplified or updated as needed
    }
}