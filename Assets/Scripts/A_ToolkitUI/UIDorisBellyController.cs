// FILE: Assets/Scripts/A_ToolkitUI/UIDorisBellyController.cs
// ============================================================================
//  DORIS'S BELLY (Block 1 B3). A row of pips above Doris (filled = food in her
//  belly, hollow = empty; shape and brightness, never hue alone) and, while she
//  is hovered, one short explanation. A view over DorisHungerSystem hunger:
//  belly = round((Max - hunger) / Max * pips), see DuskRules.BellyPipsFor.
//  Lives inside the HUD panel, so it shows whenever the world does.
// ============================================================================
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;
using Abracodabra.Ecosystem;

namespace Abracodabra.UI.Toolkit {
 public class UIDorisBellyController {
  const float PipSize = 11f;
  const float PipGap = 3f;
  const float AnchorLift = 6f; // px between Doris's sprite top and the pips

  static readonly Color PipFilledColor = new Color(0.80f, 0.88f, 0.72f, 1f);
  static readonly Color PipEdgeColor = new Color(0.80f, 0.88f, 0.72f, 0.95f);
  static readonly Color PipShadowColor = new Color(0.06f, 0.08f, 0.06f, 0.85f);
  static readonly Color HoverBackground = new Color(0.09f, 0.11f, 0.09f, 0.92f);
  static readonly Color HoverBorder = new Color(0.31f, 0.36f, 0.29f, 1f);
  static readonly Color HoverTitleColor = new Color(0.86f, 0.91f, 0.80f, 1f);
  static readonly Color HoverTextColor = new Color(0.72f, 0.79f, 0.68f, 1f);

  VisualElement parent;
  VisualElement container;
  VisualElement pipRow;
  VisualElement hoverBox;
  Label hoverTitle;
  Label hoverRules;
  VisualElement[] pips;

  DorisController doris;
  DorisHungerSystem hunger;
  Camera worldCamera;

  public int ShownPips { get; private set; } = -1;
  public bool IsHoverShown => hoverBox != null && hoverBox.style.display == DisplayStyle.Flex;

  public void Initialize(VisualElement hudParent, DorisController dorisController) {
   parent = hudParent;
   doris = dorisController;
   hunger = doris != null ? doris.HungerSystem : null;
   if (parent == null || doris == null || hunger == null) return;

   Build();

   hunger.OnHungerChanged += HandleHungerChanged;
   doris.OnHoverChanged += HandleHoverChanged;
   HandleHungerChanged(hunger.CurrentHunger, hunger.MaxHunger);
   HandleHoverChanged(doris.IsHovered);
  }

  public void Dispose() {
   if (hunger != null) hunger.OnHungerChanged -= HandleHungerChanged;
   if (doris != null) doris.OnHoverChanged -= HandleHoverChanged;
   container?.RemoveFromHierarchy();
  }

  /// <summary>Call once a frame (GameUIManager.LateUpdate): keeps the pips above Doris.</summary>
  public void Refresh() {
   if (container == null || doris == null) return;

   if (worldCamera == null) worldCamera = Camera.main;
   if (worldCamera == null || container.panel == null) return;

   // Bottom centre of the readout on Doris's sprite top. Uses the laid-out size (a percentage
   // translate kept the old width when the hover box opened and pushed the pips sideways).
   Vector2 panelPoint = RuntimePanelUtils.CameraTransformWorldToPanel(container.panel, doris.BellyAnchorWorldPosition, worldCamera);
   Vector2 local = parent.WorldToLocal(panelPoint);
   float width = float.IsNaN(container.resolvedStyle.width) ? 0f : container.resolvedStyle.width;
   float height = float.IsNaN(container.resolvedStyle.height) ? 0f : container.resolvedStyle.height;
   container.style.left = local.x - width * 0.5f;
   container.style.top = local.y - height - AnchorLift;
  }

  void Build() {
   int pipCount = DuskRules.Active.bellyPips;

   // Anchored at its bottom centre, on Doris's sprite top.
   container = new VisualElement { name = "DorisBelly", pickingMode = PickingMode.Ignore };
   container.style.position = Position.Absolute;
   container.style.alignItems = Align.Center;
   container.style.flexDirection = FlexDirection.ColumnReverse;

   pipRow = new VisualElement { name = "DorisBellyPips", pickingMode = PickingMode.Ignore };
   pipRow.style.flexDirection = FlexDirection.Row;
   pipRow.style.paddingLeft = 3;
   pipRow.style.paddingRight = 3;
   pipRow.style.paddingTop = 3;
   pipRow.style.paddingBottom = 3;
   pipRow.style.backgroundColor = PipShadowColor;
   SetRadius(pipRow, 3);

   pips = new VisualElement[pipCount];
   for (int i = 0; i < pipCount; i++) {
    var pip = new VisualElement { pickingMode = PickingMode.Ignore };
    pip.style.width = PipSize;
    pip.style.height = PipSize;
    pip.style.marginLeft = i == 0 ? 0 : PipGap;
    SetBorder(pip, 1.5f, PipEdgeColor);
    pips[i] = pip;
    pipRow.Add(pip);
   }

   hoverBox = new VisualElement { name = "DorisBellyHover", pickingMode = PickingMode.Ignore };
   hoverBox.style.marginBottom = 4;
   hoverBox.style.paddingLeft = 8;
   hoverBox.style.paddingRight = 8;
   hoverBox.style.paddingTop = 5;
   hoverBox.style.paddingBottom = 5;
   hoverBox.style.backgroundColor = HoverBackground;
   SetBorder(hoverBox, 1f, HoverBorder);
   SetRadius(hoverBox, 4);
   hoverBox.style.display = DisplayStyle.None;

   hoverTitle = MakeLabel(16, HoverTitleColor, true);
   hoverRules = MakeLabel(13, HoverTextColor, false);
   hoverBox.Add(hoverTitle);
   hoverBox.Add(hoverRules);

   // ColumnReverse: pips at the bottom (on Doris), the hover box above them.
   container.Add(pipRow);
   container.Add(hoverBox);
   parent.Add(container);
  }

  void HandleHungerChanged(float currentHunger, float maxHunger) {
   if (pips == null) return;
   var rules = DuskRules.Active;
   int filled = rules.BellyPipsFor(currentHunger, maxHunger);
   ShownPips = filled;

   for (int i = 0; i < pips.Length; i++) {
    pips[i].style.backgroundColor = i < filled ? PipFilledColor : new Color(0f, 0f, 0f, 0f);
   }

   hoverTitle.text = $"Belly {filled} / {rules.bellyPips}";
   hoverRules.text =
    "empties during the Day\n" +
    "supper at dusk: every fruit on a living plant\n" +
    $"at {rules.eatsPlantAtBellyPips} she eats a plant (once a Day)";
  }

  void HandleHoverChanged(bool isHovered) {
   if (hoverBox == null) return;
   hoverBox.style.display = isHovered ? DisplayStyle.Flex : DisplayStyle.None;
  }

  static Label MakeLabel(int fontSize, Color color, bool bold) {
   var label = new Label { pickingMode = PickingMode.Ignore };
   label.style.fontSize = fontSize;
   label.style.color = color;
   label.style.marginTop = 0;
   label.style.marginBottom = 0;
   label.style.paddingTop = 0;
   label.style.paddingBottom = 0;
   label.style.whiteSpace = WhiteSpace.NoWrap;
   if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
   return label;
  }

  static void SetBorder(VisualElement element, float width, Color color) {
   element.style.borderTopWidth = width;
   element.style.borderBottomWidth = width;
   element.style.borderLeftWidth = width;
   element.style.borderRightWidth = width;
   element.style.borderTopColor = color;
   element.style.borderBottomColor = color;
   element.style.borderLeftColor = color;
   element.style.borderRightColor = color;
  }

  static void SetRadius(VisualElement element, float radius) {
   element.style.borderTopLeftRadius = radius;
   element.style.borderTopRightRadius = radius;
   element.style.borderBottomLeftRadius = radius;
   element.style.borderBottomRightRadius = radius;
  }
 }
}
