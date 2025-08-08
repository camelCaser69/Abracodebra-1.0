// Reworked File: Assets/Scripts/Core/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using WegoSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Phase Panels")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject growthAndThreatPanel;
    [SerializeField] private GameObject geneSequenceUIPanel; // Reworked reference

    [Header("Buttons")]
    [SerializeField] private Button startGrowthPhaseButton;
    [SerializeField] private Button startNewPlanningPhaseButton;
    [SerializeField] private Button endPlanningPhaseButton;
    [SerializeField] private Button advanceTickButton;

    [Header("WE-GO Display")]
    [SerializeField] private GameObject wegoControlPanel;
    [SerializeField] private TextMeshProUGUI currentPhaseText;
    [SerializeField] private TextMeshProUGUI tickCounterText;
    [SerializeField] private TextMeshProUGUI persistentTickCounterText;
    [SerializeField] private TextMeshProUGUI phaseProgressText;

    private RunManager runManager;
    private TickManager tickManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
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
            Debug.LogError("[UIManager] RunManager.Instance not found! UI will not function correctly.");
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

    void SetupButtons()
    {
        startGrowthPhaseButton?.onClick.AddListener(OnStartGrowthPhaseClicked);
        startNewPlanningPhaseButton?.onClick.AddListener(OnStartNewPlanningPhaseClicked);
        endPlanningPhaseButton?.onClick.AddListener(OnEndPlanningPhaseClicked);
        advanceTickButton?.onClick.AddListener(OnAdvanceTickClicked);
    }

    void HandleRunStateChanged(RunState newState)
    {
        if (planningPanel != null)
            planningPanel.SetActive(newState == RunState.Planning);

        if (growthAndThreatPanel != null)
            growthAndThreatPanel.SetActive(newState == RunState.GrowthAndThreat);
        
        // The gene editor is now part of the planning phase UI
        if (geneSequenceUIPanel != null)
            geneSequenceUIPanel.SetActive(newState == RunState.Planning);

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
            InventoryBarController.Instance?.HideBar();
        }

        UpdateButtonStates(newState);
    }

    private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        UpdatePhaseDisplay();
        UpdateButtonStates(runManager.CurrentState);
    }

    private void HandleRoundChanged(int newRound) { }

    private void HandleTickAdvanced(int currentTick)
    {
        UpdateTickDisplay();
        UpdatePhaseProgressDisplay();
    }

    void UpdatePhaseDisplay()
    {
        if (currentPhaseText != null && runManager != null)
        {
            currentPhaseText.text = $"Phase: {runManager.CurrentPhase}";
        }
    }

    void UpdateTickDisplay()
    {
        if (tickManager == null) return;
        string tickInfo = $"Tick: {tickManager.CurrentTick}";
        if (tickCounterText != null) tickCounterText.text = tickInfo;
        if (persistentTickCounterText != null) persistentTickCounterText.text = tickInfo;
    }

    void UpdatePhaseProgressDisplay()
    {
        if (phaseProgressText != null && runManager != null)
        {
            phaseProgressText.text = $"Phase Ticks: {runManager.CurrentPhaseTicks}";
        }
    }

    void UpdateButtonStates(RunState state)
    {
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

    void OnStartGrowthPhaseClicked()
    {
        runManager?.StartGrowthAndThreatPhase();
    }

    void OnStartNewPlanningPhaseClicked()
    {
        runManager?.StartNewPlanningPhase();
    }

    void OnEndPlanningPhaseClicked()
    {
        runManager?.EndPlanningPhase();
    }

    void OnAdvanceTickClicked()
    {
        tickManager?.DebugAdvanceTick();
    }

    private IEnumerator ShowInventoryBarDelayed()
    {
        yield return null; // Wait one frame for everything to initialize
        InventoryBarController.Instance?.ShowBar();
    }
    
    // AutoReturnSeedFromEditorSlot is removed as the concept no longer exists.
    
    // Notification system remains unchanged
    public void ShowNotification(string message, float duration = 3f)
    {
        StartCoroutine(ShowNotificationCoroutine(message, duration));
    }

    private IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        // This implementation remains the same
        GameObject notification = new GameObject("Notification");
        notification.transform.SetParent(transform, false);

        var canvasGroup = notification.AddComponent<CanvasGroup>();
        var rectTransform = notification.AddComponent<RectTransform>();
        var image = notification.AddComponent<Image>();
        var text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(notification.transform, false);

        rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
        rectTransform.sizeDelta = new Vector2(300, 60);
        
        image.color = new Color(0, 0, 0, 0.8f);
        text.text = message;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 16;
        text.rectTransform.sizeDelta = rectTransform.sizeDelta;

        float elapsedTime = 0f;
        while (elapsedTime < 0.5f) {
            canvasGroup.alpha = elapsedTime / 0.5f;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration - 1f);

        elapsedTime = 0f;
        while (elapsedTime < 0.5f) {
            canvasGroup.alpha = 1f - (elapsedTime / 0.5f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(notification);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (runManager?.CurrentPhase == GamePhase.Planning)
            {
                OnEndPlanningPhaseClicked();
            }
            else
            {
                Debug.Log("[UIManager] Time only advances through player actions!");
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && (Application.isEditor || Debug.isDebugBuild))
        {
            runManager?.ForcePhase(GamePhase.Planning);
        }
    }
}