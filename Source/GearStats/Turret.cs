using System;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace GearStats
{
    public class Turret : Weapon
    {
        public int maxHp { get; set; }
        public float accuracyTouch { get; set; }

        public float accuracyShort { get; set; }

        public float accuracyMedium { get; set; }

        public float accuracyLong { get; set; }

        public float minRange { get; set; }

        public float maxRange { get; set; }
        
        public float warmup { get; set; }
        
        public int burstShotCount { get; set; }

        public int ticksBetweenBurstShots { get; set; }
        
        public string getAccuracyStr()
        {
            var sb = new StringBuilder();
            if (minRange > RNG_TOUCH || maxRange < RNG_TOUCH)
            {
                sb.Append(" - /");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyTouch, 1).ToString()).Append(" /");
            }

            if (minRange > RNG_SHORT || maxRange < RNG_SHORT)
            {
                sb.Append(" - /");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyShort, 1).ToString()).Append(" /");
            }

            if (minRange > RNG_MEDIUM || maxRange < RNG_MEDIUM)
            {
                sb.Append(" - /");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyMedium, 1).ToString()).Append(" /");
            }

            if (minRange > RNG_LONG || maxRange < RNG_LONG)
            {
                sb.Append(" -");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyLong, 1).ToString());
            }

            return sb.ToString();
        }

        public new void fillFromThing(Thing th, bool ce = false)
        {
            Log.Message(th.def.defName);
            base.fillFromThing(th);
            if (th.def.building.turretGunDef?.Verbs != null)
            {
                foreach (var vp in th.def.building.turretGunDef.Verbs.Where(vp => vp.ToString().StartsWith("VerbProperties")))
                {
                    warmup = vp.warmupTime;
                    maxRange = vp.range;
                    minRange = vp.minRange;
                    if (vp.defaultProjectile?.projectile != null)
                    {
                        damage = vp.defaultProjectile.projectile.GetDamageAmount(th);
                        damageType = vp.defaultProjectile.projectile.damageDef.label;
                        armorPenetration = vp.defaultProjectile.projectile.GetArmorPenetration(th);
                    }
                    else
                    {
                        damage = 0;
                        damageType = "";
                        armorPenetration = 0;
                    }

                    if (vp.burstShotCount > 0)
                    {
                        this.burstShotCount = vp.burstShotCount;
                    }
                    else
                    {
                        this.burstShotCount = 1;
                    }

                    this.ticksBetweenBurstShots = vp.ticksBetweenBurstShots;
                }
            }

            try
            {
                if (minRange <= RNG_TOUCH && maxRange >= RNG_TOUCH)
                {
                    accuracyTouch = (float) Math.Round(th.def.building.turretGunDef.GetStatValueAbstract(StatDefOf.AccuracyTouch) * 100, 2);
                }

                if (minRange <= RNG_SHORT && maxRange >= RNG_SHORT)
                {
                    accuracyShort = (float) Math.Round(th.def.building.turretGunDef.GetStatValueAbstract(StatDefOf.AccuracyShort) * 100, 2);
                }

                if (minRange <= RNG_MEDIUM && maxRange >= RNG_MEDIUM)
                {
                    accuracyMedium = (float) Math.Round(th.def.building.turretGunDef.GetStatValueAbstract(StatDefOf.AccuracyMedium) * 100, 2);
                }

                if (minRange <= RNG_LONG && maxRange >= RNG_LONG)
                {
                    accuracyLong = (float) Math.Round(th.def.building.turretGunDef.GetStatValueAbstract(StatDefOf.AccuracyLong) * 100, 2);
                }

                cooldown = th.def.building.turretGunDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
                mass = th.GetStatValue(StatDefOf.Mass);
                maxHp = (int)th.GetStatValue(StatDefOf.MaxHitPoints);
            }
            catch (System.NullReferenceException e)
            {
                this.exceptions.Add(e);
            }
        }
    }
}