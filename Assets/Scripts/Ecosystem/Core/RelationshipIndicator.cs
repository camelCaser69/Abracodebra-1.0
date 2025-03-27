using UnityEngine;
using System.Collections.Generic;

namespace Ecosystem
{
    /// <summary>
    /// Visual indicator that draws lines between related entities to show their relationships.
    /// This is a key part of the visual storytelling system.
    /// </summary>
    public class RelationshipIndicator : MonoBehaviour
    {
        [System.Serializable]
        public enum RelationshipType
        {
            PredatorPrey,    // Red - hunting relationship
            Symbiotic,       // Green - mutually beneficial
            Competitive,     // Yellow - competing for resources
            Neutral,         // Blue - opportunistic or passing
            Complex          // Purple - changing or uncertain
        }
        
        [System.Serializable]
        public class Relationship
        {
            public Transform entity1;
            public Transform entity2;
            public RelationshipType type;
            public float strength;        // 0-1 value for line thickness
            public float duration = -1f;  // How long relationship lasts (-1 = permanent)
            public float timeRemaining;   // Time left for temporary relationships
            
            public Relationship(Transform e1, Transform e2, RelationshipType t, float s, float d = -1f)
            {
                entity1 = e1;
                entity2 = e2;
                type = t;
                strength = s;
                duration = d;
                timeRemaining = d;
            }
        }
        
        [Header("Line Settings")]
        public float baseLineWidth = 0.1f;
        public float maxLineWidth = 0.5f;
        public float lineZOffset = -0.1f;  // Small offset to render behind sprites
        
        [Header("Colors")]
        public Color predatorPreyColor = Color.red;
        public Color symbioticColor = Color.green;
        public Color competitiveColor = Color.yellow;
        public Color neutralColor = Color.blue;
        public Color complexColor = new Color(0.5f, 0f, 0.5f); // Purple
        
        [Header("Animation")]
        public bool animateLines = true;
        public float pulseSpeed = 1f;
        public float pulseAmount = 0.2f;
        
        [Header("Visibility")]
        [Tooltip("Show relationship lines only when this key is held down")]
        public KeyCode visibilityKey = KeyCode.Tab;
        [Tooltip("Distance at which relationships are visible")]
        public float visibilityDistance = 20f;
        [Tooltip("Only show relationships within range of this transform (null = show all)")]
        public Transform focusEntity;
        
        // Internal state
        private List<Relationship> relationships = new List<Relationship>();
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        private Transform cameraTransform;
        private bool areRelationshipsVisible = false;
        
        private void Start()
        {
            // Find main camera
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            
            // Initially hide relationships
            UpdateVisibility(false);
        }
        
        private void Update()
        {
            // Toggle visibility with key
            if (Input.GetKeyDown(visibilityKey))
            {
                UpdateVisibility(true);
            }
            else if (Input.GetKeyUp(visibilityKey))
            {
                UpdateVisibility(false);
            }
            
            // Update existing relationships
            UpdateRelationships();
            
            // Generate line renderers if needed
            EnsureLineRenderers();
            
            // Update line positions and properties
            UpdateLineRenderers();
        }
        
        private void UpdateVisibility(bool visible)
        {
            areRelationshipsVisible = visible;
            
            // Update existing line renderers
            foreach (LineRenderer line in lineRenderers)
            {
                if (line != null)
                    line.enabled = visible;
            }
        }
        
        private void UpdateRelationships()
        {
            // Update time remaining on temporary relationships
            for (int i = relationships.Count - 1; i >= 0; i--)
            {
                Relationship rel = relationships[i];
                
                // Check if either entity is gone
                if (rel.entity1 == null || rel.entity2 == null)
                {
                    relationships.RemoveAt(i);
                    continue;
                }
                
                // Update temporary relationships
                if (rel.duration > 0)
                {
                    rel.timeRemaining -= Time.deltaTime;
                    if (rel.timeRemaining <= 0)
                    {
                        relationships.RemoveAt(i);
                        continue;
                    }
                    
                    // Fade out when close to expiring
                    if (rel.timeRemaining < 1f)
                    {
                        rel.strength *= rel.timeRemaining;
                    }
                }
            }
        }
        
        private void EnsureLineRenderers()
        {
            // Make sure we have enough line renderers
            while (lineRenderers.Count < relationships.Count)
            {
                GameObject lineObj = new GameObject("RelationshipLine");
                lineObj.transform.SetParent(transform);
                
                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = baseLineWidth;
                line.endWidth = baseLineWidth;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.enabled = areRelationshipsVisible;
                
                lineRenderers.Add(line);
            }
            
            // Disable extra line renderers
            for (int i = relationships.Count; i < lineRenderers.Count; i++)
            {
                if (lineRenderers[i] != null)
                    lineRenderers[i].enabled = false;
            }
        }
        
        private void UpdateLineRenderers()
        {
            if (!areRelationshipsVisible)
                return;
                
            for (int i = 0; i < relationships.Count; i++)
            {
                if (i >= lineRenderers.Count || lineRenderers[i] == null)
                    continue;
                    
                Relationship rel = relationships[i];
                LineRenderer line = lineRenderers[i];
                
                // Skip if entities no longer exist
                if (rel.entity1 == null || rel.entity2 == null)
                {
                    line.enabled = false;
                    continue;
                }
                
                // Check if relationship is in range of focus entity
                if (focusEntity != null)
                {
                    float distToEntity1 = Vector3.Distance(focusEntity.position, rel.entity1.position);
                    float distToEntity2 = Vector3.Distance(focusEntity.position, rel.entity2.position);
                    
                    if (distToEntity1 > visibilityDistance && distToEntity2 > visibilityDistance)
                    {
                        line.enabled = false;
                        continue;
                    }
                }
                
                // Set line positions
                Vector3 pos1 = rel.entity1.position;
                Vector3 pos2 = rel.entity2.position;
                
                // Add small Z offset to render behind sprites
                pos1.z = lineZOffset;
                pos2.z = lineZOffset;
                
                line.SetPosition(0, pos1);
                line.SetPosition(1, pos2);
                
                // Set line color based on relationship type
                switch (rel.type)
                {
                    case RelationshipType.PredatorPrey:
                        line.startColor = predatorPreyColor;
                        line.endColor = predatorPreyColor;
                        break;
                    case RelationshipType.Symbiotic:
                        line.startColor = symbioticColor;
                        line.endColor = symbioticColor;
                        break;
                    case RelationshipType.Competitive:
                        line.startColor = competitiveColor;
                        line.endColor = competitiveColor;
                        break;
                    case RelationshipType.Neutral:
                        line.startColor = neutralColor;
                        line.endColor = neutralColor;
                        break;
                    case RelationshipType.Complex:
                        line.startColor = complexColor;
                        line.endColor = complexColor;
                        break;
                }
                
                // Set line width based on relationship strength
                float width = Mathf.Lerp(baseLineWidth, maxLineWidth, rel.strength);
                
                // Add pulsing effect for animation
                if (animateLines)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                    width *= pulse;
                }
                
                line.startWidth = width;
                line.endWidth = width;
                
                // Enable the line
                line.enabled = true;
            }
        }
        
        // Public methods for other components to use
        
        /// <summary>
        /// Add a relationship between two entities.
        /// </summary>
        public void AddRelationship(Transform entity1, Transform entity2, RelationshipType type, float strength = 1f, float duration = -1f)
        {
            // Check if relationship already exists
            for (int i = 0; i < relationships.Count; i++)
            {
                Relationship rel = relationships[i];
                if ((rel.entity1 == entity1 && rel.entity2 == entity2) || 
                    (rel.entity1 == entity2 && rel.entity2 == entity1))
                {
                    // Update existing relationship
                    rel.type = type;
                    rel.strength = strength;
                    
                    // Only update duration if the new one is longer
                    if (duration > rel.timeRemaining || rel.duration < 0)
                    {
                        rel.duration = duration;
                        rel.timeRemaining = duration;
                    }
                    
                    return;
                }
            }
            
            // Create new relationship
            relationships.Add(new Relationship(entity1, entity2, type, strength, duration));
        }
        
        /// <summary>
        /// Remove a relationship between two entities.
        /// </summary>
        public void RemoveRelationship(Transform entity1, Transform entity2)
        {
            for (int i = 0; i < relationships.Count; i++)
            {
                Relationship rel = relationships[i];
                if ((rel.entity1 == entity1 && rel.entity2 == entity2) || 
                    (rel.entity1 == entity2 && rel.entity2 == entity1))
                {
                    relationships.RemoveAt(i);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Remove all relationships involving an entity.
        /// </summary>
        public void RemoveAllRelationships(Transform entity)
        {
            for (int i = relationships.Count - 1; i >= 0; i--)
            {
                Relationship rel = relationships[i];
                if (rel.entity1 == entity || rel.entity2 == entity)
                {
                    relationships.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Change the relationship type between two entities.
        /// </summary>
        public void ChangeRelationshipType(Transform entity1, Transform entity2, RelationshipType newType)
        {
            for (int i = 0; i < relationships.Count; i++)
            {
                Relationship rel = relationships[i];
                if ((rel.entity1 == entity1 && rel.entity2 == entity2) || 
                    (rel.entity1 == entity2 && rel.entity2 == entity1))
                {
                    rel.type = newType;
                    return;
                }
            }
        }
        
        /// <summary>
        /// Change the focus entity for relationship visibility.
        /// </summary>
        public void SetFocusEntity(Transform newFocus)
        {
            focusEntity = newFocus;
        }
        
        /// <summary>
        /// Get the current relationship between two entities.
        /// </summary>
        public RelationshipType GetRelationshipType(Transform entity1, Transform entity2)
        {
            for (int i = 0; i < relationships.Count; i++)
            {
                Relationship rel = relationships[i];
                if ((rel.entity1 == entity1 && rel.entity2 == entity2) || 
                    (rel.entity1 == entity2 && rel.entity2 == entity1))
                {
                    return rel.type;
                }
            }
            
            return RelationshipType.Neutral; // Default
        }
    }
}