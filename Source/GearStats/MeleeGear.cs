using System.Linq;
using RimWorld;
using Verse;

namespace GearStats
{
    internal class MeleeGear : GearItem
    {
        public float CeCounterParry;

        public MeleeGear(bool ce) : base(ce)
        {
        }

        protected override void FillCore(Thing th)
        {
            base.FillCore(th);

            if (Ce)
            {
                ArmorPenetration = th.GetStatValue(StatDef.Named("MeleePenetrationFactor"));
                CeCounterParry = th.GetStatValue(StatDef.Named("MeleeCounterParryBonus"));
            }

            // Show the strongest tool of the weapon (highest damage per second).
            Tool best = null;
            if (th.def?.tools != null)
            {
                foreach (Tool tool in th.def.tools)
                {
                    if (tool.cooldownTime > 0f && (best == null || tool.power / tool.cooldownTime > best.power / best.cooldownTime))
                    {
                        best = tool;
                    }
                }
            }

            if (best != null)
            {
                Cooldown = best.cooldownTime;
                Damage = best.power;
                if (best.capacities != null)
                {
                    foreach (ToolCapacityDef capacity in best.capacities)
                    {
                        DamageType = capacity.label + " (" + best.label + ")";
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
    }
}
