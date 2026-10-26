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

19. **Agency needs options, not just information** (Bitbloom rule 12, 2026-09-26). Knowing what's coming is worthless without a lever to answer it.
20. **Every loss must cost something he cares about** (Bitbloom rule 13, 2026-09-26), and **even the first round needs a stake** (rule 33, 2026-09-27). Toys without a goal or a loss were judged "AFK clicker" whatever their mechanics (Bitbloom log, 2026-10-05).
21. **Telegraph arrivals on the map itself, before the round** (Bitbloom rule 26 and its ‼️ note, 2026-09-27), and **every danger shows a way past it** (Bitbloom rule 46, 2026-10-25): its tell, its blind spot, one thing in reach that beats it.
22. **Pacing: not slow, then too fast** (Bitbloom rule 35, 2026-09-27). Tuning that worked (Trap_v2, 2026-10-05): one forced edit every 60–90 s, never two threats starting in one minute, few creatures in minute 1.
23. **The bite-then-burst moment is the best moment** (Bitbloom rule 40, 2026-09-30): a creature commits at the plant and the defence answers it there. Reactive triggers ("on bitten") beat timers (Bitbloom vein-bus toy, 2026-10-05).
24. **The verb before the grammar** (Bitbloom design lane, 2026-10-05, toy 5): first contact is one action understood in 5 seconds; programming arrives as a way to change it.
25. **The Expedition's UI was the clearest** (Bitbloom, Milan, 2026-10-05): worded chips, a "reads as" sentence under the strand, a screen-space card, edits while paused. Never pictogram strings drawn in the world.
26. **Chores should be automatic from the start** (Bitbloom, the Hollow toy, 2026-10-05): "produce should reach Doris automatically… not carried". The manual tier of "manual → automated" must be a decision, never a chore.
27. **Density first** (Bitbloom rule 45, 2026-10-25): a small player in a big samey space reads as empty; keep the playable area tight and full. **Test panels never cover the game** (rule 47): dev readouts dock outside the play area, at a fixed size.
28. **Czech keyboard: no Y / Z and no Ctrl / Alt as game keys** (Milan's profile, 2026-10-26, `05_Reference/Milan_Preferences_Profile.md` §2). Y and Z swap places on QWERTZ; Alt is AltGr on the right.
29. **Cozy entry, mastery ceiling, about 30-minute runs** (profile §4, 2026-10-26). Games too hard or too long for adult life lose him (Rain World: "perfect but too hard and too time-consuming").
30. **Risk should be voluntary and readable** (profile §3–4, Noita, 2026-10-26): there is always a safe line; the reward is visible before the risk; the risk is bounded per segment; one failure teaches its price. "Chill because chosen, tense because dangerous."
31. **No copycats, no generic roguelike template, no complexity without depth** (profile §3, 2026-10-26: Magicraft "copy-pasted Noita's mechanics into the same but worse game"; Primordialis "too much stuff for too small an effect"; Grim Dawn's "elegant complexity" is the bar).

## Open shortcomings (design issues that came out of reviews)

- [ ] ⚠️ **Day under-stimulation** is A's named risk #1. Test revised 2026-10-26: ≥ 1 readable, attributable event at your plants per ~10–15 s of Day, ≤ 1 threat group starting per minute, ≥ 3 real choices per Planning (Block 1 pack §4).
- [ ] ⚠️ **Perfect ≡ Good** is still the live minigame reward path (Ledger warning 4).
- [ ] ⚠️ **The game cannot be lost** and has no victory arc (audit H1, H2). → Block 1 B3 (Doris by Dusk).
- [ ] ⚠️ **The first ten minutes are undesigned** (audit H4).
- [ ] ⚠️ **Animal stats are shown by holding Left Alt** (`AnimalController.showStatsKey`), against rule 28. Rebind (e.g. hold Shift, or hover only) in the next fix pass.

## Log

_(No dated playtest entries yet. The first playtest after the A proof of concept starts here, one entry per session: date, ✅ ❌ 💡 ⚠️.)_
