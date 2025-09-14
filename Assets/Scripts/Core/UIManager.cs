using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using WegoSystem;
using Abracodabra.UI.Genes;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; set; }

    [Header("Panels")]
    [SerializeField] private GameObject uiCanvasRoot;
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject growthAndThreatPanel;
    [SerializeField] private GameObject geneSequenceUIPanel;
    [SerializeField] private GameObject gameOverPanel; // NEW: Assign your Game Over UI panel here

    [Header("Buttons")]
    [SerializeField] private Button startGrowthPhaseButton;
    [SerializeField] private Button startNewPlanningPhaseButton;
    [SerializeField] private Button endPlanningPhaseButton;
    [SerializeField] private Button advanceTickButton;
    [SerializeField] private Button restartButton; // NEW: Assign your Restart button from the GameOver panel

    [Header("Text Displays")]
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

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        HandleRunStateChanged(runManager.CurrentState);
        UpdateTickDisplay();
    }

    private void SetupButtons()
    {
        startGrowthPhaseButton?.onClick.AddListener(OnStartGrowthPhaseClicked);
        startNewPlanningPhaseButton?.onClick.AddListener(OnStartNewPlanningPhaseClicked);
        endPlanningPhaseButton?.onClick.AddListener(OnEndPlanningPhaseClicked);
        advanceTickButton?.onClick.AddListener(OnAdvanceTickClicked);
        restartButton?.onClick.AddListener(OnRestartClicked); // NEW
    }

    private void HandleRunStateChanged(RunState newState)
    {
        // Hide all major gameplay panels by default
        if (planningPanel != null) planningPanel.SetActive(false);
        if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (geneSequenceUIPanel != null) geneSequenceUIPanel.SetActive(false);
        if (InventoryGridController.Instance != null) InventoryGridController.Instance.gameObject.SetActive(false);
        InventoryBarController.Instance?.HideBar();


        // Now, show the correct panels for the current state
        switch (newState)
        {
            case RunState.Planning:
                if (planningPanel != null) planningPanel.SetActive(true);
                if (geneSequenceUIPanel != null) geneSequenceUIPanel.SetActive(true);
                if (InventoryGridController.Instance != null) InventoryGridController.Instance.gameObject.SetActive(true);
                break;

            case RunState.GrowthAndThreat:
                if (growthAndThreatPanel != null) growthAndThreatPanel.SetActive(true);
                if (geneSequenceUIPanel != null)
                {
                    geneSequenceUIPanel.GetComponent<GeneSequenceUI>()?.CleanupOnPhaseEnd();
                }
                StartCoroutine(ShowInventoryBarDelayed());
                break;

            case RunState.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                break;
        }

        UpdateButtonStates(newState);
    }

    private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        UpdateButtonStates(runManager.CurrentState);
    }

    private void HandleRoundChanged(int newRound)
    {
        // Logic for round change if needed
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
    private void OnRestartClicked() { runManager?.RestartGame(); } // NEW

    private IEnumerator ShowInventoryBarDelayed()
    {
        yield return null; // Wait one frame for layout to build
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