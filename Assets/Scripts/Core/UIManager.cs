// FILE: Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.UI; // Required for Button

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Phase Panels (Assign in Inspector)")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject growthAndThreatPanel;
    [SerializeField] private GameObject recoveryPanel;

    [Header("Buttons (Assign in Inspector)")]
    [SerializeField] private Button startGrowthPhaseButton;
    [SerializeField] private Button startRecoveryPhaseButton; // This might be automated later
    [SerializeField] private Button startNewPlanningPhaseButton;

    [Header("Node Editor Integration")]
    [Tooltip("Assign the Node Editor's main UI Panel (NodeEditorGridController.gridUIParent) here. This panel will be shown during Planning.")]
    [SerializeField] private GameObject nodeEditorPanel; // This should be NodeEditorGridController.gridUIParent

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
        // else Debug.LogWarning("[UIManager] Start Recovery Phase Button not assigned (may be automated).");

        if (startNewPlanningPhaseButton != null)
            startNewPlanningPhaseButton.onClick.AddListener(OnStartNewPlanningPhaseClicked);
        else Debug.LogError("[UIManager] Start New Planning Phase Button not assigned!");
        
        // Initial UI setup based on RunManager's starting state
        HandleRunStateChanged(RunManager.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged -= HandleRunStateChanged;
        }

        // Clean up button listeners
        if (startGrowthPhaseButton != null) startGrowthPhaseButton.onClick.RemoveAllListeners();
        if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.onClick.RemoveAllListeners();
        if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.onClick.RemoveAllListeners();
    }

    private void HandleRunStateChanged(RunState newState)
    {
        // Hide all main phase panels first
        if (planningPanel != null) planningPanel.SetActive(false);
        if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(false);
        if (recoveryPanel != null) recoveryPanel.SetActive(false);

        // Hide Node Editor Panel by default, show only in Planning
        if (nodeEditorPanel != null) nodeEditorPanel.SetActive(false);

        Debug.Log($"[UIManager] Handling state change to: {newState}");

        switch (newState)
        {
            case RunState.Planning:
                if (planningPanel != null) planningPanel.SetActive(true);
                if (nodeEditorPanel != null) nodeEditorPanel.SetActive(true); // Show Node Editor
                // Manage button interactivity
                if (startGrowthPhaseButton != null) startGrowthPhaseButton.interactable = true;
                if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.interactable = false;
                if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.interactable = false;
                break;
            case RunState.GrowthAndThreat:
                if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(true);
                // Manage button interactivity
                if (startGrowthPhaseButton != null) startGrowthPhaseButton.interactable = false;
                if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.interactable = true; // Or false if automated
                if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.interactable = false;
                break;
            case RunState.Recovery:
                if (recoveryPanel != null) recoveryPanel.SetActive(true);
                // Manage button interactivity
                if (startGrowthPhaseButton != null) startGrowthPhaseButton.interactable = false;
                if (startRecoveryPhaseButton != null) startRecoveryPhaseButton.interactable = false;
                if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.interactable = true;
                break;
        }
    }

    // --- Button Click Handlers ---
    private void OnStartGrowthPhaseClicked()
    {
        Debug.Log("[UIManager] StartGrowthPhaseButton Clicked");
        RunManager.Instance?.StartGrowthAndThreatPhase();
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