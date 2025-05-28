// FILE: Assets/Scripts/Genetics/SeedSelectionUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SeedSelectionUI : MonoBehaviour
{
    public static SeedSelectionUI Instance { get; private set; }
    
    [Header("UI References")]
    public GameObject selectionPanel;
    public Transform seedButtonContainer;
    public GameObject seedButtonPrefab;
    public Button cancelButton;
    public TMP_Text titleText;
    public TMP_Text instructionText;
    
    [Header("Settings")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Current state
    private System.Action<SeedInstance> onSeedSelectedCallback;
    private List<GameObject> currentSeedButtons = new List<GameObject>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Setup cancel button
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSelection);
        
        // Validate required components
        if (seedButtonPrefab == null)
            Debug.LogError("[SeedSelectionUI] Seed Button Prefab not assigned!", this);
        if (seedButtonContainer == null)
            Debug.LogError("[SeedSelectionUI] Seed Button Container not assigned!", this);
        
        // Start hidden
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();
            
        if (Instance == this)
            Instance = null;
    }
    
    /// <summary>
    /// Shows the seed selection UI with plantable seeds
    /// </summary>
    /// <param name="onSeedSelected">Callback when a seed is selected</param>
    public void ShowSeedSelection(System.Action<SeedInstance> onSeedSelected)
    {
        if (PlayerGeneticsInventory.Instance == null)
        {
            Debug.LogError("[SeedSelectionUI] PlayerGeneticsInventory not found!");
            return;
        }
        
        onSeedSelectedCallback = onSeedSelected;
        
        // Get plantable seeds
        List<SeedInstance> plantableSeeds = PlayerGeneticsInventory.Instance.GetPlantableSeeds();
        
        if (plantableSeeds.Count == 0)
        {
            Debug.LogWarning("[SeedSelectionUI] No plantable seeds available!");
            if (showDebugLogs)
                Debug.Log("[SeedSelectionUI] Player has no valid seeds to plant");
            return;
        }
        
        // Update UI text
        if (titleText != null)
            titleText.text = "Select Seed to Plant";
        if (instructionText != null)
            instructionText.text = $"Choose from {plantableSeeds.Count} available seeds:";
        
        // Create seed buttons
        CreateSeedButtons(plantableSeeds);
        
        // Show panel
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
        
        if (showDebugLogs)
            Debug.Log($"[SeedSelectionUI] Showing selection with {plantableSeeds.Count} seeds");
    }
    
    /// <summary>
    /// Hides the seed selection UI
    /// </summary>
    public void HideSelection()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
            
        ClearSeedButtons();
        onSeedSelectedCallback = null;
        
        if (showDebugLogs)
            Debug.Log("[SeedSelectionUI] Hidden selection UI");
    }
    
    private void CreateSeedButtons(List<SeedInstance> seeds)
    {
        if (seedButtonContainer == null || seedButtonPrefab == null)
            return;
            
        // Clear existing buttons
        ClearSeedButtons();
        
        // Create button for each seed
        foreach (SeedInstance seed in seeds)
        {
            if (seed == null) continue;
            
            GameObject buttonObj = Instantiate(seedButtonPrefab, seedButtonContainer);
            SeedSelectionButton buttonComponent = buttonObj.GetComponent<SeedSelectionButton>();
            
            if (buttonComponent == null)
                buttonComponent = buttonObj.AddComponent<SeedSelectionButton>();
                
            buttonComponent.Initialize(seed, this);
            currentSeedButtons.Add(buttonObj);
        }
    }
    
    private void ClearSeedButtons()
    {
        foreach (GameObject button in currentSeedButtons)
        {
            if (button != null)
                DestroyImmediate(button);
        }
        currentSeedButtons.Clear();
    }
    
    /// <summary>
    /// Called when a seed button is clicked
    /// </summary>
    public void OnSeedSelected(SeedInstance selectedSeed)
    {
        if (selectedSeed == null)
        {
            Debug.LogWarning("[SeedSelectionUI] Null seed selected!");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"[SeedSelectionUI] Seed selected: {selectedSeed.seedName}");
        
        // Invoke callback
        onSeedSelectedCallback?.Invoke(selectedSeed);
        
        // Hide UI
        HideSelection();
    }
    
    private void CancelSelection()
    {
        if (showDebugLogs)
            Debug.Log("[SeedSelectionUI] Selection cancelled");
            
        HideSelection();
    }
    
    /// <summary>
    /// Quick method for single seed auto-selection (when only one seed available)
    /// </summary>
    public void AttemptAutoSelection(System.Action<SeedInstance> onSeedSelected)
    {
        if (PlayerGeneticsInventory.Instance == null)
            return;
            
        List<SeedInstance> plantableSeeds = PlayerGeneticsInventory.Instance.GetPlantableSeeds();
        
        if (plantableSeeds.Count == 1)
        {
            // Auto-select the only available seed
            if (showDebugLogs)
                Debug.Log($"[SeedSelectionUI] Auto-selecting only available seed: {plantableSeeds[0].seedName}");
                
            onSeedSelected?.Invoke(plantableSeeds[0]);
        }
        else if (plantableSeeds.Count > 1)
        {
            // Show selection UI
            ShowSeedSelection(onSeedSelected);
        }
        else
        {
            // No seeds available
            Debug.LogWarning("[SeedSelectionUI] No plantable seeds available for auto-selection!");
        }
    }
}

