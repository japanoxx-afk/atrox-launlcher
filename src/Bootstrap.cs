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
            Directory.SetCurrentDirectory(Application.StartupPath);
            Program.Run(args);
        }
    }
}
