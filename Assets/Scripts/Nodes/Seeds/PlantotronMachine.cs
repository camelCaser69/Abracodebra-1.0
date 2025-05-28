// FILE: Assets/Scripts/Genetics/PlantotronMachine.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlantotronMachine : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance for player interaction")]
    public float interactionRange = 2f;
    
    [Tooltip("Key to press for interaction (default: E)")]
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("UI References")]
    [Tooltip("The Plantotron UI panel to show/hide")]
    public PlantotronUI uiPanel;
    
    [Header("Visual Feedback")]
    [Tooltip("GameObject to show when player is in range (optional)")]
    public GameObject interactionPrompt;
    
    [Tooltip("Highlight material/effect when player is nearby (optional)")]
    public Material highlightMaterial;
    
    [Header("Audio (Optional)")]
    [Tooltip("Sound to play when machine is activated")]
    public AudioClip activationSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Internal state
    private bool playerInRange = false;
    private Transform playerTransform;
    private SpriteRenderer machineRenderer;
    private Material originalMaterial;
    private AudioSource audioSource;
    
    // Cache for player detection
    private const string PLAYER_TAG = "Player";
    
    void Awake()
    {
        // Get components
        machineRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // Ensure collider is set as trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            if (showDebugLogs)
                Debug.Log($"[PlantotronMachine] Set collider on {gameObject.name} to trigger mode");
        }
        
        // Store original material
        if (machineRenderer != null)
        {
            originalMaterial = machineRenderer.material;
        }
        
        // Ensure UI panel starts hidden
        if (uiPanel != null)
        {
            uiPanel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError($"[PlantotronMachine] UI Panel not assigned on {gameObject.name}!", this);
        }
        
        // Hide interaction prompt initially
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    void Update()
    {
        // Only check for interaction if player is in range
        if (playerInRange && playerTransform != null)
        {
            // Double-check distance (in case player moved quickly)
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance <= interactionRange)
            {
                // Check for interaction input
                if (Input.GetKeyDown(interactionKey))
                {
                    ToggleMachine();
                }
            }
            else
            {
                // Player moved out of range
                SetPlayerInRange(false);
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            playerTransform = other.transform;
            SetPlayerInRange(true);
            
            if (showDebugLogs)
                Debug.Log($"[PlantotronMachine] Player entered interaction range");
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            SetPlayerInRange(false);
            
            if (showDebugLogs)
                Debug.Log($"[PlantotronMachine] Player left interaction range");
        }
    }
    
    private void SetPlayerInRange(bool inRange)
    {
        if (playerInRange == inRange) return;
        
        playerInRange = inRange;
        
        // Update visual feedback
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(inRange);
        }
        
        // Update highlight effect
        if (machineRenderer != null && highlightMaterial != null)
        {
            machineRenderer.material = inRange ? highlightMaterial : originalMaterial;
        }
        
        // If player left range while UI is open, close it
        if (!inRange && uiPanel != null && uiPanel.gameObject.activeSelf)
        {
            CloseMachine();
        }
        
        if (!inRange)
        {
            playerTransform = null;
        }
    }
    
    private void ToggleMachine()
    {
        if (uiPanel == null)
        {
            Debug.LogError("[PlantotronMachine] Cannot toggle - UI Panel is null!");
            return;
        }
        
        bool isCurrentlyOpen = uiPanel.gameObject.activeSelf;
        
        if (isCurrentlyOpen)
        {
            CloseMachine();
        }
        else
        {
            OpenMachine();
        }
    }
    
    private void OpenMachine()
    {
        if (uiPanel == null) return;
        
        // Check if player has genetics inventory
        if (PlayerGeneticsInventory.Instance == null)
        {
            Debug.LogError("[PlantotronMachine] Cannot open - PlayerGeneticsInventory not found!");
            return;
        }
        
        uiPanel.gameObject.SetActive(true);
        uiPanel.OpenUI();
        
        // Play activation sound
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Pause the game or disable player movement if needed
        // Time.timeScale = 0f; // Uncomment if you want to pause the game
        
        if (showDebugLogs)
            Debug.Log("[PlantotronMachine] Machine opened");
    }
    
    private void CloseMachine()
    {
        if (uiPanel == null) return;
        
        uiPanel.CloseUI();
        uiPanel.gameObject.SetActive(false);
        
        // Resume the game if it was paused
        // Time.timeScale = 1f; // Uncomment if you paused the game
        
        if (showDebugLogs)
            Debug.Log("[PlantotronMachine] Machine closed");
    }
    
    // Public method for external scripts to open/close the machine
    public void SetMachineOpen(bool open)
    {
        if (open)
            OpenMachine();
        else
            CloseMachine();
    }
    
    // Public method to check if machine is currently open
    public bool IsMachineOpen()
    {
        return uiPanel != null && uiPanel.gameObject.activeSelf;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw a line to player if in range during play mode
        if (Application.isPlaying && playerTransform != null && playerInRange)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
    
    void OnDestroy()
    {
        // Clean up
        if (machineRenderer != null && originalMaterial != null)
        {
            machineRenderer.material = originalMaterial;
        }
    }
}