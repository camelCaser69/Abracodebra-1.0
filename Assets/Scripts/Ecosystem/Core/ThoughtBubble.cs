using UnityEngine;
using TMPro;

namespace Ecosystem
{
    /// <summary>
    /// Component for managing thought bubbles that display entity thoughts/intentions
    /// as part of the visual storytelling system.
    /// </summary>
    public class ThoughtBubble : MonoBehaviour
    {
        [Header("Text Settings")]
        public TMP_Text textComponent;
        public float displayTime = 4f;
        public float fadeTime = 0.5f;
        
        [Header("Visual Settings")]
        public RectTransform bubbleRect;
        public float minWidth = 80f;
        public float maxWidth = 200f;
        public float paddingPerCharacter = 0.5f;
        public Vector3 offset = new Vector3(0f, 1.5f, 0f);
        
        [Header("Animation")]
        public float bobAmount = 0.1f;
        public float bobSpeed = 1f;
        public AnimationCurve appearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // Internal state
        private Transform followTarget;
        private CanvasGroup canvasGroup;
        private float timer = 0f;
        private float initialY;
        private Color textColor;
        private string currentText = "";
        private bool isFading = false;
        
        private void Awake()
        {
            if (textComponent == null)
                textComponent = GetComponentInChildren<TMP_Text>();
                
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                
            // Start invisible
            canvasGroup.alpha = 0f;
            
            // Store original text color
            if (textComponent != null)
                textColor = textComponent.color;
                
            // Store initial Y position
            initialY = transform.localPosition.y;
        }
        
        private void Start()
        {
            // Find parent to follow if not explicitly set
            if (followTarget == null)
                followTarget = transform.parent;
                
            // Set initial timer
            timer = displayTime;
        }
        
        private void Update()
        {
            // Follow target if set
            if (followTarget != null)
            {
                Vector3 newPos = followTarget.position + offset;
                
                // Add bobbing motion
                newPos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmount;
                
                transform.position = newPos;
            }
            
            // Handle timing and fading
            if (!isFading)
            {
                timer -= Time.deltaTime;
                
                if (timer <= 0f)
                {
                    isFading = true;
                    timer = fadeTime;
                }
            }
            else
            {
                timer -= Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, 1f - (timer / fadeTime));
                
                if (timer <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }
        
        /// <summary>
        /// Set the text and duration of the thought bubble.
        /// </summary>
        public void SetText(string text, float duration = -1f)
        {
            if (textComponent != null)
            {
                currentText = text;
                textComponent.text = text;
                
                // Reset timer
                if (duration > 0f)
                    timer = duration;
                else
                    timer = displayTime;
                    
                // Resize bubble based on text length
                if (bubbleRect != null)
                {
                    float width = Mathf.Clamp(text.Length * paddingPerCharacter, minWidth, maxWidth);
                    bubbleRect.sizeDelta = new Vector2(width, bubbleRect.sizeDelta.y);
                }
                
                // Reset fade state
                isFading = false;
                canvasGroup.alpha = 1f;
            }
        }
        
        /// <summary>
        /// Set the target to follow.
        /// </summary>
        public void SetTarget(Transform target)
        {
            followTarget = target;
        }
        
        /// <summary>
        /// Factory method to create a thought bubble for an entity.
        /// </summary>
        public static ThoughtBubble Create(GameObject prefab, Transform parent, string text, float duration = -1f)
        {
            if (prefab == null)
            {
                Debug.LogWarning("ThoughtBubble.Create: prefab is null");
                return null;
            }
            
            // Create the bubble
            GameObject bubbleObj = Instantiate(prefab, parent.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
            
            // Ensure it's in a Canvas
            Canvas canvas = bubbleObj.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                // If not in a canvas, parent to scene root
                bubbleObj.transform.SetParent(null);
            }
            else
            {
                // If in a canvas, make it a child of the parent transform
                bubbleObj.transform.SetParent(parent);
            }
            
            // Get or add bubble component
            ThoughtBubble bubble = bubbleObj.GetComponent<ThoughtBubble>();
            if (bubble == null)
                bubble = bubbleObj.AddComponent<ThoughtBubble>();
                
            // Set up the bubble
            bubble.SetTarget(parent);
            bubble.SetText(text, duration);
            
            return bubble;
        }
    }
}