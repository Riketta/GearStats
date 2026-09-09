using RimWorld;
using Verse;

namespace GearStats
{
    internal class ApparelGear : GearItem
    {
        public float ArmorBlunt;
        public float ArmorSharp;
        public float ArmorHeat;

        /// <summary>Insulation converted to the player's temperature unit at fill time.</summary>
        public float InsulationCold;
        public float InsulationHeat;
        public string TempUnit = "";

        public ApparelGear(bool ce) : base(ce)
        {
        }

        protected override void FillCore(Thing th)
        {
            base.FillCore(th);
            ArmorBlunt = th.GetStatValue(StatDefOf.ArmorRating_Blunt);
            ArmorSharp = th.GetStatValue(StatDefOf.ArmorRating_Sharp);
            ArmorHeat = th.GetStatValue(StatDefOf.ArmorRating_Heat);

            (string unit, float coeff) = Temperature();
            TempUnit = unit;
            InsulationCold = th.GetStatValue(StatDefOf.Insulation_Cold) * coeff;
            InsulationHeat = th.GetStatValue(StatDefOf.Insulation_Heat) * coeff;
        }
    }
}
