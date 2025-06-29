using UnityEngine;
using WegoSystem;

public class PoopController : MonoBehaviour, ITickUpdateable
{
    [SerializeField] int lifetimeTicks = 20;

    [SerializeField] float fadeRealTimeDuration = 1f;

    GridEntity gridEntity;
    SpriteRenderer spriteRenderer;
    int currentLifetimeTicks;
    bool isFading = false;
    float fadeTimer = 0f;
    Color originalColor;

    void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
        gridEntity.isTileOccupant = false;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // CRITICAL FIX: Snap and register the entity immediately upon creation.
        // This prevents a race condition where the plant's tick update could run
        // before this object's Start() method has been called.
        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] GridPositionManager not found on Awake! Poop will not be registered correctly.");
        }
    }

    void Start()
    {
        currentLifetimeTicks = lifetimeTicks;

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }

        // The SnapEntityToGrid call was moved to Awake().
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    void Update()
    {
        if (isFading && fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            UpdateFade();

            if (fadeTimer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        if (isFading) return;

        currentLifetimeTicks--;

        if (currentLifetimeTicks <= 0)
        {
            StartFading();
        }
    }

    void StartFading()
    {
        isFading = true;
        fadeTimer = fadeRealTimeDuration;

        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    void UpdateFade()
    {
        if (spriteRenderer == null) return;

        float fadeProgress = 1f - (fadeTimer / fadeRealTimeDuration);
        Color color = originalColor;
        color.a = Mathf.Lerp(1f, 0f, fadeProgress);
        spriteRenderer.color = color;
    }
}