using UnityEngine;
using URPPixelPerfectCamera = UnityEngine.Rendering.Universal.PixelPerfectCamera;
using WegoSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    public class ResolutionManager : MonoBehaviour
    {
        [System.Serializable]
        public class ResolutionProfile
        {
            public string name;
            public Vector2Int resolution;
            public bool upscaleRT;
            [Tooltip("Allows for zooming in or out relative to the base pixel-perfect size.")]
            public float cameraZoomMultiplier = 1f;
        }

        [Header("References")]
        [SerializeField] private MapConfiguration mapConfig;

        [Header("Profiles")]
        [SerializeField] private ResolutionProfile[] profiles = new[]
        {
            new ResolutionProfile { name = "Pixel Perfect (Native)", resolution = new Vector2Int(320, 180), upscaleRT = true, cameraZoomMultiplier = 1f },
            new ResolutionProfile { name = "HD Ready (2x)", resolution = new Vector2Int(640, 360), upscaleRT = true, cameraZoomMultiplier = 1f },
            new ResolutionProfile { name = "Full HD (4x)", resolution = new Vector2Int(1280, 720), upscaleRT = true, cameraZoomMultiplier = 1f }
        };

        [SerializeField] private int currentProfileIndex = 0;

        private URPPixelPerfectCamera pixelPerfectCam;
        private Camera cam;

        private void Start()
        {
            // At runtime, we still want to apply the profile automatically.
            ApplyProfile(currentProfileIndex);
        }

        // We can't call ApplyProfile directly because it uses private members not set in edit mode.
        // So we create a dedicated method for the editor button.
        public void ApplyProfileInEditor()
        {
            // In the editor, we need to find the references manually.
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[ResolutionManager] Could not find Main Camera. Ensure it has the 'MainCamera' tag.");
                return;
            }

            URPPixelPerfectCamera ppCam = mainCam.GetComponent<URPPixelPerfectCamera>();
            if (ppCam == null)
            {
                Debug.LogError("[ResolutionManager] Main Camera is missing the URP PixelPerfectCamera component.");
                return;
            }
            
            // Now, call the core logic with the found components.
            ApplyProfileLogic(currentProfileIndex, mainCam, ppCam);
        }

        public void ApplyProfile(int index)
        {
            // In Play mode, we can use the cached references.
            if (cam == null || pixelPerfectCam == null)
            {
                cam = Camera.main;
                if (cam != null)
                {
                    pixelPerfectCam = cam.GetComponent<URPPixelPerfectCamera>();
                }
            }
            
            if (cam == null || pixelPerfectCam == null)
            {
                Debug.LogError("[ResolutionManager] Could not find Main Camera or its URP PixelPerfectCamera component!", this);
                enabled = false;
                return;
            }
            
            ApplyProfileLogic(index, cam, pixelPerfectCam);
        }

        // This new method contains the shared logic.
        private void ApplyProfileLogic(int index, Camera targetCam, URPPixelPerfectCamera targetPPCam)
        {
            if (mapConfig == null)
            {
                Debug.LogError("[ResolutionManager] MapConfiguration is not assigned! Cannot calculate camera sizes.", this);
                return;
            }
            
            if (index < 0 || index >= profiles.Length)
            {
                Debug.LogWarning($"[ResolutionManager] Invalid profile index {index}. Aborting.", this);
                return;
            }

            var profile = profiles[index];
            currentProfileIndex = index;

            if (targetPPCam != null)
            {
                targetPPCam.refResolutionX = profile.resolution.x;
                targetPPCam.refResolutionY = profile.resolution.y;

                float baseOrthoSize = (float)mapConfig.referenceResolution.y / (2f * mapConfig.pixelsPerUnit);
                targetCam.orthographicSize = baseOrthoSize * profile.cameraZoomMultiplier;

                #if UNITY_EDITOR
                // When in the editor and not playing, we need to mark the objects as "dirty"
                // so that Unity knows to save the changes.
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(targetCam);
                    EditorUtility.SetDirty(targetPPCam);
                }
                #endif
            }

            var cameraController = targetCam.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.OnResolutionChanged();
            }

            Debug.Log($"[ResolutionManager] Applied resolution profile: '{profile.name}'");
        }

        public void CycleResolution()
        {
            int nextIndex = (currentProfileIndex + 1) % profiles.Length;
            ApplyProfile(nextIndex);
        }
    }
}