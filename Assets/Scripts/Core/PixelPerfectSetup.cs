using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(PixelPerfectCamera))]
public class PixelPerfectSetup : MonoBehaviour
{
    [Header("Game Resolution")]
    [SerializeField] private int referenceResolutionX = 320; // Your base game width
    [SerializeField] private int referenceResolutionY = 180; // Your base game height
    
    [Header("Pixel Settings")]
    [SerializeField] private int pixelsPerUnit = 6; // Since 1 game pixel = 6x6 real pixels
    
    private PixelPerfectCamera pixelPerfectCamera;
    private Camera cam;
    
    void Awake()
    {
        cam = GetComponent<Camera>();
        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        
        SetupPixelPerfectCamera();
    }
    
    void SetupPixelPerfectCamera()
    {
        // Configure Pixel Perfect Camera
        pixelPerfectCamera.assetsPPU = pixelsPerUnit;
        pixelPerfectCamera.refResolutionX = referenceResolutionX;
        pixelPerfectCamera.refResolutionY = referenceResolutionY;
        pixelPerfectCamera.upscaleRT = true;
        pixelPerfectCamera.pixelSnapping = true;
        pixelPerfectCamera.cropFrameX = false;
        pixelPerfectCamera.cropFrameY = false;
        pixelPerfectCamera.stretchFill = false;
        
        Debug.Log($"[PixelPerfectSetup] Configured for {referenceResolutionX}x{referenceResolutionY} at {pixelsPerUnit} PPU");
    }
    
    void Start()
    {
        // Verify the setup
        float expectedCameraSize = (float)referenceResolutionY / (2f * pixelsPerUnit);
        Debug.Log($"[PixelPerfectSetup] Expected camera orthographic size: {expectedCameraSize}");
    }
}