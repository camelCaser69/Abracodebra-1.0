using UnityEngine;

[RequireComponent(typeof(ToolSwitcher))]
public class PlayerTileInteractor : MonoBehaviour
{
    private ToolSwitcher toolSwitcher;

    private void Awake()
    {
        toolSwitcher = GetComponent<ToolSwitcher>();
        if (toolSwitcher == null)
            Debug.LogError("PlayerTileInteractor: No ToolSwitcher found on this GameObject!");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (TileInteractionManager.Instance == null)
            {
                Debug.LogError("No TileInteractionManager in scene!");
                return;
            }

            ToolDefinition currentTool = toolSwitcher.CurrentTool;
            if (currentTool == null)
            {
                Debug.Log("No tool is currently selected.");
                return;
            }

            // Attempt to apply the tool
            TileInteractionManager.Instance.ApplyToolAction(currentTool);
        }
    }
}
