using UnityEngine;
using WegoSystem;

public class ScentSource : MonoBehaviour
{
    [SerializeField] ScentDefinition definition;
    [SerializeField] float radiusModifier = 0f;
    [SerializeField] float strengthModifier = 0f;

    public ScentDefinition Definition => definition;
    public float EffectiveRadius => definition != null ? Mathf.Max(0f, definition.baseRadius + radiusModifier) : 0f;
    public float EffectiveStrength => definition != null ? Mathf.Max(0f, definition.baseStrength + strengthModifier) : 0f;

    GridEntity gridEntity;

    void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
    }

    private void Start()
    {
        // ADDED CHECK: Only snap to grid if this is a standalone entity.
        // If it's part of a plant, the parent PlantGrowth object handles positioning.
        if (GetComponentInParent<PlantGrowth>() == null)
        {
            if (GridPositionManager.Instance != null)
            {
                GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            }
        }
    }

    void Update()
    {
        UpdateRadiusVisualization();
    }

    void UpdateRadiusVisualization()
    {
        if (GridDebugVisualizer.Instance != null && definition != null && gridEntity != null)
        {
            float effectiveRadius = EffectiveRadius;
            if (effectiveRadius > 0.01f)
            {
                int radiusTiles = Mathf.RoundToInt(effectiveRadius);
                GridDebugVisualizer.Instance.VisualizeScentRadius(this, gridEntity.Position, radiusTiles);
            }
            else
            {
                GridDebugVisualizer.Instance.HideContinuousRadius(this);
            }
        }
    }

    public void SetDefinition(ScentDefinition newDefinition)
    {
        definition = newDefinition;
        UpdateRadiusVisualization(); // Update visualization when definition changes
    }

    public void SetRadiusModifier(float modifier)
    {
        radiusModifier = modifier;
    }

    public void SetStrengthModifier(float modifier)
    {
        strengthModifier = modifier;
    }

    public void ApplyModifiers(float radiusMod, float strengthMod)
    {
        radiusModifier += radiusMod;
        strengthModifier += strengthMod;
    }

    void OnDestroy()
    {
        // Clean up radius visualization when scent source is destroyed
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }
    }

    void OnDisable()
    {
        // Hide visualization when disabled
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }
    }

    void OnEnable()
    {
        // Show visualization when enabled (if appropriate)
        UpdateRadiusVisualization();
    }
}