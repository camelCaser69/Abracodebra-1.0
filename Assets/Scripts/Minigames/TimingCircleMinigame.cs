using System;
using System.Collections;
using UnityEngine;

namespace Abracodabra.Minigames {
    
    /// <summary>
    /// Wartales-style timing circle minigame.
    /// A circle shrinks toward a target zone - click when it's in the zone to succeed.
    /// </summary>
    public class TimingCircleMinigame : MonoBehaviour {
        
        TimingCircleConfig config;
        MinigameCompletedCallback onComplete;
        
        // Visual components
        LineRenderer shrinkingCircle;
        LineRenderer targetOuterCircle;
        LineRenderer targetInnerCircle;
        LineRenderer perfectCircle;
        SpriteRenderer centerPoint;
        SpriteRenderer flashOverlay;
        
        // State
        float currentRadius;
        float elapsedTime;
        bool isRunning;
        bool hasClicked;
        
        // Circle rendering settings
        const int CircleSegments = 64;

        public void Initialize(TimingCircleConfig config, MinigameCompletedCallback onComplete) {
            this.config = config;
            this.onComplete = onComplete;
            
            CreateVisuals();
        }

        void CreateVisuals() {
            // Create target zones (static)
            targetOuterCircle = CreateCircleRenderer("TargetOuter", config.targetOuterRadius, config.targetZoneColor, config.lineThickness);
            targetInnerCircle = CreateCircleRenderer("TargetInner", config.targetInnerRadius, config.targetZoneColor, config.lineThickness);
            perfectCircle = CreateCircleRenderer("Perfect", config.perfectRadius, config.perfectZoneColor, config.lineThickness * 0.8f);
            
            // Create shrinking circle
            shrinkingCircle = CreateCircleRenderer("Shrinking", config.startRadius, config.shrinkingCircleColor, config.lineThickness * 1.5f);
            
            // Create center point
            centerPoint = CreateCenterPoint();
            
            // Create flash overlay (for feedback)
            flashOverlay = CreateFlashOverlay();
            
            // Set sorting
            SetSorting(targetOuterCircle, config.sortingOrder);
            SetSorting(targetInnerCircle, config.sortingOrder + 1);
            SetSorting(perfectCircle, config.sortingOrder + 2);
            SetSorting(shrinkingCircle, config.sortingOrder + 3);
            if (centerPoint != null) {
                centerPoint.sortingLayerName = config.sortingLayerName;
                centerPoint.sortingOrder = config.sortingOrder + 4;
            }
            if (flashOverlay != null) {
                flashOverlay.sortingLayerName = config.sortingLayerName;
                flashOverlay.sortingOrder = config.sortingOrder + 10;
                flashOverlay.enabled = false;
            }
        }

        LineRenderer CreateCircleRenderer(string name, float radius, Color color, float width) {
            GameObject go = new GameObject($"Circle_{name}");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = CircleSegments;
            lr.startWidth = width;
            lr.endWidth = width;
            
            // Create a simple material
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            
            // Generate circle points
            SetCirclePoints(lr, radius);
            
            return lr;
        }

        void SetCirclePoints(LineRenderer lr, float radius) {
            Vector3[] points = new Vector3[CircleSegments];
            float angleStep = 360f / CircleSegments;
            
            for (int i = 0; i < CircleSegments; i++) {
                float angle = i * angleStep * Mathf.Deg2Rad;
                points[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f
                );
            }
            
            lr.SetPositions(points);
        }

        SpriteRenderer CreateCenterPoint() {
            GameObject go = new GameObject("CenterPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(32);
            sr.color = config.centerPointColor;
            go.transform.localScale = Vector3.one * 0.1f;
            
            return sr;
        }

        SpriteRenderer CreateFlashOverlay() {
            GameObject go = new GameObject("FlashOverlay");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(64);
            sr.color = Color.white;
            go.transform.localScale = Vector3.one * config.startRadius * 2.5f;
            
            return sr;
        }

        /// <summary>
        /// Procedurally generate a filled circle sprite
        /// </summary>
        Sprite CreateCircleSprite(int resolution) {
            Texture2D tex = new Texture2D(resolution, resolution);
            tex.filterMode = FilterMode.Bilinear;
            
            float center = resolution / 2f;
            float radius = resolution / 2f - 1;
            
            for (int y = 0; y < resolution; y++) {
                for (int x = 0; x < resolution; x++) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius) {
                        // Soft edge
                        float alpha = Mathf.Clamp01((radius - dist) / 2f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    } else {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
        }

        void SetSorting(LineRenderer lr, int order) {
            lr.sortingLayerName = config.sortingLayerName;
            lr.sortingOrder = order;
        }

        public void StartMinigame() {
            currentRadius = config.startRadius;
            elapsedTime = 0f;
            isRunning = true;
            hasClicked = false;
            
            // Play start sound
            if (config.startSound != null) {
                AudioSource.PlayClipAtPoint(config.startSound, transform.position);
            }
        }

        void Update() {
            if (!isRunning) return;
            
            // Update shrinking
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / config.duration;
            
            // Shrink from startRadius toward 0
            currentRadius = Mathf.Lerp(config.startRadius, 0f, progress);
            
            // Update visual
            SetCirclePoints(shrinkingCircle, currentRadius);
            
            // Update color based on position (visual feedback)
            UpdateShrinkingCircleColor();
            
            // Check for click
            if (Input.GetMouseButtonDown(0) && !hasClicked) {
                hasClicked = true;
                EvaluateClick();
                return;
            }
            
            // Check for timeout
            if (progress >= 1f) {
                CompleteWithResult(MinigameResultTier.Miss, 0f);
            }
        }

        void UpdateShrinkingCircleColor() {
            // Change color when entering success zone
            MinigameResultTier currentTier = config.EvaluateRadius(currentRadius);
            
            Color targetColor = config.shrinkingCircleColor;
            if (currentTier == MinigameResultTier.Perfect) {
                targetColor = config.perfectZoneColor;
            } else if (currentTier == MinigameResultTier.Good) {
                targetColor = config.targetZoneColor;
            }
            
            shrinkingCircle.startColor = targetColor;
            shrinkingCircle.endColor = targetColor;
        }

        void EvaluateClick() {
            MinigameResultTier tier = config.EvaluateRadius(currentRadius);
            float accuracy = config.CalculateAccuracy(currentRadius);
            
            CompleteWithResult(tier, accuracy);
        }

        void CompleteWithResult(MinigameResultTier tier, float accuracy) {
            isRunning = false;
            
            // Show feedback
            StartCoroutine(ShowFeedbackAndComplete(tier, accuracy));
        }

        IEnumerator ShowFeedbackAndComplete(MinigameResultTier tier, float accuracy) {
            // Flash effect
            Color flashColor = tier switch {
                MinigameResultTier.Perfect => config.perfectFlashColor,
                MinigameResultTier.Good => config.successFlashColor,
                _ => config.missFlashColor
            };
            
            // Play sound
            AudioClip clip = tier switch {
                MinigameResultTier.Perfect => config.perfectSound,
                MinigameResultTier.Good => config.successSound,
                _ => config.missSound
            };
            
            if (clip != null) {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
            
            // Show flash
            if (flashOverlay != null) {
                flashOverlay.enabled = true;
                flashOverlay.color = flashColor;
                
                float elapsed = 0f;
                Color startColor = flashColor;
                Color endColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
                
                while (elapsed < config.flashDuration) {
                    elapsed += Time.deltaTime;
                    float t = elapsed / config.flashDuration;
                    flashOverlay.color = Color.Lerp(startColor, endColor, t);
                    yield return null;
                }
                
                flashOverlay.enabled = false;
            }
            
            // Brief pause to let player see result
            yield return new WaitForSeconds(0.1f);
            
            // Complete
            MinigameResult result = new MinigameResult {
                Tier = tier,
                Accuracy = accuracy,
                TimeRemaining = config.duration - elapsedTime
            };
            
            onComplete?.Invoke(result);
        }

        /// <summary>
        /// Skip the minigame (player pressed escape or similar)
        /// </summary>
        public void Skip() {
            if (!isRunning) return;
            
            isRunning = false;
            
            MinigameResult result = new MinigameResult {
                Tier = MinigameResultTier.Skipped,
                Accuracy = 0f,
                TimeRemaining = config.duration - elapsedTime
            };
            
            onComplete?.Invoke(result);
        }

        void OnDestroy() {
            // Cleanup procedural textures
            if (centerPoint != null && centerPoint.sprite != null && centerPoint.sprite.texture != null) {
                Destroy(centerPoint.sprite.texture);
            }
            if (flashOverlay != null && flashOverlay.sprite != null && flashOverlay.sprite.texture != null) {
                Destroy(flashOverlay.sprite.texture);
            }
        }
    }
}
