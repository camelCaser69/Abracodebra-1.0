# Chat History Digest — Cowork Session Context (for account migration)

**Written 2026-07-11.** Purpose: Claude account memory is OFF (shared account) and chat
history does not survive account/app migrations — this already happened once (July 2026
app update wiped all sessions). This document preserves the context that lived **only in
chats**, so a fresh Claude account reading the KB also knows the conversational history.
Read together with `projectmemory.md` (which holds the technical/design state — not
duplicated here).

**Coverage:** the 6 game-dev Cowork sessions on this machine as of 2026-07-11.
claude.ai web chats are NOT included (not readable from Cowork).

---

## 1. Session index

| Session (Cowork title) | Project | What happened | Where the output lives |
|---|---|---|---|
| Unity project setup recovery | Abracodebra | Post-update recovery after the app wiped chats; verified nothing on disk was lost; re-oriented via memory protocol | No new docs — state confirmed in `projectmemory.md` |
| Game UI improvements research | Abracodebra | Full UI/controls audit, scenario-neutral spine + both phase-model futures | `02_Design/Concepts/UI_Systems_Research.md` (2026-07-08) |
| Game minigames and mechanics research | Abracodebra | Weighted re-scoring of all minigame/mechanic concepts, supersedes old §7 ranking | `02_Design/Concepts/Minigames_And_Mechanics_Reevaluation.md` (2026-07-08) |
| Tabletop game Unity 6 template | Fistful of Mercs | Studied that project's docs; recommended Unity 6 **Universal 2D (URP)** template + code-gen pixel art (`Texture2D.SetPixels32`); located the dice-crafting concept docs | Fistful project folder (see §3) |
| Fistful of Mercs dice system | Fistful of Mercs | The big design marathon: v0.2 "Forge & Feud" → plunder economy → design review → holes audit → Company Charters → SimV3 engine + Monte-Carlo → v0.3 playable scene → story events + green recolor → md pitch doc | Fistful project folder (see §3) |
| Chat extraction for project migration | Meta | Produced this digest | This file |

---

## 2. Milan's preferences learned in chat (not written anywhere else)

These came up as live corrections — a new account should know them up front:

- **Hates .docx.** Deliverables he'll read or share should be **.md**. He explicitly had
  a generated Word pitch deleted and remade as markdown ("i dont want docx, i hate docx").
- **Pitch/explainer voice:** explain, don't sell. Write as if the reader is already
  interested and wants to understand the thinking process. First person, Milan's voice.
  He says he's "bad at explaining" — writing explainers for friends is a task he
  delegates gladly. (He's Czech / Prague-based; a Czech version was offered once.)
- **Visual taste:** prefers **green/gray (moss-and-sage) tones over orange/brown/amber**
  — he asked for a full recolor of the Fistful prototype away from parchment-amber.
- **UI must be self-explanatory.** Strong recurring theme: he got a working prototype of
  his *own* game and "almost didn't know what to do" — phase trackers, always-on
  instruction banners, labeled decision screens, and rules overlays fixed it. Assume any
  prototype UI needs to teach the game by itself.
- **Design-conversation style:** iterative deepening. He asks for repeated adversarial
  passes ("go one more iteration, find remaining holes"), wants options weighted and
  scored, accepts rejections of his own ideas when argued (upkeep rejected; loot economy
  his instinct, confirmed). Signature design law he quotes: **"maximum combinations and
  strategies from minimum complications."**
- **Session limits interrupt work regularly** (limits reset on Prague time). Lesson
  learned: after an interruption, **verify on disk what actually landed** before redoing
  or assuming completion — an interrupted session once left him running an old menu item
  thinking the new work was missing, when all 19 files had landed fine.
- **Editor feedback loop:** Cowork can't compile Unity. Standing routine: Claude writes
  code, flags anything unverified, Milan recompiles and **pastes console errors back**.
  Always tell him exactly which menu item / scene / button to use — ambiguity there has
  already cost a session of confusion.

---

## 3. Fistful of Mercs — the other project (separate folder, own KB)

A physical **tabletop game** prototyped digitally in a **separate Unity project folder**
(its own doc tree: `00_project/`, `02_design/`, `04_prototypes/`, `Assets/Scripts/`).
This Abracodebra KB has no other reference to it — that's why it's summarized here.

**Concept:** 3–5 player competitive-with-forced-cooperation game, 15th-century mercenary
bands. Each mercenary IS a physical die; equipment mounts as swappable die faces.
Concept D ("dice ARE the band") locked 2026-07-07 out of four candidate shapes
(`02_design/base-concepts-v0.md` holds the roads not taken; `00_project/idea-bank.md`
the raw seed notes).

**Design state as of 2026-07-11 (v0.3.1 stack, all in that project's `02_design/`):**

- Five-symbol face grammar (hit / block / plunder / morale / banner) + two keywords only
  (Pierce, Fear) + Link faces (co-occurrence combos — the Backpack Battles translation).
- d4–d12 recruit ladder (slots = half the sides); captain d20 = roll-under morale checks;
  morale is a cube on a track (NOT the d20 itself — early bug, fixed).
- **Plunder economy replaced gold** (Munchkin-style loot-token draws with helper shares);
  crucially coupled to finale-scoring — one decision, not two. Round loop: contract →
  pigeon message → commit dice to jobs (quest/raid/guard/lend) → simultaneous throws.
- **Dual finale**: table behavior steers via the war clock — peaceful season → Tourney
  gauntlet; raid-heavy → The War (semi-co-op). "Every table gets the ending it deserves."
- **Company Charters** (specialization identity, 5 archetypes) adopted; **upkeep
  rejected** (jobs already done by band cap + loot scarcity; "Veteran's Wages" parked).
- Key doc chain: `rules-v0.2-concept.md` → `mechanics/loot-economy.md` →
  `v0.2-design-review.md` → `v0.3-holes-audit.md` (5 critical bugs found incl. the
  corner-pip retreat fix and the 6-dice wagon-rack band cap) →
  `mechanics/company-charters.md` → `04_prototypes/digital/unity-prototype-v2-plan.md`.

**Unity prototype state:** `SimV3` namespace (~13 files + UI layer) — full v0.3 rules
headless with bot players and Monte-Carlo (menu **Tabletop → v0.3 Sim**, E1–E10
experiment suite, targets like "War fires in 40–60% of seasons"). Playable guided scene:
**Tabletop → Setup v0.3 Playable Scene (you + 3 bots)** (NOT the old "Setup Prototype
Scene" item, which is the v1.1 gold-economy build). Six model story events in
`SimV3/StoryEventsV3.cs` behind a `StoryEvents` flag. Look: moss-and-sage palette.
Every contested rule is a `RulesetOptions` flag. **Next open loop:** Milan runs the
Monte-Carlo sweeps and brings numbers back for tuning; M4 (replay/report export) unbuilt.
An .md pitch for a friend exists at `00_project/fistful-of-mercs-pitch.md`.

---

## 4. Abracodebra sessions — chat nuance beyond the docs

The technical outcomes are fully recorded in `projectmemory.md` and the two Concepts
docs. What the chats add:

- The July 2026 app update **deleted the Cowork project setup but not the files** —
  recovery was pure re-orientation. The memory protocol worked as designed; this digest
  extends the same insurance to conversational context.
- Both research sessions followed the same commissioning pattern: Milan asks broadly
  ("make research about X, weigh the concepts"), Claude asks scoping questions, then
  delivers one big weighted .md into `02_Design/Concepts/` with projectmemory updated in
  the same session. That's the expected shape for future research requests.
- The **§4.3 phase experiment** (Commit & Watch vs player-driven WeGo) was still the
  undecided gate for both docs as of 2026-07-08 — both were written scenario-neutral on
  purpose.

---

## 5. For the new account: first-session checklist

1. Read `CLAUDE.md` (auto-loaded) → `projectmemory.md` → this digest.
2. Know the two-project situation: **Abracodebra** (this folder) and **Fistful of Mercs**
   (separate folder — ask Milan to mount it when tabletop work comes up).
3. Honor the §2 preferences without being told again (md not docx, green tones,
   self-teaching UI, honest weighted analyses, exact menu-item instructions).
4. Open loops at time of writing: Abracodebra — A-pack Part 4 checklist, F1 DNA-strand
   migration, §4.3 experiment; Fistful — Monte-Carlo tuning numbers, M4.
