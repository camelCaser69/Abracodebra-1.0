﻿// Assets/Scripts/Ticks/GridEntity.cs
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    public class GridEntity : MonoBehaviour
    {
        public bool isTileOccupant = true;

        GridPosition gridPosition;

        [SerializeField] Vector3 groundPointOffset = Vector3.zero;
        [SerializeField] Vector3 visualOffset = Vector3.zero;
        [SerializeField] float visualInterpolationSpeed = 5f;
        [SerializeField] AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        GridPosition previousGridPosition;
        Vector3 visualStartPosition;
        Vector3 visualTargetPosition;
        float movementProgress = 1f;
        bool isMoving = false;
        float speedMultiplier = 1f;

        public GridPosition Position
        {
            get => gridPosition;
            set
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
        public Vector3 GroundWorldPosition => transform.position + groundPointOffset;

        public event Action<GridPosition, GridPosition> OnPositionChanged;
        public event Action<GridPosition> OnMovementComplete;
        public event Action OnMovementStart;

        protected virtual void Start()
        {
            visualStartPosition = transform.position;
            visualTargetPosition = transform.position;
            previousGridPosition = gridPosition;
        }

        protected virtual void OnDestroy()
        {
            GridPositionManager.Instance?.UnregisterEntity(this);
        }

        protected virtual void Update()
        {
            if (movementProgress < 1f)
            {
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

        protected virtual void OnGridPositionChanged()
        {
            visualStartPosition = transform.position;

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

            OnPositionChanged?.Invoke(previousGridPosition, gridPosition);
        }

        public void SetPosition(GridPosition newPosition, bool instant = false)
        {
            Position = newPosition; // This invokes OnGridPositionChanged which sets up the tween

            if (instant)
            {
                if (GridPositionManager.Instance == null) return;

                Vector3 groundTargetPosition = GridPositionManager.Instance.GridToWorld(Position);
                transform.position = groundTargetPosition - groundPointOffset + visualOffset;

                visualStartPosition = transform.position;
                visualTargetPosition = transform.position;
                movementProgress = 1f;
                isMoving = false;
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
    }
}