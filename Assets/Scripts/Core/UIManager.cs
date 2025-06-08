using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WegoSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; set; }

    #region Panel References
    [Header("State Panels")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject growthAndThreatPanel;
    [SerializeField] private GameObject recoveryPanel;
    [SerializeField] private GameObject nodeEditorPanel;
    #endregion

    #region Real-Time Control References
    [Header("Real-Time Controls")]
    [SerializeField] private Button startGrowthPhaseButton;
    [SerializeField] private Button startRecoveryPhaseButton;
    [SerializeField] private Button startNewPlanningPhaseButton;
    #endregion

    #region Wego Control References
    [Header("Wego Controls")]
    [SerializeField] private GameObject wegoControlPanel;
    [SerializeField] private Button endPlanningPhaseButton;
    [SerializeField] private Toggle autoAdvanceToggle; // Obsolete but kept for reference
    [SerializeField] private Slider tickSpeedSlider; // Obsolete but kept for reference
    [SerializeField] private TextMeshProUGUI currentPhaseText;
    [SerializeField] private TextMeshProUGUI tickCounterText;
    [SerializeField] private TextMeshProUGUI phaseProgressText;
    [SerializeField] private Button advanceTickButton;
    #endregion

    #region System Mode References
    [Header("System Mode")]
    [SerializeField] private Toggle wegoSystemToggle;
    [SerializeField] private TextMeshProUGUI systemModeText;
    #endregion

    private bool isWegoMode = true;

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

        if (TurnPhaseManager.Instance != null)
        {
            TurnPhaseManager.Instance.OnPhaseChanged += HandleWegoPhaseChanged;
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnTickAdvanced += HandleTickAdvanced;
        }

        SetupButtons();
        SetupWegoControls();
        HandleRunStateChanged(RunManager.Instance.CurrentState);
        UpdateWegoUI();
    }

    void OnDestroy()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged -= HandleRunStateChanged;
        }

        if (TurnPhaseManager.Instance != null)
        {
            TurnPhaseManager.Instance.OnPhaseChanged -= HandleWegoPhaseChanged;
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnTickAdvanced -= HandleTickAdvanced;
        }
    }

    private void SetupButtons()
    {
        if (startGrowthPhaseButton != null)
            startGrowthPhaseButton.onClick.AddListener(OnStartGrowthPhaseClicked);
        if (startRecoveryPhaseButton != null)
            startRecoveryPhaseButton.onClick.AddListener(OnStartRecoveryPhaseClicked);
        if (startNewPlanningPhaseButton != null)
            startNewPlanningPhaseButton.onClick.AddListener(OnStartNewPlanningPhaseClicked);

        if (endPlanningPhaseButton != null)
            endPlanningPhaseButton.onClick.AddListener(OnEndPlanningPhaseClicked);
        if (advanceTickButton != null)
            advanceTickButton.onClick.AddListener(OnAdvanceTickClicked);
    }

    private void SetupWegoControls()
    {
        if (wegoSystemToggle != null)
        {
            wegoSystemToggle.isOn = isWegoMode;
            wegoSystemToggle.onValueChanged.AddListener(OnWegoSystemToggled);
        }

        // Auto-advance and tick speed are obsolete with a player-action-driven tick system.
        // The controls are hidden to prevent confusion.
        if (autoAdvanceToggle != null)
        {
            autoAdvanceToggle.gameObject.SetActive(false);
        }

        if (tickSpeedSlider != null)
        {
            tickSpeedSlider.gameObject.SetActive(false);
        }
    }

    private void HandleRunStateChanged(RunState newState)
    {
        if (planningPanel != null) planningPanel.SetActive(newState == RunState.Planning);
        if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(newState == RunState.GrowthAndThreat);
        if (recoveryPanel != null) recoveryPanel.SetActive(newState == RunState.Recovery);

        if (nodeEditorPanel != null) nodeEditorPanel.SetActive(newState == RunState.Planning);

        if (InventoryGridController.Instance != null)
        {
            InventoryGridController.Instance.gameObject.SetActive(newState == RunState.Planning);
        }

        if (newState == RunState.GrowthAndThreat)
        {
            if (InventoryBarController.Instance != null)
                StartCoroutine(ShowInventoryBarDelayed());
        }
        else
        {
            if (InventoryBarController.Instance != null)
                InventoryBarController.Instance.HideBar();
        }

        UpdateButtonStates(newState);
        UpdateWegoUI();
    }

    private void HandleWegoPhaseChanged(TurnPhase oldPhase, TurnPhase newPhase)
    {
        UpdateWegoUI();
    }

    private void HandleTickAdvanced(int currentTick)
    {
        UpdateWegoUI();
    }

    private void UpdateButtonStates(RunState state)
    {
        if (startGrowthPhaseButton != null)
        {
            startGrowthPhaseButton.interactable = (state == RunState.Planning);
        }

        if (startRecoveryPhaseButton != null)
        {
            startRecoveryPhaseButton.interactable = (state == RunState.GrowthAndThreat);
        }

        if (startNewPlanningPhaseButton != null)
        {
            startNewPlanningPhaseButton.interactable = (state == RunState.Recovery);
        }

        if (endPlanningPhaseButton != null && TurnPhaseManager.Instance != null)
        {
            endPlanningPhaseButton.interactable = TurnPhaseManager.Instance.IsInPlanningPhase;
        }

        if (advanceTickButton != null && TurnPhaseManager.Instance != null)
        {
            // Button is for a manual "Wait" or debug tick advance.
            // Should be disabled during the planning phase.
            advanceTickButton.interactable = !TurnPhaseManager.Instance.IsInPlanningPhase;
        }
    }

    private void UpdateWegoUI()
    {
        if (systemModeText != null)
        {
            systemModeText.text = isWegoMode ? "Wego Mode" : "Real-Time Mode";
        }

        if (wegoControlPanel != null)
        {
            wegoControlPanel.SetActive(isWegoMode);
        }

        if (!isWegoMode) return;

        if (currentPhaseText != null && TurnPhaseManager.Instance != null)
        {
            TurnPhase currentPhase = TurnPhaseManager.Instance.CurrentPhase;
            currentPhaseText.text = $"Phase: {currentPhase}";
        }

        if (tickCounterText != null && TickManager.Instance != null)
        {
            tickCounterText.text = $"Tick: {TickManager.Instance.CurrentTick}";
        }

        if (phaseProgressText != null && TurnPhaseManager.Instance != null)
        {
            int remainingTicks = TurnPhaseManager.Instance.GetRemainingPhaseTicks();
            int currentPhaseTicks = TurnPhaseManager.Instance.CurrentPhaseTicks;

            if (remainingTicks >= 0)
            {
                phaseProgressText.text = $"Phase Progress: {currentPhaseTicks} (Remaining: {remainingTicks})";
            }
            else
            {
                phaseProgressText.text = $"Phase Progress: {currentPhaseTicks} (Unlimited)";
            }
        }

        // The autoAdvanceToggle is now obsolete and hidden. This logic is no longer needed.
        // if (autoAdvanceToggle != null && TickManager.Instance != null)
        // {
        //    autoAdvanceToggle.SetIsOnWithoutNotify(false);
        // }

        UpdateButtonStates(RunManager.Instance?.CurrentState ?? RunState.Planning);
    }

    private IEnumerator ShowInventoryBarDelayed()
    {
        yield return null; // Wait one frame
        InventoryBarController.Instance?.ShowBar();
    }

    private void AutoReturnSeedFromEditorSlot()
    {
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

    private void OnEndPlanningPhaseClicked()
    {
        if (TurnPhaseManager.Instance != null)
        {
            AutoReturnSeedFromEditorSlot();
            TurnPhaseManager.Instance.EndPlanningPhase();
        }
    }

    private void OnAdvanceTickClicked()
    {
        TickManager.Instance?.DebugAdvanceTick();
    }

    private void OnWegoSystemToggled(bool enabled)
    {
        isWegoMode = enabled;

        if (RunManager.Instance != null)
        {
            RunManager.Instance.SetWegoSystem(enabled);
        }

        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.SetWegoSystem(enabled);
        }

        foreach (var plant in PlantGrowth.AllActivePlants)
        {
            if (plant != null)
            {
                plant.SetWegoSystem(enabled);
            }
        }

        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        foreach (var animal in animals)
        {
            animal.SetWegoMovement(enabled);
        }

        var gardener = FindFirstObjectByType<GardenerController>();
        if (gardener != null)
        {
            gardener.SetWegoMovement(enabled);
        }

        UpdateWegoUI();

        Debug.Log($"[UIManager] Switched to {(enabled ? "Wego" : "Real-Time")} mode");
    }

    // This functionality is obsolete in a player-action-driven tick system.
    // The TickManager no longer has `StartTicking`, `StopTicking`, or `IsRunning`.
    // The method is kept here commented out for reference but is no longer used.
    /*
    private void OnAutoAdvanceToggled(bool enabled)
    {
        // if (TickManager.Instance != null)
        // {
        //     if (enabled)
        //     {
        //         TickManager.Instance.StartTicking();
        //     }
        //     else
        //     {
        //         TickManager.Instance.StopTicking();
        //     }
        // }
    }
    */

    // This functionality is obsolete as tick speed is no longer relevant.
    // The method is kept here commented out for reference but is no longer used.
    /*
    private void OnTickSpeedChanged(float value)
    {
        // TickManager.Instance?.SetTickSpeed(value);
        //
        // var sliderLabel = tickSpeedSlider?.GetComponentInChildren<TextMeshProUGUI>();
        // if (sliderLabel != null)
        // {
        //     sliderLabel.text = $"Speed: {value:F1}x";
        // }
    }
    */

    void Update()
    {
        if (!isWegoMode) return;

        // Spacebar ends planning phase or advances a single tick (acts as a "Wait" action).
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (TurnPhaseManager.Instance?.IsInPlanningPhase == true)
            {
                OnEndPlanningPhaseClicked();
            }
            else
            {
                OnAdvanceTickClicked();
            }
        }

        // The 'P' hotkey for toggling auto-advance is now obsolete.
        // if (Input.GetKeyDown(KeyCode.P))
        // {
        //     if (autoAdvanceToggle != null)
        //     {
        //         autoAdvanceToggle.isOn = !autoAdvanceToggle.isOn;
        //     }
        // }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                TurnPhaseManager.Instance?.ForcePhase(TurnPhase.Planning);
            }
        }

        if (Time.frameCount % 10 == 0) // Update every 10 frames to reduce overhead
        {
            UpdateWegoUI();
        }
    }

    public void SetWegoMode(bool enabled)
    {
        if (wegoSystemToggle != null)
        {
            wegoSystemToggle.isOn = enabled;
        }
        else
        {
            OnWegoSystemToggled(enabled);
        }
    }

    public bool IsWegoMode()
    {
        return isWegoMode;
    }

    public void ShowNotification(string message, float duration = 3f)
    {
        StartCoroutine(ShowNotificationCoroutine(message, duration));
    }

    private IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
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

        float elapsedTime = 0f;
        while (elapsedTime < 0.5f)
        {
            canvasGroup.alpha = elapsedTime / 0.5f;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration - 1f);

        elapsedTime = 0f;
        while (elapsedTime < 0.5f)
        {
            canvasGroup.alpha = 1f - (elapsedTime / 0.5f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(notification);
    }

    #region Debug Methods
    public void DebugToggleWegoMode()
    {
        if (Application.isEditor || Debug.isDebugBuild)
        {
            SetWegoMode(!isWegoMode);
        }
    }

    public void DebugForcePhase(TurnPhase phase)
    {
        if (Application.isEditor || Debug.isDebugBuild)
        {
            TurnPhaseManager.Instance?.ForcePhase(phase);
        }
    }

    public void DebugAdvanceMultipleTicks(int count)
    {
        if (Application.isEditor || Debug.isDebugBuild)
        {
            TickManager.Instance?.AdvanceMultipleTicks(count);
        }
    }
    #endregion
}