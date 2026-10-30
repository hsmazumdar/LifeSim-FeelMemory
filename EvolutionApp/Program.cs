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
                        string.Equals(args[i], "--eval-revision", StringComparison.OrdinalIgnoreCase) || string.Equals(args[i], "--audit-r1", StringComparison.OrdinalIgnoreCase) || string.Equals(args[i], "--eval-stageb", StringComparison.OrdinalIgnoreCase) || string.Equals(args[i], "--eval-sens", StringComparison.OrdinalIgnoreCase) || string.Equals(args[i], "--eval-posgain", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--baseline", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "-eval", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--smoke", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--teacher-diag", StringComparison.OrdinalIgnoreCase))
                    {
                        // Attach console so WinExe can print progress when launched from cmd.
                        try { AllocConsole(); } catch { }
                        foreach (var ra in args) if (string.Equals(ra, "--eval-stageb", StringComparison.OrdinalIgnoreCase)) return StageBHarness.Run(args);
                        foreach (var ra in args) if (string.Equals(ra, "--eval-posgain", StringComparison.OrdinalIgnoreCase)) return PosGainHarness.Run(args);
                        foreach (var ra in args) if (string.Equals(ra, "--eval-sens", StringComparison.OrdinalIgnoreCase)) return SensHarness.Run(args);
                        foreach (var ra in args) if (string.Equals(ra, "--audit-r1", StringComparison.OrdinalIgnoreCase)) return AuditR1.Run(args);
                        foreach (var ra in args) if (string.Equals(ra, "--eval-revision", StringComparison.OrdinalIgnoreCase)) return RevisionHarness.Run(args);
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
