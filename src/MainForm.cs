using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace AtroxLauncher
{
    public partial class MainForm : Form
    {
        string AtroxFolderPath
        {
            get => PathTextBox.Text;
            set => PathTextBox.Text = value;
        }
        string AtroxPath => $@"{AtroxFolderPath}\Atrox.exe";

        string JPakPath
        {
            get => JPakPathTextBox.Text;
            set => JPakPathTextBox.Text = value;
        }
        string JPakPathExtractPath => $@"{AtroxFolderPath}\atrox_pak";

        string ConfigPath => "Config.json";
        string LinkLabelLink = "https://cafe.naver.com/atroxs";
        bool isInit;
        static readonly int[] ScrollRates = { 5, 10, 20, 40, 60, 80, 100 };
        readonly CheckBox autoReplay = new CheckBox { Text = "리플레이 자동 저장", Checked = true };
        readonly NumericUpDown replayMaximum = new NumericUpDown { Minimum = 1, Maximum = 1000, Value = 20 };
        readonly ComboBox scrollSpeed = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };

        public MainForm()
        {
            InitializeComponent();
            scrollSpeed.Items.AddRange(new object[] { "5% (아주 느림)", "10% (권장)", "20%", "40%", "60%", "80%", "100% (기존)" });
            scrollSpeed.SelectedIndex = 1;
            scrollSpeed.SelectedIndexChanged += WriteConfig;
            autoReplay.CheckedChanged += (sender, args) => { replayMaximum.Enabled = autoReplay.Checked; WriteConfig(); };
            replayMaximum.ValueChanged += WriteConfig;
            Text = "AtroxLauncher v" + LauncherUpdate.CurrentVersion.ToString(3);
            Shown += (sender, args) => {
                LauncherUpdate.CleanupPrevious(LauncherUpdate.ExecutablePath);
                LauncherUpdate.CleanupPrevious(System.Reflection.Assembly.GetExecutingAssembly().Location);
            };
            updateButton.Click += async (sender, args) => {
                updateButton.Enabled = false;
                try {
                    var release = await LauncherUpdate.FindAsync();
                    if (release == null) { MessageBox.Show("아직 배포된 업데이트가 없습니다.", "런처 업데이트"); return; }
                    if (release.Version <= LauncherUpdate.CurrentVersion) { MessageBox.Show("최신 버전입니다.", "런처 업데이트"); return; }
                    if (System.Diagnostics.Process.GetProcessesByName("Atrox").Length != 0) { MessageBox.Show("게임을 종료한 후 업데이트해 주세요."); return; }
                    updateButton.Text = "다운로드 중…";
                    var downloaded = await LauncherUpdate.DownloadAsync(release);
                    WriteConfig();
                    LauncherUpdate.StartApply(downloaded);
                    Close();
                } catch (Exception ex) { MessageBox.Show(ex.Message, "업데이트 실패", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { updateButton.Text = "런처 업데이트"; updateButton.Enabled = true; }
            };

            try { ReadConfig(); }
            catch (Exception ex) { isInit = true; MessageBox.Show("설정을 읽지 못했습니다. 경로와 옵션을 확인해 주세요.\n" + ex.Message, "설정 오류"); }
        }

        void ReadConfig()
        {
            var configSource = GameFiles.ConfigSource;
            if (!File.Exists(configSource))
            {
                isInit = true;
                return;
            }

            var jObject = JObject.Parse(File.ReadAllText(configSource));
            var configDirectory = Path.GetDirectoryName(configSource);
            autoReplay.Checked = (bool?)jObject["AutoSaveReplay"] ?? true;
            replayMaximum.Value = Math.Max(1, Math.Min(1000, (int?)jObject["ReplayMaximum"] ?? 20));
            replayMaximum.Enabled = autoReplay.Checked;
            // Reset legacy speed settings once: the old patch missed an input path.
            var speed = (int?)jObject["ScrollSpeedRevision"] == 2 ? (int?)jObject["ScrollSpeedPercent"] ?? 10 : 10;
            var speedIndex = Array.IndexOf(ScrollRates, speed);
            scrollSpeed.SelectedIndex = speedIndex < 0 ? 1 : speedIndex;

            AtroxFolderPath = GameFiles.ResolvePath(jObject["AtroxFolderPath"].ToString(), configDirectory);
            JPakPath = GameFiles.ResolvePath(jObject["JPakPath"].ToString(), configDirectory);

            LinkLabel.Text = jObject["Hyperlink"][0].ToString();
            LinkLabelLink = jObject["Hyperlink"][1].ToString();

            // NoCdCheckBox.Enabled = bool.Parse(jObject["NoCD"][0].ToString());
            WindowModeCheckBox.Enabled = bool.Parse(jObject["WindowMode"][0].ToString());
            HighResolutionCheckBox.Enabled = bool.Parse(jObject["HighResolution"][0].ToString());
            // ParameterCheckBox.Enabled = bool.Parse(jObject["ReadMapData"][0].ToString());
            CustomPakCheckBox.Enabled = bool.Parse(jObject["CustomPak"][0].ToString());

            // NoCdCheckBox.CheckState = EnumParse<CheckState>(jObject["NoCD"][1].ToString());
            WindowModeCheckBox.CheckState = EnumParse<CheckState>(jObject["WindowMode"][1].ToString());
            HighResolutionCheckBox.CheckState = EnumParse<CheckState>(jObject["HighResolution"][1].ToString());
            // ParameterCheckBox.CheckState = EnumParse<CheckState>(jObject["ReadMapData"][1].ToString());
            CustomPakCheckBox.CheckState = EnumParse<CheckState>(jObject["CustomPak"][1].ToString());

            isInit = true;

            T EnumParse<T>(string value)
            {
                return (T)Enum.Parse(typeof(T), value);
            }
        }

        void WriteConfig(object sender = null, EventArgs e = null)
        {
            if (!isInit) return;

            var jObject = new JObject
            {
                { "AtroxFolderPath", AtroxFolderPath },
                { "JPakPath", JPakPath },
                { "ScrollSpeedPercent", ScrollRates[scrollSpeed.SelectedIndex] },
                { "ScrollSpeedRevision", 2 },
                { "AutoSaveReplay", autoReplay.Checked },
                { "ReplayMaximum", (int)replayMaximum.Value },
                { "Hyperlink", new JArray{ LinkLabel.Text, LinkLabelLink } },
                // { "NoCD", new JArray{ NoCdCheckBox.Enabled.ToString(), NoCdCheckBox.CheckState.ToString() } },
                { "WindowMode", new JArray{ WindowModeCheckBox.Enabled.ToString(), WindowModeCheckBox.CheckState.ToString() } },
                { "HighResolution", new JArray{ HighResolutionCheckBox.Enabled.ToString(), HighResolutionCheckBox.CheckState.ToString() } },
                // { "ReadMapData", new JArray{ ParameterCheckBox.Enabled.ToString(), ParameterCheckBox.CheckState.ToString() } },
                { "CustomPak", new JArray{ CustomPakCheckBox.Enabled.ToString(), CustomPakCheckBox.CheckState.ToString() } }
            };
            try { File.WriteAllText(ConfigPath + ".tmp", jObject.ToString());
                if (File.Exists(ConfigPath)) File.Replace(ConfigPath + ".tmp", ConfigPath, ConfigPath + ".previous");
                else File.Move(ConfigPath + ".tmp", ConfigPath);
            } catch (Exception ex) { MessageBox.Show("설정 저장 실패: " + ex.Message); }

        }

        void PathButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                SelectedPath = AtroxFolderPath
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                AtroxFolderPath = dialog.SelectedPath;
            }
        }

        void JPakPathButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = string.IsNullOrWhiteSpace(JPakPath) ? Application.StartupPath : Path.GetDirectoryName(Path.GetFullPath(JPakPath)),
                FileName = Path.GetFileName(JPakPath),
                Filter = "JPak file|*.jpak"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                JPakPath = dialog.FileName;
            }
        }

        void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(LinkLabelLink);
            }
            catch { }
        }

        void RunButton_Click(object sender, EventArgs e)
        {
            RunButton.Enabled = false;
            try
            {
                if (System.Diagnostics.Process.GetProcessesByName("Atrox").Length != 0)
                    throw new IOException("게임이 이미 실행 중입니다. 종료한 후 실행해 주세요.");
                var gameDirectory = Path.GetFullPath(AtroxFolderPath);
                if (!Directory.Exists(gameDirectory)) throw new DirectoryNotFoundException("게임 경로를 확인해 주세요.");
                var template = GameFiles.FindTemplate(gameDirectory);
                if (template == null) return;
                if (CustomPakCheckBox.Checked)
                {
                    if (!File.Exists(JPakPath)) throw new FileNotFoundException("JPak 경로를 확인해 주세요.");
                    InstallArchive(JPakPath, Path.Combine(gameDirectory, "atrox_pak"));
                }
                // Keep the user's previous executable; always patch a fresh supported template.
                if (File.Exists(AtroxPath) && !File.Exists(AtroxPath + ".original")) File.Copy(AtroxPath, AtroxPath + ".original");
                File.Copy(template, AtroxPath, true);
                PatchAtrox();
                GameRenderer.Install(gameDirectory, WindowModeCheckBox.Checked);
                ReplaySupport.Install(gameDirectory, autoReplay.Checked, (int)replayMaximum.Value);
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.GetFullPath(AtroxPath)) { WorkingDirectory = gameDirectory });
                if (process == null) throw new IOException("게임 프로세스를 시작하지 못했습니다.");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "실행 실패", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { RunButton.Enabled = true; }
        }

        internal static void InstallArchive(string archive, string destination)
        {
            destination = Path.GetFullPath(destination);
            var parent = Path.GetDirectoryName(destination);
            var staging = Path.Combine(parent, "atrox-stage-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                var root = staging + Path.DirectorySeparatorChar;
                using (var zip = ZipFile.OpenRead(archive))
                {
                    long total = 0;
                    foreach (var entry in zip.Entries)
                    {
                        var path = Path.GetFullPath(Path.Combine(staging, entry.FullName));
                        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("압축 파일에 잘못된 경로가 있습니다.");
                        total += entry.Length;
                        if (total > 2L * 1024 * 1024 * 1024) throw new InvalidDataException("압축 해제 용량이 너무 큽니다.");
                        if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(path); continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        entry.ExtractToFile(path);
                    }
                }
                var backup = destination + ".previous";
                if (Directory.Exists(backup)) throw new IOException("이전 백업 폴더가 있습니다. 내용을 확인하고 다른 위치로 옮겨 주세요: " + backup);
                if (Directory.Exists(destination)) Directory.Move(destination, backup);
                try { Directory.Move(staging, destination); }
                catch { if (Directory.Exists(backup) && !Directory.Exists(destination)) Directory.Move(backup, destination); throw; }
            }
            finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        }

        void PatchAtrox()
        {
            using (var stream = File.Open(AtroxPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                ReplaySupport.Apply(writer);
                ConstructionSupport.Apply(writer);
                GameplaySupport.Apply(writer);
                if (NoCdCheckBox.CheckState == CheckState.Checked)
                {
                    writer.Seek(0x000D6AC7, SeekOrigin.Begin);
                    writer.Write((byte)0x4B);
                }

                // Keep the game's native fullscreen DirectDraw path. cnc-ddraw owns window mode and scaling.

                if (HighResolutionCheckBox.CheckState == CheckState.Checked)
                {
                    writer.Seek(0x0001D11A, SeekOrigin.Begin);
                    writer.Write((byte)0xF1);

                    writer.Seek(0x0001D211, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x55, 0x03 });

                    writer.Seek(0x0001D26E, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x31, 0x03 });

                    writer.Seek(0x0001D278, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x7D, 0x03 });

                    writer.Seek(0x0001D2E3, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xC6, 0x02 });

                    writer.Seek(0x0001D2ED, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x8F, 0x03 });

                    writer.Seek(0x0001D95B, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x8B, 0x40, 0x24, 0x05, 0xA8, 0x01, 0x00, 0x00, 0x99, 0x2B, 0xC2, 0x8B, 0xF0, 0xD1, 0xFE, 0xE9, 0x87, 0x00, 0x00, 0x00, 0x90 });

                    writer.Seek(0x0001D9EC, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xE9, 0x6A, 0xFF, 0xFF, 0xFF, 0x90, 0x90, 0x90, 0x90, 0x90 });

                    writer.Seek(0x0002384F, SeekOrigin.Begin);
                    writer.Write((byte)0xF8);

                    writer.Seek(0x00023859, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xB3, 0x03 });

                    writer.Seek(0x00023863, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xE7, 0x02 });

                    writer.Seek(0x0002386D, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xD7, 0x03 });

                    writer.Seek(0x00023877, SeekOrigin.Begin);
                    writer.Write((byte)0xF6);

                    writer.Seek(0x00023881, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xD2, 0x03 });

                    writer.Seek(0x0002388B, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x1D, 0x03 });

                    writer.Seek(0x00023895, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x92, 0x03 });

                    writer.Seek(0x0002389F, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x0C, 0x04 });

                    writer.Seek(0x000238A9, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xA3, 0x03 });

                    writer.Seek(0x000DC862, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x00, 0x05 });

                    writer.Seek(0x000DC86E, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x00, 0x04 });

                    writer.Seek(0x000DC876, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x00, 0x05 });

                    writer.Seek(0x000DC87D, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xB0, 0x03 });
                }

                if (ParameterCheckBox.CheckState == CheckState.Checked)
                {
                    writer.Seek(0x0007A33A, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xBE, 0x04, 0x00, 0x00, 0x00, 0x90 });

                    writer.Seek(0x00084571, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xBE, 0x04, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90, 0x90 });

                    writer.Seek(0x000854CD, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x81, 0xE1, 0xFF, 0xFF, 0x00, 0x00, 0xBE, 0x04, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90, 0x90 });

                    writer.Seek(0x000D882C, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xE9, 0x31, 0x01, 0x00, 0x00, 0x90 });

                    writer.Seek(0x000D917E, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xE9, 0x59, 0x01, 0x00, 0x00, 0x90 });

                    writer.Seek(0x000D917E, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xE9, 0x59, 0x01, 0x00, 0x00, 0x90 });

                    writer.Seek(0x000D9776, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xE9, 0x19, 0x01, 0x00, 0x00, 0x90 });

                    writer.Seek(0x000DA0FE, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
                }

                if (CustomPakCheckBox.CheckState == CheckState.Checked)
                {
                    writer.Seek(0x000CCA6A, SeekOrigin.Begin);
                    writer.Write((byte)0xEB);

                    writer.Seek(0x000CCA80, SeekOrigin.Begin);
                    writer.Write((byte)0xEB);

                    var offsets = new int[]
                    {
                        0x0003FD9B, 0x0003FF1E, 0x00040BA7, 0x0006AC75, 0x00074142,
                        0x000BB13A, 0x000CA025, 0x000CA4A5, 0x000CAECD, 0x000CBB9C,
                        0x000CBC7C, 0x000D58B4, 0x000D6652, 0x000D6786, 0x000D8852,
                        0x000D979A, 0x0016E106, 0x001819D6,
                    };

                    foreach (var offset in offsets)
                    {
                        writer.Seek(offset, SeekOrigin.Begin);
                        writer.Write((byte)0x02);
                    }
                }
                GamePatches.Apply(writer, HighResolutionCheckBox.Checked, ScrollRates[scrollSpeed.SelectedIndex]);
            }
        }
    }
}
