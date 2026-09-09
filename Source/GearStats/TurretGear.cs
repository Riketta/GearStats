using RimWorld;
using UnityEngine;
using Verse;

namespace GearStats
{
    internal class TurretGear : GearItem
    {
        public float MinRange;
        public float AccuracyTouch;
        public float AccuracyShort;
        public float AccuracyMedium;
        public float AccuracyLong;

        public TurretGear(bool ce) : base(ce)
        {
        }

        public float AccuracyFor(AccuracyBracket bracket)
        {
            switch (bracket)
            {
                case AccuracyBracket.Touch: return AccuracyTouch;
                case AccuracyBracket.Short: return AccuracyShort;
                case AccuracyBracket.Medium: return AccuracyMedium;
                case AccuracyBracket.Long: return AccuracyLong;
                default: return 0f;
            }
        }

        public string AccuracyCell()
        {
            return AccuracyText.Cell(MinRange, MaxRange, AccuracyTouch, AccuracyShort, AccuracyMedium, AccuracyLong);
        }

        protected override void FillCore(Thing th)
        {
            base.FillCore(th);

            ThingDef gunDef = th.def?.building?.turretGunDef;
            if (gunDef == null)
            {
                return;
            }

            VerbProperties verb = VerbWithProjectile(gunDef);
            if (verb != null)
            {
                Warmup = verb.warmupTime;
                MaxRange = verb.range;
                MinRange = verb.minRange;
                if (verb.defaultProjectile?.projectile != null)
                {
                    Damage = verb.defaultProjectile.projectile.GetDamageAmount(th);
                    DamageType = verb.defaultProjectile.projectile.damageDef.label;
                    ArmorPenetration = verb.defaultProjectile.projectile.GetArmorPenetration(th);
                }
            }

            float turretAcc = th.GetStatValue(StatDefOf.ShootingAccuracyTurret);
            bool applyAccuracy = verb == null || verb.canGoWild;
            AccuracyTouch = AdjustedAccuracy(AccuracyText.Touch, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyTouch), applyAccuracy);
            AccuracyShort = AdjustedAccuracy(AccuracyText.Short, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyShort), applyAccuracy);
            AccuracyMedium = AdjustedAccuracy(AccuracyText.Medium, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyMedium), applyAccuracy);
            AccuracyLong = AdjustedAccuracy(AccuracyText.Long, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyLong), applyAccuracy);

            Cooldown = gunDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
        }

        /// <summary>Hit chance at a bracket distance for non-pawn casters, mirroring
        /// ShotReport.HitFactorFromShooter: weapon accuracy clamped to 1-100%, times the
        /// turret's ShootingAccuracyTurret exponentiated by distance (no range-category
        /// factor), floored at 2.01% and capped at 100%. Verbs that cannot shoot wild
        /// (mortars) ignore the turret accuracy entirely; brackets outside the weapon's
        /// range stay 0 and render as "-".</summary>
        private float AdjustedAccuracy(float distance, float turretAcc, float weaponAccuracy, bool applyAccuracy)
        {
            if (!applyAccuracy || MinRange > distance || MaxRange < distance)
            {
                return 0f;
            }

            float weapon = Mathf.Clamp(weaponAccuracy, 0.01f, 1f);
            float factor = Mathf.Max(Mathf.Pow(turretAcc, distance), 0.0201f);
            return Round(Mathf.Min(weapon * factor, 1f) * 100f);
        }

        private static VerbProperties VerbWithProjectile(ThingDef def)
        {
            if (def?.Verbs == null)
            {
                return null;
            }

            VerbProperties first = null;
            foreach (VerbProperties vp in def.Verbs)
            {
                if (vp.defaultProjectile?.projectile == null)
                {
                    continue;
                }

                if (vp.isPrimary)
                {
                    return vp;
                }

                first ??= vp;
            }

            return first;
        }

        private static float Round(float value) => (float)System.Math.Round(value, 2);
    }
}
