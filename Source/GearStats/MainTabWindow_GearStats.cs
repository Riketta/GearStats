using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GearStats
{
    public class MainTabWindow_GearStats : MainTabWindow
    {
        private const float RowHeight = 30f;
        private const float HeaderRowHeight = 30f;
        private const float LeadWidth = 51f; // owner icon (20) + gap (2) + thing icon (29)
        private const float IconSize = 29f;

        private readonly bool ce = ModLister.HasActiveModWithName("Combat Extended");

        private AccuracyBracket accBracket = AccuracyBracket.All;

        private bool showGround = true;
        private bool showColonists = true;
        private bool showPrisoners;
        private bool showHostiles;
        private bool showFriendlies;
        private bool showCorpses;
        private bool showCraftable;
        private bool showStorage;

        private GearKind curTab = GearKind.Ranged;

        private readonly Dictionary<GearKind, List<GearItem>> gear = new Dictionary<GearKind, List<GearItem>>();
        private readonly Dictionary<GearKind, (string id, bool ascending)> sorts = new Dictionary<GearKind, (string, bool)>();

        /// <summary>Reusable stat prototypes for craftable items, keyed by produced def.</summary>
        private readonly Dictionary<ThingDef, Thing> protoCache = new Dictionary<ThingDef, Thing>();

        private bool isDirty = true;
        private int listUpdateNext;
        private Vector2 scrollPosition;

        public MainTabWindow_GearStats()
        {
            doCloseX = true;
            closeOnClickedOutside = true;
            foreach (GearKind kind in Enum.GetValues(typeof(GearKind)))
            {
                gear[kind] = new List<GearItem>();
                sorts[kind] = ("marketValue", false);
            }
        }

        public override Vector2 RequestedTabSize => new Vector2(1200f, 684f);

        public override void PreOpen()
        {
            base.PreOpen();
            isDirty = true;
        }

        public override void DoWindowContents(Rect rect)
        {
            rect.yMin += 35f;

            if (listUpdateNext < Find.TickManager.TicksGame)
            {
                isDirty = true;
            }

            if (isDirty)
            {
                Refresh();
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            TabDrawer.DrawTabs(rect, TabRecords());
            rect.y += 5f;

            DrawFilters(rect.y, rect.x);
            rect.y += 35f;

            DoPage(rect, curTab);
        }

        // --- layout -------------------------------------------------------------

        private List<TabRecord> TabRecords()
        {
            return new List<TabRecord>
            {
                new TabRecord("GearStats.Ranged".Translate(), () => curTab = GearKind.Ranged, curTab == GearKind.Ranged),
                new TabRecord("GearStats.Melee".Translate(), () => curTab = GearKind.Melee, curTab == GearKind.Melee),
                new TabRecord("GearStats.Grenades".Translate(), () => curTab = GearKind.Grenades, curTab == GearKind.Grenades),
                new TabRecord("GearStats.Apparel".Translate(), () => curTab = GearKind.Apparel, curTab == GearKind.Apparel),
                new TabRecord("GearStats.Turrets".Translate(), () => curTab = GearKind.Turrets, curTab == GearKind.Turrets)
            };
        }

        private void DrawFilters(float y, float startX)
        {
            float x = startX;
            if (curTab != GearKind.Turrets)
            {
                FilterCheckbox("GearStats.Ground".Translate(), ref showGround, ref x, y);
                FilterCheckbox("GearStats.Colonists".Translate(), ref showColonists, ref x, y);
                FilterCheckbox("GearStats.Prisoners".Translate(), ref showPrisoners, ref x, y);
                FilterCheckbox("GearStats.Hostiles".Translate(), ref showHostiles, ref x, y);
                FilterCheckbox("GearStats.Friendlies".Translate(), ref showFriendlies, ref x, y);
                FilterCheckbox("GearStats.Corpses".Translate(), ref showCorpses, ref x, y);
                FilterCheckbox("GearStats.Storage".Translate(), ref showStorage, ref x, y);
            }

            FilterCheckbox("GearStats.Craftable".Translate(), ref showCraftable, ref x, y);
        }

        private void FilterCheckbox(string label, ref bool value, ref float x, float y)
        {
            float width = Text.CalcSize(label).x + 25f;
            bool old = value;
            Widgets.CheckboxLabeled(new Rect(x, y, width, 30f), label, ref value);
            if (old != value)
            {
                isDirty = true;
            }

            x += width + 25f;
        }

        private void DoPage(Rect rect, GearKind kind)
        {
            if (!ce && (kind == GearKind.Ranged || kind == GearKind.Turrets))
            {
                DrawAccuracyRadios(rect);
                rect.y += 35f;
            }

            List<Column> columns = GearColumns.For(kind, ce, accBracket);
            List<GearItem> items = gear[kind];

            GUI.BeginGroup(rect);

            DrawHeaderRow(rect.width, kind, columns);

            float tableHeight = items.Count * RowHeight;
            var contentRect = new Rect(0f, HeaderRowHeight, rect.width - 16f, tableHeight + 40f);
            var scrollRect = new Rect(0f, HeaderRowHeight, rect.width, rect.height - HeaderRowHeight);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, contentRect);
            for (int i = 0; i < items.Count; i++)
            {
                DrawRow(items[i], i, contentRect.width, columns);
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawAccuracyRadios(Rect rect)
        {
            float x = rect.x;
            string label = "GearStats.ColRange".Translate() + ": ";
            float labelWidth = Text.CalcSize(label).x;
            Widgets.Label(new Rect(x, rect.y + 3f, labelWidth, 30f), label);
            x += labelWidth;

            foreach (AccuracyBracket bracket in Enum.GetValues(typeof(AccuracyBracket)))
            {
                string text = ("GearStats.AccRbName." + bracket).Translate();
                float width = Text.CalcSize(text).x + 25f;
                if (Widgets.RadioButtonLabeled(new Rect(x, rect.y, width, 30f), text, accBracket == bracket))
                {
                    accBracket = bracket;
                }

                x += width + 25f;
            }
        }

        private void DrawHeaderRow(float width, GearKind kind, List<Column> columns)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, RowHeight, width);
            GUI.color = Color.white;

            (string sortId, bool ascending) = sorts[kind];

            float x = LeadWidth;
            foreach (Column col in columns)
            {
                var rect = new Rect(x, 2f, col.TextHeader ? col.Width : 25f, 25f);
                if (col.TextHeader)
                {
                    string header = col.HeaderKey.Translate();
                    if (!header.NullOrEmpty())
                    {
                        Widgets.Label(rect, header);
                    }
                }
                else
                {
                    Texture2D icon = Icons.Get("UI/Icons/Wsh_" + col.Id);
                    if (icon != null)
                    {
                        GUI.DrawTexture(rect, icon);
                    }

                    TooltipHandler.TipRegion(rect, col.HeaderKey.Translate());
                    if (Mouse.IsOver(rect))
                    {
                        GUI.DrawTexture(rect, TexUI.HighlightTex);
                    }

                    if (col.Sortable && Widgets.ButtonInvisible(rect))
                    {
                        sortId = col.Id;
                        ascending = !ascending;
                        sorts[kind] = (sortId, ascending);
                        SortTab(kind);
                    }

                    if (col.Id == sortId)
                    {
                        Texture2D arrow = Icons.Get(ascending ? "UI/Icons/Sorting" : "UI/Icons/SortingDescending");
                        if (arrow != null)
                        {
                            GUI.DrawTexture(new Rect(rect.xMax - arrow.width - 30f, rect.yMax - arrow.height - 1f,
                                arrow.width, arrow.height), arrow);
                        }
                    }
                }

                x += col.Width;
            }
        }

        private void DrawRow(GearItem item, int num, float width, List<Column> columns)
        {
            float y = RowHeight * num;

            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, y + RowHeight, width);
            GUI.color = Color.white;

            var rowRect = new Rect(0f, y, width, RowHeight);
            if (Mouse.IsOver(rowRect))
            {
                GUI.DrawTexture(rowRect, TexUI.HighlightTex);
            }

            DrawLeadButtons(item, num, width);

            float x = LeadWidth;
            foreach (Column col in columns)
            {
                Widgets.Label(new Rect(x, y + 3f, col.Width, RowHeight - 3f), col.Cell(item));
                x += col.Width;
            }
        }

        private void DrawLeadButtons(GearItem item, int num, float width)
        {
            float y = RowHeight * num;

            if (Prefs.DevMode)
            {
                if (Widgets.ButtonText(new Rect(width - 20f, y, 20f, RowHeight), "D"))
                {
                    Find.WindowStack.Add(new Dialog_GearDebug(item));
                }
            }
            else
            {
                Widgets.InfoCardButton(width - 20f, y, item.Thing);
            }

            if (item.Craftable)
            {
                DrawLeadIcon(y, "UI/Icons/Craftable");
                if (item.CraftPos != null)
                {
                    TooltipHandler.TipRegion(new Rect(0f, y, LeadWidth, RowHeight), item.CraftPos.def.label);
                    if (Widgets.ButtonInvisible(new Rect(20f, y, width - 20f, RowHeight)))
                    {
                        CameraJumper.TryJumpAndSelect(new RimWorld.Planet.GlobalTargetInfo(item.CraftPos));
                    }
                }
            }
            else if (item.InStorage)
            {
                DrawLeadIcon(y, "UI/Icons/Storage");
                if (Widgets.ButtonInvisible(new Rect(20f, y, width - 20f, RowHeight)))
                {
                    CameraJumper.TryJumpAndSelect(new RimWorld.Planet.GlobalTargetInfo(item.StoragePos, Find.CurrentMap));
                }
            }
            else
            {
                if (Widgets.ButtonInvisible(new Rect(20f, y, width - 20f, RowHeight)))
                {
                    CameraJumper.TryJumpAndSelect(new RimWorld.Planet.GlobalTargetInfo(item.Thing));
                }

                if (item.Owner != OwnerKind.None)
                {
                    DrawLeadIcon(y, "UI/Icons/" + item.Owner);
                    if (!item.OwnerName.NullOrEmpty())
                    {
                        TooltipHandler.TipRegion(new Rect(0f, y, LeadWidth, RowHeight), item.OwnerName);
                    }
                }
            }
        }

        private static void DrawLeadIcon(float y, string iconPath)
        {
            Texture2D icon = Icons.Get(iconPath);
            if (icon == null)
            {
                return;
            }

            var rect = new Rect(0f, y + (RowHeight - icon.height) / 2f, icon.width, icon.height);
            GUI.DrawTexture(rect, icon);
        }

        // --- data collection ----------------------------------------------------

        private void Refresh()
        {
            if (Prefs.DevMode)
            {
                Log.Message("[GearStats] Refreshing gear list");
            }

            foreach (GearKind kind in gear.Keys)
            {
                gear[kind].Clear();
            }

            Map map = Find.CurrentMap;
            if (map != null)
            {
                if (showGround)
                {
                    CollectGround(map);
                }

                if (showCorpses)
                {
                    CollectCorpses(map);
                }

                CollectPawns(map);

                if (showStorage)
                {
                    CollectModdedStorage(map);
                }

                if (showCraftable)
                {
                    CollectCraftable(map);
                }

                CollectBuiltTurrets(map);
            }

            SortTab(curTab);
            listUpdateNext = Find.TickManager.TicksGame + GenTicks.TickRareInterval;
            isDirty = false;
        }

        private static GearKind? GetKind(Thing th)
        {
            ThingDef def = th.def;
            if (def.IsApparel)
            {
                return GearKind.Apparel;
            }

            if (def.IsRangedWeapon)
            {
                bool grenade = def.thingCategories != null && def.thingCategories.Any(c => c.defName == "Grenades");
                return grenade ? GearKind.Grenades : GearKind.Ranged;
            }

            if (def.IsMeleeWeapon && !def.IsStuff && !def.IsIngestible && !def.IsDrug)
            {
                return GearKind.Melee;
            }

            return null;
        }

        private void AddGear(Thing th, GearKind kind, Action<GearItem> setup = null)
        {
            GearItem item;
            switch (kind)
            {
                case GearKind.Ranged: item = new RangedGear(ce); break;
                case GearKind.Melee: item = new MeleeGear(ce); break;
                case GearKind.Grenades: item = new GrenadeGear(ce); break;
                case GearKind.Apparel: item = new ApparelGear(ce); break;
                case GearKind.Turrets: item = new TurretGear(ce); break;
                default: return;
            }

            item.Fill(th);
            setup?.Invoke(item);
            gear[kind].Add(item);
        }

        private Action<GearItem> OwnedBy(Pawn pawn, OwnerKind owner)
        {
            return it =>
            {
                it.Owner = owner;
                it.OwnerName = pawn.LabelShortCap;
            };
        }

        private void CollectGround(Map map)
        {
            foreach (Thing th in map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon))
            {
                if (!th.Position.Fogged(map) && GetKind(th) is GearKind kind)
                {
                    AddGear(th, kind);
                }
            }

            foreach (Thing th in map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel))
            {
                if (!th.Position.Fogged(map))
                {
                    AddGear(th, GearKind.Apparel);
                }
            }
        }

        private void CollectCorpses(Map map)
        {
            foreach (Thing th in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                Pawn pawn = (th as Corpse)?.InnerPawn;
                if (pawn == null || th.Position.Fogged(map))
                {
                    continue;
                }

                Action<GearItem> owned = OwnedBy(pawn, OwnerKind.Corpse);
                if (pawn.apparel != null)
                {
                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                    {
                        AddGear(apparel, GearKind.Apparel, owned);
                    }
                }

                // Corpses can keep weapons (e.g. with Keep Your Gear) - list them too.
                if (pawn.equipment != null)
                {
                    foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading)
                    {
                        if (GetKind(eq) is GearKind kind)
                        {
                            AddGear(eq, kind, owned);
                        }
                    }
                }
            }
        }

        private void CollectPawns(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.AnimalOrWildMan() || pawn.Position.Fogged(map))
                {
                    continue;
                }

                bool hostile = !pawn.IsColonist && pawn.HostileTo(Faction.OfPlayer);
                bool friendly = !pawn.IsColonist && !hostile;

                OwnerKind owner;
                if (pawn.IsColonist && showColonists) owner = OwnerKind.Colonist;
                else if (pawn.IsPrisonerOfColony && showPrisoners) owner = OwnerKind.Prisoner;
                else if (hostile && showHostiles) owner = OwnerKind.Hostile;
                else if (friendly && showFriendlies) owner = OwnerKind.Friendly;
                else continue;

                Action<GearItem> owned = OwnedBy(pawn, owner);

                if (pawn.equipment != null)
                {
                    foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading)
                    {
                        if (GetKind(eq) is GearKind kind)
                        {
                            AddGear(eq, kind, owned);
                        }
                    }
                }

                if (pawn.apparel != null)
                {
                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                    {
                        AddGear(apparel, GearKind.Apparel, owned);
                    }
                }

                if (pawn.inventory != null)
                {
                    foreach (Thing carried in pawn.inventory.innerContainer)
                    {
                        if (GetKind(carried) is GearKind kind)
                        {
                            AddGear(carried, kind, owned);
                        }
                    }
                }
            }
        }

        private void CollectModdedStorage(Map map)
        {
            foreach (Building_Storage storage in map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>())
            {
                Type type = storage.GetType();
                Action<GearItem> stored = it =>
                {
                    it.InStorage = true;
                    it.StoragePos = storage.Position;
                };

                // Optional mods are resolved by reflection only when they are loaded.
                if (type.FullName == "WeaponStorage.Building_WeaponStorage")
                {
                    var weapons = type.GetMethod("GetWeapons")?.Invoke(storage, new object[] { true })
                        as IEnumerable<ThingWithComps>;
                    if (weapons != null)
                    {
                        foreach (ThingWithComps weapon in weapons)
                        {
                            if (GetKind(weapon) is GearKind kind)
                            {
                                AddGear(weapon, kind, stored);
                            }
                        }
                    }
                }
                else if (type.FullName == "ChangeDresser.Building_Dresser")
                {
                    var apparels = type.GetProperty("Apparel")?.GetValue(storage) as IEnumerable<Apparel>;
                    if (apparels != null)
                    {
                        foreach (Apparel apparel in apparels)
                        {
                            AddGear(apparel, GearKind.Apparel, stored);
                        }
                    }
                }
            }
        }

        private void CollectCraftable(Map map)
        {
            var seenRecipes = new HashSet<ThingDef>();
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is not Building_WorkTable table)
                {
                    continue;
                }

                foreach (RecipeDef recipe in table.def.AllRecipes)
                {
                    ThingDef def = recipe.ProducedThingDef;
                    if (!recipe.AvailableNow || def == null || (!def.IsWeapon && !def.IsApparel))
                    {
                        continue;
                    }

                    if (!seenRecipes.Add(def))
                    {
                        continue;
                    }

                    Thing proto = PrototypeFor(def);
                    if (GetKind(proto) is GearKind kind)
                    {
                        AddGear(proto, kind, it =>
                        {
                            it.Craftable = true;
                            it.CraftPos = table;
                        });
                    }
                }
            }

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.building == null || !def.BuildableByPlayer || !def.IsResearchFinished)
                {
                    continue;
                }

                if (!def.building.IsTurret && !def.building.IsMortar)
                {
                    continue;
                }

                AddGear(PrototypeFor(def), GearKind.Turrets, it => it.Craftable = true);
            }
        }

        private void CollectBuiltTurrets(Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                BuildingProperties props = building.def.building;
                if (props != null && (props.IsTurret || props.IsMortar))
                {
                    AddGear(building, GearKind.Turrets);
                }
            }
        }

        private Thing PrototypeFor(ThingDef def)
        {
            if (!protoCache.TryGetValue(def, out Thing proto))
            {
                proto = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
                protoCache[def] = proto;
            }

            return proto;
        }

        private void SortTab(GearKind kind)
        {
            (string id, bool ascending) = sorts[kind];
            Column column = GearColumns.For(kind, ce, accBracket).Find(c => c.Id == id);
            if (column == null || !column.Sortable)
            {
                return;
            }

            List<GearItem> list = gear[kind];
            Comparison<GearItem> primary;
            if (column.FloatKey != null)
            {
                primary = ascending
                    ? (a, b) => column.FloatKey(a).CompareTo(column.FloatKey(b))
                    : (a, b) => column.FloatKey(b).CompareTo(column.FloatKey(a));
            }
            else
            {
                primary = ascending
                    ? (a, b) => string.CompareOrdinal(column.StringKey(a), column.StringKey(b))
                    : (a, b) => string.CompareOrdinal(column.StringKey(b), column.StringKey(a));
            }

            // Secondary key keeps rows with equal values in a deterministic order.
            list.Sort((a, b) =>
            {
                int result = primary(a, b);
                return result != 0 ? result : string.CompareOrdinal(a.Label, b.Label);
            });
        }
    }
}
