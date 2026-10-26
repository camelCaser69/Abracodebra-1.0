// FILE: Assets/Scripts/A_ToolkitUI/UIDevReadoutController.cs
// ============================================================================
//  DEV READOUT (temporary, Block 1 B1). F9 toggles it.
//  Shows tick, phase and a plant checksum so the Planning-tick invariant can be
//  checked by eye: the checksum must not move during Planning and must move in
//  the Day. Kept on through the Block 1 playtest (pack B9); remove after it.
//  Builds itself in code (no scene object): a bootstrap creates one instance per
//  play session and borrows the PanelSettings of the scene's existing UIDocument.
// ============================================================================
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;
using Abracodabra.Genes;

namespace Abracodabra.UI.Toolkit {
 public class UIDevReadoutController : MonoBehaviour {
  /// <summary>Living plants · total cells · total energy · fruit count, over every non-dead plant.</summary>
  public struct PlantChecksum {
   public int livingPlants;
   public int totalCells;
   public float totalEnergy;
   public int fruitCount;

   public override string ToString() {
    return string.Format(CultureInfo.InvariantCulture, "{0} · {1} · {2:F1} · {3}",
     livingPlants, totalCells, totalEnergy, fruitCount);
   }
  }

  const KeyCode ToggleKey = KeyCode.F9;
  const int DocumentSortingOrder = 900;
  const float PanelWidth = 250f;
  const float PanelHeight = 74f;
  const float CornerMargin = 8f;

  static readonly Color BackgroundColor = new Color(0.09f, 0.11f, 0.09f, 0.88f);
  static readonly Color BorderColor = new Color(0.31f, 0.36f, 0.29f, 1f);
  static readonly Color TitleColor = new Color(0.56f, 0.64f, 0.53f, 1f);
  static readonly Color TextColor = new Color(0.79f, 0.84f, 0.75f, 1f);

  public static UIDevReadoutController Instance { get; private set; }

  UIDocument document;
  VisualElement panel;
  Label phaseLabel;
  Label checksumLabel;
  ExecutionPhaseDriver phaseDriver;
  bool isVisible = false;

  public bool IsVisible => isVisible;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Bootstrap() {
   if (Instance != null) return;
   var go = new GameObject("DevReadout (temporary, F9)");
   DontDestroyOnLoad(go);
   go.AddComponent<UIDevReadoutController>();
  }

  /// <summary>Computes the plant checksum from PlantGrowth.AllActivePlants (dead plants excluded).</summary>
  public static PlantChecksum ComputePlantChecksum() {
   var sum = new PlantChecksum();
   foreach (var plant in PlantGrowth.AllActivePlants) {
    if (plant == null || plant.CurrentState == PlantState.Dead) continue;
    sum.livingPlants++;

    if (plant.CellManager != null) {
     sum.totalCells += plant.CellManager.cells.Count;
     foreach (var cell in plant.CellManager.cells) {
      if (cell.Value == PlantCellType.Fruit) sum.fruitCount++;
     }
    }

    if (plant.EnergySystem != null) sum.totalEnergy += plant.EnergySystem.CurrentEnergy;
   }
   return sum;
  }

  void Awake() {
   if (Instance != null && Instance != this) {
    Destroy(gameObject);
    return;
   }
   Instance = this;
  }

  void OnDestroy() {
   if (Instance == this) Instance = null;
  }

  void Update() {
   if (Input.GetKeyDown(ToggleKey)) SetVisible(!isVisible);

   if (!EnsureBuilt()) return;
   if (isVisible) Refresh();
  }

  public void SetVisible(bool visible) {
   isVisible = visible;
   if (EnsureBuilt()) {
    panel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    if (isVisible) Refresh();
   }
  }

  bool EnsureBuilt() {
   if (panel != null) return true;

   if (document == null) {
    var panelSettings = FindExistingPanelSettings();
    if (panelSettings == null) return false; // Scene UI not up yet; try again next frame.

    var docObject = new GameObject("DevReadoutDocument");
    docObject.SetActive(false);
    docObject.transform.SetParent(transform, false);
    document = docObject.AddComponent<UIDocument>();
    document.panelSettings = panelSettings;
    document.sortingOrder = DocumentSortingOrder;
    docObject.SetActive(true);
   }

   var root = document.rootVisualElement;
   if (root == null) return false;

   root.pickingMode = PickingMode.Ignore;
   BuildPanel(root);
   panel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
   return true;
  }

  PanelSettings FindExistingPanelSettings() {
   var allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
   foreach (var doc in allDocs) {
    if (doc != document && doc.panelSettings != null) return doc.panelSettings;
   }
   return null;
  }

  void BuildPanel(VisualElement root) {
   // Docked bottom-left, fixed size, never moves (Bitbloom rule 47).
   panel = new VisualElement { name = "DevReadoutPanel", pickingMode = PickingMode.Ignore };
   panel.style.position = Position.Absolute;
   panel.style.left = CornerMargin;
   panel.style.bottom = CornerMargin;
   panel.style.width = PanelWidth;
   panel.style.height = PanelHeight;
   panel.style.paddingLeft = 6;
   panel.style.paddingRight = 6;
   panel.style.paddingTop = 4;
   panel.style.paddingBottom = 4;
   panel.style.backgroundColor = BackgroundColor;
   panel.style.borderTopWidth = 1;
   panel.style.borderBottomWidth = 1;
   panel.style.borderLeftWidth = 1;
   panel.style.borderRightWidth = 1;
   panel.style.borderTopColor = BorderColor;
   panel.style.borderBottomColor = BorderColor;
   panel.style.borderLeftColor = BorderColor;
   panel.style.borderRightColor = BorderColor;
   panel.style.overflow = Overflow.Hidden;

   panel.Add(MakeLabel("DEV READOUT (temporary) · F9", 10, TitleColor));
   phaseLabel = MakeLabel("Tick –", 12, TextColor);
   panel.Add(phaseLabel);
   checksumLabel = MakeLabel("Checksum –", 12, TextColor);
   panel.Add(checksumLabel);
   panel.Add(MakeLabel("plants · cells · energy · fruit", 10, TitleColor));

   root.Add(panel);
  }

  static Label MakeLabel(string text, int fontSize, Color color) {
   var label = new Label(text) { pickingMode = PickingMode.Ignore };
   label.style.fontSize = fontSize;
   label.style.color = color;
   label.style.marginTop = 0;
   label.style.marginBottom = 0;
   label.style.paddingTop = 0;
   label.style.paddingBottom = 0;
   label.style.whiteSpace = WhiteSpace.NoWrap;
   return label;
  }

  void Refresh() {
   int tick = TickManager.Instance != null ? TickManager.Instance.CurrentTick : 0;
   phaseLabel.text = $"Tick {tick} · {DescribePhase()}";
   checksumLabel.text = $"Checksum  {ComputePlantChecksum()}";
  }

  string DescribePhase() {
   if (!RunManager.HasInstance) return "no run";

   switch (RunManager.Instance.CurrentState) {
    case RunState.Planning:
     return $"PLANNING · round {RunManager.Instance.CurrentRoundNumber}";
    case RunState.GrowthAndThreat:
     if (phaseDriver == null) phaseDriver = FindAnyObjectByType<ExecutionPhaseDriver>();
     string speed = phaseDriver != null
      ? (phaseDriver.IsPaused ? "paused" : string.Format(CultureInfo.InvariantCulture, "×{0:0.#}", phaseDriver.CurrentSpeedMultiplier))
      : "";
     return $"DAY · round {RunManager.Instance.CurrentRoundNumber} · {speed}";
    case RunState.GameOver:
     return "GAME OVER";
    default:
     return RunManager.Instance.CurrentState.ToString();
   }
  }
 }
}
