using UnityEngine;
using URPPixelPerfectCamera = UnityEngine.Rendering.Universal.PixelPerfectCamera;
using WegoSystem;

namespace WegoSystem
{
    [RequireComponent(typeof(URPPixelPerfectCamera), typeof(Camera))]
    public class PixelPerfectSetup : MonoBehaviour
    {
        [Tooltip("The central configuration for map and camera settings. This script will pull its values from here.")]
        [SerializeField] private MapConfiguration mapConfig;

        private URPPixelPerfectCamera pixelPerfectCam;
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            pixelPerfectCam = GetComponent<URPPixelPerfectCamera>();

            if (mapConfig != null)
            {
                SetupFromConfig();
            }
            else
            {
                SetupWithDefaults();
            }
        }

        private void SetupFromConfig()
        {
            if (mapConfig == null) return;

            pixelPerfectCam.assetsPPU = mapConfig.pixelsPerUnit;
            pixelPerfectCam.refResolutionX = mapConfig.referenceResolution.x;
            pixelPerfectCam.refResolutionY = mapConfig.referenceResolution.y;

            pixelPerfectCam.gridSnapping = URPPixelPerfectCamera.GridSnapping.PixelSnapping;
            pixelPerfectCam.cropFrame = URPPixelPerfectCamera.CropFrame.None;

            // REMOVED: This script will no longer control the camera's orthographic size.
            // That responsibility now belongs solely to the ResolutionManager to prevent conflicts.
            // float correctOrthoSize = (float)mapConfig.referenceResolution.y / (2f * mapConfig.pixelsPerUnit);
            // cam.orthographicSize = correctOrthoSize;

            Debug.Log($"[PixelPerfectSetup] Configured PPU and Reference Resolution from MapConfiguration.");
        }

        private void SetupWithDefaults()
        {
            Debug.LogWarning("[PixelPerfectSetup] No MapConfiguration assigned, using fallback default values. It is highly recommended to assign a MapConfiguration asset.", this);

            int referenceResolutionX = 320;
            int referenceResolutionY = 180;
            int pixelsPerUnit = 6;

            pixelPerfectCam.assetsPPU = pixelsPerUnit;
            pixelPerfectCam.refResolutionX = referenceResolutionX;
            pixelPerfectCam.refResolutionY = referenceResolutionY;
            
            pixelPerfectCam.gridSnapping = URPPixelPerfectCamera.GridSnapping.PixelSnapping;
            
            // REMOVED: This script will no longer control the camera's orthographic size.
            // float correctOrthoSize = (float)referenceResolutionY / (2f * pixelsPerUnit);
            // cam.orthographicSize = correctOrthoSize;
        }
    }
}