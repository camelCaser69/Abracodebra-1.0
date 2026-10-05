# Baseline on Unity 6000.0.39f1 (before the 6.3 upgrade)

Recorded 2026-10-05 through the Unity CLI (`com.unity.pipeline` 0.8.0-exp.1), Editor 6000.0.39f1, `SampleScene`, after the Phase B package removal and the CLI install. This is the "before" for `04_Reviews/2026-10_Unity63_Upgrade.md`. Point-in-time document: not edited after Phase E closes.

## Console

| When | Errors | Warnings | Logs |
| --- | ---: | ---: | ---: |
| Editor open, before play | 0 | 0 | 0 |
| After 10 s of play, then stop | 0 | 1 | 27 |

The one warning (present on every play start; a script-execution-order issue, not a 6.0 artefact):

```
[GeneLibraryLoader] GeneServices not initialized - initializing now. Check script execution order.
  GeneLibraryLoader:Awake () (Assets/Scripts/Genes/Core/GeneLibraryLoader.cs:29)
```

After the upgrade the same play session should show the same warning and nothing else. Expected play-mode log lines (27): GeneServices init, RunManager state Planning, ResolutionManager profile `Pixel Perfect (Native)` (PPU 16, 480×270, zoom 8.4375), ToolSwitcher, InventoryService (32 slots, 8×4), GameUIManager init (HUD, Start Day button), DorisHungerSystem registered, GridSnapStartup (1 entity), run seed, WeatherManager phase Day, WaveManager initialised, TileInteractionManager mapping backup (4 mappings).

## Captures (1280×720, 10 s into play, Planning phase)

| File | Source | Shows |
| --- | --- | --- |
| `game_view_screen.png` | `--source screen` | Game window with UI Toolkit: Gene Editor (left), Inventory 8×4 (middle), item detail panel (right), green START DAY button. The UI is opaque, so the world is hidden |
| `game_view_camera.png` | `--source camera` | Main Camera only: dual-grid terrain (grass, dirt, water with soft edges and grass fringe), Doris (the stump), the gardener, a few pickups. This is the image to compare for dual-grid and post-processing |

## Notes

- `TileInteractionManager` rewrites `Assets/Editor/TileMappingsBackup/TileMappingsBackup.json` (the `backupTime` field) on every play start. Not a change to commit.
- `Packages/packages-lock.json` changed once after the font/package cleanup and the pipeline install (committed separately).
