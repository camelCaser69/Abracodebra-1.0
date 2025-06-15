using UnityEngine;
using WegoSystem;
using UnityEngine.Rendering.Universal;

public class FireflyController : MonoBehaviour, ITickUpdateable
{
    [SerializeField] private FireflyDefinition definition;
    [SerializeField] private Light2D glowLight;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem glowParticles;
    
    // The firefly now uses its GridEntity component for all position and movement logic
    private GridEntity gridEntity;
    
    private int currentAgeTicks = 0;
    private int lifetimeTicks;
    private int lastMovementTick = 0;
    private int spawnEffectRemainingTicks = 0;

    private Vector3 currentTileCenter;
    private Vector3 localTargetPosition;
    private float localMovementAngle;
    private float currentLocalSpeed;
    private float tileSize = 1f;
    private float maxLocalOffset;

    private float baseGlowIntensity;
    private float currentGlowIntensity;
    private float glowFlickerTime;

    private Color originalColor;

    public bool IsAlive { get; set; } = true;
    public Transform AttractionTarget { get; set; }

    void Awake()
    {
        // Get the GridEntity component, which is now required.
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
            currentGlowIntensity = baseGlowIntensity;
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
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
            var grid = GridPositionManager.Instance.GetTilemapGrid();
            if (grid != null)
            {
                tileSize = grid.cellSize.x; // Assuming square tiles
                maxLocalOffset = tileSize * definition.localMovementRadius;
            }
            // Snap the entity to the grid initially to set its starting position.
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
        }

        UpdateTileCenter();
        localMovementAngle = Random.Range(0f, 360f);
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

        UpdateLifetimeFade();

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
            // CRITICAL FIX: Call SetPosition on the GridEntity to trigger the smooth move.
            gridEntity.SetPosition(bestPosition);
            UpdateTileCenter();
        }
    }

    private float EvaluateTile(GridPosition tilePos)
    {
        float score = Random.Range(0f, 1f); // Base randomness

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
        localMovementAngle += Random.Range(-definition.localMovementTurnSpeed, definition.localMovementTurnSpeed) * Time.deltaTime;

        Vector2 direction = new Vector2(Mathf.Cos(localMovementAngle * Mathf.Deg2Rad),
                                        Mathf.Sin(localMovementAngle * Mathf.Deg2Rad));

        float targetDistance = Random.Range(maxLocalOffset * 0.5f, maxLocalOffset);
        localTargetPosition = currentTileCenter + (Vector3)(direction * targetDistance);

        currentLocalSpeed = Random.Range(definition.minLocalSpeed, definition.maxLocalSpeed);
    }

    private void UpdateLocalMovement()
    {
        Vector3 toTarget = localTargetPosition - transform.position;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget < 0.1f)
        {
            UpdateLocalTargetPosition();
        }
        else
        {
            Vector3 movement = toTarget.normalized * currentLocalSpeed * Time.deltaTime;
            Vector3 newPosition = transform.position + movement;
            Vector3 fromCenter = newPosition - currentTileCenter;

            if (fromCenter.magnitude > maxLocalOffset)
            {
                fromCenter = fromCenter.normalized * maxLocalOffset;
                newPosition = currentTileCenter + fromCenter;
                UpdateLocalTargetPosition();
            }
            transform.position = newPosition;
        }
    }

    private void UpdateLifetimeFade()
    {
        if (currentAgeTicks < definition.fadeInTicks)
        {
            float fadeProgress = (float)currentAgeTicks / definition.fadeInTicks;
            ApplyVisualState(fadeProgress);
        }
        else if (currentAgeTicks > lifetimeTicks - definition.fadeOutTicks)
        {
            int fadeOutTicksElapsed = currentAgeTicks - (lifetimeTicks - definition.fadeOutTicks);
            float fadeProgress = 1f - ((float)fadeOutTicksElapsed / definition.fadeOutTicks);
            ApplyVisualState(fadeProgress);
        }
        else
        {
            ApplyVisualState(1f);
        }
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

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, definition.tileSearchRadius * tileSize);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(currentTileCenter, maxLocalOffset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, localTargetPosition);
    }
}