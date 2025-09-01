using UnityEngine;
using UnityEngine.UI; // For Button
using System.Collections;
using TMPro;
using WegoSystem;
using Abracodabra.UI.Genes;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject uiCanvasRoot;
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject growthAndThreatPanel;
    [SerializeField] private GameObject geneSequenceUIPanel;

    [Header("Buttons")]
    [SerializeField] private Button startGrowthPhaseButton;
    [SerializeField] private Button startNewPlanningPhaseButton;
    [SerializeField] private Button endPlanningPhaseButton;
    [SerializeField] private Button advanceTickButton;
    
    [Header("Displays")]
    [SerializeField] private TextMeshProUGUI tickCounterText;

    private RunManager runManager;
    private TickManager tickManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (uiCanvasRoot != null)
        {
            uiCanvasRoot.SetActive(true);
            Debug.Log($"[UIManager] Activated main UI canvas '{uiCanvasRoot.name}'.");
        }
        else
        {
            Debug.LogWarning("[UIManager] UI Canvas Root is not assigned. UI may not appear.", this);
        }
    }

    private void OnDestroy()
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (runManager?.CurrentPhase == GamePhase.Planning)
            {
                OnEndPlanningPhaseClicked();
            }
        }
        if (Input.GetKeyDown(KeyCode.R) && (Application.isEditor || Debug.isDebugBuild))
        {
            runManager?.ForcePhase(GamePhase.Planning);
        }
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
        UpdateTickDisplay();
    }

    private void SetupButtons()
    {
        startGrowthPhaseButton?.onClick.AddListener(OnStartGrowthPhaseClicked);
        startNewPlanningPhaseButton?.onClick.AddListener(OnStartNewPlanningPhaseClicked);
        endPlanningPhaseButton?.onClick.AddListener(OnEndPlanningPhaseClicked);
        advanceTickButton?.onClick.AddListener(OnAdvanceTickClicked);
    }

    private void HandleRunStateChanged(RunState newState)
    {
        if (planningPanel != null) planningPanel.SetActive(newState == RunState.Planning);
        if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(newState == RunState.GrowthAndThreat);

        if (newState == RunState.GrowthAndThreat && geneSequenceUIPanel != null)
        {
            var geneSequenceUI = geneSequenceUIPanel.GetComponent<GeneSequenceUI>();
            if (geneSequenceUI != null)
            {
                geneSequenceUI.CleanupOnPhaseEnd();
            }
        }

        if (geneSequenceUIPanel != null) geneSequenceUIPanel.SetActive(newState == RunState.Planning);

        if (InventoryGridController.Instance != null)
        {
            InventoryGridController.Instance.gameObject.SetActive(newState == RunState.Planning);
        }

        if (newState == RunState.GrowthAndThreat)
        {
            if (InventoryBarController.Instance != null)
            {
                StartCoroutine(ShowInventoryBarDelayed());
            }
        }
        else
        {
            InventoryBarController.Instance?.HideBar();
        }
        UpdateButtonStates(newState);
    }

    private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        UpdateButtonStates(runManager.CurrentState);
    }

    private void HandleRoundChanged(int newRound)
    {
        // Logic for round change can go here if needed in the future
    }

    private void HandleTickAdvanced(int currentTick)
    {
        UpdateTickDisplay();
    }

    private void UpdateTickDisplay()
    {
        if (tickCounterText != null && tickManager != null)
        {
            tickCounterText.text = $"Tick: {tickManager.CurrentTick}";
        }
    }

    private void UpdateButtonStates(RunState state)
    {
        bool isPlanning = (state == RunState.Planning);
        bool isPlanningPhase = (runManager?.CurrentPhase == GamePhase.Planning);

        if (startGrowthPhaseButton != null) startGrowthPhaseButton.interactable = isPlanning && isPlanningPhase;
        if (startNewPlanningPhaseButton != null) startNewPlanningPhaseButton.interactable = !isPlanning;
        if (endPlanningPhaseButton != null) endPlanningPhaseButton.interactable = isPlanning && isPlanningPhase;
        if (advanceTickButton != null) advanceTickButton.interactable = !isPlanningPhase;
    }

    private void OnStartGrowthPhaseClicked() { runManager?.StartGrowthAndThreatPhase(); }
    private void OnStartNewPlanningPhaseClicked() { runManager?.StartNewPlanningPhase(); }
    private void OnEndPlanningPhaseClicked() { runManager?.EndPlanningPhase(); }
    private void OnAdvanceTickClicked() { tickManager?.DebugAdvanceTick(); }

    private IEnumerator ShowInventoryBarDelayed()
    {
        yield return null;
        InventoryBarController.Instance?.ShowBar();
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
}