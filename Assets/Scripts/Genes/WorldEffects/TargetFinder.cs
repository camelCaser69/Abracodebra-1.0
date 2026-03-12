// FILE: Assets/Scripts/Genes/WorldEffects/TargetFinder.cs
using System.Collections.Generic;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;

namespace Abracodabra.Genes.WorldEffects {
    public static class TargetFinder {
        public static List<AnimalController> FindCreaturesInRadius(Vector3 worldPosition, float radiusTiles) {
            var results = new List<AnimalController>();
            var animals = Object.FindObjectsByType<AnimalController>(FindObjectsSortMode.None);

            Vector2 sourceGrid = WorldToGrid(worldPosition);

            foreach (var animal in animals) {
                if (animal == null || animal.IsDying) continue;

                Vector2 animalGrid = WorldToGrid(animal.transform.position);
                float dist = Vector2.Distance(sourceGrid, animalGrid);

                if (dist <= radiusTiles) {
                    results.Add(animal);
                }
            }

            return results;
        }

        /// <summary>
        /// Finds all active plants within a tile radius. Used by Healing Cloud to regrow leaves on nearby plants.
        /// </summary>
        public static List<PlantGrowth> FindPlantsInRadius(Vector3 worldPosition, float radiusTiles) {
            var results = new List<PlantGrowth>();

            Vector2 sourceGrid = WorldToGrid(worldPosition);

            foreach (var plant in PlantGrowth.AllActivePlants) {
                if (plant == null) continue;
                if (plant.CurrentState == PlantState.Dead) continue;

                Vector2 plantGrid = WorldToGrid(plant.transform.position);
                float dist = Vector2.Distance(sourceGrid, plantGrid);

                if (dist <= radiusTiles) {
                    results.Add(plant);
                }
            }

            return results;
        }

        public static AnimalController FindNearestCreature(Vector3 worldPosition, float maxRangeTiles) {
            var animals = Object.FindObjectsByType<AnimalController>(FindObjectsSortMode.None);

            Vector2 sourceGrid = WorldToGrid(worldPosition);

            AnimalController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var animal in animals) {
                if (animal == null || animal.IsDying) continue;

                Vector2 animalGrid = WorldToGrid(animal.transform.position);
                float dist = Vector2.Distance(sourceGrid, animalGrid);

                if (dist <= maxRangeTiles && dist < nearestDist) {
                    nearestDist = dist;
                    nearest = animal;
                }
            }

            return nearest;
        }

        public static bool HasCreatureInRange(Vector3 worldPosition, float rangeTiles) {
            var animals = Object.FindObjectsByType<AnimalController>(FindObjectsSortMode.None);

            Vector2 sourceGrid = WorldToGrid(worldPosition);

            foreach (var animal in animals) {
                if (animal == null || animal.IsDying) continue;

                Vector2 animalGrid = WorldToGrid(animal.transform.position);
                float dist = Vector2.Distance(sourceGrid, animalGrid);

                if (dist <= rangeTiles) {
                    return true;
                }
            }

            return false;
        }

        static Vector2 WorldToGrid(Vector3 worldPos) {
            if (GridPositionManager.Instance != null) {
                GridPosition gp = GridPositionManager.Instance.WorldToGrid(worldPos);
                return new Vector2(gp.x, gp.y);
            }

            return new Vector2(worldPos.x, worldPos.y);
        }
    }
}