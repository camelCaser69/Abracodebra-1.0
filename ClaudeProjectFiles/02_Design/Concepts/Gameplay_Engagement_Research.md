# Gameplay Engagement Research — Shortcomings, Fixes, Minigames & Spice-Ups

**Date:** 2026-07-05 · **Status:** CONCEPT RESEARCH ONLY — nothing in this document is implemented; no code or assets were changed. · **Home:** `02_Design/Concepts/` · **Builds on:** `01_Core/Abracodebra_Codebase_Map.md` (2026-07-05 sweep), `02_Design/gene_systems_deep_dive_v6.md`, `04_Reviews/Abracodabra_Foundation_Review_2026-06.md`.

---

## 0. TL;DR (honest version)

The tick engine, gene executor, and plant lifecycle are in good shape — the *machine* works. What's missing is almost entirely the **game around the machine**:

1. **A run has no arc.** No win condition, no game-over screen, rounds continue past the authored wave list into literally empty days, and rewards arrive silently. The roguelite has no "run."
2. **The threat doesn't threaten.** 1–3 pests per authored wave against a defense-gene arsenal built for far more. "The plant is the health bar" is a great identity that is almost never tested.
3. **The phases don't know what they are.** Planning is a menu (world verbs are hard-gated to Growth & Threat — `PlayerTileInteractor.cs:33`), while Growth & Threat is simultaneously the watch-the-plants show, the do-all-your-farming window, and the minigame window. The WeGo promise — *commit a plan, watch it resolve* — is diluted on both ends.
4. **The show sputters.** With current numbers a starter plant runs an energy deficit (≈ −16 E per sequence loop) and spends most of the execution phase visibly stalled. The automation fantasy dies on arithmetic, not on design.
5. **Moment-to-moment tactility is thin.** Four tools, two tile rules, flat 1-tick costs, instant bulk harvest with invisible yields, and one minigame whose Perfect result is mechanically identical to Good.

None of these are foundation problems. Most of the highest-impact fixes are **numbers, ceremony, and feedback** — cheap relative to what's already built. §7 proposes a concrete demo slice.

---

## 1. Method & evidence base

Four parallel code/doc surveys (minigames+tools+verbs · run/wave/hunger/reward loop · gene/plant content · design docs), then direct reads of the load-bearing files: `MinigameManager.cs`, `TimingCircleMinigame.cs`, `TimingCircleConfig.cs`, `MinigameTypes.cs`, `000_TimingCircleConfig_Planting.asset`, `RunManager.cs`, `WaveManager.cs:120–184`, `TickConfig.asset`, `PlayerTileInteractor.cs` (gate), `PlayerActionManager.cs:160–198`. Live files win; `06_Index` extracts were treated as stale (pre-A-pack). Claims sourced from survey agents rather than my own reads are marked *(survey)*; anything asset-config-dependent that should be eyeballed in the Inspector is marked **[verify in Editor]**.

---

## 2. The run as the player experiences it today

**Numbers on disk** (`TickConfig.asset`, `WaveManager.cs`, `RunManager.cs`):

| Parameter | Value | Consequence |
|---|---|---|
| Tick rate | 2 ticks/s (base; Tab cycles speed, Space pauses) | — |
| Round = 1 wave = 1 day | `ticksPerDay` = 100 ticks | ≈ 50 s of Growth & Threat at base speed |
| Day / night / transition | 60 / 40 / 10 ticks | night ≈ 20 s; photosynthesis ~0 at night |
| Planning budget | none — `maxPlanningPhaseTicks` was removed as unimplemented (`TickConfiguration.cs:25`; the .asset still carries a stale serialized `0`) | Planning is untimed |
| Wave content | `wavesSequence[round−1]`, authored list | past the list: warning + **no wave at all** (`WaveManager.cs:137–142`) |
| Wave pests | 1–3 per authored wave *(survey)* | see D2 |
| Player starvation | ≈ 667 ticks *(survey)* | ≈ 6–7 rounds; the only game-over |
| Doris starving threshold | ≈ 400 ticks *(survey)* | then she eats your plants on a cadence |
| Gene rewards | 1–3 random genes/round, tier-gated, silent inventory insert | see D6 |
| Currency / shop / score | **none** (verified absent, *survey*) | harvest has no sink beyond eating/feeding |

**A round, walked through:** Planning — you can *only* edit seeds and inventory (world input returns early outside Growth & Threat, `PlayerTileInteractor.cs:33`); no clock pressure; you press the end-planning button (`GameUIManager.cs:837`). Growth & Threat — ticks auto-advance (post-A1); you now do **everything** in real tick-time: till, plant (minigame interrupts here), water, harvest, eat, feed Doris, while plants execute sequences and 1–3 pests wander in at the authored spawn tick. Plants visibly stall on energy (§D5). The wave *timer* ends the round regardless of what happened. Rewards silently appear. Repeat — until the wave list runs out, after which rounds contain no threat, or until you personally starve.

**What already works in the code's favor** (worth saying before the criticism): deterministic per-run seed (A6) — an underexploited superpower (§5.1, §5.5); a real event surface (`GeneEventBus`, Doris events, `OnActionExecuted`) with almost no subscribers — celebration hooks pre-wired; `MinigameType`/`MinigameTrigger` enums already reserve StopTheBar / ReactionTest / RhythmTap and Watering / Harvesting / Tilling (`MinigameTypes.cs:10–30`) — the minigame roadmap is half-sketched in code; `FloatingCombatText`, `ThoughtBubbleController`, scent system, fireflies — juice components already exist, idle.

---

## 3. Diagnosis — the nine shortcomings

Format: **Evidence → why it kills engagement → fix direction** (details in §4).

### D1 · The run has no arc
No win state; `RunState.GameOver` only via player starvation (and `playerDeathEnabled` can gate even that); `RestartGame()` is a bare scene reload with no screen (C-list item confirmed). Waves stop existing past the authored list. **Effect:** no reason to care about round N vs. round N+1; roguelite motivation (run → outcome → knowledge → retry) never forms. **Fix:** finite demo run with a finale + end screens + per-round tally (§4.1).

### D2 · The threat doesn't threaten
1–3 pests/round *(survey)* vs. an arsenal of thorns, bark, traps, projectiles, bursts, poison/freeze/fear payloads. Waves end on a timer whether or not anything happened (A3 made this honest, not dangerous). Leaf loss — the core identity — is rare. **Effect:** defense genes are answers to a question the game never asks; tension flatlines; the "cozy-dark" dark half is missing. **Fix:** escalation curve, pest roles, forecasting (§4.2).

### D3 · Phase identity crisis
Design intent (WeGo docs, *survey*): Planning = Stoneshard-style action-time where positioning and prep cost ticks; Growth & Threat = hands-off resolution with reactive choices. Disk reality: Planning = seed-editor menu; **all** spatial play happens during the live phase. The dormant Planning-movement branch (projectmemory) shows this was half-intended. **Effect:** planning decisions lack spatial texture (where/what to plant is decided under real-time pressure instead); execution phase can't read as "watch your plan resolve" because you're busy farming inside it. **This is the single most consequential open design decision in the project** — half of §4 routes through it. **Fix:** §4.3 (two clean options + a cheap experiment).

### D4 · The show sputters (execution-phase spectacle)
Starter math *(survey, `FloraManager.basePhotosynthesisRatePerLeaf` = 0.1)*: 6-leaf plant ≈ 0.6 E/tick daytime, 0 at night; BasicFruit costs 20 E; a 3-slot loop + 3-tick recharge = 6-tick cycle → ≈ −16 E per loop if it fires every loop. Plants fire ~2 loops then stall; executor correctly *stays* on the unaffordable slot (`PlantSequenceExecutor` energy-fail = retry), so the visible behavior is a plant doing nothing for most of the round. **Effect:** the payoff phase — the reason the genre mashup exists — reads as idle. **Fix:** rebalance targets + stall legibility (§4.4).

### D5 · Buildcraft space is narrow and opaque
~23 gene *classes* exist in code (7 active, 3 modifier, 6 passive, ~7 payload) but only 14 authored `.asset`s *(survey)* — content gap is authoring/balance, not architecture. **One** SeedTemplate exists. Energy math dominates all build decisions (CostReduction ≈ mandatory). The quality score (`SeedQualityCalculator`) blends economy/speed/yield/defense into one number, so a legitimate glass-cannon build reads as "Poor". Trigger-lane genes (ReactiveBurst) execute on a separate track the UI never explains. No sequence reorder; combos surface only as tooltip strings. Fruit is a terminal output — payload genetics never propagate (no fruit→seed path, *survey*). **Effect:** few viable builds, poor legibility of the ones that exist, no discovery celebration. **Fix:** §4.4–4.5.

### D6 · Rewards and economy are silent
`GeneRewardSystem` inserts 1–3 random genes straight into inventory on round change — no draft, no reveal, no choice (GeneDraftSystem is design-only). Harvest yields display nothing (no numbers, no tally; `FloatingCombatText` exists unused). There is no currency, no orders, no score — nothing to want *for tomorrow*. **Effect:** the dopamine loop of roguelite acquisition is skipped entirely. **Fix:** draft ceremony + harvest report + demand layer (§4.1, §4.6).

### D7 · Doris is a timer, not a character
DorisHungerSystem is a rich event surface (hunger/state/fed/ate-plant) consumed almost nowhere; mood/personality systems are design-only; feeding is a popup chore with no reaction. **Effect:** the emotional anchor of the cozy half doesn't anchor. **Fix:** mood-lite + cravings-as-contracts (§4.6) — arguably the highest leverage/effort ratio in this document.

### D8 · Tactility is thin
Four tools, two tile interaction rules (Hoe: Grass→Tilled; WateringCan: Tilled→Wet), flat 1-tick action costs, instant all-at-once harvest, and watering's actual growth/energy multipliers may be sitting at 1.0 in the modifier manager **[verify in Editor — if so, watering is currently cosmetic]**. The one minigame is a 1.5 s toll per seed whose Perfect tier is mechanically identical to Good (`MinigameManager.cs:217–231` waters on any success; tier only changes a debug string; `PlayerActionManager.cs:191` passes `null` → default rewards). **Effect:** hands feel under-employed; skill expression exists nowhere. **Fix:** §6 (the whole minigame program).

### D9 · Systemic feedback is invisible
`GeneExecutedEvent` / `SequenceCompletedEvent` / `GeneValidationFailedEvent` fire into near-empty rooms; no wave banner; no energy-stall indicator; no combo discovery moment; night arrives as a color filter without gameplay announcement. **Effect:** the simulation is running a hidden concert. **Fix:** feedback pass (§4.7) — cheapest section in the doc.

---

## 4. Solutions

Effort scale: **S** ≤ 1 dev-day · **M** = 2–4 days · **L** = 1–2 weeks · **XL** = multi-week. Demo tags: **[DEMO-CORE]** should ship in the demo · **[DEMO-NICE]** if time allows · **[POST]** post-demo. Solo-dev buffer applies to everything (§8).

### 4.1 Give the run an arc [DEMO-CORE]

- **Finite demo run: 8 rounds → "Bloom Festival" finale.** Matches the already-recorded demo scope (6–8 authored waves, 20–40 min, *survey*). Round 8 survived = win screen: garden panorama, run stats, best plant's gene strand displayed like a recipe card (Opus Magnum solution-GIF instinct — shareable later). Effort **M** (UI + a `finalRound` check in `RunManager.StartNewRound()`; wave-list end becomes the finale trigger instead of a silent `Idle`).
- **End-of-round Harvest Report.** One modal between phases: fruits harvested (× type), energy generated vs. spent per plant, leaves lost/regrown, Doris meals, genes found. All of it is already countable from existing events (`GeneEventBus`, `OnLeafConsumed`, harvest calls) — this is a subscriber + a panel, not a system. Effort **M**. *This single screen simultaneously patches D1, D6 and D9 — highest priority item in the document alongside 4.6.*
- **Game-over screen** with the same stats + "what killed you" line + restart button (calls the existing `RestartGame()`). Effort **S**.
- **Done when:** a new player can answer "how long is a run, am I winning, and what did last round earn me" without being told.

### 4.2 Make the threat honest [DEMO-CORE]

- **Escalation curve over the 8 authored waves.** Round 1: 1 harmless herbivore (tutorial-by-contrast). Round 8: a proper siege. Author counts so that from ~round 4 the player *loses leaves unless they built defense*. The content already exists (pests eat leaves; thorns/traps/bursts/payloads all functional) — this is `WaveDefinition` authoring, not code. Effort **S–M**.
- **Pest roles, not pest counts.** Differentiate via existing systems: *Leaf-chewer* (current behavior) · *Fruit-thief* (targets `Fruit` objects — food-seek logic already exists) · *Sap-sucker* (drains plant energy — one new tick effect) · *Trampler* (moves through crops, tramples cells, ignores food — forces spatial defense). 2 new behaviors ≈ **M** each; the rest is authoring.
- **Forecast the next wave during Planning.** A banner: species icons × counts + spawn edge ("From the east at dusk: 3 fruit-thieves"). Data exists in `WaveDefinition`; Into-the-Breach kinship — perfect information makes planning *matter*. Effort **S–M**. Pairs with wind (§5.2) later.
- **Wave-list cliff fix.** Past the authored list (post-demo endless mode): repeat final wave with a multiplier instead of `Idle`. Effort **S**. [POST if the demo run is finite anyway]
- **Done when:** a zero-defense garden reliably loses plants by round 5, and the player saw it coming in Planning.

### 4.3 Resolve the phase identity (the big decision) [DEMO-CORE decision, staged execution]

Two coherent games are hiding in the current code; pick one:

- **Option A — "Commit & Watch" (recommended; matches WeGo docs and the game's name-brand fantasy).** Planning gains world verbs (till/plant/water/arrange) and a **tick budget** (e.g. 40 ticks; a `maxPlanningPhaseTicks` once existed but was removed as unimplemented — `TickConfiguration.cs:25` — so this is a ~10-line re-add; the spend path already exists because every action routes through `RequestActionTicks`). Growth & Threat shrinks to *resolution + scoped interventions*: feed Doris, emergency verbs (§6.3 Shoo), harvest windows. Planning becomes a Stoneshard/Into-the-Breach decision space ("I can re-till two beds OR relocate the trap"); execution becomes a show you mostly *read* (with §4.4 making the show worth reading).
- **Option B — "Live Garden".** Accept the current shape: Planning is renamed/reframed as the *Greenhouse* (pure seed editing), and all farming is real-tick gameplay. Then D8/D4 fixes become the priority (the live phase must feel great), and minigames become central rather than accents.
- **Cheap experiment before committing** (one sandbox afternoon): flip the `PlayerTileInteractor.cs:33` gate to allow Planning input, hack in a 40-tick planning budget (see above), hide the seed editor behind a workbench interaction, playtest 15 minutes. The feel-answer will route half of this document. Effort of experiment: **S**.
- **Done when:** you can state in one sentence what the player is doing in each phase and why they'd rather be doing the other one soon (healthy phase envy).

### 4.4 Fix the energy arithmetic & make the show legible [DEMO-CORE]

- **Rebalance target (pick numbers to taste, but state a target):** *a naked starter seed sustains ~1 BasicFruit per day-phase with zero passives*, and passives/modifiers push it to 3–4 (greed lane) or fund defense actives instead (safety lane). Levers, one of: base photosynthesis 0.1 → 0.25/leaf/tick · BasicFruit 20 E → 8 E · recharge as *earn-while-recharging* buffer. Keep night at ~0 income — night as scarcity is good texture (§5.7). Effort **S** (numbers) + **S** (spec-sheet re-verify).
- **Stall legibility.** When the executor is energy-blocked (`GeneValidationFailedEvent` already fires!), show it: dim gene icon above the plant + trickle-fill bar. A stalled plant that *shows why* is a puzzle; a silent one is a bug report. Effort **S–M**.
- **Sequence completion celebration.** `SequenceCompletedEvent` → tiny leaf-shimmer + soft chime. The plant "finishes a sentence." Effort **S**.
- **Done when:** watching a mature plant for one day-phase is informative (you can see income, spend, stall, loop) and mildly pleasurable without any player input.

### 4.5 Widen and clarify buildcraft [DEMO-NICE, staged]

- **Author the missing 9 gene assets** (23 classes exist in code; 14 authored *(survey)*). Authoring + balance, effort **M** total.
- **Second and third SeedTemplate** (e.g. Grass: 2 passive/2 sequence, fast, cheap; Canopy: 6/4, slow, expensive). Instant strategic texture — same code. Effort **S–M** each incl. visuals-lite.
- **Split the quality score** into four sub-bars (Economy / Speed / Yield / Defense) and keep the star only as flavor. Kills the "viable build reads as Trash" problem (`SeedQualityCalculator` conflation). Effort **S–M**.
- **Explain the trigger lane.** Visually separate trigger-type slots in the editor (different socket shape + "reacts, doesn't queue" tooltip). Effort **S**.
- **Combo discovery toasts (ComboDiscovery-lite).** The full designed system is [POST]; the demo version is: first time a known synergy pair executes together → toast + codex line. Synergy strings already exist in `SeedTooltipData`. Effort **M**.
- **Sequence reorder by drag** in the editor (slots are already drag-drop aware for add/remove). Effort **M**. Respect the edit-speed principle: reorder must be *faster* than remove+re-add, or skip it.

### 4.6 Doris as demand: cravings, mood-lite, ceremony [DEMO-CORE for cravings-lite]

The single best leverage in the project, because it patches **D6 (economy sink), D5 (payload purpose), D7 (character), D2 (secondary pressure)** with one mechanic that's mostly *authoring on top of existing events*:

- **Doris Cravings (contracts-lite).** Each round, Doris announces a craving via `ThoughtBubbleController` ("something SPICY… before dusk"): deliver a fruit carrying the matching payload tag (payload → `DynamicProperties` already travel through harvest → `ConsumableData`). Fulfilled = bonus reward (guaranteed rarer gene in the round's draft, §D6) + visible delight; unfulfilled = nothing punitive (she just sulks — cozy, not punishing). Suddenly payload genes have a *reason* ("why would I grow poison fruit?" → "Doris is goth"), harvest has a *customer*, and rounds have a *quest*. Effort **M** (state + UI reuse + reward hook). **Recommendation: this is the demo's second must-ship after 4.1.**
- **Mood-lite.** 3 moods driven by existing signals (fed-quality events, starvation history): Content (+small farm-wide energy %), Peckish (neutral), Grumpy (attracts +1 pest — she smells like snacks). Full DorisMoodSystem stays [POST]; this is a switch statement on events that already fire. Effort **M**.
- **Feeding ceremony.** Even without the toss minigame (§6.3), give feeding a beat: chew animation cadence + reaction bubble grade (loved/fine/refused per `DorisDefinition` diet multipliers — data exists). Effort **S–M**.

### 4.7 The cheap feedback pass [DEMO-CORE, do first]

All **S** items, mostly subscribers to events that already exist: harvest yield floaters (`FloatingCombatText.Spawn` — exists, unused for yields) · wave-incoming banner (WaveManager state changes) · night falls announcement + firefly cue · gene draft reveal SFX · tick-speed indicator with heartbeat audio at ×2+ · Doris state change stingers · plant death is currently a fade — add a leaf-scatter puff (it's the *health bar* dying; it deserves a frame of grief).

---

## 5. Possibilities not yet entertained (or under-entertained)

Things absent from all project docs *(survey confirmed)* that fit the game's bones. Verdicts are honest, not enthusiastic.

### 5.1 Deterministic Rehearsal ("ghost preview") — *the determinism dividend*
A6 gave every run a seed; `IDeterministicRandom` drives rewards and leaf-death. That makes a **Planning-phase dry-run** possible: simulate the next day for the selected seed/garden headlessly (executor + energy + weather curve are all tick-pure; exclude fauna and mark it "clear-weather rehearsal") and print: *"Day sim: 2 fruits, −4 E, stall at slot 3 around tick 41."* Text-only is enough — this is Opus Magnum's test-run button transplanted. **Why it matters:** D5's opacity dissolves; players learn energy math by reading, not dying. **Risk:** sim/live drift → mitigate by scoping to plant-internal simulation and labeling it a forecast, not a promise. Effort **L** (but the spec-sheet already estimates maturity/surplus — a mid-step is upgrading those estimates into a per-tick trace table). Verdict: **do the mid-step for demo, full ghost [POST]**.

### 5.2 Wind as the daily variable
A per-day wind vector (deterministic from RunSeed, shown in the forecast): drifts `CloudWorldEffect` positions, offsets scent radii, nudges projectiles. One float2 touching three existing systems → placement suddenly cares about *tomorrow's weather*, and no new content is needed. Effort **M**. Verdict: **strong [POST], cheap enough to prototype earlier**; pairs beautifully with 4.2 forecasting.

### 5.3 Irrigation channels — make the Hoe strategic
Water tiles already exist (`TileDefinition` water flag, refill rule); tilled tiles already exist. Add: water *flows* one tile per N ticks through connected tilled channels, wetting adjacent beds. Digging becomes routing; the watering can becomes the manual override instead of the whole system; and D8's "watering may be cosmetic" problem inverts into a spatial puzzle. Effort **L** (flow sim lite + tile states). Verdict: **best [POST] candidate for making terraforming a first-class verb**; demo can fake a taste of it via §6.3 Pour Arc.

### 5.4 Mutation on replant — bridge the fruit dead-end before cross-pollination
Fruits already carry `RuntimeGeneInstance` payloads through harvest (`HarvestedItem` → `ItemInstance.payloads`). Add a "Sow fruit" verb: planting a fruit yields its parent's sequence with **one deterministic mutation** (swap/upgrade/corrupt one gene, seeded by RunSeed + fruit id). Suddenly fruit is feedstock, greed has a second axis (eat vs. sow), and Noita-style "what did I just grow" emergence appears years before the full bee/cross-pollination system [POST-ranked in memory]. Effort **M–L**. Verdict: **the most game-changing M-sized idea here; prototype post-demo, or as the demo's single "wow" if 4.x lands early**.

### 5.5 Daily seed & score
RunSeed exists → a "Today's Garden" fixed-seed mode with a simple score (fruits × rounds survived × Doris satisfaction) is nearly free once 4.1's stats exist. Retention + streamability standard for the genre. Effort **S–M** after 4.1. Verdict: **[POST], but keep in mind while building 4.1's stat plumbing**.

### 5.6 Aggro-by-abundance (pest ecology v0.5)
Scale wave size by the garden's total leaf+fruit mass: you attract what you grow. Self-balancing difficulty, meaningful greed decisions, one formula in wave spawn count. The full scent-driven ecology (pests follow `ScentSource` gradients — the scent system already exists!) is the [POST] version. Effort **S** for the formula, **L** for true ecology. Verdict: **formula version is a sneaky-good demo knob; flag it in the difficulty pass**.

### 5.7 Night as a second loop
60/40 day/night already exists and photosynthesis already dies at night — but nothing *else* changes. Cheap additions: night-blooming genes (execute only at night — one condition on `ActiveGeneContext`), a nocturnal pest type, fireflies as harvestable light (§6.3 Firefly Jar). Verdict: **[DEMO-NICE] for one night-only gene + one nocturnal pest; the rest [POST]**.

### 5.8 Blessed/cursed genes (Inscryption texture)
Every rare gene carries a rider ("double yield; Doris refuses its fruit" · "free cast; leaks 1 E/tick at night"). Overcharge already proves the pattern (cost↑ power↑). Content multiplier with zero new systems — riders are just second effects. Effort **S per gene** at authoring time. Verdict: **adopt as an authoring *style* for the 9 missing genes (4.5) rather than a separate system**.

---

## 6. Minigames — audit, upgrade, and new concepts

### 6.1 Minigame policy (rules before content)

Every minigame in this game must obey five rules, or it will curdle a "pace-casual" WeGo game into a twitch tax:

1. **Fail-safe base action.** The underlying verb always succeeds; minigames only add bonus. (The current planting flow already gets this right — the deferred plant executes even on Miss.)
2. **Bonus-only, tier-differentiated rewards.** If Perfect ≡ Good (the current state), the skill ceiling is a lie.
3. **≤ 2 seconds, opt-in-able.** A hold-input variant of the verb bypasses the minigame (no bonus). Player agency is the anti-annoyance valve.
4. **Tick-respectful.** Either pause the tick flow during the minigame or charge its real-time as ticks *visibly*. Currently 1.5 s of live G&T = ~3 ticks of pests moving while you stare at a circle — unfair and unreadable.
5. **Graduation valve = the automation arc.** This is the elegant part: the v6 design already contains the manual→automated teaching loop (shoo rabbits by hand → Fear gene automates it). Extend that to minigames: **each minigame is the manual tier of something a gene/tool upgrade later automates at your average grade.** Minigames don't get *removed* when mastered; they get *hired away* — which is exactly the game's fantasy.

### 6.2 The existing TimingCircle ("watering") minigame — audit & upgrade pack

**As implemented** (`TimingCircleMinigame.cs`, `TimingCircleConfig.cs`, asset `000_TimingCircleConfig_Planting`): on planting (the only enabled trigger, hardcoded in `MinigameManager.Awake():63`), a world-space circle shrinks r=2→0 over **1.5 s wall-clock**. With asset radii (Good 0.35–0.7, Perfect 0.2–0.35), the timeline is: dead air 0–0.975 s → **Good window ≈ 262 ms** → **Perfect window ≈ 112 ms** → *late-Miss zone ≈ 150 ms* (r < 0.2 misses — clicking "too centered" fails) → timeout Miss. One click, Escape skips. Success = auto-water the tile via the watering-tool rule; **Perfect = Good exactly** (tier changes only a debug string, `ApplyPlantingReward`); `MinigameResult.Accuracy` is computed and **discarded**; grade sounds are unassigned (`{fileID: 0}`); it runs while ticks flow; one instance at a time; and if tile modifiers are 1.0 the watering reward itself may be mechanically nil **[verify in Editor]**.

Read as design: *a mandatory 1.5-second toll per seed, paying out a possibly-cosmetic coupon, with a fake skill ceiling and a trap zone at the center.* The bones are good — world-anchored, readable zones, fail-safe plant, flash feedback — it's the reward and pacing layer that's missing.

**Upgrade pack** (ordered; each independent):

| # | Change | Detail | Effort |
|---|---|---|---|
| U1 | **Differentiate tiers** | Good = watered. Perfect = watered + "Vigorous sprout": `startingEnergy` +25% of max (field already on `SeedTemplate`) or first recharge skipped. Show "Vigorous!" via `FloatingCombatText`. | S |
| U2 | **Use Accuracy** | Scale U1's bonus continuously by the discarded `Accuracy` value (0.5→1.0 maps 0→full). Skill smooths instead of cliffs. | S |
| U3 | **Tick fairness** | While `IsMinigameActive`, pause `ExecutionPhaseDriver` (it has pause already) — or charge a fixed 2-tick cost displayed on the circle. Pick per §4.3's outcome. | S |
| U4 | **Opt-in valve** | Tap = minigame attempt; hold-click ≈ 0.3 s = careful plant, no minigame, no bonus. Kills the mass-planting toll (10 seeds is currently +15 s of circles). | S–M |
| U5 | **Remove the center trap** | `perfectZoneInnerRadius` 0.2 → 0 (asset change): late clicks degrade to Perfect edge→center, timeout stays Miss. The current "you aimed too well" miss reads as a bug. | S |
| U6 | **Context difficulty** | Rarer seed → narrower Good band; planting on `TerrainAffinity`-preferred soil → wider band (knowledge buys dexterity slack). Extend `GetConfigForTrigger` to modulate radii. | S–M |
| U7 | **Sowing streaks** | Consecutive Good+ within a short window builds a combo counter; at ×3 all sprouts this round get a small energy gift. Turns row-planting from toll-chain into rhythm. | M |
| U8 | **Juice** | Assign the four audio fields; sprout pop scaled by tier; soil particle puff. | S |

**Done when:** planting 6 seeds in a row is something a player *chooses* to do skillfully (or skips entirely), and a Perfect visibly matters within the same round.

### 6.3 New minigame concepts

The code already reserves the hooks: `MinigameTrigger.{Watering, Harvesting, Tilling}` and `MinigameType.{StopTheBar, ReactionTest, RhythmTap}` (`MinigameTypes.cs`). Concepts below map onto exactly those, plus two systemic ones. Every entry obeys §6.1.

| Concept | Hook / type | Loop (one line) | Bonus | Valve | Effort | Verdict |
|---|---|---|---|---|---|---|
| **Pour Arc** | Watering / StopTheBar | Hold to fill an oscillating meter, release in the band → water 1–3 tiles in a line; overfill → harmless mud splash on *you* (brief slow) | Multi-tile watering stretches the can's 4 uses | Tap = single-tile safe pour | M | **Build 2nd** — gives the can a skill economy |
| **Cascade Harvest** | Harvesting / RhythmTap | With ≥3 fruits, they pop up one-by-one (~0.25 s apart); tap each in window to keep the chain; full chain = +1 bonus fruit or "fresh" tag (`nutrition_multiplier` ×1.2) | Yield becomes *visible and countable* — this minigame IS the missing harvest feedback | Hold = instant bulk harvest, no bonus | M | **Build 1st** — patches D8 *and* D9 in one stroke |
| **Row Rhythm** | Tilling / RhythmTap | After a till, a 2-beat metronome; on-beat clicks till the next tile in line at no extra tick cost | Fast bed prep rewards planning straight rows | Off-beat clicks still till normally | M | Build 3rd; synergizes with irrigation later (§5.3) |
| **Shoo!** | new trigger / ReactionTest | A pest entering the garden flashes an outline for ~0.5 s; click it → it flees 3 tiles (no damage) | The G&T phase's missing *reactive* verb; explicitly the manual tier of the Fear gene (v6 teaching loop, verbatim) | Ignorable; it's an opportunity, not a QTE | S–M | **Build with 4.2** — threat needs a hand answer before gene answers |
| **Firefly Jar** | night / drag-sweep | Sweep-drag a net near fireflies (≤3 captures); place the jar = local photosynthesis lamp for the night (reuses `FireflyManager` bonus math); dawn releases them | Night income option; peak cozy | Entirely optional side activity | M | [DEMO-NICE]; the demo's "screenshot moment" |
| **Gobble Toss** | Doris feeding / arc throw | Toss food with angle+power; mouth hit = digestion bonus (feeds future Doris-Provides); floor food lures +1 pest (real, fair cost) | Feeding ceremony + risk texture | Walk-up feeding unchanged | M–L | [POST] — wait for Doris-Provides so the bonus means something |
| **Splice Steady-Hands** | seed editor / StopTheBar | Socketing an *over-tier* gene requires a timing bar; fail = gene "bruised" (locked 1 round) | Lets low-tier seeds overreach | Never appears for normal editing | M | **Anti-recommended** for core flow — violates the edit-speed principle; keep only if strictly confined to a bonus over-tier lane, else cut |

**Frequency budget** (worth writing down as a rule): at steady state a player should meet **≤ 2 minigame moments per minute** of G&T. Cascade and Shoo are event-driven (fine); Planting and Pour are player-initiated with valves (fine); never stack two simultaneously (`MinigameManager` already enforces one-at-a-time).

### 6.4 Spice-ups (non-minigame, mostly S-effort, component-reuse noted)

1. Harvest yield floaters — `FloatingCombatText` exists, unused for yields.
2. Wave banner with pest icons + edge arrow — `WaveDefinition` data exists.
3. Energy-stall icon over blocked plants — `GeneValidationFailedEvent` already fires.
4. Sequence-complete shimmer + chime — `SequenceCompletedEvent` already fires.
5. Doris reaction bubbles on every feed, graded by diet multiplier — `ThoughtBubbleController` + `DorisDefinition` data exist.
6. Draft ceremony: 3 cards, one flips at a time (Inscryption pacing), even if the "choice" is initially accept-only — the *reveal* is the point (GeneDraft-lite, D6).
7. Night cue: firefly swirl + one-shot audio when `WeatherManager` crosses to night.
8. Tick heartbeat: soft pulse audio synced to `OnTickAdvanced`, pitch up at ×2 speed — makes WeGo time *audible*.
9. Plant death: leaf-scatter particle + half-second hitch — the health bar deserves a funeral.
10. Hover spec-mini: hovering a mature plant shows its next 3 sequence slots as tiny icons — surfaces the executor's hidden concert (D9).
11. Per-round star stamp on the Harvest Report (no leaf lost ★ / all sequences fired ★ / Doris satisfied ★) — mastery layer for free once 4.1 exists.
12. Perfect-plant streak title cards ("Green Thumb ×5") — dovetails U7.

---

## 7. Prioritization & the demo slice

Scoring: Impact on engagement (1–5) vs. effort. Demo slice = what makes a stranger's 25 minutes coherent, tense, and rewarding.

| Rank | Item | §| Impact | Effort | Demo? |
|---|---|---|---|---|---|
| 1 | Feedback pass (floaters, banner, stall icons, shimmer) | 4.7 | 5 | S | CORE |
| 2 | Harvest Report + game-over/win screens + finite 8-round run | 4.1 | 5 | M | CORE |
| 3 | Energy rebalance + stall legibility | 4.4 | 5 | S–M | CORE |
| 4 | Wave escalation + forecast banner | 4.2 | 5 | S–M | CORE |
| 5 | Doris Cravings (contracts-lite) | 4.6 | 5 | M | CORE |
| 6 | Gene draft reveal (accept-only OK) | 4.6/6.4 | 4 | M | CORE |
| 7 | Phase-identity experiment (one afternoon, then decide) | 4.3 | 5* | S→? | CORE (decision) |
| 8 | TimingCircle upgrade pack U1–U5 (+U8 juice) | 6.2 | 4 | S–M | CORE |
| 9 | Shoo! reactive verb | 6.3 | 4 | S–M | CORE (with #4) |
| 10 | Cascade Harvest | 6.3 | 4 | M | NICE |
| 11 | 9 missing gene assets (authored with §5.8 riders) + 2nd seed template | 4.5 | 4 | M | NICE |
| 12 | Quality sub-scores + trigger-lane clarity | 4.5 | 3 | S–M | NICE |
| 13 | Pour Arc + Row Rhythm | 6.3 | 3 | M | NICE |
| 14 | Firefly Jar + night-bloom gene + nocturnal pest | 5.7/6.3 | 3 | M | NICE |
| 15 | Rehearsal mid-step (per-tick trace in spec sheet) | 5.1 | 4 | M–L | NICE |
| 16 | Mutation on replant | 5.4 | 5 | M–L | POST (or the demo's wow) |
| 17 | Doris Provides · irrigation · wind · ecology · daily seed · full ghost sim | 5.x | — | L–XL | POST |

*Sequencing note:* #7 (phase experiment) should happen **first** chronologically despite ranking — its outcome tints #3, #8, #9 (tick-pause vs. tick-cost decisions). Then #1 → #3 → #4 → #2 → #5 → the rest. With solo pace + buffer, the CORE band is realistically **4–6 weeks of focused work** — it fits the "content, polish, Steam prep" phases of the late-2026 demo forecast without moving the date.

---

## 8. Risks & anti-goals

- **Twitchification.** The audience is "pace-casual, not easy-casual." Minigames must stay bonus-lane (§6.1) and the game must remain fully playable — and *winnable* — by someone who holds-to-skip every one of them. Test this explicitly.
- **Dexterity in the editor.** The edit-speed principle is load-bearing (multiple seeds edited every Planning). Any editor friction that isn't *thinking* is damage — hence the Splice minigame anti-recommendation.
- **Minigame fatigue math.** 10 plantings/round × 1.5 s is already a 15-second tax today. The valves (U4, hold-to-bulk) are not polish — they're the difference between charm and churn.
- **Balance whiplash.** 4.4's rebalance invalidates the spec sheet's warnings/estimates; re-verify `SeedTooltipData` numbers in the same pass or the UI will lie.
- **Scope gravity.** §5 is a garden of scope creep. The rule stands: POST items go to a Roadmaps doc, not into the demo branch. One exception allowed by design (§7 #16), chosen consciously.
- **Determinism erosion.** New systems (wind, mutation, cravings) must draw from `IDeterministicRandom` with the run seed — never `UnityEngine.Random` — or 5.1/5.5 die quietly.

## 9. Open questions for Milan

1. Phase identity: gut preference for Commit & Watch (A) vs. Live Garden (B) before the experiment? (§4.3)
2. Demo finale: pure survival ("survive 8 rounds") or a delivery target ("Doris's metamorphosis needs X quality meals")? The latter makes cravings the spine, not a side dish.
3. Is player starvation worth keeping at all, or should hunger pressure be Doris-only? (Two hunger clocks may be one too many for the cozy half.)
4. Watering fantasy check: is water meant to be *economy* (energy multiplier), *tempo* (growth multiplier), or *gate* (some genes need wet soil)? The answer decides Pour Arc vs. irrigation priority. **[also: verify current Wet-tile multipliers in Editor — they may be 1.0]**
5. Minigame ceiling: would you accept a run where a skilled player's gardens are ~20% stronger purely from minigame bonuses, or should the cap be lower?
6. Any appetite for the mutation-on-replant prototype (§5.4) as the demo's differentiator, accepting ~1 week of risk?

## 10. Sources & verification

Direct reads (live disk, 2026-07-05): `MinigameManager.cs` · `TimingCircleMinigame.cs` · `TimingCircleConfig.cs` · `MinigameTypes.cs` · `000_TimingCircleConfig_Planting.asset` · `TickConfig.asset` · `RunManager.cs` (100–190) · `WaveManager.cs` (120–184) · `PlayerTileInteractor.cs` (gate at :33) · `PlayerActionManager.cs` (160–198) · `GameUIManager.cs:837`. Four survey passes covered: minigames/tools/verbs, run/wave/hunger/rewards, gene/seed content + executor pacing, and all design docs — claims taken from surveys rather than direct reads are marked *(survey)*; Inspector-dependent values are marked **[verify in Editor]**. The `06_Index` extracts were treated as stale throughout (pre-A-pack); nothing in this document modifies code, assets, or the A-pack's pending Editor wiring.

---

## Next action anchor

**Run the §4.3 phase-identity experiment (one sandbox afternoon: allow Planning-phase world input, re-add a simple 40-tick planning budget, playtest 15 minutes).** Its outcome routes the tick-fairness choice in the minigame pack, the shape of the threat forecast, and half the CORE band — decide it first, then execute §7 top-down.
