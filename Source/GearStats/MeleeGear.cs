using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GearStats
{
    internal class MeleeGear : GearItem
    {
        public float CeCounterParry;

        /// <summary>All melee verb/tool pairs of the weapon, like StatWorker_MeleeAverageDPS uses.</summary>
        private List<VerbUtility.VerbPropertiesWithSource> meleeVerbs = new List<VerbUtility.VerbPropertiesWithSource>();

        /// <summary>Strongest attack; drives the damage/cooldown cells.</summary>
        private VerbUtility.VerbPropertiesWithSource? bestPair;

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

            meleeVerbs = th.def?.Verbs == null || th.def.tools == null
                ? new List<VerbUtility.VerbPropertiesWithSource>()
                : VerbUtility.GetAllVerbProperties(th.def.Verbs, th.def.tools)
                    .Where(x => x.verbProps.IsMeleeAttack)
                    .ToList();

            PickBestAttack();

            if (bestPair != null)
            {
                // Quality and material multipliers included, like vanilla weapon stats.
                Damage = bestPair.Value.verbProps.AdjustedMeleeDamageAmount(bestPair.Value.tool, null, th.def, th.Stuff, null);
                Cooldown = bestPair.Value.verbProps.AdjustedCooldown(bestPair.Value.tool, null, th.def, th.Stuff);
                FillDamageType(bestPair.Value);
            }

            if (!Ce && meleeVerbs.Count > 0)
            {
                ArmorPenetration = meleeVerbs.AverageWeighted(
                    x => RawWeight(x),
                    x => x.verbProps.AdjustedArmorPenetration(x.tool, null, th.def, th.Stuff, null));
            }

            // Weighted average like StatWorker_MeleeAverageDPS - computed explicitly so
            // equipped weapons stay raw here instead of silently using their holder.
            if (meleeVerbs.Count > 0)
            {
                float damage = meleeVerbs.AverageWeighted(
                    x => RawWeight(x),
                    x => x.verbProps.AdjustedMeleeDamageAmount(x.tool, null, th.def, th.Stuff, null));
                float cooldown = meleeVerbs.AverageWeighted(
                    x => RawWeight(x),
                    x => x.verbProps.AdjustedCooldown(x.tool, null, th.def, th.Stuff));
                Dps = cooldown > 0f ? damage / cooldown : 0f;
            }
        }

        /// <summary>Vanilla shooter math: damage x life stage x MeleeDamageFactor, cooldown
        /// x MeleeCooldownFactor, armor penetration re-weighted, DPS = weighted damage /
        /// weighted cooldown (mirrors StatWorker_MeleeAverageDPS with an attacker), times
        /// the attacker's melee hit chance like the vanilla pawn stat StatWorker_MeleeDPS.</summary>
        protected override void AdjustForShooter(Pawn shooter)
        {
            if (bestPair != null)
            {
                Damage = bestPair.Value.verbProps.AdjustedMeleeDamageAmount(bestPair.Value.tool, shooter, Thing, null);
                Cooldown = bestPair.Value.verbProps.AdjustedCooldown(bestPair.Value.tool, shooter, Thing);
            }

            if (!Ce && meleeVerbs.Count > 0)
            {
                ArmorPenetration = meleeVerbs.AverageWeighted(
                    x => ShooterWeight(x, shooter),
                    x => x.verbProps.AdjustedArmorPenetration(x.tool, shooter, Thing, null));
            }

            if (meleeVerbs.Count > 0)
            {
                float damage = meleeVerbs.AverageWeighted(
                    x => ShooterWeight(x, shooter),
                    x => x.verbProps.AdjustedMeleeDamageAmount(x.tool, shooter, Thing, null));
                float cooldown = meleeVerbs.AverageWeighted(
                    x => ShooterWeight(x, shooter),
                    x => x.verbProps.AdjustedCooldown(x.tool, shooter, Thing));
                Dps = cooldown > 0f ? damage / cooldown : 0f;

                // StatWorker_MeleeDPS folds the attacker's melee hit chance into the pawn's
                // DPS; vanilla hides the stat when hit chance is disabled (e.g. some races),
                // in which case the raw DPS is shown.
                if (CombatStats.MeleeHitChance == null
                    || !CombatStats.MeleeHitChance.Worker.IsDisabledFor(shooter))
                {
                    Dps *= CombatStats.Value(shooter, CombatStats.MeleeHitChance);
                }
            }
        }

        private void PickBestAttack()
        {
            bestPair = null;
            float bestRating = -1f;
            foreach (VerbUtility.VerbPropertiesWithSource pair in meleeVerbs)
            {
                float damage = pair.tool?.power ?? pair.verbProps.meleeDamageBaseAmount;
                float cooldown = pair.tool?.cooldownTime ?? pair.verbProps.defaultCooldownTime;
                if (cooldown <= 0f)
                {
                    continue;
                }

                float rating = damage / cooldown;
                if (rating > bestRating)
                {
                    bestRating = rating;
                    bestPair = pair;
                }
            }
        }

        private void FillDamageType(VerbUtility.VerbPropertiesWithSource pair)
        {
            if (pair.tool?.capacities != null && pair.tool.capacities.Count > 0)
            {
                DamageType = pair.tool.capacities[0].label + " (" + pair.tool.label + ")";
            }
            else
            {
                DamageType = pair.verbProps.meleeDamageDef?.label ?? "";
            }
        }

        private float RawWeight(VerbUtility.VerbPropertiesWithSource x)
        {
            return x.verbProps.AdjustedMeleeSelectionWeight(x.tool, null, Thing.def, Thing.Stuff, null, false);
        }

        private float ShooterWeight(VerbUtility.VerbPropertiesWithSource x, Pawn shooter)
        {
            return x.verbProps.AdjustedMeleeSelectionWeight(x.tool, shooter, Thing, null, false);
        }
    }
}
