using UnityEngine;

namespace Abracodabra.Minigames {
    
    /// <summary>
    /// Configuration for a timing circle minigame.
    /// Create instances via Assets > Create > Abracodabra > Minigames > Timing Circle Config
    /// </summary>
    [CreateAssetMenu(fileName = "TimingCircleConfig", menuName = "Abracodabra/Minigames/Timing Circle Config")]
    public class TimingCircleConfig : ScriptableObject {
        
        [Header("Timing")]
        [Tooltip("Total duration of the minigame in seconds")]
        [Range(0.5f, 5f)]
        public float duration = 1.5f;
        
        [Tooltip("Starting radius of the shrinking circle (in world units)")]
        [Range(0.5f, 3f)]
        public float startRadius = 2f;
        
        [Header("Success Zones")]
        [Tooltip("Outer edge of the Good zone (where success starts)")]
        [Range(0.3f, 1.5f)]
        public float goodZoneOuterRadius = 0.7f;
        
        [Tooltip("Inner edge of the Good zone / Outer edge of the Perfect zone")]
        [Range(0.15f, 1f)]
        public float goodZoneInnerRadius = 0.35f;
        
        [Tooltip("Inner edge of the Perfect zone (can be 0 for solid center)")]
        [Range(0f, 0.5f)]
        public float perfectZoneInnerRadius = 0f;
        
        [Header("Zone Colors")]
        [Tooltip("Color of the Good zone (outer ring)")]
        public Color goodZoneColor = new Color(0.3f, 0.7f, 0.3f, 0.6f); // Semi-transparent green
        
        [Tooltip("Color of the Perfect zone (inner area)")]
        public Color perfectZoneColor = new Color(0.2f, 1f, 0.2f, 0.8f); // Brighter green
        
        [Header("Shrinking Indicator")]
        [Tooltip("Color of the shrinking circle")]
        public Color shrinkingCircleColor = new Color(1f, 0.8f, 0.2f, 0.95f); // Golden
        
        [Tooltip("Thickness of the shrinking circle line")]
        [Range(0.03f, 0.15f)]
        public float shrinkingCircleThickness = 0.06f;
        
        [Tooltip("Color when shrinking circle enters Good zone")]
        public Color shrinkingInGoodZoneColor = new Color(0.5f, 1f, 0.5f, 1f); // Light green
        
        [Tooltip("Color when shrinking circle enters Perfect zone")]
        public Color shrinkingInPerfectZoneColor = new Color(1f, 1f, 0.5f, 1f); // Yellow-green
        
        [Header("Feedback")]
        [Tooltip("Flash color on successful hit")]
        public Color successFlashColor = new Color(0.2f, 1f, 0.2f, 1f);
        
        [Tooltip("Flash color on perfect hit")]
        public Color perfectFlashColor = new Color(1f, 1f, 0.2f, 1f);
        
        [Tooltip("Flash color on miss")]
        public Color missFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        
        [Tooltip("Duration of result flash effect")]
        [Range(0.1f, 0.5f)]
        public float flashDuration = 0.2f;
        
        [Header("Audio (Optional)")]
        [Tooltip("Sound to play when minigame starts")]
        public AudioClip startSound;
        
        [Tooltip("Sound to play on successful hit")]
        public AudioClip successSound;
        
        [Tooltip("Sound to play on perfect hit")]
        public AudioClip perfectSound;
        
        [Tooltip("Sound to play on miss")]
        public AudioClip missSound;
        
        [Header("Sorting")]
        [Tooltip("Sorting layer for minigame visuals")]
        public string sortingLayerName = "UI";
        
        [Tooltip("Sorting order within the layer")]
        public int sortingOrder = 1000;

        /// <summary>
        /// Evaluate what tier a given radius falls into
        /// </summary>
        public MinigameResultTier EvaluateRadius(float currentRadius) {
            // Perfect zone: between perfectZoneInnerRadius and goodZoneInnerRadius
            if (currentRadius <= goodZoneInnerRadius && currentRadius >= perfectZoneInnerRadius) {
                return MinigameResultTier.Perfect;
            }
            // Good zone: between goodZoneOuterRadius and goodZoneInnerRadius
            if (currentRadius <= goodZoneOuterRadius && currentRadius > goodZoneInnerRadius) {
                return MinigameResultTier.Good;
            }
            return MinigameResultTier.Miss;
        }

        /// <summary>
        /// Calculate accuracy (0-1) based on how close to perfect center
        /// </summary>
        public float CalculateAccuracy(float currentRadius) {
            float perfectCenter = (goodZoneInnerRadius + perfectZoneInnerRadius) / 2f;
            if (Mathf.Approximately(currentRadius, perfectCenter)) return 1f;
            if (currentRadius >= startRadius) return 0f;
            
            return 1f - Mathf.Abs(currentRadius - perfectCenter) / startRadius;
        }

        /// <summary>
        /// Check which zone the current radius is in
        /// </summary>
        public MinigameResultTier GetCurrentZone(float currentRadius) {
            if (currentRadius <= goodZoneInnerRadius) {
                return MinigameResultTier.Perfect;
            }
            if (currentRadius <= goodZoneOuterRadius) {
                return MinigameResultTier.Good;
            }
            return MinigameResultTier.Miss;
        }

        void OnValidate() {
            // Ensure radii are in correct order
            if (goodZoneInnerRadius >= goodZoneOuterRadius) {
                goodZoneInnerRadius = goodZoneOuterRadius - 0.1f;
            }
            if (perfectZoneInnerRadius >= goodZoneInnerRadius) {
                perfectZoneInnerRadius = goodZoneInnerRadius - 0.1f;
            }
            if (goodZoneOuterRadius >= startRadius) {
                startRadius = goodZoneOuterRadius + 0.5f;
            }
            if (perfectZoneInnerRadius < 0) {
                perfectZoneInnerRadius = 0;
            }
        }
    }
}