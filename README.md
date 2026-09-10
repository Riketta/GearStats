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
- Click the pawn button to apply a pawn's stats - shooting skill, genes, traits,
  age - to all weapon rows: aiming time, cooldown, melee damage, armor
  penetration, effective range and per-bracket hit chance recompute exactly as
  the game does when that pawn fires the weapon. "Pawn: none" returns the raw
  stats. Hover the pawn button to see which of the pawn's traits, genes and
  hediffs shift aiming time, cooldown, shooting accuracy, melee damage or melee
  hit chance, with the game's own value breakdown per stat.
- Ranged weapons can switch the accuracy column between all/average and a single
  range bracket, labeled with its actual distances, e.g. "Short (3-12)". The
  sorted column survives bracket switches.
- Odyssey unique-weapon traits factor into the stats like they do in game (burst
  count and speed for DPS) and are listed on the weapon's name tooltip.
- Click a row to jump to the item, and use the debug window (dev mode) to inspect
  the raw stats of a thing - or, next to the pawn button, dump the selected
  pawn's combat stats with the game's own per-stat explanations.

## Compatibility

- No hardcoded defs: modded weapons and apparel are picked up from their stats
  like vanilla gear.
- Combat Extended: extra columns (sway, spread, sights, magazine capacity,
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
- Shooter-adjusted numbers mirror the vanilla formulas (`VerbProperties.AdjustedCooldown`,
  `AdjustedMeleeDamageAmount`, `GetDamageFactorFor`, `ShotReport.HitFactorFromShooter`,
  `VerbProperties.GetHitChanceFactor`) instead of approximating them, so the table
  always agrees with what the pawn would actually dish out. Turret accuracy applies
  the turret's `ShootingAccuracyTurret` the same way, and turret warmup and cooldown
  come from the turret building's `turretBurstWarmupTime` (shown as the midpoint of
  its range) and `turretBurstCooldownTime` (falling back to the gun verb's cooldown) -
  the values the game actually cycles on, which can differ from the gun def's stats.
- Melee: damage and cooldown show the weapon's strongest attack with quality and
  material multipliers; DPS is the vanilla selection-weighted average across all
  its tools; "Max hit" is the biggest single hit of any attack (the blunt stun
  metric). With a pawn selected all of these are computed exactly like
  `StatWorker_MeleeAverageDPS` computes them for the wielding pawn, and DPS is
  multiplied by the pawn's melee hit chance like the vanilla pawn stat
  (`StatWorker_MeleeDPS`).
- With Combat Extended, shooter adjustment of the accuracy and melee hit chance
  columns is skipped: CE replaces those mechanics with its own spread, sway and hit
  chance model. Damage, cooldown and range still scale with the pawn's stats.
- The combat stat list is curated to mirror vanilla's code, which hardcodes these
  same links, and is resolved through `DefDatabase` at load: a mod that removes a
  stat only neutralizes that one adjustment instead of crashing. What modifies
  each stat is always read at runtime through the regular stat system, so modded
  traits, genes and hediffs count without any per-mod support.

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
