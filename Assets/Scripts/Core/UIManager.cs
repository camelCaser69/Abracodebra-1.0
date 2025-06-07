// FILE: Assets/Scripts/Core/UIManager.cs
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Phase Panels (Assign in Inspector)")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject growthAndThreatPanel;
    [SerializeField] private GameObject recoveryPanel;

    [Header("Buttons (Assign in Inspector)")]
    [SerializeField] private Button startGrowthPhaseButton;
    [SerializeField] private Button startRecoveryPhaseButton;
    [SerializeField] private Button startNewPlanningPhaseButton;

    [Header("Node Editor Integration")]
    [SerializeField] private GameObject nodeEditorPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ENSURE PLANNING PANEL IS ACTIVE FIRST FOR INVENTORY INITIALIZATION
        if (planningPanel != null) 
        {
            planningPanel.SetActive(true);
            if (nodeEditorPanel != null) nodeEditorPanel.SetActive(true);
        }

        if (RunManager.Instance == null)
        {
            Debug.LogError("[UIManager] RunManager.Instance not found! UI will not function correctly.");
            return;
        }

        RunManager.Instance.OnRunStateChanged += HandleRunStateChanged;

        // Setup button listeners
        if (startGrowthPhaseButton != null)
            startGrowthPhaseButton.onClick.AddListener(OnStartGrowthPhaseClicked);
        else Debug.LogError("[UIManager] Start Growth Phase Button not assigned!");

        if (startRecoveryPhaseButton != null)
            startRecoveryPhaseButton.onClick.AddListener(OnStartRecoveryPhaseClicked);

        if (startNewPlanningPhaseButton != null)
            startNewPlanningPhaseButton.onClick.AddListener(OnStartNewPlanningPhaseClicked);
        else Debug.LogError("[UIManager] Start New Planning Phase Button not assigned!");
        
        // Initial UI setup - this will be called after inventory is initialized
        HandleRunStateChanged(RunManager.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged -= HandleRunStateChanged;
        }

        if (startGrowthPhaseButton != null) startGrowthPhaseButton.onClick.RemoveAllListeners();
        if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.onClick.RemoveAllListeners();
        if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.onClick.RemoveAllListeners();
    }

    private void HandleRunStateChanged(RunState newState)
    {
        Debug.Log($"[UIManager] Handling state change to: {newState}");

        switch (newState)
        {
            case RunState.Planning:
                // Activate planning panel and inventory grid
                if (planningPanel != null) planningPanel.SetActive(true);
                if (nodeEditorPanel != null) nodeEditorPanel.SetActive(true);
                
                if (InventoryGridController.Instance != null) 
                    InventoryGridController.Instance.gameObject.SetActive(true);
                if (InventoryBarController.Instance != null) 
                    InventoryBarController.Instance.HideBar();
                
                // Hide other panels
                if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(false);
                if (recoveryPanel != null) recoveryPanel.SetActive(false);
                
                // Button states
                if (startGrowthPhaseButton != null) startGrowthPhaseButton.interactable = true;
                if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.interactable = false;
                if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.interactable = false;
                break;
                
            case RunState.GrowthAndThreat:
                // FIXED: Hide planning panel first, then show growth panel
                if (planningPanel != null) planningPanel.SetActive(false);
                if (nodeEditorPanel != null) nodeEditorPanel.SetActive(false);
                
                // Activate growth panel and inventory bar
                if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(true);
                
                if (InventoryGridController.Instance != null) 
                    InventoryGridController.Instance.gameObject.SetActive(false);
                    
                // FIXED: Add safety check and delay for inventory bar
                if (InventoryBarController.Instance != null) 
                {
                    // Small delay to ensure inventory is properly initialized before showing bar
                    StartCoroutine(ShowInventoryBarDelayed());
                }
                
                // Hide recovery panel
                if (recoveryPanel != null) recoveryPanel.SetActive(false);
                
                // Button states
                if (startGrowthPhaseButton != null) startGrowthPhaseButton.interactable = false;
                if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.interactable = true;
                if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.interactable = false;
                break;
                
            case RunState.Recovery:
                // Activate recovery panel
                if (recoveryPanel != null) recoveryPanel.SetActive(true);
                
                // Hide both inventory systems
                if (InventoryGridController.Instance != null) 
                    InventoryGridController.Instance.gameObject.SetActive(false);
                if (InventoryBarController.Instance != null) 
                    InventoryBarController.Instance.HideBar();
                
                // Hide other panels
                if (planningPanel != null) planningPanel.SetActive(false);
                if (nodeEditorPanel != null) nodeEditorPanel.SetActive(false);
                if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(false);
                
                // Button states
                if (startGrowthPhaseButton != null) startGrowthPhaseButton.interactable = false;
                if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.interactable = false;
                if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.interactable = true;
                break;
        }
    }
    
    // FIXED: Add coroutine to safely show inventory bar with delay
    private System.Collections.IEnumerator ShowInventoryBarDelayed()
    {
        // Wait one frame to ensure everything is properly set up
        yield return null;
        
        if (InventoryBarController.Instance != null)
        {
            try
            {
                InventoryBarController.Instance.ShowBar();
                Debug.Log("[UIManager] Successfully showed inventory bar after delay");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error showing inventory bar: {e.Message}\n{e.StackTrace}");
            }
        }
    }
    private void AutoReturnSeedFromEditorSlot()
    {
        // Quick null-guards so we never throw from here
        if (NodeEditorGridController.Instance == null ||
            InventoryGridController.Instance  == null) return;

        var editor   = NodeEditorGridController.Instance;
        var seedCell = editor.SeedSlotCell;            // wrapper for the slot itself
        if (seedCell == null || !seedCell.HasNode()) return; // slot already empty

        // 1️⃣  Persist any edits the player made to the seed’s sequence
        editor.RefreshGraphAndUpdateSeed();            // saves into storedSequence :contentReference[oaicite:0]{index=0}

        // 2️⃣  Grab references to move
        NodeView seedView = seedCell.GetNodeView();
        NodeData seedData = seedCell.GetNodeData();
        if (seedView == null || seedData == null) return;

        // 3️⃣  Clear the editor first (UI clean-up)
        editor.UnloadSeedFromSlot();                   // clears sequence cells & hides panel :contentReference[oaicite:1]{index=1}

        // 4️⃣  Hand the seed back to inventory
        InventoryGridController.Instance.ReturnGeneToInventory(seedView, seedData); // :contentReference[oaicite:2]{index=2}

        // 5️⃣  Finally wipe the slot’s internal reference so it appears empty
        seedCell.ClearNodeReference();

        Debug.Log($"[UIManager] Auto-returned seed “{seedData.nodeDisplayName}” to inventory.");
    }

    private void OnStartGrowthPhaseClicked()
    {
        Debug.Log("[UIManager] StartGrowthPhaseButton Clicked");

        // Ensure the forgotten-seed edge-case is handled first
        AutoReturnSeedFromEditorSlot();                // <— NEW

        // Kick off the phase transition (inventory bar appears one frame later)
        RunManager.Instance?.StartGrowthAndThreatPhase(); // original behaviour :contentReference[oaicite:3]{index=3}
    }

    private void OnStartRecoveryPhaseClicked()
    {
        Debug.Log("[UIManager] StartRecoveryPhaseButton Clicked");
        RunManager.Instance?.StartRecoveryPhase();
    }

    private void OnStartNewPlanningPhaseClicked()
    {
        Debug.Log("[UIManager] StartNewPlanningPhaseButton Clicked");
        RunManager.Instance?.StartNewPlanningPhase();
    }
}