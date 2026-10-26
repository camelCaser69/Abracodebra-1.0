// FILE: Assets/Scripts/A_ToolkitUI/UIRunEndOverlayController.cs
// ============================================================================
//  RUN END OVERLAY (Block 1 B3). Code-built, full screen, over the HUD: what
//  happened (won or lost), why (DuskRules' end text), the numbers of the last
//  dusk, the seed, and two ways on: Restart (same seed) and New seed
//  (RunManager.RestartRun). GameUIManager shows it on RunState.GameOver.
// ============================================================================
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;

namespace Abracodabra.UI.Toolkit {
 public class UIRunEndOverlayController {
  static readonly Color BackdropColor = new Color(0.04f, 0.05f, 0.04f, 0.78f);
  static readonly Color CardColor = new Color(0.10f, 0.12f, 0.10f, 0.97f);
  static readonly Color CardBorder = new Color(0.36f, 0.42f, 0.33f, 1f);
  static readonly Color KickerColor = new Color(0.56f, 0.64f, 0.53f, 1f);
  static readonly Color TitleColor = new Color(0.88f, 0.92f, 0.82f, 1f);
  static readonly Color ReasonColor = new Color(0.93f, 0.95f, 0.88f, 1f);
  static readonly Color DetailColor = new Color(0.70f, 0.77f, 0.66f, 1f);
  static readonly Color ButtonColor = new Color(0.30f, 0.42f, 0.29f, 1f);
  static readonly Color ButtonAltColor = new Color(0.20f, 0.25f, 0.20f, 1f);
  static readonly Color ButtonText = new Color(0.94f, 0.96f, 0.90f, 1f);
  static readonly Color LossAccent = new Color(0.78f, 0.55f, 0.30f, 1f); // warm only where it means "lost"

  VisualElement backdrop;
  VisualElement card;
  Label kickerLabel;
  Label titleLabel;
  Label reasonLabel;
  Label detailLabel;
  Button restartButton;
  Button newSeedButton;

  public bool IsVisible => backdrop != null && backdrop.style.display == DisplayStyle.Flex;
  public string ShownTitle => titleLabel != null ? titleLabel.text : "";
  public string ShownReason => reasonLabel != null ? reasonLabel.text : "";
  public string ShownDetail => detailLabel != null ? detailLabel.text : "";

  public void Initialize(VisualElement root) {
   backdrop = new VisualElement { name = "RunEndOverlay" };
   backdrop.style.position = Position.Absolute;
   backdrop.style.left = 0;
   backdrop.style.top = 0;
   backdrop.style.right = 0;
   backdrop.style.bottom = 0;
   backdrop.style.backgroundColor = BackdropColor;
   backdrop.style.alignItems = Align.Center;
   backdrop.style.justifyContent = Justify.Center;
   backdrop.pickingMode = PickingMode.Position; // blocks the HUD underneath
   backdrop.style.display = DisplayStyle.None;

   card = new VisualElement { name = "RunEndCard" };
   card.style.width = 560;
   card.style.paddingLeft = 32;
   card.style.paddingRight = 32;
   card.style.paddingTop = 26;
   card.style.paddingBottom = 26;
   card.style.backgroundColor = CardColor;
   card.style.alignItems = Align.Center;
   SetBorder(card, 2f, CardBorder);
   SetRadius(card, 10f);

   kickerLabel = MakeLabel(15, KickerColor, false);
   titleLabel = MakeLabel(34, TitleColor, true);
   titleLabel.style.marginTop = 4;
   reasonLabel = MakeLabel(24, ReasonColor, false);
   reasonLabel.style.marginTop = 14;
   reasonLabel.style.whiteSpace = WhiteSpace.Normal;
   reasonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
   detailLabel = MakeLabel(15, DetailColor, false);
   detailLabel.style.marginTop = 14;
   detailLabel.style.whiteSpace = WhiteSpace.Normal;
   detailLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

   var buttonRow = new VisualElement();
   buttonRow.style.flexDirection = FlexDirection.Row;
   buttonRow.style.marginTop = 24;
   restartButton = MakeButton("Restart (same seed)", ButtonColor);
   newSeedButton = MakeButton("New seed", ButtonAltColor);
   newSeedButton.style.marginLeft = 14;
   restartButton.clicked += () => Restart(true);
   newSeedButton.clicked += () => Restart(false);
   buttonRow.Add(restartButton);
   buttonRow.Add(newSeedButton);

   card.Add(kickerLabel);
   card.Add(titleLabel);
   card.Add(reasonLabel);
   card.Add(detailLabel);
   card.Add(buttonRow);
   backdrop.Add(card);
   root.Add(backdrop);
  }

  public void Show(RunManager run) {
   if (backdrop == null || run == null) return;

   var rules = DuskRules.Active;
   bool won = run.Outcome == RunOutcome.Won;

   kickerLabel.text = string.Format(CultureInfo.InvariantCulture, "DUSK {0} OF {1}", run.CurrentRoundNumber, rules.roundsToWin);
   titleLabel.text = won ? "YOU MADE IT" : "THE RUN IS OVER";
   titleLabel.style.color = won ? TitleColor : LossAccent;
   reasonLabel.text = string.IsNullOrEmpty(run.EndReason) ? (won ? rules.winText : "The run ended.") : run.EndReason;

   string supperLine = "";
   if (run.Supper != null && run.Supper.LastResult.round == run.CurrentRoundNumber) {
    var supper = run.Supper.LastResult;
    supperLine = string.Format(CultureInfo.InvariantCulture,
     "Tonight Doris ate {0} fruit (belly {1} / {2}). Living plants: {3}.\n",
     supper.fruitFed, supper.pipsAfter, rules.bellyPips, supper.livingPlants);
   }
   detailLabel.text = supperLine + string.Format(CultureInfo.InvariantCulture, "Seed {0}", run.RunSeed);

   backdrop.style.display = DisplayStyle.Flex;
   backdrop.BringToFront();
  }

  public void Hide() {
   if (backdrop != null) backdrop.style.display = DisplayStyle.None;
  }

  void Restart(bool sameSeed) {
   if (!RunManager.HasInstance) return;
   restartButton.SetEnabled(false);
   newSeedButton.SetEnabled(false);
   RunManager.Instance.RestartRun(sameSeed);
  }

  static Label MakeLabel(int fontSize, Color color, bool bold) {
   var label = new Label { pickingMode = PickingMode.Ignore };
   label.style.fontSize = fontSize;
   label.style.color = color;
   label.style.marginTop = 0;
   label.style.marginBottom = 0;
   if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
   return label;
  }

  static Button MakeButton(string text, Color background) {
   var button = new Button { text = text };
   button.style.fontSize = 19;
   button.style.paddingTop = 10;
   button.style.paddingBottom = 10;
   button.style.paddingLeft = 22;
   button.style.paddingRight = 22;
   button.style.backgroundColor = background;
   button.style.color = ButtonText;
   button.style.unityFontStyleAndWeight = FontStyle.Bold;
   SetBorder(button, 1f, CardBorder);
   SetRadius(button, 8f);
   return button;
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
