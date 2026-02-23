// File: Assets/Scripts/Genes/WorldEffects/TargetFinder.cs
using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

namespace Abracodabra.Genes.WorldEffects
{
    /// <summary>
    /// Centralized creature-finding logic reused by Cloud, Projectile, Aura, Trap, TriggerProximity.
    /// Uses grid-based distance via GridPositionManager.
    /// </summary>
    public static class TargetFinder
    {
        /// <summary>
        /// Returns all living animals within grid distance (Euclidean on grid coords).
        /// </summary>
        public static List<AnimalController> FindCreaturesInRadius(Vector3 worldPosition, float radiusTiles)
        {
            var results = new List<AnimalController>();
            var animals = Object.FindObjectsByType<AnimalController>(FindObjectsSortMode.None);

            Vector2 sourceGrid = WorldToGrid(worldPosition);

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsDying) continue;

                Vector2 animalGrid = WorldToGrid(animal.transform.position);
                float dist = Vector2.Distance(sourceGrid, animalGrid);

                if (dist <= radiusTiles)
                {
                    results.Add(animal);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns the closest living animal within range, or null if none found.
        /// </summary>
        public static AnimalController FindNearestCreature(Vector3 worldPosition, float maxRangeTiles)
        {
            var animals = Object.FindObjectsByType<AnimalController>(FindObjectsSortMode.None);

            Vector2 sourceGrid = WorldToGrid(worldPosition);

            AnimalController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsDying) continue;

                Vector2 animalGrid = WorldToGrid(animal.transform.position);
                float dist = Vector2.Distance(sourceGrid, animalGrid);

                if (dist <= maxRangeTiles && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = animal;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Fast check — returns true on first creature found in range.
        /// </summary>
        public static bool HasCreatureInRange(Vector3 worldPosition, float rangeTiles)
        {
            var animals = Object.FindObjectsByType<AnimalController>(FindObjectsSortMode.None);

            Vector2 sourceGrid = WorldToGrid(worldPosition);

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsDying) continue;

                Vector2 animalGrid = WorldToGrid(animal.transform.position);
                float dist = Vector2.Distance(sourceGrid, animalGrid);

                if (dist <= rangeTiles)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 WorldToGrid(Vector3 worldPos)
        {
            if (GridPositionManager.Instance != null)
            {
                GridPosition gp = GridPositionManager.Instance.WorldToGrid(worldPos);
                return new Vector2(gp.x, gp.y);
            }

            // Fallback: treat world units as grid units
            return new Vector2(worldPos.x, worldPos.y);
        }
    }
}
