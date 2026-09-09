using System.Linq;
using RimWorld;
using Verse;

namespace GearStats
{
    internal class MeleeGear : GearItem
    {
        public float CeCounterParry;

        private Tool bestTool;
        private VerbProperties meleeVerb;

        public MeleeGear(bool ce) : base(ce)
        {
        }

        protected override void FillCore(Thing th)
        {
            base.FillCore(th);

            meleeVerb = th.def?.Verbs?.FirstOrDefault(v => v.IsMeleeAttack);

            if (Ce)
            {
                ArmorPenetration = th.GetStatValue(StatDef.Named("MeleePenetrationFactor"));
                CeCounterParry = th.GetStatValue(StatDef.Named("MeleeCounterParryBonus"));
            }

            // Show the strongest tool of the weapon (highest damage per second).
            bestTool = null;
            if (th.def?.tools != null)
            {
                foreach (Tool tool in th.def.tools)
                {
                    if (tool.cooldownTime > 0f && (bestTool == null || tool.power / tool.cooldownTime > bestTool.power / bestTool.cooldownTime))
                    {
                        bestTool = tool;
                    }
                }
            }

            if (bestTool != null)
            {
                Cooldown = bestTool.cooldownTime;
                Damage = bestTool.power;
                if (bestTool.capacities != null)
                {
                    foreach (ToolCapacityDef capacity in bestTool.capacities)
                    {
                        DamageType = capacity.label + " (" + bestTool.label + ")";
                    }
                }
            }

            // Vanilla weapons: quality-weighted average over the melee verbs. In CE the
            // dedicated stat above is authoritative.
            if (!Ce && th.def?.Verbs != null && th.def.tools != null)
            {
                var meleeVerbs = VerbUtility.GetAllVerbProperties(th.def.Verbs, th.def.tools)
                    .Where(x => x.verbProps.IsMeleeAttack)
                    .ToList();
                if (meleeVerbs.Count > 0)
                {
                    ArmorPenetration = meleeVerbs.AverageWeighted(
                        x => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, null, th.def, th.Stuff, null, false),
                        x => x.verbProps.AdjustedArmorPenetration(x.tool, null, th.def, th.Stuff, null));
                }
            }

            Dps = th.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
        }

        /// <summary>Vanilla AdjustedMeleeDamageAmount/AdjustedCooldown math: damage scales
        /// with the shooter's life stage and MeleeDamageFactor (genes, traits), cooldown
        /// with MeleeCooldownFactor; armor penetration is re-weighted with the shooter.</summary>
        protected override void AdjustForShooter(Pawn shooter)
        {
            if (bestTool != null)
            {
                Damage = bestTool.AdjustedBaseMeleeDamageAmount(Thing, meleeVerb?.meleeDamageDef)
                    * shooter.ageTracker.CurLifeStage.meleeDamageFactor
                    * shooter.GetStatValue(StatDefOf.MeleeDamageFactor);
                Cooldown = bestTool.AdjustedCooldown(Thing) * shooter.GetStatValue(StatDefOf.MeleeCooldownFactor);
            }

            if (!Ce && Thing.def?.Verbs != null && Thing.def.tools != null)
            {
                var meleeVerbs = VerbUtility.GetAllVerbProperties(Thing.def.Verbs, Thing.def.tools)
                    .Where(x => x.verbProps.IsMeleeAttack)
                    .ToList();
                if (meleeVerbs.Count > 0)
                {
                    ArmorPenetration = meleeVerbs.AverageWeighted(
                        x => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, shooter, Thing.def, Thing.Stuff, null, false),
                        x => x.verbProps.AdjustedArmorPenetration(x.tool, shooter, Thing, null));
                }
            }

            Dps = Cooldown > 0f ? Damage / Cooldown : 0f;
        }
    }
}
