using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    public class GridEntity : MonoBehaviour
    {
        public bool isTileOccupant = true;

        private GridPosition gridPosition;

        [SerializeField] private Vector3 groundPointOffset = Vector3.zero;
        [SerializeField] private Vector3 visualOffset = Vector3.zero;
        [SerializeField] private float visualInterpolationSpeed = 5f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private GridPosition previousGridPosition;
        private Vector3 visualStartPosition;
        private Vector3 visualTargetPosition;
        private float movementProgress = 1f;
        private bool isMoving = false;
        private float speedMultiplier = 1f;

        // --- THE FIX: A state to distinguish between being placed and actively moving ---
        private bool isPositionLocked = false;

        public GridPosition Position
        {
            get => gridPosition;
            private set
            {
                if (gridPosition != value)
                {
                    previousGridPosition = gridPosition;
                    gridPosition = value;
                    // Note: We no longer call OnGridPositionChanged here. It's called from SetPosition.
                }
            }
        }

        public GridPosition PreviousPosition => previousGridPosition;
        public bool IsMoving => isMoving;
        public float MovementProgress => movementProgress;
        public Vector3 GroundWorldPosition => transform.position + groundPointOffset;

        public event Action<GridPosition, GridPosition> OnPositionChanged;
        public event Action<GridPosition> OnMovementComplete;
        public event Action OnMovementStart;

        protected virtual void Start()
        {
            // If this entity hasn't been positioned by an external script by the first frame, snap it.
            // This ensures standalone entities placed in the editor still get aligned.
            if (gridPosition == GridPosition.Zero && movementProgress >= 1f)
            {
                SnapToGrid();
            }
        }

        protected virtual void OnDestroy()
        {
            var gridManager = GridPositionManager.Instance;
            if (gridManager != null)
            {
                gridManager.UnregisterEntity(this);
            }
        }

        protected virtual void Update()
        {
            // If the position is locked (i.e., it's a static plant part), do not run movement logic.
            if (isPositionLocked || movementProgress >= 1f)
            {
                return;
            }

            movementProgress += Time.deltaTime * visualInterpolationSpeed * speedMultiplier;
            movementProgress = Mathf.Clamp01(movementProgress);

            float curvedProgress = movementCurve.Evaluate(movementProgress);
            transform.position = Vector3.Lerp(visualStartPosition, visualTargetPosition, curvedProgress);

            if (movementProgress >= 1f)
            {
                transform.position = visualTargetPosition;
                isMoving = false;
                OnMovementComplete?.Invoke(gridPosition);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Vector3 groundWorldPosition = transform.position + groundPointOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, groundWorldPosition);
            Gizmos.DrawWireSphere(groundWorldPosition, 0.1f);
            UnityEditor.Handles.Label(groundWorldPosition + Vector3.up * 0.2f, "Ground Point");

            if (Application.isPlaying && GridPositionManager.Instance != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 gridCenter = GridPositionManager.Instance.GetCellCenter(gridPosition);
                Gizmos.DrawSphere(gridCenter, 0.08f);
            }
        }
#endif

        /// <summary>
        /// Sets the entity's logical grid position and handles the visual update.
        /// </summary>
        /// <param name="newPosition">The target grid position.</param>
        /// <param name="instant">If true, the entity's state is updated without visual movement (for initial placement). 
        /// If false, it tweens from its current position (for movement).</param>
        public void SetPosition(GridPosition newPosition, bool instant = false)
        {
            if (GridPositionManager.Instance != null && !GridPositionManager.Instance.IsPositionValid(newPosition))
            {
                Debug.LogWarning($"[GridEntity] Blocked attempt to move '{gameObject.name}' to invalid position {newPosition}. Movement cancelled.");
                return;
            }

            if (instant)
            {
                // --- FOR PLACING OBJECTS (like plant parts) ---
                // We lock the position, set the state, and DO NOT move the transform.
                // The caller (PlantCellManager) is the authority on the transform's position.
                isPositionLocked = true;
                Position = newPosition;
                visualStartPosition = transform.position;
                visualTargetPosition = transform.position;
                movementProgress = 1f;
                isMoving = false;
                OnPositionChanged?.Invoke(previousGridPosition, newPosition);
            }
            else
            {
                // --- FOR MOVING OBJECTS (like animals) ---
                // Unlock the position and initiate the tweening process.
                isPositionLocked = false;
                if (!isMoving)
                {
                    visualStartPosition = transform.position;
                }

                Position = newPosition; // This will update gridPosition and previousGridPosition

                // Calculate where we need to move to
                if (GridPositionManager.Instance != null)
                {
                    Vector3 groundTargetPosition = GridPositionManager.Instance.GridToWorld(gridPosition);
                    visualTargetPosition = groundTargetPosition - groundPointOffset + visualOffset;
                }

                movementProgress = 0f;
                if (!isMoving)
                {
                    isMoving = true;
                    OnMovementStart?.Invoke();
                }
                OnPositionChanged?.Invoke(previousGridPosition, newPosition);
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            this.speedMultiplier = multiplier;
        }

        public void SnapToGrid()
        {
            if (GridPositionManager.Instance == null) return;

            Vector3 groundWorldPos = transform.position + groundPointOffset;
            GridPosition currentGridPos = GridPositionManager.Instance.WorldToGrid(groundWorldPos);

            // When snapping, we DO want to move the object.
            isPositionLocked = false;
            SetPosition(currentGridPos, true); // This will now use the instant path but we must move the transform.
            transform.position = GridPositionManager.Instance.GridToWorld(currentGridPos) - groundPointOffset + visualOffset;
            visualStartPosition = transform.position;
            visualTargetPosition = transform.position;
        }

        public void MoveInDirection(GridPosition direction)
        {
            SetPosition(gridPosition + direction);
        }

        public bool CanMoveTo(GridPosition targetPosition)
        {
            return GridPositionManager.Instance?.IsPositionValid(targetPosition) ?? false;
        }

        public void CompleteMovement()
        {
            if (isMoving)
            {
                transform.position = visualTargetPosition;
                movementProgress = 1f;
                isMoving = false;
                isPositionLocked = false;
                OnMovementComplete?.Invoke(gridPosition);
            }
        }
    }
}