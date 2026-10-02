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
            LauncherUpdate.EnsureVersionedName();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 2 && args[0] == "--startup-check")
            {
                using (var form = new MainForm())
                    System.IO.File.WriteAllText(args[1], "OK\r\n" + form.Text + "\r\n" + LauncherUpdate.ExecutablePath);
                return;
            }
            Application.Run(new MainForm());
        }
    }
}
