using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    public class SceneSetupManager : MonoBehaviour
    {
        [SerializeField] private bool setupOnStart = true;

        private void Start()
        {
            if (setupOnStart)
            {
                SetupScene();
            }
        }

        public void SetupScene()
        {
            Debug.Log("--- Starting Scene Setup ---");

            GridPositionManager gridManager = GridPositionManager.Instance;

            if (gridManager == null)
            {
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

            GardenerController player = FindFirstObjectByType<GardenerController>();
            if (player != null)
            {
                gridManager.SnapEntityToGrid(player.gameObject);

                GridPosition centerPosition = gridManager.GetMapCenter();
                player.GetComponent<GridEntity>().SetPosition(centerPosition, true);

                Debug.Log($"Player '{player.name}' snapped, registered, and moved to map center: {centerPosition}", player);

                // Update camera setup to use the new CameraController
                SetupMainCamera(player.transform);
            }
            else
            {
                Debug.LogWarning("[SceneSetupManager] No GardenerController found in the scene to position.", this);
            }

            Debug.Log("--- Scene Setup Complete ---");
        }
        
        private void SetupMainCamera(Transform target)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                CameraController cameraController = mainCamera.GetComponent<CameraController>();
                if (cameraController == null)
                {
                    Debug.LogWarning($"[SceneSetupManager] Main Camera does not have a CameraController. Adding one now.", mainCamera);
                    cameraController = mainCamera.gameObject.AddComponent<CameraController>();
                }

                cameraController.followTarget = target;
                cameraController.SnapToTarget(); // Immediate snap on scene setup
                
                Debug.Log($"Main Camera's CameraController set to follow '{target.name}' and snapped to position.", mainCamera);
            }
            else
            {
                Debug.LogWarning("[SceneSetupManager] Could not find Main Camera to configure. Ensure it has the 'MainCamera' tag.", this);
            }
        }
    }
}