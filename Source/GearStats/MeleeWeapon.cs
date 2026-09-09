using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace GearStats
{
    public class MeleeWeapon : Weapon
    {
        /* MELEE:
         * th.def.statBases = MaxHitPoints, Flammability, DeteriorationRate, Beauty, SellPriceFactor, WorkToMake, Mass,
         * 	MeleeWeapon_DamageAmount, MeleeWeapon_Cooldown
         * th.def.weaponTags = [Melee]
         */

        private List<WeaponPartTool> tools;

        public float ceCounterParry { get; set; }

        public MeleeWeapon() : base()
        {
        }

        private float getDps()
        {
            return (float)Math.Round(this.damage / this.cooldown, 2);
        }

        public new void fillFromThing(Thing th, bool ce = false)
        {
            base.fillFromThing(th);
            tools = new List<WeaponPartTool>();
            try
            {
                ThingDef material = th.Stuff;
                if (material != null)
                {
                    this.label = material.label + " " + this.label;
                }

                float tmpCldwn = 1f;
                float tmpDmg = 0f;
                bool usethis = false;
                if (ce)
                {
                    this.armorPenetration = th.GetStatValue(StatDef.Named("MeleePenetrationFactor"));
                    ceCounterParry = th.GetStatValue(StatDef.Named("MeleeCounterParryBonus"));
                }

                if (th.def != null && th.def.tools != null)
                {
                    WeaponPartTool tmptool;
                    foreach (var tl in th.def.tools)
                    {
                        tmptool = new WeaponPartTool();
                        tmptool.fillFromTool(tl, ce);
                        this.tools.Add(tmptool);
                        usethis = false;
                        if (tmpDmg / tmpCldwn < tl.power / tl.cooldownTime)
                        {
                            this.cooldown = tl.cooldownTime;
                            this.damage = tl.power;
                            usethis = true;
                        }

                        if (usethis)
                        {
                            foreach (var tcd in tl.capacities)
                            {
                                this.damageType = tcd.label + " (" + tl.label + ")";
                            }
                        }
                    }

                    this.armorPenetration = VerbUtility.GetAllVerbProperties(th.def.Verbs, th.def.tools).Where((x => x.verbProps.IsMeleeAttack))
                        .AverageWeighted((x => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, (Pawn)null, th.def, material, null, false)), (x => x.verbProps.AdjustedArmorPenetration(x.tool, null, th.def, material, null)));
                }

                this.dps = (float)Math.Round(th.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS), 2);
            }
            catch (System.NullReferenceException e)
            {
                this.exceptions.Add(e);
            }
        }
    }
}