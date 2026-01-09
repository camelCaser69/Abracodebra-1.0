// Assets/Scripts/Ecosystem/Doris/DorisController.cs
using UnityEngine;
using WegoSystem;

namespace Abracodabra.Ecosystem
{
    /// <summary>
    /// Controller for Doris, the central creature the player feeds.
    /// This is a starting point - expand with hunger system, feeding, gene rewards, etc.
    /// </summary>
    [RequireComponent(typeof(MultiTileEntity))]
    public class DorisController : MonoBehaviour, IWorldInteractable
    {
        [Header("References")]
        [SerializeField] private MultiTileEntity multiTileEntity;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Hover Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private bool isHovered = false;

        #region IWorldInteractable Implementation

        public int InteractionPriority => multiTileEntity != null ? multiTileEntity.InteractionPriority : 200;

        public bool CanInteract => enabled && gameObject.activeInHierarchy;

        public Vector3 InteractionWorldPosition => transform.position;

        public bool OnInteract(GameObject interactor, ToolDefinition tool)
        {
            if (tool == null)
            {
                // Bare hands - open Doris UI or show info
                OnClickedWithHands(interactor);
                return true;
            }

            // Check if player is trying to feed Doris with a food item
            // For now, just log - you'll hook this up to your food/inventory system
            if (debugLog)
            {
                Debug.Log($"[DorisController] Player used tool '{tool.displayName}' on Doris");
            }

            // Return true if you want to consume the interaction
            // Return false to let it fall through to tile interaction
            return HandleToolInteraction(interactor, tool);
        }

        public void OnHoverEnter()
        {
            isHovered = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }

            if (debugLog)
            {
                Debug.Log("[DorisController] Hover entered");
            }
        }

        public void OnHoverExit()
        {
            isHovered = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }

            if (debugLog)
            {
                Debug.Log("[DorisController] Hover exited");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (multiTileEntity == null)
            {
                multiTileEntity = GetComponent<MultiTileEntity>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Start()
        {
            if (spriteRenderer != null)
            {
                normalColor = spriteRenderer.color;
            }
        }

        private void OnValidate()
        {
            if (multiTileEntity == null)
            {
                multiTileEntity = GetComponent<MultiTileEntity>();
            }
        }

        #endregion

        #region Interaction Handlers

        private void OnClickedWithHands(GameObject interactor)
        {
            if (debugLog)
            {
                Debug.Log($"[DorisController] Player clicked Doris with bare hands");
            }

            // TODO: Open Doris UI panel
            // TODO: Show Doris info/status
            // TODO: Trigger dialogue if any
        }

        private bool HandleToolInteraction(GameObject interactor, ToolDefinition tool)
        {
            // TODO: Check if this is a food item being given to Doris
            // For now, consume all tool interactions so they don't affect tiles underneath

            // Example logic you might add:
            // if (tool is FoodItem food) {
            //     FeedDoris(food);
            //     return true;
            // }

            // Block interaction so player can't accidentally use tools on tiles under Doris
            return true;
        }

        #endregion

        #region Doris Mechanics (Stubs)

        // TODO: Implement these based on your design document

        /*
        [Header("Hunger System")]
        [SerializeField] private float hunger = 0f;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerPerTick = 0.5f;

        public float Hunger => hunger;
        public float HungerPercent => hunger / maxHunger;
        public bool IsStarving => hunger >= maxHunger;

        public void OnTickUpdate(int currentTick)
        {
            hunger = Mathf.Min(hunger + hungerPerTick, maxHunger);
            
            if (IsStarving)
            {
                EatRandomPlant();
            }
        }

        public void Feed(FoodItem food)
        {
            // Track fed items for gene generation
            fedItemsThisRound.Add(food);
            hunger = Mathf.Max(0, hunger - food.SatiationValue);
        }

        public void OnRoundEnd()
        {
            // Generate gene choices based on fedItemsThisRound
            // Show gene selection UI
        }
        */

        #endregion
    }
}
