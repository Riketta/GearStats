using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GearStats
{
    /// <summary>Textures are looked up once and cached; missing ones never spam the log.</summary>
    internal static class Icons
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string path)
        {
            if (!Cache.TryGetValue(path, out Texture2D texture))
            {
                texture = ContentFinder<Texture2D>.Get(path, false);
                Cache[path] = texture;
            }

            return texture;
        }
    }
}
