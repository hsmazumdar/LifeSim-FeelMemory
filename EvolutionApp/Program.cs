using System;
using System.Windows.Forms;

namespace Evolution
{
    internal static class Program
    {
        /// <summary>
        /// Entry point. Pass --eval / --smoke / --teacher-diag for headless (no UI).
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--eval", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--eval-gk-loo", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--gk-loo", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--eval-baseline", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--baseline", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "-eval", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--smoke", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--teacher-diag", StringComparison.OrdinalIgnoreCase))
                    {
                        // Attach console so WinExe can print progress when launched from cmd.
                        try { AllocConsole(); } catch { }
                        return EvalHarness.Run(args);
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LifeSim());
            return 0;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();
    }
}