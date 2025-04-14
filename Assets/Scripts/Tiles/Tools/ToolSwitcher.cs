﻿using UnityEngine;
using UnityEngine.UI;

public class ToolSwitcher : MonoBehaviour
{
    [Tooltip("Array of Tool Definition assets (order matters for cycling with Q/E).")]
    public ToolDefinition[] toolDefinitions;

    // This field is now used to display the tool icon above the player.
    [Tooltip("SpriteRenderer that displays the current tool icon above the player.")]
    public SpriteRenderer toolDisplay;

    private int currentIndex = 0;
    public ToolDefinition CurrentTool { get; private set; } = null;

    private void Start()
    {
        if (toolDefinitions.Length > 0)
        {
            currentIndex = 0;
            CurrentTool = toolDefinitions[currentIndex];
            UpdateToolDisplay();
            LogToolChange();
        }
    }

    private void Update()
    {
        if (toolDefinitions.Length == 0)
            return;

        // Cycle backwards with Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = toolDefinitions.Length - 1;
            CurrentTool = toolDefinitions[currentIndex];
            UpdateToolDisplay();
            LogToolChange();
        }
        // Cycle forwards with E
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentIndex++;
            if (currentIndex >= toolDefinitions.Length)
                currentIndex = 0;
            CurrentTool = toolDefinitions[currentIndex];
            UpdateToolDisplay();
            LogToolChange();
        }
    }

    private void UpdateToolDisplay()
    {
        if (toolDisplay == null)
            return;

        if (CurrentTool != null && CurrentTool.icon != null)
        {
            toolDisplay.sprite = CurrentTool.icon;
            toolDisplay.color = CurrentTool.iconTint; // Apply the tint defined in the ToolDefinition
            toolDisplay.enabled = true;
        }
        else
        {
            toolDisplay.enabled = false;
        }
    }

    private void LogToolChange()
    {
        string toolName = (CurrentTool != null) ? CurrentTool.displayName : "(none)";
        Debug.Log($"Switched tool to: {toolName}");
    }
}