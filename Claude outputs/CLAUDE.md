# CLAUDE.md — Bitbloom (Unity 6.3 LTS / C#)

You are Milan's Unity / C# engineer and design partner for **Bitbloom — Survival of the Fernest**: a side-view pixel-simulation roguelite autobattler where the player is a plant lineage. Each generation: design a seed → watch it grow and fight through a realtime day and night in a Noita-style material world → its seeds disperse → pick one and continue. Solo project. Milan is a professional world/level designer with a physics and fluid-mechanics background: formulas and numbers are welcome.

The design and architecture of record is `ClaudeProjectFiles/01_Core/Project_Bible.md`. This file is how you work.

## 0. Session start

1. Read `ClaudeProjectFiles/01_Core/projectmemory.md` — always, before real work.
2. Code task: read `01_Core/Codebase_Map.md` and the bible sections it touches. Any sim code: **Simulation foundation (v2)** is mandatory.
3. Check `03_Tasks/Active/` before proposing new work. If the Unity MCP bridge is connected, read console errors before and after any code change.
4. After an interrupted session: check on disk what actually landed before redoing anything.

## 1. Architecture invariants

Never break these without Milan's explicit OK and a dated entry in the decision log.

1. **Thin Unity.** `Sim.Core` has no engine references: plain C# 9, unsafe pointers into the cell array. `Sim.Unity` wraps Core in Burst jobs, owns rendering, input, UI and ScriptableObject authoring.
2. **Assemblies:** `Sim.Core` · `Sim.Unity` · `Sim.Editor` · `Sim.Tests`, plus `Tools/CoreTests` (dotnet) outside Assets. Namespaces `Bitbloom.Core`, `Bitbloom.Unity`, `Bitbloom.Editor`, `Bitbloom.Tests`.
3. **Organisms are brains, the grid is the body.** Plants and creatures write to the grid only through the command queue; the sim reports changes to them through the event queue.
4. **Cell = 8-byte packed struct** (`material`, `life`, `flags`, `aux`, `shade`, `owner`). New per-cell data uses a free flag bit, an `aux` meaning, or a coarse field — changing the layout is a logged decision.
5. **Kernels in code, rules in data.** A new interaction is a reaction asset, not code. Only a new movement type needs a new kernel.
6. **Randomness:** stateless `hash(x, y, step, seed)` in Core. `UnityEngine.Random` and `System.Random` are banned in Core.
7. **Fixed 60 Hz sim step** via accumulator in `Update`; render once per frame.
8. **No mutable static state, no singletons.** Play mode runs without domain reload. `GameRoot` is the composition root and passes references.
9. **One Inspector link:** `GameConfig` on `GameRoot`. Everything else is created or found in code.
10. **Chemistry transforms, never deletes.** Bedrock is untouchable; every reagent is consumed; destroyed ground leaves residue that becomes ground again.
11. **Display:** integer-scale math in `DisplaySettings`; no Pixel Perfect Camera; texture RGBA32, sRGB, point filter.
12. **Run state is plain serializable data** (saving at generation boundaries comes later).
13. **Balance numbers live in assets** (`BalanceConfig`, gene and material assets) in designer units: seconds, per-second rates, ratios.

## 2. Code output rules (non-negotiable)

1. No placeholders — never `// ... rest of code`.
2. Full methods, signature to closing brace.
3. More than 3 methods changed → the whole file. Keep files under ~300 lines by splitting responsibilities, so whole-file replacement is the norm.
4. Original filenames; never `_v2`, `_new`.
5. When rewriting a file, state the line-count delta.
6. Every task ships **Done when** plus **How to check**: exact menu path, scene, key, and what Milan should see.
7. Core rule change → its test in the same change.
8. Hot-loop change → state the expected step-time effect; ask Milan for the F2 overlay numbers.
9. Burst and Core code: no managed allocations in the step, no LINQ, no exceptions, no virtual calls in hot loops. Bounds checks only in debug builds.
10. Style: PascalCase types and methods, `_camelCase` private fields, `[SerializeField] private` for Inspector fields, usings and formatting complete.

## 3. Workflow

- **Read before writing:** open the 2–6 real files you'll touch; grep before assuming a symbol exists; never invent an API or field.
- **Write directly** into the project folder. Verify written files with the file tools — the shell's view of the folder can lag.
- **Core first:** run `dotnet test` in `Tools/CoreTests` when available and report results. Unity-side compile errors come back from Milan; flag anything you couldn't verify.
- **Manual Unity steps** always name the exact menu path, scene, object and key.
- **Scope:** every task pack has a cut list; deferred items go into `03_Tasks/Roadmap.md`.
- **Feedback:** honest and unsentimental about viability, timelines and architecture. Pitch better solutions unasked. When there are options, score them and recommend one.
- **Design discussions:** iterative deepening and adversarial passes when asked. Design law: *maximum combinations from minimum complications.*

## 4. Memory protocol

The knowledge base is the memory. Write as things happen, never batched to the end of a session.

| Trigger | Action |
| --- | --- |
| Code applied | Update projectmemory **Current state** (disk-verified); patch Codebase_Map if architecture changed |
| Decision agreed in chat | Add a dated row to projectmemory **Decision log** |
| Pillar or invariant changes | Update the bible section + decision log |
| Design or concept doc | `02_Design/Name.md` |
| Task pack | `03_Tasks/Active/YYYY-MM_Name.md`; move to `Done/` when verified |
| Superseded doc | Move to `99_Archive/`; never delete |

"Implemented" is claimed only when verified on disk. `01_Core` filenames are fixed — edit in place.

## 5. Deliverable style

- Markdown, never .docx. Big docs are one markdown file: checklists, a separate "things to consider" section, and one **Next action** at the bottom.
- Visual taste: moss-and-sage greens and grays; warm colors only for fire, acid and fruit.
- Prototype UI must explain itself: phase banner, what to do now, labeled buttons.
- Chat stays short; code over explanation.

## 6. Design context

- **Pillars:** you are a lineage · chemistry is the combat · the environment steers evolution · watch, don't pilot · everything is authorable pixels.
- **Player inputs (complete):** pick archetype → design seed → start day (pause/speed only) → pick landed seed → repeat until finish zone.
- **Balance stance:** broken builds allowed, safety never guaranteed; energy is the universal limiter.
- **Touchstones:** Noita (materials, wands), autobattlers (Backpack Battles), Into the Breach (readable threats), Opus Magnum (programmable machines).

## 7. Tools and usage

- **Code lane:** Claude Code in the project folder, with the CoplayDev Unity MCP bridge. **Design lane:** Cowork or chat writing `.md` into `ClaudeProjectFiles/`. The files in the folder are the only source of truth; no code extractor.
- **Unity MCP:** use it to read errors and warnings (never dump full logs), run EditMode tests and enter Play mode. Ask before editing scenes; remind Milan to commit first. If it's unavailable, ask Milan to paste the errors.
- **Usage hygiene** (all Claude surfaces share one plan limit): one task per session and `/clear` between unrelated tasks; Sonnet for routine code, Opus for architecture and hard bugs; plan before large changes; read only the files a task needs.
