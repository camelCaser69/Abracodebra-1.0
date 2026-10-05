# Gene Catalog — Experimental Annex ("The Forbidden Greenhouse")
### Abracodabra — deliberately unhinged gene concepts
*2026-07-20 · Concept only — nothing implemented. Companion to `Gene_Catalog_Expansion_Research_2026-07-20.md` (the sane catalog). That doc is a quarry; this one is the cave system underneath it. Nothing here is scoped, costed, or promised. The point is to map the outer walls of the design space so the main catalog knows where the ceiling actually is.*

**Feasibility flags (honest, per project rules):**
- 🟢 rides existing systems — could be tamed into the main catalog with modest work
- 🟡 needs a new subsystem — real cost, plausible post-demo
- 🔴 architectural — touches executor/run-structure/persistence; flagship-sized bets
- ☠️ probably never ships — but the *thinking* inside it is reusable

Every entry keeps the house format: fantasy → mechanic → why it's exciting → why it might be terrible. The "why it might be terrible" line is mandatory in this doc. Ideas that survive their own autopsy graduate to the main catalog.

---

# TABLE OF CONTENTS

1. Strand topology — genes that break the line
2. Cross-plant communication — the farm as one organism
3. Time & tick abuse — WeGo as a toy
4. Identity & mimicry — genes that lie
5. The living farm — creatures, Doris, and the player as gene targets
6. Run-memory & legacy — genes that outlive the run
7. World & weather — genes that touch the sky
8. Attention genes — Commit & Watch as a mechanic
9. Cursed genes — cozy-dark's basement
10. The distillation — what should escape this greenhouse
11. Next action

---

# 1. STRAND TOPOLOGY — GENES THAT BREAK THE LINE

The main catalog's cursor manipulators (Ritornello, Leap, Palindrome) bend the read order. These break the assumption that a strand is *a line at all*.

### ✨ Fork — 🔴 Modifier, T3
**Fantasy:** the strand splits like a branching vine.
**Mechanic:** the strand physically forks at this slot into two parallel tracks (editor shows a literal Y). Each cycle, ONE cursor walks ONE branch — alternating, or chosen by a trigger condition (Fork + Trigger:Proximity = combat branch when pests near, food branch when quiet).
**Why exciting:** conditional *programs*, not just conditional slots. This is an if-statement. The seed editor becomes visual scripting with dirt under its fingernails. A single plant with a peace-branch and a war-branch is the entire farm-tension loop inside one organism.
**Why terrible:** editor UI cost is enormous (branch layout, per-branch Spec Sheet columns); cycle time becomes state-dependent, breaking the "statically computable cycle" guardrail from the main doc. If it ever ships, it ships as THE T3 legendary of a major update, not as one gene among many.

### ✨ Knot — 🟡 Modifier, T3
**Fantasy:** two slots tied together with a visible thread.
**Mechanic:** placed on two slots (one gene, two halves — like a pair of earrings). When the cursor hits either half, both slots execute simultaneously (combined energy check).
**Why exciting:** lets distant strand segments fire together without adjacency — decouples *when* from *where* in the strand. `[Knot-A][Fruit][Nutritious]...[Knot-B][Cloud][Poison]` = the poison cloud always co-fires with breakfast. Rhythmic builds gain chords, not just melody.
**Why terrible:** the "one gene, two placements" inventory model doesn't exist; edge cases (one half wrapped, one not) multiply. Tame version: Knot only ties *adjacent-cycle* slots.

### ✨ Möbius — ☠️ Passive
**Fantasy:** the strand is a Möbius strip.
**Mechanic:** the strand has no fixed end: the wrap point drifts +1 slot per cycle, so the "start" rotates continuously. Every N cycles, every possible rotation of the strand has fired once.
**Why exciting:** one strand = N different parses over N cycles. Maximum expression of "order matters" — order *itself* becomes periodic. Theorycrafters would write dissertations.
**Why terrible:** illegible to anyone who isn't writing the dissertation. The Spec Sheet would need N columns. Keep the *idea* — "rotation as variety" — and let Palindrome carry the shippable version.

### ✨ Seed Within A Seed — 🔴 Payload, T3
**Fantasy:** a fruit that contains a whole other plant.
**Mechanic:** attach to Fruit. The fruit, when planted (not eaten), grows into a plant running a MINI-STRAND (3 slots) that the player pre-edited inside a nested editor panel. Effectively: Seedpod 📋, but with a *different, player-authored genome* rather than inheritance.
**Why exciting:** deploying pre-programmed plants as consumables — plant grenades. Stockpile turret-seeds in autumn, throw them at the ground mid-wave. It's the fruit economy and the gene system shaking hands at their most literal.
**Why terrible:** nested editors are a UX tarpit; the inventory now contains *programs*. Tame version: the mini-strand is fixed at `[Active][Payload]` chosen at edit time — two dropdowns, not an editor.

---

# 2. CROSS-PLANT COMMUNICATION — THE FARM AS ONE ORGANISM

The main catalog keeps plants as islands connected by placement. These genes make the farm a network. Biological precedent is on our side: real plants DO signal through mycorrhizal networks. The cozy-dark version is that ours gossip, argue, and unionize.

### ✨ Whisper — 🟡 Modifier, T3 — *the crown jewel of this annex*
**Fantasy:** plants lean toward each other; you can almost hear it.
**Mechanic:** any modifiers still in this plant's buffer at end-of-cycle (i.e., modifiers that found no Active — currently "wasted with warning") are NOT wasted: they drift to the adjacent Whisper-bearing plant and enter ITS buffer next cycle.
**Why exciting:** turns the wasted-modifier warning into a *transmission mechanic*. A dedicated "chanting" plant whose strand is `[Efficiency][Overcharge][Whisper]` — no Actives at all, a plant that does nothing but buff its neighbor's parse. Support-plant archetype born whole. Chains of whisper plants relay power across the farm like a nervous system. This is multi-plant strand-play, which nothing else in any catalog achieves.
**Why terrible:** debugging "why did my Cloud fire overcharged" requires tracing a neighbor's leftovers — attribution (lens L6) strains. Needs a visible drift particle + Spec Sheet "incoming whispers" row. Worth every penny of that UI. **Strongest graduation candidate in this document.**

### ✨ Hivemind — 🟡 Passive, T3
**Fantasy:** the connected plants bloom in unison, breathe in unison.
**Mechanic:** all plants bearing Hivemind share ONE pooled energy reserve (sum of individual pools, shared draw).
**Why exciting:** the Root Network idea at its logical extreme — a battery farm of leafy generators funding one monstrous spender. Also the failure mode is dramatic and fair: poison the pool's economics anywhere and the whole collective browns out together. One build, farm-scale.
**Why terrible:** removes per-plant energy legibility (the plant-is-the-health-bar principle bruises); one confused reading — "why won't MY plant fire" — spans six plants. Mitigation: shared pool shown as a literal water table under the connected tiles.

### ✨ Grudge — 🟢 Passive, T2
**Fantasy:** the tree remembers who bit it.
**Mechanic:** when a pest damages this plant, all other Grudge-bearing plants gain +25% effect against *that species* for the rest of the round.
**Why exciting:** the farm develops opinions. Wave composition variety (v6 §7.5) suddenly interacts with plant emotion — a boar-heavy run turns your garden into a boar-hating cult by round 6. Story engine: "my farm HATES slugs now."
**Why terrible:** per-species buff bookkeeping; risks invisible-math (L6) unless the buff shows as bared thorns/red tint against the hated species. Tame, shippable, and funny — near-graduate.

### ✨ Choir — 🟢 Active, T2
**Fantasy:** plants that bloom in chorus.
**Mechanic:** does nothing alone. Each OTHER Choir plant currently mid-cycle within 3 tiles adds +20% effect to this plant's next Active. Fires as a faint chime per contributor.
**Why exciting:** rewards *synchronizing* cycle timings across plants — strand length as a tuning fork. Players will deliberately pad strands with empty slots to phase-lock their choirs, which means empty slots become a *tool* (currently they're only a cost). Opus Magnum brains will bring metronomes.
**Why terrible:** phase-locking is fiddly; casual players hear chimes and never know why. Acceptable — it's a pure bonus, illegibility hurts nobody.

---

# 3. TIME & TICK ABUSE — WEGO AS A TOY

The tick is sacred and deterministic. These genes don't break determinism — they bend *which* ticks a plant experiences. Handle like plutonium.

### ✨ Haste Sap / Torpor Sap — 🟡 Passive pair, T2
**Mechanic:** this plant experiences 3 ticks per 2 world ticks (Haste) or 2 per 3 (Torpor). Torpor's trade: +50% effect on everything (slow metabolism, deep power).
**Why exciting:** per-plant time zones. A Torpor Bloom plant is a glacier that hits like one. Haste turret + Torpor artillery = the farm gains *tempo layers*, visible as different sway speeds — you can SEE time moving differently per tile. Gorgeous, readable, thematic.
**Why terrible:** fractional tick accounting must be exact (accumulator pattern, no floats drifting) or determinism dies. `ITickUpdateable` gets a rate multiplier — contained, but touching the game's heartbeat is never free.

### ✨ Prophecy Bulb — 🟢 Active, T2
**Fantasy:** the flower that dreams tomorrow.
**Mechanic:** once per day (dawn), reveals the next wave's composition banner (species + counts). No combat effect whatsoever.
**Why exciting:** information as a *plant product*. Into the Breach's whole thesis — perfect information makes placement decisions delicious — bought for one farm tile and a strand slot. The scent-trail scouting concept that was cut returns as buildcraft, where it belongs.
**Why terrible:** if forecast is too good, Planning solves the game before the wave runs; if the free baseline forecast (per Minigames doc — forecast stays free) is already decent, Prophecy is redundant. Resolution: free forecast = vague ("a large wave, from the east"); Prophecy = exact manifest. Clean graduate, honestly barely experimental.

### ✨ Echo Bloom — 🟡 Active, T3
**Fantasy:** the plant that lives one day behind.
**Mechanic:** records everything this plant's tile *received* today (damage instances, payload applications). Tomorrow, replays that log outward: damage taken yesterday is dealt back, healing received is re-emitted, at 75% strength.
**Why exciting:** a karma battery. Yesterday's beating becomes today's barrage. Feeding it healing all quiet-day makes tomorrow a healing fountain. The plant is a tape recorder and the farm's history becomes ammunition — no other gene makes *the past* a resource.
**Why terrible:** log storage per plant, replay targeting ambiguity (who receives the replay?), and a degenerate loop (two Echo Blooms feeding each other's logs — cap: Echo output is never recordable). Spectacular, expensive, wave-2-post-demo at best.

### ✨ Dormancy Contract — 🟢 Modifier, T2
**Fantasy:** the plant signs a deal with winter.
**Mechanic:** the plant skips its next K full cycles (fully dormant, invulnerable-ish per Deciduous rules), then fires its next cycle with all skipped cycles' energy income banked and a ×K effect multiplier on the first Active.
**Why exciting:** voluntary time-skip as a bet on *when the wave lands*. It's Bloom (main catalog) but player-tuned and riskier — sleep through the early wave and you've lost the farm. Reading the forecast (Prophecy synergy!) is the skill check. WeGo players love a scheduled apocalypse.
**Why terrible:** stacking with Trigger genes needs rules (dormant = triggers dead). Otherwise clean — strong graduation candidate as the "risk-timing" T2.

---

# 4. IDENTITY & MIMICRY — GENES THAT LIE

### ✨ Wildcard — 🟢 Modifier, T2
**Mechanic:** at day start, becomes a copy of the gene in the *previous* strand slot for the whole day.
**Why exciting:** a second copy of your best gene without owning a second copy — but locked to the previous-slot position, so strand ORDER decides what the Wildcard is. Doubles as Concentrate fuel (main catalog). Draft value: always useful, never exciting — the perfect "glue" rarity.
**Why terrible:** cloning genes with per-instance state needs care; clone `RuntimeGeneInstance` values fresh each dawn. Minor. Near-graduate.

### ✨ Chameleon Fruit — 🟢 Payload, T2
**Fantasy:** the fruit wears a mask.
**Mechanic:** attach to Fruit. The fruit *visually appears* as a different fruit type of your choosing (skull hidden, sparkle faked). Effects unchanged.
**Why exciting:** pure deception layer for the poison-apple economy. Bait that reads as safe food to... wait, pests don't read sprites. So who is deceived? **Doris mood system (design-only) and future multiplayer/streamer contexts aside — the player themselves.** Honestly: this gene only works if a future system (smart pests that avoid known-poison fruit, or the player's own inventory-reading skill check) gives the mask a target.
**Why terrible:** as above — deceiving nobody is art, not design. ☠️ until smart-pest memory exists; then it's 🟢 and wicked. Parked with a condition attached.

### ✨ Feral Reversion — 🟡 Passive, T3
**Fantasy:** the plant remembers being a weed.
**Mechanic:** if this plant's strand executes zero Actives for 3 consecutive cycles (energy starvation, trigger drought), it "goes feral": strand is replaced by a random aggressive wild-strand (from a curated table, seeded) until the next Planning phase.
**Why exciting:** failure produces chaos instead of silence. The energy-starved turret doesn't power down — it goes rogue, maybe better, maybe biting your other plants. Starvation becomes a slot machine you can *deliberately* trigger (underfeed on purpose for feral rolls). Noita energy: the game does something to you and you laugh.
**Why terrible:** randomness at the moment of failure can feel punitive; curation of the feral table is real content work. The *concept* — starvation has an output, not just an absence — should inform the main catalog even if this gene never ships.

### ✨ Doppelgänger — 🔴 Active, T3
**Mechanic:** grows a phantom copy of an adjacent plant on a free neighboring tile: same strand, 50% potency, dies at day's end.
**Why exciting:** your best build, twice, for one slot — but only if you keep a tile free next to the original (spatial cost) and only for a day (tempo). The nightly ritual of the phantom garden dissolving at dusk is a mood painting.
**Why terrible:** full plant instantiation with linked runtime state; every gene must behave correctly at 50% potency; edge cases breed here. Ship only after the executor is boring and stable.

---

# 5. THE LIVING FARM — CREATURES, DORIS & THE PLAYER AS GENE TARGETS

### ✨ Beast-Tongue — 🟡 Payload, T3
**Fantasy:** the fruit that teaches loyalty.
**Mechanic:** Dominate's gentle sibling. A creature that eats a Beast-Tongue fruit becomes a permanent farmhand: it stops eating crops and adopts one simple job (fetch fallen fruit to a basket / patrol-scare smaller pests) until it dies.
**Why exciting:** the pest wave is a *recruitment pool*. Converting a boar into staff is the diplomat build's endgame and the Manual→Automated principle wearing fur. Farmhands are also mortality-exposed automation — pests can kill your staff, so defense now protects *labor*, closing yet another loop between the game's halves.
**Why terrible:** it's an AI job system (fetch/patrol behaviors on `AnimalController`) — real subsystem. But the Doris Bowl/Fixtures thread (Commit & Watch doc) already walks this direction: a farmhand IS a mobile fixture. If fixtures ship, Beast-Tongue is their organic upgrade path. Genuine post-demo flagship candidate.

### ✨ Doris's Garden Plot — 🟡 Passive, T2
**Mechanic:** Doris treats this plant as *hers*: she naps beside it, defends it (weak scare pulse on approaching pests while she's there), and her digestion pellets drop on its tile.
**Why exciting:** finally, a gene about *where Doris hangs out* — space and companionship fused. The pellet-drop targeting means her gene-economy output lands where you choose. Pastoral, tactical, and it makes Doris's pathing an object of play instead of scenery.
**Why terrible:** needs Doris idle-behavior hooks; contested if multiple plots planted (rule: she picks the most recently grown — visible bond ribbon). Modest cost, high charm. Near-graduate once Doris systems firm up.

### ✨ Green Thumb — 🟢 Passive, T1
**Fantasy:** the plant likes you.
**Mechanic:** while the player character stands within 1 tile, this plant's cycle runs 1 tick faster.
**Why exciting:** *the player's body becomes a buff aura.* During the watch phase you're no longer a camera — where you STAND is a decision every tick. Ties directly into Commit & Watch's attention economy (early-act = Perfect timing asymmetry): tending is now literal proximity. Dirt cheap to build.
**Why terrible:** encourages standing still next to one plant (boring optimum). Fix: the bonus decays after 5 consecutive ticks on the same plant — you *make rounds* like a real gardener. With that patch: immediate graduate, demo-viable.

### ✨ Scarecrow Pact — 🟢 Payload, T2
**Mechanic:** attach to Fruit. Eating it yourself (not Doris) makes PESTS flee the *player* for N ticks — you become a walking Fear aura.
**Why exciting:** self-consumable combat buffs from the farm — the player's own body enters the payload matrix (food = buffs lane, currently unexplored: the matrix has a whole "on player" column nobody drew). Sprint into a wave, scatter it, herd pests into traps with your own two feet.
**Why terrible:** none structural — player status effects via existing StatusEffectManager. The real question is scope: a whole player-buff lane (Scarecrow, Haste-tea, Thorn-skin...) deserves its own doc if this lands. **Flag: possible "Player Payload Lane" future concept doc.**

---

# 6. RUN-MEMORY & LEGACY — GENES THAT OUTLIVE THE RUN

Roguelite heresy handled with tongs: nothing here grants raw power across runs — only *story* and *starting texture*, or the run-scoped kind of memory.

### ✨ Perennial — 🔴 Passive, T3
**Mechanic:** if alive at run's end (win or loss), this plant appears in your NEXT run's starting farm — as a wild, untamed version: strand intact but locked (can't edit until "re-domesticated" via a small in-run cost).
**Why exciting:** the emotional hook is enormous: your beloved turret survives the apocalypse and greets you, feral, next run. Meta-progression as *narrative continuity* rather than stat inflation — you re-tame your own history. One plant max (the most recently Perennial'd).
**Why terrible:** persistence infrastructure between runs doesn't exist; balance sensitivity (a T3 strand on turn 1, even locked, warps early rounds — hence the re-domestication gate). This is a marquee feature disguised as a gene. Park until meta-progression is designed at all.

### ✨ Ghost Graft — 🟡 Active, T3
**Mechanic:** targets the tile where one of your plants died *this run*. Raises a spectral copy: 50% potency, immune to pests (nothing to eat), fades after 2 days.
**Why exciting:** grief mechanics! The graveyard becomes a resource; catastrophic waves leave behind *fuel for necro-botany*. Pairs with Heirloom/Deadfall (main catalog) into a full death-economy suite — Abracodabra's cozy-dark identity crystallized: nothing on this farm is ever fully gone.
**Why terrible:** needs a death-site registry (cheap) and phantom-plant rendering/execution (Doppelgänger shares the cost — build once, use twice).

### ✨ Grimoire Seed — ☠️ (as a gene) / 🟡 (as a feature)
**Mechanic:** a seed that records the full strand of any plant that dies adjacent to it, archiving the build into a persistent player "grimoire" — a recipe book of every build you've ever lost.
**Why terrible-as-gene / great-as-feature:** this is UI/collection-book functionality cosplaying as a gene. Steal the *feature* (a build-archive/codex with "lost to the boars, Round 7" epitaphs — pure retention gold), drop the gene.

---

# 7. WORLD & WEATHER — GENES THAT TOUCH THE SKY

### ✨ Rainmaker — 🟡 Active, T3
**Mechanic:** channels for a full day (no other Actives fire); tomorrow, it rains: all plants +energy, all Fireseed suppressed, slugs +50% spawn weight (they love rain).
**Why exciting:** plants that program the WEATHER — the wave table and the energy economy bent by buildcraft. Weather as a player-authored global modifier means farm builds now have *diplomatic relations* with the sky. Wind (main catalog) + rain = a weather layer that both halves of the game read.
**Why terrible:** there is no weather system; this gene is a weather system's *pitch deck*. Costed accordingly.

### ✨ Lightning Rod — 🟢 Passive, T2 *(conditional on any storm/weather system existing)*
**Mechanic:** during storms, this plant absorbs strikes: massive energy influx, 1 leaf burned per strike, 1-ring stun pulse around it.
**Why exciting:** turns a hazard into a jackpot with a visible scar — the exact leaf-economy trade shape (L2) the game loves, delivered by the sky.

### ✨ Fog Bloom — 🟡 Active, T2
**Mechanic:** exhales fog over a 2-ring for the day: pests inside path *randomly* (deterministic seed) instead of toward targets; your plants inside can't target out.
**Why exciting:** area-denial via *confusion* rather than damage — a soft wall both sides pay for. Funnel builds gain a "chaos zone" tool: fog the left approach, and the only clear path walks the Tollbooth. Beautiful on screen (drifting fog = free atmosphere).
**Why terrible:** pathing intervention on `AnimalController`; "my turret won't shoot" confusion (mitigated: fog is extremely visible — that's the point).

### ✨ Deep Root Cellar — 🟢 Active, T1
**Mechanic:** payload-incompatible. Once mature, opens a 4-slot underground storage on its tile; items stored keep ripeness frozen (Ferment paused, nothing spoils).
**Why exciting:** the fruit-economy's missing refrigerator, delivered as a plant instead of a fixture — and the placement question (cellar near the orchard or near Doris?) is real spatial play. With Ferment (main catalog): precise aging control — pull the wine out of stasis exactly two days before the boar wave.
**Why terrible:** it's a fixture in a gene costume — but unlike Grimoire Seed, the costume WORKS here (tile-bound storage is spatial, plants ARE tile-bound). Immediate graduate if ripeness/spoilage ships; pointless before it.

---

# 8. ATTENTION GENES — COMMIT & WATCH AS A MECHANIC

The Commit & Watch doc names "day under-stimulation" as risk #1. These genes make *player attention itself* a resource the build economy can spend and reward. Dangerous, powerful, very on-thesis.

### ✨ Nightshade Shy — 🟢 Passive, T2
**Fantasy:** it only blooms when nobody watches.
**Mechanic:** +40% effect while the player character is ≥6 tiles away.
**Why exciting:** the anti-Green-Thumb. Together they partition your farm into "tend zone" and "trust zone" — your patrol route becomes a build decision, and the shy corner of the garden that thrives on neglect is characterization through mechanics. Watching it via the corner of the screen while pretending not to = players will roleplay this. They will.
**Why terrible:** none. It's one distance check. The only risk is that it's TOO charming and every plant wants a personality now. (...is that a risk?) **Flag: "plant temperament" as a whole passive lane — Shy, Jealous (weaker adjacent to higher-tier plants), Proud (stronger when its fruit fed Doris today). One check each. Cheap identity confetti.**

### ✨ Snooze Bud — 🟢 Active, T1
**Mechanic:** when it fires (rare — long natural cooldown), the game *auto-pauses* (via driver.SetPaused, the existing hook) and the plant presents a choice bubble: harvest its charged fruit now at 2× ripeness value, or let it keep charging.
**Why exciting:** a plant that ASKS FOR YOU. Event-density tooling (the ≥1/15–20s beat from the Commit & Watch doc) implemented as buildcraft — players who want more interrupts *plant* more interrupts. The lean-in economy becomes player-tunable, which is a better answer to under-stimulation than any scripted event cadence.
**Why terrible:** auto-pause frequency needs a global settings governor (already planned in the C&W doc) or five Snooze Buds = notification hell. Governed: graduate.

---

# 9. CURSED GENES — COZY-DARK'S BASEMENT

High power, visible price, informed consent. Every cursed gene states its price in red on the Spec Sheet. No gotchas — dread, not betrayal.

### ✨ Blood Tithe — 🟡 Modifier, T3
**Mechanic:** the next Active costs 0 energy. Instead, Doris gets 10% hungrier.
**Why terrible-but-right:** converting companion welfare into firepower is the darkest trade the game can offer while staying cozy — because the cost is *visible on her face* (hunger UI). Players who tithe too deep watch her mope. Self-limiting via guilt, mechanically via the starving spiral. The Doris relationship becomes load-bearing for a combat build — integration test passed in the most uncomfortable way.
**Risk:** if Doris's hunger is trivially refillable, the price is fake. Gate behind mid-game food scarcity. If player-hunger gets cut (per Minigames doc rec), this gene inherits the "hunger as cost" design space entirely.

### ✨ Strangler — 🟡 Passive, T3
**Mechanic:** each dawn, drains 1 leaf from every adjacent plant (they lose it; this plant gains +1 leaf each, past its cap, for the day).
**Why exciting:** a vampire plant you FARM AROUND — ring it with cheap Regrowth sacrifice-shrubs and it becomes an over-leafed monster (over-cap leaves = huge energy) fed by renewable donors. The sacrifice garden is visible: a mighty tree ringed by struggling shrubs. Dark pastoral perfection.
**Why terrible:** accidental adjacency ruins beginners' farms — needs a loud editor warning and a planting-preview drain indicator. With that: honestly shippable, wave-2.

### ✨ Rot Evangelist — 🟢 Payload, T3
**Mechanic:** attach to Fruit. If this fruit *spoils* (overripe decay, uneaten), the rot spreads: adjacent tiles' fruit instantly overripens; the rot spot becomes double-strength Fertilizer.
**Why exciting:** weaponized negligence. The failure state of the ripeness system becomes a strategy: deliberately rot a sacrificial orchard to fast-forward Ferment agings and fertilize a whole quarter. Somebody at a game jam is crying that they didn't think of this.
**Why terrible:** requires ripeness/spoilage (like Ferment). Chain rot needs a hop cap (2). Otherwise cheap and vile. Graduate alongside Ferment.

### ✨ The Gardener's Bargain — ☠️ Modifier, mythic
**Mechanic:** the plant's strand becomes UNEDITABLE for the rest of the run. In exchange: +1 strand slot and +15% all effects, compounding each day it survives.
**Why exciting:** the ultimate commitment device in a game about commitment phases — a plant you *marry*. By round 9 it's a monster you designed six rounds ago, thriving or haunting you.
**Why probably never:** punishes the game's core verb (editing). But the *shape* — escalating reward for not touching something — could return as a small "Undisturbed" passive (+2%/day, resets on edit). The tame version might be the actually-good version. That's this whole annex in one sentence.

---

# 10. THE DISTILLATION — WHAT SHOULD ESCAPE THIS GREENHOUSE

Survivors of their own autopsies, ranked by (impact × feasibility):

| Rank | Gene | Flag | Why it escapes |
|---|---|---|---|
| 1 | **Whisper** | 🟡 | Multi-plant strand-play; turns waste into transmission; genuinely novel genre-wide |
| 2 | **Green Thumb** (+ patrol decay) | 🟢 | Player-position-as-buff; demo-viable; Commit & Watch thesis in one check |
| 3 | **Nightshade Shy** (+ temperament lane) | 🟢 | One distance check; opens "plant personality" passives wholesale |
| 4 | **Prophecy Bulb** | 🟢 | Information-as-crop; resolves forecast design cleanly |
| 5 | **Snooze Bud** | 🟢 | Player-tunable interrupt density — the under-stimulation answer |
| 6 | **Dormancy Contract** | 🟢 | Risk-timing bet; pairs with Prophecy into a scheduling metagame |
| 7 | **Grudge** | 🟢 | Farm-develops-opinions; cheap, funny, story-generating |
| 8 | **Deep Root Cellar** | 🟢 | The refrigerator the fruit economy will beg for (post-ripeness) |
| 9 | **Ghost Graft** | 🟡 | Death-economy capstone; shares phantom infra with Doppelgänger |
| 10 | **Beast-Tongue** | 🟡 | Farmhand recruitment; the fixtures thread's organic endgame |
| 11 | **Blood Tithe** | 🟡 | Cozy-dark's sharpest trade; gated on food scarcity tuning |
| 12 | **Wildcard** | 🟢 | Draft glue; feeds Concentrate |

**Ideas to steal even if their genes die:** starvation-should-have-an-output (Feral Reversion), rotation-as-variety (Möbius), build-archive codex with epitaphs (Grimoire Seed), escalating undisturbed bonus (Gardener's Bargain), the unexplored "on player" payload column (Scarecrow Pact → possible *Player Payload Lane* concept doc), weather as buildcraft target (Rainmaker → weather system pitch).

**Parked with conditions:** Chameleon Fruit (needs smart-pest memory), Lightning Rod (needs weather), Rot Evangelist + Ferment interplay (needs ripeness), Perennial (needs meta-progression design), Fork (needs a stable buffer executor and a UI budget nobody has yet).

---

# 11. NEXT ACTION

**No decision required — this annex is a quarry's quarry.** When picking the demo-eight from the main doc (its §14 gate), consider whether **Green Thumb** or **Nightshade Shy** deserves to bump one of the eight (both are cheaper than anything on that list). Everything else here waits for its named system dependency.
