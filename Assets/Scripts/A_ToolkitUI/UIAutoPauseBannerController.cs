// FILE: Assets/Scripts/A_ToolkitUI/UIAutoPauseBannerController.cs
// ============================================================================
//  AUTO-PAUSE BANNER (Block 1 B5). When the Day pauses by itself (first wave of
//  the run, a plant died, Doris's first plant-eat: DayTally, D6) this strip at the
//  top edge says why, and how to go on. It hides as soon as the Day runs again
//  (Space) or leaves the Day. Lives in the HUD panel; it never covers the ground
//  and never takes mouse input. B6's phase banner owns the very top; this sits just below it.
// ============================================================================
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;

namespace Abracodabra.UI.Toolkit {
 public class UIAutoPauseBannerController {
  static readonly Color CardColor = new Color(0.10f, 0.12f, 0.10f, 0.95f);
  static readonly Color CardBorder = new Color(0.80f, 0.88f, 0.72f, 0.95f);
  static readonly Color KickerColor = new Color(0.56f, 0.64f, 0.53f, 1f);
  static readonly Color ReasonColor = new Color(0.93f, 0.95f, 0.88f, 1f);
  static readonly Color HintColor = new Color(0.70f, 0.77f, 0.66f, 1f);

  VisualElement strip;
  Label kickerLabel;
  Label reasonLabel;
  Label hintLabel;
  ExecutionPhaseDriver driver;
  bool subscribed;

  public bool IsVisible => strip != null && strip.style.display == DisplayStyle.Flex;
  public string ShownReason => reasonLabel != null ? reasonLabel.text : "";

  public void Initialize(VisualElement hudParent) {
   strip = new VisualElement { name = "AutoPauseBanner", pickingMode = PickingMode.Ignore };
   strip.style.position = Position.Absolute;
   strip.style.left = 0;
   strip.style.right = 0;
   strip.style.top = UIPhaseBannerController.Height + 8; // B6: below the phase banner
   strip.style.alignItems = Align.Center;
   strip.style.display = DisplayStyle.None;

   var card = new VisualElement { pickingMode = PickingMode.Ignore };
   card.style.paddingLeft = 22;
   card.style.paddingRight = 22;
   card.style.paddingTop = 8;
   card.style.paddingBottom = 8;
   card.style.alignItems = Align.Center;
   card.style.backgroundColor = CardColor;
   SetBorder(card, 2f, CardBorder);
   SetRadius(card, 8f);

   kickerLabel = MakeLabel("DAY PAUSED", 13, KickerColor, true);
   reasonLabel = MakeLabel("", 22, ReasonColor, true);
   reasonLabel.style.marginTop = 2;
   hintLabel = MakeLabel("Space to resume  ·  Tab changes speed", 14, HintColor, false);
   hintLabel.style.marginTop = 2;

   card.Add(kickerLabel);
   card.Add(reasonLabel);
   card.Add(hintLabel);
   strip.Add(card);
   hudParent.Add(strip);

   DayTally.OnAutoPause += HandleAutoPause;
   subscribed = true;
  }

  public void Dispose() {
   if (subscribed) DayTally.OnAutoPause -= HandleAutoPause;
   subscribed = false;
  }

  void HandleAutoPause(string reason) {
   if (strip == null) return;
   reasonLabel.text = reason;
   strip.style.display = DisplayStyle.Flex;
   strip.BringToFront();
  }

  /// <summary>Call once a frame (GameUIManager.LateUpdate).</summary>
  public void Refresh() {
   if (!IsVisible) return;

   if (driver == null) driver = Object.FindAnyObjectByType<ExecutionPhaseDriver>();
   if (!DayTally.DayRunning || driver == null || !driver.IsPaused) Hide();
  }

  public void Hide() {
   if (strip != null) strip.style.display = DisplayStyle.None;
  }

  static Label MakeLabel(string text, int fontSize, Color color, bool bold) {
   var label = new Label(text) { pickingMode = PickingMode.Ignore };
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
