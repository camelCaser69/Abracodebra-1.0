using UnityEngine;
using System;
using System.Collections.Generic;

public class ToolSwitcher : MonoBehaviour
{
    public static ToolSwitcher Instance { get; set; }

    public ToolDefinition[] toolDefinitions;

    // NEW: Dictionary to store uses for ALL tools.
    private Dictionary<ToolDefinition, int> toolUses = new Dictionary<ToolDefinition, int>();

    private int currentIndex = 0;

    public ToolDefinition CurrentTool { get; set; } = null;
    public int CurrentRemainingUses { get; private set; } = -1; // Setter is now private

    public event Action<ToolDefinition> OnToolChanged;
    public event Action<int> OnUsesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // NEW: Populate the uses dictionary at the start.
        InitializeAllToolUses();
    }

    private void Start()
    {
        // Select the initial tool based on the array.
        SelectToolByIndex(0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeAllToolUses()
    {
        toolUses.Clear();
        if (toolDefinitions == null) return;

        foreach (var toolDef in toolDefinitions)
        {
            if (toolDef != null)
            {
                if (toolDef.limitedUses)
                {
                    toolUses[toolDef] = toolDef.initialUses;
                }
                else
                {
                    toolUses[toolDef] = -1; // -1 indicates unlimited
                }
            }
        }
    }

    public void SelectToolByIndex(int index)
    {
        if (toolDefinitions == null || toolDefinitions.Length == 0 || index < 0 || index >= toolDefinitions.Length)
        {
            return;
        }

        currentIndex = index;
        ToolDefinition newTool = toolDefinitions[currentIndex];

        if (CurrentTool != newTool)
        {
            CurrentTool = newTool;
            CurrentRemainingUses = toolUses.ContainsKey(CurrentTool) ? toolUses[CurrentTool] : (CurrentTool.limitedUses ? CurrentTool.initialUses : -1);

            OnToolChanged?.Invoke(CurrentTool);
            OnUsesChanged?.Invoke(CurrentRemainingUses);
            LogToolChange($"[ToolSwitcher SelectToolByIndex]");
        }
    }

    public void SelectToolByDefinition(ToolDefinition toolDef)
    {
        if (toolDef == null || toolDefinitions == null) return;

        for (int i = 0; i < toolDefinitions.Length; i++)
        {
            if (toolDefinitions[i] == toolDef)
            {
                // If we are already on this tool, no need to do anything.
                if (currentIndex == i && CurrentTool == toolDef) return;

                currentIndex = i;
                CurrentTool = toolDef;
                CurrentRemainingUses = toolUses.ContainsKey(CurrentTool) ? toolUses[CurrentTool] : (CurrentTool.limitedUses ? CurrentTool.initialUses : -1);

                OnToolChanged?.Invoke(CurrentTool);
                OnUsesChanged?.Invoke(CurrentRemainingUses);
                LogToolChange($"[ToolSwitcher SelectToolByDefinition]");
                return;
            }
        }
        Debug.LogWarning($"[ToolSwitcher] Tool '{toolDef.displayName}' not found in definitions array");
    }

    public void RefillCurrentTool()
    {
        if (CurrentTool == null || !CurrentTool.limitedUses) return;
        
        toolUses[CurrentTool] = CurrentTool.initialUses;
        CurrentRemainingUses = CurrentTool.initialUses;
        
        Debug.Log($"[ToolSwitcher RefillCurrentTool] Refilled tool '{CurrentTool.displayName}' to {CurrentRemainingUses} uses.");
        OnUsesChanged?.Invoke(CurrentRemainingUses);
    }

    public bool TryConsumeUse()
    {
        if (CurrentTool == null) return false;
        
        if (!CurrentTool.limitedUses || CurrentRemainingUses == -1)
        {
            return true;
        }
        
        if (CurrentRemainingUses > 0)
        {
            CurrentRemainingUses--;
            toolUses[CurrentTool] = CurrentRemainingUses; // Update the persistent dictionary
            Debug.Log($"[ToolSwitcher TryConsumeUse] Consumed use for '{CurrentTool.displayName}'. Remaining: {CurrentRemainingUses}");
            OnUsesChanged?.Invoke(CurrentRemainingUses);
            return true;
        }
        else
        {
            Debug.Log($"[ToolSwitcher TryConsumeUse] Tool '{CurrentTool.displayName}' is out of uses.");
            return false;
        }
    }

    private void LogToolChange(string prefix = "[ToolSwitcher]")
    {
        string toolName = (CurrentTool != null) ? CurrentTool.displayName : "(none)";
        string usesSuffix = "";
        if (CurrentTool != null && CurrentTool.limitedUses)
        {
            usesSuffix = $" ({CurrentRemainingUses}/{CurrentTool.initialUses})";
        }
        
        Debug.Log($"{prefix} Switched to: {toolName}{usesSuffix} (Index: {currentIndex})");
    }
}