using UnityEngine;
using UnityEngine.U2D;
using WegoSystem;

namespace WegoSystem
{
    [RequireComponent(typeof(PixelPerfectCamera), typeof(Camera))]
    public class PixelPerfectSetup : MonoBehaviour
    {
        [Tooltip("The central configuration for map and camera settings. This script will pull its values from here.")]
        [SerializeField] private MapConfiguration mapConfig;

        private PixelPerfectCamera pixelPerfectCam;
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            pixelPerfectCam = GetComponent<PixelPerfectCamera>();

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
            pixelPerfectCam.upscaleRT = true;
            pixelPerfectCam.pixelSnapping = true;
            pixelPerfectCam.cropFrameX = false;
            pixelPerfectCam.cropFrameY = false;
            pixelPerfectCam.stretchFill = false;

            // Set the correct orthographic size based on the reference resolution and PPU
            // This is the most crucial part for achieving a true pixel-perfect look.
            float correctOrthoSize = (float)mapConfig.referenceResolution.y / (2f * mapConfig.pixelsPerUnit);
            cam.orthographicSize = correctOrthoSize;

            Debug.Log($"[PixelPerfectSetup] Configured from MapConfiguration: {mapConfig.referenceResolution.x}x{mapConfig.referenceResolution.y} @ {mapConfig.pixelsPerUnit} PPU. Calculated Ortho Size: {correctOrthoSize}");
        }

        private void SetupWithDefaults()
        {
            // This is a fallback in case the MapConfiguration is not assigned.
            // It uses the old hardcoded values.
            Debug.LogWarning("[PixelPerfectSetup] No MapConfiguration assigned, using fallback default values. It is highly recommended to assign a MapConfiguration asset.", this);

            int referenceResolutionX = 320;
            int referenceResolutionY = 180;
            int pixelsPerUnit = 6;

            pixelPerfectCam.assetsPPU = pixelsPerUnit;
            pixelPerfectCam.refResolutionX = referenceResolutionX;
            pixelPerfectCam.refResolutionY = referenceResolutionY;
            pixelPerfectCam.upscaleRT = true;
            pixelPerfectCam.pixelSnapping = true;

            float correctOrthoSize = (float)referenceResolutionY / (2f * pixelsPerUnit);
            cam.orthographicSize = correctOrthoSize;
        }
    }
}