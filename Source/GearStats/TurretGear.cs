using System.Collections.Generic;
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

        /// <summary>Prototype gun things per turret gun def, so damage and armor
        /// penetration read their multiplier stats from a real gun, like the game does
        /// when the turret fires (Building_TurretGun.MakeGun builds one the same way).</summary>
        private static readonly Dictionary<ThingDef, Thing> GunPrototypes = new Dictionary<ThingDef, Thing>();

        protected override void FillCore(Thing th)
        {
            base.FillCore(th);

            BuildingProperties building = th.def?.building;
            ThingDef gunDef = building?.turretGunDef;
            if (gunDef == null)
            {
                return;
            }

            VerbProperties verb = VerbWithProjectile(gunDef);
            if (verb != null)
            {
                MaxRange = verb.range;
                MinRange = verb.minRange;
                if (verb.defaultProjectile?.projectile != null)
                {
                    ProjectileProperties projectile = verb.defaultProjectile.projectile;
                    Thing gun = GunPrototype(gunDef);
                    Damage = projectile.GetDamageAmount(gun);
                    DamageType = projectile.damageDef.label;
                    ArmorPenetration = projectile.GetArmorPenetration(gun);
                }
            }

            // The game cycles turrets on the building's burst fields; the gun verb's
            // warmupTime and RangedWeapon_Cooldown stat are never consulted
            // (Building_TurretGun.TryStartShootSomething / BurstCooldownTime). Warmup
            // is a random point in the burst warmup range, shown as its midpoint.
            Warmup = (building.turretBurstWarmupTime.min + building.turretBurstWarmupTime.max) / 2f;
            Cooldown = building.turretBurstCooldownTime >= 0f
                ? building.turretBurstCooldownTime
                : verb?.defaultCooldownTime ?? 0f;

            float turretAcc = CombatStats.ShootingAccuracyTurret != null
                ? th.GetStatValue(CombatStats.ShootingAccuracyTurret)
                : 1f;
            bool applyAccuracy = verb == null || verb.canGoWild;
            AccuracyTouch = AdjustedAccuracy(AccuracyText.Touch, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyTouch), applyAccuracy);
            AccuracyShort = AdjustedAccuracy(AccuracyText.Short, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyShort), applyAccuracy);
            AccuracyMedium = AdjustedAccuracy(AccuracyText.Medium, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyMedium), applyAccuracy);
            AccuracyLong = AdjustedAccuracy(AccuracyText.Long, turretAcc, gunDef.GetStatValueAbstract(StatDefOf.AccuracyLong), applyAccuracy);
        }

        private static Thing GunPrototype(ThingDef gunDef)
        {
            if (!GunPrototypes.TryGetValue(gunDef, out Thing gun))
            {
                gun = ThingMaker.MakeThing(gunDef, gunDef.MadeFromStuff ? GenStuff.DefaultStuffFor(gunDef) : null);
                GunPrototypes[gunDef] = gun;
            }

            return gun;
        }

        /// <summary>Hit chance at a bracket distance for non-pawn casters, mirroring
        /// ShotReport.HitFactorFromShooter: weapon accuracy clamped to 1-100%, times the
        /// turret's ShootingAccuracyTurret exponentiated by distance (no range-category
        /// factor), the combined chance floored at 2.01% and capped at 100% like
        /// ShotReport.AimOnTargetChance_StandardTarget. Verbs that cannot shoot wild
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
            return Round(Mathf.Clamp(weapon * factor, 0.0201f, 1f) * 100f);
        }

        /// <summary>Hit chance at a bracket distance for non-pawn casters, mirroring
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
