# UI Systems Research — Smoothing Controls & Interface for a Complex Game

*2026-07-08 · research/concept doc · nothing here is implemented unless marked ✅ (on disk)*
*Companions: `Gameplay_Engagement_Research.md` (D1–D9), `Commit_And_Watch_Loop_Design.md` (Rev 2), codebase map §8.*

---

## 0. TL;DR (honest version)

The UI foundation is better than average for a solo project at this stage — one UI Toolkit document, controller-per-panel discipline, a real service layer. But it was built for a game that no longer exists (pure "edit seeds, then watch"), and both phase-control futures now on the table put **new weight on surfaces that are currently the weakest**: time/turn feedback, verb-cost visibility, world-space information, and input routing.

Five findings drive everything below:

1. **The scenario decision changes maybe 30 % of the UI work.** ~70 % is a shared core you can build now without deciding: feedback layer, time HUD, tooltip unification, input rewrite, run-loop screens, editor UX. Deferring the phase decision is *not* an excuse to defer UI work.
2. **Input is the single most fragile layer.** Legacy Input Manager, 30 scattered `Input.*` call sites (verified), input-blocking via a static bool on a popup (`FoodSelectionPopup.IsBlockingInput`), hard-coded keys, no rebinding, no gamepad path. Every future system (budget verbs, wait key, auto-pause toggles, minigames) lands on this layer. Centralize it **before** adding either scenario's verbs.
3. **Time is invisible.** A tick game where the player can't read time is Stoneshard's most-repeated UI criticism (players literally can't tell the time of day — see §3.2). You have `tick-text` and a wave progress bar; you need a *clock the player feels*: phase banner, sun-arc, speed indicator, "what will happen next tick" affordances.
4. **The game already emits the events a good UI needs** (`GeneValidationFailedEvent`, `SequenceCompletedEvent`, `OnPlantDied`, Doris state events, wave state changes) — most of the feedback layer is subscribers, not systems. This is the cheapest high-impact work in the whole project.
5. **Two of the three worst code smells are UI smells.** B1 (UI owns inventory data) and B2 (reflection coupling in `GameUIManager`/`WorldHoverTooltip`) both live in the UI layer; both are already specced (F2 pack / G2). UI *improvement* work should ride those fixes, not precede them.

Recommended spine (details §7): **Input Router → Feedback Layer v0 → Time & Phase HUD → run-loop screens (F3) → editor UX pass → scenario-specific layer after the §4.3 experiment.**

---

## 1. Method & evidence base

- Disk-verified against the live repo (2026-07-08): codebase map §8, `GameUI_Document.uxml` + 5 USS files (~960 lines), controller inventory in `Assets/Scripts/A_ToolkitUI/`, grep for input call sites (30 × `Input.GetKey/GetMouseButton`, 0 × `UnityEngine.InputSystem`), no pause/settings/game-over/`RoundStatsTracker` classes on disk.
- Design docs: engagement research (D1–D9, §4.7 feedback pass), Commit & Watch Rev 2 (budget UX, Report, auto-pause), F3 run-loop screens pack (G3 guide).
- External: Stoneshard community UI criticism, Against the Storm QoL/UI praise (the genre's gold standard for readable tick/speed UI), Unity 6 UI Toolkit runtime data binding docs. Sources §9.
- Convention: ✅ exists on disk · 🔧 specced in an existing pack · 💡 new in this doc · [A]/[B] scenario-specific · effort S/M/L.

---

## 2. Current UI audit — what exists and where it creaks

### 2.1 Inventory of surfaces ✅

| Surface | Implementation | State |
|---|---|---|
| Planning screen | `PlanningPanel`: SeedEditor (left) + Inventory grid (center) + SpecSheet (right) | Solid 3-column layout; editor is 742L controller |
| HUD | `HUDPanel`: top tick/wave bar; bottom monolith = player hunger + hotbar (ListView, keys 1–8) + Doris hunger | Functional, sparse |
| Drag & drop | `UIDragDropController` (all drag-drop incl. editor internal moves, 6 events) | Good centralization — keep this pattern |
| Tooltips | `hotbar-tooltip-label` + `world-hover-tooltip` labels in HUD; `WorldHoverTooltip` (364L, TMP world-space) | **Two stacks, three code paths** |
| World-space info | `PlantWorldUI` energy bar, `ThoughtBubbleController`, `FloatingCombatText` (exists, underused), `HungerUI` (legacy UGUI slider!) | Fragmented: TMP + UGUI + UI Toolkit coexist |
| Modal | `FoodSelectionPopup` (648L, static `IsBlockingInput`) | Works; the blocking pattern doesn't scale |
| Minigame | `TimingCircleMinigame` | Perfect ≡ Good by default (silent skill ceiling) |
| Run-loop screens | **None on disk** — no pause menu, no settings, no round summary, no game over | Biggest structural hole; F3/G3 covers it 🔧 |

### 2.2 What's already right (don't churn)

- Single UXML document + USS split by concern; controllers named `UI[Name]Controller`; static services bridging UI↔game. Consistent and greppable.
- One drag-drop controller instead of per-slot handlers — this is the pattern the rest of the input layer should copy.
- Spec sheet already has containers for synergies/warnings/sequence analysis — the *surface* for buildcraft legibility exists; it's the data model (`SeedQualityCalculator` conflation) that lies.
- Panel switching driven by `RunManager.OnRunStateChanged` — the phase machine already owns the UI, which is exactly what both scenarios need.

### 2.3 Verified weaknesses (ranked by how much they'll hurt in 6 months)

1. **Input layer (critical).** 30 raw `Input.*` sites across `PlayerTileInteractor`, `ExecutionPhaseDriver`, minigames, debug keys. Hard-coded: 1–8 hotbar, Space pause, Tab speed, T debug-tick, left-click contextual verb, right-click eat. No rebinding, no gamepad, no input consumption model beyond one static bool. Adding either scenario multiplies verbs; adding minigames multiplies contention (who owns Space?). This fails quietly and then all at once.
2. **B1 — UI owns game data.** `GameUIManager` owns `playerInventory` and registers it into `InventoryService`. Every future UI (chests, fixture stock, Doris Bowl contents, draft screens) compounds the inversion. Fix is specced (F2/G2: `Abracodabra.Inventory` model + service keeps name/events). UI work that touches inventory should wait for or ride this.
3. **B2 — reflection coupling, 3 sites.** `GameUIManager`/`WorldHoverTooltip` bind Doris events via `Type.GetType` + `Delegate.CreateDelegate` and read `WaveManager` private tick fields via reflection. Any rename silently kills HUD features. Replace with direct events or Unity 6 runtime data binding (§4.6).
4. **Three UI stacks.** UI Toolkit (screens) + TMP world-space (tooltip, plant bars, thoughts) + one legacy UGUI slider (`HungerUI`, B3). Every visual-polish pass pays triple. Unity 6 added world-space UI Toolkit support **[verify exact 6.x minor version in Editor before committing to migration]** — consolidation is now plausible post-demo (§4.4).
5. **Time illegibility.** `Tick: 0/100` text + wave bar is the entire clock. No phase banner, no speed indicator (Tab cycles speed with zero UI echo — verified: driver has no HUD hook), no next-tick preview, no day-beat structure. Both scenarios die without this (§3).
6. **Silent systems.** Minigame Perfect ≡ Good; rewards appear in inventory unannounced; plant death is a quiet fade; energy stalls are invisible (`GeneValidationFailedEvent` fires into the void). Engagement §4.7 already prices the fix pass at all-S.
7. **Modality by static bool.** `FoodSelectionPopup.IsBlockingInput` is checked by `PlayerTileInteractor`. Add a pause menu, a report screen, a draft overlay, and a minigame — five booleans and a bug farm. Needs a modal stack (§4.1).

---

## 3. What the references actually teach (filtered for Abracodabra)

### 3.1 Against the Storm — the genre gold standard for "complex but smooth"

The consistently praised properties, mapped to this project:

- **Speed control is free of guilt:** pause/speed affects *everything proportionally*, so there's no optimal speed — players pause constantly without penalty. Your `ExecutionPhaseDriver` already has `SetPaused`/speed; what's missing is the *UI contract*: visible speed state, keys echoed on screen, pause never punished. [A] inherits this wholesale.
- **Hover-first information economy:** every icon tooltips; every tooltip nests (hover a resource inside a tooltip → its tooltip). You have three tooltip code paths and no nesting. One tooltip service (§4.3) is the fix.
- **Cause attribution:** production/consumption deltas name their sources. This is exactly the Commit & Watch Report's "Bowl ran dry at tick 61 → Doris ate a Sunfern" — the pattern generalizes to the whole HUD (why is energy dropping? who ate this leaf?).
- **Same-kind cycling:** select a building → arrows cycle all buildings of that kind. Steal for plants: select a plant → Tab/arrows cycle all plants of the same seed, with the spec sheet following. Cheap (`AllActivePlants` is static ✅) and huge for farm-wide reading. 💡 S–M.

### 3.2 Stoneshard — the cautionary WeGo tale (directly relevant to Scenario B)

Community criticism clusters (Steam discussions, wiki):

- **You can't tell the time.** No clock; players resort to guessing by sky tint. Lesson: if ticks matter, the clock is a *primary* HUD citizen, diegetic or not. (The sun-arc meter from Commit & Watch Rev 2 answers this for [A]; [B] needs it even more.)
- **Mouse-only heaviness:** too many clicks for routine acts (loot transfers, repeat travel). Lesson for [B]: repeat-last-action, travel-by-click (many ticks in one command, interrupted by threat — Stoneshard *does* do this well), and keyboard verbs are mandatory, not QoL.
- **One-turn-at-a-time combat vs. multi-turn travel** is their core rhythm: bulk time when safe, granular time when threatened. This is the single most important mechanic to copy if you go [B] (§6.2 "danger interrupts").

### 3.3 Into the Breach / Opus Magnum (already touchstones)

- ItB's entire UI is a **consequence preview**: every hover shows exactly what next turn does. Your determinism (A-pack, `IDeterministicRandom`) makes a scoped version possible — the engagement doc's §5.1 "Deterministic Rehearsal" ghost preview. UI framing: it's a *hover consequence layer*, not a simulation feature (§4.7).
- Opus Magnum's **replay/report screen** (cost/cycles/area histograms) is the model for the Night Report: your run had numbers; show me *mine vs. my past selves*, not a grade. Feeds F3's `RoundStatsTracker` 🔧.

### 3.4 Unity 6 UI Toolkit (platform reality check)

- **Runtime data binding** (MVVM-ish) is production-ready in Unity 6 and directly replaces the B2 reflection sites and most manual `UpdateX()` HUD methods: bind `DorisHungerSystem` state → bar fill, `TickManager` tick → clock, `WaveManager.WaveProgress01` (G3 adds it) → bar. Migrate opportunistically (new UI binds; old UI converts when touched).
- **Performance hygiene** for the HUD you'll be growing: `DynamicTransform` hint on frequently-moving elements (bars, floaters), `DisplayStyle.None` (not opacity) for hidden panels, pooled floaters. Cheap rules, adopt now.
- **World-space UI Toolkit** exists in newer 6.x **[verify]** — candidate unification target for `PlantWorldUI`/`WorldHoverTooltip`/`HungerUI`, but this is polish-phase work, not demo work.

---

## 4. The shared core — scenario-agnostic systems (build regardless of the decision)

Ordered by leverage. Everything here survives either §4.3 outcome untouched.

### 4.1 Input Router & modal stack 💡 [M] — *the keystone*

One `InputRouter` (static service or singleton, matching `InventoryService` style) that owns every frame's input read and dispatches by **context stack**: `Gameplay` → `Minigame` → `Modal(popup)` → `Menu(pause)`. Push/pop replaces `IsBlockingInput`-style statics; top of stack consumes, lower layers see nothing.

- Keeps legacy Input Manager for now (30 call sites migrate to router queries; no Input System package migration during demo crunch — that's a [POST] swap the router makes trivial later).
- Single keymap table (a `ScriptableObject`, designer-editable) instead of scattered `KeyCode` literals → free rebinding screen later, free "what does Tab do" hints now.
- **Done when:** grep for `Input.Get` outside `InputRouter` returns 0; opening the food popup, a minigame, and (later) the pause menu simultaneously resolves in stack order with no static booleans.
- **Why first:** both scenarios, all minigames, auto-pause toggles, and every new screen depend on it; retrofitting a router under 60 call sites in six months will cost a week and a regression pass.

### 4.2 Time & Phase HUD 💡 [M] — *make the clock a character*

- **Phase banner** on `OnRunStateChanged`/beat changes: a 1-second sweep ("MORNING — 40 ☀ remaining" / "DAY — the garden acts"). S.
- **Sun-arc clock** (Commit & Watch Rev 2 §4.5): one arc widget serving double duty — [A] Morning: budget meter (ticks left); Day (both scenarios): time-of-day with night marker. Replaces raw `Tick: 0/100` as the primary read; keep the number as a tooltip. M.
- **Speed & pause echo** ✅→🔧: driver already has speed/pause; HUD shows current multiplier (▶ ▶▶ ⏸), flashes on change, heartbeat audio at ×2+ (engagement §4.7). S.
- **Next-tick strip (v0):** small icon row of *scheduled* things the player already deserves to know: wave arrival countdown (WaveManager state ✅), plant recharge completions, Doris threshold crossings. Not a simulation preview — just surfacing existing timers. M.
- **Done when:** a new player can answer "what phase is it, how fast is time moving, what happens next" from the HUD alone in under 3 seconds.

### 4.3 Unified tooltip & inspection service 💡 [M]

One `UITooltipService` in UI Toolkit rendering for *both* screen elements and world hovers (world → screen projection; retire the TMP `WorldHoverTooltip` path when parity is reached):

- Standard blocks: name/type header · stat table · payload/dynamic-props chips · **cause line** ("+2 energy/tick: 4 leaves × 0.5") · verb hints footer ("RMB: eat · E: feed Doris").
- Nesting one level deep (Against the Storm pattern): hover a gene chip inside a seed tooltip → gene tooltip. Gate behind a short hover-hold to protect edit speed.
- Inspection = pinned tooltip: middle-click pins it as a small draggable card (poor-man's multi-inspect for comparing two plants). S on top of the service.
- **Done when:** hotbar, inventory, editor slots, plants, animals, Doris, fixtures, and tiles all route through one service; the three legacy paths are deleted.

### 4.4 Feedback layer v0 🔧 [S-pack] — *do first, it's all subscribers*

Adopt engagement §4.7 wholesale as the UI charter: yield floaters (`FloatingCombatText` ✅ unused for yields), wave banner, night/firefly cue, reward toasts (D6), Doris stingers, leaf-scatter death puff, **stall legibility** (dim gene icon + trickle bar on `GeneValidationFailedEvent` ✅) and **sequence celebration** (`SequenceCompletedEvent` ✅). Plus one addition: 💡 **minigame grade echo** — Perfect must *say* Perfect (toast + distinct chime) even before rewards differ (U-pack fixes the reward; UI shouldn't wait for it).

### 4.5 Run-loop screens 🔧 [F3/G3 — already specced]

Round summary (Report), Game Over, `RoundStatsTracker`, HUD wave progress via `WaveProgress01`, static mirror events. This doc adds only UI requirements to G3: report rows must carry **cause attribution** (Commit & Watch §5) and **comparison to previous rounds** (Opus Magnum pattern, §3.3); Game Over names its killer ("Doris starved — bowl empty since Day 4"). Plus the missing sibling: 💡 **pause/settings menu** [S–M] — resolution/volume/rebind placeholder/auto-pause toggles live here; it's also the [A] settings surface Rev 2 assumes exists. Ride the same pack.

### 4.6 De-reflection & data binding 🔧→💡 [S–M]

Kill the three B2 reflection sites via direct public events (G3 already adds mirror statics) or runtime data binding. Policy going forward: **new HUD elements bind or subscribe; never poll, never reflect.** This is a rule, not a system — write it into the codebase map invariants when applied.

### 4.7 Editor UX pass (the edit-speed principle, operationalized) 🔧 mostly

Already-specced items to schedule as one pass, not piecemeal: quality-score split into four sub-bars (engagement §4.5) · trigger-lane visual separation · sequence reorder by drag (only if faster than remove+re-add) · combo toasts. This doc adds:

- 💡 **Phrase-chip affordance test** [S]: before building Phrase Chips (projectmemory design thread), mock the *UI only* — a chip = 3 fused slots rendered as one draggable token — and time edit tasks with/without. The chip system's value is an edit-speed claim; test it as UI before building runtime support.
- 💡 **Keyboard editing lane** [M]: arrows move slot cursor, Enter picks from a filtered popup, Del clears. Mouse-only editing is the Stoneshard complaint (§3.2) transplanted into your highest-frequency screen. Measure: full re-gene of a 6-slot seed in <15 s without touching the mouse.
- 💡 **Diff ghost** [S–M]: while editing, spec sheet shows old→new deltas (▲▼ on each metric) instead of absolute values only. Determinism makes it exact; it converts the spec sheet from a report into a *conversation*.
- 💡 **Ghost consequence hover v0** [M, gated on §5.1 experiment]: hovering "Start Day" (A) or a planted seed shows projected first-day income/first-fire tick, computed by the same deterministic math the spec sheet already does — scoped strictly to per-plant projections, not world simulation.

### 4.8 Grid & world affordances 💡 [S–M each]

- **Hover verb preview:** cursor over a tile shows the verb that would fire (till/plant/water/harvest icon) + its cost ([A]: budget ticks; [B]: world ticks). Extends `TileInteractionManager` hover (✅ hover cell + range check exist).
- **Range/coverage rendering:** one `CoverageRenderer` for fixture radii, tool ranges, trigger-proximity gates — `GridDebugVisualizer` radius APIs ✅ are the seed; promote from debug to player-facing style.
- **Selection follow-cycle:** Against the Storm same-kind cycling (§3.1) over `AllActivePlants`.

---

## 5. Scenario A — fully automated Day ("Commit & Watch")

*Premise: Morning is budgeted decision space; Day runs on `ExecutionPhaseDriver` autopilot with opt-in interventions. The UI's job: make committing feel informed, and watching feel like reading, not waiting.*

The Rev 2 doc already carries most requirements; consolidated here as the UI bill of materials:

| System | Notes | Effort | Depends on |
|---|---|---|---|
| **Budget meter (sun-arc)** | Primary Morning HUD; ticks-remaining + per-verb cost flash on hover; red glow under 10 % | M | 4.2 |
| **Verb cost hover** | Both cost models (A/B) read from config so the experiment toggle drives UI free | S | 4.8 |
| **Start Day button + foot-gun warnings** | "Bowl empty — start anyway?" modal; warnings enumerated from a checklist service (bowl, unwatered planted tiles, unspent large budget) | S–M | 4.1 modal stack |
| **Report screen with cause rows** | F3 Report + attribution lines; group by actor (You / Fixtures / Doris / Pests) | M | 4.5 |
| **Auto-pause settings** | Toggles: wave / craving / ripe window / stall → `driver.SetPaused` ✅; **this is [A]'s accessibility valve — treat as CORE, not settings garnish** | S | 4.5 pause menu |
| **Lean-in prompts** | Ripe-window sparkle + edge-of-screen arrow when off-screen; craving bubble timer ring; Shoo! flash. Budget: ≥1 prompt / 15–20 s (Rev 2 density metric) — the UI *is* the pacing tool here | M | 4.4 |
| **Intervention affordance** | During Day, cursor shows only *available* interventions (harvest hand on ripe fruit, shoo on pest); everything else greys — teaches the phase contract silently | S–M | 4.8 |
| **Fixture UI** | Placement ghost + coverage ring (4.8) · stock pips above Bowl/Basket · "acted" blips on tick (Basket blinks when collecting) | M | 4.8 |

**Failure mode the UI must prevent:** the screensaver Day (Rev 2 risk #1). UI levers: prompt density, event ticker (small scrolling log of Day events with cause lines — doubles as Report source), and auto-pause defaults **on** (wave + craving) for first runs.

**Done when:** a playtester on 2× speed can narrate the Day aloud ("basket got those, she's craving spicy, wave at dusk") without pausing; the Report contains zero surprises.

---

## 6. Scenario B — player-controlled WeGo Day (Stoneshard-style)

*Premise: no autopilot; every Day tick is player-driven (`RequestActionTicks` returns to being the only clock, i.e. revert/limit `ActionsDriveTicks` no-op behavior). The UI's job: make hundreds of small time decisions frictionless, and make waiting a deliberate act.*

### 6.1 The non-negotiables (all are Stoneshard lessons)

| System | Notes | Effort |
|---|---|---|
| **Wait/pass controls** | Space = pass 1 tick (rebind from pause); hold = repeat with acceleration; `W`+number or scroll-wheel = pass N. *Pass must show what happened*: micro-summaries float as time flows ("+3E", "pest moved") | M |
| **Danger interrupt** | Bulk waits/moves cancel instantly on: wave spawn, pest targeting your plant, Doris threshold, plant stall. This is Stoneshard's best mechanic; without it [B] is a Space-bar hold simulator | M — needs an `InterruptService` aggregating existing events ✅ |
| **Time-of-day clock** | Sun-arc (4.2) is now *critical* — [B] has no phase banner rescuing legibility; Stoneshard's #1 complaint was exactly this hole | (shared) |
| **Multi-tick travel** | Click destination → pathed move consuming ticks, interruptible (Stoneshard travel model). `GardenerController` multi-tick moves ✅ + `GridPositionManager.GetPath` ✅ — mostly wiring + a path preview line | M |
| **Repeat-last-verb** | `R` repeats last action on next valid target (water 8 tiles = 1 aim + 7 R). Routine chores are [B]'s biggest tedium risk | S–M |
| **Turn echo log** | Scrolling 3-line feed of what the last tick did (who moved, ate, fired). [B]'s equivalent of [A]'s Report — without it, simultaneous resolution reads as chaos | M |

### 6.2 The honest cost of [B]

- **Tedium is structural, not a bug to polish away.** Farming has many routine verbs; WeGo makes each one a turn. Repeat/queue/travel systems above are *mitigations* — Stoneshard still gets "too much clicking" reviews after years of QoL patches. Budget for a second QoL wave after the first playtest.
- **The show conflicts with the clock.** D4's "execution spectacle" happens *between* player inputs; impatient players will spam-pass through their own fireworks. Mitigation: brief auto-slowdown (not pause) when a sequence fires on screen — cheap, but it's a band-aid on a real tension [A] doesn't have.
- **Minigames need a time contract:** either they pause the world (frozen-time skill checks) or cost ticks to attempt. Decide once, apply to all (U3's driver-pause logic inverts here).
- **Hunger clocks + player-driven time = pressure players resent** if illegible. Both hunger bars must show *ticks-to-threshold* on hover, not just fill percent. S.

### 6.3 What [B] buys for the price

Full Into-the-Breach-style deliberation during the *reactive* phase (not just Morning), every minigame/lean-in moment happens at player-chosen tempo (accessibility win without an auto-pause system), and phase identity becomes "planning = compose, day = conduct" — closer to Milan's original "actions drive everything" instinct (Rev 2 §5 notes auto-pause-heavy [A] converges toward semi-WeGo anyway; [B] just commits fully).

---

## 7. Comparison, prioritization & build order

### 7.1 The decision in one table

| Axis | [A] Commit & Watch | [B] Player-driven WeGo |
|---|---|---|
| New UI systems | Budget meter, Report, lean-in prompts, fixture UI | Wait/pass, interrupts, travel preview, echo log, repeat |
| UI effort (beyond shared core) | ≈ 4–5 weeks solo w/ buffer | ≈ 5–7 weeks solo w/ buffer (+ mandatory 2nd QoL wave) |
| Worst UI failure | Screensaver Day | Click-fatigue chores |
| Genre-reference safety net | Against the Storm patterns map ~1:1 | Stoneshard shows the potholes but also years of unfixed ones |
| Shared-core reuse | ~70 % | ~70 % |

Note the overlap is the headline: **auto-pause-everything [A] and fast-repeat [B] converge on similar moment-to-moment feel.** The §4.3 experiment afternoon (already the gate) settles it; the shared core is buildable meanwhile without waste.

### 7.2 Build order (UI track only; slots between existing packs)

| Stage | Item | Effort | Tag |
|---|---|---|---|
| 0 | **Input Router + modal stack** (§4.1) — before any new verbs land | M | CORE |
| 1 | **Feedback layer v0** (§4.4, rides engagement §4.7) + speed/pause echo | S-pack | CORE |
| 2 | **Time & Phase HUD** (§4.2: banner + sun-arc + next-tick strip) | M | CORE |
| 3 | **Run-loop screens** (F3/G3) + pause/settings menu (§4.5) | M | CORE |
| 4 | **Tooltip service** (§4.3) + hover verb/cost preview (§4.8) | M | CORE |
| 5 | **Editor UX pass** (§4.7: sub-bars, trigger lane, keyboard lane, diff ghost) | M–L | CORE-lite |
| 6 | **Scenario layer** (§5 *or* §6, post-experiment) | M–L | gated |
| POST | Gamepad + rebind screen (router makes it cheap) · world-space UI Toolkit unification [verify] · ghost consequence hover full version · nested tooltips depth 2 · Input System package swap | — | POST |

Stages 0–4 ≈ **3–4 weeks solo with buffer**; they overlap heavily with already-planned F2/F3 work (ride, don't duplicate). Stage 5 respects the edit-speed principle as its own done-when.

### 7.3 Anti-goals (UI edition)

1. **No mid-crunch Input System package migration** — the router isolates the decision; the swap is POST.
2. **No fourth UI stack.** Anything new is UI Toolkit; TMP world-space is frozen (maintained, not extended) until the unification decision.
3. **No tutorialization pass yet** — good affordances first (hover verbs, phase banners teach silently); explicit tutorials are a demo-polish item.
4. **No settings sprawl** — settings menu ships with exactly: audio, resolution, auto-pause toggles [A], keybind list (read-only v0).
5. **Edit-speed is the tiebreak** for every editor feature: if it adds a click to the common path, it's out (Splice anti-recommendation stands).

---

## 8. Risks

1. **Router retrofit regressions** — 30 call sites moved at once. Mitigate: move one consumer per commit, play a full round between (the sandbox pack's compile-triage loop applies).
2. **HUD growth vs. cozy-dark art direction** — every §4 widget fights the "Stardew from Aliexpress → polished" pass later. Mitigate: all new HUD in USS variables from day one (colors/spacing tokenized), so the art pass is a stylesheet, not a rebuild.
3. **Building [A] UI before the experiment** — the budget meter and Report are ~40 % throwaway if [B] wins. Mitigate: stages 0–4 are scenario-neutral by construction; hold §5/§6 items until the gate.
4. **Tooltip service scope creep** — nesting + pinning + world projection can eat two weeks. Mitigate: v0 = screen-space, no nesting, standard blocks only; each addition is its own S task.
5. **Data-binding half-migration** — mixed bound/manual HUD is worse than either. Mitigate: the §4.6 policy (new binds, old converts when touched) is written into the codebase map when the first binding lands.

---

## 9. Sources & verification

- Disk: `GameUI_Document.uxml`, USS files, codebase map §8 (2026-07 sweep), grep results (input call sites; absent classes) — all verified 2026-07-08.
- Project docs: `Gameplay_Engagement_Research.md` §§3–6, `Commit_And_Watch_Loop_Design.md` Rev 2 §§2–5, `2026-07_Fable5_Last_Day_Plan.md` (F2/F3), `2026-07_Pack_Implementation_Guides.md` (G2/G3).
- External: [Stoneshard: How to Tell Time (GameRant)](https://gamerant.com/stoneshard-how-to-tell-time/) · [Stoneshard controls/UI criticism (Steam)](https://steamcommunity.com/app/869760/discussions/0/1609400247626376911/) · [Against the Storm QoL Update 2 (Eremite)](https://eremitegames.com/ats-quality-of-life-update-2/) · [Against the Storm UI reference (Interface In Game)](https://interfaceingame.com/games/against-the-storm/) · [Unity 6 UI Toolkit data binding (Manual)](https://docs.unity3d.com/6000.4/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/data-binding.html) · [UI Toolkit performance best practices (Manual)](https://docs.unity3d.com/6000.4/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html) · [Unity 6 UI Toolkit updates (Unity blog)](https://unity.com/blog/unity-6-ui-toolkit-updates).
- Flagged **[verify]**: world-space UI Toolkit minor-version availability in the project's installed Unity 6.x; current wet-tile duration (Rev 2 carryover).

---

## Next action anchor

**Approve or amend the shared-core spine (§7.2 stages 0–4), then start Stage 0: the Input Router.** It's scenario-neutral, unblocks everything else, and its done-when is a grep. The §5/§6 scenario layers stay parked until the already-planned §4.3 experiment afternoon answers A-vs-B.
