using UnityEngine;
using UnityEngine.UI;

public class ToolSwitcher : MonoBehaviour
{
    public ToolDefinition[] toolDefinitions;
    public Image toolIcon;

    private int currentIndex = 0;
    public ToolDefinition CurrentTool { get; private set; } = null;

    private void Start()
    {
        if (toolDefinitions.Length > 0)
        {
            currentIndex = 0;
            CurrentTool = toolDefinitions[currentIndex];
            UpdateUI();
        }
    }

    private void Update()
    {
        if (toolDefinitions.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = toolDefinitions.Length - 1;
            CurrentTool = toolDefinitions[currentIndex];
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentIndex++;
            if (currentIndex >= toolDefinitions.Length)
                currentIndex = 0;
            CurrentTool = toolDefinitions[currentIndex];
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        Debug.Log($"Switched tool to: {CurrentTool?.toolType}");
        if (toolIcon != null && CurrentTool != null && CurrentTool.icon != null)
        {
            toolIcon.sprite = CurrentTool.icon;
        }
    }
}