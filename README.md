# Gear Stats
![Preview](About/Preview.png)

View sortable stats of every weapon, piece of apparel and turret on the current map
in one table - damage, DPS, accuracy, range, armor, insulation, market value and
quality. Filter what is shown and sort by any column.

Fork of "Weapons Tab" by bodlosh, restructured and updated.

## How it works

A "gear" button appears in the bottom main tab bar. It opens a table of everything
relevant on the current map, split into tabs: ranged weapons, melee weapons,
grenades, apparel and turrets.

- Click any column header to sort by that column, click again to flip the order.
- Checkboxes filter what is listed: items on the ground, items equipped by
  colonists, friendlies, hostiles or prisoners, items on corpses, craftable
  items and items in storage.
- Ranged weapons can switch the accuracy column between all/average and a single
  range bracket (touch, short, medium, long).
- Click a row to jump to the item, and use the debug window (dev mode) to inspect
  the raw stats of a thing.

## Compatibility

- No hardcoded defs: modded weapons and apparel are picked up from their stats
  like vanilla gear.
- Combat Extended: extra columns (bulk, sway, spread, sights, magazine capacity,
  counter-parry, AP) appear automatically when CE is active.
- Weapon Storage and Change Dresser: items stored in their containers are
  included when those mods are loaded.
- No Harmony patches and no def changes, safe to add and remove at any time.

## Technical notes

- Requires RimWorld 1.6. No other dependencies.
- Pure UI mod: it only reads existing things on the map, nothing is saved to the
  game state. The list refreshes on a short interval while the tab is open, not
  every frame.
- The main tab is registered as a `MainButtonDef`, so other mods can reorder or
  remove it through normal def XML.

## Build from source

Requires the .NET SDK. Build the Release configuration for the dll you ship - a plain
`dotnet build` defaults to Debug:

```
cd Source/GearStats
dotnet build -c Release -p:RimWorldDir="C:\Path\To\RimWorld"
```

The output lands in `Assemblies/GearStats.dll`; the whole mod folder can be
copied or symlinked into the game's `Mods` directory.

## Credits

- Original "Weapons Tab" mod: bodlosh.
