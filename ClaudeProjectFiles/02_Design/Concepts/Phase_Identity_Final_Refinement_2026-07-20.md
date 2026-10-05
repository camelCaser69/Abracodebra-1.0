# Phase Identity — Final Refinement of A & B (2026-07-20)

**What this is.** Final Fable 5 pass on the §4.3 decision before the experiment build.
Supersedes the *definitions* of both options wherever earlier docs conflict
(`Commit_And_Watch_Loop_Design_2026-07-06.md` Rev 2 remains the detailed spec for A's systems;
`UI_Systems_Research_2026-07-08.md` scenario layer [B] is partially obsoleted — see §3).
Companion to `03_Tasks/Active/2026-07_Fable5_Design_Decision_Ledger.md` (D1/D5/D8).

**The new input that forced this pass:** Milan reports that in the simulation version,
manual movement and interaction feel **finicky and hectic** — player actions should be
programmable or automatic. This is a verdict, not a preference: **hand-piloted avatar
control during a running clock is out of the design space.** Both options must be
rebuilt around it.

---

## 1 · The reframe: what actually separates A and B

With direct piloting dead, both options become **director modes** — the player expresses
intentions, the game executes them. The old framing ("watch vs play") is gone. The real
remaining axes:

| Axis | A — Commit & Watch | B — rebuilt (see §3) |
|---|---|---|
| When time advances | Clock runs by itself (ExecutionPhaseDriver) | Only when the player spends ticks |
| Source of tension | **Lateness** — the world moves whether you act or not | **Scarcity** — the day is a fixed tick budget you allocate |
| Player's day-phase job | React to lean-in windows; automation covers the floor | Solve an allocation puzzle in calm, frozen time |
| Genre feel | Tower defense / Pikmin day | Into the Breach / Opus Magnum turn economy |
| Failure texture | "I wasn't watching" | "I planned badly" |

Critical consequence, stated once and bluntly: **ripeness windows and the whole
timing-asymmetry pillar (early=Perfect / late=Good) only exist under A.** In B, time
waits for you — you are never *late*, only *overcommitted*. If B wins, that entire
mechanism family is replaced, not ported.

---

## 2 · Option A — Commit & Watch, final form

Rev 2 stands. Refinements from the finickiness verdict:

**2.1 No avatar piloting during Day — ever. Interventions are marks (Mark & Go).**
Milan's proposal, adopted after evaluation: the player **clicks a tile to mark it**; the
gardener auto-paths there and performs the **contextual verb** — the tile knows what it
needs (ripe plant → harvest, dry soil → water, bush/mushroom → forage, pest → shoo,
decayed leaf → snip). One click, zero piloting.

*Why this beats instant cursor verbs (the previous spec):*

- **Travel time is real**, so timing asymmetry gets *stronger*, not weaker: Perfect =
  the gardener **arrives inside the window**, which means you marked *early*. The skill
  is anticipation, not reflexes — exactly right for "pace-casual, not easy-casual,"
  and it makes the sun-arc clock felt in the body of the game.
- Gardener **position and speed become a strategy surface**: where you idle-station her
  between marks matters; boots/speed upgrades and (post-demo) a second gardener are
  natural progression items. Instant clicks had no such depth.
- It resolves the tone clash: an instant-resolve click on a living garden feels gamey;
  a little gardener trotting over to do the work *is* the cozy fantasy.
- It completes the automation ladder: **marks = manual labor (Perfect), fixtures =
  programmed labor (Good), unmarked+uncovered = loss.** The run's upgrade arc is
  literally converting marks into fixture coverage.

*Risks and their guards:* needs grid pathing — on this tile scale a BFS over
`GridPositionManager` is an afternoon, not a system (and it's the same pathing B-rev
needs, see §3). Perceived unresponsiveness → show **path + ETA preview on hover** and a
flag pin on marked tiles; tune walk speed generously. Overload → **queue cap 3 marks**
in the demo, marks cancellable by re-click, FIFO order (priority reordering is
post-demo). Windows stay telegraphed and generous so a marked-in-time order reliably
lands. `GardenerController` already exists and is `ITickUpdateable` — in mode A it
consumes ticks only for movement along the path; verbs execute on arrival.

**2.2 Programmability lives in Planning, not Day.** Milan's "actions should be
programmable" instinct maps onto A as: anything you'd do *repeatedly* during Day must be
delegable — fixtures (Harvest Basket, Doris Bowl) are exactly that programming surface.
The upgrade arc of a run = converting Day clicks into fixture coverage. Day-phase manual
clicks are the *premium* path (Perfect tier), never the *mandatory* path (floor is Good
via fixtures). If a playtester feels forced to click frantically, the fixture floor is
mistuned — that's the tuning dial, not a design failure.

**2.3 Hectic-feel guards (direct answer to "weird and hectic"):**

- Lean-in density band: ≥1 meaningful prompt / 15–20 s, but also **≤1 simultaneous**
  prompt on screen in the demo. Overlaps queue; queued prompts extend their windows.
- Auto-pause defaults per Ledger D6 (first-occurrence, plant death, wave spawn ON).
- Space = pause, Tab = speed already exist (driver) — surface them in the HUD from day
  one; an unpauseable-feeling sim is where "hectic" is born.
- Timing windows are **generous and telegraphed** (sparkle before ripe, not after).

**2.4 A demo proof-of-concept must include** (in order; cut from the bottom):

1. Planning-tick hard invariant enforced in code (RunState gating on
   `PlantGrowth`/`PlantSequenceExecutor`) — the Rev 2 exploit fix. Non-negotiable.
2. Sun-arc day with the four beats (Night/Greenhouse/Morning/Day) and phase HUD.
3. **Mark & Go v0**: click → contextual order → gardener BFS-paths and executes on
   arrival; queue cap 3, re-click cancels, path+ETA preview, tile flag pin.
4. One lean-in mechanic end-to-end: **N2 ripeness windows** on harvest — Perfect =
   gardener arrives inside the window (requires U1 tier split, since Perfect≡Good is
   still the live reward path).
5. One fixture: **Harvest Basket** (coverage radius, harvests late at Good tier).
   Bowl waits for Cravings (Ledger D3).
6. End-of-day **Report** with cause attribution (rides F3/G3 screens work).
7. Budget meter for Planning verbs (cost model B verbs-only per Ledger D5).

**Done when:** a full round plays with hands off the keyboard producing a Good-tier
harvest via the Basket; an attentive player beats it to Perfect by marking early enough
for the gardener to arrive inside windows; pausing at any moment is possible and
legible; a marked order can be cancelled before arrival with no residue.

---

## 3 · Option B — dead as specced; rebuilt as "Tick Ledger" (B-rev)

**Old B (player-driven embodied WeGo — walk tile-to-tile, verbs on adjacency, Stoneshard
travel) is dead.** Milan's finickiness verdict removes its core, and
`UI_Systems_Research_2026-07-08` scenario layer [B]'s multi-tick-travel/repeat-verb items die with
it (wait/pass, danger interrupts, turn echo log survive — see below).

**B-rev: the day is a tick budget.** The strongest honest steelman consistent with
"programmable, not piloted":

- Day = **N ticks** (e.g. 60). Every order costs ticks: harvest 2, water 1, move-adjacent
  1 (avatar auto-paths; travel cost = path length — position becomes a planning
  resource, not a piloting chore).
- Player queues orders from frozen time — click plant → verb menu → enqueue. Queue
  visible as a strip (Opus Magnum program feel). **Execute** runs the queue through
  `TickManager.RequestActionTicks`; world (waves, growth, Doris) advances in lockstep.
- Waves arrive at **known tick counts** (Into the Breach telegraphing). Threat pressure
  = "the wave lands at tick 40; is my harvest done by then?" — scarcity, not lateness.
- **Danger interrupt:** queue halts when a new threat enters aggro range; player may
  re-plan free of charge. This is the WeGo promise kept.
- Doris hunger ticks with spent ticks. Idling costs nothing but *achieves* nothing —
  the run timer (rounds) still advances only via play, so turtling is self-defeating,
  not exploitable.

**What B-rev needs that doesn't exist:** grid pathing (A* over `GridPositionManager` —
real new code), order-queue component + queue UI, tick-cost table per verb, interrupt
rules, un-gating `PlayerTileInteractor` from GrowthAndThreat-only, driver in manual
mode (exists — `RequestActionTicks` is already the action-driven entry). **Delta
revised down by Mark & Go:** A now builds the pathing and the contextual-order verb
resolution itself (§2.1); B-rev's orders *are* marks executed in spent-tick time. The
remaining B-rev-only work is the queue-strip UI, per-verb tick costs, and interrupt
rules — **≈ +1 wk over A's PoC**, down from 1.5–2.

**What B-rev loses:** ripeness/timing asymmetry (§1), the sun-arc-as-clock drama,
fixture urgency (automation matters less when time is patient — Basket becomes a
tick-cost discount instead of a lateness guard). Doris ramp needs re-tuning to
tick-spend rather than wall-clock.

**Done when (B-rev PoC):** a queued 10-order day executes deterministically, a
mid-queue wave interrupt offers a free re-plan, and two runs on the same `RunSeed` with
the same queue produce identical outcomes.

---

## 4 · The honest comparison

- **A is the smaller build** (driver, windows, one fixture, report — mostly systems
  already specced/partially built) and the only version where the entire Rev-2 edifice
  (four beats, timing asymmetry, fixtures-as-programming, Doris wall-clock ramp)
  survives intact.
- **B-rev is the calmer, more ADHD-friendly thinking game** — genuinely on-touchstone
  (Into the Breach, Opus Magnum) and immune to the hectic-feel risk *by construction*.
  It is not a control-scheme variant of A; it is a different game with the same nouns.
- The finickiness verdict actually **strengthens A**: the thing Milan disliked was
  never A (A was always hands-off + clicks) — it was proto-B's piloting. A's risk
  remains only under-stimulation, and §2.3 has the dials for it.
- **Mark & Go lets A absorb B-rev's best part.** The embodied executor — the one
  genuinely appealing thing about B — now lives inside A, under an autonomous clock.
  What B-rev still uniquely offers is only *frozen-time deliberation*, and Space-pause
  already provides a bounded version of that in A.
- **Fallback symmetry (improved):** if A fails its density test, B-rev now reuses
  ~90 % of A's PoC (pathing, contextual orders, budget→tick meter, report, screens,
  invariant); only the queue strip and tick-costing are new. Building A first is the
  cheapest road *to* B-rev if needed.

**Recommendation (final):** build **A's PoC first**, exactly §2.4, and run the Ledger
D1 density test against it. Build B-rev **only if A fails after density-lever tuning**
— not as a parallel toggle. Rationale: the old A/B toggle assumed the two shared a
build; B-rev no longer does (pathing + queue are real work), and a cheap fake-B would
test a strawman. This **revises Ledger D1's "same-build toggle" protocol**: the
experiment is now sequential (A first, B-rev on failure), same seed, ≥2 runs per mode
still applies.

---

**Next action:** implement §2.4 items 1–4 (invariant → sun-arc HUD → Mark & Go v0 →
ripeness window with tier split) as the A proof-of-concept; playtest against the
≥1-prompt/15–20 s density bar before touching items 5–7.
