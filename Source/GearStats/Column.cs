using System;
using Verse;

namespace GearStats
{
    /// <summary>
    /// One table column: header (icon or text), cell renderer and sort keys. Column
    /// lists are shared by the header and row renderers, so they can never diverge.
    /// </summary>
    internal sealed class Column
    {
        /// <summary>Sort identity; kept stable across accuracy brackets so the sorted
        /// column survives a bracket switch.</summary>
        public string Id;

        /// <summary>Suffix of the "UI/Icons/Wsh_" header texture; defaults to Id.</summary>
        public string Icon;

        public string HeaderKey;
        public float Width;

        /// <summary>Draw the header as text instead of an icon (not clickable).</summary>
        public bool TextHeader;

        public Func<GearItem, string> Cell;
        public Func<GearItem, float> FloatKey;
        public Func<GearItem, string> StringKey;

        public bool Sortable => FloatKey != null || StringKey != null;

        public static Column Num(string id, float width, string headerKey,
            Func<GearItem, float> value, Func<float, string> format)
        {
            return new Column
            {
                Id = id,
                Width = width,
                HeaderKey = headerKey,
                Cell = it => format(value(it)),
                FloatKey = value
            };
        }
    }
}
