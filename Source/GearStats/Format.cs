using System.Globalization;
using Verse;

namespace GearStats
{
    internal enum AccuracyBracket
    {
        All,
        Touch,
        Short,
        Medium,
        Long
    }

    /// <summary>Culture-invariant number formatting for table cells.</summary>
    internal static class Format
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Num(float value, string format = "0.#") => value.ToString(format, Inv);

        public static string Hp(int percent) => percent + "%";

        public static string Pct1(float fraction) => Num(fraction * 100f) + "%";

        public static string F1(float value) => Num(value, "0.#");

        public static string F2(float value) => Num(value, "0.##");

        public static string F0(float value) => Num(value, "0");
    }

    /// <summary>
    /// Vanilla accuracy range brackets and rendering of the four brackets in one
    /// cell; brackets outside the weapon's range are shown as "-".
    /// </summary>
    internal static class AccuracyText
    {
        // Vanilla measurement distances, as used by VerbProperties.GetHitChanceFactor.
        public const float Touch = ShootTuning.DistTouch;
        public const float Short = ShootTuning.DistShort;
        public const float Medium = ShootTuning.DistMedium;
        public const float Long = ShootTuning.DistLong;

        public static string Cell(float minRange, float maxRange, float touch, float shot, float medium, float longR)
        {
            return Part(minRange, maxRange, Touch, touch, " /")
                   + Part(minRange, maxRange, Short, shot, " /")
                   + Part(minRange, maxRange, Medium, medium, " /")
                   + Part(minRange, maxRange, Long, longR, "");
        }

        private static string Part(float minRange, float maxRange, float bracket, float accuracy, string suffix)
        {
            if (minRange > bracket || maxRange < bracket)
            {
                return "-" + suffix;
            }

            return " " + Format.F1(accuracy) + suffix;
        }
    }
}
