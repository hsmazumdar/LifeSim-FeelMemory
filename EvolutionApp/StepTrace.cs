using System.Drawing;

namespace Evolution
{
    public enum StepOutcome : byte
    {
        Smooth = 0,
        Medium = 1,
        Rough = 2,
        Obstacle = 3
    }

    public struct StepTrace
    {
        public Point From;
        public int Dx;
        public int Dy;
        public StepOutcome Outcome;
    }
}
