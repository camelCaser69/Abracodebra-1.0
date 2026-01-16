using System;
using System.Collections;
using UnityEngine;

namespace Abracodabra.Minigames {
    
    /// <summary>
    /// Wartales-style timing circle minigame.
    /// A circle shrinks toward target zones - click when it's in the zone to succeed.
    /// Uses filled donut shapes for clear visual zones.
    /// </summary>
    public class TimingCircleMinigame : MonoBehaviour {
        
        TimingCircleConfig config;
        MinigameCompletedCallback onComplete;
        
        // Visual components
        SpriteRenderer goodZoneRenderer;
        SpriteRenderer perfectZoneRenderer;
        LineRenderer shrinkingCircle;
        SpriteRenderer flashOverlay;
        
        // State
        float currentRadius;
        float elapsedTime;
        bool isRunning;
        bool hasClicked;
        
        // Circle rendering settings
        const int CircleSegments = 64;
        const int TextureResolution = 128;
        
        // Overlap to eliminate gap between zones
        const float ZoneOverlap = 0.03f;

        public void Initialize(TimingCircleConfig config, MinigameCompletedCallback onComplete) {
            this.config = config;
            this.onComplete = onComplete;
            
            CreateVisuals();
        }

        void CreateVisuals() {
            // Create Good zone (outer ring) - render first (lower sorting order)
            goodZoneRenderer = CreateDonutZone("GoodZone", 
                config.goodZoneOuterRadius, 
                config.goodZoneInnerRadius, 
                config.goodZoneColor,
                config.sortingOrder);
            
            // Create Perfect zone (inner area) - slightly overlaps good zone to eliminate gap
            // Outer radius extends slightly into the good zone for seamless appearance
            perfectZoneRenderer = CreateDonutZone("PerfectZone", 
                config.goodZoneInnerRadius + ZoneOverlap, // Overlap into good zone
                config.perfectZoneInnerRadius, 
                config.perfectZoneColor,
                config.sortingOrder + 1); // Renders on top
            
            // Create shrinking circle (animated)
            shrinkingCircle = CreateCircleRenderer("Shrinking", config.startRadius, 
                config.shrinkingCircleColor, config.shrinkingCircleThickness);
            SetSorting(shrinkingCircle, config.sortingOrder + 2);
            
            // Create flash overlay (for feedback)
            flashOverlay = CreateFlashOverlay();
            if (flashOverlay != null) {
                flashOverlay.sortingLayerName = config.sortingLayerName;
                flashOverlay.sortingOrder = config.sortingOrder + 10;
                flashOverlay.enabled = false;
            }
        }

        /// <summary>
        /// Create a filled donut/ring zone using a procedural texture
        /// </summary>
        SpriteRenderer CreateDonutZone(string name, float outerRadius, float innerRadius, Color color, int sortOrder) {
            GameObject go = new GameObject($"Zone_{name}");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDonutSprite(outerRadius, innerRadius);
            sr.color = color;
            sr.sortingLayerName = config.sortingLayerName;
            sr.sortingOrder = sortOrder;
            
            // Scale to match world units (sprite is normalized, we scale to outer radius * 2)
            float scale = outerRadius * 2f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            
            return sr;
        }

        /// <summary>
        /// Create a donut-shaped sprite (ring/annulus)
        /// No soft inner edge - crisp boundary for seamless zone stacking
        /// </summary>
        Sprite CreateDonutSprite(float outerRadius, float innerRadius) {
            Texture2D tex = new Texture2D(TextureResolution, TextureResolution, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            
            float center = TextureResolution / 2f;
            
            // Normalize inner radius relative to outer (0 to 0.5 range)
            float normalizedOuter = 0.5f;
            float normalizedInner = (outerRadius > 0) ? (innerRadius / outerRadius) * 0.5f : 0f;
            
            Color32[] pixels = new Color32[TextureResolution * TextureResolution];
            
            for (int y = 0; y < TextureResolution; y++) {
                for (int x = 0; x < TextureResolution; x++) {
                    float dx = (x - center) / TextureResolution;
                    float dy = (y - center) / TextureResolution;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    // Check if within donut
                    if (dist <= normalizedOuter && dist >= normalizedInner) {
                        float alpha = 1f;
                        
                        // Soft outer edge only (for nice anti-aliasing at outer boundary)
                        float outerEdgeDist = normalizedOuter - dist;
                        float edgeSoftness = 0.015f; // Reduced softness for crisper edges
                        if (outerEdgeDist < edgeSoftness) {
                            alpha *= outerEdgeDist / edgeSoftness;
                        }
                        
                        // NO soft inner edge - this allows zones to stack seamlessly
                        // The zone rendered on top will have a clean inner boundary
                        
                        pixels[y * TextureResolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    } else {
                        pixels[y * TextureResolution + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }
            
            tex.SetPixels32(pixels);
            tex.Apply();
            
            return Sprite.Create(tex, new Rect(0, 0, TextureResolution, TextureResolution), 
                Vector2.one * 0.5f, TextureResolution);
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
            
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            
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

        SpriteRenderer CreateFlashOverlay() {
            GameObject go = new GameObject("FlashOverlay");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFilledCircleSprite(64);
            sr.color = Color.white;
            go.transform.localScale = Vector3.one * config.startRadius * 2.5f;
            
            return sr;
        }

        Sprite CreateFilledCircleSprite(int resolution) {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            
            float center = resolution / 2f;
            float radius = resolution / 2f - 1;
            
            Color32[] pixels = new Color32[resolution * resolution];
            
            for (int y = 0; y < resolution; y++) {
                for (int x = 0; x < resolution; x++) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius) {
                        float alpha = Mathf.Clamp01((radius - dist) / 2f);
                        pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    } else {
                        pixels[y * resolution + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }
            
            tex.SetPixels32(pixels);
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
            
            // Update color based on current zone
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
            MinigameResultTier currentZone = config.GetCurrentZone(currentRadius);
            
            Color targetColor = currentZone switch {
                MinigameResultTier.Perfect => config.shrinkingInPerfectZoneColor,
                MinigameResultTier.Good => config.shrinkingInGoodZoneColor,
                _ => config.shrinkingCircleColor
            };
            
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
            
            yield return new WaitForSeconds(0.1f);
            
            MinigameResult result = new MinigameResult {
                Tier = tier,
                Accuracy = accuracy,
                TimeRemaining = config.duration - elapsedTime
            };
            
            onComplete?.Invoke(result);
        }

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
            CleanupSpriteTexture(goodZoneRenderer);
            CleanupSpriteTexture(perfectZoneRenderer);
            CleanupSpriteTexture(flashOverlay);
        }

        void CleanupSpriteTexture(SpriteRenderer sr) {
            if (sr != null && sr.sprite != null && sr.sprite.texture != null) {
                Destroy(sr.sprite.texture);
            }
        }
    }
}