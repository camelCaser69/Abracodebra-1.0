using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using WegoSystem;

public class FireflyController : MonoBehaviour, ITickUpdateable
{
    #region Fields

    [SerializeField]
    private FireflyDefinition definition;

    [Header("Component References")]
    [SerializeField] private Light2D glowLight;
    [SerializeField] private Light2D groundLight;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem glowParticles;

    [Header("Visual Effects")]
    [SerializeField] private float groundLightRadiusMultiplier = 2.5f;

    private GridEntity gridEntity;

    // State
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

    #endregion

    #region Unity Lifecycle

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
    }

    public void Initialize()
    {
        if (definition == null || gridEntity == null)
        {
            Debug.LogError("[FireflyController] Initialization failed: Missing definition or GridEntity!", this);
            enabled = false;
            return;
        }

        lifetimeTicks = Random.Range(definition.minLifetimeTicks, definition.maxLifetimeTicks + 1);
        if (TickManager.Instance?.Config != null)
        {
            maxLifetimeSeconds = TickManager.Instance.Config.ConvertTicksToSeconds(lifetimeTicks);
        }
        else
        {
            maxLifetimeSeconds = lifetimeTicks * 0.5f; // Fallback
        }
        currentLifetimeSeconds = 0f;
        
        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
        }

        UpdateTileCenter();
        UpdateLocalTargetPosition();
        ApplyVisualState(0f);
    }

    public void OnTickUpdate(int currentTick)
    {
        if (!IsAlive) return;

        if (currentTick - lastMovementTick >= definition.movementTickInterval)
        {
            MakeMovementDecision();
            lastMovementTick = currentTick;
        }
    }

    void Update()
    {
        if (!IsAlive) return;
        
        currentLifetimeSeconds += Time.deltaTime;
        if (currentLifetimeSeconds >= maxLifetimeSeconds)
        {
            Die();
            return;
        }

        UpdateLifetimeVisuals();
        UpdateLocalMovement();
        HandleGlowAndFlicker();
    }

    #endregion

    #region AI & Movement

    private void MakeMovementDecision()
    {
        if (gridEntity == null) return;

        if (AttractionTarget != null && !AttractionTarget.gameObject.activeInHierarchy)
        {
            AttractionTarget = null;
        }

        if (AttractionTarget == null)
        {
            FindNewAttractionTarget();
        }

        if (AttractionTarget != null)
        {
            PlanMovementTowardTarget();
        }
        else
        {
            WanderRandomly();
        }
    }

    private void FindNewAttractionTarget()
    {
        if (gridEntity == null) return;

        var tilesToSearch = GridRadiusUtility.GetTilesInCircle(gridEntity.Position, definition.tileSearchRadius, true);
        Transform bestTarget = null;
        float bestScore = -1f;

        foreach (var tilePos in tilesToSearch)
        {
            var entitiesAtTile = GridPositionManager.Instance.GetEntitiesAt(tilePos);
            if (entitiesAtTile == null) continue;

            foreach (var entity in entitiesAtTile)
            {
                if (entity == null) continue;

                float currentScore = -1f;
                int distance = gridEntity.Position.ManhattanDistance(entity.Position);
                if (distance == 0) distance = 1;

                ScentSource scent = entity.GetComponent<ScentSource>();
                if (scent != null && scent.definition != null && definition.attractiveScentDefinitions.Contains(scent.definition))
                {
                    currentScore = (scent.EffectiveStrength * definition.scentAttractionWeight) / distance;
                }

                PlantGrowth plant = entity.GetComponent<PlantGrowth>();
                if (plant != null && plant.CurrentState == PlantState.Growing)
                {
                    float plantScore = definition.growingPlantAttraction / distance;
                    if (plantScore > currentScore)
                    {
                        currentScore = plantScore;
                    }
                }

                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestTarget = entity.transform;
                }
            }
        }
        AttractionTarget = bestTarget;
    }

    private void PlanMovementTowardTarget()
    {
        if (gridEntity == null || AttractionTarget == null) return;

        GridEntity targetEntity = AttractionTarget.GetComponent<GridEntity>();
        if (targetEntity == null)
        {
            WanderRandomly();
            return;
        }

        GridPosition currentPos = gridEntity.Position;
        GridPosition targetPos = targetEntity.Position;
        if (currentPos.ManhattanDistance(targetPos) <= 1) return;

        GridPosition bestMove = GridPosition.Zero;
        int bestDistance = int.MaxValue;
        GridPosition[] directions = { GridPosition.Up, GridPosition.Down, GridPosition.Left, GridPosition.Right };

        foreach (var dir in directions)
        {
            GridPosition nextPos = currentPos + dir;
            if (GridPositionManager.Instance.IsPositionValid(nextPos))
            {
                int newDist = nextPos.ManhattanDistance(targetPos);
                if (newDist < bestDistance)
                {
                    bestDistance = newDist;
                    bestMove = dir;
                }
            }
        }

        if (bestMove != GridPosition.Zero)
        {
            gridEntity.SetPosition(currentPos + bestMove);
            UpdateTileCenter();
        }
        else
        {
            WanderRandomly();
        }
    }

    private void WanderRandomly()
    {
        if (gridEntity == null) return;

        GridPosition currentPos = gridEntity.Position;
        var validMoves = new List<GridPosition>();
        GridPosition[] directions = { GridPosition.Up, GridPosition.Down, GridPosition.Left, GridPosition.Right };

        foreach (var dir in directions)
        {
            GridPosition nextPos = currentPos + dir;
            if (GridPositionManager.Instance.IsPositionValid(nextPos))
            {
                validMoves.Add(nextPos);
            }
        }

        if (validMoves.Count > 0)
        {
            GridPosition newPos = validMoves[Random.Range(0, validMoves.Count)];
            gridEntity.SetPosition(newPos);
            UpdateTileCenter();
        }
    }

    #endregion

    #region Visuals & Effects

    private void UpdateTileCenter()
    {
        if (GridPositionManager.Instance != null && gridEntity != null)
        {
            currentTileCenter = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
        }
    }

    private void UpdateLocalTargetPosition()
    {
        if (definition == null) return;
        Vector3 flightBoxCenter = currentTileCenter + new Vector3(0, definition.flightHeightOffset + definition.flightBounds.y / 2f, 0);
        float randomX = Random.Range(-definition.flightBounds.x / 2f, definition.flightBounds.x / 2f);
        float randomY = Random.Range(-definition.flightBounds.y / 2f, definition.flightBounds.y / 2f);
        localTargetPosition = flightBoxCenter + new Vector3(randomX, randomY, 0);
        currentLocalSpeed = Random.Range(definition.minLocalSpeed, definition.maxLocalSpeed);
    }
    
    private void UpdateLocalMovement()
    {
        if (definition == null) return;

        // Move towards the current local target
        Vector3 direction = (localTargetPosition - transform.position).normalized;
        transform.position += direction * currentLocalSpeed * Time.deltaTime;

        // After moving, check if we've arrived. If so, get a new target for the next frame.
        if ((localTargetPosition - transform.position).sqrMagnitude < 0.01f)
        {
            UpdateLocalTargetPosition();
        }
        
        // Clamp position to ensure it stays within the flight volume
        Vector3 flightBoxCenter = currentTileCenter + new Vector3(0, definition.flightHeightOffset + definition.flightBounds.y / 2f, 0);
        Vector3 halfBounds = new Vector3(definition.flightBounds.x / 2f, definition.flightBounds.y / 2f, 0);
        Vector3 minBounds = flightBoxCenter - halfBounds;
        Vector3 maxBounds = flightBoxCenter + halfBounds;

        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y),
            transform.position.z
        );
    }

    private void UpdateLifetimeVisuals()
    {
        float overallIntensity = 1f;

        if (currentLifetimeSeconds < definition.fadeInSeconds)
        {
            overallIntensity = Mathf.Clamp01(currentLifetimeSeconds / definition.fadeInSeconds);
        }
        else if (maxLifetimeSeconds > 0 && currentLifetimeSeconds > maxLifetimeSeconds - definition.fadeOutSeconds)
        {
            float timeIntoFadeOut = currentLifetimeSeconds - (maxLifetimeSeconds - definition.fadeOutSeconds);
            overallIntensity = 1f - Mathf.Clamp01(timeIntoFadeOut / definition.fadeOutSeconds);
        }
        
        ApplyVisualState(overallIntensity);
        UpdateGroundLightEffect(overallIntensity);
    }

    private void ApplyVisualState(float intensity)
    {
        if (spriteRenderer != null)
        {
            Color color = originalColor;
            color.a = originalColor.a * intensity;
            spriteRenderer.color = color;
        }
        if (glowLight != null)
        {
            glowLight.intensity = baseGlowIntensity * intensity;
            currentGlowIntensity = glowLight.intensity;
        }
        if (trailRenderer != null)
        {
            Color startColor = trailRenderer.startColor;
            Color endColor = trailRenderer.endColor;
            startColor.a = intensity * 0.5f;
            endColor.a = 0f;
            trailRenderer.startColor = startColor;
            trailRenderer.endColor = endColor;
        }
        if (glowParticles != null)
        {
            var main = glowParticles.main;
            Color particleColor = main.startColor.color;
            particleColor.a = intensity * 0.7f;
            main.startColor = particleColor;
        }
    }

    private void UpdateGroundLightEffect(float overallIntensity)
    {
        if (groundLight == null || definition == null) return;

        // Horizontally follow the firefly, but stay vertically on the tile's ground plane
        Vector3 lightPosition = groundLight.transform.position;
        lightPosition.x = transform.position.x;
        lightPosition.y = currentTileCenter.y;
        groundLight.transform.position = lightPosition;

        // Define the vertical flight range based on the definition
        float lowestFlightPointY = currentTileCenter.y + definition.flightHeightOffset;
        float verticalFlightRange = definition.flightBounds.y;

        // Calculate normalized progress (t) within the vertical flight range
        float currentHeightInBox = transform.position.y - lowestFlightPointY;
        float t = 0f;
        if (verticalFlightRange > 0.001f)
        {
            t = Mathf.Clamp01(currentHeightInBox / verticalFlightRange);
        }

        // Determine min and max intensity for interpolation
        float minIntensityAtMaxHeight = baseGroundLightIntensity * definition.groundLightMinIntensity;
        
        // Interpolate intensity and radius based on t
        float heightAdjustedIntensity = Mathf.Lerp(baseGroundLightIntensity, minIntensityAtMaxHeight, t);
        float heightAdjustedRadius = Mathf.Lerp(baseGroundLightOuterRadius, baseGroundLightOuterRadius * groundLightRadiusMultiplier, t);

        groundLight.intensity = heightAdjustedIntensity * overallIntensity;
        groundLight.pointLightOuterRadius = heightAdjustedRadius;
        groundLight.enabled = spriteRenderer != null && spriteRenderer.enabled;
    }

    private void HandleGlowAndFlicker()
    {
        if (glowLight == null || definition.glowFlickerAmount <= 0f) return;
        glowFlickerTime += Time.deltaTime * definition.glowFlickerSpeed;
        float flicker = Mathf.PerlinNoise(glowFlickerTime, 0f) * 2f - 1f;
        glowLight.intensity = currentGlowIntensity + (flicker * definition.glowFlickerAmount);
    }

    private void Die()
    {
        IsAlive = false;
        if (FireflyManager.Instance != null) FireflyManager.Instance.ReportFireflyDespawned(this);
        if (TickManager.Instance != null) TickManager.Instance.UnregisterTickUpdateable(this);

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

    private void OnDrawGizmosSelected()
    {
        if (definition == null) return;

        float tileSize = 1f;
        if (Application.isPlaying && GridPositionManager.Instance?.GetTilemapGrid() != null)
        {
            tileSize = GridPositionManager.Instance.GetTilemapGrid().cellSize.x;
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, definition.tileSearchRadius * tileSize);

        if (Application.isPlaying)
        {
            Vector3 flightBoxCenter = currentTileCenter + new Vector3(0, definition.flightHeightOffset + definition.flightBounds.y / 2f, 0);
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(flightBoxCenter, new Vector3(definition.flightBounds.x, definition.flightBounds.y, 0.1f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, localTargetPosition);
        }
    }

    #endregion
}