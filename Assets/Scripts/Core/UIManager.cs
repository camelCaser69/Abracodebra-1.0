// Assets\Scripts\Core\UIManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WegoSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; set; }

    #region Serialized Fields
    [Header("State Panels")]
    [SerializeField] GameObject planningPanel;
    [SerializeField] GameObject growthAndThreatPanel;
    [SerializeField] GameObject recoveryPanel;
    [SerializeField] GameObject nodeEditorPanel;

    [Header("Legacy State Buttons")]
    [SerializeField] Button startGrowthPhaseButton;
    [SerializeField] Button startRecoveryPhaseButton;
    [SerializeField] Button startNewPlanningPhaseButton;

    [Header("Wego Controls")]
    [SerializeField] GameObject wegoControlPanel;
    [SerializeField] Button endPlanningPhaseButton;
    [SerializeField] Toggle autoAdvanceToggle; // Obsolete but kept for reference
    [SerializeField] Slider tickSpeedSlider; // Obsolete but kept for reference
    [SerializeField] TextMeshProUGUI currentPhaseText;
    [SerializeField] TextMeshProUGUI tickCounterText;
    [SerializeField] TextMeshProUGUI phaseProgressText;
    [SerializeField] Button advanceTickButton;

    [Header("System Mode")]
    [SerializeField] Toggle wegoSystemToggle;
    [SerializeField] TextMeshProUGUI systemModeText;
    #endregion

    bool isWegoMode = true;

    #region Unity Lifecycle
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
            Debug.LogError("[UIManager] RunManager.Instance not found! UI will not fn correctly.");
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

    void Update()
    {
        if (!isWegoMode) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (TurnPhaseManager.Instance?.IsInPlanningPhase == true)
            {
                OnEndPlanningPhaseClicked();
            }
            else
            {
                Debug.Log("[UIManager] Time only advances through player actions!");
            }
        }

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
    #endregion

    #region Setup
    void SetupButtons()
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

    void SetupWegoControls()
    {
        if (wegoSystemToggle != null)
        {
            wegoSystemToggle.isOn = isWegoMode;
            wegoSystemToggle.onValueChanged.AddListener(OnWegoSystemToggled);
        }

        if (autoAdvanceToggle != null)
        {
            autoAdvanceToggle.gameObject.SetActive(false);
        }

        if (tickSpeedSlider != null)
        {
            tickSpeedSlider.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Event Handlers
    void HandleRunStateChanged(RunState newState)
    {
        if (planningPanel != null) planningPanel.SetActive(newState == RunState.Planning);
        if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(newState == RunState.GrowthAndThreat);

        if (recoveryPanel != null)
        {
            recoveryPanel.SetActive(false);
        }

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

    void HandleWegoPhaseChanged(TurnPhase oldPhase, TurnPhase newPhase)
    {
        UpdateWegoUI();
    }

    void HandleTickAdvanced(int currentTick)
    {
        UpdateWegoUI();
    }
    #endregion

    #region UI Update
    void UpdateButtonStates(RunState state)
    {
        if (startGrowthPhaseButton != null)
        {
            startGrowthPhaseButton.interactable = (state == RunState.Planning);
        }

        if (startRecoveryPhaseButton != null)
        {
            startRecoveryPhaseButton.gameObject.SetActive(false);
        }

        if (startNewPlanningPhaseButton != null)
        {
            startNewPlanningPhaseButton.interactable = (state == RunState.GrowthAndThreat);
        }

        if (endPlanningPhaseButton != null && TurnPhaseManager.Instance != null)
        {
            endPlanningPhaseButton.interactable = TurnPhaseManager.Instance.IsInPlanningPhase;
        }

        if (advanceTickButton != null && TurnPhaseManager.Instance != null)
        {
            advanceTickButton.interactable = !TurnPhaseManager.Instance.IsInPlanningPhase;
        }
    }

    public void SetWegoMovement(bool enabled)
    {
        // This method is obsolete as AnimalController now handles its own movement system internally.
        // It's kept here to prevent breaking any potential remaining references, but it does nothing.
        Debug.Log($"[UIManager] SetWegoMovement called with: {enabled}. This method is now obsolete.");
    }

    void UpdateWegoUI()
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
            int currentPhaseTicks = TurnPhaseManager.Instance.CurrentPhaseTicks;

            phaseProgressText.text = $"Phase Ticks: {currentPhaseTicks}";
        }

        UpdateButtonStates(RunManager.Instance?.CurrentState ?? RunState.Planning);
    }

    IEnumerator ShowInventoryBarDelayed()
    {
        yield return null; // Wait one frame
        InventoryBarController.Instance?.ShowBar();
    }
    #endregion

    #region Logic
    void AutoReturnSeedFromEditorSlot()
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
    #endregion

    #region Button Callbacks
    void OnStartGrowthPhaseClicked()
    {
        AutoReturnSeedFromEditorSlot();
        RunManager.Instance?.StartGrowthAndThreatPhase();
    }

    void OnStartRecoveryPhaseClicked()
    {
        RunManager.Instance?.StartRecoveryPhase();
    }

    void OnStartNewPlanningPhaseClicked()
    {
        RunManager.Instance?.StartNewPlanningPhase();
    }

    void OnEndPlanningPhaseClicked()
    {
        if (TurnPhaseManager.Instance != null)
        {
            AutoReturnSeedFromEditorSlot();
            TurnPhaseManager.Instance.EndPlanningPhase();
        }
    }

    void OnAdvanceTickClicked()
    {
        TickManager.Instance?.DebugAdvanceTick();
    }

    void OnWegoSystemToggled(bool enabled)
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
        
        // This is now obsolete; animal controllers manage their own state.
        // var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        // foreach (var animal in animals)
        // {
        //     animal.SetWegoMovement(enabled);
        // }

        UpdateWegoUI();

        Debug.Log($"[UIManager] Switched to {(enabled ? "Wego" : "Real-Time")} mode");
    }
    #endregion

    #region Public Methods & Debug
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

    IEnumerator ShowNotificationCoroutine(string message, float duration)
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