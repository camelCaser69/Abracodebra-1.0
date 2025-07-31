using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WegoSystem;

public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }
    
    [Header("Panels")]
    [SerializeField] GameObject planningPanel;
    [SerializeField] GameObject growthAndThreatPanel;
    [SerializeField] GameObject nodeEditorPanel;
    
    [Header("Action Buttons")]
    [SerializeField] Button startGrowthPhaseButton;
    [SerializeField] Button startNewPlanningPhaseButton;
    [SerializeField] Button endPlanningPhaseButton;
    [SerializeField] Button advanceTickButton;
    
    [Header("UI Elements")]
    [SerializeField] GameObject wegoControlPanel;
    [SerializeField] TextMeshProUGUI currentPhaseText;
    [SerializeField] TextMeshProUGUI tickCounterText;
    [SerializeField] TextMeshProUGUI persistentTickCounterText;
    [SerializeField] TextMeshProUGUI phaseProgressText;
    
    // Cached references for performance
    private RunManager runManager;
    private TickManager tickManager;
    
    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void Initialize()
    {
        runManager = RunManager.Instance;
        tickManager = TickManager.Instance;

        if (runManager == null)
        {
            Debug.LogError("[UIManager] RunManager.Instance not found! UI will not fn correctly.");
            return;
        }

        runManager.OnRunStateChanged += HandleRunStateChanged;
        runManager.OnPhaseChanged += HandlePhaseChanged;
        runManager.OnRoundChanged += HandleRoundChanged;

        if (tickManager != null)
        {
            tickManager.OnTickAdvanced += HandleTickAdvanced;
        }

        SetupButtons();

        HandleRunStateChanged(runManager.CurrentState);
        UpdatePhaseDisplay();
        UpdateTickDisplay();
    }
    
    void OnDestroy()
    {
        if (runManager != null)
        {
            runManager.OnRunStateChanged -= HandleRunStateChanged;
            runManager.OnPhaseChanged -= HandlePhaseChanged;
            runManager.OnRoundChanged -= HandleRoundChanged;
        }

        if (tickManager != null)
        {
            tickManager.OnTickAdvanced -= HandleTickAdvanced;
        }
    }
    
    void SetupButtons() {
        // Setup button listeners
        startGrowthPhaseButton?.onClick.AddListener(OnStartGrowthPhaseClicked);
        startNewPlanningPhaseButton?.onClick.AddListener(OnStartNewPlanningPhaseClicked);
        endPlanningPhaseButton?.onClick.AddListener(OnEndPlanningPhaseClicked);
        advanceTickButton?.onClick.AddListener(OnAdvanceTickClicked);
    }
    
    // Event handlers - only update UI when things actually change
    void HandleRunStateChanged(RunState newState) {
        // Update panel visibility
        if (planningPanel != null)
            planningPanel.SetActive(newState == RunState.Planning);
            
        if (growthAndThreatPanel != null)
            growthAndThreatPanel.SetActive(newState == RunState.GrowthAndThreat);
            
        if (nodeEditorPanel != null)
            nodeEditorPanel.SetActive(newState == RunState.Planning);
            
        // Update inventory visibility
        if (InventoryGridController.Instance != null) {
            InventoryGridController.Instance.gameObject.SetActive(newState == RunState.Planning);
        }
        
        if (newState == RunState.GrowthAndThreat) {
            if (InventoryBarController.Instance != null)
                StartCoroutine(ShowInventoryBarDelayed());
        }
        else {
            InventoryBarController.Instance?.HideBar();
        }
        
        UpdateButtonStates(newState);
    }
    
    void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase) {
        UpdatePhaseDisplay();
        UpdateButtonStates(runManager.CurrentState);
    }
    
    void HandleRoundChanged(int newRound) {
        // Could update round display here if needed
    }
    
    void HandleTickAdvanced(int currentTick) {
        UpdateTickDisplay();
        UpdatePhaseProgressDisplay();
    }
    
    // UI Update methods - called only when data changes
    void UpdatePhaseDisplay() {
        if (currentPhaseText != null && runManager != null) {
            currentPhaseText.text = $"Phase: {runManager.CurrentPhase}";
        }
    }
    
    void UpdateTickDisplay() {
        if (tickManager == null) return;
        
        string tickInfo = $"Tick: {tickManager.CurrentTick}";
        
        if (tickCounterText != null)
            tickCounterText.text = tickInfo;
            
        if (persistentTickCounterText != null)
            persistentTickCounterText.text = tickInfo;
    }
    
    void UpdatePhaseProgressDisplay() {
        if (phaseProgressText != null && runManager != null) {
            phaseProgressText.text = $"Phase Ticks: {runManager.CurrentPhaseTicks}";
        }
    }
    
    void UpdateButtonStates(RunState state) {
        bool isPlanning = (state == RunState.Planning);
        bool isPlanningPhase = (runManager?.CurrentPhase == GamePhase.Planning);
        
        if (startGrowthPhaseButton != null)
            startGrowthPhaseButton.interactable = isPlanning && isPlanningPhase;
            
        if (startNewPlanningPhaseButton != null)
            startNewPlanningPhaseButton.interactable = !isPlanning;
            
        if (endPlanningPhaseButton != null)
            endPlanningPhaseButton.interactable = isPlanning && isPlanningPhase;
            
        if (advanceTickButton != null)
            advanceTickButton.interactable = !isPlanningPhase;
    }
    
    // Button click handlers
    void OnStartGrowthPhaseClicked() {
        AutoReturnSeedFromEditorSlot();
        runManager?.StartGrowthAndThreatPhase();
    }
    
    void OnStartNewPlanningPhaseClicked() {
        runManager?.StartNewPlanningPhase();
    }
    
    void OnEndPlanningPhaseClicked() {
        AutoReturnSeedFromEditorSlot();
        runManager?.EndPlanningPhase();
    }
    
    void OnAdvanceTickClicked() {
        tickManager?.DebugAdvanceTick();
    }
    
    // Helper methods
    IEnumerator ShowInventoryBarDelayed() {
        yield return null; // Wait one frame
        InventoryBarController.Instance?.ShowBar();
    }
    
    void AutoReturnSeedFromEditorSlot() {
        if (NodeEditorGridController.Instance == null || InventoryGridController.Instance == null) return;
        
        var editor = NodeEditorGridController.Instance;
        var seedCell = editor.SeedSlotCell;
        
        if (seedCell == null || !seedCell.HasItem()) return;
        
        editor.RefreshGraphAndUpdateSeed();
        
        ItemView seedView = seedCell.GetItemView();
        NodeData seedData = seedCell.GetNodeData();
        if (seedView == null || seedData == null) return;
        
        editor.UnloadSeedFromSlot();
        
        InventoryGridController.Instance.ReturnGeneToInventory(seedView, seedData);
        
        seedCell.ClearNodeReference();
        Debug.Log($"[UIManager] Auto-returned seed \"{seedData.nodeDisplayName}\" to inventory.");
    }
    
    // Notification system
    public void ShowNotification(string message, float duration = 3f) {
        StartCoroutine(ShowNotificationCoroutine(message, duration));
    }
    
    IEnumerator ShowNotificationCoroutine(string message, float duration) {
        GameObject notification = new GameObject("Notification");
        notification.transform.SetParent(transform, false);
        
        var canvasGroup = notification.AddComponent<CanvasGroup>();
        var rectTransform = notification.AddComponent<RectTransform>();
        var image = notification.AddComponent<Image>();
        var text = notification.AddComponent<TextMeshProUGUI>();
        
        rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
        rectTransform.sizeDelta = new Vector2(300, 60);
        
        image.color = new Color(0, 0, 0, 0.8f);
        text.text = message;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 16;
        
        // Fade in
        float elapsedTime = 0f;
        while (elapsedTime < 0.5f) {
            canvasGroup.alpha = elapsedTime / 0.5f;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        yield return new WaitForSeconds(duration - 1f);
        
        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < 0.5f) {
            canvasGroup.alpha = 1f - (elapsedTime / 0.5f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Destroy(notification);
    }
    
    // Debug shortcut handling - kept in Update for input only
    void Update() {
        // Only handle debug shortcuts here
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (runManager?.CurrentPhase == GamePhase.Planning) {
                OnEndPlanningPhaseClicked();
            }
            else {
                Debug.Log("[UIManager] Time only advances through player actions!");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R) && (Application.isEditor || Debug.isDebugBuild)) {
            runManager?.ForcePhase(GamePhase.Planning);
        }
    }
}