using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WegoSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] GameObject planningPanel;
    [SerializeField] GameObject growthAndThreatPanel;
    [SerializeField] GameObject recoveryPanel;
    [SerializeField] GameObject nodeEditorPanel;

    [Header("Phase Transition Buttons")]
    [SerializeField] Button startGrowthPhaseButton;
    [SerializeField] Button startRecoveryPhaseButton;
    [SerializeField] Button startNewPlanningPhaseButton;

    [Header("Wego Control Panel")]
    [SerializeField] GameObject wegoControlPanel;
    [SerializeField] Button endPlanningPhaseButton;
    [SerializeField] TextMeshProUGUI currentPhaseText;
    [SerializeField] TextMeshProUGUI tickCounterText;
    [SerializeField] TextMeshProUGUI phaseProgressText;
    [SerializeField] Button advanceTickButton;

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

        // Update UI every 10 frames to reduce overhead
        if (Time.frameCount % 10 == 0)
        {
            UpdateWegoUI();
        }
    }

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

    void HandleRunStateChanged(RunState newState)
    {
        if (planningPanel != null) 
            planningPanel.SetActive(newState == RunState.Planning);
        if (growthAndThreatPanel != null) 
            growthAndThreatPanel.SetActive(newState == RunState.GrowthAndThreat);

        if (recoveryPanel != null)
        {
            recoveryPanel.SetActive(false);
        }

        if (nodeEditorPanel != null) 
            nodeEditorPanel.SetActive(newState == RunState.Planning);

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

    void UpdateWegoUI()
    {
        if (wegoControlPanel != null)
        {
            wegoControlPanel.SetActive(true);
        }

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

    public void DebugAdvanceMultipleTicks(int count)
    {
        if (Application.isEditor || Debug.isDebugBuild)
        {
            TickManager.Instance?.AdvanceMultipleTicks(count);
        }
    }
}