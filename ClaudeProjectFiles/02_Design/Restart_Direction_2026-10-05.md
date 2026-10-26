# Restart Direction — what Bitbloom taught, and Block 1 (design lane, 2026-10-05)

Status: **closed — accepted by Milan as "recs" on 2026-10-26** → `03_Tasks/Active/2026-10_Block1_Doris_By_Dusk.md` · Opus · high · design lane (Cowork)
Point-in-time doc: not edited after it closes, except a "Superseded by …" line.

**Read:** `CLAUDE.md`, `01_Core/projectmemory.md`, `03_Tasks/Active/2026-07_Fable5_Design_Decision_Ledger.md`, `02_Design/Concepts/Phase_Identity_Final_Refinement_2026-07-20.md` (§2.4), `Commit_And_Watch_Loop_Design_2026-07-06.md`, `02_Design/Feedback_Log.md`, `03_Tasks/Roadmap.md`. From Bitbloom (read only, on disk, branch `ideation/merge`): `02_Design/Feedback_Log.md` (rules 1–44 + log 2026-10-03 → 10-05, incl. the five toys), `Truths_2026-10-04.md`, `Abracodabra_Pitch_2026-10-05.md`, `Trap_v2_2026-10-05.md`. Code spot-checked on disk for reuse claims (§3.3).

---

## 0. In five lines

1. Bitbloom ran ~10 scored playtests and 5 toys in ten days. Its most reliable signal: **watching the ecosystem react to *my* plant is the one constant fun source** (the watch-only Core-3 build had the best Fun of all, 3.5). What kills runs is **no stakes, chores, hidden rules and a grammar that does not read**.
2. That **supports** Commit & Watch (D1 → A), fixtures (D2), the hectic-feel guards, no player hunger (D4), the plant as the health bar, and Abracodabra's screen-space Seed Editor.
3. It **argues against** three planned things: the Day density metric as written (≥ 1 decision per 15–20 s), Mark & Go as Block 1 content, and ripeness windows as the demo's one new system (D8). And it exposes a hole §2.4 never had: **there is no goal and no way to lose.**
4. **Recommendation: Block 1 "Doris by Dusk"** (option 2, 19.5 / 25): enforce the invariant, move planting into Planning, take the avatar off the Day, give Doris a belly that must be filled by dusk (food reaches her automatically), telegraph waves on the map, make bites and bursts readable, end each day with a three-line ledger, and make the run winnable and losable. About 2 weeks of code-lane time.
5. Mark & Go and ripeness are **not cut**; they become Block 2 candidates, chosen by what Block 1's playtest says is missing.

---

## 1. The evidence (and how far it transfers)

Bitbloom is a side-view pixel sim in real time; Abracodabra is a top-down tile game on ticks. What transfers is **Milan's taste** (stakes, readability, chores, pace, UI); what does not transfer is sim-specific detail (water physics, aiming at pixel scale). Each item below is referenced as E1–E12 in §2.

| # | Evidence (Bitbloom, dated) | What it says about Milan |
| --- | --- | --- |
| E1 | Scoreboard (Truths): watch-only Core-3 **Fun 3.5**, the best of any build; "the one constant fun source is watching the ecosystem react to his plant: sap slowing a beetle, bite-then-burst, creatures with lives" | Watching is not the problem; an empty or unreadable watch is |
| E2 | 8d Part 3 (10-03): "fun to watch for ~40 s, then scanning the world: if there were enough things I'd be entertained" | Under-stimulation = too little *to watch*, not too few *clicks* |
| E3 | Vein bus: "not even sure what I'm losing or when the game ends"; Trap: "no end game, threat, goal or progress… a bit like an AFK clicker"; rules 13, 33, 34 | **Stakes first.** Nothing else is judged fairly without a goal and a loss |
| E4 | Hollow (10-05): "produce should reach Doris automatically (pumped through the roots at the end of the day), **not carried**"; A-1: "take the growing economy out of the player's worries" | He wants chores automated from the start, not earned away |
| E5 | Trap toy: "hectic"; rule 35 "not slow, then too fast"; Trap_v2's tuning rule: **one forced edit every 60–90 s, never two threats in one minute**, ≤ 6 fliers in minute 1, "pause on ✗" on by default | Pace-casual; the decision rate that worked is ~4× slower than D1's metric |
| E6 | 8d: "~28 rules for ~1 decision a minute" (hate); A-1: "seeds, water and sugar are too many"; BC: "too generic" shop | Few currencies, few rules, each one visible |
| E7 | "Nothing I could have done", "the same doomed day twice" (hates); rule 12 "agency needs options, not just information"; rule 26 + the ‼️ ask: **telegraph an emergence on the map itself** before the round | Every threat visible and answerable before Start |
| E8 | Vein bus: **"on bitten" more interesting than timers**; Truths §4: Noita triggers, payload-in-payload; rule 40: **bite-then-burst is the best moment** | Reactive genes > clock genes; reward at the plant |
| E9 | Rule 45 (verb before grammar); **rule 46: the Expedition's UI was the clearest** — worded chips, a "reads as" sentence under the strand, a screen-space card, edits in a paused camp. The Hollow's in-world glyphs: "no idea how that game is supposed to work" | Abracodabra's Seed Editor + Spec Sheet are the *right* pattern; add a sentence |
| E10 | A-1 aim: Agency 4, Fun 3; A-2: "kinda stopped liking the shooting"; BC hand on: +0.5 Agency/Tension, Fun 1.5 → 2 | Hands-on agency spikes, then fades. A spice, not a foundation |
| E11 | Loves: a spare leaf as a hit point; the 3-seed starting pick (rule 38); keyword tooltips (44); creature hovers "doing / eats / turns after" (39) | Already Abracodabra pillars or cheap adds |
| E12 | Trap_v2 scored "Abracodabra in the pixel sim" highest (20 / 25); the Hollow toy failed on **readability**, not on Doris; Milan: "wants a balance between economy and defence" | Doris + food-vs-defence is the structure he keeps returning to |

**One caveat stated plainly:** Milan's taste moved fast this week (aim loved → aim tired → a skill-shot tongue idea, all within two days). Don't rebuild Abracodabra around the latest toy. Use the patterns that held across ≥ 3 builds (E1, E3, E5, E6, E7); treat single-toy remarks as hints.

---

## 2. Part A — adversarial audit of Abracodabra's planned choices

Legend: **▲ supported** · **▼ argued against** · **◆ revise** (keep the goal, change the means). Never colour alone: the word carries the verdict.

| Planned choice (where) | Verdict | Evidence | What changes |
| --- | --- | --- | --- |
| **D1: phase identity A, Commit & Watch** | ▲ supported | E1, E5, E10, rule 25 ("thinking and programming at the core") | Stays. Only with stakes (E3) and a full watch (E2) |
| **D1 exit metric: ≥ 1 meaningful decision per 15–20 s of Day** | ▼ against | E5 (60–90 s per forced edit worked), E2, E10 | **Replace** with a two-part test: *Day* = ≥ 1 readable, attributable event at your plants per ~10–15 s (bite, burst, kill, Doris eats, fruit set) and ≤ 1 threat starting per minute; *Planning* = ≥ 3 real choices with something to choose against. Plus the "another run?" answer |
| **Under-stimulation as risk #1** | ◆ revise | E1, E2 | The risk is real; the answer is content and legibility (bites, bursts, telegraphed arrivals, thought bubbles), not lean-in prompts |
| **Hectic-feel guards** (§2.3: ≤ 1 simultaneous prompt, auto-pause, Space/Tab in HUD, telegraphed windows) | ▲ supported | E5, E7 | Stay. Add: wave telegraph on the map in Planning; never two threats starting in one minute; small round-1 waves |
| **D6: auto-pause on first occurrence, plant death, wave spawn** | ▲ supported | E5 ("pause on ✗" default on, day 1) | Stays |
| **Mark & Go** (§2.1, decision 2026-07-20) | ▼ against *for Block 1* | E4, E10, E6; cost | Its job is lean-in chores (harvest, water, forage) — the category Milan just asked to automate. It also costs BFS, a queue, ETA preview, flag pins and cancel. **Defer to a Block 2 candidate**, built only if Block 1's playtest says "I want to do something during the Day" |
| **D8: ripeness windows as the demo's one new system** | ▼ against | E4, E8, E3 | A timing chore with a Perfect/Good split (and it needs the U1 tier split first). **D8 reopened**, decided after Block 1. Likely rival: **trigger genes** (On Bitten, At Dusk) — reactive strand sentences; `ReactiveBurstGene` already exists in code |
| **Timing asymmetry pillar** (attention early = Perfect, automation late = Good) | ▼ against | E4 | Parked with ripeness. Hands-off must be the whole game; the ceiling comes from better plans, not faster clicks |
| **D2: automation = fixtures, no pathing** | ▲ supported | E4 | Stays. But the Harvest Basket existed to clean up after ripeness; with ripeness parked, the first "fixture" is simply **food reaching Doris on its own** |
| **D3: Doris Bowl ships with Cravings** | ▲ supported (later) | E12, E3 | Block 1 gives Doris a visible belly and automatic supper; Bowl + Cravings are the Block 2+ layer that turns provisioning into a choice |
| **D4: no player hunger** | ▲ supported | E6 | Stays. Note: player starvation is today the *only* path to `GameOver` (`RunManager.HandlePlayerStarvation`); Block 1 replaces it |
| **D5: budget verbs-only by default** | ◆ revise | E6 | Default stands, but **no budget at all in Block 1**: ticks would be a third currency beside energy and food. Planning verbs are free; scarcity = seeds, tiles, genes. Revisit only if Planning becomes "do everything every morning" |
| **§2.4 item 2: sun-arc with four beats** | ◆ revise | Rule 16 (UI teaches itself) | Keep the *phase banner + "Now:" line*; the four beats and the sun-arc are presentation for later |
| **D7: 3 tiers, corner-gem marker** | ▲ supported | Rule 41 (shape, not hue) | Stays |
| **D9: Doris Provides is the gene economy** | ▲ supported | E12 (V2's pellets) | Stays; not Block 1 |
| **Planning-tick invariant** | ▲ supported | E7 (fairness) | Non-negotiable, Block 1 batch 1 |
| **Pillar "manual to automated"** (pitch: forage, feed, shoo by hand first) | ◆ revise | E4 | Keep the arc, change the manual tier: it must be a *decision* (where a scarecrow plant goes), never a chore (carrying fruit) |
| **The plant is the health bar** | ▲ supported | E11 | Stays |
| **Seed Editor + Spec Sheet in screen space** | ▲ supported | E9 | Stays. Add a one-line **"reads as"** sentence under the strand (cheap, Block 1 cut-list item) |
| **F1 buffer model** (towards a freer strand) | ▲ supported in direction | E8, E9 (free order worked in the Expedition) | Not Block 1 (size). Precondition unchanged: asmdef split + golden-run harness first |
| **Today's build: WASD gardener, planting and harvesting only during Growth & Threat** (`GardenerController`, `PlayerTileInteractor` gated to G&T) | ▼ against | Abracodabra rule 1, E4 | Block 1 moves till / plant / water into Planning and takes avatar control off the Day |
| **Waves from entry points** (vs Bitbloom's residents) | ◆ revise | E7, rule 20 | Keep waves (top-down TD), but show each one on the map before Start Day: entry marker, species, count, when |
| **B-rev Tick Ledger** (fallback) | ◆ parked | — | Bitbloom has no evidence for it; Milan's "calmer clock" there was real time *with pause*, which A already has. Stays parked |

**The one hole the plan never had:** §2.4 builds an attention economy before the game has a goal or a loss (audit H1, H2). E3 is the most repeated lesson in Bitbloom's log: every toy without a goal was judged "AFK clicker" regardless of its mechanics. Block 1 must end in something that can be won and lost.

---

## 3. Part B — the first build, re-validated

### 3.1 §2.4 items 1–7, one by one

| §2.4 item | Verdict | Goes to |
| --- | --- | --- |
| 1 Planning-tick invariant | **keep** | Block 1, batch 1 |
| 2 Sun-arc day, four beats, phase HUD | **trim** to banner + "Now:" + Space/Tab hint | Block 1; sun-arc → later |
| 3 Mark & Go v0 | **defer** | Block 2 candidate |
| 4 Ripeness windows + U1 tier split | **defer**, D8 reopened | Block 2 candidate |
| 5 Harvest Basket | **replace** with automatic supper for Doris | Block 1 |
| 6 End-of-day Report with causes | **keep, promote** to a three-line dusk ledger | Block 1 |
| 7 Budget meter (verbs-only) | **cut** from Block 1 | Roadmap |
| *new* Verify growth + feeding, fix the double plant action | **add** (Roadmap #9) | Block 1, batch 0 |
| *new* Planting in Planning, no avatar in the Day | **add** | Block 1 |
| *new* Goal and loss: Doris's belly by dusk, a 5-round run | **add** (audit H1/H2) | Block 1 |
| *new* Wave telegraph on the map | **add** (E7) | Block 1 |
| *new* Readable bites and bursts with causes | **add** (E1, E8) | Block 1 |

### 3.2 Three options for Block 1

**Option 1 — "A proof of concept as planned."** §2.4 items 1–4 + 6: invariant, sun-arc HUD, Mark & Go v0 (BFS, queue cap 3, ETA preview, flag pins, cancel), ripeness windows with the Perfect/Good split, report.
- For: follows the Ledger; tests Mark & Go, the most novel idea.
- Against: no goal or loss; leans on the lean-in chore loop E4 argues against; the heaviest build (pathing + queue + tier split). Playable but unjudgeable: a "no" would not say whether the loop or the missing stakes failed.

**Option 2 — "Doris by Dusk" (stakes-first Commit & Watch).** *Recommended.*
- Planning: till, plant, water and edit genes, all free, time frozen; waves shown on the map (entry marker, species, count, when).
- Day: hands off. Space / Tab, hover, auto-pause per D6. The gardener stands at her bench (cosmetic).
- Doris: a belly of N pips over her head. It drops on a fixed tick cadence through the Day. At dusk, **supper**: every fruit still on a living plant goes to her automatically (fruit pops off and arcs to her), one pip per fruit's nutrition. If her belly hits 0 during the Day she eats one plant (existing starving path, mercy cap 1 a day), and the ledger names it.
- Run: 5 rounds, each hungrier (+1 pip needed) and with a bigger wave. **Lose:** Doris goes to bed at 0 after supper, or no living plant at dawn. **Win:** survive round 5.
- Dusk ledger, three dry lines: what Doris ate, what the pests ate (by species), what you lost and why.
- Readability: every bite shows a leaf pop plus a "−1 leaf · slug" float; every gene kill names its gene; first wave and first plant death auto-pause.
- The core tension is the pitch's own: food plants feed Doris, defence plants keep the pests off them, tiles are few (E12).

**Option 3 — "Bitten" (reactive grammar first).** Invariant + planting in Planning + a new **trigger** gene type (On Bitten, At Dusk) that opens a sentence in the strand + the "reads as" sentence + a minimal stake (Doris belly, no ledger, no telegraph).
- For: goes straight at Bitbloom's "weakest layer is the build grammar" (E8, E9); the most Noita-like satisfaction.
- Against: touches `PlantSequenceExecutor` and the gene types right before F1 rewrites the strand (rework risk); thinner stakes and readability; the gene system is not what's unproven in Abracodabra — the loop is.

### 3.3 Scores (1–5; build cost and reuse: 5 = cheapest / most reuse)

| | Agency | Readability | Tension | Build cost | Reuse | **Total** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 · A PoC as planned | 3 | 2.5 | 2.5 | 2 | 3 | **13** |
| 2 · Doris by Dusk | 3 | 4 | 4 | 4 | 4.5 | **19.5** |
| 3 · Bitten | 4 | 3 | 3 | 2.5 | 3.5 | **16** |

Why option 2 reuses so much (checked on disk 2026-10-05): `DorisController` (`OnDorisAtePlant`, `OnDorisFed`, starving → `TryEatNearbyPlant`), `DorisHungerSystem`, `WaveManager.StartWaveForRound`, `RunManager` (`RunState.GameOver` exists, `RestartGame`), `FloatingCombatText`, `ThoughtBubbleController`, `ExecutionPhaseDriver.SetPaused`, `HarvestableTag` on fruit, `PlayerTileInteractor` (only its G&T gate changes). New code: a belly readout, the supper step, the ledger overlay, the win/lose overlay, entry-point telegraph markers. No pathing, no new gene type, no tier split.

**Recommendation: option 2, "Doris by Dusk".** It is the cheapest build that can actually *fail* in an informative way: if a goal, a loss and a readable watch still bore Milan, the problem is the loop and Mark & Go / trigger genes are the next levers, chosen with evidence. If it works, it is the demo's spine. It also gives a data point before Bitbloom's gate (11-07 / 11-14), where Trap_v2's "Abracodabra in the pixel sim" will compete with this very loop.

### 3.4 Block 1 preview (the full pack is written after "recs")

Order of batches, cut from the bottom: **0** verify growth and feeding, fix the double plant action · **1** invariant (Planning ticks move only the action economy) with an on-screen check · **2** planting in Planning, no avatar in the Day · **3** Doris's belly, supper at dusk, win / lose over 5 rounds · **4** wave telegraph on the map · **5** readable bites and kills with causes, D6 auto-pauses · **6** dusk ledger + phase banner · **7** (cuttable) "reads as" sentence under the strand · **8** (cuttable) Shoo: click a pest, it flees, 10-s cooldown — the one Day lever, to test E10. Estimate ≈ 8–10 code-lane sessions (≈ 2 weeks at Milan's pace). The asmdef split + golden-run harness (Roadmap #1) moves to Block 2, still before F1.

**Block 1 is done when:** a 5-round run plays start to finish with hands off during every Day; it can be won and lost, and the screen says why; every wave was visible on the map before Start Day; no plant changed during Planning; Milan plays two runs on the same seed and fills the playtest sheet.

---

## 4. Part C — what to look for in the old claude.ai Abracodabra project

The repo already holds the 2025 KB, the 2026-07 Cowork era and a chat digest — but the digest says outright that **claude.ai web chats were not included**. Look for, and paste (or just list the names first and I'll say which):

- [ ] **The project-knowledge file list.** Paste the names only; I'll diff them against the repo.
- [ ] **The original "WeGo felt cortisol-heavy" remark** (our Feedback_Log rule 2 flags its wording and date as missing).
- [ ] **Doris's character spec:** species and look, personality lines, mood states, the cravings list, the digestion → seed-pellet economy ("Doris Provides"). The repo has names (`DorisMoodSystem`, `DorisDigestionSystem`) but I found no full design for them.
- [ ] **Any playtest notes on the WASD version** — what exactly felt "finicky and hectic" (the 2026-07-20 verdict behind rule 1).
- [ ] **Decisions made in claude.ai after 2026-07-20** on phase identity, Mark & Go, the budget or D1–D10.
- [ ] **The pitch's source** (Bitbloom's `Abracodabra_Pitch_2026-10-05.md` is in Bitbloom only; if it was written in claude.ai, its source may carry more).
- [ ] **Art and icon work:** the gene-icon sheet / the "23 icons" list behind D7, Visual Genome sketches, palette notes.
- [ ] **Gene numbers or balance tables** not in `Gene_Systems_Deep_Dive.md` or the two gene catalog docs.
- [ ] **Project custom instructions**, if they differ from `99_Archive/2026-07_Cowork_Era/project_instructions.md`.

Anything not found is fine: the repo stands as the source of truth either way.

---

## 5. Questions for Milan (answer "recs" to accept all recommendations)

| # | Question | Options | Rec |
| --- | --- | --- | --- |
| Q1 | Block 1 | 1 A PoC as planned · 2 Doris by Dusk · 3 Bitten | **2** |
| Q2 | Replace D1's Day metric with the two-part test in §2 (events at your plants per 10–15 s; ≥ 3 real choices per Planning) | yes · keep 15–20 s decisions | **yes** |
| Q3 | Mark & Go: defer to a Block 2 candidate | defer · build in Block 1 · drop | **defer** |
| Q4 | D8 reopened (ripeness vs trigger genes), decided after Block 1's playtest | reopen · keep ripeness | **reopen** |
| Q5 | No tick budget in Block 1 (D5 default stays for later) | none · verbs-only now | **none** |
| Q6 | Food reaches Doris automatically at dusk (no carrying, no feed popup in the Day) | auto supper · Bowl now · manual feed | **auto supper** |
| Q7 | Lose = Doris at 0 after supper, or no living plant at dawn; win = survive round 5 | as written · tune later | **as written** |
| Q8 | Shoo (one Day click, 10-s cooldown) as the last, cuttable batch | include · leave out | **include, cuttable** |
| Q9 | asmdef split + golden-run harness move from Block 1 to Block 2 (still before F1) | move · keep first | **move** |

---

## Things to consider

- **Transfer risk.** Bitbloom's lessons are about Milan's taste; they generalise. Its sim lessons (water, aiming, pixel scale) do not, and I have not used them.
- **The two projects may be converging.** Trap_v2 recommends "Abracodabra in the pixel sim" for Bitbloom. If Bitbloom adopts it at the gate, the same design runs in two engines and one should fold or the two should split the space clearly (Abracodabra = plan and watch, top-down, ticks; Bitbloom = sim and skill). Block 1 is sized to give that gate a real data point.
- **Taste is moving fast.** Five toys in a day, and the latest note points toward a skill-shot tongue. Abracodabra's rule 1 (no piloting during a running clock) still stands; aiming is a different thing but E10 says it fades. Don't import it here.
- **Supper at dusk makes Doris's belly a forecast.** The player can count fruit on the plants during the Day and know whether supper will be enough: a vigil with a number, the thing that made the Trap tense (E5). Keep it visible on her, in pips, never as a bar alone.
- **Hands-off Day is the experiment.** If Block 1's Day feels empty even with stakes, the levers in order: shorter Day → denser, telegraphed waves → Shoo → Mark & Go. Don't jump to Mark & Go first.
- **The invariant without tests.** With the harness moved to Block 2, batch 1 proves the invariant with an on-screen check (plant state before/after a long Planning). Weaker than a test, good enough to play on.
- **Removing WASD from the Day** touches some of the 36 legacy `Input.*` sites; the Input System migration stays out of scope.
- **Perfect ≡ Good** stops mattering while ripeness is parked; the warning stays in the Ledger for when it returns.
- **Feedback_Log:** Bitbloom rules 12, 13, 26, 33, 35, 40, 45 and 46 bear on Abracodabra and are not yet copied into its cross-project rules. Copy them with the task pack.
- **This doc lives in `02_Design/`** as asked; by CLAUDE.md §4 a dated pass would normally sit in `02_Design/Concepts/`. Move it with `git mv` if you prefer.

---

**Next action:** Milan answers Q1–Q9 (or "recs"). Then, in this design chat: write `03_Tasks/Active/2026-10_Block1_Doris_By_Dusk.md`, add the decisions to projectmemory's Decision log, copy the Bitbloom rules into `Feedback_Log.md`, and update `claude/abracodabra_status.md`.
