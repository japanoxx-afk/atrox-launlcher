using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtroxLauncher
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        internal static void Run(string[] args)
        {
            if (args.Length == 4 && args[0] == "--apply-update") { LauncherUpdate.Apply(args); return; }
            if (LauncherUpdate.EnsureVersionedName()) return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
