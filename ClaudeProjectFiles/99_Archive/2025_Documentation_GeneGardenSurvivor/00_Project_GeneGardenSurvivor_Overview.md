# 00_Project_GeneGardenSurvivor_Overview.md

**Project:** Gene Garden Survivor
**Synthesized:** 2025-05-31

## 🧬 Vision
Strategic roguelike: engineer plant DNA against predators & environmental crises. Systems depth of *Noita* meets *Stardew Valley*'s vibe, with auto-battler tactics. Procedural plant growth from node-graphs, interactive animal AI, dynamic environment. Player engineers genes, discovers sequences in a roguelike loop.

## 🎮 Core Gameplay Loop
Player-driven three-phase cycle, managed by `RunManager`.

| Phase               | Workflow                                                        | Key Systems                                       | Time Scale |
| :------------------ | :-------------------------------------------------------------- | :------------------------------------------------ | :--------- |
| **Planning (⏸)**   | Modify genetics, design garden, analyze threats, manage resources. | Node DNA editor, planting grid UI.                | `0`        |
| **Growth & Threat (⚡)** | Plants grow (6x speed), threats spawn. Limited player panic tools. | `PlantGrowth`, `WaveManager` (threats), auto-combat. | `~6`       |
| **Recovery (📊)**   | Review performance, collect rewards, unlock research, heal.      | Analytics, meta-progression UI.                   | `0`        |

## 🌿 Key Pillars & Design
1.  **Living Ecosystem:** Dynamic animal AI, plant interactions.
2.  **Procedural Genetics:** Deep node-graph plant system, emergent behaviors.
3.  **Environmental Interaction:** Dual-grid tiles, tool modifications.
4.  **Genetic Discovery:** Roguelike progression, biome challenges, meta-progression.

**Principles:** Strategic depth; program plants, not armies; peaceful vibe with deep systems; accessible yet scalable; emergent synergies.

## 🧬 Genetic Programming: "Leaf = Life"
*   No Leaves → Plant Death.
*   Fewer Leaves → Lower Energy.
*   Leaf Damage: Permanent by default; regen requires specific genes.

### Core Gene Categories (Examples)
| Category       | Examples                                                              | Effects                       |
| :------------- | :-------------------------------------------------------------------- | :---------------------------- |
| 🛡️ **Defensive** | `Thorns`, `LeafArmor`, `CamouflageSpores`, `RootNetwork`, `BackupLeaves` | Reduce/redirect damage.       |
| ⚔️ **Offensive** | `PoisonSap`, `LeafExplosives`, `ProjectileSeed`, `LeafSacrifice`        | Harm/misdirect attackers.     |
| 🌍 **Ecosystem** | `PredatorLure`, `MushroomSpawn`, `WaterIrrigation`, `PollinatorFriend`  | Influence wildlife, terrain.  |
| 🔧 **Utility**   | `WaterRetention`, `LeafRegeneration`, `SeedScatter`, `Adaptation`       | Survival, resource management. |

**Synergies:** e.g., `Thorns` + `PoisonSap` → `VenomousSpikes`.

## 🎯 Player Agency & Roguelike Structure

### Starting Classes (Examples)
| Class                   | Starter Genes                               | Special Tool (Uses/Round) | Passive                     |
| :---------------------- | :------------------------------------------ | :------------------------ | :-------------------------- |
| 🛡️ Defensive Botanist   | `Thorns`, `LeafArmor`, `HealingRoot`        | Stone Wall (3)            | Auto-regen 1 leaf/2 rounds  |
| ⚔️ Aggressive Geneticist| `PoisonSap`, `ProjectileSeed`, `Explosives` | Gene Splicer (2)          | +50% dmg, +25% energy use   |
| 🤝 Symbiotic Ecologist  | `PredatorLure`, `MushroomSpawn`, `Friend`   | Nature Call (3)           | Ecosystem effects +50% dura |
| 🔧 Survival Specialist  | `WaterRetention`, `SeedScatter`, `Adapt`    | Emergency Supply (5)      | Plant on any tile           |

### Resources
*   **Seeds:** Limited starting (2-3), earn more.
*   **Gene Echoes:** Meta-currency for permanent unlocks.
*   **Emergency Tools:** Class-specific, limited uses per round.

### Roguelike Structure
*   **Biomes:** Grasslands (Tutorial) → Wetlands (Disease) → Desert (Drought) → Tundra (Freeze) → Toxic Wasteland (Acid). Each with unique challenges & enemies.
*   **Rounds:** ~7-10 per biome, escalating waves.
*   **Permadeath:** Meta-currency & unlocked genes persist.
*   **Risk/Reward:** Option to bank earnings or continue.
*   **Meta-Progression:** Gene Library expansion, research points.
*   **Victory Tiers:** Minimal, Standard, Perfect based on plant survival.

## 🚀 Success Vision
Player discovery of gene combos ("VenomousSpikes wrecks foxes!") drives engagement. Loop of creativity, mastery, progression.