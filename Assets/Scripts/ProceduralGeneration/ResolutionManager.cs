using UnityEngine;
using UnityEngine.U2D;
using WegoSystem;

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
            new ResolutionProfile { name = "HD Ready (2x)", resolution = new Vector2Int(640, 360), upscaleRT = false, cameraZoomMultiplier = 1f },
            new ResolutionProfile { name = "Full HD (4x)", resolution = new Vector2Int(1280, 720), upscaleRT = false, cameraZoomMultiplier = 1f }
        };

        [SerializeField] private int currentProfileIndex = 0;

        private PixelPerfectCamera pixelPerfectCam;
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
            if (cam != null)
            {
                pixelPerfectCam = cam.GetComponent<PixelPerfectCamera>();
            }
            
            if (cam == null || pixelPerfectCam == null)
            {
                Debug.LogError("[ResolutionManager] Could not find Main Camera or its PixelPerfectCamera component!", this);
                enabled = false;
                return;
            }

            if (mapConfig == null)
            {
                Debug.LogError("[ResolutionManager] MapConfiguration is not assigned! Cannot calculate camera sizes.", this);
                enabled = false;
                return;
            }

            ApplyProfile(currentProfileIndex);
        }

        /// <summary>
        /// Applies a resolution profile by its index in the profiles array.
        /// </summary>
        /// <param name="index">The index of the profile to apply.</param>
        public void ApplyProfile(int index)
        {
            if (index < 0 || index >= profiles.Length)
            {
                Debug.LogWarning($"[ResolutionManager] Invalid profile index {index}. Aborting.", this);
                return;
            }

            var profile = profiles[index];
            currentProfileIndex = index;

            if (pixelPerfectCam != null)
            {
                pixelPerfectCam.refResolutionX = profile.resolution.x;
                pixelPerfectCam.refResolutionY = profile.resolution.y;
                pixelPerfectCam.upscaleRT = profile.upscaleRT;

                // Adjust camera size based on the base config, profile, and zoom
                float baseOrthoSize = (float)mapConfig.referenceResolution.y / (2f * mapConfig.pixelsPerUnit);
                cam.orthographicSize = baseOrthoSize * profile.cameraZoomMultiplier;
            }

            // Notify camera controller to recalculate its movement bounds
            var cameraController = cam.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.OnResolutionChanged();
            }

            Debug.Log($"[ResolutionManager] Applied resolution profile: '{profile.name}'");
        }

        /// <summary>
        /// A public method for UI buttons or keybinds to cycle to the next resolution profile.
        /// </summary>
        public void CycleResolution()
        {
            int nextIndex = (currentProfileIndex + 1) % profiles.Length;
            ApplyProfile(nextIndex);
        }
    }
}