using System;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace GearStats
{
    /// <summary>Dev-mode dump of the raw def data behind one table row.</summary>
    internal class Dialog_GearDebug : Window
    {
        private readonly Thing thing;
        private readonly GearItem item;

        public Dialog_GearDebug(GearItem item)
        {
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            this.item = item;
            thing = item?.Thing;
        }

        public override Vector2 InitialSize => new Vector2(700f, 700f);

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.x = UI.screenWidth - windowRect.width;
            windowRect.y = UI.screenHeight - 35f - windowRect.height;
        }

        public override void DoWindowContents(Rect rect)
        {
            rect.yMin += 35f;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            if (thing == null)
            {
                Widgets.Label(rect, "no thing");
                return;
            }

            GUI.BeginGroup(rect);
            Widgets.Label(new Rect(0f, 0f, 350f, 700f), LeftPane());
            Widgets.Label(new Rect(350f, 0f, 350f, 700f), RightPane());
            GUI.EndGroup();
        }

        private string LeftPane()
        {
            var sb = new StringBuilder();
            if (thing.Stuff != null)
            {
                sb.Append("Stuff: ").Append(thing.Stuff.label).AppendLine();
            }

            if (thing.def.thingCategories != null)
            {
                sb.AppendLine("--- thingCategories ---");
                foreach (ThingCategoryDef category in thing.def.thingCategories)
                {
                    sb.AppendLine(category.defName);
                }
            }

            if (thing.def.statBases != null)
            {
                sb.AppendLine("--- statBases ---");
                foreach (StatModifier stat in thing.def.statBases)
                {
                    sb.Append(stat.stat).Append(": ").Append(thing.GetStatValue(stat.stat)).AppendLine();
                }
            }

            if (thing.def.weaponTags != null)
            {
                sb.AppendLine("--- weaponTags ---");
                foreach (string tag in thing.def.weaponTags)
                {
                    sb.AppendLine(tag);
                }
            }

            if (thing.def.Verbs != null)
            {
                sb.AppendLine("--- verbs ---");
                ProjectileProperties projectile = null;
                foreach (VerbProperties verb in thing.def.Verbs)
                {
                    sb.Append(verb.isPrimary ? "P" : "").Append("+ ").Append(verb.verbClass)
                        .Append(" (").Append(verb.category).Append(")").AppendLine();
                    sb.Append("++ ").Append(verb).Append(": ").Append(verb.label).AppendLine();
                    if (verb.defaultProjectile?.projectile != null)
                    {
                        projectile = verb.defaultProjectile.projectile;
                    }
                }

                if (projectile != null)
                {
                    sb.AppendLine("--- projectile ---");
                    sb.Append("damageDef: ").Append(projectile.damageDef.label).AppendLine();
                    sb.Append("damageAmountBase: ").Append(projectile.GetDamageAmount(thing)).AppendLine();
                    sb.Append("speed: ").Append(projectile.speed).AppendLine();
                    sb.Append("explosionRadius: ").Append(projectile.explosionRadius).AppendLine();
                }
            }

            sb.AppendLine("--- exceptions ---");
            foreach (Exception e in item.Exceptions)
            {
                sb.AppendLine(e.Message);
                sb.AppendLine(e.StackTrace);
                sb.AppendLine("------");
            }

            sb.AppendLine("--- stats ---");
            sb.AppendLine("MeleeDPS: " + thing.GetStatValue(StatDefOf.MeleeDPS));
            sb.AppendLine("AverageDPS: " + thing.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS));
            sb.AppendLine("CooldownMultiplier: " + thing.GetStatValue(StatDefOf.MeleeWeapon_CooldownMultiplier));
            sb.AppendLine("RngCooldown: " + thing.GetStatValue(StatDefOf.RangedWeapon_Cooldown));
            return sb.ToString();
        }

        private string RightPane()
        {
            var sb = new StringBuilder();
            if (thing.def.tools != null)
            {
                sb.AppendLine("--- tools ---");
                foreach (Tool tool in thing.def.tools)
                {
                    sb.Append(tool.label).AppendLine(": ");
                    sb.Append("power: ").Append(tool.power).AppendLine();
                    sb.Append("cooldown: ").Append(tool.cooldownTime).AppendLine();
                    sb.Append("armor pen.: ").Append(tool.armorPenetration).AppendLine();
                    sb.Append("capacities: ");
                    foreach (ToolCapacityDef capacity in tool.capacities)
                    {
                        sb.Append(capacity.label).Append(", ");
                    }

                    sb.AppendLine().AppendLine("---");
                }
            }

            return sb.ToString();
        }
    }
}
