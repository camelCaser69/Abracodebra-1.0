# Roadmap — Abracodabra

Living doc, edited in place (history: `git log -p`). Candidates for the next blocks, in rough priority order. Deferred items from task packs land here. The old code-optimization backlog is merged in §3; the original is `99_Archive/2026-07_Cowork_Era/Code_Optimization_Backlog.md`.

"Verify" = written in July 2026 or earlier; check against live code before acting.

---

## 1. Next candidates

| # | Item | Why / source | Notes |
| --- | --- | --- | --- |
| 1 | **asmdef split + golden-run harness** | Audit C3 (`04_Reviews/2026-07_Project_Audit_Gaps_And_Opportunities.md`): determinism (`RunSeed` + `IDeterministicRandom`) is built but nothing tests it. There are no asmdefs of our own and no tests, so EditMode tests cannot reference gameplay code | Candidate for Block 1 or 2. Do it **before** F1 (buffer migration). Pack 2026-10 §8 |
| 2 | **Ledger A/B experiment** (D1, D5) | `03_Tasks/Active/2026-07_Fable5_Design_Decision_Ledger.md`; `02_Design/Concepts/Phase_Identity_Final_Refinement_2026-07-20.md` §2.4 / §4: build A's proof of concept first, B-rev only if A fails the density test | Needs: Planning-tick invariant, sun-arc HUD, Mark & Go v0, ripeness window |
| 3 | **Planning-tick invariant is not enforced in code** | Ledger / Rev 2: Planning ticks must advance only the action economy. `TickManager` treats Planning as action-driven (lines 29–35), but nothing gates `PlantGrowth` / `PlantSequenceExecutor` on `RunState` | Verify before touching `TickManager` / `PlantGrowth`. Non-negotiable for the A proof of concept |
| 4 | **F1–F4 packs** (DNA-strand buffer migration, inventory inversion, run-loop screens, hygiene sweep) | `03_Tasks/Active/2026-07_Pack_Implementation_Guides.md` (G1–G4). All unapplied: `RuntimeSequenceSlot` is still live, no `PlayerInventory` / `SequenceParser`, no run-loop screens | Symbols were verified 2026-07-07: re-verify. The ranking rationale is in `99_Archive/2026-07_Cowork_Era/2026-07_Fable5_Last_Day_Plan.md` |
| 5 | **Testing sandbox / cheat console** | `03_Tasks/Active/2026-07_Testing_Sandbox.md`: undecided proposal. Overlaps item 1 | Milan decides. Its "already exists" table is stale (`ExecutionPhaseDriver` is wired since 2026-07-06) |
| 6 | **A-pack Part-4 checks never run** | `03_Tasks/Done/Abracodabra_A_Category_Implementation.md` | Verify in play: A1 no-double-advance · A1 Planning unchanged · A2 tile frees · A2 no zombie ticks · A3 round ends on timer · A4 energy survives recalc · A5 clean cold start · A6 reproducibility (same seed, same actions) |
| 7 | **SEGUIEMJ → Noto Color Emoji** | `SEGUIEMJ.TTF` is Windows' Segoe UI Emoji and cannot be redistributed. `StatusEffect_Icon.prefab` uses it (`UnicodeText` child); Noto (OFL) is already the TMP emoji fallback | **Before any public build or Steam page** |
| 8 | ~~Unity 6.3 upgrade~~ **done 2026-10-05** (Phase E of the 2026-10 pack) | `04_Reviews/2026-10_Unity63_Upgrade.md` | Branch `upgrade/unity-6.3`; tag `pre-unity-6.3` |
| 9 | **Verify plant growth and Doris feeding in play** (open after the 6.3 smoke test) | In the 2026-10-05 smoke test on 6.3 the plant did not visibly grow and Doris's popup had no food to give. Not shown to be a 6.3 regression: no console error; the plant reached `Growing` (`baseGrowthChance` = 1); it was planted *after* START DAY; the plant action fired twice (`PlayerActionManager` "Executing PlantSeed" twice, the second hit "already occupied"); the popup (`FeedingSystem` / `FoodSelectionPopup`) is click-to-select and lists only consumables in the inventory, none exist before a harvest | Repro: plant in Planning, START DAY, wait 20 s, read the plant's `PlantGrowth` state through the CLI. If it still does not grow, run `pre-unity-6.3` on 6.0 to see whether it is a regression. Overlaps item 6 (A-pack Part-4 checks) |

Audit shortlist beyond the above (`04_Reviews/2026-07_Project_Audit_Gaps_And_Opportunities.md` §5): H1 lose condition → C2 static teardown (before F2/F3 add statics) → H4 scripted first round → H3c round-boundary save; cheap wins W1 (daily seed) and W5 (assist settings).

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

**Next action:** pick Block 1 from §1 (rows 1 and 3 are the foundation for rows 2 and 4).
