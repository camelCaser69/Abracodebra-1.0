// FILE: Assets/Scripts/A_ToolkitUI/UIDuskLedgerController.cs
// ============================================================================
//  DUSK LEDGER (Block 1 B6). After supper, before the dusk check is applied, a
//  card says what the Day did in three lines, total first and parts after:
//    DORIS   what she ate and where her belly stands (the pips she is wearing)
//    PESTS   leaves eaten per species, what your plants killed or scared off
//    PLANTS  which plants the Day took and to whom, and how many still grow
//  then Continue (RunManager.ContinueFromLedger). The numbers come from
//  SupperRunner.SupperResult and DayTally, the same sources the pips, the bite
//  text and the F9 readout use, so they agree with what was shown.
//  The verdict is not shown here: the end card (or Planning) comes after Continue.
//  Sits at the top right, under the phase banner: Doris and her pips (centre of the
//  garden) and the tick bar (top centre) stay visible while you read.
// ============================================================================
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;
using Abracodabra.Ecosystem;

namespace Abracodabra.UI.Toolkit {
 public class UIDuskLedgerController {
  static readonly Color CardColor = new Color(0.10f, 0.12f, 0.10f, 0.97f);
  static readonly Color CardBorder = new Color(0.56f, 0.64f, 0.53f, 1f);
  static readonly Color KickerColor = new Color(0.56f, 0.64f, 0.53f, 1f);
  static readonly Color TitleColor = new Color(0.88f, 0.92f, 0.82f, 1f);
  static readonly Color TagColor = new Color(0.62f, 0.72f, 0.58f, 1f);
  static readonly Color TextColor = new Color(0.93f, 0.95f, 0.88f, 1f);
  static readonly Color HintColor = new Color(0.70f, 0.77f, 0.66f, 1f);
  static readonly Color RuleColor = new Color(0.30f, 0.36f, 0.28f, 1f);
  static readonly Color ButtonColor = new Color(0.30f, 0.42f, 0.29f, 1f);
  static readonly Color ButtonText = new Color(0.94f, 0.96f, 0.90f, 1f);

  VisualElement strip;
  Label kickerLabel;
  Label dorisLabel;
  Label pestsLabel;
  Label plantsLabel;
  Button continueButton;

  public bool IsVisible => strip != null && strip.style.display == DisplayStyle.Flex;
  public string ShownKicker => kickerLabel != null ? kickerLabel.text : "";
  public string ShownDoris => dorisLabel != null ? dorisLabel.text : "";
  public string ShownPests => pestsLabel != null ? pestsLabel.text : "";
  public string ShownPlants => plantsLabel != null ? plantsLabel.text : "";

  public void Initialize(VisualElement root) {
   strip = new VisualElement { name = "DuskLedger", pickingMode = PickingMode.Ignore };
   strip.style.position = Position.Absolute;
   strip.style.left = 0;
   strip.style.right = 16;
   strip.style.top = UIPhaseBannerController.Height + 12;
   strip.style.alignItems = Align.FlexEnd;
   strip.style.display = DisplayStyle.None;

   var card = new VisualElement { name = "DuskLedgerCard", pickingMode = PickingMode.Position };
   card.style.width = 540;
   card.style.paddingLeft = 28;
   card.style.paddingRight = 28;
   card.style.paddingTop = 18;
   card.style.paddingBottom = 18;
   card.style.backgroundColor = CardColor;
   card.style.alignItems = Align.Stretch;
   SetBorder(card, 2f, CardBorder);
   SetRadius(card, 10f);

   kickerLabel = MakeLabel("", 14, KickerColor, false);
   var title = MakeLabel("DUSK LEDGER", 28, TitleColor, true);
   title.style.marginTop = 2;
   var subtitle = MakeLabel("What the Day did to Doris's supper, the pests and your plants.", 14, HintColor, false);
   subtitle.style.marginTop = 2;
   subtitle.style.whiteSpace = WhiteSpace.Normal;

   var rule = new VisualElement { pickingMode = PickingMode.Ignore };
   rule.style.height = 1;
   rule.style.marginTop = 10;
   rule.style.marginBottom = 4;
   rule.style.backgroundColor = RuleColor;

   dorisLabel = AddRow(card, "DORIS");
   pestsLabel = AddRow(card, "PESTS");
   plantsLabel = AddRow(card, "PLANTS");

   var footer = new VisualElement { pickingMode = PickingMode.Ignore };
   footer.style.flexDirection = FlexDirection.Column;
   footer.style.alignItems = Align.FlexStart;
   footer.style.marginTop = 16;

   continueButton = new Button { text = "Continue" };
   continueButton.style.fontSize = 20;
   continueButton.style.paddingTop = 8;
   continueButton.style.paddingBottom = 8;
   continueButton.style.paddingLeft = 26;
   continueButton.style.paddingRight = 26;
   continueButton.style.backgroundColor = ButtonColor;
   continueButton.style.color = ButtonText;
   continueButton.style.unityFontStyleAndWeight = FontStyle.Bold;
   SetBorder(continueButton, 1f, CardBorder);
   SetRadius(continueButton, 8f);
   continueButton.clicked += OnContinue;

   var hint = MakeLabel("Next comes the dusk check. To go on you need a living plant and a belly above 0.", 14, HintColor, false);
   hint.style.marginTop = 8;
   hint.style.flexShrink = 1;
   hint.style.whiteSpace = WhiteSpace.Normal;

   footer.Add(continueButton);
   footer.Add(hint);

   card.Insert(0, kickerLabel);
   card.Insert(1, title);
   card.Insert(2, subtitle);
   card.Insert(3, rule);
   card.Add(footer);
   strip.Add(card);
   root.Add(strip);
  }

  Label AddRow(VisualElement card, string tag) {
   var row = new VisualElement { pickingMode = PickingMode.Ignore };
   row.style.flexDirection = FlexDirection.Row;
   row.style.marginTop = 8;

   var tagLabel = MakeLabel(tag, 14, TagColor, true);
   tagLabel.style.width = 74;
   tagLabel.style.flexShrink = 0;
   tagLabel.style.paddingTop = 4;

   var text = MakeLabel("", 20, TextColor, false);
   text.style.flexGrow = 1;
   text.style.flexShrink = 1;
   text.style.whiteSpace = WhiteSpace.Normal;

   row.Add(tagLabel);
   row.Add(text);
   card.Add(row);
   return text;
  }

  /// <summary>Fill the three lines from the supper and the Day's tally, and show the card.</summary>
  public void Show(SupperRunner.SupperResult supper, DuskRules rules) {
   if (strip == null) return;

   string[] lines = BuildLines(supper, rules);
   kickerLabel.text = string.Format(CultureInfo.InvariantCulture, "DUSK {0} OF {1}", supper.round, rules.roundsToWin);
   dorisLabel.text = lines[0];
   pestsLabel.text = lines[1];
   plantsLabel.text = lines[2];

   continueButton.SetEnabled(true);
   strip.style.display = DisplayStyle.Flex;
   strip.BringToFront();
  }

  public void Hide() {
   if (strip != null) strip.style.display = DisplayStyle.None;
  }

  void OnContinue() {
   continueButton.SetEnabled(false);
   Hide();
   if (RunManager.HasInstance) RunManager.Instance.ContinueFromLedger();
  }

  /// <summary>The three ledger lines: Doris, pests, plants. Total first, then the parts.</summary>
  public static string[] BuildLines(SupperRunner.SupperResult supper, DuskRules rules) {
   var inv = CultureInfo.InvariantCulture;
   int max = rules.bellyPips;

   string doris;
   if (!supper.dorisFound) {
    doris = "Doris was not there for supper.";
   }
   else if (supper.fruitFed <= 0) {
    doris = string.Format(inv, "Doris got no fruit tonight (belly {0} / {1}).", supper.pipsAfter, max);
   }
   else {
    doris = string.Format(inv, "Doris ate {0} fruit from {1} {2}: belly {3} / {4} before supper, {5} / {4} now.",
     supper.fruitFed, supper.plantsWithFruit, supper.plantsWithFruit == 1 ? "plant" : "plants",
     supper.pipsBefore, max, supper.pipsAfter);
   }

   string pests;
   int bites = DayTally.Bites;
   if (bites <= 0) {
    pests = "The pests ate no leaves.";
   }
   else {
    pests = string.Format(inv, "The pests ate {0} {1}: {2}.", bites, bites == 1 ? "leaf" : "leaves", DayTally.BiteBreakdown());
   }
   string answers = "";
   int kills = DayTally.Kills;
   int repels = DayTally.Repels;
   if (kills > 0) {
    string sources = DayTally.KillSourceBreakdown();
    answers = string.Format(inv, "killed {0}{1}", kills, sources.Length > 0 ? " (" + sources + ")" : "");
   }
   if (repels > 0) {
    answers += (answers.Length > 0 ? " and " : "") + string.Format(inv, "scared off {0}", repels);
   }
   if (answers.Length > 0) pests += " Your plants " + answers + ".";

   string plants;
   int lost = DayTally.PlantsLost + DayTally.DorisAtePlants;
   string alive = supper.livingPlants > 0
    ? string.Format(inv, " {0} {1} still growing.", supper.livingPlants, supper.livingPlants == 1 ? "is" : "are")
    : " None are left.";
   if (lost <= 0) {
    plants = "You lost no plants." + alive;
   }
   else {
    plants = string.Format(inv, "You lost {0} {1}: {2}.", lost, lost == 1 ? "plant" : "plants", DayTally.LostPlantsBreakdown()) + alive;
   }

   return new[] { doris, pests, plants };
  }

  static Label MakeLabel(string text, int fontSize, Color color, bool bold) {
   var label = new Label(text) { pickingMode = PickingMode.Ignore };
   label.style.fontSize = fontSize;
   label.style.color = color;
   label.style.marginTop = 0;
   label.style.marginBottom = 0;
   label.style.paddingTop = 0;
   label.style.paddingBottom = 0;
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
