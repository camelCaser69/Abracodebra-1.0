// FILE: Assets/Scripts/Tiles/Tools/ToolSwitcher.cs
using UnityEngine;
using System;

public class ToolSwitcher : MonoBehaviour
{
    [Tooltip("All available tool definitions, e.g. Hoe, WateringCan, etc.")]
    public ToolDefinition[] toolDefinitions;

    private int currentIndex = 0;

    public ToolDefinition CurrentTool { get; private set; } = null;
    public event Action<ToolDefinition> OnToolChanged;

    private void Awake()
    {
        Debug.Log("[ToolSwitcher Awake] Initializing...");
        if (toolDefinitions == null || toolDefinitions.Length == 0)
        {
            Debug.LogWarning("[ToolSwitcher Awake] Tool Definitions array is null or empty!");
        }
        else
        {
            Debug.Log($"[ToolSwitcher Awake] Found {toolDefinitions.Length} tool definitions.");
            // Check for nulls in the array
            for(int i=0; i < toolDefinitions.Length; i++)
            {
                if (toolDefinitions[i] == null)
                {
                     Debug.LogWarning($"[ToolSwitcher Awake] Tool definition at index {i} is NULL!");
                }
            }
        }
    }


    private void Start()
    {
        if (toolDefinitions != null && toolDefinitions.Length > 0)
        {
            // Check again for null at index 0 specifically before assigning
            if (toolDefinitions[0] != null)
            {
                currentIndex = 0;
                CurrentTool = toolDefinitions[currentIndex];
                LogToolChange("[ToolSwitcher Start - Initial Tool]");
                Debug.Log("[ToolSwitcher Start] Firing initial OnToolChanged event.");
                OnToolChanged?.Invoke(CurrentTool);
            }
            else
            {
                 Debug.LogError("[ToolSwitcher Start] Initial tool definition (index 0) is NULL. Cannot set initial tool.");
                 CurrentTool = null; // Explicitly set to null
                 LogToolChange("[ToolSwitcher Start - Initial Tool NULL]");
                 Debug.Log("[ToolSwitcher Start] Firing initial OnToolChanged event with NULL tool.");
                 OnToolChanged?.Invoke(CurrentTool); // Still invoke with null
            }
        }
        else
        {
            CurrentTool = null;
            LogToolChange("[ToolSwitcher Start - No Tools]");
            Debug.Log("[ToolSwitcher Start] No tools defined. Firing initial OnToolChanged event with NULL tool.");
            OnToolChanged?.Invoke(CurrentTool);
        }
    }

    private void Update()
    {
        if (toolDefinitions == null || toolDefinitions.Length == 0) return;

        bool toolChanged = false;
        int previousIndex = currentIndex; // Store previous index for comparison

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
            // Ensure the new index points to a valid definition
            if (toolDefinitions[currentIndex] != null)
            {
                CurrentTool = toolDefinitions[currentIndex];
                LogToolChange("[ToolSwitcher Update]");
                Debug.Log($"[ToolSwitcher Update] Firing OnToolChanged event for tool: {CurrentTool?.displayName ?? "NULL"}");
                OnToolChanged?.Invoke(CurrentTool);
            }
            else
            {
                // The selected tool definition is null, revert index and maybe log error
                Debug.LogError($"[ToolSwitcher Update] Attempted to switch to a NULL tool definition at index {currentIndex}. Reverting to previous tool.");
                currentIndex = previousIndex; // Revert to the last valid index
                // Optionally, fire the event again with the *previous* valid tool if needed, or do nothing
                // OnToolChanged?.Invoke(CurrentTool); // CurrentTool still holds the previous valid one
            }
        }
    }

    // Added prefix parameter for context
    private void LogToolChange(string prefix = "[ToolSwitcher]")
    {
        string toolName = (CurrentTool != null && !string.IsNullOrEmpty(CurrentTool.displayName))
                          ? CurrentTool.displayName
                          : "(none)";
        Debug.Log($"{prefix} Switched tool to: {toolName} (Index: {currentIndex})");
    }
}