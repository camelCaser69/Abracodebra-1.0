using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;
using Abracodabra.Genes;

namespace Abracodabra.UI.Toolkit {
    public class WorldHoverTooltip : MonoBehaviour {
        [Header("References")]
        [SerializeField] Camera mainCamera;
        [SerializeField] TileInteractionManager tileInteractionManager;
        [SerializeField] UIDocument uiDocument;

        [Header("Tooltip Settings")]
        [SerializeField] Vector2 tooltipOffset = new Vector2(0f, 30f);

        [Header("Debug")]
        [SerializeField] bool showDebug = false;

        Label tooltipLabel;
        VisualElement rootElement;

        void Start() {
            if (mainCamera == null) {
                mainCamera = Camera.main;
            }

            if (tileInteractionManager == null) {
                tileInteractionManager = TileInteractionManager.Instance;
            }

            if (uiDocument == null) {
                uiDocument = FindFirstObjectByType<UIDocument>();
            }

            if (uiDocument != null) {
                rootElement = uiDocument.rootVisualElement;
                tooltipLabel = rootElement.Q<Label>("world-hover-tooltip");

                if (tooltipLabel == null) {
                    Debug.LogWarning("[WorldHoverTooltip] Could not find 'world-hover-tooltip' label in UI Document. Creating fallback.");
                    CreateFallbackLabel();
                }
            }
            else {
                Debug.LogError("[WorldHoverTooltip] No UIDocument found!");
            }

            if (showDebug) Debug.Log("[WorldHoverTooltip] Initialized with UI Toolkit");
        }

        void CreateFallbackLabel() {
            tooltipLabel = new Label();
            tooltipLabel.name = "world-hover-tooltip";
            tooltipLabel.style.position = Position.Absolute;
            tooltipLabel.style.fontSize = 14;
            tooltipLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            tooltipLabel.style.color = Color.white;
            tooltipLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            tooltipLabel.style.display = DisplayStyle.None;
            tooltipLabel.pickingMode = PickingMode.Ignore;

            var hudPanel = rootElement.Q<VisualElement>("HUDPanel");
            if (hudPanel != null) {
                hudPanel.Add(tooltipLabel);
            }
            else {
                rootElement.Add(tooltipLabel);
            }
        }

        void Update() {
            if (tooltipLabel == null) return;
            UpdateTooltip();
        }

        void UpdateTooltip() {
            if (mainCamera == null || tileInteractionManager == null) {
                HideTooltip();
                return;
            }

            Vector3Int? hoveredCell = tileInteractionManager.CurrentlyHoveredCell;

            if (!hoveredCell.HasValue) {
                HideTooltip();
                return;
            }

            Vector3Int cellPos = hoveredCell.Value;
            GridPosition gridPos = new GridPosition(cellPos);

            string content = GetTooltipContent(gridPos);

            if (string.IsNullOrEmpty(content)) {
                HideTooltip();
                return;
            }

            Vector3 cellCenter = tileInteractionManager.CellCenterWorld(cellPos);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(cellCenter);

            float uiToolkitY = Screen.height - screenPos.y;

            float finalX = screenPos.x + tooltipOffset.x;
            float finalY = uiToolkitY + tooltipOffset.y;

            tooltipLabel.style.left = finalX;
            tooltipLabel.style.top = finalY;
            tooltipLabel.style.translate = new Translate(Length.Percent(-50), 0);

            tooltipLabel.text = content;

            if (tooltipLabel.style.display == DisplayStyle.None) {
                tooltipLabel.style.display = DisplayStyle.Flex;
            }

            if (showDebug) {
                Debug.Log($"[WorldHoverTooltip] Showing '{content}' at screen ({finalX:F0}, {finalY:F0})");
            }
        }

        void HideTooltip() {
            if (tooltipLabel != null && tooltipLabel.style.display != DisplayStyle.None) {
                tooltipLabel.style.display = DisplayStyle.None;
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

        void OnDisable() {
            HideTooltip();
        }
    }
}