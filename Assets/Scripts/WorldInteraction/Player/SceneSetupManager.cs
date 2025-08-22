// FILE: Assets/Scripts/Core/SceneSetupManager.cs
using UnityEngine;
using WegoSystem;

public class SceneSetupManager : MonoBehaviour
{
    [Header("Setup Settings")]
    [Tooltip("If checked, the scene will be set up automatically when the game starts.")]
    [SerializeField]
    private bool setupOnStart = true;

    private void Start()
    {
        if (setupOnStart)
        {
            SetupScene();
        }
    }

    /// <summary>
    /// Finds and positions the player and main camera to the center of the map.
    /// Safe to call from both Play Mode and the Unity Editor.
    /// </summary>
    public void SetupScene()
    {
        Debug.Log("--- Starting Scene Setup ---");

        // --- START OF FIX ---

        // Get a reference to the GridPositionManager.
        GridPositionManager gridManager = GridPositionManager.Instance;

        // In Edit Mode, the static 'Instance' might be null. If so, find it manually.
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridPositionManager>();
            if (gridManager != null)
            {
                Debug.LogWarning("[SceneSetupManager] GridPositionManager.Instance was null (expected in Edit Mode). Found manager manually.", this);
            }
        }
        
        // Now, perform the null check on our local variable.
        if (gridManager == null)
        {
            Debug.LogError("[SceneSetupManager] GridPositionManager could not be found in the scene. Aborting setup.", this);
            return;
        }

        // --- END OF FIX ---

        // --- Find and Position Player ---
        GardenerController player = FindObjectOfType<GardenerController>();
        if (player != null)
        {
            // This is the crucial step that registers the player with the grid system.
            gridManager.SnapEntityToGrid(player.gameObject);
            
            // Now we can safely move it to the center.
            GridPosition centerPosition = gridManager.GetMapCenter();
            player.GetComponent<GridEntity>().SetPosition(centerPosition, true);
            
            Debug.Log($"Player '{player.name}' snapped, registered, and moved to map center: {centerPosition}", player);

            // --- Find and Position Camera (after player is moved) ---
            CenterMainCameraOnTarget(player.transform);
        }
        else
        {
            Debug.LogWarning("[SceneSetupManager] No GardenerController found in the scene to position.", this);
        }
        
        Debug.Log("--- Scene Setup Complete ---");
    }
    
    /// <summary>
    /// Finds the main camera and centers it on the specified target transform.
    /// </summary>
    private void CenterMainCameraOnTarget(Transform target)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 targetWorldPosition = target.position;
            Transform cameraTransform = mainCamera.transform;

            cameraTransform.position = new Vector3(
                targetWorldPosition.x,
                targetWorldPosition.y,
                cameraTransform.position.z
            );
            
            Debug.Log($"Main Camera moved to focus on '{target.name}' at {targetWorldPosition}", mainCamera);
        }
        else
        {
            Debug.LogWarning("[SceneSetupManager] Could not find Main Camera to center. Ensure it has the 'MainCamera' tag.", this);
        }
    }
}