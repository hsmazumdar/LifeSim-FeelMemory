using System.Collections.Generic;
using System.Drawing;

namespace Evolution
{
    /// <summary>
    /// Motor memory: terrain feel for the body based on continuous roughness.
    /// Feel is graded: Smooth/Medium/Rough based on roughness thresholds.
    /// Obstacle remains a separate hard-blocked feel.
    /// </summary>
    public sealed class MotorMemory
    {
        public enum Feel
        {
            Unknown = 0,
            Smooth,
            Medium,
            Rough,
            Obstacle
        }

        public const float SmoothThreshold = 0.30f;
        public const float RoughThreshold = 0.65f;

        public const double SmoothStepsPerSec = 30.0;
        public const double MediumStepsPerSec = 15.0;
        public const double RoughStepsPerSec = 5.0;
        public const double ObstacleStepsPerSec = 5.0;

        readonly Dictionary<Point, Feel> _cells = new Dictionary<Point, Feel>();
        readonly Dictionary<Point, float> _roughnessCache = new Dictionary<Point, float>();

        public int Count => _cells.Count;

        public void Clear()
        {
            _cells.Clear();
            _roughnessCache.Clear();
        }

        public Feel Get(Point cell)
        {
            Feel f;
            return _cells.TryGetValue(cell, out f) ? f : Feel.Unknown;
        }

        public float GetRoughness(Point cell)
        {
            float r;
            return _roughnessCache.TryGetValue(cell, out r) ? r : 0.5f;
        }

        public Feel Observe(World world, Point cell)
        {
            Feel feel;
            float roughness;

            if (world.CellHasObstacle(cell))
            {
                feel = Feel.Obstacle;
                roughness = 1.0f;
            }
            else
            {
                roughness = world.CellRoughness(cell);
                if (roughness <= SmoothThreshold)
                {
                    feel = Feel.Smooth;
                }
                else if (roughness >= RoughThreshold)
                {
                    feel = Feel.Rough;
                }
                else
                {
                    feel = Feel.Medium;
                }
            }

            _cells[cell] = feel;
            _roughnessCache[cell] = roughness;
            return feel;
        }

        public static double StepsPerSec(Feel feel)
        {
            switch (feel)
            {
                case Feel.Smooth:
                    return SmoothStepsPerSec;
                case Feel.Medium:
                    return MediumStepsPerSec;
                case Feel.Rough:
                    return RoughStepsPerSec;
                case Feel.Obstacle:
                    return ObstacleStepsPerSec;
                default:
                    return MediumStepsPerSec;
            }
        }

        public static double StepsPerSecFromRoughness(float roughness)
        {
            if (roughness <= 0) return SmoothStepsPerSec;
            if (roughness >= 1) return RoughStepsPerSec;
            return SmoothStepsPerSec - (SmoothStepsPerSec - RoughStepsPerSec) * roughness;
        }

        public static float FeelToRoughness(Feel feel)
        {
            switch (feel)
            {
                case Feel.Smooth: return 0.15f;
                case Feel.Medium: return 0.50f;
                case Feel.Rough: return 0.80f;
                case Feel.Obstacle: return 1.0f;
                default: return 0.50f;
            }
        }
    }
}
