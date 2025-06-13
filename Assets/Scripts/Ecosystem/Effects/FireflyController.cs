// Assets\Scripts\Ecosystem\Effects\FireflyController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(GridEntity))]
public class FireflyController : MonoBehaviour, ITickUpdateable
{
    [SerializeField] FireflyDefinition definition;
    [SerializeField] Light2D glowLight;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] TrailRenderer trailRenderer;
    [SerializeField] ParticleSystem glowParticles;

    GridEntity gridEntity;
    int currentAgeTicks = 0;
    int lifetimeTicks;
    int lastMovementTick = 0;
    int spawnEffectRemainingTicks = 0;

    Vector3 currentTileCenter;
    Vector3 localTargetPosition;
    float localMovementAngle;
    float currentLocalSpeed;
    float tileSize = 1f;
    float maxLocalOffset;

    float baseGlowIntensity;
    float currentGlowIntensity;
    float glowFlickerTime;

    Color originalColor;

    public bool IsAlive { get; private set; } = true;
    public Transform AttractionTarget { get; private set; }

    void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
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

        // Initialization is now called from the manager after instantiation
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
        if (definition == null)
        {
            Debug.LogError("[FireflyController] No FireflyDefinition assigned!", this);
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
        }

        UpdateTileCenter();
        localMovementAngle = Random.Range(0f, 360f);
        UpdateLocalTargetPosition();

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
        }

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

    void MakeMovementDecision()
    {
        GridPosition bestPosition = gridEntity.Position;
        float bestScore = EvaluateTile(gridEntity.Position);

        var nearbyPositions = GridRadiusUtility.GetTilesInCircle(gridEntity.Position, definition.tileSearchRadius, true);

        foreach (var pos in nearbyPositions)
        {
            if (!GridPositionManager.Instance.IsPositionValid(pos) ||
                GridPositionManager.Instance.IsPositionOccupied(pos))
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

        if (bestPosition != gridEntity.Position)
        {
            gridEntity.SetPosition(bestPosition);
            UpdateTileCenter();
        }
    }

    float EvaluateTile(GridPosition tilePos)
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
                    GridPosition scentGridPos = GridPositionManager.Instance.WorldToGrid(hit.transform.position);
                    int distance = tilePos.ManhattanDistance(scentGridPos);
                    float distanceScore = 1f - (distance / (float)definition.tileSearchRadius);
                    float scentScore = distanceScore * scent.EffectiveStrength;

                    if (scentScore > bestTargetScore)
                    {
                        bestTargetScore = scentScore;
                        bestTarget = hit.transform;
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

    void UpdateTileCenter()
    {
        if (GridPositionManager.Instance != null)
        {
            currentTileCenter = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
        }
    }

    void UpdateLocalTargetPosition()
    {
        localMovementAngle += Random.Range(-definition.localMovementTurnSpeed, definition.localMovementTurnSpeed) * Time.deltaTime;

        Vector2 direction = new Vector2(Mathf.Cos(localMovementAngle * Mathf.Deg2Rad),
            Mathf.Sin(localMovementAngle * Mathf.Deg2Rad));

        float targetDistance = Random.Range(maxLocalOffset * 0.5f, maxLocalOffset);
        localTargetPosition = currentTileCenter + (Vector3)(direction * targetDistance);

        currentLocalSpeed = Random.Range(definition.minLocalSpeed, definition.maxLocalSpeed);
    }

    void UpdateLocalMovement()
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

    void UpdateLifetimeFade()
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

    void ApplyVisualState(float intensity)
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

    void HandleGlowAndFlicker()
    {
        if (glowLight == null || definition.glowFlickerAmount <= 0f) return;

        glowFlickerTime += Time.deltaTime * definition.glowFlickerSpeed;
        float flicker = Mathf.PerlinNoise(glowFlickerTime, 0f) * 2f - 1f;
        glowLight.intensity = currentGlowIntensity + (flicker * definition.glowFlickerAmount);
    }

    void Die()
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