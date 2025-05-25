// FILE: Assets/Scripts/Visuals/WaterReflectionCameraSetup.cs
using UnityEngine;

/// <summary>
/// Helper script to automatically set up the water reflection system
/// </summary>
public class WaterReflectionCameraSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Header("Layer Configuration")]
    [Tooltip("Layers to exclude from reflections (like water itself)")]
    [SerializeField] private string[] excludeLayers = new string[] { "Water", "UI" };
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupReflectionSystem();
        }
    }
    
    public void SetupReflectionSystem()
    {
        // Check if system already exists
        if (WaterReflectionManager.Instance != null)
        {
            Debug.Log("Water reflection system already exists.");
            return;
        }
        
        // Create reflection camera object
        GameObject reflectionCameraObj = new GameObject("Water Reflection Camera");
        Camera reflectionCamera = reflectionCameraObj.AddComponent<Camera>();
        WaterReflectionManager manager = reflectionCameraObj.AddComponent<WaterReflectionManager>();
        
        // Configure camera layers
        LayerMask reflectionMask = -1; // Start with all layers
        
        // Exclude specified layers
        foreach (string layerName in excludeLayers)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1)
            {
                reflectionMask &= ~(1 << layer);
            }
        }
        
        manager.reflectionLayers = reflectionMask;
        
        // Set pixel art friendly defaults
        manager.resolutionDivisor = 2;
        manager.pixelPerfectReflections = true;
        manager.pixelsPerUnit = 16f;
        manager.reflectionIntensity = 0.6f;
        manager.enableRipples = true;
        manager.rippleStrength = 0.015f;
        
        Debug.Log("Water reflection system created successfully!");
    }
}