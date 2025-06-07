using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; set; }

    [Header("Phase Panels")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject growthAndThreatPanel;
    [SerializeField] private GameObject recoveryPanel;

    [Header("Phase Buttons")]
    [SerializeField] private Button startGrowthPhaseButton;
    [SerializeField] private Button startRecoveryPhaseButton;
    [SerializeField] private Button startNewPlanningPhaseButton;

    [Header("Other UI Panels")]
    [SerializeField] private GameObject nodeEditorPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (RunManager.Instance == null)
        {
            Debug.LogError("[UIManager] RunManager.Instance not found! UI will not function correctly.");
            return;
        }

        RunManager.Instance.OnRunStateChanged += HandleRunStateChanged;

        if (startGrowthPhaseButton != null) startGrowthPhaseButton.onClick.AddListener(OnStartGrowthPhaseClicked);
        if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.onClick.AddListener(OnStartRecoveryPhaseClicked);
        if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.onClick.AddListener(OnStartNewPlanningPhaseClicked);

        // Initial state setup
        HandleRunStateChanged(RunManager.Instance.CurrentState);
    }

    void OnDestroy()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged -= HandleRunStateChanged;
        }
    }

    private void HandleRunStateChanged(RunState newState)
    {
        // Panel & Button visibility logic
        planningPanel?.SetActive(newState == RunState.Planning);
        growthAndThreatPanel?.SetActive(newState == RunState.GrowthAndThreat);
        recoveryPanel?.SetActive(newState == RunState.Recovery);
        
        nodeEditorPanel?.SetActive(newState == RunState.Planning);
        InventoryGridController.Instance?.gameObject.SetActive(newState == RunState.Planning);

        if (newState == RunState.GrowthAndThreat)
        {
            if (InventoryBarController.Instance != null) StartCoroutine(ShowInventoryBarDelayed());
        }
        else
        {
             if (InventoryBarController.Instance != null) InventoryBarController.Instance.HideBar();
        }

        startGrowthPhaseButton.interactable = (newState == RunState.Planning);
        startRecoveryPhaseButton.interactable = (newState == RunState.GrowthAndThreat);
        startNewPlanningPhaseButton.interactable = (newState == RunState.Recovery);
    }

    private IEnumerator ShowInventoryBarDelayed()
    {
        yield return null; // Wait one frame
        InventoryBarController.Instance?.ShowBar();
    }
    
    // --- MODIFIED: This method now uses the new API ---
    private void AutoReturnSeedFromEditorSlot()
    {
        if (NodeEditorGridController.Instance == null || InventoryGridController.Instance == null) return;

        var editor = NodeEditorGridController.Instance;
        var seedCell = editor.SeedSlotCell;
        
        // Use the new HasItem() method
        if (seedCell == null || !seedCell.HasItem()) return;

        editor.RefreshGraphAndUpdateSeed();

        // Use the new GetItemView() method
        ItemView seedView = seedCell.GetItemView();
        NodeData seedData = seedCell.GetNodeData();
        if (seedView == null || seedData == null) return;

        editor.UnloadSeedFromSlot();
        
        // ReturnGeneToInventory now expects an ItemView
        InventoryGridController.Instance.ReturnGeneToInventory(seedView, seedData);

        seedCell.ClearNodeReference();
        Debug.Log($"[UIManager] Auto-returned seed “{seedData.nodeDisplayName}” to inventory.");
    }

    private void OnStartGrowthPhaseClicked()
    {
        AutoReturnSeedFromEditorSlot();
        RunManager.Instance?.StartGrowthAndThreatPhase();
    }

    private void OnStartRecoveryPhaseClicked()
    {
        RunManager.Instance?.StartRecoveryPhase();
    }

    private void OnStartNewPlanningPhaseClicked()
    {
        RunManager.Instance?.StartNewPlanningPhase();
    }
}