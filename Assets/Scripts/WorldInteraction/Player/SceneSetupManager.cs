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
        
        GridPositionManager gridManager = GridPositionManager.Instance;

        // In Edit Mode, the static 'Instance' might be null. If so, find it manually.
        if (gridManager == null)
        {
            // OBSOLETE CALL FIXED: Replaced FindObjectOfType with FindFirstObjectByType
            gridManager = FindFirstObjectByType<GridPositionManager>();
            if (gridManager != null)
            {
                Debug.LogWarning("[SceneSetupManager] GridPositionManager.Instance was null (expected in Edit Mode). Found manager manually.", this);
            }
        }
        
        if (gridManager == null)
        {
            Debug.LogError("[SceneSetupManager] GridPositionManager could not be found in the scene. Aborting setup.", this);
            return;
        }

        // --- Find and Position Player ---
        // OBSOLETE CALL FIXED: Replaced FindObjectOfType with FindFirstObjectByType
        GardenerController player = FindFirstObjectByType<GardenerController>();
        if (player != null)
        {
            gridManager.SnapEntityToGrid(player.gameObject);
            
            GridPosition centerPosition = gridManager.GetMapCenter();
            player.GetComponent<GridEntity>().SetPosition(centerPosition, true);
            
            Debug.Log($"Player '{player.name}' snapped, registered, and moved to map center: {centerPosition}", player);
            
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