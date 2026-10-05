# Gene Catalog Expansion Research
### Abracodabra — Future Genes, Refinements & Synergy Web
*2026-07-20 · Concept only — nothing in this document is implemented. Companion to `02_Design/gene_systems_deep_dive_v6.md` (buffer model, payload×delivery matrix, tier system) — this doc EXTENDS it, does not repeat it.*

**Status legend used throughout:**
- ✅ **In code** (verified on disk 2026-07-20)
- 📋 **Designed** (specced in deep-dive v6, not in code)
- ✨ **New** (proposed in this document)

---

# TABLE OF CONTENTS

1. Where the catalog stands today
2. Design lenses — what makes a gene worth adding
3. Refinement pass on the existing 23
4. New Modifiers — the buffer is the playground
5. New Actives — new delivery verbs
6. New Payloads — new substances
7. New Passives — identity, aggro & economy
8. The Synergy Web — named combos & build archetypes
9. Anti-synergies — deliberate tension pairs
10. Run variety: themed gene pools ("Biome Baskets")
11. Balance guardrails & invariants
12. Tier & category master table
13. Prioritization — demo cut vs post-demo
14. Next action

---

# 1. WHERE THE CATALOG STANDS TODAY

## ✅ In code (23 player-facing genes)

| Category | Genes |
|---|---|
| **Active (7)** | BasicFruit, Cloud, Projectile, Aura, Trap, ReactiveBurst, Pruning |
| **Modifier (3)** | CostReduction (Efficiency), Overcharge, TriggerProximity |
| **Passive (6)** | GrowthSpeed, EnergyRoots, Regrowth, TerrainAffinity, ThickBark, ThornedLeaves |
| **Payload (7)** | Nutritious, Poison, Slow, Freeze, Explosive, Fear, Healing |

## 📋 Designed in v6 but not in code

Actives: Seedpod, Root Network, Spore Burst, Carnivorous Trap. Modifiers: Multicast, Volatile, Trigger:Timer, Echo, Leaf Armor. Passives: Deep Reserves, Iron Roots, Spreading Roots, Deciduous, Mycorrhizal Network. Payloads: Fireseed, Charm, Dominate.

## The diagnosis

The catalog's weight is badly skewed: **7 Actives and 7 Payloads, but only 3 Modifiers.** In a Noita-wand-style system, modifiers are where build expression lives — they are the genes that make *order matter*. Payloads multiply archetypes linearly; modifiers multiply them combinatorially. The single highest-leverage investment for build diversity is the Modifier column, and specifically **modifiers that manipulate the strand parse itself** (cursor movement, buffer contents, wrap behavior). No other game in the genre has that design space, because no other game has the strand.

Second gap: **no aggro/targeting management**. Tower defense lives on "which target does the pest pick." Two cheap passives (Alluring, Camouflage) open an entire tank/carry dimension that currently doesn't exist.

Third gap: **payloads only push outward**. Every payload does something *to a creature*. None of them touch the *soil*, the *fruit economy*, or *other plants' genes* — three loops the game already has (TerrainAffinity, fruit stockpiling / ripeness windows, Seedpod inheritance) that payloads could feed.

---

# 2. DESIGN LENSES — WHAT MAKES A GENE WORTH ADDING

Every proposed gene below was filtered through these six lenses. When pitching future genes, reuse them.

**L1 — Order-sensitivity.** Does the gene read differently depending on where it sits in the strand? A gene that's position-agnostic (most passives) is fine, but strand genes should reward rearrangement. Best-in-class: a modifier whose *distance* from its Active matters.

**L2 — Leaf economy.** Leaves are HP, energy, and ammo simultaneously. Genes that spend, protect, count, or convert leaves plug into the game's best system. (v6 §8.4 started this; there's more room.)

**L3 — Spatial play.** The farm is the build. Genes that care about adjacency, pathing, facing, or tile type deepen TD placement decisions. Z stays 0; everything here is 2D-grid-native.

**L4 — Tick rhythm.** WeGo means everything is countable. Genes that create *rhythms* (every Nth cycle, at dawn, after N quiet ticks) give the Commit & Watch phase legible texture — they answer the "day under-stimulation" risk by making plants visibly syncopated.

**L5 — Economy hooks.** Doris digestion, fruit stockpiles, ripeness windows, pest drops (chitin/manure) are live design threads. Genes that feed those loops make the two halves of the game (farm ↔ defense) change each other's decisions — the Cult of the Lamb integration test.

**L6 — Determinism & readability.** Any randomness routes through `IDeterministicRandom`. Any effect must be visually attributable in one glance during the auto-driven phase ("that plant did that because of that gene"). No invisible math.

**Hard rule for every new gene: at least two cross-category synergies and at least one anti-synergy** (a popular gene it fights with). A gene that combos with everything is a stat stick; a gene that combos with nothing is a dead draft. The anti-synergy is what creates draft tension.

---

# 3. REFINEMENT PASS ON THE EXISTING 23

Not redesigns — sharpening passes that make each existing gene participate in more combos. Ordered by category.

## Actives

**BasicFruit ✅ → "Fruit ripens".** Tie directly into the ripeness-window concept (Minigames doc N2): fruit produced by the gene has a ripeness curve — *underripe → perfect → overripe* — and payload potency scales with harvest timing (0.7× / 1.0× / 1.3× but overripe fruit decays in N ticks if unharvested). One field on the fruit object, and suddenly Fruit interacts with player attention, Harvest Basket fixture timing, and the new Ferment payload (§6). This is the single cheapest refinement with the biggest loop-integration payoff.

**Cloud ✅ → wind-aware.** Clouds drift 1 tile per K ticks in the day's wind direction (per-run deterministic, shown on the HUD sun-arc). Placement upwind of the pest path becomes a skill. Free depth for Fireseed (fire spreads downwind) and Pollen Drift (§5) later. *Costs a direction vector and an offset — WorldEffect already tracks position.*

**Projectile ✅ → visible targeting priority.** Currently TargetFinder picks nearest. Expose the priority as part of the gene ("nearest" default; Overcharge → "strongest"; Trigger:Proximity → "closest to plant"). Modifier-driven retargeting makes existing modifiers matter more without new genes.

**Aura ✅ → radius rings readable.** No mechanical change; commit to the invariant that aura radius is always an exact tile ring (1/2/3), never a float radius. Every aura combo below assumes countable rings.

**Trap ✅ → visible arming state + salvage.** Armed traps show a subtle glyph (pests don't avoid them; the *player* reads them). Manual dig-up of an armed trap during Planning refunds nothing but clears the tile — traps otherwise squat tiles forever, which will hurt when fixtures arrive.

**ReactiveBurst ✅ → cooldown is a stat.** Promote its internal cooldown to a designer-facing field that modifiers can touch (Overcharge = bigger burst, Catalyst §4 = double burst, longer cooldown). Chain Reactor balance then lives in one number.

**Pruning ✅ → oldest-leaf rule is sacred.** Keep "sacrifices the *oldest* leaf" — with the Decayed-leaf state from the Pruning Snip concept, Pruning the plant's decayed leaf is *free value*, and the gene automates the manual care verb exactly as the manual→automated principle demands. No change, just a wiring commitment.

## Modifiers

**CostReduction ✅ → rename to Efficiency everywhere.** Docs already call it Efficiency; code says CostReduction. One identity. Mechanic untouched (it's the teaching modifier — leave it simple).

**Overcharge ✅ → +40% everything, +1 tick executionDelay.** Give it a cost besides energy: the overcharged Active takes one extra tick to fire (`executionDelayTicks` already exists on ActiveGene ✅). Now Overcharge on a fast turret is a real decision, and Trigger:Timer + Overcharge (pre-banked, delay hidden inside the quiet cycles) becomes the clean combo it wants to be.

**TriggerProximity ✅ → range = targetRange of the Active.** One number, not two. Also: while the trigger holds the slot closed, the plant *banks* the unspent energy (v6 §2 carry-over already promises this — make the Spec Sheet show the bank filling).

## Passives

**GrowthSpeed ✅** — fine as is; the additive stack table in v6 stands.

**EnergyRoots ✅** — fine; it's the default "more juice" pick and every energy-hungry combo below leans on it.

**Regrowth ✅ → regrows into the *newest* leaf slot.** Interaction with Pruning (which eats the *oldest*): a Pruning+Regrowth plant visibly cycles its foliage front-to-back. Pure readability, zero balance change.

**TerrainAffinity ✅ → one gene, per-terrain variants via Fertilizer.** Keep the per-terrain assets, but let the new Fertilizer payload (§6) *create* the matching terrain. Affinity stops being "hope the map rolled dirt" and becomes a terraforming build-around.

**ThickBark ✅ / ThornedLeaves ✅** — already load-bearing (Thorn Garden archetype). Only note: Thorned damage should tick through `StatusEffectManager` so Lifesteal/Spore payload interactions (§6) get one pipeline.

## Payloads

**Nutritious ✅** — the dual behavior (food on Fruit, scent-lure elsewhere) is the best design in the catalog. Don't touch.

**Poison ✅ / Slow ✅ / Freeze ✅** — fine; they're the status-effect teaching trio. Freeze's stack model (N stacks = frozen) is the keystone for every lockdown combo — document the stack threshold as a global constant, not per-source.

**Explosive ✅** — add one rule: explosions push 1-tile knockback on Small creatures. Suddenly Explosive interacts with pathing, traps, and the Tendril/funnel builds (§5) instead of being raw damage.

**Fear ✅ → directional.** Feared creatures flee *away from the payload source*. That single vector turns Fear from "soft despawn" into a herding tool (v6 §8.3 already dreams of this — make it literal).

**Healing ✅** — the 50%-speed plant-regrowth rule is good. Keep the wasted-on-Trap entry warned, not blocked.

---

# 4. NEW MODIFIERS — THE BUFFER IS THE PLAYGROUND

The unique design space. Three families: **buffer manipulators** (touch the modifier/payload buffers), **cursor manipulators** (touch the read head — the most novel family), and **conditional triggers** (gate execution on world state).

## 4.1 Buffer manipulators

### ✨ Catalyst — T2 Modifier
The next Active consumes **double energy** and fires at **double effect** (potency, radius, duration ×2).
*Order play:* the mirror-image of Efficiency — you place it before your *cheap* Active, because doubling a cheap cost is affordable while doubling Overcharge'd costs starves the strand.
*Synergies:* Efficiency + Catalyst on the same Active nearly cancel on cost but keep the ×2 effect (the "free lunch" discovery — 2 slots of strand time is the hidden price). Catalyst + Freeze projectile = 2 stacks per hit, halving shots-to-frozen.
*Anti-synergy:* Overcharge (multiplicative cost explosion — Spec Sheet warns).
*Impl note:* pure `ModifyEnergyCost` + `PreExecution` on the existing ModifierGene ✅ surface. Cheapest new modifier to ship.

### ✨ Prism — T2 Modifier
The next Active's payloads are **split**: each payload applies at 50% potency, but the Active fires **twice** (two projectiles, two cloud pulses, twin fruit).
*Distinct from Multicast 📋:* Multicast = same full effect twice at extra cost. Prism = free split into two half-strength copies. Multicast is power; Prism is *coverage*.
*Synergies:* Prism + Slow (half-potency slow is still slow — CC doesn't care about potency) = the discovery that CC payloads love Prism while damage payloads hate it. Prism + Fruit + Nutritious = two small meals, better for hand-feeding Doris cadence than one big one.
*Anti-synergy:* Freeze (half potency may round below 1 stack — visible "0 stacks" feedback teaches thresholds).

### ✨ Incubate — T2 Modifier
The next Active *doesn't fire*; its payload potency **grows +25% per cycle** it stays held (cap +100%). It releases when the strand's *other* trigger condition fires (proximity, timer, leaf-consumed) or when the player manually harvests.
*Fantasy:* pressure building inside the plant — the bud that swells for three days then bursts.
*Synergies:* Incubate + Trigger:Proximity + Cloud + Poison = a plant that quietly brews a monster cloud during peace and dumps it the moment pests arrive (the payoff of the Commit & Watch quiet beats — L4 rhythm made visible: the bud sprite swells each cycle). Incubate + Fruit = intentional overripe-plus mechanic tying into ripeness windows.
*Anti-synergy:* short strands (a 2-slot strand cycles so fast the cap trivializes — Incubate wants long, slow strands).

### ✨ Concentrate — T3 Modifier
Duplicate payloads on the next Active **merge**: two Poisons become one Poison at 2.5× potency (superadditive).
*Why:* creates demand for *duplicate* gene drops, which currently read as dead drafts. The moment Concentrate exists, a second Poison gene is a build-around, not a shrug.
*Synergies:* any doubled payload; Doris digestion economy (she produces diet-biased pellets — feeding her poison fruit farms duplicate Poison genes… which Concentrate wants. Loop closed).
*Anti-synergy:* Prism (split then merge = pure waste; editor warns).

## 4.2 Cursor manipulators — nothing else in the genre has these

The cursor advancing one slot per tick is the game's heartbeat. These genes make the heartbeat itself editable. **These are the genes that will fill forum threads.**

### ✨ Ritornello (Anchor) — T3 Modifier
When the cursor passes this slot for the **second** time in a cycle-window, it instead **jumps back N slots** (N = 2 default) — once per full loop.
*Effect:* a sub-loop inside the strand. `[Fruit][Nutritious][Ritornello][Cloud][Poison]` runs Fruit once, then bounces the tail `[Cloud][Poison]`… no — bounces back over `[Nutritious][Ritornello]`, re-firing the fruit segment. A 5-slot strand where 2 slots run twice per loop.
*Fantasy:* musical repeat sign. The Spec Sheet literally draws it as one.
*Synergies:* anything you want *more often* without shortening the strand — the answer to "my turret needs its rhythm but I also want a food segment." Opus Magnum players will build clockwork with it.
*Anti-synergy:* Incubate (re-passing the held Active resets nothing but wastes the bounce), energy-starved plants (doubling executions doubles drain).
*Guardrail:* max one Ritornello per strand; bounce fires once per loop (no infinite sub-loops). Deterministic, no state beyond a bool.

### ✨ Leap — T2 Modifier
The cursor **skips the next slot** (0 ticks, nothing fires) **every other cycle**.
*Effect:* alternating behavior from one strand. `[Projectile][Poison][Leap][Fruit][Nutritious]` = cycle A fires turret+food, cycle B turret only (Fruit skipped) → the plant syncopates: *defend-defend-feed, defend-defend-feed*.
*Fantasy:* a skipped heartbeat; the sheet shows A/B cycle columns.
*Synergies:* lets one plant be 70% combat / 30% food instead of 50/50 — the ratio dial the farm-tension loop (v6 §5) currently lacks. Leap before an expensive Active = fire it half as often, bank energy meanwhile (poor man's Trigger:Timer at T2).
*Anti-synergy:* Ritornello (cursor chaos — legal, warned, and someone will make it work; that's the Noita spirit).

### ✨ Palindrome — T3 Modifier (rare showpiece)
The strand reads **left-to-right, then right-to-left**, alternating each cycle.
*Effect:* on reverse cycles, payloads *precede* their Actives — so the buffer rules produce a completely different parse of the same strand. A strand that is two plants on alternating days. The ultimate expression of "same genes, different order" — here, same genes, *both* orders.
*Synergies:* symmetric strands (`[Poison][Cloud][Efficiency][Cloud][Poison]`) parse identically both ways — discovering palindromic strand-building is a legitimate endgame puzzle hobby.
*Anti-synergy:* everything fragile (wrap-trick builds break — reversal replaces the wrap). One per strand; T3 seeds only.
*Cut-line note:* flashiest and least necessary — post-demo, but write it down now because it defines the ceiling of the cursor-manipulator family.

## 4.3 Conditional triggers (extend the ✅ trigger surface)

All use the existing `CheckTriggerCondition` hook ✅ — cheap to add, and each one gives plants a *schedule*, which is exactly what makes watching the auto-phase legible (L4).

### ✨ Trigger: Dusk / Trigger: Dawn — T1 Modifier pair
Slot fires only in the last/first quarter of the day (tick-window on the sun-arc HUD).
*Synergies:* Dusk + Cloud + Poison = the night-shift guard that doesn't waste energy at noon; Dawn + Fruit = breakfast plant synced to Doris's feeding rhythm. Cheapest possible "my farm has a daily routine" texture — and it makes the sun-arc meter *mechanical*, not decorative.
*Anti-synergy:* each other (Dusk+Dawn on one Active = never fires; editor warns — and one cheeky player will pair it with Palindrome to prove the editor wrong).

### ✨ Trigger: Last Stand — T2 Modifier
Slot fires only while the plant is at **≤50% leaves**.
*Effect:* desperation genes. `[LastStand][Overcharge][ReactiveBurst][Explosive]` = a plant that fights hardest while dying. Inverts the Leaf Armor 📋 curve (which scales *down* with damage) — the two together define a "berserk vs. fortress" axis for combat plants.
*Synergies:* ThornedLeaves tank that unlocks a second phase; Healing neighbor turns Last Stand off (a *positive* anti-synergy the player manages spatially — heal the tank *after* the wave, not during).
*Anti-synergy:* ThickBark (delays reaching the threshold — fortress genes fight berserk genes for the same plant. Perfect.)

### ✨ Trigger: Sated — T1 Modifier
Slot fires only if Doris was fed within the last N ticks.
*Why:* wires the pet economy directly into strand logic (L5). "My defense turret only runs when Doris is happy" is an absurd, memorable, thematic constraint that Doris-focused runs get for free — and it mechanically *rewards* the feeding chore.
*Anti-synergy:* neglect-Doris strategies (as intended — this gene is a vote in the player's ongoing Doris relationship).

---

# 5. NEW ACTIVES — NEW DELIVERY VERBS

The 8 delivery methods (6 payload-compatible) cover *emit outward*. The gaps: **pull**, **place**, **attach**, and **channel**.

### ✨ Tendril — T2 Active (payload-compatible)
A vine lashes the nearest creature within `targetRange` and **pulls it 2 tiles toward the plant**, applying payloads on contact. Moderate cooldown.
*Why pull matters:* every existing verb pushes pests *away* or waits for them. Pull inverts pathing — the plant *chooses* who enters the kill zone. Into the Breach positioning-play, plant edition.
*Synergies:* Tendril + Poison = drag through your own poison cloud; Tendril + Trap = yank onto the armed tile (the trap finally gets to pick its victim); Tendril + Alluring passive (§7) = the full aggro-tank kit. With Explosive knockback (§3): push-pull pinball between two plants.
*Anti-synergy:* Fear (you pulled it in, then scared it away — comedy, warned).
*Impl:* rides ProjectileWorldEffect targeting ✅ + a move-creature call on `AnimalController` grid movement ✅.

### ✨ Sap Well — T1 Active (payload-compatible, ground-placement)
Deposits a **sap puddle item on an adjacent tile** carrying the attached payloads; first creature to step on/eat it consumes the whole charge. Puddle persists M ticks then dries. Effectively a *consumable, rebuilding trap* the plant manufactures.
*Distinct from Trap ✅:* Trap is armed-mechanism (cooldown, re-arms, on-contact); Sap Well is *ammunition on the ground* — visible, finite, steals a tile's attention.
*Synergies:* Sap + Nutritious = self-refilling Doris feeding station **— this is the gene-form of the Doris Bowl fixture**, reachable through buildcraft instead of furniture (the two should share code). Sap + Slow in a corridor = sticky floor lane. Prism + Sap = two half-puddles = wider lane coverage.
*Anti-synergy:* Cloud on the same plant (two area tools, one energy pool).

### ✨ Symbiote Burr — T3 Active (payload-compatible)
Fires a burr that **attaches to a creature**; for N ticks, the creature carries the payloads as a moving emitter (1-tile ring, weak per-tick application).
*Effect:* the pest becomes your delivery drone. Burr + Poison on a fast creature = a plague rat running through its own swarm. Burr + Fear = a panicked creature radiating panic — one burr can rout a wave. Burr + Charm on a *friendly* animal = a walking peace aura.
*Why it's worth T3 rarity:* it's the only gene where *enemy movement* becomes your weapon — waves stop being a threat vector and become a topology you exploit.
*Anti-synergy:* kill-fast builds (dead carrier = wasted burr; you want the target *alive and running*, which fights every turret on your farm — a real farm-layout decision).
*Impl:* a component stamped onto the creature GameObject + ring application via StatusEffectManager ✅. Deterministic; no pathing changes.

### ✨ Bloom — T3 Active (payload-compatible, channel)
Does nothing for **3 full cycles** (bud visibly swelling), then fires a **massive** payload release (×3 potency, double radius) and resets.
*Distinct from Incubate ✨ (modifier):* Incubate holds *another* Active's payload until a trigger; Bloom **is** the Active, on a fixed visible schedule — the town-clock of the farm. Countable, predictable, plannable — pure WeGo.
*Synergies:* GrowthSpeed-stacked seed to reach maturity before round's midpoint; Trigger:Dusk + Bloom = guaranteed nightly nova; the whole farm can be *choreographed* around Bloom timing (Thronefall siege rhythm).
*Anti-synergy:* Leap/Ritornello cursor tricks (desyncs the countdown — legal, dizzying).

### ✨ Pollen Drift — T2 Active (payload-compatible, directional)
Releases a payload-carrying pollen stream that travels **downwind** (see wind-aware Cloud refinement, §3) as a moving 1-wide line, L tiles long.
*Effect:* the only *linear* area verb (Cloud = blob, Aura = ring). Lanes! Pollen + Slow across the map entrance = a tollbooth. Pollen + Nutritious = a scent *road* leading pests where you want — the Funnel of Death gets a paintbrush.
*Synergies:* wind system (makes daily wind a build parameter); Fireseed 📋 (fire lane, downwind, obviously).
*Anti-synergy:* wind rotation days (per-run wind table means a lane build must adapt mid-run — scarcity-drives-creativity applied to geometry).

### 📋→✨ Root Network — promote from concept, one addition
As specced in v6 (adjacent energy sharing), plus: **shares with plants, fixtures, and Sap Wells alike**, making it the infrastructure gene for automation-heavy late farms. Keep payload-incompatible.

---

# 6. NEW PAYLOADS — NEW SUBSTANCES

Existing payloads all target creatures. The new ones target **soil, corpses, fruit, and the economy** — plus two creature-payloads that fill genuine combat gaps.

### ✨ Fertilizer — T1 Payload (targets: ground)
Where it lands (cloud footprint, projectile impact, sap puddle, fruit rot spot), the tile's terrain **enriches one step** toward the plant-favored type for R rounds.
*Why it changes everything:* TerrainAffinity ✅ stops being map-luck and becomes buildable. Cloud + Fertilizer = a plant that *terraforms its own neighborhood*; next Planning phase you replant into the ground your last build prepared. **Runs gain memory.**
*Synergies:* TerrainAffinity (obviously — the pair is a two-gene engine); pest manure drops (same soil-enrichment pipeline — one system, two sources); Pollen Drift + Fertilizer = fertile lane for next round's wall of plants.
*Anti-synergy:* none in combat — its cost is *being useless in combat*. A pure greed payload competing for the same slots as Poison. That IS the tension.

### ✨ Spore — T2 Payload (targets: creature, spreads)
Infects the target; on the target's **death**, releases a 1-ring puff applying Spore + any co-attached payloads to nearby creatures.
*Effect:* damage that *chains through the wave's own density*. Big packs melt; single elites shrug. The exact inverse of Freeze (great vs elites, wasteful vs swarms) — together they form the swarm/elite axis every wave composition (v6 §7.5) can be read against.
*Synergies:* Charm + Spore = the infamous plague-diplomat (charmed pest walks home, dies among friends); Symbiote Burr + Spore = patient-zero delivery; Bounty (below) + Spore = mass-marked drops.
*Anti-synergy:* Explosive knockback (scatters the pack you needed clustered).
*Guardrail:* spread depth max 2 hops; all rolls via IDeterministicRandom.

### ✨ Lifesteal (Leech) — T2 Payload (targets: creature)
Damage-over-time; a fraction of damage dealt returns to the **source plant as leaf regrowth progress** (uses the Healing 50%-rate pipeline ✅).
*Why:* completes the vampire archetype: the combat plant that sustains *itself* instead of needing a Healing neighbor. Solo-plant viability for small farms and early rounds.
*Synergies:* ThornedLeaves (bite me, feed me — the tank that heals off being eaten); Last Stand builds (leech faster while desperate).
*Anti-synergy:* Regrowth passive (redundant sustain — spend the passive slot on offense instead; Spec Sheet should surface the overlap).

### ✨ Ferment — T3 Payload (targets: fruit — the first fruit-only payload)
The fruit's *other* payloads **transmute as it ages**: underripe = weak, perfect = as-built, overripe = *mutated* (Nutritious→Charm, Poison→Spore, Healing→Lifesteal… fixed transmutation table, printed in the Spec Sheet).
*Effect:* the stockpile becomes a cellar. Harvest timing (ripeness windows, §3) now chooses *which payload* you get — one plant, three products by patience alone. Deliberately overripening becomes a strategy ("I'm aging Charm wine for the boar wave").
*Synergy:* the entire fruit economy, Doris cravings (aged goods as craving satisfiers), Harvest Basket fixture (auto-harvest timing = product selection).
*Anti-synergy:* Prism (two half-fruits age twice as fast — spoilage risk doubled).

### ✨ Bounty — T2 Payload (targets: creature, economy)
Marks the target; if it dies while marked, it **drops double pest resources** (chitin/manure per the pest-drops concept) and grants Doris digestion bonus if she eats the corpse.
*Why:* the greed lane made into a payload — convert defense into economy explicitly. "Do I attach Poison (kill faster) or Bounty (kill richer)?" is a clean, recurring dilemma.
*Synergies:* Spore (mass marking through spread); Projectile snipers (mark the elite before the traps finish it).
*Anti-synergy:* Fear/Charm displacement (marked pest that *flees off-map* drops nothing — don't mix herding with harvesting).

### ✨ Petrify — T3 Payload (targets: creature, rare)
Stacks like Freeze but at threshold the creature turns to **stone permanently — becoming a wall tile** pests must path around (crumbles after R rounds).
*Effect:* the only payload that produces *terrain*. Late-game funnel builds literally sculpt the map out of their enemies. Cozy-dark as hell.
*Synergies:* Funnel of Death (grow your own walls at the choke); Tendril (position the statue *exactly*).
*Anti-synergy:* Bounty/Doris economy (statues drop nothing, feed no one — beauty has a price). Boss-immune, Large-resistant like Dominate.

---

# 7. NEW PASSIVES — IDENTITY, AGGRO & ECONOMY

### ✨ Alluring — T1 Passive (aggro)
Pests within 4 tiles **prefer targeting this plant** over neighbors.
*The taunt-tank enabler.* Alluring + ThickBark + ThornedLeaves + Regrowth = a dedicated bait-tank that *is* the defense, freeing every other tile for food. The Thorn Garden archetype gets a keystone. Aggro management is the single biggest missing TD verb in the catalog — and it's one targeting-weight field in the pest's target selection.
*Anti-synergy:* putting it on your food plant by accident. (The editor will not stop you. Round 3 will teach you.)

### ✨ Camouflage — T1 Passive (aggro, mirror)
Pests **deprioritize** this plant unless it's the last one standing.
*The carry-protector.* Camouflage on the Bloom nuke plant while the Alluring tank draws the wave = the tank/carry duo, two T1 passives, infinite compositions. Ship both or neither.

### ✨ Heliotrope / ✨ Nocturnal — T1 Passive pair (rhythm)
+40% energy generation in day-half / night-half respectively; −15% in the other.
*Synergy:* Trigger:Dawn/Dusk (§4.3) — a Nocturnal + Trigger:Dusk + Cloud + Poison plant is a complete "night watchman" identity from three commons. Rhythm passives make two plants with identical strands play differently — cheap variety multiplier.

### ✨ Overgrowth — T2 Passive
+2 max leaves (more HP, more energy, more Pruning ammo, more ReactiveBurst triggers)…
*…and* +1 tick to base cycle recharge (a bushier plant is a slower plant). Raw stats with a real cost — the "big slow tree" stat direction, opposite of the Empty Slot Speedrun.

### ✨ Heirloom — T2 Passive
On death, the plant **drops one seed** containing a random ~half of its genes (deterministic roll).
*Effect:* death stops being pure loss — every casualty is a partial backup. Emotionally load-bearing for Commit & Watch (watching a plant die *and drop a legacy* converts frustration into a next-round plan). Synergy with Spreading Roots 📋 (full inheritance vs half-on-death — two different legacy strategies).
*Anti-synergy:* none mechanical; its cost is a passive slot spent on insurance instead of power.

### ✨ Deadfall — T2 Passive (leaf economy)
Leaves this plant loses (eaten, pruned, self-damaged) **drop onto adjacent tiles as mulch** — minor Fertilizer effect + minor pest slow (soggy footing).
*Effect:* even *dying badly* feeds the farm. Hellfield and Chain Reactor builds fertilize their own graveyard, so next round's replant grows faster there. Loss becomes loam. This is the cozy-dark thesis in one gene.

### ✨ Stubborn Stump — T3 Passive
On what would be plant death, survive **once per run** as a leafless stump (no energy generation) for 10 ticks; if any leaf regrows in that window (Healing neighbor, Regrowth, Lifesteal), the plant lives.
*Effect:* one dramatic save per plant per run — a *visible* 10-tick rescue emergency during the watch phase. Creates the game's best "lean-in" moment (engagement research risk #1) without any scripting: the game state itself makes you sit up.

---

# 8. THE SYNERGY WEB — NAMED COMBOS & BUILD ARCHETYPES

The v6 §8.2 combos still stand. These are the *new* constellations the expanded catalog creates. Names matter — players adopt named builds (Backpack Battles lesson); consider surfacing names in-game on discovery (Inscryption-style acknowledgment).

### The Night Shift
`Nocturnal + Trigger:Dusk + [Incubate][Cloud][Poison]` — brews all day on solar surplus, detonates a double-strength cloud at dusk, exactly when the wave table sends its heavy spawns. **Teaches:** rhythm stacking (passive rhythm + trigger rhythm + Incubate growth all point at the same hour).

### The Tollbooth
`Pollen Drift + Slow` across the entrance + `Sap Well + Poison` puddles in the slowed lane + one `Projectile + Bounty` sniper behind. Everything dies *slowly, profitably, in a line*. **Teaches:** linear-verb geometry; Bounty vs Poison slot economics.

### Tank & Carry
Plant A: `Alluring + ThickBark + ThornedLeaves + Lifesteal fruit-strand`. Plant B (behind): `Camouflage + [Bloom][Spore]`. The tank drinks the wave and heals off it while the hidden Bloom winds up a spore nova that chains through the clustered attackers *held in place by their own aggro*. **Teaches:** aggro as a build axis; density as a resource for Spore.

### The Vintner
`[Fruit][Nutritious][Ferment]` + ripeness timing + Harvest Basket fixture set to *late*. The farm produces aged Charm-fruit ("wine") stockpiled for boar rounds — a zero-combat plant that solves combat waves through the pantry. **Teaches:** harvest timing as payload selection; stockpile-as-armory.

### Patient Zero
`[Symbiote Burr][Spore][Slow]` on the fastest pest in the wave + Camouflage on everything you love. The carrier limps through its swarm sowing infection; the wave defeats itself. Fails vs elites (nothing to chain through) — pair with a Freeze trap for the counter-case. **Teaches:** wave-composition reading (swarm/elite axis).

### The Sculptor
`Tendril` positioning + `[Projectile][Petrify]` — pull the pest to the choke tile, petrify it *there*. Over rounds, the player literally builds a stone maze out of the season's invaders. Post-demo showpiece; screenshots sell the game by themselves. **Teaches:** permanent spatial investment; runs-with-memory.

### The Ritornello Engine
`[Efficiency][Projectile][Poison][Ritornello][Fruit][Nutritious]` — the cursor loops the combat segment twice per cycle, the food segment once: a 2:1 combat-food ratio inside a single plant. The Opus Magnum audience will speedrun-optimize cursor paths for fun. **Teaches:** cursor choreography as the endgame skill ceiling.

### The Legacy Grove
`Heirloom + Deadfall + Spreading Roots 📋` on expendable frontline plants. Every death drops a half-genome seed onto self-fertilized mulch. By round 8 the frontline is a self-replanting graveyard-garden that gets *stronger* where it bled most. **Teaches:** death as an economy; the cozy-dark loop closed.

### Doris's Favorite
`Trigger:Sated` gating the whole combat strand + `Sap Well + Nutritious` auto-feeder + Bounty corpses routed to Doris. Feeding Doris literally powers the defense; the pet is the reactor. **Teaches:** the two game halves changing each other's decisions (integration test passed by construction).

---

# 9. ANTI-SYNERGIES — DELIBERATE TENSION PAIRS

Draft tension needs *fights*, not just fits. Keep these pairs mutually hostile on purpose; the editor warns, never blocks (v6 philosophy).

| Pair | Tension | Why keep it |
|---|---|---|
| Overcharge × Catalyst | cost multiplication starves strands | forces choosing your amplifier |
| Prism × Concentrate | split-then-merge = pure loss | punishes auto-piloted stacking |
| Fear/Charm × Bounty | herded pests drop nothing off-map | herding vs harvesting are different economies |
| Explosive × Spore | knockback scatters the chain cluster | AoE isn't one archetype |
| ThickBark × Last Stand / ReactiveBurst | durability delays your own triggers | fortress vs berserk axis |
| Lifesteal × Regrowth | redundant sustain | anti-stat-stick pressure |
| Alluring × food strands | you taunted with your lunch | placement literacy check |
| Tendril × Fear | pull in, scare away | comedy — and it teaches verb directions |
| Ritornello/Leap × Bloom/Incubate | cursor tricks desync countdowns | keeps clockwork builds honest |
| Dusk × Dawn (same Active) | never fires | the warning system's tutorial case |

---

# 10. RUN VARIETY: THEMED GENE POOLS ("BIOME BASKETS")

Random gene availability (v6 §7.1) currently means a uniform shuffle. Uniform shuffles converge to samey mid-run soup. Instead, partition each run's pool into **2–3 themed baskets** drawn deterministically from the run seed:

| Basket | Guaranteed backbone | Flavor genes weighted in |
|---|---|---|
| **Toxin** | Poison, Cloud | Spore, Concentrate, Sap Well, Bounty |
| **Frost** | Freeze, Trap | Petrify, Tendril, Incubate, Nocturnal |
| **Orchard** | Fruit, Nutritious | Ferment, Prism, Heirloom, Sated, Fertilizer |
| **Thorn** | ThornedLeaves, ThickBark | Alluring, Lifesteal, Last Stand, Deadfall |
| **Clockwork** | Efficiency, Trigger:Proximity | Ritornello, Leap, Bloom, Trigger:Dawn/Dusk |
| **Wind** | Cloud, Projectile | Pollen Drift, Symbiote Burr, Camouflage, Fear |

Rules: every run gets **Orchard or a food-viable substitute** (starvation-proof floor), plus 1–2 others; ~15% cross-basket bleed so pools never feel walled; Doris digestion bias (her diet shifts drop weights) steers *within* the run's baskets, keeping the pity/pellet economy coherent. Each basket must independently pass the check: **≥2 viable archetypes using only its genes + the T1 commons.** The table above passes by construction — each basket's flavor column was chosen so its members synergize *internally* first.

This also gives runs a nameable identity ("the Frost-Clockwork run") — which is what players retell (v6 principle 7: every run tells a story).

---

# 11. BALANCE GUARDRAILS & INVARIANTS

1. **Determinism**: every probabilistic gene (Spore spread, Heirloom half-genome, basket draws) rolls through `IDeterministicRandom` ✅. No exceptions; this is already an invariant — new genes don't get to break it.
2. **Idempotency**: all derived stats recomputed from `SeedTemplate` per call (the `BaseEnergyPerLeaf` pattern ✅). Incubate/Bloom charge counters are explicit runtime state on the plant, never baked into stats.
3. **Cursor manipulators are bounded**: max one Ritornello per strand, one bounce per loop; Leap alternates on a simple parity bit; Palindrome excludes other cursor genes. Cycle length must remain statically computable so the Spec Sheet can always print exact cycle time.
4. **No new categories.** Everything above is Active/Modifier/Passive/Payload on the existing ✅ base classes. Fruit-only (Ferment) and ground-target (Fertilizer) payloads are `CanAttachTo`/context checks, not new architecture.
5. **Size gates stay**: Petrify follows the Dominate rules (Large resistant, Boss immune). Knockback (Explosive) is Small-only.
6. **Warn, never block** (v6): every anti-synergy in §9 is a Spec Sheet warning, not a validation failure.
7. **One glance attribution** (L6): each new gene needs a distinct world-space tell (swelling bud = Incubate/Bloom, repeat-sign glyph = Ritornello, burr on creature = Symbiote). Budget pixel-art time per gene *at design time* — a gene without a tell doesn't ship. (Extends the 23-gene icon system thread: new categories of tells, same color-coding language.)
8. **Energy is the governor**: no new gene generates energy from nothing except through leaves (Lifesteal→regrowth→leaves→energy is the approved loop shape; Carnivorous Trap 📋 is the sanctioned exception).

---

# 12. TIER & CATEGORY MASTER TABLE (proposed additions only)

| Gene | Cat | Tier | One-liner | Lens hits |
|---|---|---|---|---|
| Catalyst | Mod | T2 | 2× cost, 2× effect | L1 |
| Prism | Mod | T2 | split payloads, twin cast | L1 |
| Incubate | Mod | T2 | hold & grow until triggered | L1 L4 |
| Concentrate | Mod | T3 | merge duplicate payloads 2.5× | L1 L5 |
| Ritornello | Mod | T3 | cursor repeat-sign sub-loop | L1 L4 |
| Leap | Mod | T2 | skip next slot every other cycle | L1 L4 |
| Palindrome | Mod | T3 | strand reads both directions | L1 |
| Trigger: Dawn/Dusk | Mod | T1 | day-phase gate | L4 |
| Trigger: Last Stand | Mod | T2 | fires at ≤50% leaves | L2 |
| Trigger: Sated | Mod | T1 | fires while Doris fed | L5 |
| Tendril | Act | T2 | pull creature 2 tiles, apply payloads | L3 |
| Sap Well | Act | T1 | place consumable payload puddle | L3 L5 |
| Symbiote Burr | Act | T3 | creature becomes mobile emitter | L3 |
| Bloom | Act | T3 | 3-cycle channel, ×3 nova | L4 |
| Pollen Drift | Act | T2 | downwind payload lane | L3 |
| Fertilizer | Pay | T1 | enrich terrain toward affinity | L3 L5 |
| Spore | Pay | T2 | on-death chain infection | L3 |
| Lifesteal | Pay | T2 | damage → own leaf regrowth | L2 |
| Ferment | Pay | T3 | payloads transmute with fruit age | L5 |
| Bounty | Pay | T2 | marked kills drop double | L5 |
| Petrify | Pay | T3 | freeze-to-stone permanent wall | L3 |
| Alluring | Pas | T1 | taunt aggro | L3 |
| Camouflage | Pas | T1 | deprioritized by pests | L3 |
| Heliotrope / Nocturnal | Pas | T1 | day/night energy identity | L4 |
| Overgrowth | Pas | T2 | +2 leaves, +1 recharge tick | L2 |
| Heirloom | Pas | T2 | death drops half-genome seed | L5 |
| Deadfall | Pas | T2 | lost leaves become mulch | L2 L3 |
| Stubborn Stump | Pas | T3 | survive death once, 10-tick rescue | L2 L4 |

28 proposed + 23 in code + ~17 designed = a ~68-gene ceiling catalog. **Do not ship all of it.** The catalog is a quarry, not a checklist.

---

# 13. PRIORITIZATION — DEMO CUT VS POST-DEMO

Judged demo-first: build variety gained per implementation cost, riding existing systems (`ModifierGene` hooks ✅, `StatusEffectManager` ✅, WorldEffects ✅), and zero new infrastructure.

## Demo tier (recommend ~8, order of value)

1. **Alluring + Camouflage** — two targeting weights; opens the entire tank/carry dimension. Highest variety-per-line-of-code in this document.
2. **Catalyst** — one `ModifyEnergyCost` override + effect multiplier; instantly triples modifier-ordering decisions (Efficiency/Overcharge/Catalyst triangle).
3. **Trigger: Dawn/Dusk** — `CheckTriggerCondition` on the existing tick clock; makes the sun-arc HUD mechanical and gives Commit & Watch its daily rhythm.
4. **Sap Well** — TrapWorldEffect variant; doubles as the Doris Bowl gene-form (shared implementation with the fixture — build once).
5. **Fertilizer** — hooks TerrainAffinity into buildcraft; pairs with pest-manure drops if those ship.
6. **Lifesteal** — StatusEffectManager DoT + existing healing pipeline; solo-plant sustain for the demo's small farms.
7. **Leap** — a parity bit on the executor; cheapest possible taste of cursor magic, safe enough for a demo.
8. **Last Stand** — one leaf-percentage check; makes leaf vitality generate builds, not just deaths.

*Demo skip-list (explicit, per scope discipline): all T3s, Ritornello/Palindrome (executor risk), Symbiote Burr (creature-attach infra), Spore (chain infra), Ferment (needs ripeness system first), Petrify (tile conversion), Pollen Drift (needs wind), Heirloom/Deadfall/Stubborn Stump (death-pipeline hooks), Incubate/Bloom/Prism/Concentrate/Overgrowth/Heliotrope/Nocturnal/Sated/Bounty/Tendril.*

## Post-demo wave 1 (system-payoff cluster)
Ripeness windows → **Ferment**; wind on Cloud → **Pollen Drift**; pest drops → **Bounty**; death pipeline → **Heirloom, Deadfall, Stubborn Stump**; then **Incubate + Bloom** as the rhythm flagship pair.

## Post-demo wave 2 (ceiling content)
**Ritornello, Palindrome, Symbiote Burr, Petrify, Concentrate** — the forum-thread genes. Ship with the T3 economy (Doris drops, exploration finds) so their rarity is felt.

## Honest viability note
28 new genes is a *quarry* (see §12) — the demo needs eight, and the eight above were chosen because each rides a system already on disk. The biggest risk in this document is not any single gene; it's the temptation to implement the cursor manipulators before the executor migration (F1 slot→buffer) is done and stable. **Hard dependency: every §4.2 gene assumes the buffer-model executor. Do not build cursor genes on the slot model.** Sequence: F1 first, demo-eight second, waves after.

---

# 14. NEXT ACTION

**Pick the demo-eight (or amend the list) and confirm the Biome Basket direction (§10) — then a task pack `03_Tasks/Active/2026-07_Demo_Gene_Pack.md` can be written with per-gene "done when" criteria, riding the existing ModifierGene/StatusEffectManager/WorldEffect surfaces.**
