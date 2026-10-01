using System.Drawing;
using System.Windows.Forms;

namespace AtroxLauncher
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components;
        private TextBox PathTextBox, JPakPathTextBox;
        private Button PathButton, JPakPathButton, RunButton, updateButton;
        private CheckBox NoCdCheckBox, ParameterCheckBox, WindowModeCheckBox, HighResolutionCheckBox, CustomPakCheckBox;
        private LinkLabel LinkLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("맑은 고딕", 9F);
            BackColor = Color.FromArgb(17, 23, 32);
            ForeColor = Color.FromArgb(229, 236, 244);
            ClientSize = new Size(640, 620);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Text = "AtroxLauncher";

            var accent = Color.FromArgb(86, 222, 196);
            var muted = Color.FromArgb(150, 167, 185);
            Label title = Caption("ATROX", 28, 21, 300, 46);
            title.Font = new Font("Segoe UI", 27F, FontStyle.Bold);
            title.ForeColor = accent;
            Caption("게임 런처", 31, 72, 220, 24).ForeColor = muted;
            var version = Caption("v" + LauncherUpdate.CurrentVersion.ToString(3), 492, 42, 120, 24);
            version.TextAlign = ContentAlignment.MiddleRight;
            version.ForeColor = muted;

            Caption("게임 폴더", 28, 118, 180, 22);
            PathTextBox = PathField("C:\\Program Files\\Joymax\\Atrox", 28, 146, 480);
            PathTextBox.TabIndex = 0;
            PathTextBox.TextChanged += WriteConfig;
            PathButton = ActionButton("찾아보기", 520, 143, 92, 32);
            PathButton.TabIndex = 1;
            PathButton.Click += PathButton_Click;

            var options = new Panel { Location = new Point(28, 195), Size = new Size(584, 132), BackColor = Color.FromArgb(26, 35, 47) };
            Controls.Add(options);
            WindowModeCheckBox = Option("창 모드로 시작", 18, 16, options);
            WindowModeCheckBox.TabIndex = 0;
            HighResolutionCheckBox = Option("1280 × 1024 해상도", 300, 16, options);
            HighResolutionCheckBox.TabIndex = 1;
            options.Controls.Add(new Label { Text = "Alt + Enter로 전체화면 전환", Location = new Point(18, 47), Size = new Size(530, 22), ForeColor = muted });
            options.Controls.Add(new Label { Text = "화면 이동 속도", Location = new Point(18, 89), Size = new Size(145, 24) });
            scrollSpeed.SetBounds(300, 85, 265, 28);
            scrollSpeed.TabIndex = 2;
            scrollSpeed.FlatStyle = FlatStyle.Flat;
            scrollSpeed.BackColor = Color.FromArgb(39, 51, 66);
            scrollSpeed.ForeColor = ForeColor;
            options.Controls.Add(scrollSpeed);
            options.TabIndex = 2;

            var replayPanel = new Panel { Location = new Point(28, 340), Size = new Size(584, 98), BackColor = Color.FromArgb(26, 35, 47), TabIndex = 3 };
            Controls.Add(replayPanel);
            autoReplay.SetBounds(18, 12, 265, 28);
            replayPanel.Controls.Add(autoReplay);
            replayPanel.Controls.Add(new Label { Text = "최대", Location = new Point(300, 17), Size = new Size(45, 24) });
            replayMaximum.SetBounds(347, 13, 92, 28);
            replayMaximum.BackColor = Color.FromArgb(39, 51, 66);
            replayMaximum.ForeColor = ForeColor;
            replayPanel.Controls.Add(replayMaximum);
            replayPanel.Controls.Add(new Label { Text = "개 보관", Location = new Point(450, 17), Size = new Size(95, 24) });
            replayPanel.Controls.Add(new Label { Text = "경기 종료 시 저장 · 오래된 자동 저장 파일부터 정리\n수동 저장 파일은 유지 · 리플레이는 양쪽 시야로 재생", Location = new Point(18, 49), Size = new Size(548, 43), ForeColor = muted });

            CustomPakCheckBox = Option("사용자 정의 JPak 사용", 28, 455, this);
            CustomPakCheckBox.Checked = false;
            CustomPakCheckBox.TabIndex = 3;
            JPakPathTextBox = PathField("C:\\Program Files\\Joymax\\AtroxLauncher\\Atrox.jpak", 28, 490, 480);
            JPakPathTextBox.TabIndex = 4;
            JPakPathTextBox.TextChanged += WriteConfig;
            JPakPathButton = ActionButton("찾아보기", 520, 487, 92, 32);
            JPakPathButton.TabIndex = 5;
            JPakPathButton.Click += JPakPathButton_Click;

            LinkLabel = new LinkLabel { Text = "아트록스 카페 바로가기", Location = new Point(28, 534), Size = new Size(270, 22), LinkColor = muted, ActiveLinkColor = accent, VisitedLinkColor = muted, TabIndex = 6 };
            LinkLabel.LinkClicked += LinkLabel_LinkClicked;
            Controls.Add(LinkLabel);
            updateButton = ActionButton("런처 업데이트", 28, 568, 190, 36);
            updateButton.TabIndex = 7;
            RunButton = ActionButton("게임 시작  →", 238, 562, 374, 42);
            RunButton.BackColor = accent;
            RunButton.ForeColor = Color.FromArgb(12, 34, 34);
            RunButton.Font = new Font(Font, FontStyle.Bold);
            RunButton.TabIndex = 8;
            RunButton.Click += RunButton_Click;
            NoCdCheckBox = new CheckBox { Checked = true };
            ParameterCheckBox = new CheckBox();
            ResumeLayout(false);
            PerformLayout();
        }

        private Label Caption(string text, int x, int y, int width, int height)
        {
            var label = new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height) };
            Controls.Add(label);
            return label;
        }

        private TextBox PathField(string text, int x, int y, int width)
        {
            var field = new TextBox { Text = text, Location = new Point(x, y), Size = new Size(width, 28), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(26, 35, 47), ForeColor = ForeColor };
            Controls.Add(field);
            return field;
        }

        private Button ActionButton(string text, int x, int y, int width, int height)
        {
            var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(39, 51, 66), ForeColor = ForeColor, Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = 0;
            Controls.Add(button);
            return button;
        }

        private CheckBox Option(string text, int x, int y, Control parent)
        {
            var check = new CheckBox { Text = text, Location = new Point(x, y), Size = new Size(265, 28), Checked = true };
            check.CheckedChanged += WriteConfig;
            parent.Controls.Add(check);
            return check;
        }
    }
}
