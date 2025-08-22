using System.Collections;
using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    public class GardenerController : MonoBehaviour, IStatusEffectable, ITickUpdateable
    {
        [Header("Movement")]
        [SerializeField] private float multiTickDelay = 0.5f;

        [Header("Animation")]
        [SerializeField] private bool useAnimations = true;
        [SerializeField] private Animator animator;
        [SerializeField] private string runningParameterName = "isRunning";
        [SerializeField] private string plantingTriggerName = "plant";

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipSpriteWhenMovingLeft = true;
        [SerializeField] private bool flipHorizontalDirection = true;

        private GridEntity gridEntity;
        private StatusEffectManager statusManager;
        private StatusEffectUIManager statusEffectUI;
        private PlayerHungerSystem hungerSystem;

        private GridPosition currentTargetPosition;
        private bool isProcessingMovement = false;

        public GridEntity GridEntity => gridEntity;
        public StatusEffectManager StatusManager => statusManager;
        public PlayerHungerSystem HungerSystem => hungerSystem;

        private void Awake()
        {
            gridEntity = GetComponent<GridEntity>();
            if (gridEntity == null) gridEntity = gameObject.AddComponent<GridEntity>();
            statusManager = GetComponent<StatusEffectManager>();
            if (statusManager == null) statusManager = gameObject.AddComponent<StatusEffectManager>();
            hungerSystem = GetComponent<PlayerHungerSystem>();
            if (hungerSystem == null) hungerSystem = gameObject.AddComponent<PlayerHungerSystem>();

            statusEffectUI = GetComponentInChildren<StatusEffectUIManager>(true);
            if (statusEffectUI == null) Debug.LogWarning("[GardenerController] StatusEffectUIManager not found in children. Icons won't display.", this);
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) Debug.LogWarning("[GardenerController] SpriteRenderer not found.", gameObject);
            if (animator == null && useAnimations) Debug.LogWarning("[GardenerController] Animator not found.", gameObject);
        }

        private void Start()
        {
            statusManager.Initialize(this);

            if (statusEffectUI != null)
            {
                statusEffectUI.Initialize(statusManager);
            }

            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }
            if (gridEntity != null)
            {
                gridEntity.OnPositionChanged += OnGridPositionChanged;
            }
        }

        private void OnDestroy()
        {
            var tickManager = TickManager.Instance;
            if (tickManager != null)
            {
                tickManager.UnregisterTickUpdateable(this);
            }

            if (gridEntity != null)
            {
                gridEntity.OnPositionChanged -= OnGridPositionChanged;
            }
        }

        public void OnTickUpdate(int currentTick)
        {
            statusManager?.OnTickUpdate(currentTick);
        }

        private void Update()
        {
            if (RunManager.Instance?.CurrentState == RunState.GrowthAndThreat)
            {
                HandlePlayerInput();
            }
            if (gridEntity != null && statusManager != null)
            {
                gridEntity.SetSpeedMultiplier(statusManager.VisualSpeedMultiplier);
            }
            UpdateAnimations();
            UpdateSpriteDirection();
        }

        private void LateUpdate()
        {
            // Snap final visual position to the pixel grid to prevent artifacts.
            transform.position = PixelGridSnapper.SnapToGrid(transform.position);
        }

        private void OnGridPositionChanged(GridPosition oldPos, GridPosition newPos)
        {
            EnvironmentalStatusEffectSystem.Instance?.CheckAndApplyTileEffects(this);
        }

        #region IStatusEffectable Implementation
        public string GetDisplayName() { return "Gardener"; }
        public void TakeDamage(float amount) { Debug.Log($"Gardener took {amount} damage!"); }
        public void Heal(float amount) { Debug.Log($"Gardener was healed for {amount}!"); }
        public void ModifyHunger(float amount)
        {
            if (hungerSystem != null)
            {
                hungerSystem.Eat(-amount); // Negative amount to increase hunger (reduce satiation)
            }
        }
        #endregion

        private void HandlePlayerInput()
        {
            if (gridEntity == null || gridEntity.IsMoving || isProcessingMovement) return;

            GridPosition moveDir = GridPosition.Zero;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) moveDir = GridPosition.Up;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) moveDir = GridPosition.Down;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) moveDir = GridPosition.Left;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) moveDir = GridPosition.Right;

            if (moveDir != GridPosition.Zero)
            {
                TryMove(moveDir);
                return;
            }
        }

        private void TryMove(GridPosition direction)
        {
            if (gridEntity == null) return;
            GridPosition targetPos = gridEntity.Position + direction;
            if (GridPositionManager.Instance != null && PlayerActionManager.Instance != null && TickManager.Instance != null &&
                GridPositionManager.Instance.IsPositionValid(targetPos) &&
                !GridPositionManager.Instance.IsPositionOccupied(targetPos))
            {
                Vector3 currentWorldPos = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
                int moveCost = PlayerActionManager.Instance.GetMovementTickCost(currentWorldPos, this);
                if (moveCost > 1)
                {
                    StartCoroutine(ProcessMultiTickMovement(targetPos, moveCost));
                }
                else
                {
                    gridEntity.SetPosition(targetPos);
                    currentTargetPosition = targetPos;
                    TickManager.Instance.AdvanceTick();
                }
            }
        }

        private IEnumerator ProcessMultiTickMovement(GridPosition targetPos, int tickCost)
        {
            isProcessingMovement = true;
            for (int i = 0; i < tickCost - 1; i++)
            {
                TickManager.Instance.AdvanceTick();
                yield return new WaitForSeconds(multiTickDelay);
            }
            gridEntity.SetPosition(targetPos);
            currentTargetPosition = targetPos;
            TickManager.Instance.AdvanceTick();
            isProcessingMovement = false;
        }

        private void UpdateAnimations()
        {
            if (!useAnimations || animator == null) return;
            bool isMoving = gridEntity != null && gridEntity.IsMoving;
            animator.SetBool(runningParameterName, isMoving);
        }

        private void UpdateSpriteDirection()
        {
            if (spriteRenderer == null || !flipSpriteWhenMovingLeft || gridEntity == null || !gridEntity.IsMoving) return;
            Vector3 worldTarget = GridPositionManager.Instance.GridToWorld(currentTargetPosition);
            Vector3 currentWorld = transform.position;
            Vector2 directionToCheck = (worldTarget - currentWorld).normalized;
            if (Mathf.Abs(directionToCheck.x) > 0.01f)
            {
                bool shouldFlip = directionToCheck.x < 0;
                spriteRenderer.flipX = flipHorizontalDirection ? shouldFlip : !shouldFlip;
            }
        }

        public void Plant()
        {
            if (useAnimations && animator != null)
            {
                animator.SetTrigger(plantingTriggerName);
            }
        }

        public GridPosition GetCurrentGridPosition()
        {
            return gridEntity?.Position ?? GridPosition.Zero;
        }
    }
}