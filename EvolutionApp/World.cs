using System.Collections.Generic;
using System.Drawing;

namespace Evolution
{
    /// <summary>
    /// A terrain world with continuous roughness values per cell.
    /// Roughness in [0,1]: 0 = smooth (fast, low energy), 1 = rough (slow, high energy).
    /// Obstacles are separate and remain hard-blocked.
    /// </summary>
    public sealed class World
    {
        public string Name { get; }
        public int Cols { get; }
        public int Rows { get; }
        public Point StartCell { get; }
        public Point GoalCell { get; }

        readonly float[,] _roughness;
        readonly bool[,] _obstacle;

        public World(
            string name,
            int cols,
            int rows,
            Point startCell,
            Point goalCell,
            float[,] roughness,
            IReadOnlyList<Rectangle> obstacles)
        {
            Name = name;
            Cols = cols;
            Rows = rows;
            StartCell = startCell;
            GoalCell = goalCell;

            _roughness = new float[cols, rows];
            _obstacle = new bool[cols, rows];

            if (roughness != null)
            {
                for (int x = 0; x < cols; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        float r = (x < roughness.GetLength(0) && y < roughness.GetLength(1))
                            ? roughness[x, y] : 0.5f;
                        _roughness[x, y] = Clamp01(r);
                    }
                }
            }

            if (obstacles != null)
            {
                foreach (var rect in obstacles)
                {
                    for (int x = rect.X; x < rect.X + rect.Width; x++)
                    {
                        for (int y = rect.Y; y < rect.Y + rect.Height; y++)
                        {
                            if (x >= 0 && x < cols && y >= 0 && y < rows)
                            {
                                _obstacle[x, y] = true;
                            }
                        }
                    }
                }
            }
        }

        public bool CellHasObstacle(Point cell)
        {
            if (cell.X < 0 || cell.Y < 0 || cell.X >= Cols || cell.Y >= Rows)
                return true;
            return _obstacle[cell.X, cell.Y];
        }

        public bool CellHasObstacle(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Cols || y >= Rows)
                return true;
            return _obstacle[x, y];
        }

        /// <summary>
        /// Get terrain roughness for a cell.
        /// Roughness 0 = smooth (fast), 1 = rough (slow).
        /// Returns 1.0 for out-of-bounds cells.
        /// </summary>
        public float CellRoughness(Point cell)
        {
            if (cell.X < 0 || cell.Y < 0 || cell.X >= Cols || cell.Y >= Rows)
                return 1.0f;
            return _roughness[cell.X, cell.Y];
        }

        public float CellRoughness(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Cols || y >= Rows)
                return 1.0f;
            return _roughness[x, y];
        }

        /// <summary>
        /// Energy cost to traverse this cell. Higher roughness = more energy.
        /// Base formula: energy = 0.2 + 0.8 * roughness (so smooth = 0.2, rough = 1.0)
        /// </summary>
        public float CellEnergyCost(Point cell)
        {
            if (CellHasObstacle(cell))
                return 2.0f;
            float r = CellRoughness(cell);
            return 0.2f + 0.8f * r;
        }

        public float CellEnergyCost(int x, int y)
        {
            return CellEnergyCost(new Point(x, y));
        }

        /// <summary>
        /// Steps per second on this cell. Smoother = faster.
        /// Range: smooth (r=0) = 30 sps, rough (r=1) = 5 sps
        /// </summary>
        public double CellStepsPerSec(Point cell)
        {
            if (CellHasObstacle(cell))
                return 5.0;
            float r = CellRoughness(cell);
            return 30.0 - 25.0 * r;
        }

        static float Clamp01(float v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }
    }
}
