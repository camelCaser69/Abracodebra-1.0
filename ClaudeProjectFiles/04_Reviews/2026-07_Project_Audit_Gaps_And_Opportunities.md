# Project Audit — Gaps, Holes & Cheap Wins (2026-07-20)

**What this is.** Final Fable 5 sweep across concepts + codebase for problems and
opportunities **not already documented** in the KB. Known issues (Perfect≡Good, wave
list past round N, planning-tick exploit, input fragility, modifier scarcity, etc.)
are deliberately excluded — see the concept docs for those.

Legend: **✔** = disk-verified this session (grep/read) · **~** = high-confidence
inference from map + extracts, verify before acting.

---

## 1 · Critical holes (each can sink the demo on its own)

### H1 · The game will have no lose condition ✔

The **only** `SetState(RunState.GameOver)` trigger in the entire codebase is player
starvation in `RunManager` — and it sits behind a `playerDeathEnabled` flag that
already short-circuits it. Ledger D4 cuts player hunger for the demo. Net result:
**an unloseable game.** Garden-as-fail-state was chosen in the minigames doc but has
**zero code path and zero concrete definition** anywhere.

Define it now, small and legible: **run ends in defeat when the garden is dead** —
no living plants AND no plantable seeds in inventory (check at round boundary, not
mid-wave, so a wipe still gets its dramatic beat + Report). `OnPlantDied` already
exists to drive the check. Optional cozy-dark alternative worth considering: Doris
leaves when starving too long — softer, on-tone, but needs the Bowl/Cravings work
first; ship the inventory check for the demo.

### H2 · No victory arc either ~

Rounds escalate, but nothing defines "you won the run." The documented wave-list
behavior (past round N → no wave at all) means late rounds just go quiet. A demo needs
a bow on it: **fixed run length (e.g. survive N rounds) → Victory screen with the
run's garden on display.** Rides the F3/G3 screens work almost for free — it's a state
transition + one screen. Decide N; everything else exists or is already planned.

### H3 · No save/resume — and runs are 30+ minutes ✔

Zero serialization anywhere (no PlayerPrefs, no save path, nothing). Quit = lose the
run. For a "pace-casual" audience that's a demo killer. Three options:

- (a) Full state serialization — expensive, don't.
- (b) Determinism dividend: save = `RunSeed` + action log, replay to restore. Elegant,
  cheap on disk format, but demands *perfect* determinism discipline forever and a
  fast headless replay — fragile until C3 (below) exists.
- (c) **Round-boundary snapshot (recommended):** you may only quit-save between rounds,
  where live state is minimal — round #, inventory, planted strands + growth stage,
  Doris meters, budget. A hand-written serializer over ~5 systems, not 200 scripts.

Do (c) for the demo; (b) becomes viable later as a bonus once C3 is trusted.

### H4 · The first ten minutes are undesigned ~

Demo-first strategy, gene system + WeGo + fixtures + phases — and **no onboarding doc
exists anywhere in the KB.** The demo's first 10 minutes *are* the product. Cheapest
credible version, no tutorial system needed: **a scripted first round** — fixed
`RunSeed` (infrastructure exists), a pre-built starter strand, first-occurrence
auto-pauses (already Ledger D6 defaults) carrying 3–5 one-line contextual tips, and
the gene codex (W4) as the pull-based "what does this do" answer. Write the script as
a design doc first; it will also expose which UI affordances are still missing.

---

## 2 · Design gaps (not fatal, but real)

### G1 · Audio is not a topic anywhere ✔

Grep confirms: scattered `AudioSource.PlayClipAtPoint` on animal feeding + minigame
clips — no AudioManager, no music, no mixer, no phase ambience, and **no audio
direction in any design doc.** For a tick-based game this is a doubly missed lever:
ticks want to be *heard* (Thronefall's day/night musical shift does massive tonal
work). Cheap v0: one `AudioManager` singleton, ~10 SFX wired to events that already
fire (`OnPlantDied`, wave spawn, harvest tiers — distinct Perfect vs Good sounds is
feedback design, not polish), one ambient loop per day-beat. Audio is the cheapest
juice multiplier the project has and it's currently at zero.

### G2 · Nothing persists between runs ~

No meta-progression is defined or coded. Without *any* between-run hook, "one more
run" relies on curiosity alone. Cheapest on-brand version: **persistent gene codex** —
genes/combos you've discovered stay discovered (ties into Doris Provides discovery
and doubles as H4's pull-based tutorial and a collection meta). Heavier unlock trees
are post-demo; the codex alone is a PlayerPrefs-sized feature.

### G3 · Economy has sources but no sink ~

Pest drops (chitin/manure) and Doris Provides both *produce*; nothing consumes beyond
planting. Either give the demo **one sink** (fixture crafting — Basket/Bowl cost
materials → defense-as-greed loop closes, and fixtures gain earned-not-given weight)
or **cut pest drops from the demo** — an inventory of stuff with no use is worse than
no stuff.

### G4 · Icon system has a colorblind cliff ~

Category = full background color fill is the system's strongest read — and its
weakest for the ~5 % colorblind audience. Rule to adopt now, while only 23 icons
exist: **motif silhouette alone must disambiguate category** (color is reinforcement,
not sole carrier). One pass through a CB simulator before the icon set grows.

### G5 · "Cozy-dark" is asserted, not carried ~

Tone currently lives in docs, not in any shipped surface. Cheapest carrier: **Doris
reactions** — she already has hunger states and feeding events; small emotes on
existing triggers (fed, starving, plant died nearby, wave incoming) make her the
emotional barometer of the garden at sprite-swap cost. Pairs with the Report: one
Doris mood line per day summary.

### G6 · Nobody owns wave content ~

Threat *mechanics* are designed; the actual **authoring** of N rounds × waves for the
demo has no pipeline or doc. Hand-authored lists rot (the past-round-N bug is this
smell already). Small fix: a wave *formula* (budget curve by round + weighted spawn
table) plus a short hand-authored "spice" list for signature moments. One
ScriptableObject, one curve.

---

## 3 · Code & systems risks

### C1 · PlayerHungerSystem is now an orphan ✔

Exists on disk (`ITickUpdateable`, `IFeedable`), design has cut it (D4), and its one
systemic consequence — GameOver — is the thing H1 must re-point anyway. Strip or
hard-gate it in the F4 hygiene pack; don't let a dead system keep ticking (it's a
determinism hazard and a confusing IFeedable target for the feeding UI).

### C2 · Statics are a run-2 bug farm ~

`InventoryService`, `HotbarSelectionService`, `FoodSelectionPopup.IsBlockingInput`,
plus G3's planned static mirror events (`AnyPlantDied` etc.) all hold state/subscribers
that outlive a run. Restart-without-app-quit is exactly what demo players do after
losing. Mandate: **every static surface gets a `Reset()` called from a single
`RunTeardown` path**, and the A6 two-run seed-repro test doubles as the smoke test —
run 1 and a restarted run 2 on the same seed must match. Do this *before* F2/F3 add
more statics, not after.

### C3 · Determinism is bought but not spent: no golden-run harness ~

The per-run seed + `IDeterministicRandom` work was done precisely so runs repro — yet
nothing exploits it. **Golden-run regression test:** headless-advance N ticks on a
fixed seed, hash the world state (plant stages, energy, positions, inventory), assert
against a stored hash. Runs in the sandbox, no Editor. This single harness protects
every future pack — above all the F1 buffer migration, the riskiest change on the
books. Build it before F1, not after.

### C4 · UI Toolkit ↔ world-space bridge is unplanned ~

Mark & Go pins, path/ETA previews, ripeness telegraphs, tile tooltips — all need
world→panel coordinate mapping (`RuntimePanelUtils.CameraTransformWorldToPanel`), and
the UI research doc never covers the seam. Small, but spec it once (one
`WorldUIAnchor` helper) before three features hand-roll it three ways.

### C5 · GameUIManager is quietly becoming a god object ~

~875 lines as coordinator, and F3 screens + budget meter + Report all gravitate toward
it. Rule, adopted now: **new features get their own `UI[Name]Controller`;
GameUIManager only instantiates and wires.** Zero refactor cost today; expensive to
retrofit in six months.

---

## 4 · Cheap wins (ranked by value ÷ cost)

| # | Win | Cost | Why it punches up |
|---|---|---|---|
| W1 | **Daily seed run** — date-derived `RunSeed`, one menu button | ~hours | Infrastructure already exists; retention + community comparison for free |
| W2 | **Strand share codes** — strand → base64 string | days (needs F1 buffer model) | Backpack Battles/Noita community energy; turns builds into social objects |
| W3 | **End-of-run garden snapshot card** — Report + Visual Genome render → shareable image | days | Players generate the marketing; the "it IS what I made" principle, exported |
| W4 | **Gene codex** (G2/H4) | days | Meta + tutorial + collection in one PlayerPrefs-sized feature |
| W5 | **Assist settings** — window-scale slider, default speed, auto-pause toggles | ~day (rides F3 settings) | Pace-casual promise made real; D6 toggles need a home anyway |
| W6 | **Audio v0** (G1) | days | Cheapest juice multiplier, currently at zero |
| W7 | **"All clear" chime + Quiet Time** | hours | Already-scored mechanic completed by one sound |

---

## 5 · Priority shortlist

If only five things from this audit happen: **H1** (lose condition — the game must be
losable) → **C3** (golden-run harness — before F1 touches the executor) → **C2**
(static teardown — before F2/F3 add statics) → **H4** (scripted first round doc) →
**H3c** (round-boundary save). W1 and W5 ride along nearly free.

---

**Next action:** write the H1 defeat check (round-boundary: no living plants ∧ no
plantable seeds → `GameOver`) into the A-PoC task alongside §2.4 item 1 — the same
session that enforces the Planning-tick invariant should make the game losable.
