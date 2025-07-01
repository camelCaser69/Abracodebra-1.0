using System;
using UnityEngine;

public class ToolSwitcher : MonoBehaviour
{
    public static ToolSwitcher Instance { get; private set; }

    public ToolDefinition[] toolDefinitions;

    int currentIndex = 0;

    public ToolDefinition CurrentTool { get; set; } = null;
    public int CurrentRemainingUses { get; set; } = -1; // -1 for unlimited

    public event Action<ToolDefinition> OnToolChanged;
    public event Action<int> OnUsesChanged;

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
        InitializeToolState(true); // Initialize and fire events
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (toolDefinitions == null || toolDefinitions.Length == 0) return;

        bool toolChanged = false;
        int previousIndex = currentIndex;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = toolDefinitions.Length - 1;
            toolChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentIndex++;
            if (currentIndex >= toolDefinitions.Length)
                currentIndex = 0;
            toolChanged = true;
        }

        if (toolChanged)
        {
            if (currentIndex >= 0 && currentIndex < toolDefinitions.Length && toolDefinitions[currentIndex] != null)
            {
                InitializeToolState(false); // Update state for the new tool and fire events
            }
            else
            {
                Debug.LogError($"[ToolSwitcher Update] Attempted to switch to an invalid/NULL tool definition at index {currentIndex}. Reverting.");
                currentIndex = previousIndex; // Revert to the last valid index
            }
        }
    }

    void InitializeToolState(bool isInitialSetup)
    {
        ToolDefinition previousTool = CurrentTool; // Store previous tool for change check
        int previousUses = CurrentRemainingUses; // Store previous uses

        if (toolDefinitions == null || toolDefinitions.Length == 0 || currentIndex < 0 || currentIndex >= toolDefinitions.Length || toolDefinitions[currentIndex] == null)
        {
            CurrentTool = null;
            CurrentRemainingUses = -1; // No tool = unlimited uses conceptually
        }
        else
        {
            CurrentTool = toolDefinitions[currentIndex];
            if (CurrentTool.limitedUses)
            {
                CurrentRemainingUses = CurrentTool.initialUses;
            }
            else
            {
                CurrentRemainingUses = -1; // Mark as unlimited
            }
        }

        LogToolChange("[ToolSwitcher InitializeToolState]"); // Log the state after update

        bool toolActuallyChanged = previousTool != CurrentTool;
        bool usesActuallyChanged = previousUses != CurrentRemainingUses;

        if (isInitialSetup || toolActuallyChanged)
        {
            Debug.Log($"[ToolSwitcher InitializeToolState] Firing OnToolChanged for tool: {CurrentTool?.displayName ?? "NULL"}");
            OnToolChanged?.Invoke(CurrentTool);
        }
        if (isInitialSetup || usesActuallyChanged || toolActuallyChanged) // Fire uses changed if tool changed too (to reset UI)
        {
            Debug.Log($"[ToolSwitcher InitializeToolState] Firing OnUsesChanged with value: {CurrentRemainingUses}");
            OnUsesChanged?.Invoke(CurrentRemainingUses);
        }
    }

    public void SelectToolByDefinition(ToolDefinition toolDef)
    {
        if (toolDef == null || toolDefinitions == null) return;

        for (int i = 0; i < toolDefinitions.Length; i++)
        {
            if (toolDefinitions[i] == toolDef)
            {
                currentIndex = i;
                InitializeToolState(false);
                Debug.Log($"[ToolSwitcher] Externally selected tool: {toolDef.displayName} at index {i}");
                return;
            }
        }

        Debug.LogWarning($"[ToolSwitcher] Tool '{toolDef.displayName}' not found in definitions array");
    }

    public void RefillCurrentTool()
    {
        if (CurrentTool == null)
        {
            Debug.LogWarning("[ToolSwitcher RefillCurrentTool] Cannot refill: No tool selected.");
            return;
        }

        if (!CurrentTool.limitedUses)
        {
            Debug.LogWarning($"[ToolSwitcher RefillCurrentTool] Cannot refill tool '{CurrentTool.displayName}': It has unlimited uses.");
            return;
        }

        if (CurrentRemainingUses == CurrentTool.initialUses)
        {
            if(Debug.isDebugBuild) Debug.Log($"[ToolSwitcher RefillCurrentTool] Tool '{CurrentTool.displayName}' is already full ({CurrentRemainingUses} uses).");
            return;
        }

        int previousUses = CurrentRemainingUses;
        CurrentRemainingUses = CurrentTool.initialUses;

        Debug.Log($"[ToolSwitcher RefillCurrentTool] Refilled tool '{CurrentTool.displayName}' to {CurrentRemainingUses} uses (was {previousUses}).");

        OnUsesChanged?.Invoke(CurrentRemainingUses);
    }

    public bool TryConsumeUse()
    {
        if (CurrentTool == null)
        {
            Debug.LogWarning("[ToolSwitcher TryConsumeUse] Cannot consume use: No tool selected.");
            return false; // Cannot use a non-existent tool
        }

        if (!CurrentTool.limitedUses || CurrentRemainingUses == -1)
        {
            return true;
        }

        if (CurrentRemainingUses > 0)
        {
            CurrentRemainingUses--;
            Debug.Log($"[ToolSwitcher TryConsumeUse] Consumed use for '{CurrentTool.displayName}'. Remaining: {CurrentRemainingUses}");
            OnUsesChanged?.Invoke(CurrentRemainingUses); // Notify listeners
            return true;
        }
        else
        {
            Debug.Log($"[ToolSwitcher TryConsumeUse] Tool '{CurrentTool.displayName}' is out of uses (0 remaining).");
            return false;
        }
    }

    void LogToolChange(string prefix = "[ToolSwitcher]")
    {
        string toolName = (CurrentTool != null && !string.IsNullOrEmpty(CurrentTool.displayName))
            ? CurrentTool.displayName
            : "(none)";
        string usesSuffix = "";
        if (CurrentTool != null && CurrentTool.limitedUses && CurrentRemainingUses >= 0)
        {
            usesSuffix = $" ({CurrentRemainingUses}/{CurrentTool.initialUses})";
        }
        else if (CurrentTool != null && !CurrentTool.limitedUses)
        {
            // No uses to show for unlimited tools
        }

        Debug.Log($"{prefix} Switched tool to: {toolName}{usesSuffix} (Index: {currentIndex})");
    }
}