using RimWorld;
using Verse;

namespace GearStats
{
    /// <summary>The pawn stats the vanilla combat formulas consume, resolved defensively:
    /// a total-conversion mod that removes one only neutralizes that adjustment instead of
    /// throwing. Which stats matter is vanilla code (ShotReport, VerbProperties,
    /// StatWorker_MeleeDPS); what modifies each of them is resolved at runtime by the
    /// regular stat pipeline (traits, genes, hediffs, apparel, skills, age).</summary>
    internal static class CombatStats
    {
        public static readonly StatDef AimingDelay = Get("AimingDelayFactor");
        public static readonly StatDef RangedCooldown = Get("RangedCooldownFactor");
        public static readonly StatDef ShootingAccuracy = Get("ShootingAccuracyPawn");
        public static readonly StatDef AccFactorTouch = Get("ShootingAccuracyFactor_Touch");
        public static readonly StatDef AccFactorShort = Get("ShootingAccuracyFactor_Short");
        public static readonly StatDef AccFactorMedium = Get("ShootingAccuracyFactor_Medium");
        public static readonly StatDef AccFactorLong = Get("ShootingAccuracyFactor_Long");
        public static readonly StatDef MeleeDamage = Get("MeleeDamageFactor");
        public static readonly StatDef MeleeCooldown = Get("MeleeCooldownFactor");
        public static readonly StatDef MeleeHitChance = Get("MeleeHitChance");
        public static readonly StatDef ShootingAccuracyTurret = Get("ShootingAccuracyTurret");

        /// <summary>The stat's runtime value for the pawn, or the neutral multiplier 1
        /// when a removed stat would have scaled the number.</summary>
        public static float Value(Pawn pawn, StatDef stat)
        {
            return stat != null ? pawn.GetStatValue(stat) : 1f;
        }

        private static StatDef Get(string defName)
        {
            return DefDatabase<StatDef>.GetNamedSilentFail(defName);
        }
    }
}
