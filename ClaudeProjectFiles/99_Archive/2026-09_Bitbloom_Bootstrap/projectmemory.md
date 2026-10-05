# projectmemory — Bitbloom

## Current state

- **2026-09-20:** Project bible v1 written (`Project_Bible.md`). No Unity project or code exists yet.
- **Next action:** Milan does *Unity project setup* (bible, steps 1–7), copies the three files in, commits, and sends the first prompt (bible · Files and first prompt). First code block: **Sat AM — Foundation**.

## Decision log

| Date | Decision | Where |
| --- | --- | --- |
| 2026-09-20 | Name: **Bitbloom — Survival of the Fernest** | Start here |
| 2026-09-20 | Fresh Unity project; no code reused from Abracodabra | Start here |
| 2026-09-20 | Side view, realtime autobattler; no avatar, no WeGo | Core loop |
| 2026-09-20 | Player is a plant lineage; travel by seed dispersal, world runs left to right with caves below; finish zone wins | Core loop · Run structure |
| 2026-09-20 | Genes as Noita-style wand strand; four kinds (Emit, Shape, Chain, Trait) | Genes |
| 2026-09-20 | Seed inherits parent + one mutation; Vigor is the only currency; season clock limits a run | Seed editor · Core loop |
| 2026-09-20 | Living ecosystem: wild plants on the same system; cross-pollination as gene source | Living ecosystem |
| 2026-09-20 | Chemistry transforms, never deletes; soil survival soak test ≥ 70 % | Chemistry |
| 2026-09-20 | Unity 6.3 LTS, used thinly; Godot rejected | Engine |
| 2026-09-20 | Pure C# `Sim.Core` with Burst wrappers; dotnet-testable | Engine · Setup |
| 2026-09-20 | 8-byte packed cells, active window + chunk store, dirty rects, stateless hash RNG | Simulation foundation (v2) |
| 2026-09-20 | Area 480 × 270, integer scaling, no Pixel Perfect Camera | Display math |
| 2026-09-20 | Day/night designed in, built after the PoC | Day and night |
| 2026-09-20 | Two lanes: Claude Code for code, Cowork/chat for design; folder files are the only source of truth; no extractor | Start here · CLAUDE.md |
| 2026-09-20 | CoplayDev Unity MCP bridge, pinned v10.0.0 | Unity project setup · step 7 |

## Open threads

- PoC verification list and open design questions: bible · Open questions.

## Learnings carried from Abracodabra

- GameObject-per-cell and tilemaps don't scale to pixel worlds.
- Static singletons and services produced run-teardown bugs; hence no mutable statics.
- Invariants that aren't enforced in code get broken (the Planning-tick exploit); hence tests with every Core rule.
- Edit speed, not model depth, decides the gene-editor design.
- The plant is the health bar: structural loss is more readable than HP.
- Milan almost didn't know what to do in his own prototype: UI must teach the game.

## Code facts

(None yet. Add verified facts about the live code here.)
