# Kingmaker Level-Up Selection Scroll Fix

Version 1.0.0

A small Unity Mod Manager / Harmony mod for Pathfinder: Kingmaker that fixes the level-up feature selection UI when too many feature-selection buttons are shown.

Source code:

https://github.com/RUSBAR-ITS/RB_KingmakerLevelUpSelectionScrollFix

## What It Does

Some mods or settings can create many feature-selection entries during level-up. The main motivating case is [Bag of Tricks](https://www.nexusmods.com/pathfinderkingmaker/mods/26) with `Feat Selection Multiplier`.

Pathfinder: Kingmaker already wraps the top feature-selection icons onto multiple rows, but the whole top selector block keeps growing vertically. When there are many entries, it steals space from the feat list below, even though that lower list already has its own scrollbar.

This mod fixes only that UI problem:

- limits the height of the top feature-selection button area;
- adds vertical scrolling to that top area;
- keeps the game's existing icon wrapping behavior;
- keeps the lower feat list usable;
- uses the game's own level-up scrollbar style where possible;
- keeps scrolling position when the selection UI refreshes.

## Requirements

- Pathfinder: Kingmaker
- Unity Mod Manager
- A normal PC UI level-up screen

The mod was built as a classic Unity Mod Manager / Harmony C# mod for the old Unity / .NET Framework style used by Pathfinder: Kingmaker.

## Installation

1. Download the mod archive.
2. Open Unity Mod Manager.
3. Select Pathfinder: Kingmaker.
4. Go to the `Mods` tab.
5. Install the archive with `Install Mod`.
6. Start the game.

The installed mod folder should contain:

```text
KingmakerLevelUpSelectionScrollFix/
|- Info.json
`- KingmakerLevelUpSelectionScrollFix.dll
```

The `.pdb` file may be included for diagnostics, but it is not required.

## Settings

The mod exposes a small settings panel in Unity Mod Manager:

- `Enable patch`: enables the UI patch. Changing this requires a mod reload.
- `Max selector height`: maximum height of the top selection area. Default: `220`.
- `Scroll sensitivity`: mouse wheel scroll speed. Default: `35`.
- `Show vertical scrollbar`: shows or hides the scrollbar.
- `Dump relevant UI hierarchy`: diagnostic option for troubleshooting. Keep it disabled during normal play.

## Compatibility Notes

This mod is designed for the normal PC level-up UI.

It does not patch console/gamepad UI classes. It also does not patch the lower feat list, because the lower list already scrolls and is not the cause of the layout problem.

The mod should be compatible with [Bag of Tricks](https://www.nexusmods.com/pathfinderkingmaker/mods/26). It does not replace or modify Bag of Tricks behavior; it only makes the game's level-up UI handle many generated selection buttons better.

## Troubleshooting

If the mod loads but the top selector still does not scroll:

1. Confirm that the mod is enabled in Unity Mod Manager.
2. Confirm that the problematic screen is the PC level-up feature selection screen.
3. Try increasing or decreasing `Max selector height`.
4. Enable `Dump relevant UI hierarchy`, reproduce the issue once, then check the game log.

For normal play, disable `Dump relevant UI hierarchy` after troubleshooting so the log stays small.

## Credits

Created for the Pathfinder: Kingmaker level-up UI case where many feature-selection buttons are present, especially when using [Bag of Tricks](https://www.nexusmods.com/pathfinderkingmaker/mods/26) `Feat Selection Multiplier`.
