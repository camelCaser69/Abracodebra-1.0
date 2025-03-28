using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
public class AnimalController : MonoBehaviour
{
    // Assigned via FaunaManager on instantiation
    private AnimalDefinition definition;

    [Header("Thought/Dialogue Setup")]
    public AnimalThoughtLibrary thoughtLibrary;      // Assign GlobalAnimalThoughtLibrary asset
    public GameObject thoughtBubblePrefab;           // Assign your ThoughtBubble prefab

    [Header("Transforms for Visual Alignment")]
    public Transform mouthTransform;                 // For aligning mouth (future VFX)
    public Transform bubbleSpawnTransform;           // Where thought bubbles spawn

    [Header("Thought Bubble Settings")]
    public float thoughtCooldownTime = 5f;             // Minimum time between thought bubbles
    private float thoughtCooldownTimer = 0f;

    private string speciesName;                      // Derived from definition.animalName

    // Basic runtime stats
    private float currentHealth;
    private float currentHunger;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;           // For sprite flipping
    private Vector2 moveDirection;

    [Header("Animal Behavior Settings")]
    [Tooltip("Intensity multiplier (0 to 1) controlling how frequently the animal pauses while wandering.")]
    [Range(0f, 1f)]
    public float wanderPauseIntensity = 0.5f;
    [Tooltip("Maximum duration (in seconds) for a wander pause.")]
    public float maxWanderPauseDuration = 2f;
    // Instead of fixed wander durations, we generate them randomly.

    [Header("Movement Bounds (World Space)")]
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Eating Settings")]
    public float eatDuration = 2f;                    // Time spent eating
    [Tooltip("How close (in world units) the animal must be to a leaf to eat it.")]
    public float eatDistance = 0.5f;
    private bool isEating = false;
    private float eatTimer = 0f;
    private GameObject currentTargetLeaf;

    public void Initialize(AnimalDefinition def)
    {
        definition = def;
        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        speciesName = definition.animalName; // e.g., "Bunny"
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (definition == null)
        {
            Debug.LogWarning("[AnimalController] 'definition' is null. Did you call Initialize()?");
            return;
        }

        // Decrement thought bubble cooldown
        thoughtCooldownTimer -= Time.deltaTime;

        // Increase hunger over time
        currentHunger += definition.hungerDecayRate * Time.deltaTime;

        // If in eating state, update timer and exit
        if (isEating)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer <= 0f)
            {
                isEating = false;
                FinishEatingLeaf();
            }
            return;
        }

        // Decide behavior based on hunger
        if (currentHunger >= definition.hungerThreshold)
        {
            Debug.Log($"{speciesName} is hungry! (Hunger: {currentHunger:0.00}/{definition.hungerThreshold})");
            if (thoughtCooldownTimer <= 0f)
            {
                ShowThought(ThoughtTrigger.Hungry);
                thoughtCooldownTimer = thoughtCooldownTime;
            }

            if (currentTargetLeaf == null)
                currentTargetLeaf = FindNearestLeaf();

            if (currentTargetLeaf != null)
                MoveTowardLeaf(currentTargetLeaf);
            else
                Wander();
        }
        else
        {
            Wander();
            currentTargetLeaf = null;
        }

        // Flip sprite based on horizontal movement
        FlipSpriteBasedOnDirection(moveDirection);
    }

    private void FixedUpdate()
    {
        if (isEating || rb == null) return;

        Vector2 newPos = rb.position + moveDirection * definition.movementSpeed * Time.fixedDeltaTime;
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);
        rb.MovePosition(newPos);
    }

    // Improved wander: alternating between moving and pausing, with random durations affected by intensity.
    private void Wander()
    {
        // Random chance to pause each frame
        if (Random.value < wanderPauseIntensity * Time.deltaTime)
        {
            // Pause: set moveDirection to zero for a random duration up to maxWanderPauseDuration
            moveDirection = Vector2.zero;
            float pauseDuration = Random.Range(0.5f, maxWanderPauseDuration);
            Invoke("ResumeWandering", pauseDuration);
        }
        // If not pausing, choose a random direction occasionally
        else if (moveDirection == Vector2.zero)
        {
            float angle = Random.Range(0f, 360f);
            moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    private void ResumeWandering()
    {
        // If no current move direction, pick a random one
        if (moveDirection == Vector2.zero)
        {
            float angle = Random.Range(0f, 360f);
            moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
    
    public void SetMovementBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }

    private void MoveTowardLeaf(GameObject leafObj)
    {
        if (leafObj == null)
        {
            currentTargetLeaf = null;
            return;
        }

        Vector2 leafPos = leafObj.transform.position;
        Vector2 myPos = transform.position;
        float distance = Vector2.Distance(myPos, leafPos);

        if (distance <= eatDistance)
        {
            isEating = true;
            eatTimer = eatDuration;
            ShowThought(ThoughtTrigger.Eating);
        }
        else
        {
            moveDirection = (leafPos - myPos).normalized;
        }
    }

    private void FinishEatingLeaf()
    {
        if (currentTargetLeaf)
        {
            Destroy(currentTargetLeaf);
            currentTargetLeaf = null;
        }
        currentHunger -= definition.eatAmount;
        if (currentHunger < 0f) currentHunger = 0f;
    }

    private GameObject FindNearestLeaf()
    {
        GameObject[] leaves = GameObject.FindGameObjectsWithTag("Leaf");
        if (leaves.Length == 0) return null;

        Vector2 myPos = transform.position;
        GameObject nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var leaf in leaves)
        {
            float dist = Vector2.Distance(myPos, leaf.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = leaf;
            }
        }
        return nearest;
    }

    // Thought bubble logic: spawns a bubble as a child of bubbleSpawnTransform.
    private void ShowThought(ThoughtTrigger trigger)
    {
        if (!thoughtLibrary || !thoughtBubblePrefab)
        {
            Debug.LogWarning($"[{speciesName}] Missing thought library or bubble prefab!");
            return;
        }
        if (string.IsNullOrEmpty(speciesName))
            return;

        var matchingEntries = thoughtLibrary.allThoughts
            .Where(t => t.speciesName == speciesName && t.trigger == trigger)
            .ToList();

        if (matchingEntries.Count == 0)
        {
            Debug.Log($"[{speciesName}] No thought lines for trigger {trigger}.");
            return;
        }

        var chosenEntry = matchingEntries[Random.Range(0, matchingEntries.Count)];
        if (chosenEntry.lines == null || chosenEntry.lines.Count == 0)
        {
            Debug.Log($"[{speciesName}] Thought entry for trigger {trigger} has no lines.");
            return;
        }

        string randomLine = chosenEntry.lines[Random.Range(0, chosenEntry.lines.Count)];
        Debug.Log($"[{speciesName}] Spawning thought bubble: {randomLine}");

        // Spawn bubble as a child of bubbleSpawnTransform (if set) or this transform
        Transform spawnParent = bubbleSpawnTransform ? bubbleSpawnTransform : transform;
        GameObject bubbleObj = Instantiate(thoughtBubblePrefab, spawnParent.position, Quaternion.identity, spawnParent);
        bubbleObj.transform.localPosition = Vector3.zero;

        ThoughtBubbleController bubble = bubbleObj.GetComponent<ThoughtBubbleController>();
        if (bubble != null)
        {
            bubble.Initialize(randomLine, spawnParent, 2f);
        }
        else
        {
            Debug.LogWarning($"[{speciesName}] ThoughtBubblePrefab is missing ThoughtBubbleController!");
        }
    }

    // Flip sprite based on horizontal movement direction
    private void FlipSpriteBasedOnDirection(Vector2 direction)
    {
        if (!spriteRenderer) return;
        if (direction.x < -0.01f)
            spriteRenderer.flipX = true;
        else if (direction.x > 0.01f)
            spriteRenderer.flipX = false;
    }
}
