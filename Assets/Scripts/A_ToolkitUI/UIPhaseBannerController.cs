// FILE: Assets/Scripts/A_ToolkitUI/UIPhaseBannerController.cs
// ============================================================================
//  PHASE BANNER (Block 1 B6). A slim strip along the top edge that always says
//  where the run is and what to do now:
//    PLANNING  "Plant, water, edit seeds. The wave is on the map."  [START DAY]
//    DAY       "Hands off. Space pause · Tab speed. Supper at dusk."
//    DUSK      "Supper." (then "Read the ledger, then Continue.")
//  The phase name is text, never a hue (Milan is partly red/green colour-blind);
//  the strip is one moss colour in every phase. Hidden once the run is over (the
//  end card speaks). Lives in the root of the UI document so it shows over the
//  seed editor too. GameUIManager moves the other top-edge elements below it.
// ============================================================================
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;

namespace Abracodabra.UI.Toolkit {
 public class UIPhaseBannerController {
  /// <summary>Height of the strip in UI pixels; other top-edge elements start below it.</summary>
  public const float Height = 38f;

  static readonly Color StripColor = new Color(0.10f, 0.13f, 0.10f, 0.96f);
  static readonly Color StripBorder = new Color(0.30f, 0.36f, 0.28f, 1f);
  static readonly Color PhaseColor = new Color(0.88f, 0.92f, 0.82f, 1f);
  static readonly Color NowTagColor = new Color(0.56f, 0.64f, 0.53f, 1f);
  static readonly Color NowColor = new Color(0.93f, 0.95f, 0.88f, 1f);
  static readonly Color ButtonColor = new Color(0.30f, 0.42f, 0.29f, 1f);
  static readonly Color ButtonText = new Color(0.94f, 0.96f, 0.90f, 1f);

  VisualElement strip;
  Label phaseLabel;
  Label nowLabel;
  Button startDayButton;
  ExecutionPhaseDriver driver;

  string shownPhase = "";
  string shownNow = "";

  public bool IsVisible => strip != null && strip.style.display == DisplayStyle.Flex;
  public string ShownPhase => shownPhase;
  public string ShownNow => shownNow;
  public bool StartDayVisible => startDayButton != null && startDayButton.style.display == DisplayStyle.Flex;

  public void Initialize(VisualElement root, Action onStartDay) {
   strip = new VisualElement { name = "PhaseBanner", pickingMode = PickingMode.Position };
   strip.style.position = Position.Absolute;
   strip.style.left = 0;
   strip.style.right = 0;
   strip.style.top = 0;
   strip.style.height = Height;
   strip.style.flexDirection = FlexDirection.Row;
   strip.style.alignItems = Align.Center;
   strip.style.paddingLeft = 16;
   strip.style.paddingRight = 10;
   strip.style.backgroundColor = StripColor;
   strip.style.borderBottomWidth = 2;
   strip.style.borderBottomColor = StripBorder;

   phaseLabel = MakeLabel("", 18, PhaseColor, true);
   phaseLabel.style.minWidth = 230;
   phaseLabel.style.flexShrink = 0;

   var nowTag = MakeLabel("Now:", 15, NowTagColor, true);
   nowTag.style.marginRight = 8;
   nowTag.style.flexShrink = 0;

   nowLabel = MakeLabel("", 17, NowColor, false);
   nowLabel.style.flexGrow = 1;
   nowLabel.style.flexShrink = 1;

   startDayButton = new Button { text = "START DAY" };
   startDayButton.style.fontSize = 16;
   startDayButton.style.paddingTop = 3;
   startDayButton.style.paddingBottom = 3;
   startDayButton.style.paddingLeft = 16;
   startDayButton.style.paddingRight = 16;
   startDayButton.style.marginLeft = 12;
   startDayButton.style.backgroundColor = ButtonColor;
   startDayButton.style.color = ButtonText;
   startDayButton.style.unityFontStyleAndWeight = FontStyle.Bold;
   SetBorder(startDayButton, 1f, StripBorder);
   SetRadius(startDayButton, 6f);
   startDayButton.style.display = DisplayStyle.None;
   if (onStartDay != null) startDayButton.clicked += onStartDay;

   strip.Add(phaseLabel);
   strip.Add(nowTag);
   strip.Add(nowLabel);
   strip.Add(startDayButton);
   strip.style.display = DisplayStyle.None;
   root.Add(strip);
  }

  public void Dispose() { }

  /// <summary>Call once a frame (GameUIManager.LateUpdate). Only touches the UI when the text changes.</summary>
  public void Refresh() {
   if (strip == null) return;

   if (!RunManager.HasInstance) {
    strip.style.display = DisplayStyle.None;
    return;
   }

   var run = RunManager.Instance;
   int round = run.CurrentRoundNumber;
   int total = DuskRules.Active.roundsToWin;
   string phase;
   string now;
   bool showStart = false;

   switch (run.CurrentState) {
    case RunState.Planning:
     phase = string.Format(CultureInfo.InvariantCulture, "PLANNING  ·  ROUND {0} OF {1}", round, total);
     now = GameUIManager.IsPlanningPanelOpen
      ? "Edit your seeds. Press G for the garden: that is where you plant, and where the wave is on the map."
      : "Plant, water, edit seeds. The wave is on the map.";
     showStart = true;
     break;

    case RunState.GrowthAndThreat:
     phase = string.Format(CultureInfo.InvariantCulture, "DAY  ·  ROUND {0} OF {1}", round, total);
     now = DayHint();
     break;

    case RunState.Dusk:
     phase = string.Format(CultureInfo.InvariantCulture, "DUSK  ·  ROUND {0} OF {1}", round, total);
     now = run.AwaitingLedger ? "Read the ledger, then press Continue." : "Supper. Every fruit goes to Doris.";
     break;

    default:
     if (strip.style.display != DisplayStyle.None) strip.style.display = DisplayStyle.None;
     shownPhase = "";
     shownNow = "";
     return;
   }

   if (strip.style.display != DisplayStyle.Flex) strip.style.display = DisplayStyle.Flex;
   if (phase != shownPhase) {
    shownPhase = phase;
    phaseLabel.text = phase;
   }
   if (now != shownNow) {
    shownNow = now;
    nowLabel.text = now;
   }
   var wantStart = showStart ? DisplayStyle.Flex : DisplayStyle.None;
   if (startDayButton.style.display != wantStart) startDayButton.style.display = wantStart;
  }

  string DayHint() {
   if (driver == null) driver = UnityEngine.Object.FindAnyObjectByType<ExecutionPhaseDriver>();
   if (driver != null && driver.IsPaused) {
    return "Paused. Space resumes · Tab speed. Hands off, supper at dusk.";
   }
   return "Hands off. Space pause · Tab speed. Supper at dusk.";
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
   label.style.overflow = Overflow.Hidden;
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
