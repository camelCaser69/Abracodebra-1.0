# Minigames & Mechanics — Full Weighted Re-evaluation

**Date:** 2026-07-08 · **Status:** CONCEPT RESEARCH ONLY — nothing implemented; no code or assets changed. · **Home:** `02_Design/Concepts/` · **Builds on:** `Gameplay_Engagement_Research.md` (2026-07-05 — the D1–D9 diagnosis and §6.1 minigame policy remain canonical), `Commit_And_Watch_Loop_Design.md` (Rev 2), `UI_Systems_Research.md` (2026-07-08).

**This document SUPERSEDES the §7 priority table of `Gameplay_Engagement_Research.md`.** All old concepts are re-scored here alongside new ones, under a demo-first weighting and the working assumption that §4.3 resolves toward **Option A (Commit & Watch)** — Milan's stated gut pick, pending the experiment. Where a score would flip under Option B, it's flagged.

---

## 0. TL;DR

- The **master ranking (§5)** re-scores 30+ items (old + new) with an explicit formula. Top band: feedback pass, aggro-by-abundance formula, early-clear tempo bonus, blessed/cursed gene riders, energy rebalance, TimingCircle pack, Shoo!, Doris Cravings, pest drops, run arc.
- **Five genuinely new concepts** enter the roster: **Pruning Snip** (the plant-is-the-health-bar maintenance verb), **harvest ripeness windows** (the intervention-window generator Commit & Watch needs), **pest drops** (defense becomes a greed lane), **garden-as-fail-state** (substitutes player starvation), and **Graft Splice** as a world verb (mid-run buildcraft without editor friction).
- **Five substitutions recommended** (§4): cut the editor Splice minigame, replace player starvation with garden-wipe, replace the silent wave-list cliff with the finale, replace pure timer round-end with an early-clear bonus window, and fix Perfect≡Good before shipping any new minigame.
- Comparable-game research (§2) mostly *confirms* the existing direction and sharpens three points: finite runs beat endless (Against the Storm ends runs "before steamrolling turns into boredom"), tier-differentiated minigame rewards are load-bearing (Stardew's Perfect = 2.4× quality multiplier), and half-integrated genre mashups are the #1 critical complaint pattern (Cult of the Lamb).
- Sequencing is unchanged: **the §4.3 phase experiment still gates everything.** Scores here assume Commit & Watch; re-read §5's flip-flags if the experiment says otherwise.

---

## 1. Scoring model (read before arguing with the table)

Every item gets:

| Axis | Range | Meaning |
|---|---|---|
| **Impact** (I) | 1–5 | How much it moves engagement for a demo player |
| **Reach** (R) | 0–4 | How many of the nine diagnosed shortcomings (D1–D9) it patches |
| **Fit** (F) | 1–5 | Identity fit: cozy-dark, WeGo commit-and-watch, plant-is-the-health-bar, edit-speed principle, determinism |
| **Effort** (E) | S=1 · S–M=1.5 · M=2 · M–L=3 · L=4 · XL=7 | Solo-dev days-ish, buffer included |
| **Demo mult** (D) | ×1.5 demo-shippable · ×1.0 demo-stretch/gated · ×0.5 post-demo | The demo-first weighting |

**Score = (2·I + F + R) × D / E**

Honest caveats: the formula deliberately favors cheap high-fit items (that's what demo-first *means*); scores are a conversation tool, not gospel — anything within ~2 points is a tie; Reach counts diagnosed problems patched, so pure-new-content items (genes, templates) score structurally lower than they feel; and effort estimates carry the usual solo-dev ±50%.

---

## 2. What the comparables actually teach (research digest)

Fresh web pass (2026-07-08) across the touchstone-adjacent space. Only the transferable lessons, no tourism:

**Thronefall (TD/builder hybrid, 20–30 min sessions).** Day phase: unlimited thinking time, player *chooses* when to trigger night. Night: minimal direct control alongside automated defenses. Lessons: (a) the player-triggered phase transition is load-bearing for a feeling of consent — Abracodabra already has this (end-planning button); keep it sacred even after a Planning tick-budget lands (budget caps *actions*, never forces the transition); (b) its minimalist verb set (build, upgrade, walk, one attack) proves few verbs + strong escalation beats many shallow verbs — supports the "fewer, deeper minigames" stance below.

**Against the Storm (cozy-dark city roguelite).** Its two genius moves: the **orders system** (external demand — the Queen wants X — turns production into purpose) and **ending runs before mastery becomes boredom** (win at 18 reputation and the game *sends you away*). Direct transfers: Doris Cravings IS the orders system at demo scale — this research upgrades its confidence, not just its priority; and the finite 8-round run (old §4.1) is re-confirmed as CORE — "the garden is your avatar" framing also independently validates plant-is-the-health-bar.

**Stardew Valley fishing (the genre's reference minigame).** Why it survived a decade of "fix fishing" forum threads: skill progression *widens the input window* (bobber bar grows with level), Perfect catches pay a **2.4× multiplicative quality bonus**, and gear (tackle/bait) lets knowledge substitute for dexterity. Transfers: U1/U2 (tier differentiation + accuracy scaling) are not polish, they're the entire point; U6 (context difficulty — better soil = wider band) is Stardew's tackle system re-derived; a *progression* hook (minigame windows widen as related mastery grows) is currently missing from the whole Abracodabra minigame program → new item **N6**.

**Cult of the Lamb (the cautionary tale).** Recurring critical verdict: roguelite half and management half are both shallow and *barely feed each other* — "not greater than the sum of its parts." The integration test for every Abracodabra mechanic: **does the farming half change decisions in the defense half, and vice versa, within the same round?** Items that fail this test (standalone side activities) score lower here than in the old doc — this demoted Firefly Jar and Gobble Toss.

**Niche (genetics survival).** Real-genetics legibility works because every gene's phenotype is *visible on the creature*. Re-confirms Visual Genome as the right call; adds the note that recessive/hidden genes are where Niche players report confusion — if mutation-on-replant ships, mutations must be *announced*, never silent.

**Prune / plant-care cozies (Garden Life, Strange Horticulture).** The under-used verb in the whole plant-game space is **shaping** — cutting away to direct growth. Nobody in the farming-roguelite lane owns it. This seeds the strongest new minigame concept below (Pruning Snip): pruning is simultaneously care (cozy), triage (dark), and economy (energy redirection) — a rare three-way identity fit.

---

## 3. New concepts (not in the 2026-07-05 doc)

All obey minigame policy §6.1 (fail-safe base verb, bonus-only tiers, ≤2 s, valve, graduation-to-automation). Graduation targets named per the v6 teaching loop.

### N1 · Pruning Snip — the missing maintenance verb ★ best new idea
**Loop:** Damaged/decayed leaves (post-pest-bite, post-storm) linger on the plant, visibly browned. They leak −0.05 E/tick and count toward aggro-by-abundance (they *smell*). Click a decayed leaf → snip. A ~0.5 s "clean cut" timing tick (StopTheBar micro-variant): hit = leaf drops as **compost matter** (feeds N5/K later; for demo: small energy refund), miss = still removed, no bonus.
**Why it's strong:** gives "the plant is the health bar" its *nursing* verb — damage currently either kills a leaf or doesn't exist; a lingering-wound state creates visible triage and makes pest attacks legible after the fact (D2, D8, D9). It's care-as-gameplay: peak cozy-dark.
**Valve:** hold-click = snip without minigame. **Graduation:** a `SelfPruning` passive gene automates it at average grade (exactly the shoo→Fear-gene arc).
**Cost:** one new leaf state (`Decayed`) on the existing leaf model + click handler + micro-timing reuse of TimingCircle code. Effort **M**.
**Done when:** after a pest wave, a player can *see* what was hurt and spend 10 satisfying seconds nursing the garden — and later meet a gene that does it for them.

### N2 · Harvest ripeness windows — the intervention-window generator
**Mechanic (not a minigame):** fruit matures → **Ripe** (tick window, e.g. 15–25 ticks) → **Overripe** (falls, ferments on the ground; attracts +pests via scent; Doris will still eat it but "meh" grade). Harvesting in the Ripe window = full value; early = reduced; fermented = pest bait / compost only.
**Why it's strong:** Commit & Watch needs *scheduled reasons to lean in* during resolution (named risk #1: day under-stimulation — this is the direct answer, density is tunable via ripening spread). It's also exactly what the **Harvest Basket fixture** automates ("acts late → Good" timing asymmetry from Rev 2 — the fixture catches fruit at window-end; attentive players beat it). Integration test: passes both ways (defense protects ripe windows; ripe timing shapes what you plant).
**Risk:** stacked with Doris hunger + waves it can overload attention — tune window generosity first, shrink later. Effort **M** (fruit state machine + scent hook + basket interplay). Flip-flag: under Option B (Live Garden) this becomes even MORE core.

### N3 · Pest drops — defense becomes a greed lane
**Mechanic:** killed pests drop something real: *chitin* (1–2 per kill → round-end bonus currency for the draft: +1 reroll or upgrade one drafted gene's tier) and occasional *manure* (instant fertilizer: +wet-equivalent on one tile). Trampler-class drops more; fruit-thieves drop stolen fruit back.
**Why:** right now defense genes only *prevent* — pure insurance is psychologically weak (players under-buy insurance in every genre). Drops make thorns/traps *productive*, which widens buildcraft (a "hunter garden" archetype appears for free) and makes waves something a greedy player can *want*. Patches D2+D5+D6 with one loot table. Effort **S–M** (drop spawn on existing death events + pickup reuse). Determinism: roll from `IDeterministicRandom`.

### N4 · Garden-as-fail-state (substitution for player starvation)
**Substitution:** remove the player-starvation game-over (or set `playerDeathEnabled` false by default). New fail state: **all plants dead AND no plantable seed in inventory** = run over ("The garden is gone."). Player hunger, if kept at all, demotes to a soft debuff (slower walk when hungry — eat to fix, never die).
**Why:** two hunger clocks is one too many (old doc open question 3 — this is the recommendation). Player starvation punishes the *cozy* activity loop; garden-wipe aligns the fail state with the game's own thesis — the plant is the health bar, so the garden should be the life bar. Against the Storm's "the city is your avatar" is the same move.
**Cost:** wipe-detection (plants==0 && seeds==0), a mercy rule (Doris's 1-plant/day cap already specced in Rev 2 guards the spiral), game-over screen reuse from 4.1. Effort **S–M**.

### N5 · Graft Splice — world verb, not editor verb
**Loop:** late-run tool (round 5+ unlock): select mature plant A → "take cutting" (costs A one leaf) → apply to mature plant B → 1.5 s steady-hands bar → success copies ONE gene from A's sequence into B's first empty slot *for the rest of the run* (runtime state only, never the template); fail = cutting wilts, leaf still spent.
**Why:** mid-run buildcraft without touching the editor (the old Splice concept died on the edit-speed principle — this respects it by living entirely in the world, on plants, during play); creates "my two best plants had a baby" stories. **Post-demo lean:** overlaps mutation-on-replant's niche; ship at most one of the two in the demo, and mutation-on-replant is the stronger candidate. Effort **M–L**.

### N6 · Mastery-widens-the-window (minigame progression hook)
**Mechanic:** per-minigame hidden mastery counter (times played × average grade) gently widens the Good band (cap ~+40%). Stardew's core trick, transplanted.
**Why:** currently minigame difficulty is static forever — mastery has no arc, so long-run players feel tolls, not skills. Cheap because `GetConfigForTrigger` already exists as the modulation point (U6 uses the same seam). Effort **S**. Note: widening must never make Perfect *automatic* — cap before that.

### N7 · Early-clear tempo bonus (round-end substitution, scoped)
**Substitution:** the round still ends on the day timer (the sun-arc is the WeGo covenant — don't break it), but if the wave is fully cleared early, the remainder of the day is announced as **Quiet Time**: a banner, +1 craving-fulfillment window, harvest/prune verbs get a small grace (e.g. Ripe windows freeze). The *reward for winning fast is a calmer, richer tail* — not a skipped day.
**Why:** pure-timer rounds make victory feel unacknowledged (D1/D9); skipping time would fight the day/night texture and Doris's tick-driven hunger. This is ceremony + 2 small rules, effort **S**.

### N8 · Preserves bench (Mortar & Pestle) — economy sink, post-demo
Surplus fruit → grind (RhythmTap) → one-shot consumables: repellent puff (1-tile pest fear, 20 ticks), energy tonic (+15 E to one plant), Doris treat (craving wildcard). Gives harvest a third sink (eat / feed / craft) and payload genes a second customer. **Post-demo:** needs an item-crafting surface the demo doesn't have; Doris Cravings must land first or it cannibalizes her demand role. Effort **M–L**.

---

## 4. The substitution board (mechanics to improve or replace)

| # | Current mechanic | Verdict | Substitute / improvement | Cost |
|---|---|---|---|---|
| S1 | Perfect ≡ Good in TimingCircle | **Fix before any new minigame ships** | U1+U2 (tier bonus + accuracy scaling) — Stardew's 2.4× lesson: no tier payoff = fake skill ceiling | S |
| S2 | Player starvation as the only game-over | **Substitute** | N4 garden-as-fail-state; hunger → soft debuff or cut entirely | S–M |
| S3 | Wave list ends → silent empty rounds | **Substitute** (already specced) | Finite 8-round run + Bloom Festival finale (old 4.1); endless mode post-demo repeats final wave ×mult | M |
| S4 | Round ends on timer regardless of outcome | **Improve** | N7 Quiet Time — acknowledge early clears without skipping the day | S |
| S5 | Instant bulk harvest, invisible yields | **Improve** | Cascade Harvest (old 6.3) + yield floaters; N2 ripeness gives harvest *timing* meaning | M |
| S6 | Watering possibly cosmetic (multipliers may be 1.0) | **Decide the fantasy, then fix** | Answer old open-question 4 (economy vs. tempo vs. gate). Recommendation: **tempo** (growth multiplier) for demo — cheapest to read visually; gate-genes later. **[verify multipliers in Editor]** | S |
| S7 | Quality star conflates 4 axes | **Substitute** (already specced) | Four sub-bars, star as flavor (old 4.5) | S–M |
| S8 | Rewards insert silently | **Substitute** (already specced) | Draft reveal ceremony, accept-only OK (old 6.4.6) | M |
| S9 | Splice Steady-Hands (editor minigame) | **CUT** | Violates edit-speed. N5 Graft Splice covers the fantasy in the world instead | 0 |
| S10 | Scent-trail scouting idea (make forecast earned) | **Rejected in favor of free forecast** | Into the Breach teaches: perfect information IS the game. Don't paywall planning info behind a chore | 0 |
| S11 | Static minigame difficulty forever | **Improve** | N6 mastery widening + U6 context difficulty | S |
| S12 | Damage is binary (leaf exists or doesn't) | **Improve** | N1 Decayed leaf state + Pruning Snip — makes past damage visible and actionable | M |

---

## 5. MASTER RANKING (supersedes Gameplay_Engagement_Research.md §7)

Assumption: §4.3 → Commit & Watch. Formula from §1. Flip-flags: 🅱 = score changes under Option B.

| Rank | Item | Origin | I | R | F | E | D | **Score** | Demo band |
|---|---|---|---|---|---|---|---|---|---|
| 0 | **§4.3 phase experiment (one afternoon, A/B toggle)** | old 4.3 | — | — | — | 1 | — | **GATE** | FIRST, chronologically |
| 1 | Feedback pass (floaters, banners, stall icons, shimmer, funeral) | old 4.7 | 5 | 3 | 5 | 1 | 1.5 | **27.0** | CORE |
| 2 | Aggro-by-abundance formula (wave size scales with garden mass) | old 5.6 | 4 | 2 | 5 | 1 | 1.5 | **22.5** | CORE (difficulty knob) |
| 3 | Blessed/cursed riders as authoring style for new genes | old 5.8 | 3 | 1 | 5 | 1 | 1.5 | **18.0** | CORE (style rule, folds into #12) |
| 4 | N7 · Quiet Time early-clear bonus | **new** | 3 | 2 | 4 | 1 | 1.5 | **18.0** | CORE |
| 5 | Energy rebalance + stall legibility | old 4.4 | 5 | 2 | 5 | 1.5 | 1.5 | **17.0** | CORE |
| 6 | TimingCircle pack U1–U5 + U8 (S1 fix) | old 6.2 | 4 | 2 | 5 | 1.5 | 1.5 | **15.0** | CORE |
| 7 | Shoo! reactive verb 🅱(rises) | old 6.3 | 4 | 2 | 5 | 1.5 | 1.5 | **15.0** | CORE |
| 8 | N6 · Mastery-widens-the-window | **new** | 3 | 1 | 5 | 1 | 1.5 | **15.0**¹ | CORE (rides #6) |
| 9 | Doris Cravings (contracts-lite) — confidence ↑ post-research | old 4.6 | 5 | 4 | 5 | 2 | 1.5 | **14.3** | CORE |
| 10 | N3 · Pest drops (chitin/manure) | **new** | 4 | 2 | 4 | 1.5 | 1.5 | **14.0** | CORE |
| 11 | Run arc: finite 8 rounds + Harvest Report + end screens (S3) | old 4.1 | 5 | 3 | 5 | 2 | 1.5 | **13.5** | CORE |
| 12 | Wave escalation + pest roles + free forecast (S10) | old 4.2 | 5 | 2 | 5 | 2 | 1.5 | **12.8** | CORE |
| 13 | N1 · Pruning Snip + Decayed leaf state (S12) | **new** | 4 | 3 | 5 | 2 | 1.5 | **12.0** | CORE-if-room² |
| 14 | N2 · Harvest ripeness windows 🅱(rises to top-5) | **new** | 4 | 3 | 5 | 2 | 1.5 | **12.0** | CORE-if-room² |
| 15 | Cascade Harvest (S5) | old 6.3 | 4 | 2 | 5 | 2 | 1.5 | **11.3** | NICE |
| 16 | Quality sub-scores + trigger-lane clarity (S7) | old 4.5 | 3 | 1 | 4 | 1.5 | 1.5 | **11.0** | NICE |
| 17 | Gene draft reveal ceremony (S8) | old 6.4 | 4 | 1 | 5 | 2 | 1.5 | **10.5** | CORE (cheap dopamine) |
| 18 | N4 · Garden-as-fail-state (S2) | **new** | 4 | 1 | 5 | 2 | 1.5 | **10.5** | CORE (decision needed) |
| 19 | 9 missing genes + 2nd/3rd SeedTemplate (with #3's riders) | old 4.5 | 4 | 1 | 5 | 2 | 1.5 | **10.5** | NICE |
| 20 | Firefly Jar (demoted — fails integration test as pure side-activity)³ | old 6.3 | 3 | 1 | 5 | 2 | 1.5 | **9.0** | NICE |
| 21 | Pour Arc 🅱(rises) | old 6.3 | 3 | 1 | 4 | 2 | 1.5 | **8.3** | NICE |
| 22 | Rehearsal mid-step (per-tick trace in spec sheet) | old 5.1 | 4 | 2 | 5 | 2.5 | 1.0 | **6.0** | NICE |
| 23 | Row Rhythm | old 6.3 | 2 | 1 | 3 | 2 | 1.5 | **6.0** | NICE-low |
| 24 | Mutation on replant (the chosen "wow", if any) | old 5.4 | 5 | 2 | 5 | 3 | 1.0 | **5.7** | STRETCH (conscious pick) |
| 25 | Fixtures: Doris Bowl + Harvest Basket (gated on §4.3 + Cravings) | Rev 2 | 4 | 2 | 5 | 3 | 1.0 | **5.0** | GATED |
| 26 | N5 · Graft Splice world verb | **new** | 4 | 2 | 4 | 3 | 1.0 | **4.7** | POST (loses to #24) |
| 27 | Compost/soil-quality loop (receives N1/N3 outputs) | **new** | 3 | 2 | 5 | 3 | 1.0 | **4.3** | POST |
| 28 | Daily seed & score | old 5.5 | 3 | 1 | 4 | 1.5 | 0.5 | **3.7** | POST |
| 29 | Wind as daily variable | old 5.2 | 3 | 2 | 4 | 2 | 0.5 | **3.0** | POST |
| 30 | N8 · Preserves bench | **new** | 3 | 2 | 4 | 3 | 0.5 | **2.2** | POST |
| 31 | Gobble Toss (demoted — waits on Doris Provides) | old 6.3 | 3 | 2 | 4 | 3 | 0.5 | **2.0** | POST |
| 32 | Irrigation channels | old 5.3 | 4 | 2 | 5 | 4 | 0.5 | **1.9** | POST (flagship candidate) |
| — | Splice Steady-Hands (editor) | old 6.3 | — | — | — | — | — | **CUT** (S9) | — |
| — | Scent-trail scouting | **new** | — | — | — | — | — | **REJECTED** (S10) | — |

¹ N6 scores high because it's an S-effort rider on the TimingCircle pack — implement together.
² #13/#14 are the two "new system" candidates; the CORE band above them is ~4–6 weeks alone. Recommendation: pick **one** for the demo. If §4.3 confirms Commit & Watch, pick **N2 ripeness** (it directly answers the day-under-stimulation risk #1 and feeds the Harvest Basket fixture). If Live Garden wins, N2 still wins. N1 Pruning Snip is the better *post-demo* start because it wants compost (#27) to pay off fully.
³ Firefly Jar keeps NICE on charm alone — it's the screenshot moment — but it no longer outranks integrated items.

### Demo slice, restated (CORE band, in build order)
1. §4.3 experiment → decision (gates tick-fairness choices in everything below)
2. Feedback pass (#1) → 3. Energy rebalance (#5) → 4. Wave escalation + forecast + aggro formula (#12, #2) → 5. Run arc screens (#11, rides F3/G3 from the Fable-5 plan) → 6. Cravings (#9) + draft ceremony (#17) → 7. TimingCircle pack + mastery hook (#6, #8) + Shoo! (#7) → 8. Pest drops (#10) + Quiet Time (#4) → 9. N4 fail-state swap (#18, one decision + small code) → 10. one of N2/N1 if the schedule holds (footnote ²).

Realistic total: **5–7 focused weeks** with buffer — still inside the late-2026 demo forecast, but only if footnote ² discipline holds (one new system, not two).

---

## 6. Risks & anti-goals (delta from the old doc — those still stand)

- **Attention budget collapse (new, from N2+N1+Cravings+waves stacking).** Commit & Watch's resolution phase has a lean-in density target (≥1 meaningful moment / 15–20 s, Rev 2). These systems each *generate* moments — together they can exceed the budget and recreate the Live-Garden frenzy by accident. Rule: tune ripeness windows and decay rates so that **at most two systems demand attention in any 20-tick span**; the Report attributes what was missed, calmly.
- **Loot-pinata drift (N3).** Pest drops must stay S-tier rewards (a reroll, a tile of fertilizer) — if drops outvalue harvest, the farming game becomes a hunting game and the identity inverts. Cap: drops ≤ ~15% of a round's total value.
- **Formula worship.** §5's numbers encode *my* weightings of *your* stated priorities. The two places I'd most expect your gut to disagree: Firefly Jar's demotion (charm is strategy for a cozy game's marketing) and mutation-on-replant at #24 (it's still the biggest single "wow" available). Both disagreements are legitimate — the table is for arguing with.
- **Everything else:** twitchification, editor dexterity, minigame fatigue math, balance whiplash, determinism erosion — unchanged from old §8; all new items above route RNG through `IDeterministicRandom` by spec.

## 7. Open questions for Milan (updated set)

1. **N4 fail-state swap:** comfortable cutting player starvation entirely for the demo, or keep hunger as a soft debuff? (Recommendation: cut for demo, decide the debuff post-playtest.)
2. **Footnote ² pick:** if only one new system ships — ripeness windows (N2) or Pruning Snip (N1)? (Recommendation: N2 under Commit & Watch.)
3. **Watering fantasy (S6, inherited):** economy, tempo, or gate? Blocks Pour Arc vs. irrigation priority AND the Editor multiplier verify.
4. **Firefly Jar:** accept its demotion, or protect it as the designated screenshot moment regardless of score?
5. **Minigame ceiling (inherited):** is ~20% garden-strength from minigame skill acceptable? N6 mastery widening slightly *lowers* the long-run ceiling (windows widen → grades homogenize) — intended?

## 8. Sources & verification

**Project (read 2026-07-08, live disk):** `Gameplay_Engagement_Research.md` (full), `projectmemory.md` (full), Commit & Watch Rev 2 + UI research via projectmemory summaries. No code read this session — all code claims inherit the 2026-07-05 verification and its *(survey)* / **[verify in Editor]** marks; nothing here modifies code or assets.
**Web (2026-07-08):** Thronefall design — [Grokipedia](https://grokipedia.com/page/Thronefall), [GameLuster review](https://gameluster.com/thronefall-review-holding-on-for-one-last-night/); Against the Storm — [Game Developer interview](https://www.gamedeveloper.com/business/how-against-the-storm-managed-to-mix-city-building-and-roguelite-play), [Rogueliker review](https://rogueliker.com/against-the-storm-review/); Stardew fishing — [Stardew Valley Wiki: Fishing](https://stardewvalleywiki.com/Fishing), [Game Dev's Guide to Fishing Minigames](https://gamedevsjourney.substack.com/p/the-game-devs-guide-to-fishing-minigames); Cult of the Lamb critique — [Josh Bycer / Medium](https://medium.com/@GWBycer/cult-of-the-lamb-is-a-devilishly-cute-roguelite-a350de89dde1), [Inverse review](https://www.inverse.com/gaming/cult-of-the-lamb-review); Niche genetics — [niche-game.com](https://niche-game.com/), [Wikipedia](https://en.wikipedia.org/wiki/Niche_(video_game)); plant-care verbs — [GameSpew: games about plants](https://www.gamespew.com/2025/02/four-excellent-games-about-plants-and-horticulture/), [UnusualSeeds: gardening games](https://unusualseeds.net/the-best-video-games-about-gardening-and-plants/); Atomicrops — [Raw Fury](https://rawfury.com/games/atomicrops/).

---

## Next action anchor

**Unchanged and now twice-confirmed: run the §4.3 phase-identity experiment (one sandbox afternoon).** Its outcome flips three flags in the master table (#7, #14, #21) and decides the tick-fairness rule for every minigame. Decide it, then execute §5's demo slice top-down — and answer §7 Q1/Q2 in the same sitting, since both are S-effort decisions that unblock CORE items.
