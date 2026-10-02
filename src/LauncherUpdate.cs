using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AtroxLauncher
{
    // Releases contain a single managed executable. Settings and game files are never replaced.
    internal static class LauncherUpdate
    {
        internal const string Repository = "japanoxx-afk/atrox-launlcher";
        internal static readonly Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        internal static string ExecutablePath = Assembly.GetExecutingAssembly().Location;
        internal sealed class Release
        {
            internal Version Version;
            internal string Url;
            internal string HashUrl;
        }
        internal static async Task<Release> FindAsync()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var client = Client())
            {
                var response = await client.GetAsync("https://api.github.com/repos/" + Repository + "/releases/latest");
                if (response.StatusCode == HttpStatusCode.NotFound) return null;
                response.EnsureSuccessStatusCode();
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                Version version;
                if (!Version.TryParse(((string)json["tag_name"]).TrimStart('v'), out version))
                    throw new InvalidDataException("릴리스 버전 형식이 올바르지 않습니다.");
                var assets = (JArray)json["assets"];
                Func<string, string> asset = name => assets.Where(a => (string)a["name"] == name)
                    .Select(a => (string)a["browser_download_url"]).SingleOrDefault();
                var result = new Release { Version = NormalizeVersion(version), Url = asset("AtroxLauncher.exe"), HashUrl = asset("AtroxLauncher.exe.sha256") };
                if (result.Url == null || result.HashUrl == null) throw new InvalidDataException("릴리스에 실행 파일 또는 검증 파일이 없습니다.");
                ValidateUrl(result.Url); ValidateUrl(result.HashUrl);
                return result;
            }
        }
        internal static Version NormalizeVersion(Version version)
        {
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
        }
        private static HttpClient Client()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AtroxLauncher/" + CurrentVersion);
            return client;
        }
        private static void ValidateUrl(string url)
        {
            var uri = new Uri(url);
            if (uri.Scheme != "https" || uri.Host != "github.com" || !uri.AbsolutePath.StartsWith("/" + Repository + "/releases/download/", StringComparison.Ordinal))
                throw new InvalidDataException("허용되지 않은 업데이트 주소입니다.");
        }
        internal static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        internal static async Task<string> DownloadAsync(Release release)
        {
            var directory = Path.Combine(Path.GetTempPath(), "AtroxLauncherUpdate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "AtroxLauncher.exe");
            using (var client = Client())
            {
                var hash = (await client.GetStringAsync(release.HashUrl)).Trim().Split(' ', '\t', '\r', '\n')[0];
                if (hash.Length != 64 || !hash.All(Uri.IsHexDigit)) throw new InvalidDataException("SHA-256 형식이 올바르지 않습니다.");
                using (var response = await client.GetAsync(release.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var source = await response.Content.ReadAsStreamAsync())
                    using (var destination = File.Create(path))
                    {
                        byte[] buffer = new byte[81920]; int count; long total = 0;
                        while ((count = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            total += count;
                            if (total > 20 * 1024 * 1024) throw new InvalidDataException("업데이트 파일이 너무 큽니다.");
                            await destination.WriteAsync(buffer, 0, count);
                        }
                    }
                }
                if (!string.Equals(Sha256(path), hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("업데이트 파일 검증에 실패했습니다. 현재 런처는 그대로 유지됩니다.");
                if (AssemblyName.GetAssemblyName(path).Version != release.Version)
                    throw new InvalidDataException("업데이트 실행 파일의 버전이 릴리스와 다릅니다.");
            }
            return path;
        }
        internal static void StartApply(string stagedPath)
        {
            // The helper is a copy of THIS version, so downloaded code is not run before installation.
            var directory = Path.Combine(Path.GetTempPath(), "AtroxLauncherApply-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var helper = Path.Combine(directory, "ApplyUpdate.exe");
            File.Copy(ExecutablePath, helper);
            System.Diagnostics.Process.Start(new ProcessStartInfo(helper, "--apply-update " + System.Diagnostics.Process.GetCurrentProcess().Id + " " + Quote(stagedPath) + " " + Quote(ExecutablePath)) { UseShellExecute = false });
        }
        internal static string Quote(string value) { return "\"" + value + "\""; }
        internal static string VersionedPath(string target, Version version)
        {
            var normalized = NormalizeVersion(version);
            var display = normalized.Revision == 0 ? normalized.ToString(3) : normalized.ToString(4);
            return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(target)), "AtroxLauncher_v" + display + ".exe");
        }
        internal static void EnsureVersionedName()
        {
            var current = ExecutablePath;
            var target = VersionedPath(current, CurrentVersion);
            if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase)) return;
            // Renaming is optional startup housekeeping. Never exit the launcher
            // or require a second executable in Temp before displaying its window.
            try
            {
                File.Move(current, target);
                ExecutablePath = target;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        internal static string ReplaceAndRename(string staged, string original)
        {
            var target = VersionedPath(original, AssemblyName.GetAssemblyName(staged).Version);
            if (!string.Equals(target, original, StringComparison.OrdinalIgnoreCase) && File.Exists(target))
            {
                if (!string.Equals(Sha256(target), Sha256(staged), StringComparison.OrdinalIgnoreCase))
                    throw new IOException("새 버전의 이름을 가진 다른 파일이 이미 있습니다: " + target);
                File.Copy(original, original + ".previous", true);
                return target;
            }
            File.Copy(staged, original + ".next", true);
            File.Replace(original + ".next", original, original + ".previous", true);
            try
            {
                if (!string.Equals(target, original, StringComparison.OrdinalIgnoreCase)) File.Move(original, target);
            }
            catch
            {
                File.Replace(original + ".previous", original, null, true);
                throw;
            }
            return target;
        }
        internal static void CleanupPrevious(string target)
        {
            var backup = Path.GetFullPath(target) + ".previous";
            try
            {
                if (!File.Exists(backup)) return;
                var previous = AssemblyName.GetAssemblyName(backup);
                var current = Assembly.GetExecutingAssembly().GetName();
                if (previous.Name == current.Name && NormalizeVersion(previous.Version) <= NormalizeVersion(current.Version))
                    File.Delete(backup);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (BadImageFormatException) { }
            // Locked backups are retried on the next successful launch.
        }
        internal static void Apply(string[] args)
        {
            var target = Path.GetFullPath(args[3]);
            var staged = Path.GetFullPath(args[2]);
            var backup = target + ".previous";
            string renamed = null;
            try
            {
                try { using (var parent = System.Diagnostics.Process.GetProcessById(int.Parse(args[1]))) if (!parent.WaitForExit(30000)) throw new IOException("기존 런처가 종료되지 않았습니다."); }
                catch (ArgumentException) { }
                renamed = ReplaceAndRename(staged, target);
                using (var launched = System.Diagnostics.Process.Start(new ProcessStartInfo(renamed) { WorkingDirectory = Path.GetDirectoryName(renamed) }))
                {
                    if (launched == null || !launched.WaitForInputIdle(30000) || launched.HasExited)
                        throw new IOException("새 런처가 정상적으로 시작되지 않았습니다.");
                }
                if (!string.Equals(renamed, target, StringComparison.OrdinalIgnoreCase) && File.Exists(target)) File.Delete(target);
                CleanupPrevious(target);
            }
            catch (Exception e)
            {
                // Restore only when this helper completed replacement; collision
                // failures must not consume a backup from an earlier update.
                if (renamed != null && File.Exists(backup))
                {
                    try
                    {
                        if (File.Exists(target)) File.Replace(backup, target, null, true);
                        else File.Move(backup, target);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                System.Windows.Forms.MessageBox.Show("업데이트 적용 실패: " + e.Message + "\n백업: " + backup, "런처 업데이트");
            }
        }
    }
}

