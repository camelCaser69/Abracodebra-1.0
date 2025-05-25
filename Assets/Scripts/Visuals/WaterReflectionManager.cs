// FILE: Assets/Scripts/Visuals/WaterReflectionManager.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class WaterReflectionManager : MonoBehaviour
{
    public static WaterReflectionManager Instance { get; private set; }

    [Header("Reflection Settings")]
    [Tooltip("Layers to include in reflections")]
    public LayerMask reflectionLayers = -1;
    
    [Tooltip("Resolution divisor for reflection texture (higher = lower quality but better performance)")]
    [Range(1, 4)]
    public int resolutionDivisor = 2;
    
    [Tooltip("Pixel perfect snapping for reflections")]
    public bool pixelPerfectReflections = true;
    
    [Tooltip("Pixels per unit for pixel snapping")]
    public float pixelsPerUnit = 16f;

    [Header("Water Tilemap References")]
    [Tooltip("The water tilemap renderer that will receive reflections")]
    public TilemapRenderer waterTilemapRenderer;
    
    [Header("Reflection Appearance")]
    [Range(0f, 1f)]
    [Tooltip("Overall reflection intensity")]
    public float reflectionIntensity = 0.7f;
    
    [Tooltip("Tint color for reflections")]
    public Color reflectionTint = new Color(0.8f, 0.9f, 1f, 1f);
    
    [Tooltip("Vertical offset for reflection (positive = lower reflection)")]
    public float reflectionOffsetY = 0.1f;
    
    [Tooltip("Add ripple distortion to reflections")]
    public bool enableRipples = true;
    
    [Range(0f, 0.1f)]
    [Tooltip("Ripple distortion strength")]
    public float rippleStrength = 0.02f;
    
    [Range(0.1f, 10f)]
    [Tooltip("Ripple animation speed")]
    public float rippleSpeed = 2f;

    [Header("Performance")]
    [Tooltip("Update reflection every N frames (1 = every frame)")]
    [Range(1, 5)]
    public int updateInterval = 1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Private members
    private Camera reflectionCamera;
    private Camera mainCamera;
    private RenderTexture reflectionRT;
    private Material waterMaterial;
    private int frameCounter = 0;
    
    // Shader property IDs for performance
    private static readonly int ReflectionTexID = Shader.PropertyToID("_ReflectionTex");
    private static readonly int ReflectionIntensityID = Shader.PropertyToID("_ReflectionIntensity");
    private static readonly int ReflectionTintID = Shader.PropertyToID("_ReflectionTint");
    private static readonly int ReflectionOffsetYID = Shader.PropertyToID("_ReflectionOffsetY");
    private static readonly int RippleStrengthID = Shader.PropertyToID("_RippleStrength");
    private static readonly int RippleSpeedID = Shader.PropertyToID("_RippleSpeed");
    private static readonly int EnableRipplesID = Shader.PropertyToID("_EnableRipples");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        reflectionCamera = GetComponent<Camera>();
        if (reflectionCamera == null)
        {
            Debug.LogError("[WaterReflectionManager] No Camera component found!", this);
            enabled = false;
            return;
        }
        
        // Configure reflection camera
        reflectionCamera.enabled = false; // We'll render manually
        reflectionCamera.cullingMask = reflectionLayers;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[WaterReflectionManager] No main camera found!", this);
            enabled = false;
            return;
        }
        
        if (waterTilemapRenderer == null)
        {
            // Try to find water tilemap
            FindWaterTilemap();
        }
        
        SetupReflectionTexture();
        SetupWaterMaterial();
    }
    
    void FindWaterTilemap()
    {
        if (TileInteractionManager.Instance != null)
        {
            foreach (var mapping in TileInteractionManager.Instance.tileDefinitionMappings)
            {
                if (mapping.tileDef != null && mapping.tileDef.displayName.ToLower().Contains("water"))
                {
                    Transform renderTilemap = mapping.tilemapModule.transform.Find("RenderTilemap");
                    if (renderTilemap != null)
                    {
                        waterTilemapRenderer = renderTilemap.GetComponent<TilemapRenderer>();
                        if (waterTilemapRenderer != null)
                        {
                            Debug.Log($"[WaterReflectionManager] Found water tilemap: {renderTilemap.name}");
                            break;
                        }
                    }
                }
            }
        }
        
        if (waterTilemapRenderer == null)
        {
            Debug.LogError("[WaterReflectionManager] Could not find water tilemap renderer!");
        }
    }
    
    void SetupReflectionTexture()
    {
        int width = Screen.width / resolutionDivisor;
        int height = Screen.height / resolutionDivisor;
        
        // For pixel art, we want point filtering
        reflectionRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        reflectionRT.filterMode = FilterMode.Point;
        reflectionRT.wrapMode = TextureWrapMode.Clamp;
        reflectionRT.name = "Water Reflection RT";
        
        reflectionCamera.targetTexture = reflectionRT;
        
        if (showDebugInfo)
            Debug.Log($"[WaterReflectionManager] Created reflection RT: {width}x{height}");
    }
    
    void SetupWaterMaterial()
    {
        if (waterTilemapRenderer == null) return;
        
        // Create material with our water shader
        Shader waterShader = Shader.Find("Sprites/WaterReflection");
        if (waterShader == null)
        {
            Debug.LogError("[WaterReflectionManager] Water shader 'Sprites/WaterReflection' not found!");
            return;
        }
        
        waterMaterial = new Material(waterShader);
        waterMaterial.name = "Water Reflection Material";
        
        // Apply to tilemap renderer
        waterTilemapRenderer.material = waterMaterial;
        
        UpdateMaterialProperties();
    }
    
    void UpdateMaterialProperties()
    {
        if (waterMaterial == null) return;
        
        waterMaterial.SetTexture(ReflectionTexID, reflectionRT);
        waterMaterial.SetFloat(ReflectionIntensityID, reflectionIntensity);
        waterMaterial.SetColor(ReflectionTintID, reflectionTint);
        waterMaterial.SetFloat(ReflectionOffsetYID, reflectionOffsetY);
        waterMaterial.SetFloat(RippleStrengthID, rippleStrength);
        waterMaterial.SetFloat(RippleSpeedID, rippleSpeed);
        waterMaterial.SetFloat(EnableRipplesID, enableRipples ? 1f : 0f);
    }

    void LateUpdate()
    {
        if (mainCamera == null || reflectionCamera == null || waterMaterial == null) return;
        
        frameCounter++;
        if (frameCounter % updateInterval != 0) return;
        
        UpdateReflectionCamera();
        RenderReflection();
        UpdateMaterialProperties();
    }
    
    void UpdateReflectionCamera()
    {
        // Copy main camera settings
        reflectionCamera.orthographic = mainCamera.orthographic;
        reflectionCamera.orthographicSize = mainCamera.orthographicSize;
        reflectionCamera.aspect = mainCamera.aspect;
        reflectionCamera.nearClipPlane = mainCamera.nearClipPlane;
        reflectionCamera.farClipPlane = mainCamera.farClipPlane;
        
        // Position reflection camera at the same position as main camera
        Vector3 pos = mainCamera.transform.position;
        
        // Pixel perfect snapping if enabled
        if (pixelPerfectReflections)
        {
            float snapValue = 1f / pixelsPerUnit;
            pos.x = Mathf.Round(pos.x / snapValue) * snapValue;
            pos.y = Mathf.Round(pos.y / snapValue) * snapValue;
        }
        
        reflectionCamera.transform.position = pos;
        reflectionCamera.transform.rotation = mainCamera.transform.rotation;
    }
    
    void RenderReflection()
    {
        // Temporarily flip the culling
        GL.invertCulling = true;
        
        // Use a flipped projection matrix for reflection
        Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1, -1, 1));
        reflectionCamera.projectionMatrix = mainCamera.projectionMatrix * scale;
        
        // Render
        reflectionCamera.Render();
        
        // Restore culling
        GL.invertCulling = false;
    }
    
    void OnDestroy()
    {
        if (reflectionRT != null)
        {
            reflectionRT.Release();
            Destroy(reflectionRT);
        }
        
        if (waterMaterial != null)
        {
            Destroy(waterMaterial);
        }
        
        if (Instance == this)
            Instance = null;
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && waterMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }
    
    // Public method to force update
    public void ForceUpdateReflection()
    {
        if (enabled && mainCamera != null && reflectionCamera != null)
        {
            UpdateReflectionCamera();
            RenderReflection();
            UpdateMaterialProperties();
        }
    }
}