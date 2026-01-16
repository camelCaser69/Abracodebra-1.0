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
        [Tooltip("Radius of the outer target ring (success zone outer edge)")]
        [Range(0.2f, 1f)]
        public float targetOuterRadius = 0.6f;
        
        [Tooltip("Radius of the inner target ring (success zone inner edge / perfect zone outer edge)")]
        [Range(0.1f, 0.8f)]
        public float targetInnerRadius = 0.3f;
        
        [Tooltip("Radius for 'Perfect' tier (center of target)")]
        [Range(0.05f, 0.5f)]
        public float perfectRadius = 0.15f;
        
        [Header("Visuals")]
        [Tooltip("Color of the shrinking circle")]
        public Color shrinkingCircleColor = new Color(1f, 0.8f, 0.2f, 0.9f); // Golden
        
        [Tooltip("Color of the target zone (outer ring)")]
        public Color targetZoneColor = new Color(0.2f, 0.8f, 0.2f, 0.6f); // Green
        
        [Tooltip("Color of the perfect zone (inner ring)")]
        public Color perfectZoneColor = new Color(0.2f, 1f, 0.2f, 0.8f); // Bright green
        
        [Tooltip("Color of the center point")]
        public Color centerPointColor = new Color(1f, 1f, 1f, 0.8f); // White
        
        [Tooltip("Thickness of circle lines (in world units)")]
        [Range(0.02f, 0.2f)]
        public float lineThickness = 0.05f;
        
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
            if (currentRadius <= perfectRadius) {
                return MinigameResultTier.Perfect;
            }
            if (currentRadius <= targetOuterRadius && currentRadius >= targetInnerRadius) {
                return MinigameResultTier.Good;
            }
            if (currentRadius <= targetInnerRadius && currentRadius > perfectRadius) {
                // Between perfect and inner target - still Good
                return MinigameResultTier.Good;
            }
            return MinigameResultTier.Miss;
        }

        /// <summary>
        /// Calculate accuracy (0-1) based on how close to perfect center
        /// </summary>
        public float CalculateAccuracy(float currentRadius) {
            if (currentRadius <= perfectRadius) return 1f;
            if (currentRadius >= startRadius) return 0f;
            
            // Linear interpolation from start to perfect
            return 1f - (currentRadius - perfectRadius) / (startRadius - perfectRadius);
        }

        void OnValidate() {
            // Ensure radii are in correct order
            if (targetInnerRadius >= targetOuterRadius) {
                targetInnerRadius = targetOuterRadius - 0.1f;
            }
            if (perfectRadius >= targetInnerRadius) {
                perfectRadius = targetInnerRadius - 0.05f;
            }
            if (targetOuterRadius >= startRadius) {
                startRadius = targetOuterRadius + 0.5f;
            }
        }
    }
}
