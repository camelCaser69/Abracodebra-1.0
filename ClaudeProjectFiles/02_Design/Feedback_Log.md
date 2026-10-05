# Milan's feedback log — Abracodabra

Started 2026-10-05 · Status: **living document, appended after every playtest or review** · Related: `01_Core/projectmemory.md` (decision log)

What Milan liked, disliked and asked for, in his words where possible, so general rules and design shortcomings can be read out later. Claude appends here as feedback arrives, never batched. Decisions live in the decision log; this file keeps the *taste* behind them. Every rule names its source and date. Cross-project rules are copied from Bitbloom's Feedback_Log ("Bitbloom rule N") and apply here too.

## How to use

- **Distilled rules** are the patterns across notes. Update them when a new note confirms or contradicts one.
- **Log** entries are dated, per review: ✅ liked · ❌ disliked · 💡 asked for / idea · ⚠️ shortcoming still open.

## Distilled rules

### Abracodabra's own

1. **No hand-piloted avatar control during a running clock** (2026-07-20, Phase Identity). Milan's verdict: manual movement and interaction in the simulation feel **"finicky and hectic"**, so player actions must be programmable or automatic. This takes piloting out of the design space for both A and B. What replaced it: Mark & Go (click a tile, the gardener walks there and acts), fixtures, queued orders. `02_Design/Concepts/Phase_Identity_Final_Refinement_2026-07-20.md`.
2. **WeGo as built felt cortisol-heavy** (reported by Milan 2026-10-05, in the modernization pack brief). ⚠️ The exact wording and date of the original remark are not on disk; nothing in the 2025–2026 docs uses the word. The nearest written trace is Phase Identity §4: B-rev is "the calmer, more ADHD-friendly thinking game", and A's named risk is under-stimulation, not stress. Read it as: pace-casual, never frantic. Pausing and speed (Space, Tab) must be visible from the first day.
3. **Pace-casual, not easy-casual** (the target audience line since the 2025 chats; `projectmemory` Snapshot, Phase Identity §2.1). Skill is anticipation, not reflexes.
4. **Edit speed governs the gene editor** (2025 design chats; `Gene_Systems_Deep_Dive.md`). Players edit several seeds every Planning phase, so complexity must scale with run progression (Telescope Strand).
5. **Honest, unsentimental feedback** on viability, timelines and architecture (CLAUDE.md since 2025). Pitch better solutions unasked; when there are options, score them and recommend one.

### Cross-project (about Milan, not about a game)

6. **Readable at a glance, not dense** (Bitbloom rule 7, 2026-09). Text size in between (too small and too big both failed); the row ↔ part mapping must be instant; nothing that points at things that aren't there.
7. **Testing aids are welcome, clearly marked as temporary** (Bitbloom rule 9, 2026-09).
8. **Clear recommendations speed him up** (Bitbloom rule 10, 2026-09). He often answers "recs": give options with one recommended. (This pack's §7.4 was answered that way on 2026-10-05.)
9. **Polish the base before building on it** (Bitbloom rule 21, 2026-09-25). Too many systems at once hide what feels wrong; new systems only when they are a pillar of the base loop. (Same as this project's scope discipline.)
10. **The picture first, words second** (Bitbloom rule 30, 2026-09-27, sharpened 2026-09-30). State should read from the world itself; descriptions are a bonus. When text is needed: fewer lines, later keywords explained on the side (Crusader Kings 3 style), not paragraphs.
11. **Show it in the world, not in a label** (Bitbloom rule 36, 2026-09-28). Ranges, forecast times and growth read from the map (markers, icons, hover); numbers in labels read as clutter. Detail goes into tooltips on demand.
12. **Never encode state by red/green hue alone** (Bitbloom rule 41, 2026-09-30). Milan is partly red/green colour-blind. Use brightness, shape, outline or pattern, and check palettes for red/green pairs. (The Abracodabra icon system's colour-blind cliff, audit G4, is the same problem.)
13. **One concept, one or two places, in words, with a hover** (Bitbloom rule 42, 2026-09-30). A new resource or rule gets a plain name the player would use, at most two places on screen, and a hover saying what it does; a UI hover beats the world's tooltip.
14. **Playtest sheets are short and walk the play in order** (Bitbloom rule 43, 2026-10-02). One sheet ≤ ~8 items in the order they happen; each item = *Do* / *Look for* / *Answer* on separate short lines; a new mechanic gets one plain sentence first; conditional items go on a separate "if it comes up" list. Don't assume what the screen says.
15. **Terms explain themselves where they appear** (Bitbloom rule 44, 2026-10-02). Any game term inside a hover gets its own small explanation box stacked under the main hover, like keyword tooltips in modern roguelikes.
16. **UI must teach the game by itself** (2026-07 Fistful of Mercs sessions, `99_Archive/2026-07_Cowork_Era/Chat_History_Digest.md` §2). Given a working prototype of his own game he "almost didn't know what to do": phase tracker, always-on instruction banner, labeled decision screens.
17. **Markdown, never .docx; moss-and-sage greens and grays over orange, brown and amber** (same digest §2, 2026-07). Explainers: explain, don't sell; first person, his voice.
18. **Iterative deepening in design talks** (same digest, 2026-07): repeated adversarial passes ("go one more iteration, find the remaining holes"), options weighted and scored, his own ideas may be rejected when argued. His design law: *maximum combinations and strategies from minimum complications.*

## Open shortcomings (design issues that came out of reviews)

- [ ] ⚠️ **Day under-stimulation** is A's named risk #1: needs ≥ 1 meaningful decision or reaction per 15–20 s of Day (Ledger D1, Phase Identity §2.3).
- [ ] ⚠️ **Perfect ≡ Good** is still the live minigame reward path (Ledger warning 4).
- [ ] ⚠️ **The game cannot be lost** and has no victory arc (audit H1, H2).
- [ ] ⚠️ **The first ten minutes are undesigned** (audit H4).

## Log

_(No dated playtest entries yet. The first playtest after the A proof of concept starts here, one entry per session: date, ✅ ❌ 💡 ⚠️.)_
