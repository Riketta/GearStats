using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GearStats
{
    /// <summary>Static column sets per tab. Ids match the shipped "UI/Icons/Wsh_" textures.</summary>
    internal static class GearColumns
    {
        public const float StatWidth = 60f;
        public const float LabelWidth = 200f;
        public const float AccuracyWidth = 120f;
        public const float DamageTypeWidth = 180f;

        private const string Key = "GearStats.Col";

        private static readonly Dictionary<(GearKind kind, bool ce, AccuracyBracket bracket), List<Column>> Cache =
            new Dictionary<(GearKind, bool, AccuracyBracket), List<Column>>();

        public static List<Column> For(GearKind kind, bool ce, AccuracyBracket bracket)
        {
            var cacheKey = (kind, ce, bracket);
            if (!Cache.TryGetValue(cacheKey, out List<Column> columns))
            {
                columns = Build(kind, ce, bracket);
                Cache[cacheKey] = columns;
            }

            return columns;
        }

        private static List<Column> Build(GearKind kind, bool ce, AccuracyBracket bracket)
        {
            switch (kind)
            {
                case GearKind.Ranged: return Ranged(ce, bracket);
                case GearKind.Melee: return Melee(ce);
                case GearKind.Grenades: return Grenades();
                case GearKind.Apparel: return Apparel();
                case GearKind.Turrets: return Turrets(ce, bracket);
                default: return new List<Column> { Label() };
            }
        }

        private static List<Column> Ranged(bool ce, AccuracyBracket bracket)
        {
            var cols = new List<Column> { Label(), Quality(), Hp() };
            if (ce)
            {
                cols.Add(Value());
                cols.Add(Num("maxRange", "GearStats.ColRange", it => it.MaxRange, Format.F1));
                cols.Add(Cooldown());
                cols.Add(Warmup());
                cols.Add(Num("ceSightsEfficiency", Key + "Sights", it => ((RangedGear)it).CeSightsEfficiency, Format.Pct1));
                cols.Add(Num("ceShotSpread", Key + "Spread", it => ((RangedGear)it).CeShotSpread, Format.F2));
                cols.Add(Num("ceSwayFactor", Key + "Sway", it => ((RangedGear)it).CeSwayFactor, Format.F2));
                cols.Add(Num("ceMagazineCapacity", Key + "MagazineCapacity", it => ((RangedGear)it).CeMagazineCapacity, Format.F1));
            }
            else
            {
                cols.Add(Dps());
                cols.Add(Dpsa(bracket));
                cols.Add(Value());
                cols.Add(Damage());
                cols.Add(ArmorPenetration());
                cols.Add(Num("maxRange", "GearStats.ColRange", it => it.MaxRange, Format.F1));
                cols.Add(Cooldown());
                cols.Add(Warmup());
                cols.Add(Accuracy(bracket));
                cols.Add(DamageType());
            }

            return cols;
        }

        private static List<Column> Melee(bool ce)
        {
            var cols = new List<Column> { Label(), Quality(), Hp(), Dps(), Value(), Damage(), ArmorPenetration(), Cooldown() };
            if (ce)
            {
                cols.Add(Num("ceCounterParry", Key + "CounterParry", it => ((MeleeGear)it).CeCounterParry, Format.F2));
            }

            cols.Add(DamageType());
            return cols;
        }

        private static List<Column> Grenades()
        {
            return new List<Column>
            {
                Label(),
                Quality(),
                Hp(),
                Value(),
                Damage(),
                ArmorPenetration(),
                Num("maxRange", "GearStats.ColRange", it => it.MaxRange, Format.F1),
                Cooldown(),
                Warmup(),
                Num("explosionRadius", Key + "Radius", it => ((GrenadeGear)it).ExplosionRadius, Format.F1),
                Num("explosionDelay", Key + "Delay", it => ((GrenadeGear)it).ExplosionDelay, Format.F0),
                DamageType()
            };
        }

        private static List<Column> Apparel()
        {
            return new List<Column>
            {
                Label(),
                Quality(),
                Hp(),
                Value(),
                Num("armorBlunt", Key + "Blunt", it => ((ApparelGear)it).ArmorBlunt, Format.Pct1),
                Num("armorSharp", Key + "Sharp", it => ((ApparelGear)it).ArmorSharp, Format.Pct1),
                Num("armorHeat", Key + "Heat", it => ((ApparelGear)it).ArmorHeat, Format.Pct1),
                new Column
                {
                    Id = "insulation",
                    Width = StatWidth,
                    HeaderKey = Key + "ICold",
                    Cell = it => Format.F1(((ApparelGear)it).InsulationCold) + ((ApparelGear)it).TempUnit,
                    FloatKey = it => ((ApparelGear)it).InsulationCold
                },
                new Column
                {
                    Id = "insulationh",
                    Width = StatWidth,
                    HeaderKey = Key + "IHeat",
                    Cell = it => Format.F1(((ApparelGear)it).InsulationHeat) + ((ApparelGear)it).TempUnit,
                    FloatKey = it => ((ApparelGear)it).InsulationHeat
                }
            };
        }

        private static List<Column> Turrets(bool ce, AccuracyBracket bracket)
        {
            var cols = new List<Column>
            {
                Label(),
                Hp(),
                Value(),
                Damage(),
                ArmorPenetration(),
                Num("maxRange", "GearStats.ColRange", it => it.MaxRange, Format.F1),
                Cooldown(),
                Warmup()
            };
            if (!ce)
            {
                cols.Add(AccuracyTurret(bracket));
                cols.Add(DamageType());
            }

            return cols;
        }

        // --- column factories ---------------------------------------------------

        private static Column Label()
        {
            return new Column
            {
                Id = "label",
                Width = LabelWidth,
                HeaderKey = Key + "Name",
                Cell = it => it.Label,
                StringKey = it => it.Label
            };
        }

        private static Column Quality()
        {
            return new Column
            {
                Id = "qualityNum",
                Width = StatWidth,
                HeaderKey = Key + "Quality",
                Cell = it => it.Quality.GetLabel(),
                FloatKey = it => (float)it.Quality
            };
        }

        private static Column Hp()
        {
            return Column.Num("hp", StatWidth, Key + "HP", it => it.HpPercent, v => Format.Hp((int)v));
        }

        private static Column Dps()
        {
            return Column.Num("dps", StatWidth, Key + "DPS", it => it.Dps, Format.F2);
        }

        private static Column Dpsa(AccuracyBracket bracket)
        {
            string id = bracket == AccuracyBracket.All ? "dpsa" : "dpsa" + bracket;
            return Column.Num(id, AccuracyWidth, Key + "DPSA", it => ((RangedGear)it).DpsaFor(bracket), Format.F1);
        }

        private static Column Value()
        {
            return Column.Num("marketValue", StatWidth, Key + "Value", it => it.MarketValue, Format.F1);
        }

        private static Column Damage()
        {
            return Column.Num("damage", StatWidth, Key + "Damage", it => it.Damage, Format.F2);
        }

        private static Column ArmorPenetration()
        {
            return Column.Num("armorPenetration", StatWidth, Key + "ArmorPenetration", it => it.ArmorPenetration, Format.Pct1);
        }

        private static Column Cooldown()
        {
            return Column.Num("cooldown", StatWidth, Key + "Cooldwn", it => it.Cooldown, Format.F2);
        }

        private static Column Warmup()
        {
            return Column.Num("warmup", StatWidth, Key + "Warmup", it => it.Warmup, Format.F2);
        }

        private static Column Accuracy(AccuracyBracket bracket)
        {
            if (bracket == AccuracyBracket.All)
            {
                // Average over the four brackets: a single icon, not sortable.
                return new Column
                {
                    Id = "accuracy",
                    Width = AccuracyWidth,
                    HeaderKey = Key + "Accuracy",
                    Cell = it => ((RangedGear)it).AccuracyCell()
                };
            }

            string id = "accuracy" + bracket;
            return Column.Num(id, AccuracyWidth, Key + "Accuracy", it => ((RangedGear)it).AccuracyFor(bracket), Format.F1);
        }

        private static Column AccuracyTurret(AccuracyBracket bracket)
        {
            if (bracket == AccuracyBracket.All)
            {
                return new Column
                {
                    Id = "accuracy",
                    Width = AccuracyWidth,
                    HeaderKey = Key + "Accuracy",
                    Cell = it => ((TurretGear)it).AccuracyCell()
                };
            }

            string id = "accuracy" + bracket;
            return Column.Num(id, AccuracyWidth, Key + "Accuracy", it => ((TurretGear)it).AccuracyFor(bracket), Format.F1);
        }

        private static Column DamageType()
        {
            return new Column
            {
                Id = "type",
                Width = DamageTypeWidth,
                HeaderKey = Key + "Type",
                TextHeader = true,
                Cell = it => it.DamageType
            };
        }

        private static Column Num(string id, string headerKey, System.Func<GearItem, float> value, System.Func<float, string> format)
        {
            return Column.Num(id, StatWidth, headerKey, value, format);
        }
    }
}
