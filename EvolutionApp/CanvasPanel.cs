using System.Windows.Forms;

namespace Evolution
{
    /// <summary>
    /// Panel with double-buffering enabled (stock Panel has it off).
    /// </summary>
    public class CanvasPanel : Panel
    {
        public CanvasPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
        }
    }
}
