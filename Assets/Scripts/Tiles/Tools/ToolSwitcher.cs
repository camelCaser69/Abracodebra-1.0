using UnityEngine;

public class ToolSwitcher : MonoBehaviour
{
    [Tooltip("All available tool definitions, e.g. Hoe, WateringCan, etc.")]
    public ToolDefinition[] toolDefinitions;

    private int currentIndex = 0;

    /// <summary>
    /// The currently selected tool definition.
    /// </summary>
    public ToolDefinition CurrentTool { get; private set; } = null;

    private void Start()
    {
        if (toolDefinitions.Length > 0)
        {
            currentIndex = 0;
            CurrentTool = toolDefinitions[currentIndex];
            LogToolChange();
        }
    }

    private void Update()
    {
        if (toolDefinitions.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = toolDefinitions.Length - 1;
            CurrentTool = toolDefinitions[currentIndex];
            LogToolChange();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentIndex++;
            if (currentIndex >= toolDefinitions.Length)
                currentIndex = 0;
            CurrentTool = toolDefinitions[currentIndex];
            LogToolChange();
        }
    }

    private void LogToolChange()
    {
        string toolName = (CurrentTool != null) ? CurrentTool.displayName : "(none)";
        Debug.Log($"Switched tool to: {toolName}");
    }
    
}