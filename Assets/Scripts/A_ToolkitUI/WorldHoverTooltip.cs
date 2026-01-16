using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;

namespace Abracodabra.UI.Toolkit {
    
    /// <summary>
    /// Attach this to track what destroys an object - logs stack trace on destruction
    /// </summary>
    public class DestroyTracker : MonoBehaviour {
        void OnDestroy() {
            Debug.LogError($"[DestroyTracker] '{gameObject.name}' is being DESTROYED! Stack trace:\n{System.Environment.StackTrace}");
        }
    }
    
    public class WorldHoverTooltip : MonoBehaviour {
        [Header("References")]
        [SerializeField] Camera mainCamera;
        [SerializeField] TileInteractionManager tileInteractionManager;

        [Header("Positioning Mode")]
        [Tooltip("If enabled, tooltip follows the mouse cursor instead of being anchored to the tile center.")]
        [SerializeField] bool followCursor = false;
        
        [Tooltip("Offset from cursor position when followCursor is enabled.")]
        [SerializeField] Vector2 cursorOffset = new Vector2(0.5f, 0.5f);

        [Header("Tooltip Settings")]
        [SerializeField] Vector2 tooltipOffset = new Vector2(0f, 1.5f);
        [SerializeField] float zOffset = -5f;
        [SerializeField] float fontSize = 4f;
        [SerializeField] Color textColor = Color.white;

        [Header("Text Style")]
        [Tooltip("Enable italic/cursive text style.")]
        [SerializeField] bool useItalic = false;
        
        [Tooltip("Enable bold text style.")]
        [SerializeField] bool useBold = false;

        [Header("Sorting")]
        [SerializeField] string sortingLayerName = "UI";
        [SerializeField] int sortingOrder = 5000;

        [Header("Debug")]
        [SerializeField] bool showDebug = false;
        [SerializeField] bool forceShowTestText = false;
        [SerializeField] bool trackDestruction = false;

        TextMeshPro tooltipText;
        Transform tooltipTransform;
        
        string tooltipChildName;

        void Awake() {
            tooltipChildName = $"WorldHoverTooltip_Text_{GetInstanceID()}";
        }

        void Start() {
            if (mainCamera == null) {
                mainCamera = Camera.main;
            }

            if (tileInteractionManager == null) {
                tileInteractionManager = TileInteractionManager.Instance;
            }

            CreateTooltip();
        }

        void CreateTooltip() {
            var existing = transform.Find(tooltipChildName);
            if (existing != null) {
                tooltipTransform = existing;
                tooltipText = existing.GetComponent<TextMeshPro>();
                
                if (tooltipText != null) {
                    if (showDebug) Debug.Log($"[WorldHoverTooltip] Reusing existing tooltip");
                    ApplyFontStyle();
                    return;
                }
            }

            var tooltipGO = new GameObject(tooltipChildName);
            tooltipGO.transform.SetParent(transform);
            tooltipTransform = tooltipGO.transform;
            tooltipTransform.localPosition = Vector3.zero;

            if (trackDestruction) {
                tooltipGO.AddComponent<DestroyTracker>();
            }

            tooltipText = tooltipGO.AddComponent<TextMeshPro>();

            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.fontSize = fontSize;
            tooltipText.color = textColor;

            RectTransform rect = tooltipText.rectTransform;
            rect.sizeDelta = new Vector2(20f, 5f);

            tooltipText.textWrappingMode = TextWrappingModes.NoWrap;
            tooltipText.overflowMode = TextOverflowModes.Overflow;

            var meshRenderer = tooltipText.GetComponent<MeshRenderer>();
            if (meshRenderer != null) {
                meshRenderer.sortingLayerName = sortingLayerName;
                meshRenderer.sortingOrder = sortingOrder;
            }

            tooltipText.outlineWidth = 0.2f;
            tooltipText.outlineColor = Color.black;

            ApplyFontStyle();

            tooltipGO.SetActive(false);

            if (showDebug) Debug.Log($"[WorldHoverTooltip] Created new tooltip: {tooltipChildName}");
        }

        void ApplyFontStyle() {
            if (tooltipText == null) return;

            FontStyles style = FontStyles.Normal;
            
            if (useItalic) style |= FontStyles.Italic;
            if (useBold) style |= FontStyles.Bold;
            
            tooltipText.fontStyle = style;
        }

        void Update() {
            if (this == null || gameObject == null) return;
            UpdateTooltip();
        }

        void UpdateTooltip() {
            if (tooltipTransform == null || tooltipText == null) {
                var existing = transform.Find(tooltipChildName);
                if (existing != null) {
                    tooltipTransform = existing;
                    tooltipText = existing.GetComponent<TextMeshPro>();
                }
                
                if (tooltipTransform == null || tooltipText == null) {
                    if (showDebug) Debug.LogWarning($"[WorldHoverTooltip] Tooltip missing, recreating...");
                    CreateTooltip();
                    return;
                }
            }

            if (forceShowTestText) {
                tooltipTransform.position = transform.position + new Vector3(0, 1, zOffset);
                tooltipText.text = $"DEBUG TOOLTIP";
                tooltipTransform.gameObject.SetActive(true);
                return;
            }

            if (mainCamera == null || tileInteractionManager == null) return;

            Vector3Int? hoveredCell = tileInteractionManager.CurrentlyHoveredCell;

            if (!hoveredCell.HasValue) {
                if (tooltipTransform.gameObject.activeSelf) tooltipTransform.gameObject.SetActive(false);
                return;
            }

            Vector3Int cellPos = hoveredCell.Value;
            GridPosition gridPos = new GridPosition(cellPos);

            string content = GetTooltipContent(gridPos);

            if (string.IsNullOrEmpty(content)) {
                if (tooltipTransform.gameObject.activeSelf) tooltipTransform.gameObject.SetActive(false);
                return;
            }

            Vector3 finalPos;
            
            if (followCursor) {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
                finalPos = new Vector3(worldPos.x + cursorOffset.x, worldPos.y + cursorOffset.y, zOffset);
            } else {
                Vector3 cellCenter = tileInteractionManager.CellCenterWorld(cellPos);
                finalPos = cellCenter + new Vector3(tooltipOffset.x, tooltipOffset.y, zOffset);
            }

            tooltipTransform.position = finalPos;
            tooltipText.text = content;
            tooltipText.sortingOrder = sortingOrder;

            if (!tooltipTransform.gameObject.activeSelf) {
                tooltipTransform.gameObject.SetActive(true);
            }
        }

        string GetTooltipContent(GridPosition gridPos) {
            string entityName = GetEntityNameAtPosition(gridPos);
            if (!string.IsNullOrEmpty(entityName)) {
                return entityName;
            }

            TileDefinition tileDef = tileInteractionManager.HoveredTileDef;
            if (tileDef != null && !string.IsNullOrEmpty(tileDef.displayName)) {
                return tileDef.displayName;
            }

            return null;
        }

        string GetEntityNameAtPosition(GridPosition gridPos) {
            if (GridPositionManager.Instance == null) return null;

            MultiTileEntity multiTileEntity = GridPositionManager.Instance.GetMultiTileEntityAt(gridPos);
            if (multiTileEntity != null) {
                return GetDisplayNameFromGameObject(multiTileEntity.gameObject);
            }

            HashSet<GridEntity> entities = GridPositionManager.Instance.GetEntitiesAt(gridPos);
            if (entities == null || entities.Count == 0) return null;

            var nonPlayerEntities = entities
                .Where(e => e != null && e.GetComponent<GardenerController>() == null)
                .ToList();

            if (nonPlayerEntities.Count > 0) {
                return GetDisplayNameFromGameObject(nonPlayerEntities[0].gameObject);
            }

            return null;
        }

        string GetDisplayNameFromGameObject(GameObject go) {
            if (go == null) return null;

            var animal = go.GetComponent<AnimalController>();
            if (animal != null) return animal.GetDisplayName();

            var plant = go.GetComponent<PlantGrowth>();
            if (plant != null) {
                if (plant.geneRuntimeState?.template != null) return plant.geneRuntimeState.template.templateName;
                if (plant.seedTemplate != null) return plant.seedTemplate.templateName;
                return "Plant";
            }

            var dorisType = System.Type.GetType("Abracodabra.Ecosystem.DorisController, Assembly-CSharp");
            if (dorisType != null) {
                var doris = go.GetComponent(dorisType);
                if (doris != null) return "Doris";
            }

            string name = go.name.Replace("(Clone)", "").Trim();
            return name;
        }

        void OnDestroy() {
            if (tooltipTransform != null) Destroy(tooltipTransform.gameObject);
        }

        void OnValidate() {
            // Apply font style changes in editor
            ApplyFontStyle();
        }

        void OnDrawGizmos() {
            if (tooltipTransform != null && tooltipTransform.gameObject.activeSelf) {
                Gizmos.color = Color.green;
                
                if (followCursor) {
                    Vector3 cursorApprox = tooltipTransform.position - new Vector3(cursorOffset.x, cursorOffset.y, 0);
                    Gizmos.DrawLine(tooltipTransform.position, cursorApprox);
                } else {
                    Gizmos.DrawLine(tooltipTransform.position, tooltipTransform.position - new Vector3(tooltipOffset.x, tooltipOffset.y, zOffset));
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(tooltipTransform.position, 0.2f);
            }
        }
    }
}