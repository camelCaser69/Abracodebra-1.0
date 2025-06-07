// FILE: Assets/Scripts/Tiles/Tools/ToolSwitcher.cs
using UnityEngine;
using System;

public class ToolSwitcher : MonoBehaviour
{
    [Tooltip("All available tool definitions, e.g. Hoe, WateringCan, etc.")]
    public ToolDefinition[] toolDefinitions;

    private int currentIndex = 0;

    // --- Public Properties ---
    public ToolDefinition CurrentTool { get; private set; } = null;
    /// <summary>
    /// Gets the remaining uses for the current tool. Returns -1 if the tool has unlimited uses.
    /// </summary>
    public int CurrentRemainingUses { get; private set; } = -1; // <<< NEW: Track remaining uses (-1 for unlimited)

    // --- Events ---
    public event Action<ToolDefinition> OnToolChanged;
    /// <summary>
    /// Event fired when the remaining uses of the current tool changes. Passes the new remaining count (-1 for unlimited).
    /// </summary>
    public event Action<int> OnUsesChanged; // <<< NEW EVENT for UI updates

    private void Awake()
    {
        // Debug logs from previous step can be kept or removed
        // Debug.Log("[ToolSwitcher Awake] Initializing...");
    }

    private void Start()
    {
        InitializeToolState(true); // Initialize and fire events
    }

    private void Update()
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
            // Ensure the new index points to a valid definition before updating state
            if (currentIndex >= 0 && currentIndex < toolDefinitions.Length && toolDefinitions[currentIndex] != null)
            {
                InitializeToolState(false); // Update state for the new tool and fire events
            }
            else
            {
                 Debug.LogError($"[ToolSwitcher Update] Attempted to switch to an invalid/NULL tool definition at index {currentIndex}. Reverting.");
                 currentIndex = previousIndex; // Revert to the last valid index
                 // No state change, no events needed here
            }
        }
    }

    /// <summary>
    /// Sets the CurrentTool and resets CurrentRemainingUses based on the tool's definition.
    /// Optionally fires OnToolChanged and OnUsesChanged events.
    /// </summary>
    /// <param name="isInitialSetup">If true, forces event firing even if tool doesn't technically change.</param>
    private void InitializeToolState(bool isInitialSetup)
    {
        ToolDefinition previousTool = CurrentTool; // Store previous tool for change check
        int previousUses = CurrentRemainingUses; // Store previous uses

        if (toolDefinitions == null || toolDefinitions.Length == 0 || currentIndex < 0 || currentIndex >= toolDefinitions.Length || toolDefinitions[currentIndex] == null)
        {
            // Handle cases with no tools or invalid selection
            CurrentTool = null;
            CurrentRemainingUses = -1; // No tool = unlimited uses conceptually
        }
        else
        {
            // Valid tool selected
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

        // Fire events if state actually changed or if it's the initial setup
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
    
    // Add this method to ToolSwitcher to allow external tool selection
    public void SelectToolByDefinition(ToolDefinition toolDef)
    {
        if (toolDef == null || toolDefinitions == null) return;
    
        // Find the index of this tool definition
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


    /// <summary>
    /// Refills the current tool to its maximum capacity if it's a limited-use tool.
    /// </summary>
    public void RefillCurrentTool() // <<< NEW METHOD
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

        // Check if already full to avoid unnecessary event firing
        if (CurrentRemainingUses == CurrentTool.initialUses)
        {
            if(Debug.isDebugBuild) Debug.Log($"[ToolSwitcher RefillCurrentTool] Tool '{CurrentTool.displayName}' is already full ({CurrentRemainingUses} uses).");
            return;
        }

        // Set uses back to initial amount
        int previousUses = CurrentRemainingUses;
        CurrentRemainingUses = CurrentTool.initialUses;

        Debug.Log($"[ToolSwitcher RefillCurrentTool] Refilled tool '{CurrentTool.displayName}' to {CurrentRemainingUses} uses (was {previousUses}).");

        // Notify listeners that uses changed
        OnUsesChanged?.Invoke(CurrentRemainingUses);
    }
    
    /// <summary>
    /// Attempts to consume one use of the current tool.
    /// </summary>
    /// <returns>True if a use was consumed or if the tool has unlimited uses. False if the tool has limited uses and is out of uses.</returns>
    public bool TryConsumeUse()
    {
        if (CurrentTool == null)
        {
            Debug.LogWarning("[ToolSwitcher TryConsumeUse] Cannot consume use: No tool selected.");
            return false; // Cannot use a non-existent tool
        }

        if (!CurrentTool.limitedUses || CurrentRemainingUses == -1)
        {
            // Tool is unlimited, consumption always succeeds
            // Debug.Log($"[ToolSwitcher TryConsumeUse] Tool '{CurrentTool.displayName}' has unlimited uses."); // Optional log
            return true;
        }

        // Tool has limited uses
        if (CurrentRemainingUses > 0)
        {
            CurrentRemainingUses--;
            Debug.Log($"[ToolSwitcher TryConsumeUse] Consumed use for '{CurrentTool.displayName}'. Remaining: {CurrentRemainingUses}");
            OnUsesChanged?.Invoke(CurrentRemainingUses); // Notify listeners
            return true;
        }
        else
        {
            // Out of uses
            Debug.Log($"[ToolSwitcher TryConsumeUse] Tool '{CurrentTool.displayName}' is out of uses (0 remaining).");
            // Optionally play an 'empty click' sound here
            return false;
        }
    }

    // Added prefix parameter for context
    private void LogToolChange(string prefix = "[ToolSwitcher]")
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
            // usesSuffix = " (Unlimited)"; // Optional: Indicate unlimited
        }

        Debug.Log($"{prefix} Switched tool to: {toolName}{usesSuffix} (Index: {currentIndex})");
    }
}