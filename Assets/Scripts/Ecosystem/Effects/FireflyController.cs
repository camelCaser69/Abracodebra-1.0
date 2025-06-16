using UnityEngine;
using WegoSystem;
using UnityEngine.Rendering.Universal;

public class FireflyController : MonoBehaviour, ITickUpdateable
{
    [Header("Core Configuration")]
    [SerializeField] private FireflyDefinition definition;

    [Header("Visual Components")]
    [SerializeField] private Light2D glowLight;
    [SerializeField] private Light2D groundLight;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem glowParticles;

    [Header("Ground Light Effect")]
    [SerializeField] private float groundLightMaxHeight = 0.8f;
    [SerializeField] private float groundLightRadiusMultiplier = 2.5f;
    
    private GridEntity gridEntity;

    private int currentAgeTicks = 0;
    private int lifetimeTicks;
    private int lastMovementTick = 0;
    private int spawnEffectRemainingTicks = 0;

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

        if (definition.useSpawnEffect)
        {
            spawnEffectRemainingTicks = definition.spawnEffectTicks;
        }

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

        currentAgeTicks++;

        if (currentAgeTicks >= lifetimeTicks)
        {
            Die();
            return;
        }

        UpdateLifetimeVisuals();

        if (spawnEffectRemainingTicks > 0)
        {
            spawnEffectRemainingTicks--;
        }

        if (currentTick - lastMovementTick >= definition.movementTickInterval)
        {
            MakeMovementDecision();
            lastMovementTick = currentTick;
        }
    }

    void Update()
    {
        if (!IsAlive) return;
        UpdateLocalMovement();
        HandleGlowAndFlicker();
    }

    private void MakeMovementDecision()
    {
        if (gridEntity == null) return;
        
        GridPosition currentPos = gridEntity.Position;
        GridPosition bestPosition = currentPos;
        float bestScore = EvaluateTile(currentPos);

        var nearbyPositions = GridRadiusUtility.GetTilesInCircle(currentPos, definition.tileSearchRadius, true);

        foreach (var pos in nearbyPositions)
        {
            if (!GridPositionManager.Instance.IsPositionValid(pos))
            {
                continue;
            }

            float score = EvaluateTile(pos);
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = pos;
            }
        }

        if (bestPosition != currentPos)
        {
            gridEntity.SetPosition(bestPosition);
            UpdateTileCenter();
        }
    }

    private float EvaluateTile(GridPosition tilePos)
    {
        float score = Random.Range(0f, 1f);
        var tilesInRange = GridRadiusUtility.GetTilesInCircle(tilePos, definition.tileSearchRadius);
        Transform bestTarget = null;
        float bestTargetScore = 0f;

        foreach (var checkPos in tilesInRange)
        {
            Vector3 worldPos = GridPositionManager.Instance.GridToWorld(checkPos);
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

            foreach (var hit in hits)
            {
                ScentSource scent = hit.GetComponent<ScentSource>();
                if (scent != null && scent.definition != null &&
                    definition.attractiveScentDefinitions.Contains(scent.definition))
                {
                    GridEntity scentEntity = scent.GetComponent<GridEntity>();
                    if (scentEntity != null) {
                        int distance = tilePos.ManhattanDistance(scentEntity.Position);
                        float distanceScore = 1f - (distance / (float)definition.tileSearchRadius);
                        float scentScore = distanceScore * scent.EffectiveStrength;
                        if (scentScore > bestTargetScore)
                        {
                            bestTargetScore = scentScore;
                            bestTarget = hit.transform;
                        }
                    }
                }
                PlantGrowth plant = hit.GetComponent<PlantGrowth>();
                if (plant != null && plant.CurrentState == PlantState.Growing)
                {
                    score += definition.growingPlantAttraction;
                }
            }
        }
        this.AttractionTarget = bestTarget;
        score += bestTargetScore * definition.scentAttractionWeight;
        return score;
    }

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
        Vector3 flightBoxCenter = currentTileCenter + new Vector3(0, definition.flightBounds.y / 2f, 0);
        float randomX = Random.Range(-definition.flightBounds.x / 2f, definition.flightBounds.x / 2f);
        float randomY = Random.Range(-definition.flightBounds.y / 2f, definition.flightBounds.y / 2f);
        localTargetPosition = flightBoxCenter + new Vector3(randomX, randomY, 0);
        currentLocalSpeed = Random.Range(definition.minLocalSpeed, definition.maxLocalSpeed);
    }

    private void UpdateLocalMovement()
    {
        if (definition == null) return;
        Vector3 toTarget = localTargetPosition - transform.position;
        if (toTarget.magnitude < 0.1f)
        {
            UpdateLocalTargetPosition();
        }
        else
        {
            transform.position += toTarget.normalized * currentLocalSpeed * Time.deltaTime;
        }
        Vector3 flightBoxCenter = currentTileCenter + new Vector3(0, definition.flightBounds.y / 2f, 0);
        Vector3 positionRelativeToFlightBox = transform.position - flightBoxCenter;
        positionRelativeToFlightBox.x = Mathf.Clamp(positionRelativeToFlightBox.x, -definition.flightBounds.x / 2f, definition.flightBounds.x / 2f);
        positionRelativeToFlightBox.y = Mathf.Clamp(positionRelativeToFlightBox.y, -definition.flightBounds.y / 2f, definition.flightBounds.y / 2f);
        transform.position = flightBoxCenter + positionRelativeToFlightBox;
    }

    private void UpdateLifetimeVisuals()
    {
        float overallIntensity = 1f;
        if (currentAgeTicks < definition.fadeInTicks)
        {
            overallIntensity = (float)currentAgeTicks / definition.fadeInTicks;
        }
        else if (currentAgeTicks > lifetimeTicks - definition.fadeOutTicks)
        {
            int fadeOutTicksElapsed = currentAgeTicks - (lifetimeTicks - definition.fadeOutTicks);
            overallIntensity = 1f - ((float)fadeOutTicksElapsed / definition.fadeOutTicks);
        }
        ApplyVisualState(overallIntensity);
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
        UpdateGroundLightEffect(intensity);
    }
    
    private void UpdateGroundLightEffect(float overallIntensity)
    {
        if (groundLight == null) return;
        groundLight.transform.position = currentTileCenter;
        float height = Mathf.Max(0, transform.position.y - currentTileCenter.y);
        float t = Mathf.Clamp01(height / groundLightMaxHeight);
        float heightAdjustedIntensity = Mathf.Lerp(baseGroundLightIntensity, 0f, t);
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

    void OnDrawGizmosSelected()
    {
        if (definition == null) return;
        
        float tileSize = 1f; // Assuming default for gizmos
        if (Application.isPlaying && GridPositionManager.Instance?.GetTilemapGrid() != null)
        {
            tileSize = GridPositionManager.Instance.GetTilemapGrid().cellSize.x;
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, definition.tileSearchRadius * tileSize);

        if (Application.isPlaying)
        {
            Vector3 flightBoxCenter = currentTileCenter + new Vector3(0, definition.flightBounds.y / 2f, 0);
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(flightBoxCenter, new Vector3(definition.flightBounds.x, definition.flightBounds.y, 0.1f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, localTargetPosition);
        }
    }
}