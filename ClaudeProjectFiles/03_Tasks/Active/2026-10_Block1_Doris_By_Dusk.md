# Block 1 — Doris by Dusk (task pack, 2026-10)

Status: **ready for the code lane** · written 2026-10-26 (design lane, Opus · high) · accepted by Milan as "recs" to `02_Design/Restart_Direction_2026-10-05.md` §5 (Q1–Q9).
Design of record for this block: Restart_Direction §3.2 option 2 and this pack. Replaces Phase Identity §2.4 as the first build.

**★ The one question this block answers** (Bitbloom's 2026-10-18 process rule: one clear question per test):
*With a goal, a way to lose and a hands-off Day, does watching my garden hold out against Doris's hunger feel like my plan being tested, and do I want another run?*

---

## 0. Before batch 0 (Milan, ~10 min)

- [ ] In GitHub Desktop: commit or discard the three files Unity re-saved on `upgrade/unity-6.3` (`Assets/Settings/UniversalRP.asset`, `ProjectSettings/GraphicsSettings.asset`, `Assets/Editor/TileMappingsBackup/TileMappingsBackup.json`: look at the diff first; the JSON is referenced by `TileInteractionManager`, keep it if unsure), then commit the design-lane docs (Restart_Direction, this pack, and the edits to projectmemory, Feedback_Log, Ledger, Phase Identity, Roadmap).
- [ ] Merge `upgrade/unity-6.3` into `main`, push (still open from 2026-10-05).
- [ ] Create branch `block1/doris-by-dusk` from `main`. Every batch commits there, one commit per step.
- [ ] Open the Unity Editor on Abracodabra only (one Editor = no `AMBIGUOUS_EDITOR`).

## 1. The rules being built (starting numbers; all in one ScriptableObject)

All tunables live in a new `DuskRules` ScriptableObject (`Assets/Scriptable Objects/Run/DuskRules.asset`), never hard-coded (CLAUDE.md §1.9).

| Rule | Starting value | Built in |
| --- | --- | --- |
| **Planning** is free and frozen: till, plant, water, harvest, edit genes, walk. No tick budget (D5 parked). Plants, energy and genes do not change | — | B1, B2 |
| **Day** is hands off: Space pause, Tab speed, hover. No avatar input, no tile clicks (Shoo is the one exception, B8) | — | B2 |
| **Doris's belly** = `round((Max − hunger) / Max × pips)`, a view over the existing `DorisHungerSystem` hunger (higher hunger = emptier belly) | 8 pips | B3 |
| Belly drains during the Day only (existing G&T gate in `DorisHungerSystem.OnTickUpdate`), faster each round | hunger rate × (1 + 0.15 × (round − 1)) | B3 |
| **Supper at dusk**: when the Day ends, every fruit on every living plant goes to Doris (`PlantGrowth.HarvestAllFruits()` → `DorisController.FeedDoris(nutrition)`), one at a time, visibly | 0.3 s per fruit | B3 |
| Starving Doris eats a plant (existing `TryEatNearbyPlant`) at most **once per Day** (mercy cap) | 1 / Day | B3 |
| **Lose** (checked after supper): belly 0 → *"Doris went to bed hungry."* · no living plant → *"The garden is gone."* | — | B3 |
| **Win**: dusk of round 5 survived → *"Doris sleeps. You made it through the season."* | 5 rounds | B3 |
| Player starvation is off (D4): `RunManager.playerDeathEnabled = false` | — | B3 |
| **Waves** are shown on the map during Planning: where, what, how many, when | rounds 1–5 | B4 |
| Round 1 is small: ≤ 3 pests; never two spawn groups starting within 60 s (rule 35 / Trap_v2's tuning rule) | — | B4 |
| **Auto-pause** (D6): first wave spawn of the run, every plant death, Doris's first plant-eat | on | B5 |
| **Dusk ledger**: three dry lines, then Continue | — | B6 |

Belly pips are filled vs hollow squares (shape and brightness, never hue alone: rule 41).

---

## 2. Batches

Cut from the bottom (§3). Each batch: one fresh session, `/clear` before, commit after each step. Code-lane checks per CLAUDE.md §7: `recompile` → `recompile_status` → `console --level error` before and after.

### B0 · Verify growth and feeding, fix the double plant action
**Sonnet · high · Est. 45–60 min · Est. usage: low**

- Roadmap #9. Read `PlayerActionManager.cs` (`ExecutePlayerAction`, `ExecutePlantSeedWithMinigame`), `PlayerTileInteractor.cs` (`HandleLeftClick`, `LateUpdate`'s pending-click flags), `PlantGrowth.cs` (`OnTickUpdate` at ~219, the Growing → Mature path).
- Find why "Executing PlantSeed" logs twice (likely the pending-click flag consumed twice, or the minigame callback plus the immediate path). Fix at the source.
- Repro growth: plant, START DAY, wait 20 s, read the plant's `CurrentState` through the CLI (ask Milan before any `eval`). If it does not grow, compare on tag `pre-unity-6.3` before changing anything.
- **Done when:** one click plants once (one log line); a planted seed reaches `Mature` and grows visible cells during a Day; harvested fruit can be fed to Doris through the existing popup.
- **How to check:** `SampleScene` → Play → plant one seed (today still in Growth & Threat; B2 moves it) → console shows one "Executing PlantSeed" → watch it grow → harvest → click Doris → feed. Note the times in projectmemory.

### B1 · The Planning-tick invariant, with an on-screen check
**opusplan · high · Est. 60–90 min · Est. usage: medium**

- Gate every plant-simulation tick path on `RunManager.Instance.CurrentState == RunState.GrowthAndThreat`, uniformly (Commit & Watch §4.1): `PlantGrowth.OnTickUpdate` (covers `EnergySystem` and `PlantSequenceExecutor` called from it, ~253–260), plus every other `ITickUpdateable` that changes plants or world effects (grep `RegisterTickUpdateable`; check `Genes/WorldEffects/*`, status effects on plants, scent/fruit timers). Fauna and Doris are already gated: confirm, don't duplicate.
- Same early-out pattern as `DorisHungerSystem.OnTickUpdate` (line ~93). `PlantGrowth.cs` is 884 lines: method-level patch with anchors.
- Dev readout (temporary, clearly marked; F9 toggles; docked in a corner outside the play area, fixed size: Bitbloom rule 47): tick count, phase, and a plant checksum (living plants · total cells · total energy · fruit count).
- **Done when:** walking and acting for 40+ ticks in Planning leaves the checksum unchanged; in the Day it changes.
- **How to check:** Play → F9 → note the checksum → walk around in Planning ~40 steps → same checksum → START DAY → it moves within a few seconds.

### B2 · Planting moves into Planning; hands off the Day
**Sonnet · high · Est. 60 min · Est. usage: medium**

- `PlayerTileInteractor.Update` (line ~33): accept clicks in **Planning**, ignore them in Growth & Threat (flip the gate). Keep the `FoodSelectionPopup.IsBlockingInput` guard.
- `GardenerController.Update` (line ~111): call `HandlePlayerInput()` in **Planning** only (time is frozen, so this is not piloting under a running clock: Abracodabra rule 1). In the Day she stands still (idle animation).
- Check `PlayerActionManager` paths for any G&T-only assumptions (tick costs call `RequestActionTicks`, which only advances in Planning: fine with B1). The planting minigame runs in frozen time: keep it.
- Do not touch the 36 legacy `Input.*` sites beyond these two gates.
- **Done when:** till, plant, water and harvest work in Planning; nothing responds to clicks or WASD during the Day except Space / Tab / hover; START DAY still works.
- **How to check:** Play → plant 3 seeds and water them in Planning → START DAY → click tiles and press WASD: nothing happens; Space pauses, Tab speeds up.

### B3 · Doris's belly, supper at dusk, a run that can be won and lost ⚑ the hardest step
**opusplan · xhigh · Est. 90–120 min · Est. usage: high**

- New `DuskRules` ScriptableObject (§1 numbers) and one assigned asset.
- **Belly readout** above Doris: 8 pips (filled / hollow), world-space or a UI Toolkit element anchored to her, from `DorisHungerSystem.OnHungerChanged`. Hover (extend `DorisController.OnHoverEnter`): *"Belly 5 / 8 · empties during the Day · supper at dusk: every fruit on a living plant · at 0 she eats a plant (once a Day)"* (rule 42: one name, two places max).
- **Round ramp:** hunger rate multiplier per round from `DuskRules` (read where `DorisHungerSystem` applies its per-tick increase).
- **Mercy cap:** `TryEatNearbyPlant` at most once per Day (reset on `OnRunStateChanged` → GrowthAndThreat).
- **Supper:** hook the Day's end where `WaveManager` (line ~224) calls `RunManager.StartNewPlanningPhase()`. Insert a `SupperRunner` step before the state change: for each living `PlantGrowth`, `HarvestAllFruits()`; for each item, a short arc to Doris then `FeedDoris(nutrition)`; counts kept for the ledger (B6). Runs on frames while ticks are stopped; deterministic order (plants sorted by grid position).
- **Lose / win:** after supper, apply §1's checks; `RunManager` → `RunState.GameOver` with a reason, or a Victory flag after round 5. Set `playerDeathEnabled = false`. A code-built end overlay (UI Toolkit, like guide G3's overlays): title, reason, *Restart (same seed)* and *New seed* buttons → `RunManager.RestartGame()`.
- Add `Wave_003.asset` and `Wave_004.asset` (only 3 waves exist) so rounds 4–5 have waves; content tuning is B4.
- **Done when:** a run reaches a win at dusk 5 or a loss with the right reason; supper visibly moves every fruit to Doris and the pips rise by the fed amount; Doris eats at most one plant a Day.
- **How to check:** Play → round 1: plant food plants → START DAY → at dusk watch fruit go to Doris, pips rise → to force a loss, plant nothing in round 2 → dusk shows "The garden is gone." → Restart (same seed) works.
- **Checkpoint A (Milan, 10 min):** play one run to its end. One line in `Feedback_Log.md`: could I tell why I won or lost?

### B4 · Waves on the map before the Day
**Sonnet · high · Est. 60–90 min · Est. usage: medium**

- Today all three waves spawn `Offscreen` (`spawnLocationType: 2`); `RandomNearPlayer` makes no sense with a still gardener. Give each `WaveSpawnEntry` a fixed, visible entry (an edge point or a named spawn marker in the scene); keep the spawn deterministic through `IDeterministicRandom`.
- In Planning, for the coming round (`WaveManager` uses index `round − 1`): at each entry a marker with the species icon, "×3" and the time ("0:20" = `delayAfterSpawnTime` in Day time at ×1). Hover: species name, what it eats, how fast it bites (rule 39: doing / eats / turns after).
- Tune the 5 waves to §1: round 1 ≤ 3 pests, then growing; never two groups starting within 60 s.
- **Done when:** before every START DAY, each group of the coming wave shows where and when it will appear, and it appears there.
- **How to check:** Play → read the markers → START DAY → pests enter at the marked spots near the marked times.

### B5 · Readable bites and the auto-pauses
**Sonnet · high · Est. 60 min · Est. usage: medium**

- In `PlantGrowth.HandleBeingEaten(AnimalController eater, PlantCell eatenCell)` (~680): `FloatingCombatText.Spawn` *"−1 leaf · {species}"* at the cell, and count bites per species for the ledger (a small `DayTally` service, reset at dawn).
- Where a gene payload kills or repels a pest and its source is known (poison, thorns, Reactive Burst: `RactiveBurstHandler.cs` ~165 already spawns text), name the gene; count kills per species. Don't build a damage-source system: unknown sources stay unnamed.
- D6 auto-pause via `ExecutionPhaseDriver.SetPaused(true)` plus a one-line banner reason: first wave spawn of the run, any `OnPlantDied`, Doris's first plant-eat. Flags in `DuskRules` (default on).
- Dev readout (B1) gains: *events this Day: N in T s* (bites + kills + plant deaths + Doris eats + fruit set) — the density test's number.
- **Done when:** every bite shows who did it; the three pauses fire once each with their reason; the readout counts events.
- **How to check:** Play → START DAY → first wave: pause with "First wave: 3 slugs from the east" → resume → bites show "−1 leaf · slug" → let a plant die → pause.

### B6 · Dusk ledger and phase banner
**Sonnet · high · Est. 60–90 min · Est. usage: medium**

- After supper, before the end check result is shown: a code-built overlay with three lines from `DayTally` + supper counts, then Continue:
  *"Dusk 2. Doris ate 4 fruit (belly 6 / 8). The pests ate 9 leaves: slugs 6, rabbits 3. You lost 1 plant: Sunfern, to rabbits."* Numbers must agree with what was shown (rule 17: total first, then parts).
- Phase banner (top edge, never over the ground: rule 5) with a "Now:" line: **PLANNING** "Plant, water, edit seeds. The wave is on the map." [START DAY] · **DAY** "Hands off. Space pause · Tab speed. Supper at dusk." · **DUSK** "Supper." Reuse `GameUIManager`'s phase UI if it has one (875 lines: patch).
- **Done when:** every dusk shows the three lines with agreeing numbers; the banner always says what to do now.
- **How to check:** play two Days; compare ledger vs pips vs what you saw.

### B7 · "Reads as" under the strand *(cuttable)*
**Sonnet · high · Est. 45 min · Est. usage: low**

- In `UISpecSheetController`: one plain sentence built from the current strand, e.g. *"Each cycle: a cheap poison cloud, then a nutritious fruit. Skips the fruit below 3 energy."* (Bitbloom 2026-10-05: the Expedition's "reads as" line was the clearest UI of all toys.)
- **Done when / How to check:** open the Seed Editor, reorder two genes: the sentence changes to match.

### B8 · Shoo, the one Day lever *(cuttable)*
**Sonnet · high · Est. 45–60 min · Est. usage: low–medium**

- During the Day only: click a pest → it flees toward its entry for 2 s; 10 s cooldown shown as a draining ring on the cursor. Uses the existing flee/movement in `AnimalController` (verify); `DuskRules` holds the numbers. Counts in the ledger ("You shooed 2").
- **Done when / How to check:** START DAY → click a slug → it turns away; a second click within 10 s does nothing and the ring shows why.

### B9 · Close-out and playtest prep
**Sonnet · high · Est. 30 min · Est. usage: low**

- projectmemory: Snapshot + Recent entries + Code facts (disk-verified); `Codebase_Map.md` for `DuskRules`, `SupperRunner`, `DayTally`, the overlays; Roadmap for anything cut; copy §4's sheet into the session notes. Remove nothing from the dev readout: it is part of the playtest.
- **Done when:** a fresh session can read projectmemory and know what Block 1 built and what was cut.

---

## 3. Cut list (cut from the top of this list first)

1. B8 Shoo → Block 2 (the playtest then answers "did I miss a lever?" by itself).
2. B7 "reads as" sentence → Block 2.
3. B4's hover detail (keep the markers).
4. B3's fruit arc animation (fruit just vanishes and the pips tick up, one at a time).
5. B5's gene names on kills (keep bite text and the pauses).
6. B6 ledger shrinks to one line: *"Doris ate N fruit. Pests ate M leaves. Lost K plants."*

**Never cut:** B0, B1, B2, B3's win / lose, B4's markers. Without them the ★ question can't be answered.

**Out of scope (to Roadmap, unchanged):** Mark & Go, ripeness windows and the U1 tier split, trigger genes (D8 is reopened, decided after this playtest), Doris Bowl + Cravings, a tick budget, the asmdef split + golden-run harness (Block 2, before F1), F1–F4, the Input System migration, save / resume, audio, a 3-seed starting pick.

---

## 4. Playtest sheet (after B6, or B8 if built; ≤ 8 items, in play order)

New this time: you plant in Planning; the Day is hands off; Doris eats every fruit at dusk; she must not go to bed hungry, for 5 rounds.

1. **Do:** before planting, hover Doris and each wave marker. **Look for:** belly pips + hover; a marker per group with species, count, time. **Answer:** can I say what comes, from where, and what Doris needs tonight? yes / no
2. **Do:** plant 3–4 seeds, water, edit one strand; walk around for a while; F9. **Look for:** the checksum doesn't move in Planning. **Answer:** did anything grow or change that shouldn't? yes / no
3. **Do:** START DAY; hands off; Space once, Tab once. **Look for:** the banner's "Now:" line; pause works at any moment. **Answer:** did I feel I *had* to do something with my hands? yes / no
4. **Do:** watch the first wave reach your plants. **Look for:** the auto-pause and its reason; "−1 leaf · slug" at each bite. **Answer:** could I tell who ate what without hovering? yes / no
5. **Do:** watch a defence gene answer a bite. **Look for:** the pest leaving or dying; the name of what did it. **Answer:** did a choice I made in Planning visibly pay off? yes / no
6. **Do:** at dusk, watch supper, then read the ledger. **Look for:** each fruit going to Doris; pips rising; three lines. **Answer:** did fruit, pips and ledger agree? yes / no
7. **Do:** play on to a win or a loss. **Look for:** the reason on the end screen. **Answer:** was the loss (or the close call) my plan's fault, and did I know which part? yes / no
8. **Do:** a second run on the same seed (Restart same seed). **Answer:** another run? yes / maybe / no · Clarity · Agency · Tension · Fun (1–5) · F9's "events this Day" on Day 2: ___ in ___ s

**If it comes up:** Doris eats a plant (she should, at most once a Day, and the ledger names it) · a plant dies (auto-pause with a reason) · Shoo (B8): did you use it, and did it matter?

**Density test** (replaces Ledger D1's metric, decided 2026-10-26): passes if item 3 is "no", items 4 and 5 are "yes", Day 2's events are ≥ 1 per 10–15 s with ≤ 1 group starting per minute, and item 8 is "yes" or "maybe". Fails → levers in order: shorter Day → denser telegraphed waves → Shoo → then Block 2 picks between Mark & Go and trigger genes.

---

## Things to consider

- **Bitbloom has moved on** (since 2026-10-17 it is a spider in a dark cave; it did not adopt Trap_v2's "Abracodabra in the pixel sim"). The convergence risk in Restart_Direction is gone for now: Abracodabra is again the only plants-and-Doris game. Its newer rules still apply here: **density first** (45: a sparse garden reads as empty; keep the playable area small) and **every danger shows a way past it** (46: the wave markers must say what beats each pest — B4's hover is the place, so think twice before cutting it).
- **Supper makes the belly a forecast.** During the Day the player can count fruit and know whether supper will do. If that makes the Day *more* boring ("already decided"), the fix is threats to the fruit during the Day (pests that prefer fruit), not hands.
- **Harvesting in Planning** puts fruit in the inventory instead of at supper; hand-feeding Doris from the popup in Planning still works. That is a deliberate escape hatch, not a chore: if Milan does it every round, supper's rule needs a look.
- **The invariant has no test** until the harness (Block 2). B1's checksum is the stand-in; keep it on through the playtest.
- **`PlantGrowth.cs` (884) and `GameUIManager.cs` (875)** get method-level patches only (CLAUDE.md §2.3).
- **Determinism:** supper order, spawn points and the mercy cap must not use raw `UnityEngine.Random`; "Restart same seed" should replay the same waves.
- **Usage:** B3 is the only xhigh step; everything else is Sonnet · high or opusplan · high. One batch per session.
- **If B0 shows plants don't grow on 6.3** (a regression), stop the block and fix that first: nothing else is testable.

---

**Progress:** B0 done 2026-10-26 (unattended; see projectmemory). B1 done 2026-10-26 (unattended; invariant enforced, F9 readout; see projectmemory). B2 done 2026-10-26 (unattended; planting, walking and tool refill in Planning, Day hands off; see projectmemory). **Next: B3.**

**Next action (original):** Milan does §0 (commit, merge, push, branch), then pastes into a fresh Claude Code session in the repo: *"Sonnet · high · Est. 45–60 min · Est. usage: low — Read CLAUDE.md, projectmemory, then `03_Tasks/Active/2026-10_Block1_Doris_By_Dusk.md` §1 and batch B0 only. Do B0, commit per step, update projectmemory, stop."*
