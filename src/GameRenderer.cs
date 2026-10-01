using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AtroxLauncher
{
    internal static class GameRenderer
    {
        internal const string RendererHash = "85e0f7d530dfda134793a57cb3e76b0287dcc96892ee57162dd68f47283b03a9";

        internal static void Install(string gameDirectory, bool windowed)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var destination = Path.Combine(gameDirectory, "ddraw.dll");
            if (!File.Exists(destination) || LauncherUpdate.Sha256(destination) != RendererHash)
            {
                var staged = destination + ".next";
                using (var resource = assembly.GetManifestResourceStream("AtroxLauncher.Rendering.ddraw.dll"))
                using (var output = File.Create(staged)) resource.CopyTo(output);
                if (LauncherUpdate.Sha256(staged) != RendererHash) throw new InvalidDataException("화면 출력 모듈의 무결성 검사에 실패했습니다.");
                if (File.Exists(destination)) File.Replace(staged, destination, BackupPath(destination));
                else File.Move(staged, destination);
            }
            var license = Path.Combine(gameDirectory, "cnc-ddraw-LICENSE.txt");
            using (var resource = assembly.GetManifestResourceStream("AtroxLauncher.Rendering.LICENSE"))
            using (var output = File.Create(license)) resource.CopyTo(output);
            var config = Path.Combine(gameDirectory, "ddraw.ini");
            if (File.Exists(config))
            {
                var backup = BackupPath(config);
                if (!File.Exists(backup)) File.Copy(config, backup);
            }
            // Preserve unrelated settings; write a per-game profile with controlled presentation options.
            foreach (var section in new[] { "ddraw", "Atrox" })
            {
                Set(config, section, "windowed", "true");
                Set(config, section, "fullscreen", windowed ? "false" : "true");
                Set(config, section, "toggle_borderless", "true");
                Set(config, section, "maintas", "true");
                Set(config, section, "adjmouse", "true");
                Set(config, section, "width", "0"); Set(config, section, "height", "0");
                Set(config, section, "posX", "-32000"); Set(config, section, "posY", "-32000");
                Set(config, section, "renderer", "direct3d9");
                Set(config, section, "d3d9_filter", "1");
                Set(config, section, "maxfps", "60");
                Set(config, section, "maxgameticks", "-1");
                Set(config, section, "vsync", "true");
                Set(config, section, "minfps", "-2");
                Set(config, section, "nonexclusive", "true");
                Set(config, section, "resizable", "false");
                Set(config, section, "keytogglefullscreen", "0x0D");
            }
        }

        static string BackupPath(string path) => path + ".before-launcher-" + LauncherUpdate.Sha256(path);
        static void Set(string path, string section, string key, string value)
        {
            if (!WritePrivateProfileString(section, key, value, path))
                throw new IOException("화면 출력 설정을 저장하지 못했습니다: " + path);
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool WritePrivateProfileString(string section, string key, string value, string path);
    }
}
