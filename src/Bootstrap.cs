using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace AtroxLauncher
{
    internal static class Bootstrap
    {
        [STAThread]
        internal static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                var name = new AssemblyName(e.Name).Name;
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AtroxLauncher." + name + ".dll"))
                {
                    if (stream == null) return null;
                    using (var memory = new MemoryStream()) { stream.CopyTo(memory); return Assembly.Load(memory.ToArray()); }
                }
            };
            try
            {
                Directory.SetCurrentDirectory(Application.StartupPath);
                Program.Run(args);
            }
            catch (Exception error)
            {
                var log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtroxLauncher", "Startup.log");
                try { Directory.CreateDirectory(Path.GetDirectoryName(log)); File.AppendAllText(log, DateTime.Now.ToString("s") + "\r\n" + error + "\r\n"); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                if (args.Length == 2 && args[0] == "--startup-check")
                {
                    File.WriteAllText(args[1], "FAILED\r\n" + error); Environment.ExitCode = 1; return;
                }
                MessageBox.Show("런처를 시작하지 못했습니다.\n" + error.Message + "\n\n오류 기록: " + log, "런처 실행 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
