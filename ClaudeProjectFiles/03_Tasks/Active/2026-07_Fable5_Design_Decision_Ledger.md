# Fable 5 Design Decision Ledger (2026-07-20)

**What this is.** Final-session Fable 5 output. Every open design decision across the
concept docs, with a **final recommendation**, the reasoning, and **pre-computed branch
consequences** so Milan + weaker models can execute without re-deriving judgment.
Where a decision was previously "pending," this ledger is the tie-breaker of record.
Overrule freely — but write the overrule here, don't leave it implicit.

Sources reconciled: `Commit_And_Watch_Loop_Design_2026-07-06.md` (Rev 2), `UI_Systems_Research_2026-07-08.md`,
`Minigames_And_Mechanics_Reevaluation_2026-07-08.md`, `Gameplay_Engagement_Research_2026-07-05.md`,
`99_Archive/2026-07_Cowork_Era/2026-07_Fable5_Last_Day_Plan.md` (archived 2026-10), `2026-07_Pack_Implementation_Guides.md`.

Status: ✅ decided here · 🧪 experiment decides, default recorded · ⏸️ genuinely Milan's call

---

## D1 · §4.3 Phase identity — Commit & Watch (A) vs Live Garden (B) 🧪

**Call: A (Commit & Watch), with a falsifiable exit condition.** Milan's gut pick, the
Rev 2 doc, the fixture model, and the ripeness-window system all already lean A; B would
strand ~3 documents of design work and re-open Doris pacing.

**The experiment should test exactly one thing:** day under-stimulation (named risk #1).
Don't evaluate "which feels better" holistically — evaluate: *with N2 ripeness windows +
wave events + lean-in prompts active, does a meaningful decision or reaction land at
least once per 15–20 s of Day phase?*

- **Pass → A is final.** Proceed per D5–D8 A-branches. UI scenario layer [A] from
  UI_Systems_Research_2026-07-08 (budget meter, Report cause-rows, fixture UI).
- **Fail even after density levers (shorter day, denser waves, tighter windows) → B.**
  Consequences of B: UI scenario layer [B] (wait/pass verb, danger interrupts,
  multi-tick travel, turn echo — the Stoneshard-lessons set, more input-layer work);
  Pruning Snip replaces ripeness as the demo's new system (D8); fixtures survive but
  Harvest Basket loses its main justification; Planning-tick hard invariant still applies.
- **Anti-thrash rule:** run the A/B toggle in the same build, same seed (`RunSeed`),
  ≥2 runs per mode before judging. One bad run is noise.
  **Revised 2026-07-20 (Phase Identity §4):** the experiment is sequential (A first, B-rev only if A fails), not a same-build toggle.

## D2 · Automation: Fixtures vs embodied gardener ✅

**Fixtures. Final.** Doris Bowl + Harvest Basket, coverage radii, no pathing,
deterministic, reads TD-spatially. The walking gardener is a **cosmetic skin on top of
fixture logic later**, never a separate behavior system. Do not re-litigate this if B
wins D1 — B changes who spends attention, not whether automation should path-find.

## D3 · Doris Bowl ✅

**Placeable fixture, ships together with Cravings or not at all** (solo it's a chore —
already established). Introduction mid-run via the starving ramp (~400 ticks ≈ day 3–4),
1-plant/day mercy cap stays. Nothing here is gated on D1.

## D4 · Player hunger ✅

**Cut for demo. Final.** Garden-as-fail-state substitutes personal stakes at zero extra
systems cost. Revisit post-demo *only* if playtesters report runs feel consequence-free —
and even then, prefer deepening garden stakes over adding a second hunger meter.

## D5 · Budget cost model — A per-tile vs B verbs-only 🧪

**Default: B (verbs-only) if the experiment is inconclusive.** Reasoning: verbs-only is
legible at a glance (one verb = one cost, no mental tile math) and doesn't tax garden
size — per-tile costs punish exactly the expansion the farming fantasy rewards. Choose
A per-tile only if playtests show degenerate verb spam across huge gardens (the failure
mode A exists to prevent). If A is ever adopted, cap displayed cost math at the tile
group level, never per-tile mid-drag.

## D6 · Auto-pause defaults ✅

**Default ON:** first occurrence of any event type, plant death, wave spawn.
**Default OFF:** lean-in prompts (ripeness windows, cravings) — pausing them deletes the
attention game they exist to create. All individually toggleable in settings
(`driver.SetPaused` path already specced). Tutorial-tier players get pauses; the skill
expression is turning them off.

## D7 · Icon rarity marker & tier count ✅

**3 tiers for demo (common / uncommon / rare), corner-gem marker.** Reasoning: 23 icons ×
frame variants is real pipeline cost; the gold frame competes with the category
background fill (the system's strongest read), while a corner gem is additive and scales
to a 4th tier post-demo without redrawing anything. Unblocks the pixel-art thread today.

## D8 · The demo's one new system ✅ (branch-dependent, both branches decided)

- **A wins D1 → N2 harvest ripeness windows** (as already ranked). Feeds Harvest Basket,
  answers under-stimulation, timing asymmetry (early attention = Perfect, late
  automation = Good) comes for free.
- **B wins D1 → Pruning Snip instead** (care verb suits embodied play; Decayed-leaf
  state; graduates to SelfPruning gene). Ripeness depends on the Commit & Watch
  attention economy and mostly evaporates under B.
- Either way: **one** new system. The substitution board S1–S12 governs everything else;
  cuts already confirmed (editor Splice minigame, scent-trail scouting) stay cut.

## D9 · Gene acquisition ✅

**Doris Provides is the primary economy; draft logic demoted to pity floor** (already
recommended — this ledger confirms it as final). **Gene Extraction is the first thing to
build** in this thread: low-cost, and both the digestion economy and the seed editor
depend on it. It is not gated on D1.

## D10 · Post-Fable execution order ✅

Reaffirming the Last Day Plan with model-strength routing now that Fable is gone:

| Pack | Needs | Route to |
|---|---|---|
| F1 buffer/DNA-strand migration | strongest available model | G1 guide is executor-proof — Opus/Sonnet with G1 open, sandbox test vectors first, **never** freestyle |
| F2 inventory inversion | mid | G2 guide, full caller table is in the doc |
| F3 run-loop screens | mid | G3 guide (mirror events, code-built overlays) |
| F4 hygiene sweep | weak is fine | G4 tables are mechanical |
| §4.3 experiment build | Milan + any model | it's a toggle + playtesting, not model-bound |

One pack per fresh session (2026-10: the extractor is retired; nothing to re-run) — unchanged.

---

## Standing integration warnings (cross-doc, easy to lose)

1. **Planning-tick hard invariant** (Rev 2 exploit): Planning ticks advance *only* the
   player's action economy — never plant growth, energy, or gene execution. Any future
   pack touching `TickManager`/`PlantGrowth` must re-verify this; it is not yet enforced
   in code.
2. **Minigames re-evaluation supersedes** Gameplay_Engagement_Research_2026-07-05 §7 ranking. If a
   future session cites §7, it's citing a stale ranking.
3. **UI shared-core spine (stages 0–4) is scenario-neutral** — safe to start regardless
   of D1. The scenario layers are not.
4. **Perfect ≡ Good is still the live code reward path** — U1/U2 tier differentiation is
   mandatory before any minigame tuning matters (Stardew lesson).

---

**Next action:** build the §4.3 A/B experiment toggle (same build, same `RunSeed`,
Commit & Watch vs Live Garden), run ≥2 runs per mode, then walk this ledger top to
bottom marking D1/D5 resolved. Everything else here is already decided.
