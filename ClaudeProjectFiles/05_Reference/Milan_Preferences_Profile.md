# Milan: preferences profile

*A portable summary of how Milan works, what he loves in games and design, and what to avoid. Paste the short version at the top into a new chat or project; use the sections below when a task needs them. Compiled 2026-10-26 from his own statements across our work. Edit freely: it is his document.*

*(Copied into the Abracodabra KB on 2026-10-26 at Milan's request, verbatim. Living doc: edit in place. Abracodabra-specific rules derived from it live in `02_Design/Feedback_Log.md`, cross-project rules 28–31.)*

---

## The short version (paste into a new chat)

> I'm Milan, a world/level designer (Kingdom Come: Deliverance 2) with a mechanical-engineering, fluid-mechanics and hydraulics background, a past in music composition, and side work in marketing automation and data engineering. I build solo games in Unity 6 / C# with AI help. I'm partly red/green colour-blind: never rely on red vs green alone (use value, shape, outline, motion). I use a Czech keyboard (avoid Y/Z and Ctrl/Alt as game keys). How I like to work: give me options with one clear recommendation (I often just answer "recs"); use checklists plus a separate "Things to consider" section; give me direct counterarguments rather than validation; plain English, no invented jargon; short and scannable. In games I love Noita, Rain World, CK3, Battle Brothers, Grim Dawn, Kerbal Space Program and Stardew Valley; I value organic, simulation-driven, readable systems with elegant complexity, and I dislike generic templates, heavy UI and tedious complexity that only adds artificial difficulty.

---

## 1. Working with an AI (or any collaborator)

**Decisions**
- Options with **one clear recommendation**; he usually answers "recs" (take all recommendations).
- **Direct counterarguments over validation,** especially in hard conversations; intellectual rigour wanted.
- Planning output as **explicit checklists** plus a separate **"Things to consider"** section, not prose.

**Format**
- Plain English, no invented jargon; define a term once if it is needed.
- Short and scannable; more text only where a new idea needs explaining.
- When a document is mentioned, give it as something clickable he can open at once (a plain `file:///` link with any launch parameters), not a filename to look up.

**Prompts for coding agents**
- Every prompt pasted in full, ready to copy, with a header: **Model · Effort · Est. time · Est. usage**, then **Where** (repo, branch, model, effort) and **When** (parallel or not, after what).
- When prompts are referred to by short names, the name goes as a heading right above the prompt block.
- Long prompts also saved as a file; long work committed section by section so "continue" can resume after a usage limit.
- Usage efficiency matters more than speed: the weekly limit is the real constraint. Spare capacity before a weekly reset is welcome on research and design evaluations.

**Testing and feedback**
- Small, focused tests with **one clear question each**; big full tests make him forget what to test.
- Play sheets short, in play order, as **Do / Look for / Answer**, with ★ for the answers that matter; at most about eight items.
- He plays in short sittings and often late; when tired he wants step-by-step instructions in order.
- He gives feedback in rough notes; he likes them logged faithfully and turned into the next step.

---

## 2. Hard constraints (any project he makes or uses)

- **Partly red/green colour-blind:** state never told by red vs green alone; value, shape, outline, pattern and motion first; check in greyscale.
- **Czech keyboard:** avoid Y and Z for game keys; no Ctrl or Alt as game keys.
- **Languages:** Czech, English, Slovak (rusty German).

---

## 3. Games he loves, and why

| Game | What he loves | His complaint |
| --- | --- | --- |
| **Noita** | "Perfection": a simulated world where everything reacts; composing spells into combos; curiosity as the drive; voluntary risk (explore for gold and better wands: chill because chosen, tense because dangerous) | None; he treats Noita-level material chemistry as the expected baseline, not a highlight |
| **Rain World** | A living, indifferent ecosystem; creatures with their own lives | "Perfect but too hard and too time-consuming for adult life"; never finished |
| **Crusader Kings 3** | Generating stories for his character; creating things his own way; elaborate roleplay (e.g. a chivalric mercenary knight with custom culture and faith reform) | — |
| **Battle Brothers** | Mechanically excellent; a named band of brothers | Outdated graphics and UI; wants more build diversity |
| **Wartales** | A favourite* | Mechanically weaker than Battle Brothers |
| **Grim Dawn** | "Elegant complexity": a simple surface with deep options | (He prefers it over Path of Exile, which he finds complicated but shallow) |
| **Kerbal Space Program** | Physics, building and testing* | Graphics and performance; unfulfilled potential |
| **Stardew Valley** | The vibe of growing something | — |
| **Valheim** | Survival, building, a world to explore* | — |
| **Spore** (cell phase) | Evolution, the creature growing* | — |
| **Cyberpunk 2077** | The topic, story and vibe; loves the idea of "cyberpunk nature" | — |
| **Backpack Battles** | Build-then-watch; synergies* | — |
| **Mechabellum** | Build-then-watch strategy | Too clunky; dislikes its UI |
| **Total War series · The Witcher · Guildrun** | Favourites; no detail recorded yet | — |
| **Primordialis** | — | "Too heavy": too much stuff for too small an effect |

*\* My reading of why, not his own words: correct it.*

**What he avoids:** copycats (he skipped Magicraft because it "copy-pasted Noita's mechanics into the same but worse game"); the generic roguelike template; tedious, complicated systems that only add artificial difficulty; heavy UI; high-APM stress ("StarCraft cortisol"); Factorio-style tedium; games too hard or too long for adult life.

---

## 4. Design taste (what he consistently wants)

**Feel and structure**
- Build-then-watch strategy over twitch; real-time skill only as spice.
- Calm pace; cozy entry difficulty with a Rain World-level mastery ceiling; about 30-minute runs.
- Organic over generic; the world simulated, not scripted; a living ecosystem with residents, not waves.
- **Fun is a self-chosen gamble plus the anticipation of a combo firing.** Risk should be voluntary and readable.
- Exploration and path choice; synergy discovery; programming-like logic at the core, randomness as the cherry on top.
- Named characters or crew; emergent stories; dark humour (tone either wholesome or tragically grim-funny).
- Find the fun first: many small experiments, keep the best, build around them; polish the base before adding systems.

**Readability**
- The picture first, words second: state readable from the world itself; text as a bonus, in tooltips on demand.
- Readable at a glance, not dense; every danger shows a way past it; causes are visible; deaths are attributed.
- Every visible thing has a purpose, or is clearly cosmetic: nothing decorative may look interactive.
- Simple, strict, learnable rules ("small insects can't cross water").
- One concept, one plain name; numbers agree everywhere.
- Agency needs options, not just information; every choice needs something to be chosen against; every loss costs something he cares about.
- Nothing may look broken; creatures arrive from somewhere you can see.

**Look and sound**
- "Pixel art, but modern pixel art with modern rendering": crisp, small pixels (about Noita's scale, around 4 screen pixels per world pixel at 1080p), smooth light, glow and bloom allowed, never washing things out.
- Density over size: places dense with detail at three scales (small life, middle-sized features, the rock); a big empty space reads as dead.
- Minimal UI, kept at the edges; test and dev panels never cover the game.
- Motion that feels good but restrained; creatures told apart by how they move.
- Sound: one clear cue per event; nothing that repeats like a machine gun.

---

## 5. Topics he loves (for themes, research and recommendations)

- **Nature and science:** botany and plants (his top interest), insects, spiders, evolution ("oh boy, I love evolution"), cells, paleontology and dinosaurs, physics and fluid mechanics.
- **History:** the ancient world (Roman, Egyptian, Japanese), European history, WW2, Czech history and secularism (he has a YouTube project, "Why God Died in Bohemia").
- **Fantasy and other:** fantasy magic, alchemy, cyberpunk nature, sound and music; Warhammer Fantasy (dwarves, chaos dwarves, the Empire).
- **Hobbies:** tabletop miniatures (Warhammer, D&D; Pathfinder 2e over Foundry VTT), 3D printing and maker projects, fragrance, game design.

---

## 6. Finding new games: a quick filter

**Likely to love it if it has:**
- a simulated world where systems interact (materials, ecosystems, physics);
- build-then-watch or plan-then-execute play, with depth under a simple surface;
- emergent stories, named characters, a band or lineage;
- organic, procedural places worth exploring; voluntary risk;
- nature, evolution, biology, engineering or history as its subject;
- a distinct pixel or hand-made look, readable at a glance.

**Likely to bounce off it if it:**
- clones a beloved game without adding anything;
- is high-APM, punishing for its own sake, or demands long sessions to progress;
- buries its systems under heavy UI, or adds complexity without depth;
- relies on red vs green to tell things apart.

**A prompt to paste when asking for recommendations:**
> Recommend 5 games I haven't played, ranked, one line each on why I'd like it and one honest risk. I love Noita, Rain World (but find it too hard and long), CK3 for its stories, Battle Brothers, Grim Dawn's elegant complexity, Kerbal Space Program, Stardew's vibe, Spore's cell phase and Backpack Battles. I want simulation-driven, organic systems, build-then-watch or plan-then-act play, emergent stories, and nature, evolution or engineering themes. I dislike copycats, generic roguelike templates, heavy UI, high-APM stress and artificial difficulty. I'm partly red/green colour-blind.

---

## Things to consider

- This is a snapshot: tastes move. Re-check sections 3 and 4 every few months or after a big new game.
- The design taste in section 4 comes mostly from Bitbloom's playtests; some of it is specific to pixel-sim games (the pixel scale, one cue per event). Treat those lines as defaults, not laws, for other genres.
- Sensitive or private details are deliberately left out; add only what you want any collaborator to know.
