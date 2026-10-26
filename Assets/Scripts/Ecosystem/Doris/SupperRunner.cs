// FILE: Assets/Scripts/Ecosystem/Doris/SupperRunner.cs
// ============================================================================
//  SUPPER AT DUSK (Block 1 B3). When the Day ends, every fruit on every living
//  plant goes to Doris, one at a time and visibly: the plants are visited in
//  grid order (top row first, then left to right), each fruit arcs to her and
//  feeds her on arrival (DorisController.FeedDoris). Runs on frames while ticks
//  are stopped (RunState.Dusk); RunManager starts it and judges the dusk after.
//  Lives on RunManager's GameObject (added in code, no scene wiring).
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;

namespace Abracodabra.Ecosystem {
    public class SupperRunner : MonoBehaviour {
        public struct SupperResult {
            public int round;
            public int plantsWithFruit;
            public int fruitFed;
            public float nutritionFed;
            public float hungerBefore;
            public float hungerAfter;
            public int pipsBefore;
            public int pipsAfter;
            public int livingPlants;
            public bool dorisFound;

            public override string ToString() {
                return string.Format(CultureInfo.InvariantCulture,
                    "round {0}: {1} fruit from {2} plants, nutrition {3:F1}, hunger {4:F2} -> {5:F2}, belly {6} -> {7}, living plants {8}",
                    round, fruitFed, plantsWithFruit, nutritionFed, hungerBefore, hungerAfter, pipsBefore, pipsAfter, livingPlants);
            }
        }

        const string FlyerName = "SupperFruit";
        const int FlyerSortingBoost = 50;

        public bool IsRunning { get; private set; }
        public SupperResult LastResult { get; private set; }
        public int FruitFedSoFar { get; private set; }

        /// <summary>Fires after each supper (the dusk ledger, B6, reads it).</summary>
        public event Action<SupperResult> OnSupperFinished;

        public void Run(Action<SupperResult> onFinished) {
            if (IsRunning) {
                Debug.LogWarning("[SupperRunner] Supper is already running.");
                return;
            }
            StartCoroutine(RunSupper(onFinished));
        }

        /// <summary>Plants that are not dead (a plant in its death sequence is already Dead).</summary>
        public static int CountLivingPlants() {
            int count = 0;
            foreach (var plant in PlantGrowth.AllActivePlants) {
                if (plant != null && plant.CurrentState != PlantState.Dead) count++;
            }
            return count;
        }

        IEnumerator RunSupper(Action<SupperResult> onFinished) {
            IsRunning = true;
            FruitFedSoFar = 0;

            var rules = DuskRules.Active;
            var doris = FindFirstObjectByType<DorisController>();
            DorisHungerSystem hunger = doris != null ? doris.HungerSystem : null;

            var result = new SupperResult {
                round = RunManager.HasInstance ? RunManager.Instance.CurrentRoundNumber : 0,
                dorisFound = hunger != null
            };
            if (hunger != null) {
                result.hungerBefore = hunger.CurrentHunger;
                result.pipsBefore = hunger.BellyPips;
            }

            if (doris != null) {
                foreach (var plant in LivingPlantsInGridOrder()) {
                    if (plant == null || plant.CurrentState == PlantState.Dead) continue;

                    var fruitObjects = new List<GameObject>();
                    List<HarvestedItem> items = plant.HarvestAllFruits(fruitObjects);
                    if (items.Count == 0) continue;
                    result.plantsWithFruit++;

                    // The fruit GameObjects are destroyed at the end of this frame: copy their look now.
                    var flyers = new List<GameObject>(items.Count);
                    for (int i = 0; i < items.Count; i++) {
                        flyers.Add(MakeFlyer(i < fruitObjects.Count ? fruitObjects[i] : null, plant, doris));
                    }

                    for (int i = 0; i < items.Count; i++) {
                        yield return Fly(flyers[i], doris.MouthWorldPosition, rules);
                        if (flyers[i] != null) Destroy(flyers[i]);

                        float nutrition = items[i].GetNutritionValue();
                        doris.FeedDoris(nutrition, items[i]);
                        result.fruitFed++;
                        result.nutritionFed += nutrition;
                        FruitFedSoFar = result.fruitFed;
                    }
                }
            }
            else {
                Debug.LogWarning("[SupperRunner] No Doris in the scene; supper skipped.");
            }

            if (hunger != null) {
                result.hungerAfter = hunger.CurrentHunger;
                result.pipsAfter = hunger.BellyPips;
            }
            result.livingPlants = CountLivingPlants();

            if (rules.holdAfterSupper > 0f) yield return new WaitForSeconds(rules.holdAfterSupper);

            LastResult = result;
            IsRunning = false;
            Debug.Log($"[SupperRunner] Supper done, {result}");

            OnSupperFinished?.Invoke(result);
            onFinished?.Invoke(result);
        }

        /// <summary>Deterministic visiting order: top row first, then left to right.</summary>
        static List<PlantGrowth> LivingPlantsInGridOrder() {
            var plants = new List<PlantGrowth>();
            foreach (var plant in PlantGrowth.AllActivePlants) {
                if (plant != null && plant.CurrentState != PlantState.Dead) plants.Add(plant);
            }
            plants.Sort((a, b) => {
                GridPosition pa = GridOf(a), pb = GridOf(b);
                if (pa.y != pb.y) return pb.y.CompareTo(pa.y);
                return pa.x.CompareTo(pb.x);
            });
            return plants;
        }

        static GridPosition GridOf(PlantGrowth plant) {
            var gridEntity = plant.GetComponent<GridEntity>();
            if (gridEntity != null) return gridEntity.Position;
            Vector3 p = plant.transform.position;
            return new GridPosition(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        }

        GameObject MakeFlyer(GameObject fruitObject, PlantGrowth plant, DorisController doris) {
            var flyer = new GameObject(FlyerName);
            SpriteRenderer source = fruitObject != null ? fruitObject.GetComponentInChildren<SpriteRenderer>() : null;
            if (source == null) {
                // No visual to copy: an invisible stand-in keeps the pacing (one pip tick per fruit).
                flyer.transform.position = plant.transform.position;
                return flyer;
            }

            flyer.transform.position = source.transform.position;
            flyer.transform.localScale = source.transform.lossyScale;

            var renderer = flyer.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.color = source.color;
            renderer.sharedMaterial = source.sharedMaterial;

            // Above Doris while it flies into her.
            SpriteRenderer dorisRenderer = doris.GetComponentInChildren<SpriteRenderer>();
            if (dorisRenderer != null) {
                renderer.sortingLayerID = dorisRenderer.sortingLayerID;
                renderer.sortingOrder = Mathf.Max(source.sortingOrder, dorisRenderer.sortingOrder) + FlyerSortingBoost;
            }
            else {
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder + FlyerSortingBoost;
            }
            return flyer;
        }

        static IEnumerator Fly(GameObject flyer, Vector3 target, DuskRules rules) {
            float duration = Mathf.Max(0.05f, rules.secondsPerFruit);
            if (flyer == null || flyer.GetComponent<SpriteRenderer>() == null) {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            Transform t = flyer.transform;
            Vector3 start = t.position;
            Vector3 end = new Vector3(target.x, target.y, start.z);
            Vector3 baseScale = t.localScale;
            float elapsed = 0f;

            while (elapsed < duration && flyer != null) {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                Vector3 position = Vector3.Lerp(start, end, k);
                position.y += rules.fruitArcHeight * 4f * k * (1f - k);
                t.position = position;

                // Pops up to flight size, then shrinks a little as she swallows it.
                float grow = Mathf.Clamp01(k / 0.15f);
                float scale = Mathf.Lerp(1f, rules.fruitFlightScale, grow) * Mathf.Lerp(1f, 0.6f, k);
                t.localScale = baseScale * scale;
                yield return null;
            }
        }
    }
}
