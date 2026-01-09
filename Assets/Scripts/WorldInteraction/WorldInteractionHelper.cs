// Assets/Scripts/WorldInteraction/WorldInteractionHelper.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    /// <summary>
    /// Helper class for checking and resolving world interactions.
    /// Checks for interactable entities before falling through to tile interactions.
    /// </summary>
    public static class WorldInteractionHelper
    {
        /// <summary>
        /// Gets the highest priority interactable at a grid position.
        /// Returns null if no interactables are found.
        /// </summary>
        public static IWorldInteractable GetInteractableAt(GridPosition position)
        {
            if (GridPositionManager.Instance == null) return null;

            var entities = GridPositionManager.Instance.GetEntitiesAt(position);
            if (entities.Count == 0) return null;

            IWorldInteractable best = null;
            int bestPriority = int.MinValue;

            foreach (var entity in entities)
            {
                // Check for IWorldInteractable on the entity or its parent
                var interactable = entity.GetComponent<IWorldInteractable>();
                if (interactable == null)
                {
                    interactable = entity.GetComponentInParent<IWorldInteractable>();
                }

                if (interactable != null && interactable.CanInteract)
                {
                    if (interactable.InteractionPriority > bestPriority)
                    {
                        bestPriority = interactable.InteractionPriority;
                        best = interactable;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Gets all interactables at a grid position, sorted by priority (highest first).
        /// </summary>
        public static List<IWorldInteractable> GetAllInteractablesAt(GridPosition position)
        {
            if (GridPositionManager.Instance == null) return new List<IWorldInteractable>();

            var entities = GridPositionManager.Instance.GetEntitiesAt(position);
            var interactables = new List<IWorldInteractable>();

            foreach (var entity in entities)
            {
                var interactable = entity.GetComponent<IWorldInteractable>();
                if (interactable == null)
                {
                    interactable = entity.GetComponentInParent<IWorldInteractable>();
                }

                if (interactable != null && interactable.CanInteract && !interactables.Contains(interactable))
                {
                    interactables.Add(interactable);
                }
            }

            return interactables.OrderByDescending(i => i.InteractionPriority).ToList();
        }

        /// <summary>
        /// Attempts to interact with any interactable at the given position.
        /// </summary>
        /// <param name="position">Grid position to check.</param>
        /// <param name="interactor">The interacting GameObject (usually player).</param>
        /// <param name="tool">The tool being used, if any.</param>
        /// <returns>True if an interactable handled the interaction.</returns>
        public static bool TryInteractAt(GridPosition position, GameObject interactor, ToolDefinition tool)
        {
            var interactable = GetInteractableAt(position);
            if (interactable != null)
            {
                return interactable.OnInteract(interactor, tool);
            }
            return false;
        }

        /// <summary>
        /// Gets the MultiTileEntity at a position, if any.
        /// Convenience method that wraps GridPositionManager.GetMultiTileEntityAt.
        /// </summary>
        public static MultiTileEntity GetMultiTileEntityAt(GridPosition position)
        {
            return GridPositionManager.Instance?.GetMultiTileEntityAt(position);
        }

        /// <summary>
        /// Checks if a position has any interactable entity that blocks tile interactions.
        /// </summary>
        public static bool HasBlockingInteractable(GridPosition position, int tileInteractionPriority = 100)
        {
            var interactable = GetInteractableAt(position);
            return interactable != null && interactable.InteractionPriority >= tileInteractionPriority;
        }
    }
}
