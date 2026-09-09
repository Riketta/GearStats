using RimWorld;
using Verse;

namespace GearStats
{
    internal class GrenadeGear : GearItem
    {
        public float MinRange;
        public float ExplosionRadius;
        public int ExplosionDelay;

        private VerbProperties mainVerb;

        public GrenadeGear(bool ce) : base(ce)
        {
        }

        protected override void FillCore(Thing th)
        {
            base.FillCore(th);
            Cooldown = th.GetStatValue(StatDefOf.RangedWeapon_Cooldown);

            VerbProperties verb = VerbWithProjectile(th);
            if (verb == null)
            {
                return;
            }

            mainVerb = verb;
            Warmup = verb.warmupTime;
            MaxRange = verb.range;
            MinRange = verb.minRange;
            if (verb.defaultProjectile?.projectile != null)
            {
                ProjectileProperties projectile = verb.defaultProjectile.projectile;
                Damage = projectile.GetDamageAmount(th);
                DamageType = projectile.damageDef.label;
                ExplosionDelay = projectile.explosionDelay;
                ExplosionRadius = projectile.explosionRadius;
                ArmorPenetration = projectile.GetArmorPenetration(th);
            }
        }

        /// <summary>Same shooter scaling as ranged weapons (mirrors vanilla AdjustedCooldown).</summary>
        protected override void AdjustForShooter(Pawn shooter)
        {
            Cooldown *= shooter.GetStatValue(StatDefOf.RangedCooldownFactor);
            if (mainVerb?.rangeStat != null)
            {
                MaxRange = shooter.GetStatValue(mainVerb.rangeStat);
            }
        }

        private static VerbProperties VerbWithProjectile(Thing th)
        {
            if (th.def?.Verbs == null)
            {
                return null;
            }

            VerbProperties first = null;
            foreach (VerbProperties vp in th.def.Verbs)
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
    }
}
