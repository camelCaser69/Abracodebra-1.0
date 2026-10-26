// FILE: Assets/Scripts/A_ToolkitUI/UIWaveMarkersController.cs
// ============================================================================
//  WAVE MARKERS (Block 1 B4). During Planning, one marker per spawn group of the
//  COMING wave sits at the edge of the view where that group will walk in:
//  species icon, "x3", and the time it appears ("0:05" = seconds into the Day at
//  x1). Hover: who it is, where and when, what it eats, how fast it bites, and
//  what sends it away. Same point and same clock as the real spawn:
//  WaveEntryGeometry (position) and WaveManager.SecondsUntilEntryStarts (time).
//  Lives inside the HUD panel, so it shows in the garden view (G) and not behind
//  the seed editor; hidden in the Day.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;

namespace Abracodabra.UI.Toolkit {
 public class UIWaveMarkersController {
  const float MarkerInset = 1.2f;   // world units inside the view edge
  const float EdgeMarginPx = 6f;    // keep the marker inside the panel

  static readonly Color MarkerBackground = new Color(0.09f, 0.11f, 0.09f, 0.92f);
  static readonly Color MarkerBorder = new Color(0.80f, 0.88f, 0.72f, 0.95f);
  static readonly Color TitleColor = new Color(0.86f, 0.91f, 0.80f, 1f);
  static readonly Color TextColor = new Color(0.72f, 0.79f, 0.68f, 1f);
  static readonly Color HoverBorder = new Color(0.31f, 0.36f, 0.29f, 1f);

  class Marker {
   public WaveSpawnEntry entry;
   public VisualElement root;
   public VisualElement hover;
  }

  VisualElement parent;
  Camera worldCamera;
  readonly List<Marker> markers = new List<Marker>();

  int builtRound = -1;
  WaveDefinition builtWave;
  bool visible;

  /// <summary>How many markers are on the map right now (for checks).</summary>
  public int MarkerCount => visible ? markers.Count : 0;

  public void Initialize(VisualElement hudParent) {
   parent = hudParent;
  }

  public void Dispose() {
   ClearMarkers();
  }

  /// <summary>Text of marker i, for checks: "Bunny x2 · 0:05 · east".</summary>
  public string DescribeMarker(int index) {
   if (index < 0 || index >= markers.Count) return "";
   var m = markers[index];
   var label = m.root.Q<Label>("marker-count");
   var time = m.root.Q<Label>("marker-time");
   return $"{NameOf(m.entry)} {label?.text} · {time?.text} · {WaveEntryGeometry.EdgeName(m.entry.entryEdge)}";
  }

  /// <summary>Call once a frame (GameUIManager.LateUpdate).</summary>
  public void Refresh() {
   if (parent == null) return;

   bool planning = RunManager.HasInstance && RunManager.Instance.CurrentState == RunState.Planning;
   var waveManager = WaveManager.Instance;
   int round = planning ? RunManager.Instance.CurrentRoundNumber : -1;
   WaveDefinition wave = planning && waveManager != null ? waveManager.PeekWaveForRound(round) : null;

   visible = planning && wave != null;
   if (!visible) {
    if (markers.Count > 0) ClearMarkers();
    return;
   }

   if (round != builtRound || wave != builtWave) Rebuild(round, wave, waveManager);

   if (worldCamera == null) worldCamera = Camera.main;
   if (worldCamera == null || parent.panel == null) return;

   foreach (var m in markers) {
    if (m.entry.spawnLocationType != WaveSpawnLocationType.FixedEntry) {
     m.root.style.display = DisplayStyle.None; // an old-style entry has no fixed point to show
     continue;
    }
    m.root.style.display = DisplayStyle.Flex;

    Vector2 world = WaveEntryGeometry.MarkerPoint(worldCamera, m.entry, MarkerInset);
    Vector2 panelPoint = RuntimePanelUtils.CameraTransformWorldToPanel(parent.panel, world, worldCamera);
    Vector2 local = parent.WorldToLocal(panelPoint);

    float w = float.IsNaN(m.root.resolvedStyle.width) ? 0f : m.root.resolvedStyle.width;
    float h = float.IsNaN(m.root.resolvedStyle.height) ? 0f : m.root.resolvedStyle.height;
    float pw = parent.layout.width;
    float ph = parent.layout.height;
    float left = local.x - w * 0.5f;
    float top = local.y - h * 0.5f;
    if (pw > 0f) left = Mathf.Clamp(left, EdgeMarginPx, Mathf.Max(EdgeMarginPx, pw - w - EdgeMarginPx));
    if (ph > 0f) top = Mathf.Clamp(top, EdgeMarginPx, Mathf.Max(EdgeMarginPx, ph - h - EdgeMarginPx));
    m.root.style.left = left;
    m.root.style.top = top;
   }
  }

  void Rebuild(int round, WaveDefinition wave, WaveManager waveManager) {
   ClearMarkers();
   builtRound = round;
   builtWave = wave;

   foreach (var entry in wave.spawnEntries) {
    if (entry == null || entry.animalDefinition == null || entry.spawnCount <= 0) continue;
    markers.Add(BuildMarker(entry, waveManager));
   }
  }

  void ClearMarkers() {
   foreach (var m in markers) m.root.RemoveFromHierarchy();
   markers.Clear();
   builtRound = -1;
   builtWave = null;
  }

  Marker BuildMarker(WaveSpawnEntry entry, WaveManager waveManager) {
   var def = entry.animalDefinition;

   var root = new VisualElement { name = "WaveMarker" };
   root.style.position = Position.Absolute;
   root.style.flexDirection = FlexDirection.Row;
   root.style.alignItems = Align.Center;
   root.style.paddingLeft = 6;
   root.style.paddingRight = 9;
   root.style.paddingTop = 4;
   root.style.paddingBottom = 4;
   root.style.backgroundColor = MarkerBackground;
   SetBorder(root, 2f, MarkerBorder);
   SetRadius(root, 4);
   root.pickingMode = PickingMode.Position;

   var icon = new VisualElement { name = "marker-icon", pickingMode = PickingMode.Ignore };
   icon.style.width = 44;
   icon.style.height = 44;
   var sprite = def.prefab != null ? def.prefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite : null;
   if (sprite != null) {
    icon.style.backgroundImage = new StyleBackground(sprite);
    icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
   }
   root.Add(icon);

   var column = new VisualElement { pickingMode = PickingMode.Ignore };
   column.style.marginLeft = 7;
   var count = MakeLabel($"x{entry.spawnCount}", 26, TitleColor, true);
   count.name = "marker-count";
   var time = MakeLabel(FormatSeconds(waveManager.SecondsUntilEntryStarts(entry)), 20, TextColor, false);
   time.name = "marker-time";
   column.Add(count);
   column.Add(time);
   root.Add(column);

   // Hover box: opens on the side facing into the view.
   var hover = new VisualElement { name = "WaveMarkerHover", pickingMode = PickingMode.Ignore };
   hover.style.position = Position.Absolute;
   hover.style.paddingLeft = 8;
   hover.style.paddingRight = 8;
   hover.style.paddingTop = 5;
   hover.style.paddingBottom = 5;
   hover.style.backgroundColor = MarkerBackground;
   SetBorder(hover, 1f, HoverBorder);
   SetRadius(hover, 4);
   hover.style.display = DisplayStyle.None;
   PlaceHover(hover, entry.entryEdge);

   hover.Add(MakeLabel($"{NameOf(entry)} x{entry.spawnCount}", 20, TitleColor, true));
   foreach (var line in HoverLines(entry, waveManager)) hover.Add(MakeLabel(line, 16, TextColor, false));
   root.Add(hover);

   root.RegisterCallback<PointerEnterEvent>(_ => {
    hover.style.display = DisplayStyle.Flex;
    root.BringToFront();
   });
   root.RegisterCallback<PointerLeaveEvent>(_ => hover.style.display = DisplayStyle.None);

   parent.Add(root);
   return new Marker { entry = entry, root = root, hover = hover };
  }

  static void PlaceHover(VisualElement hover, WaveEntryEdge edge) {
   const float gap = 6f;
   switch (edge) {
    case WaveEntryEdge.West:
     hover.style.left = new StyleLength(new Length(100f, LengthUnit.Percent));
     hover.style.marginLeft = gap;
     hover.style.top = 0;
     break;
    case WaveEntryEdge.East:
     hover.style.right = new StyleLength(new Length(100f, LengthUnit.Percent));
     hover.style.marginRight = gap;
     hover.style.top = 0;
     break;
    case WaveEntryEdge.North:
     hover.style.top = new StyleLength(new Length(100f, LengthUnit.Percent));
     hover.style.marginTop = gap;
     hover.style.left = 0;
     break;
    default:
     hover.style.bottom = new StyleLength(new Length(100f, LengthUnit.Percent));
     hover.style.marginBottom = gap;
     hover.style.left = 0;
     break;
   }
  }

  // Rule 39: doing / eats / how fast / what sends it away. All numbers come from the definition.
  static List<string> HoverLines(WaveSpawnEntry entry, WaveManager waveManager) {
   var def = entry.animalDefinition;
   var lines = new List<string>();
   float tickSeconds = TickManager.Instance != null && TickManager.Instance.Config != null
    ? TickManager.Instance.Config.GetRealSecondsPerTick() : 0.5f;

   lines.Add($"comes in from the {WaveEntryGeometry.EdgeName(entry.entryEdge)}, " +
             $"{FormatSeconds(waveManager.SecondsUntilEntryStarts(entry))} into the Day");
   if (entry.spawnCount > 1 && entry.spawnInterval > 0f)
    lines.Add($"one every {entry.spawnInterval:0.#} s");

   if (def.isPest) {
    lines.Add("walks to the nearest plant and eats its leaves");
    lines.Add($"eats: a leaf about every {def.baseEatSpeedTicks * tickSeconds:0.#} s (tough leaves take longer)");
   }
   else {
    lines.Add("wanders and looks for food");
   }

   lines.Add(def.immuneToFear ? "a Fear gene does not work on it" : "a Fear gene sends it running");
   return lines;
  }

  static string NameOf(WaveSpawnEntry entry) {
   var def = entry.animalDefinition;
   return def != null && !string.IsNullOrEmpty(def.animalName) ? def.animalName : "Pest";
  }

  public static string FormatSeconds(float seconds) {
   int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
   return $"{total / 60}:{total % 60:00}";
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
