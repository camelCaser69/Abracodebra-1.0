// FILE: Assets/Scripts/Ecosystem/Feeding/IFeedable.cs
using System;
using UnityEngine;

namespace Abracodabra.Ecosystem.Feeding
{
    /// <summary>
    /// Interface for any entity that can be fed (Doris, Player, Animals).
    /// Implementations handle their own diet restrictions and satiation logic.
    /// </summary>
    public interface IFeedable
    {
        /// <summary>
        /// Display name shown in UI and logs
        /// </summary>
        string FeedableName { get; }

        /// <summary>
        /// World position where the food selection popup should anchor
        /// </summary>
        Vector3 FeedPopupAnchor { get; }

        /// <summary>
        /// Check if this entity can accept and eat the given consumable.
        /// Use this to filter diet restrictions.
        /// </summary>
        /// <param name="consumable">The consumable data to check</param>
        /// <returns>True if the entity can eat this food</returns>
        bool CanAcceptFood(ConsumableData consumable);

        /// <summary>
        /// Feed this entity the given consumable.
        /// Called after CanAcceptFood returns true.
        /// </summary>
        /// <param name="consumable">The consumable being fed</param>
        /// <param name="feeder">The GameObject doing the feeding (usually player)</param>
        /// <returns>The actual satiation amount applied</returns>
        float ReceiveFood(ConsumableData consumable, GameObject feeder);

        /// <summary>
        /// Event fired when this entity is successfully fed.
        /// Parameters: (ConsumableData consumed, float satiationApplied)
        /// </summary>
        event Action<ConsumableData, float> OnFed;
    }
}