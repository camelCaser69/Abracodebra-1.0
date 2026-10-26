# Roadmap — Abracodabra

Living doc, edited in place (history: `git log -p`). Candidates for the next blocks, in rough priority order. Deferred items from task packs land here. The old code-optimization backlog is merged in §3; the original is `99_Archive/2026-07_Cowork_Era/Code_Optimization_Backlog.md`.

"Verify" = written in July 2026 or earlier; check against live code before acting.

---

## 1. Next candidates

| # | Item | Why / source | Notes |
| --- | --- | --- | --- |
| 1 | **asmdef split + golden-run harness** | Audit C3 (`04_Reviews/2026-07_Project_Audit_Gaps_And_Opportunities.md`): determinism (`RunSeed` + `IDeterministicRandom`) is built but nothing tests it. There are no asmdefs of our own and no tests, so EditMode tests cannot reference gameplay code | **Block 2** (moved out of Block 1 on 2026-10-26). Do it **before** F1 (buffer migration). Pack 2026-10 §8 |
| 2 | ~~Ledger A/B experiment~~ → **Block 1 "Doris by Dusk"** (built 2026-10-26, B0-B7 + P; playtest pending; `03_Tasks/Active/2026-10_Block1_Doris_By_Dusk.md`) | `03_Tasks/Active/2026-07_Fable5_Design_Decision_Ledger.md`; `02_Design/Concepts/Phase_Identity_Final_Refinement_2026-07-20.md` §2.4 / §4: build A's proof of concept first, B-rev only if A fails the density test | Needs: Planning-tick invariant, sun-arc HUD, Mark & Go v0, ripeness window |
| 3 | **Planning-tick invariant is not enforced in code** | Ledger / Rev 2: Planning ticks must advance only the action economy. `TickManager` treats Planning as action-driven (lines 29–35), but nothing gates `PlantGrowth` / `PlantSequenceExecutor` on `RunState` | **Done in Block 1 B1 (2026-10-26)**; no automated test until the harness (row 1) |
| 4 | **F1–F4 packs** (DNA-strand buffer migration, inventory inversion, run-loop screens, hygiene sweep) | `03_Tasks/Active/2026-07_Pack_Implementation_Guides.md` (G1–G4). All unapplied: `RuntimeSequenceSlot` is still live, no `PlayerInventory` / `SequenceParser`, no run-loop screens | Symbols were verified 2026-07-07: re-verify. The ranking rationale is in `99_Archive/2026-07_Cowork_Era/2026-07_Fable5_Last_Day_Plan.md` |
| 5 | **Testing sandbox / cheat console** | `03_Tasks/Active/2026-07_Testing_Sandbox.md`: undecided proposal. Overlaps item 1 | Milan decides. Its "already exists" table is stale (`ExecutionPhaseDriver` is wired since 2026-07-06) |
| 6 | **A-pack Part-4 checks never run** | `03_Tasks/Done/Abracodabra_A_Category_Implementation.md` | Verify in play: A1 no-double-advance · A1 Planning unchanged · A2 tile frees · A2 no zombie ticks · A3 round ends on timer · A4 energy survives recalc · A5 clean cold start · A6 reproducibility (same seed, same actions) |
| 7 | **SEGUIEMJ → Noto Color Emoji** | `SEGUIEMJ.TTF` is Windows' Segoe UI Emoji and cannot be redistributed. `StatusEffect_Icon.prefab` uses it (`UnicodeText` child); Noto (OFL) is already the TMP emoji fallback | **Before any public build or Steam page** |
| 8 | ~~Unity 6.3 upgrade~~ **done 2026-10-05** (Phase E of the 2026-10 pack) | `04_Reviews/2026-10_Unity63_Upgrade.md` | Branch `upgrade/unity-6.3`; tag `pre-unity-6.3` |
| 9 | **Verify plant growth and Doris feeding in play** (open after the 6.3 smoke test) **— closed 2026-10-26 by Block 1 B0: plants grow to Mature on 6.3 (the Day starts paused: press Space), feeding works, double plant fixed; see projectmemory** | In the 2026-10-05 smoke test on 6.3 the plant did not visibly grow and Doris's popup had no food to give. Not shown to be a 6.3 regression: no console error; the plant reached `Growing` (`baseGrowthChance` = 1); it was planted *after* START DAY; the plant action fired twice (`PlayerActionManager` "Executing PlantSeed" twice, the second hit "already occupied"); the popup (`FeedingSystem` / `FoodSelectionPopup`) is click-to-select and lists only consumables in the inventory, none exist before a harvest | **Block 1 B0.** Repro: plant in Planning, START DAY, wait 20 s, read the plant's `PlantGrowth` state through the CLI. If it still does not grow, run `pre-unity-6.3` on 6.0 to see whether it is a regression. Overlaps item 6 (A-pack Part-4 checks) |

Audit shortlist beyond the above (`04_Reviews/2026-07_Project_Audit_Gaps_And_Opportunities.md` §5): H1 lose condition → C2 static teardown (before F2/F3 add statics) → H4 scripted first round → H3c round-boundary save; cheap wins W1 (daily seed) and W5 (assist settings).

| 10 | **Block 2 candidates (decided by Block 1's playtest)** | `02_Design/Restart_Direction_2026-10-05.md` §3.1; pack §3 | Mark & Go v0 (Phase Identity §2.1) · D8: ripeness windows + U1 tier split **vs** trigger genes (On Bitten, At Dusk) · Doris Bowl + Cravings (D3) · Shoo (4.1; "reads as" was built in B7) · a 3-seed starting pick (Bitbloom rule 38) · tick budget (D5 verbs-only) only if Planning shows "do everything every morning" |

---

## 2. Parked

- Git LFS: not now (repo ≈ 140 MB). Revisit when audio or large art arrives.
- Adapt Bitbloom's `Tools/UnityCompileCheck/compile_check.py` to this project's single `Assembly-CSharp.csproj` (for when the Editor stops answering).

---

## 3. Code-optimization backlog (merged from the old Roadmaps/ file)

| # | Item | Status |
| --- | --- | --- |
| 1 | Remove unused combat scripts (`UI/Combat/StatusEffect.cs`, `BurningStatusEffect.cs`, `SpellProjectile.cs`) | Looks done: the `UI/Combat` files are gone (`StatusEffect.cs` now lives in `Ecosystem/Status Effects/` as a different class). Verify |
| 2 | Remove unused UI scripts (`NodeSelectable.cs`, `DeselectOnClickOutside.cs`) | Looks done: neither file exists. Verify |
| 3 | Shared `SpeedModifiable` base for `AnimalController` / `GardenerController` | `SpeedModifiable.cs` does not exist; the speed code may have changed. Verify before acting |
| 4 | `if (Debug.isDebugBuild)` around `Debug.Log` (PlantGrowth, FaunaManager, AnimalController) | Verify. A project log flag may cover it |
| 5 | Cache component references (`ShadowPartController`, `OutlinePartController`, `WaterReflection`) | Files exist: verify before acting |
| 6 | Remove empty `Awake` / `Start` methods and unused handler stubs | Verify |
| 7 | Simplify `NodeData` serialization | `NodeData.cs` does not exist any more (the gene model moved to `Abracodabra.Genes`). Stale: drop |
| 8 | Base class for visual synchronisation (Shadow / Outline / WaterReflection) | Optional. Verify |
| 9 | `FloraManager`: replace `FindObjectsByType` in `Update` with a cached plant list | `FloraManager.cs` exists; 13 `FindObjectsByType` calls in `Assets/`. Verify |
| 10 | Simplify the `WaterReflection` override system | Optional. Verify |
| 11 | Constants for magic numbers (epsilon, animation state names, layer ids) | Optional |
| 12 | Consolidate tooltip logic (`BuildNodeDetails` / `BuildToolDetails`) | Likely stale: the UI is now UI Toolkit. Verify |
| 13 | Object pooling (fireflies, animal spawns, thought bubbles) | Hard; only if profiling asks. Also in the 2025 WeGo rework 5 (Priority 7) |

After any of these: compile, enter Play, check plant growth, the gene editor, animal spawns, inventory, shadows/outlines, day/night and tile interactions.

---

## 4. Cut or deferred from Block 1 "Doris by Dusk" (B9, 2026-10-26)

Source: the pack's cut list and the Needs Milan sections (`03_Tasks/Active/2026-10_Block1_Needs_Milan.md`). Playtest-dependent items are marked; the rest waits for a block that has room.

| # | Item | Why / source | Notes |
| --- | --- | --- | --- |
| 4.1 | **B8 Shoo** (one Day click on a pest, 10-s cooldown ring, counted in the ledger) | Pack B8, cut list #1 | **Not built; the playtest decides** (density-test lever order: shorter Day, denser telegraphed waves, Shoo). Block 2 candidate (row 10) |
| 4.2 | `counterHint` on `AnimalDefinition` | Wave marker hover names only the Fear flag (rule 46: every danger shows a way past it); poison, thorns, Reactive Burst are not on the definition | Needs Milan B4. Small data + hover line |
| 4.3 | Definitions for the deer, fox, bird (second species); a wave with a second group | Deer prefab has no `AnimalDefinition` and is out of every wave; one group per Day was forced by the 60-s rule inside a 50-s Day | Needs Milan B0, B4. Design first (what each pest eats, what beats it) |
| 4.4 | Animal walk-off at dusk | Pests vanish at the wave's last tick | Needs Milan B4. Cosmetic |
| 4.5 | Proper seed names | The ledger and pause text show "Starter Seed Template" for every plant | Needs Milan B6. Comes with a 3-seed pick or gene-built names |
| 4.6 | Real hover tooltips for new terms (Dusk ledger, Reads as, belly) | CLAUDE.md §5; UI Toolkit tooltips do not show at runtime, so hovers are hand-built per control | Needs Milan B6, B7. One reusable hover helper |
| 4.7 | **Buffer model replaces `RuntimeSequenceSlot` in Reads as** | `BuildReadsAs` reads the slot list; no `SequenceParser` | Part of F1 (row 4). Only `UISpecSheetController.BuildReadsAs` needs replacing |
| 4.8 | Legacy `Input.*` sites (F9, G added in Block 1) | Input System package installed, `activeInputHandler` = both | The Input System migration (out of scope for Block 1) |
| 4.9 | projectmemory trim cadence | Block 1 grew it from 18 KB to 44 KB in a day | Trim at every block close and past ~35 KB; entry template: Built / Measured (archive the numbers) / Not proven / Line deltas |
| 4.10 | Pause when a pest **enters the view**, not when it spawns (D6 first wave) | The pause fires at spawn, just off-screen | Needs Milan B5. Per-animal view-entry check |
| 4.11 | Pause only on the **first** plant death of a run (or fewer pauses) | Every death pauses; a bad Day pauses 4 times. Toggle exists: `autoPausePlantDeath` | Playtest-dependent (D6 vs the pack's "every") |
| 4.12 | Count fruit per plant-cycle in the events number | Fruit set dominates "events this Day" (15 fruit vs 6 bites) | Playtest-dependent: the density test reads the breakdown line until then |
| 4.13 | Lock camera zoom in the Day / freeze the wave entry at START DAY | The entry point comes from the live camera view, so zooming between marker and Day moves it | Needs Milan B4 |
| 4.14 | Spec sheet "Reads as" refreshes on each edit | It refreshes on hover or click only | Needs Milan B7 |
| 4.15 | Ledger layout calls: skip the ledger at the last dusk (end card has the numbers); thinner or semi-transparent banner; one START DAY button per view; use the empty left half of the bottom bar | Taste calls after the first playtest | Needs Milan B6. `DuskRules.ledgerWaitsForContinue` removes the wait everywhere |
| 4.16 | Fear gene scare text and a live Fear gene in a run | Compiled, never run (no live Fear gene in a scripted run) | Check in the playtest or the harness |
| 4.17 | Remove the F9 dev readout | Temporary; it is part of the playtest | After the playtest: delete `UIDevReadoutController.cs` or keep behind a dev flag. Its checksum stands in for the missing invariant test until the harness (row 1) |
| 4.18 | Doris's bite counted as "plants lost" in F9 | Doris destroys the plant directly; F9 "lost" excludes it, the ledger includes it | Needs Milan B5, B6. Unify when the death pipeline is touched |
| 4.19 | Scene hygiene: stale `ExecutionPhaseDriver.currentSpeedIndex` / `RunManager.currentState` in `SampleScene`, `runInBackground` in `ProjectSettings.asset` | Step P side finding | Fix recipe in Needs Milan B9; commit the scene alone |
| 4.20 | Merge `upgrade/unity-6.3` and `block1/doris-by-dusk` into `main`, push | Never pushed; the CLI push failed with an authentication error | Milan, GitHub Desktop |

**Next action:** Milan plays Block 1's Checkpoint A (playtest sheet in `03_Tasks/Active/2026-10_Block1_Needs_Milan.md`, B9 section) and decides B8 Shoo (4.1) and the row-10 candidate. Then Block 2 = row 1 (asmdef + harness) + that candidate.
