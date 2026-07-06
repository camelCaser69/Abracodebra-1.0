# Gene Systems — Design Document v6
## Abracodabra — DNA Strand Sequencer (Final Architecture)

---

# TABLE OF CONTENTS

1. [How This Fits Your Game](#1-how-this-fits-your-game)
2. [Energy System Fundamentals](#2-energy-system-fundamentals)
3. [The DNA Strand Sequencer](#3-the-dna-strand-sequencer)
4. [The Fruit Problem & Delivery Methods](#4-the-fruit-problem--delivery-methods)
5. [Plant Specialization & The Farm Ecosystem](#5-plant-specialization--the-farm-ecosystem)
6. [The Player's Hands — What You Do Besides Genes](#6-the-players-hands--what-you-do-besides-genes)
7. [Multi-Solution Design — No Two Runs Alike](#7-multi-solution-design--no-two-runs-alike)
8. [Gene Interaction Deep Dive — Where The Fun Lives](#8-gene-interaction-deep-dive--where-the-fun-lives)
9. [Tier Progression & Strand Growth](#9-tier-progression--strand-growth)
10. [Open Problems & Solutions](#10-open-problems--solutions)

---

# 1. HOW THIS FITS YOUR GAME

## The WeGo Loop

Abracodabra runs on a WeGo system with two phases:

**Planning Phase** — Time is frozen. The player moves around the farm (tick-based, Stoneshard-style), places seeds, edits gene strands, manages inventory, feeds Doris, forages, explores, and prepares defenses. Every action (move, tool use, plant) costs ticks. Once the player commits ("Start Day"), Planning ends.

**Growth & Threat Phase** — The TickManager advances automatically. Plants grow, mature, and execute their gene strands. Waves of pests spawn. Doris gets hungrier. The player watches their farm operate and can react (pick up fruit, throw items, reposition) but CANNOT edit gene strands. This is the "see the results" phase.

**Design implication**: Gene editing is always a bet. The player commits during Planning, watches during Growth. If the build is wrong, they wait until next Planning to fix it. Combined with tick costs for every action, the player must budget their time: spend ticks exploring and foraging, or stay home and optimize genes?

## What Already Exists In Code

| Component | Status | Relevance |
|---|---|---|
| `PlantSequenceExecutor` | ✅ Working | 1 slot per tick, left-to-right. Already a linear sequence executor. |
| `RuntimeSequenceSlot` | ✅ Working | Groups Active + Mods + Payloads per slot. **Slot model.** |
| `PlantGeneRuntimeState` | ✅ Working | Passive instances + active sequence. Passives separate from strand. |
| `SeedTemplate` | ✅ Working | passiveSlotCount, activeSequenceLength, energy params. |
| `PlantEnergySystem` | ✅ Working | Energy pool with per-leaf generation, spending, storage cap. |
| `PlantGrowthLogic` | ✅ Working | Applies passive stat multipliers (growth, energy, defense). |
| `PlantCellManager` | ✅ Working | Tracks individual leaves, stems, fruit as cells with grid coordinates. |
| `UISeedEditorController` | ✅ Working | Passive slots + Active/Mod/Payload rows with drag-drop. |
| `UISpecSheetController` | ✅ Working | Build preview with stats. |
| `PlayerActionManager` | ✅ Working | Tick-based action execution (Move, UseTool, PlantSeed, Harvest, Interact). |
| `PlayerHungerSystem` | ✅ Working | Player hunger increases per tick, starvation → Game Over. |
| `DorisHungerSystem` | ✅ Working | Doris hunger, feeding, starving → eats plants. |
| `AnimalController` | ✅ Working | Grid-based animal movement, needs, behaviors. |
| `StatusEffectManager` | ✅ Working | Per-entity status effect framework. |
| `WaveManager` | ✅ Working | Wave sequencing, spawning, round transitions. |
| Buffer-based strand parser | ❌ Not built | DNA buffer model needs new parser. |
| Continuous effects (Aura) | ❌ Not built | Always-on effects outside the sequence cursor. |
| Trigger-based Actives | ❌ Not built | Trap/Reactive Burst need event-driven firing. |
| Foraging / exploration | ❌ Not built | Manual food-gathering outside the farm. |

## The Implementation Path: Slot → Buffer

The current code uses the **slot model** (each `RuntimeSequenceSlot` explicitly assigns one modifier and one payload to one Active). The DNA Strand design uses the **buffer model** (flat gene list parsed left-to-right, modifiers accumulate, Actives consume). These are different architectures.

**For POC (now):** Stay with the slot model. It works, it's built, it proves genes are fun. The slot model is essentially a simplified DNA strand where each "group" is pre-assigned rather than parsed.

**For full release:** Migrate to buffer model. Flatten `RuntimeSequenceSlot` into a `List<RuntimeGeneInstance>`. Build a `SequenceParser` that reads left-to-right and produces execution groups. The `PlantSequenceExecutor` calls the parser once at strand load, then executes the computed groups identically to current logic. The `UISeedEditorController` becomes a horizontal gene strip. Moderate refactor — execution logic barely changes, just input format.

**Why this order matters:** Players need to PLAY the game to understand why gene order matters. Ship with slot model → players learn Active+Mod+Payload relationships → introduce buffer model → players go "oh, NOW I can rearrange for different results." The buffer model is more rewarding when players already understand the building blocks.

---

# 2. ENERGY SYSTEM FUNDAMENTALS

## How Energy Works

Every plant has an **energy pool** that fills and drains each growth cycle.

**Generation**: `SeedTemplate.energyRegenRate × leafCount × passiveMultiplier`. More leaves = more energy. A freshly matured plant with few leaves generates less than a fully grown one. Plants ramp up over time — early cycles are energy-starved, later cycles are energy-rich.

**Storage**: Pool caps at `SeedTemplate.maxEnergy × energyStorageMultiplier`. The `Deep Reserves` passive increases the multiplier. A larger pool lets the plant bank energy for expensive burst combos.

**Spending**: Active genes cost energy each time they fire (defined by `ActiveGene.baseEnergyCost`, modified by Efficiency/Overcharge). If the pool empties mid-strand, remaining Actives skip until the next cycle.

**Carry-over**: Unused energy persists between cycles, up to the cap. A plant with Trigger:Proximity that never fires banks energy every quiet cycle. This makes burst strategies viable: store energy during peace, unleash when triggered.

**Recharge**: After the strand completes (all slots executed or skipped), the plant enters recharge. No Actives fire. Duration = `SeedTemplate.baseRechargeTime` ticks. Energy continues generating during recharge.

## Why Energy-Per-Leaf Matters

The per-leaf model creates a natural power curve:

- Freshly mature plant (few leaves) → might afford 1 Active per cycle
- Fully grown plant (max leaves) → might afford 3-4 Actives per cycle
- `Swift Growth` passive → faster leaf growth → earlier full power
- `Energy Roots` passive → more energy per leaf → higher budget per cycle
- Combat plants need to survive long enough to grow leaves → early rounds, they're weak; late rounds, powerhouses

This means the SAME gene build behaves differently depending on plant age. A complex 6-slot strand might only fire 2-3 slots in early cycles, then fire the full strand once mature. The player watches their plant "come online" gradually. Satisfying.

## Plant Vitality — Leaves Are Life

Plants have no abstract HP bar. A plant's health IS its structure — specifically, its ability to produce energy. When a plant can no longer sustain itself, it dies.

### The Core Rule: Leaves = Health

Every leaf is a photosynthetic organ. Losing a leaf directly reduces energy generation. A plant with 6 leaves generating 2 energy/leaf/tick produces 12 energy/tick. Lose 3 leaves: 6 energy/tick. Lose all 6: zero. A complex strand that requires 10 energy per cycle can't fire when the plant is down to 3 leaves. The plant doesn't die from an abstract number reaching zero — it dies because it can no longer function.

### How Plants Take Damage

Pests don't deal abstract damage — they **eat leaves.** Each pest type has an `eatSpeed` that determines how many ticks it takes to consume a single leaf:

| Pest Type | Eat Speed | Effect |
|---|---|---|
| Insects (aphids, beetles) | 3 ticks per leaf | Slow but persistent. Swarm strips a plant over time. |
| Small (slugs, mice) | 2 ticks per leaf | Moderate. One slug is manageable. Three is a problem. |
| Medium (rabbits, crows) | 1 tick per leaf | Fast. Immediate pressure. |
| Large (boars, bears) | 1 tick per leaf + eats stems | Devastating. Can destroy the entire plant structure. |

This means combat is inherently visual — the player SEES their plant being stripped. No HP bar required. The plant itself IS the health indicator.

### Leaf Durability (Defense)

Each leaf has a `durability` value that determines how many eat-ticks a pest must spend before the leaf is consumed. Default durability = 1 (consumed at the pest's normal rate). The **Thick Bark** passive multiplies durability:

- No Thick Bark: durability 1 (normal)
- Thick Bark ×1: durability 2 (pests take twice as long per leaf)
- Thick Bark ×2: durability 3 (three times as long)

A pest with eatSpeed=2 vs a durability-3 leaf: 2 × 3 = 6 ticks to consume. Thick Bark is visible — the player sees pests gnawing uselessly on tough leaves while combat plants eliminate them.

### Leaf Regrowth (Healing)

Lost leaves can be **regrown** by the Healing payload and the Regrowth passive. Regrowth picks a previously-destroyed leaf position and respawns it with a sprouting animation. Leaves can't exceed the plant's maximum leaf count.

- **Regrowth passive**: auto-regrows 1 lost leaf every N ticks (default: 5 ticks)
- **Healing Cloud/Burst**: regrows a lost leaf on the target plant, at 50% speed compared to creature healing (slower regrowth = takes longer per leaf)
- Regrowth is capped at max leaf count — can't grow beyond what the plant originally had

### Plant Death — The Withering Window

When a plant reaches 0 active leaves AND has no alternative energy sources, it enters a **Withering** state:

1. **Withering** (2-3 ticks): Plant turns brown, droops visually. No energy generation, no gene execution. BUT — if a Healing effect regrows even one leaf during this window, the plant survives. This creates dramatic rescue moments.
2. **Death** (after withering expires): Plant collapses, is removed from the grid. Gone.

The withering window is critical — it gives the gene system time to save the plant. A Healing Cloud from a neighbor plant regrowing a single leaf on the dying plant cancels the withering. The player watches their healer rescue a dying combat plant in the nick of time. That's a story.

### Visual States

| Leaf % | Visual | Gameplay Effect |
|---|---|---|
| 100% | Full, vibrant plant | Full energy generation, all strand slots can fire |
| 75% | Slightly thinned | 75% energy, expensive strands may skip late slots |
| 50% | Noticeably damaged, visible gaps | 50% energy, only moderate Actives can fire |
| 25% | Barely alive, mostly stems | 25% energy, only cheapest Actives work |
| 0% (withering) | Brown, drooping, stem-only | 0 energy, 2-3 tick window before death |
| Dead | Collapsed/wilted/removed | Removed from game |

No HP bar is displayed. The plant's physical appearance IS the health readout. The energy bar (yellow) remains — it shows current energy pool, which naturally reflects leaf count since fewer leaves = less generation.

### Alternative Energy Sources (Post-POC)

Leaves are the PRIMARY energy source, but the architecture supports future alternatives:

| Source | How It Works | Gene/Mechanic |
|---|---|---|
| **Leaves** (primary) | `energyPerLeaf × activeLeafCount × sunlight` | Default for all plants |
| **Mycorrhizal Roots** | `energyPerMushroom × adjacentMushroomCount` | Mycorrhizal Network passive |
| **Carnivorous Trap** | `energyPerCapturedPest × capturedPests` | Carnivorous Trap active |
| **Root Network** | `sharedEnergyFromNeighbors` | Root Network active (already in v5 design) |
| **Symbiotic Creatures** | `energyPerFriendlyCreature × nearbyFriendlies` | Charm payload + future passive |
| **Decomposition** | `energyFromNearbyCorpses` | Future: scavenger plant archetype |

A leafless plant on a Mycorrhizal Network survives — diminished, unusual-looking, but functional. This opens late-game builds where the "health" system is genuinely different from the default. The death condition is `totalEnergyCapacity <= 0` — not just leaf count, but ALL sources.

### Self-Damage (Explosive, Volatile, etc.)

Some gene builds damage the plant itself. In the leaf model, self-damage = **destroying one of the plant's own leaves**:

- **Explosive Aura**: Destroys 1 of the plant's leaves every N explosion ticks (configurable)
- **Volatile modifier**: 15% chance per Active execution to destroy 1 leaf
- **Chain Reactor**: Explosions from adjacent plants destroy leaves on neighbors

Self-damage creates a visible cycle: the plant loses leaves → generates less energy → fires less → takes less self-damage → leaves regrow (if Regrowth exists) → fires more → repeat. This oscillating behavior is fascinating to watch and optimize. With abstract HP, it's just a number going down.

---

# 3. THE DNA STRAND SEQUENCER

## Core Concept

A linear sequence of genes read left-to-right, one per tick. Order matters. Same genes, different arrangement = different plant. This is the Noita wand model applied to plant genetics.

## The Three Rules (Buffer Model)

**Rule 1: Modifiers ACCUMULATE in a buffer.** Reading left-to-right, every Modifier gene encountered goes into a temporary buffer. Nothing fires yet.

**Rule 2: When an Active is reached, it CONSUMES the buffer.** All accumulated modifiers apply to this Active. The buffer empties. The Active fires (if energy permits).

**Rule 3: Payloads ATTACH to the most recent Active.** After an Active fires, subsequent Payload genes attach to it until the next Modifier or Active is reached.

```
Strand:  [Efficiency] [Multicast] [Cloud] [Poison] [Slow] [Fruit] [Nutritious]
          ——— buffer ————————————→  │← payloads ——→│      │← pay →│
                                  ▼                       ▼
                     Cloud fires with Efficiency          Fruit fires ALONE,
                     + Multicast applied,                 delivers Nutritious only
                     delivers Poison + Slow
```

**The Wrap Trick**: Modifiers at the END of the strand carry over to the FIRST Active on the next loop. `[Cloud][Poison][Efficiency]` → first cycle: Cloud fires unmodified. Second cycle onwards: Efficiency wraps → Cloud fires cheap. An advanced strategy that rewards understanding the loop.

## Same Genes, Different Order — The Core Magic

**Build A**: `[Cloud] [Poison] [Efficiency] [Fruit] [Nutritious]`
- Cloud fires unmodified → full-price poison cloud
- Efficiency buffers → Fruit fires cheap → delivers Nutritious
- Result: full-price defense + cheap food

**Build B**: `[Efficiency] [Cloud] [Poison] [Fruit] [Nutritious]`
- Efficiency buffers → Cloud fires cheap → delivers Poison
- Fruit fires unmodified → delivers Nutritious
- Result: cheap defense + full-price food

**Build C**: `[Efficiency] [Fruit] [Nutritious] [Poison] [Cloud]`
- Efficiency buffers → Fruit fires cheap → Nutritious AND Poison attach
- Cloud fires unmodified → no payloads left → harmless mist (wasted)
- Result: cheap poisoned food + wasted Cloud

Three arrangements. Same five genes. Three different plants. THIS is why the buffer model matters. The player rearranges genes and sees completely different outcomes. The editor is a puzzle box, not a form to fill.

## Cycle Time = Strategic Lever

Each slot takes 1 tick to process. A 3-slot strand cycles every 3 ticks + recharge. A 7-slot strand cycles every 7 ticks + recharge. Shorter strands are FASTER but less powerful per cycle. Longer strands are slower but pack more effects.

This means "add more genes" has a COST — not just energy, but TIME. A turret plant with `[Projectile][Poison]` fires every 2+recharge ticks. Adding Efficiency to make it cheaper lengthens the cycle to 3+recharge. Is the energy savings worth the slower fire rate? THAT is a real decision.

## Passive Backbone

Passives sit in a separate row below the strand. Always-on stats. No sequencing. Fixed count by seed tier (3/4/6 slots). They modify the plant globally: growth speed, energy generation, energy storage, leaf durability, fruit yield, leaf regrowth, etc.

Passives are the "foundation" — they determine what the plant CAN do. The strand determines what it DOES.

### Core Passive Genes

| Passive | Effect | Stacking |
|---|---|---|
| **Swift Growth** | Faster leaf/stem growth → earlier maturity, earlier full power | Additive: ×1.25, ×1.5, ×1.75... |
| **Energy Roots** | +25% energy generation per leaf | Additive |
| **Deep Reserves** | +30% max energy storage | Additive |
| **Thick Bark** | Leaf durability ×2 per stack. Pests take twice as long per leaf. | Multiplicative: ×2, ×3, ×4... |
| **Regrowth** | Auto-regrows 1 lost leaf every N ticks (default: 5) | Faster: every 5, 4, 3 ticks... |
| **Thorned Leaves** | Pests take damage when they consume a leaf | Additive damage per stack |
| **Spreading Roots** | Seeds from this plant inherit passives | Binary (has or doesn't) |
| **Terrain Affinity** | Tile-specific growth/energy bonuses | Per terrain type |
| **Iron Roots** | Flat bonus to leaf durability on top of Thick Bark | Additive: +1, +2, +3... |

**Thick Bark + Iron Roots example:** Base durability 1 + Iron Roots (+1) = 2. Then Thick Bark ×2 = durability 4. A pest with eatSpeed=2 takes 8 ticks to eat one leaf. Food plants with this combo are nearly indestructible to small pests.

**Regrowth + Thick Bark interaction:** Regrowth passives make the plant regenerate lost leaves. Thick Bark makes each regrown leaf harder to eat again. Together they create extremely resilient plants. But both are passive slots — spending 3 of your 3 T1 passive slots on defense means no Swift Growth, no Energy Roots. The defensive plant is tough but weak. Trade-offs.

## How Continuous & Trigger Actives Work In The Strand

The strand executor advances one slot per tick. Most Actives (Fruit, Cloud, Projectile, Seedpod) fire when reached and the cursor moves on. But four Actives have special behavior:

**Aura / Root Network (continuous):** When the cursor reaches this slot, the effect ACTIVATES and persists independently. It drains energy per tick continuously, competing with other Actives for the pool. On the next loop, reaching the slot again confirms it's running (no re-activation). Deactivates only when energy depletes or the plant dies. The cursor advances immediately — no tick spent waiting.

**Trap / Reactive Burst (trigger-based):** When the cursor reaches this slot, the effect ARMS. The cursor advances immediately (no tick spent). The armed effect fires on an external event (creature contact for Trap, **leaf consumed for Reactive Burst**). After firing, cooldown starts. Re-arms on the next loop pass.

**Reactive Burst trigger — leaf consumption:** In the leaf-vitality model, Reactive Burst fires when one of the plant's leaves is consumed (by a pest, by self-damage, by any source). Each leaf death = one potential trigger (limited by cooldown). A plant being stripped by a swarm triggers Reactive Burst repeatedly — each eaten leaf = an explosion. A plant with Thick Bark triggers LESS often because leaves take longer to eat. This creates a real build decision: skip Thick Bark on a Reactive Burst plant to trigger more explosions at the cost of faster death.

**In all cases, trigger/persistent Actives don't "block" the strand.** `[Trap][Poison][Fruit][Nutritious]` is effectively a 2-tick cycle (Fruit + Nutritious) with a trap running independently. The trap and its payload don't add to cycle time. This makes them efficient to include — no speed penalty for arming a trap.

## The Editor Experience

**As the player drags genes into the strand, the UI shows:**
- Colored brackets grouping which modifiers apply to which Active
- Payload arrows showing which payloads attach to which Active
- Per-cycle breakdown in the Spec Sheet: "Cycle 1: Cloud (8E, Efficiency applied) → Poison + Slow. Cycle 2: Fruit (8E) → Nutritious."
- Warning icons on wasted genes (Payload after no Active, Modifier at end with no wrap target)
- **Leaf balance indicator**: For builds with self-damage (Explosive, Volatile), the Spec Sheet shows projected leaf consumption rate vs regrowth rate: "Self-damage: ~1 leaf/3 ticks. Regrowth: 1 leaf/5 ticks. Net: -1 leaf per 7.5 ticks. Estimated lifespan: ~45 ticks with 6 leaves." Actionable: "Add 1 Regrowth passive to sustain."

**Real-time preview prevents confusion.** The buffer model's learning curve is steep for new players. The editor UI must compensate by making the parsing visible.

---

# 4. THE FRUIT PROBLEM & DELIVERY METHODS

## The Core Tension

Most early gene sequences produce fruit — a delayed output. Long wait between editing and seeing results, compared to Noita's instant fire.

## Why It's The Right Design

Abracodabra is a farm. The delay is a feature:

**Strategic stockpiling.** Fruits are physical inventory objects. Hoard healing fruit for a hard wave. Stockpile poison bait for a boss pest. Bank Doris food for when you're busy defending.

**Player agency over timing.** YOU decide when fruit is used. Feed to Doris, throw at a pest, eat yourself, or leave as bait. A Nutritious + Poison fruit is food OR a trap depending on deployment.

**Thematic truth.** Plants grow things. Farms produce harvests. The delay IS the genre.

The real issue isn't delay — it's **legibility**. Players need to see what the fruit will do (Spec Sheet previews this) and get visual feedback during growth (color-coded fruit).

## Two Output Branches

### Branch A: Fruit (Delayed, Intentional)
Physical object. Harvestable. Stored in inventory. Player-deployed. The existing `UISpecSheetController` previews effects. Visual encoding: gold = nutritious, green = poisonous, blue = frozen, purple = psychic, red = explosive.

### Branch B: Ambient/Reactive (Instant, Automatic)
Actives that fire their effect directly into the world:

| Output Type | Timing | Example |
|---|---|---|
| Periodic | Fires every cycle | Poison cloud, projectile volley |
| Constant | Always on, drains energy | Fear aura, healing zone |
| Reactive | Fires when triggered | Explosion on leaf consumed, trap on contact |

## The Eight Delivery Methods

```
PAYLOAD-COMPATIBLE (6):
  Fruit          → physical object, harvestable, player-deployed
  Cloud          → area effect around plant, periodic, fades after N ticks
  Projectile     → aimed single-target shot, periodic
  Aura           → persistent field, constant energy drain
  Trap           → arms tile, fires on creature contact, 0 energy
  Reactive Burst → fires on leaf consumed, AoE, 0 energy

PAYLOAD-INCOMPATIBLE (2):
  Seedpod        → produces plantable seed (gene container)
  Root Network   → buffs adjacent plants' energy (systemic)
```

## The Payload × Delivery Matrix

| Payload | on Fruit | on Cloud | on Projectile | on Aura | on Trap | on Reactive Burst |
|---|---|---|---|---|---|---|
| **Nutritious** | Food (+nutrition) | Food scent (lures) | — *wasted* | Food scent field (lure) | Bait trap | — *wasted* |
| **Poison** | Poisoned food | Poison gas | Poison dart | Toxic field | Poisoned snare | Poison nova |
| **Fireseed** | Incendiary fruit | Fire cloud (spreads) | Fire bolt | Heat field | Fire mine | Fire nova |
| **Freeze** | Frost fruit (+1 stack) | Frost cloud (+1/tick) | Ice bolt (+1 stack) | Frost field (+1/tick) | Cryo trap (+2 stacks) | Frost nova (+1 all) |
| **Explosive** | Grenade (AoE) | Concussive cloud | Rocket (AoE) | Pulse field (strips own leaves) | Land mine | Mega-blast |
| **Charm** | Diplomacy food | Charm cloud | Charm bolt | Peace field | Catch-and-calm | Charm pulse |
| **Fear** | Terror food | Fear gas | Scare bolt | Scarecrow field | Scare trap | Panic wave |
| **Dominate** | Mind-control food | Recruitment cloud | Command bolt | Command field | Recruit trap | Rally wave |
| **Slow** | Sluggish food | Bog cloud | Slowing bolt | Quicksand field | Sticky trap | Slow wave |
| **Healing** | Healing food (+nutrition) | Leaf regrowth mist (50% speed on plants) | Healing bolt | Regen field (50% speed on plants) | — *wasted* | Leaf regrowth burst |

**Wasted entries**: Allowed but warned. The editor tells you "Nutritious has no effect on Projectile" but lets you place it. Experimentation is never punished with a hard block — just with honest feedback.

**Nutritious dual behavior**: On Fruit = actual food (satisfies hunger). On everything else = food SCENT (attracts creatures, doesn't feed them). The lure mechanic.

**Explosive + Aura**: Allowed. Periodic AoE pulse that damages everything nearby including the plant's own leaves. Editor warns "This build will consume its own leaves." Shows estimated leaf balance. Let the player build it and learn.

**Healing on plants**: Healing effects REGROW lost leaves at 50% speed compared to creature healing. A Healing Cloud that restores 1 creature HP per tick instead regrows 1 leaf every 2 ticks on a plant. The healer keeps a damaged combat plant alive by resprouting its foliage. Visually spectacular — the player watches stripped stems sprout fresh green leaves in a healing mist.

---

# 5. PLANT SPECIALIZATION & THE FARM ECOSYSTEM

## How Specialization Emerges

The player never picks a "type." Active + Payload determines archetype:

| Active Gene | + Payload | = Archetype |
|---|---|---|
| Fruit | Nutritious | Food producer |
| Fruit | Poison | Bait factory |
| Fruit | Healing | Medicine cabinet |
| Cloud | Poison | Area denial |
| Cloud | Slow | Crowd control |
| Cloud | Healing | Plant hospital (regrows neighbors' leaves) |
| Projectile | Poison | Turret |
| Projectile | Freeze | Cryo cannon |
| Aura | Fear | Scarecrow |
| Aura | Nutritious | Bait station |
| Trap | Poison | Kill box |
| Trap | Freeze | Cryo snare |
| Reactive Burst | Explosive | Living land mine (each eaten leaf = explosion) |
| Root Network | *(none)* | Energy hub |
| Seedpod | *(none)* | Seed factory |

15+ archetypes from 8 Actives × 10 Payloads. Modifiers multiply further. Strand ORDER multiplies again.

## The Farm Tension Loop

1. **You need food plants** (Fruit + Nutritious) to feed Doris and yourself
2. **Food plants are defenseless** — pests eat their leaves, stripping their energy capacity
3. **You need combat plants** to protect food → but combat plants produce no food
4. **You must balance** producers vs protectors across limited farm tiles
5. **Doris gets hungrier** each round → need more food → need more protection
6. **Higher-tier pests arrive** → need stronger combat → takes gene slots from food
7. **Gene editing is locked to Planning** → you commit before seeing the wave
8. **Every Planning action costs ticks** → time spent exploring = less time optimizing

Step 8 is new and critical: the player's TIME is a resource. Spending ticks foraging delays when you can Start Day. But starting too early means less food stockpiled. The tension isn't just genes vs genes — it's player time vs plant time.

**Leaf vitality deepens step 2:** In the leaf model, pest damage is gradual and visible. A food plant that's lost half its leaves still works — it just produces fruit more slowly (less energy per cycle). The player might choose to tolerate partial damage on a food plant rather than dedicating an entire tile to defense. "My cornstalk only has 3 of 6 leaves, but it still fires Fruit + Nutritious every cycle. It's slow but it's alive." That's an interesting micro-decision that didn't exist with binary alive/dead HP.

## Inter-Plant Synergies (DNA Strand Perspective)

In the DNA strand model, synergies come from MULTIPLE PLANTS working together (since one strand = one plant's behavior):

**The Funnel**: Plant A (Aura+Nutritious = lure) → Plant B (Cloud+Slow = bog down) → Plant C (Projectile+Poison = kill). Three plants, one killing corridor. Pest pathing matters.

**The Shield Wall**: Ring of Trap+Poison plants around food plants. Pests trigger traps on approach.

**The Root Network Hub**: One Root Network plant in center, buffing all neighbors' energy. Makes expensive builds viable within its range. Also keeps leafless plants alive — a combat plant that's lost all leaves but sits next to a Root Network hub still receives shared energy and can keep firing.

**The Healing Symbiosis**: Cloud+Healing plant regrowing leaves on a Reactive Burst+Explosive plant. The healer offsets the bomber's leaf loss from self-damage. The player watches leaves die from explosions and sprout back from healing mist — a visible living cycle.

**The Freeze Lock**: Trap+Freeze plant (2 stacks on contact) adjacent to Cloud+Freeze plant (1 stack/tick). Creature triggers trap: 2 stacks. Held in cloud range: 3 stacks next tick → FROZEN. Two plants, 6 ticks of total lockdown.

---

# 6. THE PLAYER'S HANDS — WHAT YOU DO BESIDES GENES

*This section addresses the critical question: what does the player DO, especially in the early game before genes dominate? The answer: a lot. Genes are the mid-to-late game optimization engine. The early game is about SURVIVAL THROUGH DIRECT ACTION.*

## Design Philosophy: The Automation Curve

```
EARLY GAME (Rounds 1-3):
  Player does 80% manually, plants do 20%
  → Foraging, manual feeding, scaring animals, exploring

MID GAME (Rounds 4-7):
  Player does 40% manually, plants do 60%
  → Gene systems taking over food/defense, player optimizes and explores

LATE GAME (Rounds 8+):
  Player does 10% manually, plants do 90%
  → Farm runs itself, player makes strategic gene decisions between waves
```

The game starts as a survival roguelike where you scramble to feed yourself and Doris with your bare hands. It BECOMES a tower defense where your gene builds handle everything. The transition from manual to automated is the core progression arc. Genes don't replace player skill — they REWARD it. The player who forages smartly in early rounds has more time and resources to build better gene sequences by mid game.

## 6.1 FORAGING — Manual Food Gathering

**The problem foraging solves:** Round 1, the player has seeds but no mature plants. Doris is hungry NOW. The player needs food before any plant produces fruit.

### Wild Forageable Items

The map has scattered forageable nodes. Each costs ticks to gather (you walk there + spend an action tick). They respawn slowly or not at all.

| Forageable | Where Found | Tick Cost | Nutrition | Special |
|---|---|---|---|---|
| **Wild Berries** | Forest edges, bushes | 1 tick | 15 nutrition | Common. Reliable early food. |
| **Mushrooms** | Shaded areas, caves | 1 tick | 20 nutrition | Some are poisonous (visual tells). High risk/reward. |
| **Grubs & Insects** | Under rocks, logs | 2 ticks (dig + collect) | 10 nutrition | Also usable as bait for animals. |
| **Wild Roots** | Marsh, riverbanks | 2 ticks (dig + wash) | 25 nutrition | Rare. Worth the trip. |
| **Bird Eggs** | Nests (trees, cliffs) | 1 tick (if reachable) | 30 nutrition | Risky — parent bird may attack. |
| **Honeycomb** | Bee hives | 3 ticks (smoke + extract) | 40 nutrition | Very valuable but alerts bees (temporary pest wave). |

### Foraging Design Rules

**Diminishing returns on nearby tiles.** The tiles closest to the farm have sparse forageables. Rich patches are 5-10 tiles away. Walking there and back costs 10-20 ticks. This creates the tension: forage far for better food (spending ticks) or stay home and optimize genes (investing in future)?

**Forageables deplete.** Wild berry bushes produce 2-3 berries then go dormant for several rounds. The player can't rely on foraging forever — it's a BRIDGE to plant-based food production, not a permanent strategy. This is the gentle push toward gene mastery.

**Poisonous look-alikes.** Some mushrooms are safe (brown caps), some are poisonous (spotted caps). Early game, the player learns to read visual tells. Later, a plant with `[Fruit][Poison]` makes identical-looking poisoned food — the visual literacy transfers.

**Foraged items go in inventory.** Same slot system as harvested fruit. Can be fed to Doris, eaten by the player, or used as bait/trade. No separate "foraging inventory."

### Why This Matters For Genes

Foraging teaches the player the food economy BEFORE genes complicate it. They learn: food has nutrition values, Doris has preferences, some food is dangerous, inventory is limited. When Fruit + Nutritious enters the picture, the player already understands the system it plugs into.

## 6.2 ANIMAL INTERACTIONS — Before Combat Plants Exist

Early game animals aren't waves of hostile pests — they're curious, hungry, or skittish creatures that show up organically. The player interacts with them MANUALLY before combat plants automate defense.

### Early Animal Behaviors

| Animal | Behavior | Manual Interaction | Gene Equivalent (Later) |
|---|---|---|---|
| **Rabbits** | Nibble at planted seeds | Shoo away (walk toward them, 1 tick) | Fear Aura automates this |
| **Crows** | Steal harvested fruit from ground | Throw rock (1 tick, aim check) | Projectile scares them off |
| **Slugs** | Slowly eat plant leaves (visible leaf loss) | Pick up and relocate (1 tick each) | Trap catches them |
| **Beetles** | Burrow near roots, eat leaves from below | Dig out (2 ticks with hoe) | Poison Cloud kills them |
| **Field Mice** | Steal seeds from inventory (!) | Set manual bait trap (place food + wait) | Trap + Nutritious does this |
| **Friendly Bees** | Pollinate nearby plants (+growth) | Leave flower offerings (foraged) to attract | Aura + Nutritious lures |

### The Key Insight: Manual Actions Teach Gene Behaviors

Every early manual interaction has a gene equivalent that automates it later:

- Shooing rabbits → Fear Aura
- Throwing rocks at crows → Projectile
- Picking slugs off plants → Trap
- Digging beetles → Poison (kills underground)
- Baiting mice → Trap + Nutritious
- Attracting bees → Aura + Nutritious (lure)

The player learns the CONCEPT through manual action, then discovers the GENE that does it automatically. "Wait, I've been shooing rabbits by hand for three rounds... this Fear Aura gene does it FOREVER?" That's the dopamine hit. The gene system doesn't feel abstract because the player already solved each problem with their hands.

**Leaf vitality connection:** The player's first encounter with leaf damage is slugs and beetles eating leaves off their plants in Round 1-2. They see the leaf disappear, notice the plant producing less energy, and learn the relationship between structure and function BEFORE genes introduce self-damage or defensive passives. When Thick Bark shows up later, the player immediately understands: "This makes my leaves harder for those slugs to chew."

### Befriending vs Repelling

Not all animals are pests. Some are beneficial:

**Bees** boost plant growth speed when near flowering plants. The player can attract them with foraged wildflowers placed near the farm. Later, Aura + Nutritious does this automatically (food scent field = bee magnet).

**Ladybugs** eat aphids (a tiny pest that eats plant leaves). The player can manually relocate ladybugs from the wild to their farm. Later, Dominate genes recruit insects permanently.

**Worms** improve soil quality (hidden stat that boosts energy-per-leaf for plants in that tile). The player finds them while digging. Moving worms to farm tiles is a powerful early investment. No gene equivalent — this stays a manual advantage for observant players.

**This creates a choice:** Kill all animals with combat plants? Or selectively protect beneficial ones? The player who learns which animals help and which hurt makes better gene decisions. A Fear Aura scares EVERYTHING — including friendly bees. A smarter player uses Trigger:Proximity + Projectile (only fires at hostile pests) instead.

## 6.3 EXPLORATION & DISCOVERY — Why Leave The Farm

The map extends beyond the farm. Exploration costs ticks but yields unique rewards.

### What's Out There

| Discovery | Distance | Reward | Tick Investment |
|---|---|---|---|
| **Abandoned Garden** | 8-12 tiles | 1-2 wild plants with random genes (harvestable seed) | ~20 ticks round trip |
| **Mushroom Grotto** | 6-10 tiles | Reliable mushroom source + rare Healing mushrooms | ~15 ticks |
| **Insect Colony** | 10-15 tiles | Grub supply + chance to find beetle-resistant seed | ~25 ticks |
| **Ancient Stump** | 15-20 tiles | Contains a rare T3 gene guaranteed | ~35 ticks |
| **Animal Den** | 8-12 tiles | Befriend animal type (won't attack your farm) | ~20 ticks + food offering |
| **Crystal Cave** | 20+ tiles | Gene Mutation Stone (reroll one gene slot) | ~40 ticks |

### Exploration as Gene Acquisition

The primary way to get NEW genes (beyond round rewards) is discovering them in the world:

**Wild plants** found during exploration can be harvested for seeds. These seeds have random gene configurations — some good, some bad. The player extracts them, examines the strand in the Seed Editor, and decides: plant as-is, or cannibalize for individual genes?

**Gene Extraction**: During Planning, the player can DISASSEMBLE a seed to recover individual genes. A seed with `[Cloud][Poison][Fruit][Nutritious]` yields 4 separate genes for the player's gene inventory. This destroys the seed but provides building blocks for custom strands. The choice: plant a mediocre wild seed as-is (quick, cheap) or break it for parts (delayed, flexible)?

**Rare nodes** (Ancient Stump, Crystal Cave) guarantee high-tier genes. They're far away and cost many ticks. The player who scouts early rounds discovers them, then plans a ticked expedition when they can afford the time investment. This rewards map awareness and planning across multiple rounds.

### The Expedition Decision

Every tick spent exploring is a tick NOT spent:
- Watering plants (growth speed depends on watering during Planning)
- Optimizing gene strands
- Feeding Doris preventatively
- Preparing manual defenses

A player who explores Round 1 might find a Poison gene that saves Round 3. A player who stays home Round 1 has more optimized Round 1 genes but fewer options in Round 3. Both are valid. The game never tells you which is better — it lets you discover through play.

## 6.4 MANUAL TOOLS & ACTIONS

The player has tools that interact with the farm directly. These start as the PRIMARY way to farm and gradually become supplementary as genes take over.

### Tool Progression (Early → Mid → Late)

| Tool | Early Game Use | Gene That Replaces It | When Replacement Kicks In |
|---|---|---|---|
| **Watering Can** | Water plants to grow (1 tick/tile) | Never fully replaced — watering always helps, but Swift Growth passive reduces dependency | Mid game |
| **Gardening Hoe** | Till soil for planting (1 tick/tile) | Never replaced — always needed for new plots | Never |
| **Harvest Pouch** | Pick fruit off plants (1 tick) | Auto-harvest when fruit ripens (potential future passive) | Late game |
| **Throwing Rocks** | Scare/damage animals (1 tick, aim) | Projectile gene, Fear gene | Mid game |
| **Shovel** | Dig up foragables, move worms, remove dead plants | Partial: genes prevent leaf loss. Digging stays manual. | Partially mid game |
| **Bait Trap (craftable)** | Place food item on tile, lures one animal | Trap + Nutritious gene | Mid game |
| **Grafting Shears** | Transplant a leaf from one plant to another (2 ticks) | Healing Cloud automates this | Mid game |

### Crafting (Minimal)

The player can combine foraged items into simple tools at no tick cost during Planning (menu-based, not a crafting table):

- **Bait Trap**: 1 foraged food item → places a tile that lures one animal and holds it for 3 ticks. The manual version of Trap+Nutritious.
- **Smoke Bundle**: 3 mushrooms → creates a 1-tile cloud that scares all animals for 2 ticks. The manual version of Cloud+Fear.
- **Fertilizer**: 2 grubs + 1 wild root → apply to one planted tile, +50% growth speed for one round. The manual version of stacking Swift Growth passives.

Crafting is NOT a core loop. It's 3-5 recipes max, all designed to bridge the gap before genes do it better. If the player is still crafting Smoke Bundles in Round 8, something went wrong with their gene builds.

### Grafting — Manual Leaf Transplant

A new tool action enabled by the leaf-vitality model. During Planning or Growth, the player can spend 2 ticks to transplant one leaf from a healthy plant to a damaged one:

- Select Grafting Shears → click a plant with excess leaves → click a damaged plant with missing leaves
- The donor plant loses 1 leaf (and its associated energy generation)
- The recipient plant gains 1 leaf (restoring energy capacity)
- The transplanted leaf keeps the donor's durability (a Thick Bark leaf grafted onto a regular plant retains its toughness)

This is the manual equivalent of Healing Cloud — the player literally gives one plant's structure to another. The "automation curve" moment: "I've been grafting leaves for four rounds... I wish there was a gene for this." Then they discover Cloud + Healing does it automatically.

## 6.5 DORIS INTERACTION — Not Just A Hunger Bar

Doris isn't a passive meter to fill. She's a creature with personality and feedback.

### Early Game: Manual Feeding Dance

Round 1-2, the player hand-delivers foraged food to Doris. Walking to her, selecting a food item, feeding her — each step costs ticks. Doris has preferences:

- **Loves**: Sweet food (berries, honeycomb). +150% nutrition effectiveness.
- **Tolerates**: Neutral food (roots, mushrooms). Normal nutrition.
- **Dislikes**: Insects (grubs, beetles). 50% nutrition effectiveness.

The player learns to forage what Doris prefers. This transfers directly to genes: Fruit + Nutritious produces food Doris "tolerates." Later, discovering how to make fruit Doris "loves" (specific payload combos? rare seed strains?) becomes a mid-game optimization puzzle.

### Mid Game: Automated Feeding

Once food plants are producing reliably, the player drops fruit near Doris during Planning (1 tick) and she eats automatically during Growth. The manual feeding dance reduces to inventory management — harvest the right fruit, stockpile it near Doris.

### Doris Mood (Hidden Depth)

Doris's mood affects her behavior:

| Mood | Trigger | Effect |
|---|---|---|
| **Happy** | Fed high-quality food consistently | Occasionally drops a gene seed (reward!) |
| **Neutral** | Adequately fed | Normal behavior |
| **Grumpy** | Fed low-quality or same food repeatedly | Moves around the farm, tramples 1 plant |
| **Starving** | Not fed for too long | Eats your plants directly (already implemented) |
| **Ecstatic** | Fed a "perfect meal" (rare combo) | Drops a T3 gene + temporary farm-wide growth boost |

Doris mood is the hidden progression system. Players who invest in food QUALITY (not just quantity) get gene rewards. This creates a gentle push toward gene optimization: better genes → better food → better Doris mood → rarer gene drops → even better genes. A virtuous cycle.

---

# 7. MULTI-SOLUTION DESIGN — NO TWO RUNS ALIKE

## The Problem: Solvability Breeds Sameness

If there's one "best" gene build for each problem, every run converges to the same strategy. The game needs MULTIPLE viable paths so experienced players still face interesting choices.

## 7.1 RANDOMIZED GENE AVAILABILITY

**Not all genes are available every run.** At game start, the player receives a random subset of genes from the full pool. Each run might offer:

- Round 1 start: 8-12 random genes from T1 pool
- Round rewards: 2-4 random genes per round from available tier
- Exploration finds: Random genes from any tier (weighted by discovery type)
- Doris drops: Mood-dependent, any tier

**What this means:** Run A might offer Poison + Cloud early, making area denial the obvious defense. Run B might offer Freeze + Projectile, making cryo turrets the path. Run C might offer only Nutritious + Fear, forcing a pacifist strategy (lure away instead of kill). The player adapts to what they're given rather than executing a memorized build.

**Gene Draft (optional mechanic):** Between rounds, instead of random drops, the player picks 1 gene from a choice of 3. Slay the Spire style. More agency, but still constrained by what the game offers.

## 7.2 MULTIPLE SOLUTIONS TO EVERY PROBLEM

Every major problem in the game should have at least 3 viable approaches:

### Problem: "Pests Are Eating My Food Plants"

| Solution | Approach | Genes Needed | Difficulty |
|---|---|---|---|
| **Kill them** | Combat plants (Projectile/Cloud + Poison) | Projectile, Poison | Straightforward |
| **Scare them** | Fear plants (Aura + Fear) | Aura, Fear | Cheaper but pests come back |
| **Trap them** | Kill corridor (Trap + Poison around perimeter) | Trap, Poison | Placement puzzle |
| **Slow + outlast** | Bog field (Cloud + Slow) + Thick Bark food plants | Cloud, Slow, Thick Bark | Defensive, needs patience |
| **Redirect them** | Bait station (Aura + Nutritious) lures pests to decoy area | Aura, Nutritious | Clever, uses "wasted" gene combo |
| **Recruit them** | Dominate small pests to fight larger ones | Cloud/Aura + Dominate | High-tier, risky |
| **Manual** | Player throws rocks, places bait traps, stays vigilant | None (just tools) | High tick cost, viable Round 1-2 |
| **Thorned defense** | Thorned Leaves on food plants — pests take damage when eating | Thorned Leaves passive | Passive-only defense, no active needed |

Eight approaches to one problem. The leaf-vitality model adds Thorned Leaves as a purely passive defense strategy that wasn't possible with abstract HP. The player's available genes, map layout, and personal preference determine which they use. No walkthrough can say "just build Projectile+Poison."

### Problem: "Doris Is Starving"

| Solution | Approach | Requirements |
|---|---|---|
| **Mass farming** | Many cheap food plants (Efficiency + Fruit + Nutritious) | Farm tiles + Efficiency gene |
| **Quality food** | Fewer plants with Overcharge + Nutritious (each fruit worth more) | Overcharge gene |
| **Forage rush** | Sprint to rich foraging spots, hand-deliver | Map knowledge + tick budget |
| **Mushroom cave** | Discover and repeatedly harvest mushroom grotto | Exploration investment |
| **Doris bait** | Aura + Nutritious plant lures animals near Doris → she eats them when starving | Aura + Nutritious + knowledge of Doris eating behavior |
| **Befriend animals** | Attract friendly animals that drop food items (bees → honey) | Foraged flowers + patience |

### Problem: "I Have No Combat Genes"

| Solution | Approach |
|---|---|
| **Fear-based defense** | Fear aura or manual scarecrow placement |
| **Trap gauntlet** | Even Trap alone (no payload) roots creatures for 3 ticks. Enough to delay. |
| **Manual combat** | Player throws rocks, uses crafted smoke bundles |
| **Sacrificial plants** | Cheap decoy plants in pest path. Pests eat those leaves, not your farm's. |
| **Fortification** | Thick Bark + Iron Roots on food plants. Their leaves are too tough to eat quickly. |
| **Redirect pests** | Nutritious bait plant far from farm draws pests away |
| **Thorned sacrifice** | Thorned Leaves on sacrificial plants. Pests eat the leaves and take damage. The plant dies, but so do the pests. |
| **Grafting triage** | Manually graft leaves from healthy plants to damaged ones mid-wave |

## 7.3 SEED VARIETY — Different Starting Templates

Not all seeds are identical blanks. Different seed species have different strand lengths, passive slot counts, and energy profiles:

| Seed Species | Strand Length | Passive Slots | Energy Profile | Flavor |
|---|---|---|---|---|
| **Cornstalk** | 5 | 4 | High regen, low storage | Steady food producer |
| **Thornbrush** | 3 | 3 | Low regen, high storage | Fast-cycling combat |
| **Mossbell** | 7 | 5 | Med regen, med storage | Versatile, slow cycle |
| **Sunbloom** | 4 | 6 | Very high regen, low storage | Passive-focused support |
| **Nightshade** | 6 | 2 | Low regen, very high storage | Burst damage specialist |

The player doesn't just choose genes — they choose WHICH SEED to put genes into. A 3-slot Thornbrush with `[Projectile][Poison]` cycles faster than a 7-slot Mossbell with the same genes buried in a longer strand. But the Mossbell has room for modifiers.

**Leaf vitality note:** Different seed species also have different maximum leaf counts and leaf durability baselines. A Thornbrush has few leaves but they're naturally tough (baseline durability 1.5). A Sunbloom has many leaves but they're fragile (durability 0.8). This means the same Thick Bark passive has different defensive value depending on the species — multiplicative with the base durability.

Seed species are found through exploration, round rewards, and Doris drops. Different runs offer different seeds = different build constraints.

## 7.4 MAP VARIATION

Each run generates a different map layout:

- **Farm tile arrangement**: Which tiles are fertile (plantable), rocky, watery
- **Forageable placement**: Where berries, mushrooms, and rare nodes spawn
- **Pest approach paths**: Where waves enter from — affects which plants need to be combat vs food
- **Doris location**: Where her stump is relative to fertile tiles — affects feeding logistics
- **Terrain features**: Rivers (block movement, need bridges), elevation (can't plant, good for scouting), caves (foraging, exploration)

Map variation means optimal plant PLACEMENT changes every run. The same gene build might need different farm layouts to work.

## 7.5 PEST COMPOSITION VARIETY

Waves are composed differently each run:

- **Elemental resistances**: Some pests resist poison (need fire or freeze). Some resist fire (need poison or slow). Player must read the wave and adapt.
- **Size categories**: Small (insects) → Charm/Dominate works. Medium (rabbits, rats) → physical attacks work. Large (boars, bears) → immune to mind control, need raw damage or traps.
- **Behavior types**: Swarm (many weak, benefits from area effects). Tank (one big, benefits from single-target focus). Sneaky (avoids plants, goes for inventory — manual intervention required).
- **Eat speed**: Different pest types strip leaves at different rates. Swarm insects eat slowly but attack many plants simultaneously. A single boar rips through one plant fast. The player learns to read which pests are dangerous to which plants.

The player can't always rely on the same defense. "Projectile + Poison works on everything" fails when poison-resistant beetles arrive. Adaptation = replayability.

---

# 8. GENE INTERACTION DEEP DIVE — WHERE THE FUN LIVES

*This section catalogs the most interesting emergent interactions. These aren't designed — they emerge from the rules. The game's job is to make them DISCOVERABLE.*

## 8.1 STRAND ORDER PUZZLES

These only exist because of the buffer model. Same genes, different strand, different outcome.

### The Efficiency Debate

`[Efficiency][Cloud][Poison][Fruit][Nutritious]` — Cheap cloud, full-price fruit.
`[Cloud][Poison][Efficiency][Fruit][Nutritious]` — Full-price cloud, cheap fruit.
`[Efficiency][Cloud][Poison][Efficiency][Fruit][Nutritious]` — Cheap cloud AND cheap fruit, but 6 slots = slow cycle.

Which is correct? Depends on the situation. Energy-starved plant? Efficiency on the expensive Active. Slow cycle? Drop the second Efficiency for speed. Neither answer is universal.

### The Poison Apple Trick

`[Efficiency][Fruit][Nutritious][Poison]` — Fruit fires cheap, attaches Nutritious AND Poison. One fruit, two payloads: it feeds AND poisons. Feed to pests = bait. Feed to Doris = BAD (but she gets nutrition AND poison).

The player discovers this by accident. "Wait, I put Poison after Fruit and now my food is poisoned?" This is the "aha" moment that makes the buffer model exciting. The next question: "Can I make a SECOND fruit in the same strand that ISN'T poisoned?" Yes:

`[Efficiency][Fruit][Nutritious][Poison][Fruit][Nutritious]` — First fruit is cheap + nutritious + poisonous (bait). Second fruit is unmodified + nutritious only (safe food). One plant, two products, different functions. 6-slot strand but dual-purpose.

### The Wrap Trick Discovery

`[Cloud][Poison][Slow][Overcharge]` — 4 slots. First cycle: Cloud fires unmodified, delivers Poison + Slow. Second cycle: Overcharge wraps → Cloud fires OVERCHARGED (+40% to everything: bigger radius, longer duration, more damage). The plant gets STRONGER on its second cycle.

Advanced version: `[Cloud][Poison][Overcharge][Multicast]` — First cycle: plain poison cloud. Second cycle: Overcharge + Multicast wrap → DOUBLE OVERCHARGED poison cloud. Devastating. But 4-slot cycle + recharge = fires every ~7 ticks. Worth the wait?

### Empty Slot Speedrun

The player realizes an empty strand slot still takes 1 tick. So filling every slot is important for efficiency. But what if you WANT a faster cycle?

`[Projectile][Poison]` — 2-slot strand. Fires every 2 ticks + recharge. Extremely fast turret. Weak (no modifiers) but the fire rate compensates. "Machine gun vs sniper" — both from the same Projectile gene.

## 8.2 MULTI-PLANT COMBOS

### The Freeze Lock (Two Plants)
```
Plant A: [Trap][Freeze]                    (on pest path)
Plant B: [Cloud][Freeze]                   (adjacent to A)
```
Creature triggers Plant A trap: 2 freeze stacks + 3-tick root. While rooted, Plant B's frost cloud hits: +1 stack/tick. After 1 tick: 3 stacks → FROZEN. Total: 3 ticks root + 3 ticks frozen = 6 ticks immobile.

Add Plant C behind them: `[Overcharge][Projectile][Poison]`. Fires overcharged poison dart at the frozen target. 6 ticks of free damage on a helpless creature.

### The Chain Reactor (Two Plants)
```
Plant A: [Reactive Burst][Explosive]       (near Plant B)
Plant B: [Reactive Burst][Explosive]       (near Plant A)
```
Plant A takes leaf damage → Reactive Burst fires → explosion strips a leaf from Plant B → Plant B's Reactive Burst fires → explosion strips a leaf from Plant A → triggers again (if off cooldown). CHAIN REACTION. Each chain link costs one leaf from each plant. With 6 leaves each, that's up to 6 chain exchanges before both plants are leafless and die. Against a large swarm: every link deals AoE to all nearby pests. Worth the sacrifice.

**Leaf model makes this countable:** The player looks at both plants and thinks: "6 leaves each = 6 chains = 6 AoE blasts hitting everything in range. That's enough to wipe a 12-pest wave." With HP, it was abstract. With leaves, you can COUNT your remaining firepower.

**Recovery variant:** Give Plant A the Regrowth passive. Each explosion strips a leaf, but Regrowth slowly regrows them. The chain reaction is asymmetric — Plant A outlives Plant B because it regenerates. After Plant B dies, Plant A continues as a weakened but living combatant. Layered strategy.

### The Infinite Orchard (Self-Replicating)
```
Plant with: [Multicast][Seedpod] + Spreading Roots passive
```
Seeds inherit Spreading Roots + Seedpod + Multicast. Each offspring produces more seeds. Map fills with wild plants over several rounds. Most inherit partial gene sets. After 10 rounds, one plant becomes a grove. Chaotic, beautiful, emergent. Energy gating prevents instant explosion — young plants can't afford Multicast+Seedpod until fully grown.

### The Healing Symbiosis (Two Plants)
```
Plant A: [Cloud][Healing]                  (leaf regrowth mist)
Plant B: [Reactive Burst][Explosive]       (explodes when a leaf is consumed)
```
Plant B takes pest damage → leaf consumed → Reactive Burst fires → explosion strips another of Plant B's own leaves → another burst fires (if off cooldown). Plant B is eating itself alive with explosions. But Plant A's Healing Cloud is regrowing Plant B's leaves between detonations.

The player watches a visible cycle: Plant B loses a leaf → explodes → loses another leaf from self-damage → a fresh leaf sprouts from healing mist → gets eaten by the next pest → explodes again. The plant is a constantly-regenerating bomb. Without the healer, Plant B dies in ~12 ticks. With the healer, it lasts 40+ ticks — enough to clear most waves.

Balance note: Healing on plants at 50% regrowth speed. Plant B still gradually loses ground under sustained pressure. Lasts much longer, not forever. The player can add Regrowth to Plant B as well for even more sustainability — but that's a passive slot not spent on Energy Roots or Swift Growth. Trade-offs.

### The Funnel of Death (Three Plants)
```
Plant A: [Aura][Nutritious]               (bait station — persistent lure)
Plant B: [Cloud][Slow][Poison]             (bog + poison — periodic)
Plant C: [Trigger:Proximity][Projectile][Freeze]  (cryo turret — only when enemies near)
```
Pests smell food at Plant A → path toward it → enter Plant B's cloud → slowed + poisoned → Plant C fires frost bolts at anything in range → freeze stacks accumulate → FROZEN in poison cloud.

Three plants, one kill zone. The player discovers this by noticing: "Pests always walk toward my Nutritious Aura... what if I put bad things between them and it?"

### The Diplomat's Garden (Zero Combat)
```
Plant A: [Fruit][Nutritious][Charm]        (feed + charm approaching pests)
Plant B: [Aura][Fear]                      (scarecrow for charmed creatures that wear off)
Plant C: [Fruit][Nutritious]               (safe food for Doris)
```
No damage dealt. Pests eat charmed fruit → become passive for 4 ticks → wander into Fear aura → flee off-map. Non-lethal pest management. Works on Small and Medium creatures. Fails spectacularly against Large (immune to Charm). The player who discovers this feels brilliant — until the first boar wave.

### The Thorn Garden (Passive Defense Only)
```
Plant A: Thick Bark ×2 + Thorned Leaves ×2 + Iron Roots (tough food plant)
Plant B: Thick Bark ×2 + Thorned Leaves ×2 + Iron Roots (tough food plant)
Plant C: [Fruit][Nutritious] (standard food, protected by Plant A/B)
```
No Active combat genes at all. Plant A and B are sacrificial tanks with incredibly tough leaves. Pests spend many ticks gnawing on each leaf, taking Thorned Leaves damage with every bite. By the time they strip a leaf, they've taken enough damage to die. The plants slowly lose leaves over the round but Regrowth keeps them going. Plant C sits safely behind them producing food.

This build is only possible because leaves ARE health. With abstract HP, "Thorned Leaves" would just be damage reflection — boring. With structural leaves, each bite is a visible exchange: the pest gnaws, the thorn stabs, the leaf eventually breaks, the pest is bleeding. Natural, readable, satisfying.

## 8.3 COUNTER-INTUITIVE DISCOVERIES

Things the player stumbles into that feel like "breaking" the game:

### "Wasted" Genes That Aren't

**Nutritious on Cloud** — the Spec Sheet says "food scent only." Seems useless. But food scent LURES creatures. Cloud + Nutritious + Poison = "come here for food" + "die of poison." The "wasted" gene is actually bait.

**Fear on Fruit** — why would you make food that scares? Because when an approaching pest eats it, it panics and runs... into your trap line. Directional pest herding through fruit placement.

**Healing on Reactive Burst** — explode and regrow? Yes. The healing burst regrows leaves on the plant itself AND neighboring plants. A Reactive Burst + Healing plant regrows its own leaves whenever it detonates — each leaf eaten triggers a burst that regrows a leaf. If the regrowth rate equals the consumption rate, the plant sustains itself indefinitely. Tanky, weird, and effective.

### Self-Destructive Builds That Work

**The Hellfield**: `[Aura][Explosive]` with Thick Bark ×2 + Regrowth. Periodic AoE that strips nearby creatures AND the plant's own leaves. But Thick Bark makes the self-damage slower (the explosion needs to overcome leaf durability), and Regrowth replaces lost leaves between detonations. The player watches the Hellfield plant constantly cycling: losing a leaf from its own explosion, sprouting a new one, losing it again. The editor warns with a leaf balance indicator: "Self-damage: ~1 leaf/4 ticks. Regrowth: 1 leaf/5 ticks. Net: -1 leaf per 20 ticks. Lifespan: ~120 ticks." The player builds it anyway. It works. Barely.

**Volatile Turret**: `[Volatile][Projectile][Poison]` — +30% damage with 15% self-leaf-loss chance. Over many cycles, the plant strips its own leaves. But each lost leaf means less energy, which means fewer shots, which means fewer self-damage rolls... the turret naturally throttles itself as it degrades. If pests die before the turret runs out of leaves, that's a win. A race condition the player can optimize by adding Thick Bark (slows self-damage) or Regrowth (replaces lost leaves). The oscillating power curve — strong when healthy, weak when damaged, recovering when healing — creates dynamic moment-to-moment gameplay.

**The Pruning Cannon** (post-POC): `[Pruning][Overcharge][Projectile][Poison]` — Pruning voluntarily sacrifices a leaf for a massive energy burst. The turret fires an absurdly powerful shot, drops to fewer leaves, generates less energy next cycle, but has enough stored from the burst to fire again. Each shot costs a leaf. 6 leaves = 6 mega-shots before the plant is exhausted. If Regrowth is active, it regrows leaves between volleys — a self-reloading cannon.

### The "Useless" Gene That Completes A Build

**Trigger:Timer** seems bad. "Only fire every 3rd cycle? Why?" Because when it fires, it's FREE (pre-charged energy). So: `[Trigger:Timer][Overcharge][Multicast][Projectile][Poison]` fires a MASSIVE double-overcharged volley every 3rd cycle at zero energy cost. The other 2 cycles, the plant banks energy for its non-Timer actives. Timer turns an unaffordable build into a periodic nuke.

## 8.4 NEW GENES ENABLED BY LEAF VITALITY

These genes emerge naturally from the structural leaf model and wouldn't exist with abstract HP.

### Thorned Leaves (Passive)
When a pest consumes one of this plant's leaves, the pest takes damage. Each stack increases the thorn damage. Stacks with Poison: the eaten leaf was poisonous. Turns leaf loss into a defensive mechanic — a food plant with Thorned Leaves is a poisoned apple tree. Pests eat the leaves and get hurt.

### Pruning (Active)
When executed, the plant voluntarily sacrifices its oldest leaf to gain a massive energy burst. Converts structure into fuel. A 6-leaf plant with Pruning fires a huge Overcharged Projectile, dropping to 5 leaves. Next cycle, less energy, so the combo might not fire. With Regrowth, the leaf grows back and the cycle repeats — a self-reloading weapon.

### Deciduous (Passive — Post-POC)
Every N cycles, the plant drops ALL leaves and enters dormancy for 2 ticks. Then regrows all leaves at once. During dormancy: no energy, no Active execution, but INVULNERABLE to leaf-eating (nothing to eat). Timing the shedding to coincide with pest waves is the skill expression. Combine with Reactive Burst: shedding all leaves at once triggers a burst for each leaf — a massive simultaneous explosion followed by safe dormancy.

### Spore Burst (Active — Trigger: Leaf Consumed)
A new trigger type: fires specifically when a leaf is destroyed. Different from Reactive Burst (which fires on any structural damage). Each dying leaf releases a cloud of spores applying payloads to nearby creatures. A food plant with Spore Burst + Poison becomes a minefield — every leaf a pest eats releases poison spores. The MORE the pest eats, the more poison it inhales.

### Leaf Armor (Modifier — Post-POC)
Scales the Active gene's effect power by current leaf percentage. More leaves = more damage/radius/effect. A turret at full leaf count hits hard. A stripped turret hits weakly. Creates urgency to protect combat plants and rewards the Healing Symbiosis combo.

### Mycorrhizal Network (Passive — Post-POC)
Connects the plant to adjacent mushroom tiles. While connected, generates bonus energy per mushroom neighbor — even with zero leaves. Opens an alternative survival archetype: a leafless plant on a mushroom network is diminished but functional. The player cultivates mushrooms around defensive positions as living energy batteries.

### Carnivorous Trap (Active — Post-POC)
Instead of damaging pests, CAPTURES them. A captured pest is consumed over several ticks, generating energy directly. A leafless carnivorous plant that MUST catch pests to survive. No pests = starves. During waves = self-sustaining. An anti-fragile defense that gets STRONGER under pressure.

---

# 9. TIER PROGRESSION & STRAND GROWTH

## Seed Tiers

| Tier | Strand Slots | Passive Slots | T3 Genes Allowed? | When Available |
|---|---|---|---|---|
| **T1 "Seedling"** | 3 | 3 | No | Round 1 (starting seeds) |
| **T2 "Mature"** | 5 | 4 | Yes (1 max) | Round 3-4 (rewards, exploration) |
| **T3 "Ancient"** | 8 | 6 | Yes (unlimited) | Round 6+ (rare finds, Doris drops) |

### What Tiers Mean For Builds

**T1 (3 strand + 3 passive):** Simple builds. `[Fruit][Nutritious]` + Swift Growth is about as complex as it gets. Room for ONE active + ONE payload + ONE modifier max. Forces clean, focused builds. With only 3 passive slots, the player must choose: offense (Energy Roots), speed (Swift Growth), or defense (Thick Bark). Can't have all three. Leaf vitality makes this choice tangible — a T1 plant without Thick Bark is visibly vulnerable.

**T2 (5 strand + 4 passive):** The "real game" starts. Room for dual-purpose strands: `[Efficiency][Cloud][Poison][Fruit][Nutritious]`. Can support one combat output and one food output. Modifiers become meaningful (Efficiency savings compound with longer strands). 4 passive slots allow Thick Bark + Regrowth combos — the first self-sustaining defensive plants.

**T3 (8 strand + 6 passive):** Complex multi-function plants. The wrap trick becomes relevant. Enough slots for two modified actives with full payload complement. Energy budget is the primary constraint, not slot count. Deep Reserves + Energy Roots stacking becomes important to fuel long strands. 6 passive slots allow full defensive suites (Thick Bark ×2 + Regrowth + Iron Roots) alongside utility — the Hellfield build becomes possible.

### Gene Tier Limits

Genes themselves have tiers (T1, T2, T3). High-tier genes are rarer and more powerful:

| Gene Tier | Available In | Power Level |
|---|---|---|
| T1 | All seeds | Basics: Fruit, Efficiency, Poison, Swift Growth, Thick Bark, Thorned Leaves |
| T2 | T2+ seeds | Advanced: Multicast, Overcharge, Freeze, Trap, Regrowth, Pruning |
| T3 | T3 seeds only | Powerful: Echo, Explosive, Dominate, Reactive Burst, Deciduous, Spore Burst |

A T1 seed physically can't USE a T3 gene — the slot rejects it. This prevents early-game power spikes while giving late-game builds access to the full toolkit.

---

# 10. OPEN PROBLEMS & SOLUTIONS

## "Strand Order Is Confusing"

The buffer model's power is also its accessibility problem. A new player who puts `[Fruit][Efficiency][Nutritious]` expects Efficiency to make fruit cheaper. But in buffer model, Efficiency is AFTER the Active — it buffers, then wraps to Fruit on NEXT cycle. First cycle: full-price fruit.

**Mitigation**: The editor shows real-time groupings. As the player drags genes, colored brackets show which modifiers apply to which Active. The Spec Sheet shows per-cycle breakdown: "Cycle 1: Fruit (full price) → Nutritious. Cycle 2+: Fruit (−25% cost) → Nutritious." Visual feedback prevents confusion.

**POC approach**: Ship with slot model first (explicit modifier assignment). Buffer model arrives as an upgrade when players already understand gene relationships.

## "Active With No Payload"

Allowed but warned. Behavior:
- **Fruit**: bland fruit (1 nutrition). Functional but barely worth the energy.
- **Cloud**: harmless mist. Energy wasted. Editor warns.
- **Projectile**: plain thorn, 5 damage. Weak but functional.
- **Aura**: faint glow. No effect. Energy wasted. Editor warns.
- **Trap**: pure root. 3-tick immobilize, no additional effect. Useful as CC.
- **Reactive Burst**: 15 base AoE damage. Useful as-is. Triggers on leaf consumed.

## "Nutritious + Poison Fruit" (The Poison Apple)

Embraced, not prevented. Inventory visually distinguishes: skull overlay on poisoned fruit, glow on charmed, sparkle on explosive. The skill check is inventory management — don't feed Doris the wrong fruit. This is a feature: it teaches the player to READ their fruit, which matters when 5 different plants produce 5 different fruit types.

## "Mind Control Feels OP"

Tiered by creature size:
- **Small** (insects, mice): Full effect.
- **Medium** (rabbits, rats): Half duration.
- **Large** (boars, bears): Immune to Charm/Dominate. Slow and Freeze still work.
- **Boss**: Fully immune to all mind effects. Must be fought directly.

## "Energy Starvation on Complex Strands"

Intentional. Complex strands fail gracefully: the executor processes slots left-to-right, fires what it can afford, skips the rest. The player sees which slots fired (visual feedback) and which were skipped (dimmed in Spec Sheet). Solution: add Energy Roots passives, use Efficiency modifiers, or shorten the strand. The system teaches itself.

**Leaf vitality amplifies this:** A plant that's lost leaves generates less energy AND can't run expensive strands. The player watches their turret "power down" as pests eat its leaves — it goes from firing every cycle to skipping every other cycle to going silent. This visible degradation is more communicative than a hidden HP bar dropping. The fix is also visible: a Healing Cloud from a neighbor plant regrows a leaf, and the turret comes back online.

## "When Does Gene Editing Happen?"

Planning phase only. `RunManager.CurrentState == RunState.Planning`. During Growth & Threat, the Seed Editor is read-only. All bets placed before the day starts. This is non-negotiable — it's what gives gene decisions weight.

## "Is Leaf Vitality Too Complicated?"

No — it's actually LESS complicated than HP for the player. With HP, the player needs to learn: what HP is, what the HP bar means, what defense multiplier does, how damage is calculated, when the plant dies. With leaves: "pests eat leaves. fewer leaves = less energy. no leaves = plant dies." The entire system is visually self-explanatory. A new player in Round 1 watches a slug eat a leaf off their plant and immediately understands the threat. No tutorial needed.

For advanced players, the leaf model is richer: durability, regrowth, thorned leaves, pruning, self-damage cycling, alternative energy sources. The floor is lower AND the ceiling is higher. That's the design sweet spot.

---

# DESIGN PRINCIPLES (FINAL v6)

### 1. Low Floor, High Ceiling
Round 1: pick up berries, feed Doris, plant a simple seed, watch a slug eat a leaf. Round 10: manage a network of specialized plants with regenerating Hellfield bombers, Thorned sacrificial walls, Mycorrhizal combat plants on mushroom networks, and a happy Doris dropping T3 genes. Same game. Vastly different depth. A new player survives by foraging and simple Fruit+Nutritious plants. A veteran thrives by building freeze-lock kill corridors and self-replicating orchards.

### 2. Manual → Automated Progression
Every manual action has a gene equivalent. The player's journey IS discovering these automations: "I can stop picking slugs off my plants because my Trap gene does it now." The satisfaction isn't just efficiency — it's recognition. You solved the problem yourself first. Now the gene does it for you. Grafting leaves by hand becomes Healing Cloud. Throwing rocks becomes Projectile. Even watching your plants suffer teaches you which passives (Thick Bark, Regrowth) to prioritize.

### 3. Genes Are The Answer, Not The Question
The game never says "use the Poison gene here." It says "pests are eating your crops' leaves." The player discovers that Poison is ONE answer. Fear is another. Traps are another. Thorned Leaves are another. Manual rocks are another. The gene catalog is a toolbox, not a questionnaire.

### 4. Scarcity Drives Creativity
Random gene availability forces adaptation. "I don't have Poison this run, but I have Freeze and Slow. Can I build a non-lethal defense?" Yes. "I don't have any combat genes at all, but I have Thick Bark and Iron Roots. Can I just make my food plants too tough to eat?" Yes — and Thorned Leaves turns that defense into offense. The player who adapts to what they're given develops deeper system understanding than one who memorizes "always pick Poison."

### 5. Strand Order Is The Puzzle
In the DNA buffer model, the same genes in different order produce different plants. This is the game's unique mechanic. The editor must make parsing VISIBLE (brackets, arrows, preview) so the puzzle is legible, not opaque. When a player rearranges three genes and sees the Spec Sheet change, that's the moment they're hooked.

### 6. The Farm Is The Build
Individual plants are simple (3-8 gene strand). The FARM is the build: where plants are placed relative to each other, which ones protect which, how creature pathing interacts with layout. A wall of Fear Aura plants with a Trap+Poison kill corridor through the gap. A Root Network hub buffing four expensive combat plants. A Healing Cloud plant keeping a Chain Reactor pair alive. A decoy garden of Nutritious Aura plants drawing pests away from the real farm. Mastery lives in spatial design, not individual gene optimization.

### 7. Every Run Tells A Story
Random genes + random map + random pests = unique problems. The player's story emerges from adaptation: "This run I had no combat genes until Round 4, so I stacked Thick Bark and Thorned Leaves on my food plants and survived by letting pests eat themselves to death on my armored lettuce until I found a Projectile gene in an abandoned garden." No two runs identical. No optimal strategy. Just player skill applied to novel constraints.

### 8. The Plant Is The Health Bar
No abstract HP. No numbers going down. The plant's physical body IS the health readout. A thriving plant has full leaves and generates abundant energy. A damaged plant has visible gaps, produces less, fires fewer strand slots. A dying plant is a bare stem with no leaves, withering for 2-3 ticks before collapsing. The player reads plant health the way a real gardener does — by looking at it. This is the cozy farming fantasy made mechanically real.

---

*v6 changes from v5: Replaced abstract Plant HP system with leaf-based structural vitality throughout. Plants now take damage through leaf consumption rather than HP reduction. Added leaf durability (Thick Bark), leaf regrowth (Healing, Regrowth passive), withering grace period, self-damage via leaf destruction. Added new genes: Thorned Leaves, Pruning, Deciduous, Spore Burst, Leaf Armor, Mycorrhizal Network, Carnivorous Trap. Added Grafting manual tool action. Updated Reactive Burst trigger to fire on leaf consumed. Added leaf balance indicator to Spec Sheet. Added alternative energy source framework (post-POC). Updated all combo descriptions (Chain Reactor, Healing Symbiosis, Hellfield, Volatile) to use leaf mechanics. Added Section 8.4 (new leaf-vitality genes). Updated Design Principles with Principle 8: "The Plant Is The Health Bar." Reorganized passive gene listing into dedicated table. Added seed species baseline durability concept.*
