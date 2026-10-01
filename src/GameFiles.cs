using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AtroxLauncher
{
    internal static class GameFiles
    {
        internal const string TemplateHash = "b9561ed32e1c5f4275862b5b2afda5425fd4600735a1179ffbdf9b5ac603d6f5";
        internal static string LegacyDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "AtroxLauncher");
        internal static string ConfigSource => File.Exists(Path.Combine(Application.StartupPath, "Config.json"))
            ? Path.Combine(Application.StartupPath, "Config.json") : Path.Combine(LegacyDirectory, "Config.json");
        internal static string ResolvePath(string path, string directory) => Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(directory, path));

        internal static string FindTemplate(string gameDirectory)
        {
            var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtroxLauncher", "Templates", TemplateHash, "Atrox.ex_");
            var parent = Directory.GetParent(gameDirectory);
            var candidates = new[] {
                Path.Combine(Application.StartupPath, "Atrox.ex_"),
                Path.Combine(gameDirectory, "Atrox.ex_"),
                Path.Combine(parent == null ? gameDirectory : parent.FullName, "Atrox.ex_"),
                Path.Combine(LegacyDirectory, "Atrox.ex_"), cache
            };
            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(candidate) || LauncherUpdate.Sha256(candidate) != TemplateHash) continue;
                // Caching is optional: a read-only launcher folder must not prevent starting the game.
                if (!string.Equals(candidate, cache, StringComparison.OrdinalIgnoreCase))
                {
                    try { Directory.CreateDirectory(Path.GetDirectoryName(cache)); File.Copy(candidate, cache, true); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                return candidate;
            }
            using (var dialog = new OpenFileDialog { Title = "기존 런처의 Atrox.ex_ 원본 선택", Filter = "Atrox 원본|Atrox.ex_", InitialDirectory = gameDirectory })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return null;
                if (LauncherUpdate.Sha256(dialog.FileName) != TemplateHash)
                    throw new InvalidDataException("지원하는 Atrox.ex_ 원본이 아닙니다. 기존 런처의 원본 파일을 선택해 주세요.");
                try { Directory.CreateDirectory(Path.GetDirectoryName(cache)); File.Copy(dialog.FileName, cache, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                return dialog.FileName;
            }
        }
    }
}
