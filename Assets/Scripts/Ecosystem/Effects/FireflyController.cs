using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using WegoSystem;

public class FireflyController : MonoBehaviour, ITickUpdateable
{
    [SerializeField] private FireflyDefinition definition;

    [SerializeField] private Light2D glowLight;
    [SerializeField] private Light2D groundLight;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem glowParticles;

    [SerializeField] private float groundLightRadiusMultiplier = 2.5f;

    private GridEntity gridEntity;

    private int lifetimeTicks;
    private int lastMovementTick = 0;

    private float currentLifetimeSeconds = 0f;
    private float maxLifetimeSeconds = 0f;

    private Vector3 currentTileCenter;
    private Vector3 localTargetPosition;
    private float currentLocalSpeed;

    private float baseGlowIntensity;
    private float currentGlowIntensity;
    private float baseGroundLightIntensity;
    private float baseGroundLightOuterRadius;
    private float glowFlickerTime;
    private Color originalColor;

    public bool IsAlive { get; set; } = true;
    public Transform AttractionTarget { get; set; }

    void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            Debug.LogError($"[FireflyController] Firefly prefab is missing the required GridEntity component!", this);
            enabled = false;
            return;
        }

        if (glowLight != null)
        {
            baseGlowIntensity = glowLight.intensity;
            currentGlowIntensity = glowLight.intensity;
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (groundLight != null)
        {
            baseGroundLightIntensity = groundLight.intensity;
            baseGroundLightOuterRadius = groundLight.pointLightOuterRadius;
        }
    }

    void Start()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }

        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }
    }

    public void Initialize()
    {
        // Access the public definition field directly from FireflyManager
        if (FireflyManager.Instance?.defaultFireflyDefinition == null || gridEntity == null)
        {
            Debug.LogError("[FireflyController] Initialization failed: Missing definition or GridEntity!", this);
            enabled = false;
            return;
        }

        definition = FireflyManager.Instance.defaultFireflyDefinition;

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            currentTileCenter = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
        }

        lifetimeTicks = Random.Range(definition.minLifetimeTicks, definition.maxLifetimeTicks + 1);
        currentLocalSpeed = Random.Range(definition.minLocalSpeed, definition.maxLocalSpeed);

        SetRandomLocalTarget();

        if (glowParticles != null)
        {
            glowParticles.Play();
        }

        if (TickManager.Instance?.Config != null)
        {
            maxLifetimeSeconds = lifetimeTicks / TickManager.Instance.Config.ticksPerRealSecond;
        }
        else
        {
            maxLifetimeSeconds = lifetimeTicks * 0.5f;
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        if (!IsAlive) return;

        currentLifetimeSeconds += Time.deltaTime;
        if (currentLifetimeSeconds >= maxLifetimeSeconds)
        {
            Die();
            return;
        }

        if (currentTick - lastMovementTick >= definition.movementTickInterval)
        {
            UpdateMovement();
            lastMovementTick = currentTick;
        }

        UpdatePhotosynthesisVisualization();
    }

    void Update()
    {
        if (!IsAlive) return;

        UpdateLocalMovement();
        UpdateGlowEffect();
        UpdateGroundLight();
    }

    void UpdateMovement()
    {
        FindAttractionTarget();

        if (AttractionTarget != null)
        {
            Vector3 attractionDirection = (AttractionTarget.position - currentTileCenter).normalized;
            localTargetPosition = currentTileCenter + attractionDirection * Random.Range(0.1f, 0.3f);
        }
        else
        {
            SetRandomLocalTarget();
        }
    }

    void UpdatePhotosynthesisVisualization()
    {
        if (GridDebugVisualizer.Instance != null && FireflyManager.Instance != null && gridEntity != null)
        {
            int photosynthesisRadius = Mathf.RoundToInt(FireflyManager.Instance.photosynthesisRadius);
            if (photosynthesisRadius > 0)
            {
                GridDebugVisualizer.Instance.VisualizeFireflyPhotosynthesisRadius(this, gridEntity.Position, photosynthesisRadius);
            }
        }
    }

    void FindAttractionTarget()
    {
        if (definition == null || gridEntity == null) return;

        float bestScore = 0f;
        Transform bestTarget = null;

        GridPosition currentPos = gridEntity.Position;
        int searchRadius = definition.tileSearchRadius;

        for (int x = currentPos.x - searchRadius; x <= currentPos.x + searchRadius; x++)
        {
            for (int y = currentPos.y - searchRadius; y <= currentPos.y + searchRadius; y++)
            {
                GridPosition checkPos = new GridPosition(x, y);
                if (!GridPositionManager.Instance.IsPositionValid(checkPos)) continue;

                // Fixed: Use proper distance calculation with ToVector2Int()
                float distance = Vector2.Distance(currentPos.ToVector2Int(), checkPos.ToVector2Int());
                if (distance > searchRadius) continue;

                // Fixed: Use GetEntitiesAt instead of GetEntitiesAtPosition
                var entitiesAtPosition = GridPositionManager.Instance.GetEntitiesAt(checkPos);
                foreach (var entity in entitiesAtPosition)
                {
                    var scentSources = entity.GetComponentsInChildren<ScentSource>();
                    foreach (var source in scentSources)
                    {
                        if (definition.attractiveScentDefinitions.Contains(source.Definition))
                        {
                            float score = definition.scentAttractionWeight / (distance + 1f);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestTarget = entity.transform;
                            }
                        }
                    }

                    // Check for growing plants
                    var plantGrowth = entity.GetComponent<PlantGrowth>();
                    if (plantGrowth != null && plantGrowth.CurrentState == PlantState.Growing)
                    {
                        float score = definition.growingPlantAttraction / (distance + 1f);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = entity.transform;
                        }
                    }
                }
            }
        }

        AttractionTarget = bestTarget;
    }

    void SetRandomLocalTarget()
    {
        if (definition == null) return;

        Vector2 randomOffset = new Vector2(
            Random.Range(-definition.flightBounds.x * 0.5f, definition.flightBounds.x * 0.5f),
            Random.Range(definition.flightHeightOffset, definition.flightHeightOffset + definition.flightBounds.y)
        );

        localTargetPosition = currentTileCenter + (Vector3)randomOffset;
    }

    void UpdateLocalMovement()
    {
        if (definition == null) return;

        transform.position = Vector3.MoveTowards(transform.position, localTargetPosition, currentLocalSpeed * Time.deltaTime);

        Vector3 direction = (localTargetPosition - transform.position).normalized;
        if (direction.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, definition.localMovementTurnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);
        }
    }

    void UpdateGlowEffect()
    {
        if (glowParticles == null) return;

        glowFlickerTime += Time.deltaTime * definition.glowFlickerSpeed;
        float flicker = Mathf.Sin(glowFlickerTime) * definition.glowFlickerAmount;
        currentGlowIntensity = Mathf.Clamp01(baseGlowIntensity + flicker);

        var emission = glowParticles.emission;
        emission.rateOverTime = currentGlowIntensity * 10f;
    }

    void UpdateGroundLight()
    {
        if (groundLight == null) return;

        glowFlickerTime += Time.deltaTime * definition.glowFlickerSpeed;
        float flicker = Mathf.Sin(glowFlickerTime) * definition.glowFlickerAmount;

        groundLight.intensity = Mathf.Clamp(baseGroundLightIntensity + flicker, definition.groundLightMinIntensity, 1f);
        groundLight.pointLightOuterRadius = baseGroundLightOuterRadius * groundLightRadiusMultiplier;
    }

    void Die()
    {
        IsAlive = false;

        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }

        if (FireflyManager.Instance != null)
        {
            FireflyManager.Instance.ReportFireflyDespawned(this);
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }

        if (glowParticles != null)
        {
            glowParticles.Stop();
            Destroy(gameObject, glowParticles.main.duration);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (definition == null) return;

        float tileSize = 1f;
        if (Application.isPlaying && GridPositionManager.Instance?.GetTilemapGrid() != null)
        {
            tileSize = GridPositionManager.Instance.GetTilemapGrid().cellSize.x;
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, definition.tileSearchRadius * tileSize);

        if (Application.isPlaying && FireflyManager.Instance != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, FireflyManager.Instance.photosynthesisRadius * tileSize);
        }

        if (Application.isPlaying)
        {
            Vector3 flightBoxCenter = currentTileCenter + new Vector3(0, definition.flightHeightOffset + definition.flightBounds.y / 2f, 0);
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(flightBoxCenter, new Vector3(definition.flightBounds.x, definition.flightBounds.y, 0.1f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, localTargetPosition);
        }
    }
}