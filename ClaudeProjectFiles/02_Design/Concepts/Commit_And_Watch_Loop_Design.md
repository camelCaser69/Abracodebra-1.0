# Commit & Watch — Closing the Loop (Gardener & Doris Automation)

**Date:** 2026-07-06 · **Rev 2** (same day, doctor pass) · **Status:** CONCEPT — nothing implemented; no code changed. · **Home:** `02_Design/Concepts/` · **Builds on:** `Gameplay_Engagement_Research.md` (§4.3 Option A, §6.1 rule 5), `04_Reviews/Abracodabra_Foundation_Review_2026-06.md`, code facts verified on live disk 2026-07-06.

**Prompted by:** Milan's gut pick of §4.3 **Option A ("Commit & Watch")** + the two worries it raises: *"how do I manage time and strategic decisions?"* and *"who moves the player and feeds Doris while I'm watching?"*

**Rev 2 changelog (what the doctor pass changed):**
1. **Found a real exploit** — Planning ticks would grow plants / recharge energy / execute genes threat-free (verified: neither `PlantGrowth.cs` nor `PlantSequenceExecutor.cs` checks `RunState`; only hunger/fauna/world-effects are phase-gated). Fixed as a hard invariant (§4.1).
2. **Automation re-based: Fixtures instead of a walking gardener-AI** (§2.2) — kills pathing/interruption jank entirely, fits the tower-defense identity, and is cheaper. Walking gardener demoted to cosmetic layer / variant.
3. **Floor/ceiling got its missing mechanism: timing asymmetry** (§3) — v1's Auto-Harvest would have *consumed* the Perfect window it was supposed to leave for the player.
4. Budget hardened: two cost models to A/B, budget progression rule, exhaustion UX, and the hunger-as-tick-tax observation (§4).
5. Watch phase hardened: under-stimulation named as the #1 playtest risk, a lean-in density metric, event auto-pause settings, Report cause-attribution (§5).
6. Day reframed as **four diegetic beats** — Night / Greenhouse / Morning / Day (§4.5).

---

## 0. TL;DR

Commit & Watch does **not** break the loop — it orphans **three verbs** currently living in Growth & Threat (feed Doris · harvest · react to pests). Each gets a new owner, and the owners are cheap because the code already leans this way:

1. **Doris Bowl** — Doris feeds *herself* from a stockpile you provision. She is stationary (verified: `DorisController` has zero movement code), so it's a stockpile + a tick check on her existing `ReceiveFood`/`TryEatNearbyPlant` paths. The starving→eats-plants cascade is already implemented and becomes the failure state.
2. **Fixtures** — automation as *placeable furniture with a coverage area* (the Bowl is the first one; the **Harvest Basket** is the second). No gardener AI, no pathing, deterministic by construction, and placement is spatial strategy — tower defense with cozy furniture.
3. **Floor / ceiling / timing** — automation acts **late** at **Good** tier; personal attention acts **early** for **Perfect**. One rule keeps the Watch phase worth watching without making it APM-mandatory.
4. **Planning budget** ~40 ticks + progression, with one hard invariant: **budgeted Planning ticks advance only the player's action economy — never plant simulation** (§4.1, the exploit fix).

Recommended demo scope: **Bowl + budget + Harvest Basket**. The §4.3 experiment afternoon remains the gate, now with an A/B on movement costs (§9).

---

## 1. The worry, restated precisely: which verbs get orphaned

| Verb (today: all in G&T) | Under Commit & Watch | Owner | Effort |
|---|---|---|---|
| Till / plant / water | Move to Morning (Planning), cost budget ticks | Player | S — ungate `PlayerTileInteractor.cs:33`, add costs |
| Seed editing | Unchanged — Greenhouse beat, free, at the workbench | Player | 0 |
| **Feed Doris** | **Provision the Bowl; Doris self-serves during Day** | Bowl fixture (§2.1) | M |
| **Harvest** | **Basket fixture collects late at Good; manual harvest in the ripe window earns Perfect** | Basket fixture (§2.2) + lean-in | M |
| React to pests | Scoped intervention — Shoo! (engagement §6.3, already ranked #9) | Player (Day, opt-in) | S–M |
| Eat (player hunger) | Eat during Morning (1 budget tick — hunger becomes a *tick tax*) — or fold into Doris-only (§7.3) | Player | S |
| Mid-day watering | Morning prep only; **requires wet tiles to persist the full day** — one duration change **[verify current wet duration in Editor]**; irrigation is the [POST] answer | — | S |

Nothing is homeless. Decisions move to Morning, execution moves to fixtures, hands stay useful for the ceiling.

---

## 2. Who acts during the Day (watch) phase

### 2.1 Doris: self-service via the **Doris Bowl** ✅ unchanged from Rev 1, hardened

**Code facts (verified):** `DorisController` — stationary `MonoBehaviour`, `IWorldInteractable` + `IFeedable`, `ReceiveFood(ConsumableData, feeder)` with payload passthrough, `TryEatNearbyPlant()` on a tick cadence when Starving. `DorisHungerSystem` ticks only during G&T (phase-gated — codebase map §phase-gating).

**Mechanic:** the Bowl holds N items (start: 3). Stocking = Morning verb (1 tick / 2 items). During Day, every M ticks while ≥ Hungry she takes one item via `ReceiveFood`. Empty bowl + Starving → existing plant-eating, unchanged.

**Rev 2 hardening:**
- **Pacing ramp is free:** her starving threshold is ≈ 400 ticks *(survey)* ≈ 4 days — so rounds 1–3 need no provisioning at all. The Bowl introduces itself mid-run; no tutorial needed.
- **Death-spiral guard:** empty bowl → eats plants → fewer fruit → harder to restock is a genuine spiral. Mercy rule: **she eats at most 1 plant per day**, always the *marked/nearest*, with a loud thought-bubble warning when the bowl empties (`ThoughtBubbleController` exists). The Report attributes it: *"Bowl ran dry at tick 61 → Doris ate a Sunfern."*
- **Decision-weight dependency (honest):** with no currency and no sinks (D6), "how much to stock" is trivially answered "everything." Provisioning only becomes a *decision* alongside **Cravings + diet multipliers** (§4.6 — CORE anyway) and later Doris-Provides (the Bowl is its input hopper) and player-hunger competition. Ship Bowl + Cravings together or the Bowl reads as a checkbox.
- **Robustness:** if Doris ever becomes mobile, the Bowl still works (she paths to it — `AnimalController` food-seek is the existing pattern). Nothing here bets on her staying stationary.

### 2.2 The farm: **Fixtures** ✅ new recommendation (replaces walking-AI Standing Orders)

Rev 1 proposed a gardener AI walking around executing duties. Scrutiny kills it: pathing edge cases ("couldn't reach plot 4"), interruption/resume states, unreachable-target logging, visual confusion about who did what — a support-ticket generator inside a demo. The same automation as **placeable fixtures** deletes every one of those problems:

| Fixture | Coverage | Behavior (all on tick boundaries) | Floor tier |
|---|---|---|---|
| **Doris Bowl** | Doris | Feeds her when Hungry (§2.1) | Fed, never delighted |
| **Harvest Basket** | e.g. 3×3 | Collects ripe fruit **after the ripe window closes** (§3) into its stock; player empties it any time (free) or during Morning | Good — never fresh/Perfect |
| *(later)* Scare-Windmill | radius | Auto-shoo at your average minigame grade — §6.1 r5 verbatim | Fled, no bonus |
| *(later)* Sprinkler | cross | Re-wets tiles at dawn | Watered, no Pour Arc bonus |

**Why fixtures beat a walking AI:**
- **Zero AI.** A fixture is an `ITickUpdateable` with a radius check — deterministic by construction, no pathing, no interruption model, no resume bugs. (`GridDebugVisualizer` already has radius show/hide APIs for the coverage preview.)
- **Spatial strategy for free.** Coverage areas make *placement* the skill: which beds does the Basket cover? Bowl near the gate or the plots? This is the same muscle as tower placement — the TD half of the genre mashup finally gets a planning-phase expression.
- **On-brand.** Visible farm furniture beats an abstract duties menu (Visual Genome ethos: the farm *shows* its configuration). Placement/moving reuses the existing tile-interaction path, not a new UI.
- **Progression-legible.** Fixture count is the automation throttle: 1 fixture slot at round 3, second at round 5 (fixed schedule for demo; *draftable fixtures* as reward-pool entries is the roguelite-flavored [POST] variant).
- **The gardener stays yours.** No AI ever moves your avatar — which was the most uncanny part of Rev 1.

**Cost:** Basket ≈ **M** (placeable + stock + tick check + radius preview), reusing the Bowl's plumbing. Cheaper than Standing Orders v0 was.

**What's lost vs. Rev 1, honestly:** the charm of watching your little witch do chores. Recoverable later as pure presentation — fixture fires, gardener *walks over cosmetically* (no logic dependency). Charm as a coat of paint, not a state machine.

### 2.3 Variant kept in the drawer: walking Standing Orders

If playtests say fixtures feel too "vending machine," the Rev 1 duties design (2 checkbox slots, priority order, stateless re-evaluation on tick boundaries, instant player-input interrupt, `IDeterministicRandom` tie-breaks) is still coherent — just costlier and jankier. Revisit only with evidence.

### 2.4 Rejected outright (unchanged from Rev 1)

Full autoplay AI (resentment machine) · disembodied watch phase (kills D8 tactility, Shoo!, minigame homes) · popup-only status-quo feeding (the D7 chore, now on a timer).

### 2.5 The post-demo crown: the **Routine Strand** [POST]

Milan's "I program the player too" instinct, full version: a behavior strand in the sequencer UI — WHEN sockets (Doris hungry · fruit ripe · pest enters · plant stalled) + DO verbs (feed · harvest · shoo · stand at X), slots scaling via Telescope Strand. In fixture language: **the Routine Strand programs the network** — fixtures are its dumb tier, exactly like manual shooing is the Fear gene's dumb tier. The witch who automates her garden eventually automates herself. **L–XL**, new execution engine + failure-legibility surface. It stays POST; fixtures teach its concept in the demo.

---

## 3. Floor / ceiling / **timing** — the full rule

Rev 1 said "automation = Good, attention = Perfect" but missed the mechanism: if the Basket collects fruit *the moment it ripens*, the Perfect window never exists. The fix is a **timing asymmetry**, and it *is* the implementation:

> **Attention acts early. Automation acts late.** Every automatable event opens a short player-first window; the fixture is the janitor that cleans up after the window closes.

| System | Early (hands, Perfect) | Late (fixture, Good) |
|---|---|---|
| Harvest | Ripe window (~10–15 ticks): manual pick → fresh tag / Cascade chain | Window closes → Basket collects, plain fruit |
| Feeding | Hand-feed the *craving* before dusk → delight + mood | Bowl feeds on Hungry threshold → fed, no delight |
| Defense | Shoo! at the fence flash | Windmill/Fear gene fires after entry, slower |
| Watering | Pour Arc skill pour (multi-tile) | Sprinkler re-wets at dawn, single tiles |

Ceiling margin target: hands-on out-earns hands-off by **~15–20 %** (engagement §9.5), never more — pace-casual stays winnable hands-off. **Done when:** a full round played hands-off during Day survives at Good tier, and a hands-on player measurably (visibly, in the Report) beats it.

---

## 4. Morning under the budget — time & strategy

### 4.1 The invariant that makes it fair (Rev 2 exploit fix)

**Verified:** `PlantGrowth.OnTickUpdate` and `PlantSequenceExecutor` have **no phase gating** — they run on every tick, and Planning ticks are real `TickManager` ticks. Under a budget, walking in circles would therefore grow plants, recharge energy, and fire gene sequences **threat-free** (fauna spawns *are* phase-gated). Optimal play would be "always burn the whole budget pacing" — a chore-exploit that poisons the entire design.

> **Invariant:** during Planning/Morning, budget ticks advance **only** the player's action economy. Plant growth, energy, gene execution, hunger, waves, world effects all remain Day-phase-only. Implementation: the same early-out pattern the hunger systems already use (`RunManager.Instance.CurrentState` check at the top of the plant tick paths), decided once, tested by "spend 40 ticks pacing → plants unchanged."

(Corollary: `WeatherManager.PauseCycleAtDay()` already freezes sun during Planning, so photosynthesis wouldn't accrue meaningfully even unfixed — but energy recharge (`rechargeEnergyDuringGrowth`) and sequence ticks would. Gate them all uniformly; partial gating is how the next exploit is born.)

### 4.2 Two cost models — A/B them in the experiment, don't argue in a doc

- **Model A — Stoneshard:** movement 1/tile + verb costs (till 2 · plant 1 · water 1 · place/move fixture 2 · stock bowl 1/2 items · eat 1). Layout, walking routes, and fixture placement all matter. Risk: nickel-and-dime feel; big farms get squeezed superlinearly by walk distance.
- **Model B — verbs-only:** movement free; verb costs raised (till 3 · plant 2 · water 2 · fixture 3). Cleaner reads, no route-optimizing; loses walking-distance strategy (fixture *coverage* still preserves spatial play).
- No stuck states in either: 0 ticks left just means no more verbs — Start Day is always pressable, from anywhere.
- UX in both: hover shows a verb's cost before commit; Start Day warns on obvious foot-guns (*"Bowl is empty — start anyway?"*).

### 4.3 Budget size & progression

Base **40**, tune with: **the pressure should stay roughly constant across the run** — each morning affords ≈ (replant ⅓ of the farm + provisioning + ~20 % slack). Since farms grow, that means the budget grows: **+4/round** as the starting guess, or tie increments to round rewards ("longer mornings" as a draftable boon — [POST] flavor). Rounds 1–2 deliberately over-budgeted (teaching space).

### 4.4 Notes that fell out of scrutiny

- **Hunger as tick tax:** if player hunger survives (§7.3), eating costs 1 Morning tick + food — hunger and the budget become one currency. Elegant enough that keeping player hunger is now *cheaper* than it was pre-Option-A; still leaning Doris-only for clock-count reasons.
- **U3 dissolves for planting:** the planting minigame now runs in frozen Morning time — no live ticks stolen. Pause-the-driver (U3) applies only to Day-phase minigames (Cascade, Shoo).
- **Pour Arc adaptation:** its overfill penalty ("mud slow") is meaningless in frozen time → overfill wastes 1 tick instead.
- **B5 reminder:** `ProcessMultiTickMovement` animates with `WaitForSeconds` — with movement now spending budget in Planning, convert it to tick-counters when B5 is done anyway.

### 4.5 The day as four beats (framing, not new systems)

**Night** (Report + Draft, frozen) → **Greenhouse** (seed editing at the workbench, frozen, *free* — the edit-speed principle untouched) → **Morning** (budgeted field prep: plant, water, fixtures, provisioning) → **Day** (commit; the machine runs). Diegetic skin: the budget meter is a **sun-arc creeping toward the horizon**, not a number — same data, cozier read. This also answers "when is editing allowed" cleanly: always free, anchored at the workbench, no budget interaction.

---

## 5. The Day (watch) phase — keeping it worth watching

**Named risk #1 of the whole design: under-stimulation.** A day is 100 ticks ≈ 50 s at 1× (≈ 25 s at 2×). With decisions moved to Morning and floors automated, an empty Day phase is a screensaver and the design dies in its first playtest. Guards:

- **Hard prerequisites, not nice-to-haves:** §4.4 energy rebalance + stall legibility (the show must *do* something), §4.2 escalation + forecast (the threat must threaten), §4.7 feedback pass (the show must be *legible*). Option A ships with these or not at all.
- **Lean-in density metric:** ≥ 1 optional hands-moment per ~15–20 s of Day at 1× — ripe windows, craving delivery, Shoo! flashes, (later) Cascade chains. Author waves/ripeness staggering against this number; it's a content-tuning dial, not code.
- **Report attribution:** every automated outcome names its cause (*"Basket collected 3 (window missed)" · "Bowl fed Doris ×2" · "Perfect harvest ×1 — fresh"*). Hands-off must never feel like hidden dice.
- **Event auto-pause (settings, cheap, big):** driver already has `SetPaused` — expose toggles: *pause on wave arrival / on craving / on ripe window / on plant stall*. Deliberate players get semi-WeGo Day for free; it's also the accessibility valve and the bridge for anyone who wanted Milan's original "actions drive everything" instinct.
- **Intervention economy v0: free-form** (Day actions already cost nothing post-A1). Add scarcity (charges) only if playtests show APM-degenerate play. Don't pre-build an economy for a problem not yet observed.

**The closed loop, one line:** Forecast → Greenhouse (edit) → Morning (spend the sun: prep, provision, place) → Commit → Day (machine runs; you read, pause, and take the early windows) → Night (Report with causes → Draft) → next Forecast.

Phase sentences (§4.3 done-when): *Morning is where you make every decision that matters, under a sun that won't wait. Day is where you find out if you were right — and earn the ceiling with your hands.*

---

## 6. Build order (staged, demo-first — Rev 2)

| Stage | Item | Effort | Tag |
|---|---|---|---|
| 0 | **Experiment afternoon** (§9) — now with cost-model A/B toggle + gating verification + Bowl mock | S | **CORE — the gate** |
| 1 | Phase-gate plant simulation (invariant §4.1) · budget re-add + sun-arc HUD · ungate Planning verbs + costs (both models behind a config) · **Doris Bowl** | S+S+S+M | CORE if A confirmed |
| 2 | **Harvest Basket** (+ ripe-window timing asymmetry) · Report cause-rows (rides §4.1 report) · craving hand-delivery delight (rides §4.6) · empty-bowl warning bubble | M+S+S+S | CORE |
| 3 | Event auto-pause settings · Shoo! (already #9) · budget progression +4/round · fixture slot #2 unlock (R5) | S+S–M+S+S | NICE |
| POST | **Routine Strand** · Scare-Windmill & Sprinkler · draftable fixtures · walking-gardener cosmetic layer · Gobble Toss (waits for Doris-Provides; the Bowl is its hopper) · irrigation | L–XL | POST |

Stages 1–2 ≈ **1.5–2 weeks with solo buffer**, subsuming several already-ranked CORE items (they were counted in the engagement doc's 4–6-week CORE band; this is re-sorting, not adding).

---

## 7. Open questions for Milan (Rev 2)

1. **Fixtures vs. walking gardener:** does automation-as-furniture satisfy the fantasy, or is the embodied witch doing chores essential to you? (Fixtures are the recommendation; §2.3 keeps the fallback.)
2. **Bowl placement:** fixed fixture at Doris's pen (simplest) or placeable like other fixtures (consistent, one more decision)? Rev 2 lean: **placeable** — now that fixtures are the pattern, the Bowl should obey it.
3. **Player hunger:** fold into Doris-only, or keep as the 1-tick Morning tax (§4.4)? Still leaning Doris-only.
4. **Cost model gut-lean before the A/B:** Stoneshard per-tile (A) or verbs-only (B)?
5. **Auto-pause defaults:** ship on (wave + craving) or all-off?

## 8. Risks & anti-goals (Rev 2, ordered by kill-probability)

1. **Day-phase under-stimulation** — the design's #1 risk; guarded by §5's prerequisites + density metric + auto-pause. Test explicitly at 2×.
2. **The Planning-tick exploit** — fixed by the §4.1 invariant; regression-test it ("40 ticks of pacing changes nothing but the clock").
3. **Doris death-spiral** — 1-plant/day mercy cap + warning bubble + Report attribution.
4. **Idle-game drift** — fixture slots capped at 2 for the demo; early windows never automatable at demo scope; ceiling margin real (~15–20 %).
5. **Provisioning-as-chore** — Bowl ships together with Cravings or not at all (§2.1).
6. **Determinism erosion** — fixtures act on tick boundaries with `IDeterministicRandom` tie-breaks; the A6 contract stays "same seed + same player actions ⇒ same outcome" (interventions are actions — don't over-promise replay).
7. **Scope siren** — the Routine Strand stays POST. So do draftable fixtures.
8. **Edit-speed principle** — Greenhouse editing is free and un-minigamed, forever (Splice anti-recommendation stands).

---

## 9. Next action anchor

**Run the Stage-0 experiment afternoon** — now specced as: ungate Planning input · hack the 40-tick budget with the **A/B cost-model toggle** · workbench-gate the editor · verify (and if needed stub-gate) plant ticking during Planning · chest-as-Bowl mock if under an hour. Then two 15-minute plays (one per cost model), answering **four** questions: does budgeted Morning feel like decisions? per-tile or verbs-only? does hands-off Day hold attention at 2× (count your own lean-ins per minute)? did anything grow during Morning that shouldn't have? Report answers → §7 gets decided → Stage 1 becomes a task pack in `03_Tasks/Active/`.
