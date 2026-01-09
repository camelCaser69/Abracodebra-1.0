// Assets/Scripts/WorldInteraction/IWorldInteractable.cs
using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    /// <summary>
    /// Interface for world entities that can be interacted with.
    /// Entities implementing this interface will be checked before tile interactions.
    /// </summary>
    public interface IWorldInteractable
    {
        /// <summary>
        /// Higher priority interactables are checked first.
        /// Multi-tile entities typically have priority > 100.
        /// Tiles typically have priority 0-100.
        /// </summary>
        int InteractionPriority { get; }

        /// <summary>
        /// Whether this interactable can currently be interacted with.
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Called when the player attempts to interact with this entity.
        /// </summary>
        /// <param name="interactor">The GameObject attempting to interact (usually the player).</param>
        /// <param name="tool">The tool being used, if any. Null for bare hands.</param>
        /// <returns>True if the interaction was handled, false to fall through to tile interaction.</returns>
        bool OnInteract(GameObject interactor, ToolDefinition tool);

        /// <summary>
        /// Called when this entity is hovered over.
        /// </summary>
        void OnHoverEnter();

        /// <summary>
        /// Called when the hover leaves this entity.
        /// </summary>
        void OnHoverExit();

        /// <summary>
        /// The world position for targeting/highlighting purposes.
        /// </summary>
        Vector3 InteractionWorldPosition { get; }
    }
}
