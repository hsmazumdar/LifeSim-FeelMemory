using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Evolution
{
    /// <summary>
    /// Canvas terrain generator: stratified 4x4 white seeds, Jacobi Laplacian smoothing
    /// on an upsampled grid, block-mean downsample, farthest dark-cell Start/Goal.
    /// </summary>
    public static class CanvasWorldGenerator
    {
        public const int DefaultSize = 20;
        public const int DefaultBlocks = 4;
        public const int DefaultUpsample = 4;
        public const int DefaultSmoothIters = 80;
        public const int DefaultNormalizeEvery = 10;

        public static readonly int[] FourSeeds = { 101, 202, 303, 404 };
        public static readonly string[] FourNames = { "Canvas-A", "Canvas-B", "Canvas-C", "Canvas-D" };

        public static readonly int[] TerrainSeeds = { 101, 202, 303, 404 };
        public static readonly string[] TerrainNames = { "Terrain-A", "Terrain-B", "Terrain-C", "Terrain-D" };
        // Fix2-known start/goal geometry (same seeds); roughness regenerated via hard recipe.
        public static readonly Point[] TerrainStarts = {
            new Point(5, 19), new Point(0, 3), new Point(3, 0), new Point(17, 5)
        };
        public static readonly Point[] TerrainGoals = {
            new Point(13, 0), new Point(19, 17), new Point(16, 19), new Point(0, 13)
        };

        public static World Generate(
            string name,
            int seed,
            int size = DefaultSize,
            int blocks = DefaultBlocks,
            int upsample = DefaultUpsample,
            int smoothIters = DefaultSmoothIters,
            int normalizeEvery = DefaultNormalizeEvery)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (blocks <= 0 || size % blocks != 0)
                throw new ArgumentException("size must be divisible by blocks", nameof(blocks));
            if (upsample < 1) upsample = 1;

            int blockCells = size / blocks;
            int fine = size * upsample;
            var field = new float[fine, fine];
            var rng = new Random(seed);

            // Stratified seeds: one white (255) seed per block, mapped onto fine grid.
            var seedMask = new bool[fine, fine];
            for (int by = 0; by < blocks; by++)
            {
                for (int bx = 0; bx < blocks; bx++)
                {
                    int lx = rng.Next(blockCells);
                    int ly = rng.Next(blockCells);
                    int wx = bx * blockCells + lx;
                    int wy = by * blockCells + ly;
                    int fx = wx * upsample + upsample / 2;
                    int fy = wy * upsample + upsample / 2;
                    if (fx >= fine) fx = fine - 1;
                    if (fy >= fine) fy = fine - 1;
                    field[fx, fy] = 255f;
                    seedMask[fx, fy] = true;
                }
            }

            // Discrete 4-neighbor Laplacian Jacobi smoothing; keep seeds fixed as Dirichlet peaks.
            var next = new float[fine, fine];
            for (int iter = 1; iter <= smoothIters; iter++)
            {
                for (int y = 0; y < fine; y++)
                {
                    for (int x = 0; x < fine; x++)
                    {
                        if (seedMask[x, y])
                        {
                            next[x, y] = 255f;
                            continue;
                        }

                        float sum = 0f;
                        int n = 0;
                        if (x > 0) { sum += field[x - 1, y]; n++; }
                        if (x + 1 < fine) { sum += field[x + 1, y]; n++; }
                        if (y > 0) { sum += field[x, y - 1]; n++; }
                        if (y + 1 < fine) { sum += field[x, y + 1]; n++; }
                        next[x, y] = n > 0 ? sum / n : field[x, y];
                    }
                }

                var tmp = field;
                field = next;
                next = tmp;

                if (normalizeEvery > 0 && (iter % normalizeEvery == 0 || iter == smoothIters))
                    MinMaxNormalize(field, fine, fine, seedMask);
            }

            // Block-mean quantize back to world grid (upsample x upsample).
            var bytes = new byte[size, size];
            var roughness = new float[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    int x0 = x * upsample;
                    int y0 = y * upsample;
                    for (int dy = 0; dy < upsample; dy++)
                    {
                        for (int dx = 0; dx < upsample; dx++)
                        {
                            sum += field[x0 + dx, y0 + dy];
                            count++;
                        }
                    }
                    float v = sum / count;
                    if (v < 0) v = 0;
                    if (v > 255) v = 255;
                    byte b = (byte)Math.Round(v);
                    bytes[x, y] = b;
                    roughness[x, y] = b / 255f;
                }
            }

            Point start, goal;
            PickFarthestDarkPair(bytes, size, size, out start, out goal);

            return new World(name, size, size, start, goal, roughness, new List<Rectangle>());
        }

        public static IReadOnlyList<World> GenerateFour()
        {
            var list = new List<World>(4);
            for (int i = 0; i < FourSeeds.Length; i++)
                list.Add(Generate(FourNames[i], FourSeeds[i]));
            return list;
        }

        /// <summary>
        /// Terrain colormap: low roughness = dark blue/black, high = yellow/white.
        /// Marks Start as green "S", Goal as blue "D", optional yellow 4x4 block grid.
        /// </summary>
        public static void SaveColorPreview(World world, string path, int cellPx = 24, bool drawBlockGrid = true, int blocks = DefaultBlocks)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            int w = world.Cols * cellPx;
            int h = world.Rows * cellPx;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                using (var font = new Font("Segoe UI", Math.Max(8f, cellPx * 0.45f), FontStyle.Bold, GraphicsUnit.Pixel))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    for (int y = 0; y < world.Rows; y++)
                    {
                        for (int x = 0; x < world.Cols; x++)
                        {
                            float r = world.CellRoughness(x, y);
                            Color c = TerrainColor(r);
                            using (var br = new SolidBrush(c))
                                g.FillRectangle(br, x * cellPx, y * cellPx, cellPx, cellPx);
                        }
                    }

                    if (drawBlockGrid && blocks > 0 && world.Cols % blocks == 0 && world.Rows % blocks == 0)
                    {
                        int bx = world.Cols / blocks;
                        int by = world.Rows / blocks;
                        using (var pen = new Pen(Color.FromArgb(200, 255, 220, 40), 2f))
                        {
                            for (int i = 0; i <= blocks; i++)
                            {
                                int px = i * bx * cellPx;
                                int py = i * by * cellPx;
                                g.DrawLine(pen, px, 0, px, h);
                                g.DrawLine(pen, 0, py, w, py);
                            }
                        }
                    }

                    using (var startBr = new SolidBrush(Color.FromArgb(46, 200, 90)))
                    using (var goalBr = new SolidBrush(Color.FromArgb(70, 140, 255)))
                    using (var textBr = new SolidBrush(Color.White))
                    {
                        var sr = new Rectangle(
                            world.StartCell.X * cellPx,
                            world.StartCell.Y * cellPx,
                            cellPx, cellPx);
                        var gr = new Rectangle(
                            world.GoalCell.X * cellPx,
                            world.GoalCell.Y * cellPx,
                            cellPx, cellPx);
                        g.FillEllipse(startBr, sr);
                        g.FillEllipse(goalBr, gr);
                        g.DrawString("S", font, textBr, sr, sf);
                        g.DrawString("D", font, textBr, gr, sf);
                    }
                }

                bmp.Save(path, ImageFormat.Png);
            }
        }

        public static void SaveRoughnessCsv(World world, string path)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            using (var sw = new StreamWriter(path))
            {
                for (int y = 0; y < world.Rows; y++)
                {
                    var parts = new string[world.Cols];
                    for (int x = 0; x < world.Cols; x++)
                        parts[x] = ((int)Math.Round(world.CellRoughness(x, y) * 255f)).ToString();
                    sw.WriteLine(string.Join(",", parts));
                }
            }
        }

        /// <summary>Map roughness [0,1] to terrain heatmap (dark blue -> cyan -> yellow -> white).</summary>
        public static Color TerrainColor(float roughness01)
        {
            float t = roughness01;
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            // stops: 0 dark navy, 0.33 blue, 0.66 cyan/green, 1 yellow/white
            Color[] stops =
            {
                Color.FromArgb(8, 12, 40),
                Color.FromArgb(20, 60, 140),
                Color.FromArgb(40, 180, 160),
                Color.FromArgb(240, 220, 60),
                Color.FromArgb(255, 250, 230),
            };
            float scaled = t * (stops.Length - 1);
            int i = (int)Math.Floor(scaled);
            if (i >= stops.Length - 1) return stops[stops.Length - 1];
            float f = scaled - i;
            return Color.FromArgb(
                Lerp(stops[i].R, stops[i + 1].R, f),
                Lerp(stops[i].G, stops[i + 1].G, f),
                Lerp(stops[i].B, stops[i + 1].B, f));
        }

        public static void SaveFourPreviews(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
            Directory.CreateDirectory(directory);
            var worlds = GenerateFour();
            for (int i = 0; i < worlds.Count; i++)
            {
                string baseName = FourNames[i];
                SaveColorPreview(worlds[i], Path.Combine(directory, baseName + ".png"));
                SaveRoughnessCsv(worlds[i], Path.Combine(directory, baseName + ".csv"));
            }
        }

        static void MinMaxNormalize(float[,] field, int w, int h, bool[,] seedMask)
        {
            float min = float.MaxValue, max = float.MinValue;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float v = field[x, y];
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }
            float range = max - min;
            if (range < 1e-6f)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        field[x, y] = (seedMask != null && seedMask[x, y]) ? 255f : 0f;
                return;
            }
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (seedMask != null && seedMask[x, y])
                    {
                        field[x, y] = 255f;
                        continue;
                    }
                    field[x, y] = (field[x, y] - min) / range * 255f;
                }
            }
        }

        static void PickFarthestDarkPair(byte[,] bytes, int cols, int rows, out Point src, out Point dst)
        {
            var values = new List<byte>(cols * rows);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    values.Add(bytes[x, y]);
            values.Sort();
            int pctIdx = Math.Max(0, (values.Count * 25) / 100 - 1);
            int threshold = Math.Min(64, (int)values[pctIdx]);

            var dark = new List<Point>();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (bytes[x, y] <= threshold)
                        dark.Add(new Point(x, y));
                }
            }

            if (dark.Count < 2)
            {
                // Fallback: use absolute darkest cells, then any cells.
                dark.Clear();
                byte minB = values[0];
                for (int y = 0; y < rows; y++)
                    for (int x = 0; x < cols; x++)
                        if (bytes[x, y] <= minB + 5)
                            dark.Add(new Point(x, y));
                if (dark.Count < 2)
                {
                    dark.Clear();
                    for (int y = 0; y < rows; y++)
                        for (int x = 0; x < cols; x++)
                            dark.Add(new Point(x, y));
                }
            }

            src = dark[0];
            dst = dark[Math.Min(1, dark.Count - 1)];
            double best = -1;
            for (int i = 0; i < dark.Count; i++)
            {
                for (int j = i + 1; j < dark.Count; j++)
                {
                    int dx = dark[i].X - dark[j].X;
                    int dy = dark[i].Y - dark[j].Y;
                    double d2 = (double)dx * dx + (double)dy * dy;
                    if (d2 > best)
                    {
                        best = d2;
                        src = dark[i];
                        dst = dark[j];
                    }
                }
            }
        }

        static int Lerp(int a, int b, float t)
        {
            return (int)Math.Round(a + (b - a) * t);
        }

        /// <summary>
        /// Harder Terrain-A..D (V12 Fix1 recipe family): same 4x4 white-seed Laplacian,
        /// but seeds pinned only for early smoothing iters, then percentile + gamma contrast
        /// so mean roughness lands ~0.35-0.55 with broad hills. Start/goal pinned to Fix2
        /// geometry so straight/greedy path crosses rough (comparable LOO protocol).
        /// </summary>
        public static World GenerateTerrain(string name, int seed, int index,
            int size = DefaultSize, int blocks = DefaultBlocks, int upsample = DefaultUpsample,
            int smoothIters = DefaultSmoothIters, int pinIters = 8, float gamma = 1.35f)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException("size");
            if (blocks <= 0 || size % blocks != 0)
                throw new ArgumentException("size must be divisible by blocks", "blocks");
            if (upsample < 1) upsample = 1;
            if (pinIters < 1) pinIters = 1;

            int blockCells = size / blocks;
            int fine = size * upsample;
            var field = new float[fine, fine];
            var seedMask = new bool[fine, fine];
            var rng = new Random(seed);

            for (int by = 0; by < blocks; by++)
            {
                for (int bx = 0; bx < blocks; bx++)
                {
                    int lx = rng.Next(blockCells);
                    int ly = rng.Next(blockCells);
                    int wx = bx * blockCells + lx;
                    int wy = by * blockCells + ly;
                    int fx = wx * upsample + upsample / 2;
                    int fy = wy * upsample + upsample / 2;
                    if (fx >= fine) fx = fine - 1;
                    if (fy >= fine) fy = fine - 1;
                    field[fx, fy] = 255f;
                    seedMask[fx, fy] = true;
                }
            }

            var next = new float[fine, fine];
            for (int iter = 1; iter <= smoothIters; iter++)
            {
                bool pin = iter <= pinIters;
                for (int y = 0; y < fine; y++)
                {
                    for (int x = 0; x < fine; x++)
                    {
                        if (pin && seedMask[x, y])
                        {
                            next[x, y] = 255f;
                            continue;
                        }
                        float sum = 0f; int n = 0;
                        if (x > 0) { sum += field[x - 1, y]; n++; }
                        if (x + 1 < fine) { sum += field[x + 1, y]; n++; }
                        if (y > 0) { sum += field[x, y - 1]; n++; }
                        if (y + 1 < fine) { sum += field[x, y + 1]; n++; }
                        next[x, y] = n > 0 ? sum / n : field[x, y];
                    }
                }
                var tmp = field; field = next; next = tmp;
                if (DefaultNormalizeEvery > 0 && (iter % DefaultNormalizeEvery == 0 || iter == smoothIters))
                    MinMaxNormalize(field, fine, fine, pin ? seedMask : null);
            }

            // Block-mean downsample
            var bytes = new byte[size, size];
            var values = new List<float>(size * size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f; int count = 0;
                    int x0 = x * upsample, y0 = y * upsample;
                    for (int dy = 0; dy < upsample; dy++)
                        for (int dx = 0; dx < upsample; dx++)
                        { sum += field[x0 + dx, y0 + dy]; count++; }
                    float v = sum / count;
                    if (v < 0) v = 0; if (v > 255) v = 255;
                    bytes[x, y] = (byte)Math.Round(v);
                    values.Add(v);
                }
            }
            values.Sort();
            float p5 = values[Math.Max(0, values.Count * 5 / 100)];
            float p95 = values[Math.Min(values.Count - 1, values.Count * 95 / 100)];
            float range = p95 - p5;
            if (range < 1e-3f) range = 1f;

            var roughness = new float[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = (bytes[x, y] - p5) / range;
                    if (t < 0) t = 0; if (t > 1) t = 1;
                    t = (float)Math.Pow(t, gamma);
                    roughness[x, y] = t;
                }
            }

            Point start = TerrainStarts[index % TerrainStarts.Length];
            Point goal = TerrainGoals[index % TerrainGoals.Length];
            // Clamp if size differs from 20
            if (start.X >= size) start = new Point(size / 4, size - 1);
            if (start.Y >= size) start = new Point(start.X, size - 1);
            if (goal.X >= size) goal = new Point(size * 3 / 4, 0);
            if (goal.Y >= size) goal = new Point(goal.X, 0);

            return new World(name, size, size, start, goal, roughness, new List<Rectangle>());
        }

        public static IReadOnlyList<World> GenerateFourTerrain()
        {
            var list = new List<World>(4);
            for (int i = 0; i < TerrainSeeds.Length; i++)
                list.Add(GenerateTerrain(TerrainNames[i], TerrainSeeds[i], i));
            return list;
        }

    }
}
