using UnityEngine;
using WegoSystem;

public class PoopController : MonoBehaviour, ITickUpdateable
{
    [Tooltip("The number of ticks this object exists before it starts to fade away.")]
    [SerializeField] private int lifetimeTicks = 20;
    
    [Tooltip("The duration of the fade-out animation, in real-time seconds.")]
    [SerializeField] private float fadeRealTimeDuration = 1f;

    private GridEntity gridEntity;
    private SpriteRenderer spriteRenderer;
    private int currentLifetimeTicks;
    private bool isFading = false;
    private float fadeTimer = 0f;
    private Color originalColor;

    private void Awake()
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
    }

    private void Start()
    {
        currentLifetimeTicks = lifetimeTicks;

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    private void Update()
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

        // --- FIX: Simplified lifetime check ---
        if (currentLifetimeTicks <= 0)
        {
            StartFading();
        }
    }

    private void StartFading()
    {
        isFading = true;
        fadeTimer = fadeRealTimeDuration;
        
        // Unregister from tick updates once fading starts to save performance.
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    private void UpdateFade()
    {
        if (spriteRenderer == null) return;

        float fadeProgress = 1f - (fadeTimer / fadeRealTimeDuration);
        Color color = originalColor;
        color.a = Mathf.Lerp(1f, 0f, fadeProgress);
        spriteRenderer.color = color;
    }
}