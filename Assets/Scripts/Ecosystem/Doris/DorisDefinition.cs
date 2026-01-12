// FILE: Assets/Scripts/Ecosystem/Doris/DorisDefinition.cs
using System;
using UnityEngine;

namespace Abracodabra.Ecosystem {
    /// <summary>
    /// ScriptableObject that defines all configuration values for Doris.
    /// Allows designers to tweak Doris's behavior without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "DorisDefinition", menuName = "Abracodabra/Ecosystem/Doris Definition")]
    public class DorisDefinition : ScriptableObject {
        [Header("Identity")]
        [Tooltip("Display name for UI and logs.")]
        public string displayName = "Doris";
        
        [Tooltip("Description shown in tooltips/UI.")]
        [TextArea(2, 4)]
        public string description = "A mysterious creature living in a stump. Always hungry.";

        [Header("Hunger Configuration")]
        [Tooltip("Maximum hunger value. When reached, Doris is fully starving.")]
        [Min(1f)]
        public float maxHunger = 100f;
        
        [Tooltip("How much hunger increases per tick during Growth & Threat phase.")]
        [Min(0f)]
        public float hungerPerTick = 0.25f;
        
        [Tooltip("Percentage of maxHunger where Doris becomes 'hungry' (visual/audio cues).")]
        [Range(0f, 1f)]
        public float hungryThreshold = 0.4f;
        
        [Tooltip("Percentage of maxHunger where Doris becomes 'starving' (will eat plants).")]
        [Range(0f, 1f)]
        public float starvingThreshold = 0.8f;

        [Header("Starvation Behavior")]
        [Tooltip("How many ticks between each plant eating attempt when starving.")]
        [Min(1)]
        public int ticksBetweenPlantEating = 5;
        
        [Tooltip("How far (in tiles) Doris can reach to eat plants when starving.")]
        [Min(1)]
        public int plantEatingRadius = 4;
        
        [Tooltip("How much hunger is reduced when Doris eats a plant (less than player feeding).")]
        [Min(0f)]
        public float hungerReductionFromPlant = 10f;

        [Header("Feeding")]
        [Tooltip("Base multiplier for nutrition when player feeds Doris.")]
        [Min(0.1f)]
        public float feedingEfficiency = 1f;

        [Header("Visual")]
        [Tooltip("Sprite for happy state (low hunger).")]
        public Sprite happySprite;
        
        [Tooltip("Sprite for hungry state (medium hunger).")]
        public Sprite hungrySprite;
        
        [Tooltip("Sprite for starving/hangry state (high hunger).")]
        public Sprite starvingSprite;

        [Header("Audio")]
        [Tooltip("Sound when fed.")]
        public AudioClip feedSound;
        
        [Tooltip("Sound when becoming hungry.")]
        public AudioClip hungrySound;
        
        [Tooltip("Sound when eating a plant (starving behavior).")]
        public AudioClip eatPlantSound;

        // Computed thresholds for easy access
        public float HungryHungerValue => maxHunger * hungryThreshold;
        public float StarvingHungerValue => maxHunger * starvingThreshold;

        private void OnValidate() {
            maxHunger = Mathf.Max(1f, maxHunger);
            hungerPerTick = Mathf.Max(0f, hungerPerTick);
            ticksBetweenPlantEating = Mathf.Max(1, ticksBetweenPlantEating);
            plantEatingRadius = Mathf.Max(1, plantEatingRadius);
            hungerReductionFromPlant = Mathf.Max(0f, hungerReductionFromPlant);
            feedingEfficiency = Mathf.Max(0.1f, feedingEfficiency);
            
            // Ensure starving threshold is higher than hungry threshold
            if (starvingThreshold < hungryThreshold) {
                starvingThreshold = hungryThreshold;
            }
        }
    }
}