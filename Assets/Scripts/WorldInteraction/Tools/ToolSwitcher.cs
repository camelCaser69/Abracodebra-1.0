// FILE: Assets/Scripts/WorldInteraction/Tools/ToolSwitcher.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class ToolSwitcher : MonoBehaviour {
    public static ToolSwitcher Instance { get; private set; }

    public ToolDefinition[] toolDefinitions;

    private Dictionary<ToolDefinition, int> toolUses = new Dictionary<ToolDefinition, int>();

    private int currentIndex = 0;

    public ToolDefinition CurrentTool { get; private set; } = null;
    public int CurrentRemainingUses { get; private set; } = -1;

    public event Action<ToolDefinition> OnToolChanged;
    public event Action<int> OnUsesChanged;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeAllToolUses();
    }

    void Start() {
        SelectToolByIndex(0);
    }

    void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }

    void InitializeAllToolUses() {
        toolUses.Clear();
        if (toolDefinitions == null) return;

        foreach (var toolDef in toolDefinitions) {
            if (toolDef != null) {
                if (toolDef.limitedUses) {
                    toolUses[toolDef] = toolDef.initialUses;
                }
                else {
                    toolUses[toolDef] = -1; // -1 indicates unlimited
                }
            }
        }
    }

    public void SelectToolByIndex(int index) {
        if (toolDefinitions == null || toolDefinitions.Length == 0 || index < 0 || index >= toolDefinitions.Length) {
            return;
        }

        currentIndex = index;
        ToolDefinition newTool = toolDefinitions[currentIndex];

        if (CurrentTool != newTool) {
            CurrentTool = newTool;
            CurrentRemainingUses = toolUses.ContainsKey(CurrentTool) ? toolUses[CurrentTool] : (CurrentTool.limitedUses ? CurrentTool.initialUses : -1);

            OnToolChanged?.Invoke(CurrentTool);
            OnUsesChanged?.Invoke(CurrentRemainingUses);
            LogToolChange($"[ToolSwitcher SelectToolByIndex]");
        }
    }

    public void SelectToolByDefinition(ToolDefinition toolDef) {
        if (toolDef == null || toolDefinitions == null) return;

        for (int i = 0; i < toolDefinitions.Length; i++) {
            if (toolDefinitions[i] == toolDef) {
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

    public void RefillCurrentTool() {
        if (CurrentTool == null || !CurrentTool.limitedUses) return;

        toolUses[CurrentTool] = CurrentTool.initialUses;
        CurrentRemainingUses = CurrentTool.initialUses;

        Debug.Log($"[ToolSwitcher RefillCurrentTool] Refilled tool '{CurrentTool.displayName}' to {CurrentRemainingUses} uses.");
        OnUsesChanged?.Invoke(CurrentRemainingUses);
    }

    /// <summary>
    /// Check if the current tool has uses remaining without consuming.
    /// Returns true if tool has unlimited uses or has remaining uses > 0.
    /// </summary>
    public bool HasUsesRemaining() {
        if (CurrentTool == null) return false;

        // Unlimited uses
        if (!CurrentTool.limitedUses || CurrentRemainingUses == -1) {
            return true;
        }

        return CurrentRemainingUses > 0;
    }

    /// <summary>
    /// Check if a specific tool has uses remaining without consuming.
    /// </summary>
    public bool HasUsesRemaining(ToolDefinition toolDef) {
        if (toolDef == null) return false;

        // Unlimited uses
        if (!toolDef.limitedUses) {
            return true;
        }

        // Check stored uses
        if (toolUses.TryGetValue(toolDef, out int uses)) {
            return uses > 0 || uses == -1;
        }

        // Fall back to initial uses if not tracked yet
        return toolDef.initialUses > 0;
    }

    /// <summary>
    /// Attempts to consume one use of the current tool.
    /// Returns true if successful (tool has uses or is unlimited).
    /// Returns false if the tool is out of uses.
    /// </summary>
    public bool TryConsumeUse() {
        if (CurrentTool == null) return false;

        if (!CurrentTool.limitedUses || CurrentRemainingUses == -1) {
            return true;
        }

        if (CurrentRemainingUses > 0) {
            CurrentRemainingUses--;
            toolUses[CurrentTool] = CurrentRemainingUses;
            Debug.Log($"[ToolSwitcher TryConsumeUse] Consumed use for '{CurrentTool.displayName}'. Remaining: {CurrentRemainingUses}");
            OnUsesChanged?.Invoke(CurrentRemainingUses);
            return true;
        }
        else {
            Debug.Log($"[ToolSwitcher TryConsumeUse] Tool '{CurrentTool.displayName}' is out of uses.");
            return false;
        }
    }

    void LogToolChange(string prefix = "[ToolSwitcher]") {
        string toolName = (CurrentTool != null) ? CurrentTool.displayName : "(none)";
        string usesSuffix = "";
        if (CurrentTool != null && CurrentTool.limitedUses) {
            usesSuffix = $" ({CurrentRemainingUses}/{CurrentTool.initialUses})";
        }

        Debug.Log($"{prefix} Switched to: {toolName}{usesSuffix} (Index: {currentIndex})");
    }
}