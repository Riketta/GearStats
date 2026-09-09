using RimWorld;
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

            if (MinRange <= AccuracyText.Touch && MaxRange >= AccuracyText.Touch)
                AccuracyTouch = Round(gunDef.GetStatValueAbstract(StatDefOf.AccuracyTouch) * 100f);
            if (MinRange <= AccuracyText.Short && MaxRange >= AccuracyText.Short)
                AccuracyShort = Round(gunDef.GetStatValueAbstract(StatDefOf.AccuracyShort) * 100f);
            if (MinRange <= AccuracyText.Medium && MaxRange >= AccuracyText.Medium)
                AccuracyMedium = Round(gunDef.GetStatValueAbstract(StatDefOf.AccuracyMedium) * 100f);
            if (MinRange <= AccuracyText.Long && MaxRange >= AccuracyText.Long)
                AccuracyLong = Round(gunDef.GetStatValueAbstract(StatDefOf.AccuracyLong) * 100f);

            Cooldown = gunDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
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
