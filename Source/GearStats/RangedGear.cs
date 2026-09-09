using System;
using RimWorld;
using Verse;

namespace GearStats
{
    internal class RangedGear : GearItem
    {
        public float MinRange;
        public float AccuracyTouch;
        public float AccuracyShort;
        public float AccuracyMedium;
        public float AccuracyLong;
        public float DpsaTouch;
        public float DpsaShort;
        public float DpsaMedium;
        public float DpsaLong;
        public float Dpsa { get; private set; }
        public float CeSightsEfficiency;
        public float CeShotSpread;
        public float CeSwayFactor;
        public float CeMagazineCapacity;
        public int BurstShotCount = 1;
        public int TicksBetweenBurstShots;

        public RangedGear(bool ce) : base(ce)
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

        public float DpsaFor(AccuracyBracket bracket)
        {
            switch (bracket)
            {
                case AccuracyBracket.Touch: return DpsaTouch;
                case AccuracyBracket.Short: return DpsaShort;
                case AccuracyBracket.Medium: return DpsaMedium;
                case AccuracyBracket.Long: return DpsaLong;
                default: return Dpsa;
            }
        }

        public string AccuracyCell()
        {
            return AccuracyText.Cell(MinRange, MaxRange, AccuracyTouch, AccuracyShort, AccuracyMedium, AccuracyLong);
        }

        protected override void FillCore(Thing th)
        {
            base.FillCore(th);
            Cooldown = th.GetStatValue(StatDefOf.RangedWeapon_Cooldown);

            if (Ce)
            {
                FillCe(th);
            }
            else
            {
                FillVanilla(th);
            }

            ComputeDps();
            ComputeDpsa();
        }

        private void FillVanilla(Thing th)
        {
            VerbProperties verb = VerbWithProjectile(th);
            if (verb == null)
            {
                return;
            }

            Warmup = verb.warmupTime;
            MaxRange = verb.range;
            MinRange = verb.minRange;
            BurstShotCount = verb.burstShotCount > 0 ? verb.burstShotCount : 1;
            TicksBetweenBurstShots = verb.ticksBetweenBurstShots;
            if (verb.defaultProjectile?.projectile != null)
            {
                Damage = verb.defaultProjectile.projectile.GetDamageAmount(th);
                DamageType = verb.defaultProjectile.projectile.damageDef.label;
                ArmorPenetration = verb.defaultProjectile.projectile.GetArmorPenetration(th);
            }

            FillAccuracy(th.GetStatValue(StatDefOf.AccuracyTouch),
                th.GetStatValue(StatDefOf.AccuracyShort),
                th.GetStatValue(StatDefOf.AccuracyMedium),
                th.GetStatValue(StatDefOf.AccuracyLong));
        }

        private void FillCe(Thing th)
        {
            CeSightsEfficiency = th.GetStatValue(StatDef.Named("SightsEfficiency"));
            CeShotSpread = th.GetStatValue(StatDef.Named("ShotSpread"));
            CeSwayFactor = th.GetStatValue(StatDef.Named("SwayFactor"));
            CeMagazineCapacity = th.GetStatValue(StatDef.Named("MagazineCapacity"));

            foreach (VerbProperties vp in th.def.Verbs)
            {
                string verbClass = vp.verbClass?.FullName;
                if (verbClass == "CombatExtended.Verb_ShootCE" || verbClass == "CombatExtended.Verb_ShootCEOneUse")
                {
                    Warmup = vp.warmupTime;
                    MaxRange = vp.range;
                    break;
                }
            }

            // CE verbs carry no vanilla range gate; MinRange stays 0, so only max range matters.
            FillAccuracy(th.GetStatValue(StatDefOf.AccuracyTouch),
                th.GetStatValue(StatDefOf.AccuracyShort),
                th.GetStatValue(StatDefOf.AccuracyMedium),
                th.GetStatValue(StatDefOf.AccuracyLong));
        }

        private void FillAccuracy(float touch, float shot, float medium, float longR)
        {
            if (MinRange <= AccuracyText.Touch && MaxRange >= AccuracyText.Touch)
                AccuracyTouch = Round(touch, 2);
            if (MinRange <= AccuracyText.Short && MaxRange >= AccuracyText.Short)
                AccuracyShort = Round(shot, 2);
            if (MinRange <= AccuracyText.Medium && MaxRange >= AccuracyText.Medium)
                AccuracyMedium = Round(medium, 2);
            if (MinRange <= AccuracyText.Long && MaxRange >= AccuracyText.Long)
                AccuracyLong = Round(longR, 2);
        }

        private void ComputeDps()
        {
            float burstDamage = Damage * BurstShotCount;
            float burstTicks = (BurstShotCount - 1) * TicksBetweenBurstShots;
            float totalSeconds = Cooldown + Warmup + burstTicks / (float)TicksPerSecond;
            Dps = totalSeconds > 0f ? Round(burstDamage / totalSeconds, 2) : 0f;
        }

        private void ComputeDpsa()
        {
            DpsaTouch = Round(Dps * AccuracyTouch / 100f, 1);
            DpsaShort = Round(Dps * AccuracyShort / 100f, 1);
            DpsaMedium = Round(Dps * AccuracyMedium / 100f, 1);
            DpsaLong = Round(Dps * AccuracyLong / 100f, 1);

            float sum = 0f;
            int count = 0;
            foreach (float accuracy in new[] { AccuracyTouch, AccuracyShort, AccuracyMedium, AccuracyLong })
            {
                if (accuracy > 0f)
                {
                    sum += accuracy;
                    count++;
                }
            }

            Dpsa = count > 0 ? Round(Dps * sum / count / 100f, 1) : 0f;
        }

        /// <summary>The main shooting verb: the primary verb among those firing a projectile.</summary>
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

        private static float Round(float value, int digits) => (float)Math.Round(value, digits);
    }
}
