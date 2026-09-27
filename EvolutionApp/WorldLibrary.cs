using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Evolution
{
    /// <summary>
    /// Built-in world library.
    /// Primary = harder Terrain-A..D (curriculum / Pref / Teacher / --eval).
    /// All = Terrain + Canvas-A..D (kept for A/B comparison).
    /// </summary>
    public static class WorldLibrary
    {
        public static IReadOnlyList<World> Primary { get; private set; }
        public static IReadOnlyList<World> Canvas { get; private set; }
        public static IReadOnlyList<World> All { get; private set; }

        static WorldLibrary()
        {
            Primary = CanvasWorldGenerator.GenerateFourTerrain();
            Canvas = CanvasWorldGenerator.GenerateFour();
            var all = new List<World>();
            all.AddRange(Primary);
            all.AddRange(Canvas);
            All = all;
            TrySavePreviews(all);
        }

        public static World GetByName(string name)
        {
            return All.FirstOrDefault(w => w.Name == name) ?? Primary[0];
        }

        
        /// <summary>
        /// Generate N procedural terrains (Terrain-01..) without mutating Primary A-D.
        /// Caller may merge with Primary for expanded LOO. Does not replace static Primary/All.
        /// </summary>
        public static IReadOnlyList<World> GenerateProcedural(int count, int baseSeed = 9001)
        {
            return CanvasWorldGenerator.GenerateProceduralTerrains(count, baseSeed);
        }

        /// <summary>Primary A-D plus procedural Terrain-01..N (new list; A-D preserved).</summary>
        public static IReadOnlyList<World> PrimaryPlusProcedural(int proceduralCount, int baseSeed = 9001)
        {
            var list = new List<World>();
            list.AddRange(Primary);
            list.AddRange(GenerateProcedural(proceduralCount, baseSeed));
            return list;
        }
static void TrySavePreviews(IReadOnlyList<World> worlds)
        {
            try
            {
                string dir = ResolveWorldsDirectory();
                if (string.IsNullOrEmpty(dir)) return;
                Directory.CreateDirectory(dir);
                for (int i = 0; i < worlds.Count; i++)
                {
                    var w = worlds[i];
                    CanvasWorldGenerator.SaveColorPreview(w, Path.Combine(dir, w.Name + ".png"));
                    CanvasWorldGenerator.SaveRoughnessCsv(w, Path.Combine(dir, w.Name + ".csv"));
                }
            }
            catch { }
        }

        static string ResolveWorldsDirectory()
        {
            var candidates = new List<string>();
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                candidates.Add(Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\worlds")));
                candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "worlds")));
            }
            catch { }
            foreach (var c in candidates)
            {
                try
                {
                    string parent = Path.GetDirectoryName(c);
                    if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                        return c;
                }
                catch { }
            }
            return candidates.Count > 0 ? candidates[0] : null;
        }
    }
}
