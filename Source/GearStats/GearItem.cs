using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GearStats
{
    internal enum GearKind
    {
        Ranged,
        Melee,
        Grenades,
        Apparel,
        Turrets
    }

    internal enum OwnerKind
    {
        None,
        Colonist,
        Prisoner,
        Hostile,
        Friendly,
        Corpse
    }

    /// <summary>
    /// Snapshot of one piece of gear on the map. Instances are rebuilt on every list
    /// refresh; nothing here is saved to the game state.
    /// </summary>
    internal abstract class GearItem
    {
        /// <summary>Game ticks per second, for converting warmup/cooldown seconds.</summary>
        protected const int TicksPerSecond = 60;

        protected readonly bool Ce;

        /// <summary>Pawn whose stats are applied to the displayed values; null = raw weapon stats.</summary>
        protected Pawn Shooter;

        public Thing Thing;
        public string Label = "<unknown>";
        public string Traits;
        public QualityCategory Quality = QualityCategory.Normal;
        public int HpPercent = 100;
        public float MarketValue;
        public float Damage;
        public float ArmorPenetration;
        public float Dps;
        public float Cooldown;
        public float Warmup;
        public float MaxRange;
        public string DamageType = "";
        public OwnerKind Owner;
        public string OwnerName;
        public bool Craftable;
        public Building_WorkTable CraftPos;
        public bool InStorage;
        public IntVec3 StoragePos;
        public readonly List<Exception> Exceptions = new List<Exception>();

        protected GearItem(bool ce)
        {
            Ce = ce;
        }

        /// <summary>Catch-all wrapper: a modded def with broken stats must never break the table.</summary>
        public void Fill(Thing th, Pawn shooter = null)
        {
            Shooter = shooter;
            try
            {
                FillCore(th);
                if (shooter != null)
                {
                    AdjustForShooter(shooter);
                }
            }
            catch (Exception e)
            {
                Exceptions.Add(e);
                if (Prefs.DevMode)
                {
                    Log.Warning("[GearStats] Failed to read stats of \"" + Label + "\": " + e);
                }
            }
        }

        /// <summary>Second pass: apply the shooter's stats (skills, genes, traits, age) on
        /// top of the raw values. Runs only when a pawn is selected.</summary>
        protected virtual void AdjustForShooter(Pawn shooter)
        {
        }

        protected virtual void FillCore(Thing th)
        {
            Thing = th;
            MarketValue = th.MarketValue;
            Label = th.Stuff != null ? th.Stuff.label + " " + th.def.label : th.def.label;
            HpPercent = 100 * th.HitPoints / th.MaxHitPoints;
            if (th.TryGetQuality(out QualityCategory qc))
            {
                Quality = qc;
            }

            // Odyssey unique weapons: the comp renames the weapon (CompUniqueWeapon
            // .TransformLabel) and carries its trait list - without the rename the
            // table would show them as a generic "revolver". Trait offsets/factors
            // already flow through GetStatValue like in vanilla combat.
            if (th.TryGetComp<CompUniqueWeapon>(out CompUniqueWeapon unique) && unique.TraitsListForReading.Count > 0)
            {
                Label = unique.TransformLabel(Label);
                Traits = unique.TraitsListForReading.Select(t => t.LabelCap.RawText).ToCommaList().CapitalizeFirst();
            }
        }

        /// <summary>Display unit and conversion factor for the player's temperature settings.</summary>
        protected static (string unit, float coeff) Temperature()
        {
            switch (Prefs.TemperatureMode)
            {
                case TemperatureDisplayMode.Celsius: return ("°C", 1f);
                case TemperatureDisplayMode.Fahrenheit: return ("°F", 1.8f);
                case TemperatureDisplayMode.Kelvin: return ("K", 1f);
                default: return ("", 1f);
            }
        }
    }
}
