using UnityEngine;
using URPPixelPerfectCamera = UnityEngine.Rendering.Universal.PixelPerfectCamera;
using WegoSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    public class ResolutionManager : SingletonMonoBehaviour<ResolutionManager>
    {
        [System.Serializable]
        public class ResolutionProfile
        {
            public string name;
            public Vector2Int resolution;
            public int pixelsPerUnit = 6;
            public float cameraZoomMultiplier = 1f;
        }

        [Header("Core References")]
        [Tooltip("Drag your Main Camera GameObject here. This is the most reliable way to link the camera.")]
        [SerializeField] private Camera mainCamera; 
        [SerializeField] private MapConfiguration mapConfig;

        [Header("Profiles")]
        [SerializeField] ResolutionProfile[] profiles = {
            new ResolutionProfile { 
                name = "Pixel Perfect 320x180", 
                resolution = new Vector2Int(320, 180), 
                pixelsPerUnit = 16, 
                cameraZoomMultiplier = 1f 
            },
            new ResolutionProfile { 
                name = "HD 640x360", 
                resolution = new Vector2Int(640, 360), 
                pixelsPerUnit = 16, 
                cameraZoomMultiplier = 1f 
            },
            new ResolutionProfile { 
                name = "Full HD 1280x720", 
                resolution = new Vector2Int(1280, 720), 
                pixelsPerUnit = 16, 
                cameraZoomMultiplier = 1f 
            },
            new ResolutionProfile { 
                name = "4K 2560x1440", 
                resolution = new Vector2Int(2560, 1440), 
                pixelsPerUnit = 16, 
                cameraZoomMultiplier = 1f 
            }
        };

        [Tooltip("The profile that will be applied on game start and by the editor button.")]
        [SerializeField] private int currentProfileIndex = 0;
        
        public int CurrentPPU { get; private set; } = 6;

        private URPPixelPerfectCamera pixelPerfectCam;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (mainCamera != null)
            {
                pixelPerfectCam = mainCamera.GetComponent<URPPixelPerfectCamera>();
            }
        }

        private void Start()
        {
            ApplyProfile(currentProfileIndex);
        }

        public void ApplyProfileInEditor()
        {
            Camera camToApply = mainCamera;
            if (camToApply == null)
            {
                camToApply = Camera.main;
            }

            if (camToApply == null)
            {
                Debug.LogError("[ResolutionManager] Could not find Main Camera. Ensure it's tagged or assigned in the Inspector.");
                return;
            }

            // --- FINAL, ROBUST FIX FOR EDITOR MODE ---
            // We fetch the component using its full type name as a string.
            // This bypasses the editor's issue with generic lookups on aliased types from packages.
            var ppCamComponent = camToApply.GetComponent("UnityEngine.Rendering.Universal.PixelPerfectCamera");
            
            if (ppCamComponent == null)
            {
                Debug.LogError($"[ResolutionManager] Failed to find URP PixelPerfectCamera component on '{camToApply.name}'. Please ensure the component exists.", camToApply.gameObject);
                return;
            }
            
            // Cast the found generic component to the specific type we need to work with.
            URPPixelPerfectCamera ppCam = ppCamComponent as URPPixelPerfectCamera;
            // --- END OF FIX ---
            
            ApplyProfileLogic(currentProfileIndex, camToApply, ppCam);
        }

        public void ApplyProfile(int index)
        {
            if (mainCamera == null || pixelPerfectCam == null)
            {
                Debug.LogError("[ResolutionManager] The 'Main Camera' reference is not set in the Inspector or was not found at startup! Disabling ResolutionManager.", this);
                this.enabled = false; 
                return;
            }
            
            ApplyProfileLogic(index, mainCamera, pixelPerfectCam);
        }

        private void ApplyProfileLogic(int index, Camera targetCam, URPPixelPerfectCamera targetPPCam)
        {
            if (mapConfig == null)
            {
                Debug.LogError("[ResolutionManager] MapConfiguration is not assigned! Cannot apply profile.", this);
                return;
            }
            
            if (index < 0 || index >= profiles.Length)
            {
                Debug.LogWarning($"[ResolutionManager] Invalid profile index {index}. Aborting.", this);
                return;
            }

            var profile = profiles[index];
            currentProfileIndex = index;
            
            CurrentPPU = profile.pixelsPerUnit;
            targetPPCam.assetsPPU = profile.pixelsPerUnit;
            targetPPCam.refResolutionX = profile.resolution.x;
            targetPPCam.refResolutionY = profile.resolution.y;

            float baseOrthoSize = (float)profile.resolution.y / (2f * profile.pixelsPerUnit);
            targetCam.orthographicSize = baseOrthoSize * profile.cameraZoomMultiplier;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(targetCam);
                EditorUtility.SetDirty(targetPPCam);
            }
            #endif

            var cameraController = targetCam.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.OnResolutionChanged();
            }

            Debug.Log($"[ResolutionManager] Applied profile: '{profile.name}' (PPU: {CurrentPPU}, Res: {profile.resolution}, Zoom: {targetCam.orthographicSize})");
        }

        public void CycleResolution()
        {
            int nextIndex = (currentProfileIndex + 1) % profiles.Length;
            ApplyProfile(nextIndex);
        }
    }
}