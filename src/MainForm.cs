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

        string ScenarioFolderPath => "CustomScenario";
        string ConfigPath => "Config.json";
        string LinkLabelLink = "https://cafe.naver.com/atroxs";
        bool isInit;

        public MainForm()
        {
            InitializeComponent();

            if (Directory.Exists(ScenarioFolderPath))
            {
                foreach (var fileName in Directory.GetFiles(ScenarioFolderPath, "*.zip", SearchOption.TopDirectoryOnly).Select(t => Path.GetFileNameWithoutExtension(t)))
                {
                    ScenarioListBox.Items.Add(fileName);
                }
            }

            ReadConfig();
        }

        void ReadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                isInit = true;
                return;
            }

            var jObject = JObject.Parse(string.Join(string.Empty, File.ReadAllLines(ConfigPath).Select(t => t)));

            AtroxFolderPath = jObject["AtroxFolderPath"].ToString();
            JPakPath = jObject["JPakPath"].ToString();

            var target = jObject["CustomScenario"].ToString();
            foreach (var item in ScenarioListBox.Items)
            {
                if (item.ToString() == target)
                {
                    ScenarioListBox.SelectedItem = item;
                    break;
                }
            }

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

            ScenarioLabel.Enabled = CustomPakCheckBox.CheckState == CheckState.Checked;
            ScenarioListBox.Enabled = CustomPakCheckBox.CheckState == CheckState.Checked;
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
                { "CustomScenario", ScenarioListBox.SelectedItem?.ToString() ?? "" },
                { "Hyperlink", new JArray{ LinkLabel.Text, LinkLabelLink } },
                // { "NoCD", new JArray{ NoCdCheckBox.Enabled.ToString(), NoCdCheckBox.CheckState.ToString() } },
                { "WindowMode", new JArray{ WindowModeCheckBox.Enabled.ToString(), WindowModeCheckBox.CheckState.ToString() } },
                { "HighResolution", new JArray{ HighResolutionCheckBox.Enabled.ToString(), HighResolutionCheckBox.CheckState.ToString() } },
                // { "ReadMapData", new JArray{ ParameterCheckBox.Enabled.ToString(), ParameterCheckBox.CheckState.ToString() } },
                { "CustomPak", new JArray{ CustomPakCheckBox.Enabled.ToString(), CustomPakCheckBox.CheckState.ToString() } }
            };
            File.WriteAllText(ConfigPath, jObject.ToString());

            ScenarioLabel.Enabled = CustomPakCheckBox.CheckState == CheckState.Checked;
            ScenarioListBox.Enabled = CustomPakCheckBox.CheckState == CheckState.Checked;
        }

        void MainForm_Click(object sender, EventArgs e)
        {
            ScenarioListBox.ClearSelected();
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
                InitialDirectory = Path.GetFullPath(JPakPath),
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
            if (CustomPakCheckBox.CheckState == CheckState.Checked)
            {
                if (Directory.Exists(JPakPathExtractPath))
                {
                    Directory.Delete(JPakPathExtractPath, true);
                }
                ZipFile.ExtractToDirectory(JPakPath, JPakPathExtractPath);
            }

            var mapsFolderPath = $@"{JPakPathExtractPath}\maps\scenario";
            if (Directory.Exists(mapsFolderPath) && ScenarioListBox.Enabled && ScenarioListBox.SelectedItem != null)
            {
                Directory.Delete(mapsFolderPath, true);
                ZipFile.ExtractToDirectory($@"{ScenarioFolderPath}\{ScenarioListBox.SelectedItem.ToString()}.zip", mapsFolderPath);
            }

            File.Copy("Atrox.ex_", AtroxPath, true);
            PatchAtrox();
            try
            {
                var process = System.Diagnostics.Process.Start(AtroxPath);
                process.EnableRaisingEvents = true;
                process.Exited += (s, args) => OnGameEnd();
            }
            catch
            {
                OnGameEnd();
            }

            void OnGameEnd()
            {
                //if (Directory.Exists(JPakPathExtractPath))
                //{
                //    Directory.Delete(JPakPathExtractPath, true);
                //}
                //File.Delete(AtroxPath);
            }
        }

        void PatchAtrox()
        {
            using (var stream = File.OpenWrite(AtroxPath))
            using (var writer = new BinaryWriter(stream))
            {
                if (NoCdCheckBox.CheckState == CheckState.Checked)
                {
                    writer.Seek(0x000D6AC7, SeekOrigin.Begin);
                    writer.Write((byte)0x4B);
                }

                if (WindowModeCheckBox.CheckState == CheckState.Checked)
                {
                    writer.Seek(0x000894EB, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x50, 0xE8, 0xE1, 0x80, 0xF7, 0xFF, 0xEB, 0x32 });

                    writer.Seek(0x00089521, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xEB, 0xC8, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });

                    writer.Seek(0x000DC497, SeekOrigin.Begin);
                    writer.Write((byte)0x00);

                    writer.Seek(0x0017C626, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xE9, 0xC1, 0x00, 0x00, 0x00, 0x90 });
                }

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

                    writer.Seek(0x0002D4C0, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x90, 0x90, 0x90 });

                    writer.Seek(0x0002D477, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xEB, 0x3C, 0x90, 0x90, 0x90, 0x90 });

                    writer.Seek(0x0002D481, SeekOrigin.Begin);
                    writer.Write((byte)0x90);

                    writer.Seek(0x0002D484, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xEB, 0x3A, 0x90, 0x90, 0x90 });

                    writer.Seek(0x0002D4B8, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0x56, 0xE8, 0xBC, 0x9D, 0xFD, 0xFF, 0xEB, 0xBD, 0x90, 0x90, 0x90, 0x53, 0xE8, 0x4B, 0x4F, 0xFD, 0xFF, 0xEB, 0xBE, 0x90, 0x90, 0x90, 0x90, 0x90 });

                    writer.Seek(0x000894EB, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x50, 0xE8, 0xE1, 0x80, 0xF7, 0xFF, 0xEB, 0x32 });

                    writer.Seek(0x00089521, SeekOrigin.Begin);
                    writer.Write(new byte[] { 0xEB, 0xC8, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });

                    writer.Seek(0x000DC497, SeekOrigin.Begin);
                    writer.Write((byte)0x00);

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
            }
        }
    }
}
