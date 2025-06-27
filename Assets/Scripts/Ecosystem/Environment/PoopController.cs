// Update the PoopController.cs with these changes:

using UnityEngine;
using WegoSystem;

[RequireComponent(typeof(GridEntity))]
public class PoopController : MonoBehaviour, ITickUpdateable
{
    [Header("Configuration")]
    [SerializeField] private int lifetimeTicks = 20;
    [SerializeField] private int fadeStartTicks = 15;
    [SerializeField] private float fadeRealTimeDuration = 1f; // Real-time seconds for fade
    
    private GridEntity gridEntity;
    private SpriteRenderer spriteRenderer;
    private int currentLifetimeTicks;
    private bool isFading = false;
    private float fadeTimer = 0f;
    private Color originalColor;
    
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
    }
    
    void Start()
    {
        currentLifetimeTicks = lifetimeTicks;
        
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        
        // Snap to grid
        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
        }
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
        // Handle real-time fade
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
        
        // Start fade when reaching fade threshold
        if (currentLifetimeTicks <= (lifetimeTicks - fadeStartTicks) && !isFading)
        {
            StartFading();
        }
    }
    
    private void StartFading()
    {
        isFading = true;
        
        // Calculate real-time duration
        if (TickManager.Instance?.Config != null)
        {
            fadeRealTimeDuration = (lifetimeTicks - fadeStartTicks) / TickManager.Instance.Config.ticksPerRealSecond;
        }
        
        fadeTimer = fadeRealTimeDuration;
        Debug.Log($"[PoopController] Starting fade. Duration: {fadeRealTimeDuration}s");
    }
    
    private void UpdateFade()
    {
        if (spriteRenderer == null) return;
        
        float fadeProgress = 1f - (fadeTimer / fadeRealTimeDuration);
        Color color = originalColor;
        color.a = Mathf.Lerp(1f, 0f, fadeProgress);
        spriteRenderer.color = color;
    }
    
    public void Initialize()
    {
        // Called by AnimalBehavior after spawning
        // Can be used to set custom lifetime or other properties
    }
}