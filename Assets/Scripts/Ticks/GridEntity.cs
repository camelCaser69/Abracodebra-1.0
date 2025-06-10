// Assets/Scripts/Ticks/GridEntity.cs

using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    /// <summary>
    /// Component to be added to any GameObject that should exist on the grid.
    /// Manages the object's grid position and its visual representation, including smooth movement.
    /// Allows pre-configuration of a "ground point" offset on prefabs.
    /// </summary>
    public class GridEntity : MonoBehaviour
    {
        #region Fields

        [Header("Grid Position")]
        [SerializeField]
        private GridPosition gridPosition;
        // The 'snapToGridOnStart' field has been removed as it's no longer used.

        [Header("Visuals & Positioning")]
        [Tooltip("The local offset from the object's pivot to its 'ground' or 'feet' position. This point is used for grid calculations.")]
        [SerializeField]
        private Vector3 groundPointOffset = Vector3.zero;
        [Tooltip("An additional visual offset applied after grid positioning. Useful for tweaking Z-order or slight positional adjustments without affecting grid logic.")]
        [SerializeField]
        private Vector3 visualOffset = Vector3.zero;

        [Header("Movement Tweening")]
        [SerializeField]
        private float visualInterpolationSpeed = 5f;
        [SerializeField]
        private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private GridPosition previousGridPosition;
        private Vector3 visualStartPosition;
        private Vector3 visualTargetPosition;
        private float movementProgress = 1f;
        private bool isMoving = false;

        #endregion

        #region Properties
        public GridPosition Position
        {
            get => gridPosition;
            private set // Keep setter to be controlled by SetPosition method
            {
                if (gridPosition != value)
                {
                    previousGridPosition = gridPosition;
                    gridPosition = value;
                    OnGridPositionChanged();
                }
            }
        }

        public GridPosition PreviousPosition => previousGridPosition;
        public bool IsMoving => isMoving;
        public float MovementProgress => movementProgress;
        
        /// <summary>
        /// Calculates the world position of this entity's defined ground point.
        /// </summary>
        public Vector3 GroundWorldPosition => transform.position + groundPointOffset;

        #endregion

        #region Events
        public event Action<GridPosition, GridPosition> OnPositionChanged;
        public event Action<GridPosition> OnMovementComplete;
        public event Action OnMovementStart;
        #endregion

        #region Unity Methods
        protected virtual void Start()
        {
            // Snapping is now handled externally by GridSnapStartup.cs.

            // This initialization is still important.
            visualStartPosition = transform.position;
            visualTargetPosition = transform.position;
            previousGridPosition = gridPosition;

            GridPositionManager.Instance?.RegisterEntity(this);
        }

        protected virtual void OnDestroy()
        {
            GridPositionManager.Instance?.UnregisterEntity(this);
        }

        protected virtual void Update()
        {
            if (movementProgress < 1f)
            {
                movementProgress += Time.deltaTime * visualInterpolationSpeed;
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
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Draw a line from the pivot to the ground point
            Vector3 groundWorldPosition = transform.position + groundPointOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, groundWorldPosition);
            Gizmos.DrawWireSphere(groundWorldPosition, 0.1f);
            UnityEditor.Handles.Label(groundWorldPosition + Vector3.up * 0.2f, "Ground Point");

            // If playing, show where the grid thinks the ground is
            if (Application.isPlaying && GridPositionManager.Instance != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 gridCenter = GridPositionManager.Instance.GetCellCenter(gridPosition);
                Gizmos.DrawSphere(gridCenter, 0.08f);
            }
        }
#endif
        #endregion

        #region Positioning Logic
        protected virtual void OnGridPositionChanged()
        {
            visualStartPosition = transform.position;

            // The target position from the grid manager is the GROUND position.
            Vector3 groundTargetPosition = GridPositionManager.Instance.GridToWorld(gridPosition);
            // The final transform position must be offset by our ground point.
            visualTargetPosition = groundTargetPosition - groundPointOffset + visualOffset;

            movementProgress = 0f;

            if (!isMoving)
            {
                isMoving = true;
                OnMovementStart?.Invoke();
            }

            OnPositionChanged?.Invoke(previousGridPosition, gridPosition);
        }

        public void SetPosition(GridPosition newPosition, bool instant = false)
        {
            Position = newPosition; // This invokes OnGridPositionChanged which sets up the tween

            if (instant)
            {
                if (GridPositionManager.Instance == null) return;
                
                // The target position from the grid manager is the GROUND position.
                Vector3 groundTargetPosition = GridPositionManager.Instance.GridToWorld(Position);
                // The final transform position must be offset by our ground point.
                transform.position = groundTargetPosition - groundPointOffset + visualOffset;

                // Reset tweening variables
                visualStartPosition = transform.position;
                visualTargetPosition = transform.position;
                movementProgress = 1f;
                isMoving = false;
            }
        }

        public void SnapToGrid()
        {
            if (GridPositionManager.Instance == null) return;

            // Use the ground point to determine the grid position
            Vector3 groundWorldPos = transform.position + groundPointOffset;
            GridPosition currentGridPos = GridPositionManager.Instance.WorldToGrid(groundWorldPos);
            SetPosition(currentGridPos, true);
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
                OnMovementComplete?.Invoke(gridPosition);
            }
        }
        #endregion
    }
}