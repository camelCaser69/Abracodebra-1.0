# Pack Implementation Guides — F1–F4 (written by Fable 5, 2026-07-07)

**Purpose:** Step-by-step implementation guides for the four packs in `2026-07_Fable5_Last_Day_Plan.md`, written so a less advanced model — or a human — can execute each pack without architectural mistakes. All symbols, signatures, and call-site lists below were **verified against live disk on 2026-07-07**. The hard design decisions are made HERE; the executor's job is faithful mechanical application.

---

## G0. Rules for whoever executes these packs (read first, non-negotiable)

1. **Symbols are the contract; line numbers are hints.** Every line number below was valid 2026-07-07. Before editing a file, open it and re-locate the symbol. If a named symbol does not exist on disk, **STOP** — do not improvise. Re-grep, re-read this guide, or ask Milan.
2. **Read before writing.** For each pack there is a "Read fully before starting" list. Read those live files first, entirely. Do not rely on `06_Index` extracts for editing.
3. **Output rules (from CLAUDE.md, they bind you too):** complete methods signature-to-`}`, no `// ... rest of code` placeholders, full file rewrite if >3 methods change, never create `_v2`/`_new` filenames, re-state `using` directives, note line-count delta when rewriting a file.
4. **No Inspector work.** Every new MonoBehaviour self-installs from code (pattern: `GameBootstrap.Awake` → `AddComponent<T>()`, or construction inside `GameUIManager`). If you catch yourself writing "then in the Inspector, drag…" — redesign.
5. **Serialization safety:** never rename or delete a serialized field on a ScriptableObject class (`SeedTemplate`, `GeneBase` subclasses, definitions) — 51 `.asset` files depend on them. Additive fields are fine. `[FormerlySerializedAs]` if a rename is truly unavoidable (it never is in these packs).
6. **Invariants:** gameplay RNG via `IDeterministicRandom` (hybrid fallback pattern, see G4-§1); tick-state changes on tick boundaries, wall-clock for visuals only; `GridPosition` Z stays 0; UI controllers named `UI[Name]Controller`; services static.
7. **Compile triage protocol:** you cannot compile. At the end of the pack, output a short list titled "Compile-check focus" naming the files most likely to error. Milan opens Unity, pastes Console errors back, you fix. Expect 2–10 trivial errors on a big pack; that is normal, not failure.
8. **Verification:** after writes, verify host-side with Read/Grep (the bash mount can serve stale content). Each pack ends with verification greps — run them and report results.
9. **KB duties per pack:** update `01_Core/projectmemory.md` Current state, patch the affected `01_Core/Abracodebra_Codebase_Map.md` section (F1→§5, F2→§8, F3→§8/§3, F4→§12), flag `06_Index` stale (or ask Milan to re-run `unity_extractor_RUN.bat`), and ask Milan to git-commit before starting the next pack.
10. **Execution order if running multiple packs: F1 → F2 → F3 → F4**, one pack per session.

---

## G1. Pack F1 — DNA Strand buffer migration (slot → Noita bus)

**Design source:** `02_Design/gene_systems_deep_dive_v6.md` §3 (the three rules, wrap trick, trigger semantics, cycle time) and §1 "Implementation Path". Re-read both before starting.

**Read fully before starting:**
`Assets/Scripts/Genes/Runtime/PlantGeneRuntimeState.cs` · `Runtime/RuntimeSequenceSlot.cs` · `Runtime/RuntimeGeneInstance.cs` · `Genes/PlantSequenceExecutor.cs` · `Genes/Templates/SeedTemplate.cs` · `Genes/Core/ActiveGene.cs` + `GeneBase.cs` (GeneCategory) · `Assets/Scripts/A_ToolkitUI/UISeedEditorController.cs` · `UIDragDropController.cs` · `UISpecSheetController.cs` · `GameUIManager.cs` (the handler methods wired at its lines ~395–407).

### G1.0 Verified current-state facts you will build on

- `PlantGeneRuntimeState` (plain C# class, **not** Unity-serialized — it lives only in memory, held by `UIInventoryItem.SeedRuntimeState`): fields `template`, `passiveInstances : List<RuntimeGeneInstance>`, `activeSequence : List<RuntimeSequenceSlot>`, `[NonSerialized] currentPosition / rechargeTicksRemaining / isExecuting`; methods `InitializeFromTemplate()`, `Reset()`, `CalculateTotalEnergyCost()`. **There is no save system — runtime flattening carries near-zero serialization risk.** The only serialized authoring data is `SeedTemplate`.
- `RuntimeSequenceSlot`: `activeInstance`, `modifierInstances`, `payloadInstances` (all `RuntimeGeneInstance`), `HasContent => activeInstance != null`, `GetEnergyCost()` → `ActiveGene.GetFinalEnergyCost(modifierInstances)`, `[NonSerialized] delayTicksRemaining / isExecuting / isHighlighted`.
- `SeedTemplate` (SO, 51 assets project-wide): `passiveSlotCount`, `activeSequenceLength` (both `[Range(1,8)]`), `passiveGenes : List<GeneTemplateEntry>`, `activeSequence : List<SequenceSlotTemplate>` where `SequenceSlotTemplate { ActiveGene activeGene; List<GeneTemplateEntry> modifiers; payloads; Validate() }`; `OnValidate()` pads/truncates both lists; `CreateRuntimeState()`.
- `PlantSequenceExecutor.OnTickUpdate(int)`: gates on `runtimeState/template/plantGrowth` null, `PlantState.Mature`, `EnergySystem`; recharge countdown; `TryExecuteCurrentSlot()` semantics — empty slot → `return true` (skip+advance); `delayTicksRemaining > 0` → decrement, `return false` (hold); `isTriggerType` → `return true` (free pass); target check (`TargetFinder.HasCreatureInRange`) → skip; Trigger-type modifiers gate via `CheckTriggerCondition`; **insufficient energy → publish `GeneValidationFailedEvent`, `return false` (STALL, cursor holds)**; on success: `SpendEnergy`, `SetValue("effect_multiplier", 1f)`, modifiers `PreExecution`, delay arming OR `PerformExecutionLogic` (Execute → `PostExecution` → `GeneExecutedEvent`), `return true`. Sequence end → `SequenceCompletedEvent`, `currentPosition = 0`, `rechargeTicksRemaining = template.baseRechargeTime`. Trigger genes are wired once in `InitializeTriggerGenes()` (ReactiveBurstGene → get/add `ReactiveBurstHandler`, `Initialize(plantGrowth, burstGene, slot.payloadInstances, slot.modifierInstances, slot.activeInstance)`).
- `GeneCategory` enum (in `GeneBase.cs`): `Passive, Active, Modifier, Payload, Seed`. `GeneBase.tier` exists (int, default 1).
- `UISeedEditorController` (plain class): slot addressing protocol is `(int slotIndex, string slotType)` with slotType ∈ `"passive" | "active" | "modifier" | "payload" | "seed"`. Key members: `DisplaySeed(UIInventoryItem)` (rebuilds passive row + a header row + one `.sequence-row` per template slot with 3 columns), `AddGeneToSlot(GeneBase, int, string)` (modifier/payload = `Clear()` then `Add` — single occupancy), `RemoveGeneFromSlot(int, string)` (right-click), `GetGeneAtSlot`, `HighlightCompatibleSlots(GeneCategory?)`, `SetInteractable(bool)` (Planning lock — preserve), wrapper `userData = SlotMetadata { index, type }` (line ~645). Events: `OnGeneSlotPointerDown(GeneBase, VisualElement, int, string)`, `OnGeneRemovedFromEditor(GeneBase, int, string)`, hover events, name/color events.
- **`UIDragDropController` derives drop slotType from COLUMN POSITION** — line ~185 `(seedSlot, "seed", 0)`, ~193 `(wrapper, "passive", passiveIndex)`, ~208–210 `0 => "active", 1 => "modifier", 2 => "payload"`. This positional logic is what the strip replaces.
- `GameUIManager` wires editor/dragdrop events at lines ~395–407: `OnInventorySwapRequested, OnGeneDropRequested, OnDragStarted/Ended, OnGeneDroppedToInventory, OnGeneEditorInternalMove, OnGeneSlotPointerDown, OnGeneRemovedFromEditor` → handlers named `Handle*`.

### G1.1 Locked design decisions (do not relitigate)

1. **SeedTemplate stays slot-shaped.** Do NOT restructure `activeSequence` or its types. Flattening happens at runtime in `PlantGeneRuntimeState.InitializeFromTemplate()`. A grouped template is just a pre-grouped strand — semantics are provably identical when flattened as `[...modifiers, active, ...payloads]` per slot, in slot order.
2. **Additive capacity field on SeedTemplate** (safe for all 51 assets):

```csharp
[Header("Strand (Buffer Model)")]
[Tooltip("Gene capacity of the flat strand. 0 = derive from legacy slots (activeSequenceLength * 3).")]
public int strandLength = 0;

public int StrandCapacity => strandLength > 0 ? strandLength : activeSequenceLength * 3;
```

3. **`RuntimeSequenceSlot` is repurposed as the PARSE PRODUCT, not deleted.** The parser outputs groups shaped exactly like today's slots, so ~90% of `TryExecuteCurrentSlot` transfers verbatim. `RuntimeSequenceSlot.InitializeFromTemplate(...)` becomes unused → delete that method only.
4. **Tick semantics (pinned from bible §3, incl. the `[Trap][Poison][Fruit][Nutritious]` = 2-tick example):** every gene position costs 1 tick as the cursor passes it — modifiers and payloads included (that IS the cycle-time cost of long strands) — EXCEPT trigger groups: a trigger Active and its attached payload/modifier positions cost 0 ticks (cursor passes all of them within the same tick). Energy stall keeps parity with today: cursor HOLDS at a normal group's Active position until energy suffices.
5. **Wrap trick:** trailing modifiers (after the last Active) are `wrapModifiers`. Cycle 1: groups[0] fires without them. Cycle 2+: they are prepended to groups[0]'s modifier list. Implement by keeping two modifier lists on the compiled first group (`baseModifiers`, plus `wrapModifiers` applied when `cycleCount >= 1`).
6. **Buffer rules (bible §3):** Rule 1 — Modifiers accumulate left-to-right into a buffer. Rule 2 — an Active consumes the entire buffer (buffer empties). Rule 3 — Payloads after an Active attach to that Active **until the next Modifier or Active is reached**. A Payload with no prior Active in the current cycle = wasted (diagnostic). A Passive gene inside a strand should be impossible via UI, but the parser must tolerate it: flag as `invalidGene` diagnostic, skip.

### G1.2 Stage 1 — engine-agnostic parser core (sandbox-testable)

**New file:** `Assets/Scripts/Genes/Runtime/SequenceParser.cs`, namespace `Abracodabra.Genes.Runtime`. Two layers in one file:

**Layer A — pure core, zero `UnityEngine` usings.** Operates on a minimal projection so it compiles under plain dotnet:

```csharp
public struct ParsedGeneInfo {
    public GeneCategory category;   // enum from Abracodabra.Genes.Core (engine-free)
    public bool isTriggerActive;    // ActiveGene.isTriggerType
    public bool isValid;            // gene resolved (not null/placeholder-missing)
}

public class ParsedGroup {
    public int activeIndex = -1;              // strand index of the Active
    public List<int> modifierIndices = new List<int>();
    public List<int> payloadIndices = new List<int>();
    public bool isTrigger;
    public List<int> MemberIndices { get; }   // active + modifiers + payloads, computed
}

public class StrandParseResult {
    public List<ParsedGroup> groups = new List<ParsedGroup>();
    public List<int> wrapModifierIndices = new List<int>();  // trailing mods → groups[0] from cycle 2+
    public List<int> wastedPayloadIndices = new List<int>(); // payload with no owning Active this cycle
    public List<int> invalidIndices = new List<int>();       // null/passive/unresolvable genes
    public int cycleTickLength;                               // count of indices that cost a tick
}

public static class SequenceParserCore {
    public static StrandParseResult Parse(IReadOnlyList<ParsedGeneInfo> strand) { /* rules G1.1-§4/5/6 */ }
}
```

**Layer B — Unity adapter** (same file, below the core): `StrandCompiler.Compile(List<RuntimeGeneInstance> strand)` → builds `ParsedGeneInfo[]` via `GetGene()` / `Category` / `isTriggerType`, calls the core, then materializes `CompiledStrand`:

```csharp
public class CompiledStrand {
    public StrandParseResult parse;                    // index-level truth (UI + spec sheet read this)
    public List<RuntimeSequenceSlot> groups;           // parse groups materialized as slots (executor reads this)
    public List<RuntimeGeneInstance> wrapModifiers;    // applied to groups[0] from cycle 2 onward
    public Dictionary<int, int> indexToGroup;          // strand index -> group index (or -1)
}
```

Each `RuntimeSequenceSlot` in `groups` gets `activeInstance` + `modifierInstances` + `payloadInstances` filled from the strand list by index. Do NOT copy instances — reference the same `RuntimeGeneInstance` objects.

**Sandbox test harness (do this BEFORE integrating):** create `outputs/strand_parser_test/` with: (a) a copy of Layer A only, (b) a one-line stub `public enum GeneCategory { Passive, Active, Modifier, Payload, Seed }` in its own namespace-matching file, (c) `Program.cs` with asserts. Run `dotnet run` (check `dotnet --version` first; if unavailable, port the same asserts into `Assets/Scripts/Tests/Editor/SequenceParserTests.cs` as EditMode tests for Milan instead). **Canonical test vectors — all from the design bible, encode them exactly:**

| Strand | Expected |
|---|---|
| `[Cloud][Poison][Efficiency][Fruit][Nutritious]` (Build A) | G0: Cloud, mods[], pay[Poison] · G1: Fruit, mods[Efficiency], pay[Nutritious] · tickLen 5 |
| `[Efficiency][Cloud][Poison][Fruit][Nutritious]` (Build B) | G0: Cloud, mods[Efficiency], pay[Poison] · G1: Fruit, mods[], pay[Nutritious] · tickLen 5 |
| `[Efficiency][Fruit][Nutritious][Poison][Cloud]` (Build C) | G0: Fruit, mods[Efficiency], pay[Nutritious, Poison] · G1: Cloud, mods[], pay[] · tickLen 5 |
| `[Cloud][Poison][Efficiency]` (wrap) | G0: Cloud, pay[Poison]; wrapModifiers=[Efficiency] · tickLen 3 |
| `[Trap*][Poison][Fruit][Nutritious]` (*trigger) | G0: Trap trigger, pay[Poison], costs 0 ticks · G1: Fruit, pay[Nutritious] · tickLen 2 |
| `[Poison][Cloud]` | wastedPayloads=[0]; G0: Cloud, pay[] · tickLen 2 |
| `[Mod][Mod2][Active]` | G0: Active, mods[Mod,Mod2] — buffer accumulates multiple |

Rule-3 subtlety encoded in Build A: after Cloud takes Poison, the next gene Efficiency is a Modifier → payload attachment for Cloud CLOSES, Efficiency starts the buffer for Fruit.

### G1.3 Stage 2 — runtime + executor swap

**`PlantGeneRuntimeState`** — replace `activeSequence` with the flat model:

```csharp
public List<RuntimeGeneInstance> strand = new List<RuntimeGeneInstance>(); // ordered; null = empty UI slot
[NonSerialized] public CompiledStrand compiled;
[NonSerialized] public int cycleCount = 0;   // for wrap-modifier activation

public void RecompileStrand() { compiled = StrandCompiler.Compile(strand); }
```

- `InitializeFromTemplate()`: passives unchanged; then flatten — for each `SequenceSlotTemplate` in `template.activeSequence`: add modifier instances (with `SetValue("power_multiplier", entry.powerMultiplier)` exactly as `RuntimeSequenceSlot.InitializeFromTemplate` did — copy that logic before deleting it), then the active instance, then payload instances. Finally `RecompileStrand()`.
- **Strand list may contain nulls** (empty editor slots). `StrandCompiler` must skip nulls (not diagnostics — they're just gaps) and `cycleTickLength` must NOT count them; the executor cursor skips null positions free of tick cost (matches today's empty-slot skip).
- `CalculateTotalEnergyCost()` → sum over `compiled.groups` `GetEnergyCost()`.
- `Reset()` → also `cycleCount = 0`.
- `currentPosition` keeps its name but now means **strand gene index**.

**`PlantSequenceExecutor`** — this file will change in >3 methods → rewrite the whole file. Preserve verbatim: service acquisition in `Awake`, `Start` fallback, both `InitializeWithTemplate` overloads (they now also call `RecompileStrand()` if `compiled == null`), energy-stall semantics, delay semantics, `GeneValidationFailedEvent` / `GeneExecutedEvent` / `SequenceCompletedEvent` payload shapes (F3's tracker subscribes to these — do not rename fields). Changes:

- `InitializeTriggerGenes()`: iterate `runtimeState.compiled.groups` where `isTrigger`; identical `ReactiveBurstHandler` wiring (pass `group.payloadInstances`, `group.modifierInstances`, `group.activeInstance`).
- `OnTickUpdate`: after recharge gate, resolve `currentPosition`:
  1. `>= strand.Count` → `OnSequenceComplete()` (also `cycleCount++`).
  2. Null strand entry → `currentPosition++`, loop again within the same tick (free skip).
  3. Position in a **trigger group** (`indexToGroup` + `groups[g].isTrigger`) → advance past ALL that group's member indices this tick, loop again (free pass).
  4. Position is a modifier/payload index of a normal group → `currentPosition++`, **consume the tick** (return).
  5. Position is a normal group's `activeIndex` → run the ported `TryExecuteGroup(group)` (today's `TryExecuteCurrentSlot` body operating on the group's slot; wrap: if group is groups[0] and `cycleCount >= 1`, energy cost + Pre/PostExecution use `baseModifiers + wrapModifiers` — build the combined list once per fire, don't mutate the slot). `true` → `currentPosition++` consume tick; `false` (stall/delay) → hold position, consume tick.
- `OnSequenceComplete()`: unchanged event; `currentPosition = 0; rechargeTicksRemaining = template.baseRechargeTime;`.
- `GetEnergyCost` for wrap-affected fires: use `ActiveGene.GetFinalEnergyCost(combinedModifierList)` directly (public, verified).

**Touchpoint sweep (grep each, fix mechanically):** grep `activeSequence` and `RuntimeSequenceSlot` across `Assets/Scripts`. Verified consumers as of today: `PlantSequenceExecutor`, `UISeedEditorController` (DisplaySeed/Add/Remove/GetGeneAtSlot), `SeedTemplate` (its own authoring list — DO NOT touch), `PlantGeneRuntimeState`, plus read-only uses in `UISpecSheetController`/`SeedTooltipData`/`SeedQualityCalculator` (grep `SeedRuntimeState` and `.activeSequence` in `A_ToolkitUI/` — adapt reads to `strand`/`compiled`). `NodeExecutor.SpawnPlantFromState` passes the state object whole — no change expected; verify by grep.

**Stage 2 compile checkpoint for Milan here.** Plants should behave identically to pre-migration for existing seeds (grouped templates flatten to equivalent strands) — that's the regression test: plant a starting seed, watch it fire as before, only cycle timing differs per pinned semantics (a slot-model 3-slot seed with mods+payloads now cycles slower — expected, flag to Milan for recharge rebalance via `baseRechargeTime`).

### G1.4 Stage 3 — seed editor UI (strip + parse visualization)

Slot-type protocol change: `"active" | "modifier" | "payload"` collapse into **`"strand"`** (`"passive"`, `"seed"` unchanged). Sweep ALL string literals — verified locations: `UISeedEditorController` (Add/Remove/GetGeneAtSlot switches, `HighlightCompatibleSlots`), `UIDragDropController` (~185–210 positional resolution), `GameUIManager` `Handle*` methods (grep `"active"`, `"modifier"`, `"payload"` within `A_ToolkitUI/` — expect only these three files).

- **`UISeedEditorController.DisplaySeed`:** passive row unchanged. Replace header row + `.sequence-row` loop with ONE horizontal strip container (`.strand-strip`, `flexDirection: Row`, wrap allowed) of `template.StrandCapacity` slots via the existing `CreateGeneSlotWithLabel(gene, label, i, "strand")` (wrapper `userData` already carries `SlotMetadata` — drag-drop will read it). After building, run `runtimeState.RecompileStrand()` and paint:
  - group tint: `gene-slot--group{g % 6}` class on each member slot's `background` (define 6 border-color classes in `Styles/GameUI_Planning.uss`);
  - wasted/invalid indices: add class `gene-slot--wasted` (existing `gene-slot--incompatible` styling is the visual precedent to copy);
  - wrap modifiers: class `gene-slot--wrap` (distinct border, e.g. dashed).
- **`AddGeneToSlot(gene, index, "strand")`:** accept categories Active/Modifier/Payload only; pad `strand` with nulls to `index`; **replace** occupied slot v1 (returning the displaced gene to inventory is `GameUIManager.HandleGeneDrop`'s existing job — mirror current replace behavior exactly); then `RecompileStrand()` + `DisplaySeed(currentSeedItem)`. Same pattern for `RemoveGeneFromSlot` (right-click already wired) and `GetGeneAtSlot`.
- **`HighlightCompatibleSlots`:** `"strand"` slots compatible ⇔ `draggedCategory ∈ {Active, Modifier, Payload}`; passive row logic unchanged.
- **`UIDragDropController`:** replace the positional `(0/1/2 => type)` resolution (~lines 185–210) with reading `SlotMetadata` from `wrapper.userData` — strand wrappers report `("strand", index)`. Internal editor moves (`OnGeneEditorInternalMove`) become strand reorder: remove at source index, place at target index (replace semantics v1).
- **`UISpecSheetController` / `SeedTooltipData` / `SeedQualityCalculator`:** read these on-session. Replace slot iteration with `compiled.groups`; add a per-cycle breakdown sourced ONLY from `CompiledStrand` (never re-derive grouping in UI): one line per group — active name, final energy cost (`slot.GetEnergyCost()`), modifier names, payload names, `(trigger)` tag; plus footer `Cycle: {cycleTickLength} ticks + {template.baseRechargeTime} recharge` and warnings for `wastedPayloadIndices`/wrap.
- **UXML/USS:** `GeneSlot.uxml` reused as-is; new classes go in `GameUI_Planning.uss`. No new UXML file needed.

**Done when (whole pack):** sandbox tests green on all 7 vectors · existing SeedTemplate assets load and plants fire equivalently (Milan play-check) · rearranging `[Efficiency][Cloud][Poison]` → `[Cloud][Poison][Efficiency]` visibly regroups tints and changes spec-sheet costs per Build A/B expectations · `grep -r "RuntimeSequenceSlot" Assets/Scripts` hits only: its own file, SequenceParser.cs, PlantSequenceExecutor.cs · `grep "\"active\"\|\"modifier\"\|\"payload\"" Assets/Scripts/A_ToolkitUI` returns nothing.

**Do NOT:** touch `SeedTemplate.activeSequence`/`SequenceSlotTemplate` serialized shape · rename `RuntimeSequenceSlot` or any SO class · change GeneEventBus event field names · remove the `SetInteractable` Planning lock · alter passive-row behavior · convert `PlantGeneRuntimeState` to a MonoBehaviour or SO.

---

## G2. Pack F2 — Inventory inversion (review B1: model out of the UI)

**Read fully before starting:** `A_ToolkitUI/InventoryService.cs` · `A_ToolkitUI/UIInventoryItem.cs` · `A_ToolkitUI/GameUIManager.cs` (inventory regions: fields near top, `SetupPlayerInventory` ~410–431, `RefreshHotbar` ~433–439, `HandleInventoryServiceChanged` ~441+, the `Register` call site) · `A_ToolkitUI/UIInventoryGridController.cs` · `UIHotbarController.cs` · `HotbarSelectionService.cs` · `Genes/Config/GeneRewardSystem.cs` · `Genes/Config/StartingLoadoutApplier.cs` + `StartingLoadoutConfig.cs` · `Ecosystem/Feeding/FeedingSystem.cs` (inventory regions ~277–290, ~360–400, ~475–490) · `WorldInteraction/Placement/PlayerTileInteractor.cs` (~160–250) · `WorldInteraction/Player/PlayerActionManager.cs` (~285–295).

### G2.0 Verified current-state facts

- `InventoryService` (static, `Abracodabra.UI.Genes`, file lives in `A_ToolkitUI/`): holds `List<UIInventoryItem> _inventory` **by reference from GameUIManager** (`Register(list, columns, rows)` fires `OnInventoryReady`). API: `GetItemAt(int)`, `RemoveItemAtIndex(int)`, `AddItem(item)→int`, `SetItemAt(int,item)`, `GetFirstEmptySlot()`, `HasEmptySlot()`, `GetHotbarItems()` (first `Columns` entries, nulls preserved), `IsHotbarIndex(int)`, `AddHarvestedItem(ItemInstance)` (wraps in `new UIInventoryItem(itemInstance)`), `TotalSlots`, `Columns`, `IsInitialized`, events `OnInventoryChanged` / `OnSlotChanged(int)` / `OnInventoryReady`, `Unregister()`.
- `UIInventoryItem` (plain class, `Abracodabra.UI.Toolkit`): `ItemType {Gene, Seed, Tool, Resource}` derived from `OriginalData` (`SeedTemplate`/`ToolDefinition`/`GeneBase`/`ItemDefinition`); **player-authored data lives here**: `SeedRuntimeState : PlantGeneRuntimeState` (settable), `CustomName`, `BackgroundColor`, plus `ResourceInstance : ItemInstance`, `GeneInstance : RuntimeGeneInstance`, `StackSize`. 6 constructors + `From*` factories. View-ish members that must stay UI-side: `GetDisplayCount()` (queries `ToolSwitcher.Instance`), `Icon`, `ShouldShowCounter`, `GetTypeDisplayString`.
- `GameUIManager.SetupPlayerInventory()` builds `playerInventory` from its `[SerializeField] startingInventory` (`StartingInventory` asset: `startingTools/startingSeeds/startingGenes`), pads nulls to `TotalInventorySlots`, truncates overflow. It then calls `InventoryService.Register(playerInventory, inventoryColumns, inventoryRows)`.
- **Complete non-UI caller list (all verified, whole file paths under `Assets/Scripts/`):**
  - `WorldInteraction/Placement/PlayerTileInteractor.cs:164, 248` — `RemoveItemAtIndex(selectedIndex)` (consume planted seed / eaten consumable).
  - `WorldInteraction/Player/PlayerActionManager.cs:288–290` — `IsInitialized` + `AddHarvestedItem(harvestedItem.Item)`.
  - `Genes/Config/GeneRewardSystem.cs:56, 78, 88–89` — `IsInitialized`, `HasEmptySlot()`, **constructs `new UIInventoryItem(randomGene)`**, `AddItem(item)`.
  - `Genes/Config/StartingLoadoutApplier.cs:17–77` — `IsInitialized`, `OnInventoryReady` subscribe/unsubscribe (the A5 pattern), `AddItem(item)` ×2.
  - `Ecosystem/Feeding/FeedingSystem.cs:277–279, 367, 386–394, 478–486` — `IsInitialized`, `GetHotbarItems()`, `RemoveItemAtIndex(inventoryIndex)`, `TotalSlots` + `GetItemAt(i)` scans.
- `HotbarSelectionService.SelectedItem` is `UIInventoryItem`-typed; consumed by `PlayerTileInteractor` and `GameUIManager`.

### G2.1 Locked design decisions

1. **New folder + namespace:** `Assets/Scripts/Inventory/`, namespace `Abracodabra.Inventory`. Three files: `InventoryEntry.cs`, `PlayerInventory.cs`, `PlayerInventorySystem.cs`.
2. **The model:**

```csharp
namespace Abracodabra.Inventory {
    public enum InventoryEntryKind { Seed, Tool, Gene, Resource }

    public class InventoryEntry {
        public InventoryEntryKind kind;
        public SeedTemplate seedTemplate;             // kind == Seed
        public PlantGeneRuntimeState seedState;       // kind == Seed (player's edited strand)
        public ToolDefinition tool;                   // kind == Tool
        public RuntimeGeneInstance gene;              // kind == Gene
        public ItemInstance resource;                 // kind == Resource
        public int stackSize = 1;
        public string customName = "";
        public Color backgroundColor = new Color(0, 0, 0, 0);
        // static factories: FromSeed(SeedTemplate) [creates seedState via CreateRuntimeState()],
        // FromTool, FromGene(GeneBase), FromGene(RuntimeGeneInstance), FromResource(ItemInstance)
        // + GetDisplayName() mirroring UIInventoryItem.GetDisplayName() (pure data, no ToolSwitcher)
    }

    public class PlayerInventory {
        // owns List<InventoryEntry> slots (fixed size, null = empty), int Columns, int Rows
        // methods mirroring today's service semantics 1:1: GetEntryAt, RemoveEntryAt, AddEntry→int,
        // SetEntryAt, FirstEmptySlot, HasEmptySlot, GetHotbarEntries, IsHotbarIndex, TotalSlots
        // events: OnChanged, OnSlotChanged(int)  (instance events; service re-broadcasts)
    }

    public class PlayerInventorySystem : MonoBehaviour {
        public static PlayerInventorySystem Instance { get; private set; }
        public PlayerInventory Inventory { get; private set; }
        public void InitializeModel(StartingInventory config, int columns, int rows) { … }  // idempotent guard
    }
}
```

3. **Self-install, zero scene edits:** in `GameBootstrap.Awake` (execution order −100, already initializes `GeneServices` — read it first), add `gameObject.AddComponent<PlayerInventorySystem>()` guarded by a null-check on `Instance`. No Inspector reference needed at this stage.
4. **Config donor stays GameUIManager (pragmatic v1):** `GameUIManager` keeps its serialized `startingInventory` field, but `SetupPlayerInventory()` is REPLACED by a call to `PlayerInventorySystem.Instance.InitializeModel(startingInventory, inventoryColumns, inventoryRows)`; the model performs the build/pad/truncate logic (port lines 410–431 verbatim, constructing `InventoryEntry` via factories instead of `UIInventoryItem`). Model construction is thereby UI-triggered in timing but UI-free in type — headless init becomes possible later by calling `InitializeModel` from anywhere. `OnInventoryReady` timing is preserved exactly (fires when the model registers — see next point).
5. **`InventoryService` keeps its name, namespace, and event surface** (so `StartingLoadoutApplier`'s A5 binding pattern survives untouched), but internally fronts `PlayerInventory` and its item type becomes `InventoryEntry`:
   - `Register(...)` is replaced by `RegisterModel(PlayerInventory model)` (called from `PlayerInventorySystem.InitializeModel`); it re-broadcasts the model's events and fires `OnInventoryReady`.
   - Method renames — mechanical mapping (old → new): `GetItemAt → GetEntryAt` · `RemoveItemAtIndex → RemoveEntryAt` (keep an `[Obsolete] RemoveItemAtIndex` shim delegating, to soften the sweep) · `AddItem(UIInventoryItem)` → **delete**, replaced by typed conveniences `AddGene(GeneBase)`, `AddGene(RuntimeGeneInstance)`, `AddSeed(SeedTemplate)`, `AddTool(ToolDefinition)`, `AddResource(ItemInstance)` and generic `AddEntry(InventoryEntry)` · `AddHarvestedItem(ItemInstance)` → keeps name, builds `InventoryEntry.FromResource` · `GetHotbarItems → GetHotbarEntries`.
6. **`UIInventoryItem` becomes a pure view wrapper:** new single constructor `UIInventoryItem(InventoryEntry entry)` exposing what the grid/hotbar render (icon resolved from entry refs, display count incl. the `ToolSwitcher` special case, colors). `SeedRuntimeState`/`CustomName`/`BackgroundColor` become pass-throughs to the entry (`entry.seedState` etc.) so the seed editor keeps working — it mutates the MODEL through the wrapper. Grid/hotbar controllers construct wrappers on refresh (`UIInventoryGridController.RefreshVisuals` — read it and adapt; it currently reads the shared list).
7. **`HotbarSelectionService.SelectedItem` retypes to `InventoryEntry`** (and gains `SelectedEntry` as the primary name with `SelectedItem` kept as `[Obsolete]` alias returning the same reference). `PlayerTileInteractor` reads: sweep its member accesses (`.Type`, `.SeedTemplate`, `.ToolDefinition`, `.ResourceInstance`…) to entry equivalents (`.kind`, `.seedTemplate`, `.tool`, `.resource`).

### G2.2 Caller-by-caller change list (mechanical)

| File (lines are hints) | Change |
|---|---|
| `GeneRewardSystem.cs:88–89` | `var item = new UIInventoryItem(randomGene); InventoryService.AddItem(item)` → `InventoryService.AddGene(randomGene)`. Delete `using Abracodabra.UI.Toolkit;`. |
| `StartingLoadoutApplier.cs:61, 77` | `AddItem(item)` → typed `AddSeed`/`AddGene`/`AddTool` per branch (read the method — it builds items from `StartingLoadoutConfig` entries). Delete UI usings. Keep the `OnInventoryReady` pattern byte-identical. |
| `PlayerActionManager.cs:290` | `AddHarvestedItem` unchanged (signature survives). Remove UI usings if now unused. |
| `PlayerTileInteractor.cs:164, 248` | `RemoveItemAtIndex` → `RemoveEntryAt` (or leave on the Obsolete shim, then clean at end). Retype `SelectedItem` reads per G2.1-§7. |
| `FeedingSystem.cs` (5 regions) | `GetHotbarItems→GetHotbarEntries`, `GetItemAt→GetEntryAt`, `RemoveItemAtIndex→RemoveEntryAt`; its `ConsumableData` bridging reads item members — map to entry fields (`resource`, `seedTemplate`, `stackSize`). |
| `GameUIManager` | Delete `playerInventory` ownership + `SetupPlayerInventory` body (→ `InitializeModel` call), rewire `RefreshHotbar`/grid refresh to build `UIInventoryItem` wrappers from `InventoryService` entries. `HandleInventoryServiceChanged` stays the refresh hub. |

**Sequencing within the session:** (1) create the three model files; (2) rewrite `InventoryService`; (3) `GameBootstrap` self-install; (4) `GameUIManager` donor rewiring + view-wrapper `UIInventoryItem`; (5) grid/hotbar/dragdrop controller adaptation; (6) non-UI caller sweep; (7) verification.

**Done when:** `grep -rn "Abracodabra.UI.Toolkit\|Abracodabra.UI.Genes" Assets/Scripts/Genes Assets/Scripts/Ecosystem Assets/Scripts/WorldInteraction` returns ZERO hits · `GameUIManager` contains no `List<UIInventoryItem>` field · rewards/loadout/feeding/planting compile against the model only · Milan play-check: starting items appear once, hotbar 1–8 works, seed editor still edits (wrapper pass-through), harvest lands in inventory, feeding consumes.

**Do NOT:** change `OnInventoryReady` firing order relative to UI construction · break the locked-seed-slot behavior in `UIInventoryGridController` (read it; port, don't redesign) · introduce a second source of starting items (StartingLoadoutApplier stays scene-absent; `StartingInventory` via the donor path remains the single active source — per projectmemory 2026-07-06) · serialize the model (no save system yet — keep it plain).

**If F1 already landed:** `InventoryEntry.seedState` is the flat-strand `PlantGeneRuntimeState`; nothing else differs.

---

## G3. Pack F3 — Run-loop screens (stats tracker, round summary, Game Over, HUD/reflection finisher)

**Read fully before starting:** `Core/RunManager.cs` · `Ecosystem/Management/WaveManager.cs` · `A_ToolkitUI/GameUIManager.cs` (reflection sites ~268–310 and ~531–539; `HandleRunStateChanged` ~844–874; panel queries ~74–77) · `A_ToolkitUI/GameUI_Document.uxml` · `PlantSystem/Growth/PlantGrowth.cs` (event region ~80–110) · `Ecosystem/Doris/DorisHungerSystem.cs` (~20–50) · `WorldInteraction/Player/PlayerHungerSystem.cs` (~50–63) · `WorldInteraction/Player/PlayerActionManager.cs` (~13–35, ~85–90) · `Ecosystem/Animals/AnimalController.cs` (`StartDying` ~464–496) · `WorldInteraction/Player/HungerUI.cs` · `Genes/Services/GeneEventBus.cs`.

### G3.0 Verified event/state surface (exact signatures)

- `RunManager` (SingletonMonoBehaviour, `WegoSystem`): `event Action<RunState> OnRunStateChanged` · `event Action<GamePhase,GamePhase> OnPhaseChanged` · `event Action<int> OnRoundChanged` (lines ~42–44); `RunState {Planning, GrowthAndThreat, GameOver}`; `int CurrentRoundNumber` (~38); `int RunSeed` (~40) + `randomizeSeedOnStart` (~30–34); `StartNewPlanningPhase()` (~152–163, branches on `WaveManager.Instance.IsWaveTimerComplete()` → `StartNewRound()` or `SetState(RunState.Planning)`); `RestartGame()` (~175–177) = scene reload; starvation → GameOver via `PlayerHungerSystem.OnStarvation` subscription (~63, handler ~99).
- `WaveManager` (hand-rolled `static Instance`): private `int waveStartTick / waveEndTick / waveSpawnTick` (~37–40); **TMP fields `waveStatusText` / `timeTrackerText`** (~30–31) updated by `UpdateTimeTracker()` (~294–313) and `UpdateWaveStatus()` (~315–346); public `IsWaveActive`, `IsWaveTimerComplete()`; `WaveState {Idle, Active, Spawning}`.
- `GameUIManager` reflection to remove: Doris block (~268–310): `Type.GetType("Abracodabra.Ecosystem.DorisHungerSystem, Assembly-CSharp")` → `FindFirstObjectByType(dorisType)` → `GetEvent("OnHungerChanged")` + `Delegate.CreateDelegate` → `GetProperty("CurrentHunger"/"MaxHunger"/"CurrentState")`; wave-tick private-field reads (~531–539). Panel switching: `HandleRunStateChanged` (~844–874) — note **`RunState.GameOver` currently falls through to `ShowHUDUI()`** — that's the hook to replace. Panels: `planningPanel = rootElement.Q<VisualElement>("PlanningPanel")`, `hudPanel = Q("HUDPanel")` (~76–77), both class `root-panel`; root from `GetComponent<UIDocument>().rootVisualElement` (~74).
- Stats events: `PlantGrowth.OnPlantDied : event Action<PlantGrowth>` (~line 83, fired in `Die()`) and `OnLeafConsumed : event Action<PlantGrowth, Vector2Int>` (~103, fired ~393 and ~691) — **both are INSTANCE events**, see G3.1-§2. `GeneEventBus` (service via `GeneServices.Get<IGeneEventBus>()`): `Subscribe<T>(Action<T>)/Unsubscribe<T>/Publish<T>`; `GeneExecutedEvent {ActiveGene Gene; int SequencePosition; bool Success; float EnergyCost}`; `SequenceCompletedEvent {int TotalSlotsExecuted; float TotalEnergyUsed}`. Doris: `OnHungerChanged(float,float)`, `OnStateChanged(HungerState,HungerState)`, `OnBecameHungry/Starving/Satisfied`, `OnStarvationTick`, `OnFed(float)`; `HungerState {Satisfied,Hungry,Starving}` (~22–49). Player: `OnHungerChanged(float,float)`, `OnStateChanged(HungerState)`, `OnStarvation`, `OnFed(ConsumableData,float)` (~50–63). Actions: `PlayerActionManager.OnActionExecuted : event Action<PlayerActionType, object>` (~33, fired ~88), `PlayerActionType {Move, UseTool, PlantSeed, Harvest, Interact}`. **Animals fire NO death event** — death path is `AnimalController.OnTickUpdate` health check → `StartDying()` (~464–496, internal fade then destroy).
- `HungerUI` (`WorldInteraction/Player/HungerUI.cs`): legacy UGUI `Slider` + TMP text bound to `PlayerHungerSystem.OnHungerChanged` — fully redundant with GameUIManager's UI Toolkit hunger bar (`playerHungerBarFill`/`playerHungerText`, updated ~460–479).

### G3.1 Locked design decisions

1. **Four deliverables, one session:** `RoundStatsTracker` → summary panel → Game Over panel → B3/B2 finisher. Build in that order (panels consume the tracker).
2. **Static mirror events (tiny, safe, precise anchors):** instance events can't be tracked across dynamically spawned objects without registration churn. Add static mirrors fired alongside the existing instance events — do not remove the instance events:
   - `PlantGrowth`: `public static event Action<PlantGrowth> AnyPlantDied;` invoke inside `Die()` right where `OnPlantDied` fires; `public static event Action<PlantGrowth, Vector2Int> AnyLeafConsumed;` invoke at BOTH fire sites (~393, ~691 — grep `OnLeafConsumed?.Invoke` to find them exactly).
   - `AnimalController`: `public static event Action<AnimalController> AnyAnimalDied;` invoke at the top of `StartDying()`; `public static event Action<AnimalController> AnyAnimalSpawned;` invoke where FaunaManager-spawned animals initialize (grep `FaunaManager` for the instantiation call and fire post-setup, OR fire from `AnimalController.Start` — pick whichever is unambiguous on read; count both spawn and death).
   - Static events MUST be cleared on scene reload: add `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStaticEvents() { AnyPlantDied = null; … }` in each host class (pattern exists in `GameBootstrap` — copy it).
3. **`RoundStatsTracker : MonoBehaviour`**, new file `Assets/Scripts/Core/RoundStatsTracker.cs`, global namespace (Core managers convention), self-installed from `GameBootstrap.Awake` via `AddComponent`, `static RoundStatsTracker Instance`. Subscribes in `Start()` (singletons ready): the static mirrors, `GeneEventBus.Subscribe<GeneExecutedEvent>`, `PlayerActionManager.OnActionExecuted` (count `Harvest`), `RunManager.OnRunStateChanged` + `OnRoundChanged` (reset/finalize boundaries). Unsubscribe in `OnDestroy`. Data:

```csharp
public class RoundStats {
    public int round;
    public int leavesLost, plantsDied, pestsSpawned, pestsDied, harvests, genesFired;
    public float energySpent;
    public string dorisEndState = "?", playerEndState = "?";   // one-shot reads at finalize
}
// API: RoundStats Current; RoundStats LastCompleted; event Action<RoundStats> OnRoundFinalized;
```

   Finalize on `GrowthAndThreat → Planning` transition and on `→ GameOver`; Doris/player states read at finalize via `FindFirstObjectByType<DorisHungerSystem>()` (one-shot — acceptable) and `PlayerHungerSystem` (find once, cache).
4. **Panels are code-built, not UXML edits** (precedent: GameUIManager already builds the seed-editor lock label, name editor, color picker entirely in code — G1 read shows the style). Two new controllers, files in `A_ToolkitUI/`, namespace `Abracodabra.UI.Toolkit`, plain classes per convention:
   - `UIRoundSummaryController`: builds an overlay `VisualElement` (absolute, full-screen, centered card, class `round-summary-panel`), appended to `rootElement`; `Show(RoundStats)` populates label rows (leaves lost, pests, harvests, Doris state, round number) + a "Begin Planning" button that just hides the overlay (informational v1 — `StartNewPlanningPhase` has already run; do NOT try to block the state machine).
   - `UIGameOverController`: same overlay pattern, class `game-over-panel`; `Show(RoundStats last, int round, int seed)` renders "Run Over" + stats + `Seed: {RunManager.Instance.RunSeed}` + Restart button → `RunManager.Instance.RestartGame()`.
   - `GameUIManager` owns both (construct in its controller-setup region, wire like existing controllers), shows summary on `GrowthAndThreat→Planning` in `HandleRunStateChanged`, and replaces the GameOver fall-through with `gameOverController.Show(...)` (HUD may stay visible behind; overlay sits on top). Styles into `Styles/GameUI_HUD.uss`.
5. **B3+B2 finisher:**
   - Add to `WaveManager`: `public float WaveProgress01 => …` computed from `waveStartTick/waveEndTick` and the TickManager current-tick accessor (**grep `TickManager.cs` for the public tick property — do not guess its name**), clamped, 0 when `WaveState.Idle`; plus `public int TicksRemainingInWave` if the HUD text wants it.
   - `GameUIManager` ~531–539: replace the `GetField` reflection reads with the new properties.
   - Delete `waveStatusText`/`timeTrackerText` fields and `UpdateTimeTracker()`/`UpdateWaveStatus()` from `WaveManager` (~30–31, ~288–346) and any calls to them — WaveManager stops being a UI class. **Tell Milan:** orphaned TMP GameObjects may remain in SampleScene — deleting them is a 2-minute manual step, purely cosmetic, non-blocking.
   - Doris reflection block (~268–310): replace with a typed field `DorisHungerSystem dorisHunger`, found via `FindFirstObjectByType<DorisHungerSystem>()`, normal `+=` subscriptions to `OnHungerChanged`/`OnStateChanged`, and direct property reads. **First verify DorisHungerSystem's namespace on disk** (the reflection string claims `Abracodabra.Ecosystem`) and add the matching `using`. Keep the null-tolerant "Doris UI disabled" logging behavior.
   - Delete `HungerUI.cs` (and tell Milan: remove the component/Slider GameObject in the scene later — non-blocking; the UI Toolkit hunger bar already covers it).

**Done when:** a full session shows Plan → Watch → **Summary overlay** → Plan, starvation shows a **Game Over screen with stats + seed + working Restart** · `grep -n "GetField\|GetEvent\|GetProperty\|Type.GetType\|CreateDelegate" Assets/Scripts/A_ToolkitUI/GameUIManager.cs` → zero hits · `grep -n "TextMeshPro" Assets/Scripts/Ecosystem/Management/WaveManager.cs` → zero hits · tracker numbers plausibly nonzero after a round with pests (Milan eyeball).

**Do NOT:** block or reorder the RunManager state machine for the summary (informational overlay only, v1) · migrate world-space TMP (thought bubbles, floating combat text, `PlantWorldUI`) — explicitly out of scope per review B3 · remove instance events when adding static mirrors · poll `FindObjectsByType` per tick for stats (mirrors exist precisely to avoid that).

---

## G4. Pack F4 — Hygiene sweep (mechanical; work top-down, stop anywhere cleanly)

Items are independent. For each: re-grep the listed sites first (line numbers are 2026-07-07 hints), apply the recipe, run the item's verification grep.

### G4.1 Deterministic RNG follow-up (A6 leftovers)

**The canonical project pattern (verified in `PlantGrowth.cs` ~270–271 and `GeneRewardSystem` — hybrid with fallback, keep it for consistency):**

```csharp
var rng = GeneServices.Get<IDeterministicRandom>();   // interface: Range(float,float), Range(int,int), SetSeed(int) — GeneServices.cs ~85–89
int v = (rng != null) ? rng.Range(a, b) : UnityEngine.Random.Range(a, b);
```

Cache the service reference in `Awake`/`Start` per class (don't `Get` per call). Seeded per run by `RunManager.InitializeRunSeed` (~70–83) — no change needed there.

**Convert (gameplay-outcome):** `FaunaManager.cs` ~155–179 (spawn edge/position/offset — spawn positions affect gameplay) · `AnimalMovement.cs` ~169 (flee dir), ~466–481 (wander pause chance/duration, direction shuffle) · `AnimalController.cs` ~319 (pest leaf pick) · `AnimalBehavior.cs` ~61, 68 (poop timing/variant — poop is fertilizer, gameplay) · `AuraWorldEffect.cs` ~171 and `CloudWorldEffect.cs` ~58 (leaf-regrow chance) · `BasicFruitGene.cs` ~58 (`OrderBy(x => Random.value)` shuffle → deterministic Fisher–Yates with `rng.Range(int,int)`; do NOT keep OrderBy-with-random, it's also O(n log n) and unstable) · `TrapGene.cs` ~94–95 already hybrid — leave.

**Keep on `UnityEngine.Random` + add `// cosmetic RNG — intentionally non-deterministic`:** `FireflyController.cs` ~110–111, 166, 248–249 · `AnimalController.cs` ~524–540 (thought-bubble text picks) · `PlantPlacementManager.cs` ~344 (visual jitter — note review B6 says this jitter is suspect anyway; leave, don't fix here) · `FloatingCombatText.cs` ~53 · `MapGenerationProfile.cs` ~53 (map seed has its own system) · `FireflyManager.cs` ~181–182 spawn rectangles (visual ambience — cosmetic).

**Verify:** `grep -rn "UnityEngine.Random\|Random.Range\|Random.value\|Random.insideUnitCircle" Assets/Scripts --include=*.cs` → every hit is either the hybrid fallback arm or carries the cosmetic comment.

### G4.2 Wall-clock → tick (B5)

- **`PlantGrowth.DelayedGrowthStart`** (~214, `WaitForSeconds(0.5f)`): replace coroutine with a tick counter — add `int growthStartDelayTicks = 1` consumed at the top of the growth branch of `OnTickUpdate` (decrement, return until 0). Delete the coroutine + its `StartCoroutine` call site.
- **`FaunaManager.SpawnEntryCoroutine`** (~110 `delayAfterSpawnTime`, ~127 `spawnInterval` — both `WaitForSeconds`): replace with a tick-scheduled queue: on wave start, precompute `List<(int spawnTick, entry)>` converting seconds → ticks via `TickConfiguration.ticksPerRealSecond` (grep TickManager/TickConfiguration for the accessor; round up, min 1); process the queue in a `TickManager.OnTickAdvanced` subscription (subscribe on wave start, unsubscribe on wave end/`OnDestroy`). Flag to Milan: spawn pacing now scales with game speed (Tab) — intended.
- **`WaveManager.EndWaveCoroutine`** (~204, `WaitForSeconds(1f)` state hold): convert to a 2-tick countdown in its existing `OnTickUpdate`.
- **Leave (visual-only):** `PlantGrowth.DamageFlash` (~877), `CleanupTemporarySpawnPoints` (~813 — dies with B4 later), `PlantSequenceExecutor.ClearExecutionFlag` (0.5s highlight), `TimingCircleMinigame` (~329, real-time minigame by design), `GeneEffectPool` lifetime `Invoke` (~102–107, pooled VFX).
- **Do NOT touch** `GardenerController.MultiTickActionCoroutine` (~218) or `PlayerActionManager.DelayedActionCoroutine` (~315) — the Planning-movement branch is documented-dormant (projectmemory "on the horizon"); converting it interacts with `ExecutionPhaseDriver` and needs Milan's design call.

**Verify:** `grep -rn "WaitForSeconds" Assets/Scripts --include=*.cs` → only the "Leave" list + minigames remain.

### G4.3 Init `Invoke` delays → A5 pattern

Template is `StartingLoadoutApplier.cs:17–34` (verified): if `InventoryService.IsInitialized` act now, else subscribe `OnInventoryReady`, always unsubscribe in handler + `OnDestroy`. Apply the same shape with **`InitializationManager.IsReady` / `OnReady`** (both exist, added by A5):

- `FeedingSystem.cs:78` `Invoke(nameof(AutoRegisterFeedables), 0.2f)` → gate on `InitializationManager`.
- `PlayerHungerSystem.cs:149` + `AnimalFeedableAdapter.cs:122` `Invoke(nameof(DelayedFeedingRegistration), 0.3f)` → these wait for `FeedingSystem.Instance` — gate on `InitializationManager.OnReady` and null-check `FeedingSystem.Instance` in the handler (log error if still null: tripwire, not silent self-repair).

**Verify:** `grep -rn "Invoke(nameof\|Invoke(\"" Assets/Scripts --include=*.cs` → only `GeneEffectPool` remains.

### G4.4 Spatial-query hotspots → registries

1. Add `public static readonly List<AnimalController> AllActiveAnimals` to `AnimalController` — copy the exact `PlantGrowth.AllActivePlants` implementation (grep it first: registration in Awake/OnEnable, removal in OnDestroy — mirror precisely, plus the static-reset method from G3.1-§2 if F3 didn't land).
2. `TargetFinder.cs` (static; `FindCreaturesInRadius`, `FindPlantsInRadius`, `FindNearestCreature`, `HasCreatureInRange` — all currently `FindObjectsByType` at ~11, 53, 76): iterate `AnimalController.AllActiveAnimals` / `PlantGrowth.AllActivePlants` instead. Signatures unchanged — callers (`ReactiveBurstHandler` ~115, world effects, `PlantSequenceExecutor` target gate) untouched.
3. Per-tick/per-frame offenders: `TickDebugMonitor.cs:37` (per-frame `Update`) · `EnvironmentalStatusEffectSystem.cs:126–127` (`OnTickUpdate`) · `WaveManager.cs:261` (`OnTickUpdate`) → registries. `FeedingSystem.cs:138` `FindObjectsByType<MonoBehaviour>()` (!) → iterate the two registries + `GardenerController` (cache) instead of scanning every MonoBehaviour in the scene.
4. Leave init-time finds (GridSnapStartup, FoodSelectionPopup.Awake, TileDefinition.Awake, FaunaManager spawn-position lookups — cache the latter in fields on first use if trivial).

**Verify:** `grep -rn "FindObjectsByType\|FindObjectOfType\|FindAnyObjectByType" Assets/Scripts --include=*.cs` → no hits inside `Update`/`OnTickUpdate` bodies.

### G4.5 B6 nits

- **`GeneInstanceData` duplication** (`Runtime/RuntimeGeneInstance.cs` ~109–178): delete public fields `powerMultiplier` and `stackCount` from **`GeneInstanceData`** — the dict (`_runtimeValues` via `SetValue/GetValue("power_multiplier")`) is what runtime reads. ⚠️ Precision: `GeneTemplateEntry.powerMultiplier` (`SeedTemplate.cs`) is a DIFFERENT, serialized authoring field — keep it. `ItemInstance.stackCount` is unrelated — keep it. Sweep grep: `\.powerMultiplier` and `\.stackCount` checking the receiver type of each hit.
- **Filename typo:** both files exist and the class inside is already correctly `ReactiveBurstHandler`. Rename via bash (mount path): `mv ".../Assets/Scripts/Genes/Implementations/Active/RactiveBurstHandler.cs" ".../ReactiveBurstHandler.cs" && mv ".../RactiveBurstHandler.cs.meta" ".../ReactiveBurstHandler.cs.meta"` — **both together, GUID inside the .meta is preserved**. Then grep `RactiveBurst` → zero hits.
- **`PlantGrowth.TakeDamage`** (~862): already `[System.Obsolete]` with ZERO callers (verified) — delete the method.
- **Log gate:** new `Assets/Scripts/Core/GameLog.cs`: `public static class GameLog { public static bool Verbose = false; public static void V(string msg, UnityEngine.Object ctx = null) { if (Verbose) Debug.Log(msg, ctx); } }`. Route the unconditional per-tick offenders through it — worst first: `PlantGrowth` (~10 unconditional logs incl. per-withering-tick ~235), `PlantSequenceExecutor` (init + editor logs), `InventoryService`/`UISeedEditorController` per-action logs. Flip default-TRUE debug flags to false: `PlantPlacementManager.verboseLogging` (~27), `PlantGrowthModifierManager.showDebugMessages` (~29), `FoodSelectionPopup.debugLog` (~42), `GridSnapStartup.debugLog` (~11). ⚠️ These are `[SerializeField]` — scene/prefab values override code defaults; tell Milan to check the four components in-Editor once (10 seconds each) or leave — the code-default flip covers new scenes.
- **Skip by default:** singleton unification (16 hand-rolled `static Instance` classes — census available in this session's research; touching lifecycles buys little pre-demo). Only if everything above is done and time remains, and then only `WaveManager`/`WeatherManager`.

**Done when (any stopping point):** each finished item's verification grep passes; unfinished items listed explicitly in the session's closing report (scope discipline — no silent drops).

---

## Closing duties for every executor session

1. Report: what shipped, what didn't, compile-check focus list.
2. Verify all written files host-side (Read the tail of each).
3. Update `01_Core/projectmemory.md` Current state (+ codebase-map section per G0-§9) — claims must be disk-verified, never aspirational.
4. Ask Milan to: open Unity (compile triage), run the Part-4-style play checks listed in the pack's Done-when, re-run `unity_extractor_RUN.bat`, git-commit.
5. When a pack's Done-when fully passes, move its section status in `2026-07_Fable5_Last_Day_Plan.md` and, when all four packs land, move both docs to `03_Tasks/Done/`.

**Next action:** execute G1 Stage 1 (parser core + sandbox tests) — it validates the entire F1 design before any Unity file changes.
