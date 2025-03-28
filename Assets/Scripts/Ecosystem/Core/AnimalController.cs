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
    public Transform mouthTransform;                 // For future eating VFX (set in prefab)
    public Transform bubbleSpawnTransform;           // For thought bubble spawn location

    [Header("Thought Bubble Settings")]
    public float thoughtCooldownTime = 5f;             // Minimum time between thought bubbles
    private float thoughtCooldownTimer = 0f;

    private string speciesName;                      // Derived from definition.animalName

    // Basic runtime stats
    private float currentHealth;
    private float currentHunger;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;           // Used for sprite flipping
    private Vector2 moveDirection;

    // Wander behavior settings
    private float wanderTimer = 0f;
    public float wanderMoveDuration = 2f;            // How long animal moves in one wander phase
    public float wanderPauseDuration = 1f;           // How long animal pauses between moves
    private bool isWanderingPaused = false;
    private float wanderStateTimer = 0f;             // Timer for current wander state

    [Header("Movement Bounds (World Space)")]
    public Vector2 minBounds = new Vector2(-10f, -5f);
    public Vector2 maxBounds = new Vector2(10f, 5f);

    [Header("Eating Settings")]
    public float eatDuration = 2f;                   // How long the animal stays still while eating
    [Tooltip("How close the animal must be to a leaf to eat it.")]
    public float eatDistance = 0.5f;                 // Adjustable in the Inspector
    private bool isEating = false;
    private float eatTimer = 0f;
    private GameObject currentTargetLeaf;

    public void Initialize(AnimalDefinition def)
    {
        definition = def;
        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        speciesName = definition.animalName; // e.g. "Bunny"
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

        // Decrement thought bubble cooldown timer
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

        // Determine behavior based on hunger
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
        if (isEating || rb == null) 
            return;

        Vector2 newPos = rb.position + moveDirection * definition.movementSpeed * Time.fixedDeltaTime;
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);
        rb.MovePosition(newPos);
    }

    // Improved wander: alternating move and pause phases
    private void Wander()
    {
        wanderStateTimer -= Time.deltaTime;
        if (wanderStateTimer <= 0f)
        {
            // Toggle state
            isWanderingPaused = !isWanderingPaused;
            if (isWanderingPaused)
            {
                // Pause movement; set timer to pause duration
                wanderStateTimer = wanderPauseDuration;
                moveDirection = Vector2.zero;
            }
            else
            {
                // Resume moving: pick a new random direction and set move duration
                float angle = Random.Range(0f, 360f);
                moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                wanderStateTimer = wanderMoveDuration;
            }
        }
    }

    private void MoveTowardLeaf(GameObject leafObj)
    {
        if (!leafObj)
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
        if (currentHunger < 0f)
            currentHunger = 0f;
    }

    private GameObject FindNearestLeaf()
    {
        GameObject[] leaves = GameObject.FindGameObjectsWithTag("Leaf");
        if (leaves.Length == 0)
            return null;

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

    // Thought bubble logic: now spawns as a child of bubbleSpawnTransform
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

        // Spawn bubble as a child of bubbleSpawnTransform (or fallback to self)
        Transform spawnParent = bubbleSpawnTransform ? bubbleSpawnTransform : transform;
        GameObject bubbleObj = Instantiate(thoughtBubblePrefab, spawnParent.position, Quaternion.identity, spawnParent);
        bubbleObj.transform.localPosition = Vector3.zero; // Align it with parent

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

    // Sprite flipping based on horizontal movement
    private void FlipSpriteBasedOnDirection(Vector2 direction)
    {
        if (!spriteRenderer) return;
        if (direction.x < -0.01f)
            spriteRenderer.flipX = true;
        else if (direction.x > 0.01f)
            spriteRenderer.flipX = false;
    }
}
