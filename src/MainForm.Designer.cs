namespace AtroxLauncher
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.PathTextBox = new System.Windows.Forms.TextBox();
            this.PathButton = new System.Windows.Forms.Button();
            this.PathLabel = new System.Windows.Forms.Label();
            this.RunButton = new System.Windows.Forms.Button();
            this.NoCdCheckBox = new System.Windows.Forms.CheckBox();
            this.OptionLabel = new System.Windows.Forms.Label();
            this.WindowModeCheckBox = new System.Windows.Forms.CheckBox();
            this.HighResolutionCheckBox = new System.Windows.Forms.CheckBox();
            this.ParameterCheckBox = new System.Windows.Forms.CheckBox();
            this.CustomPakCheckBox = new System.Windows.Forms.CheckBox();
            this.JPakPathLabel = new System.Windows.Forms.Label();
            this.JPakPathButton = new System.Windows.Forms.Button();
            this.JPakPathTextBox = new System.Windows.Forms.TextBox();
            this.CreditLabel = new System.Windows.Forms.Label();
            this.LinkLabel = new System.Windows.Forms.LinkLabel();
            this.ScenarioListBox = new System.Windows.Forms.ListBox();
            this.ScenarioLabel = new System.Windows.Forms.Label();
            this.OverlayInfoLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // PathTextBox
            // 
            this.PathTextBox.Location = new System.Drawing.Point(81, 11);
            this.PathTextBox.Name = "PathTextBox";
            this.PathTextBox.Size = new System.Drawing.Size(375, 21);
            this.PathTextBox.TabIndex = 0;
            this.PathTextBox.Text = "C:\\Program Files\\Joymax\\Atrox";
            this.PathTextBox.TextChanged += new System.EventHandler(this.WriteConfig);
            // 
            // PathButton
            // 
            this.PathButton.Location = new System.Drawing.Point(462, 11);
            this.PathButton.Name = "PathButton";
            this.PathButton.Size = new System.Drawing.Size(34, 21);
            this.PathButton.TabIndex = 1;
            this.PathButton.Text = "...";
            this.PathButton.UseVisualStyleBackColor = true;
            this.PathButton.Click += new System.EventHandler(this.PathButton_Click);
            // 
            // PathLabel
            // 
            this.PathLabel.AutoSize = true;
            this.PathLabel.Location = new System.Drawing.Point(12, 16);
            this.PathLabel.Name = "PathLabel";
            this.PathLabel.Size = new System.Drawing.Size(61, 12);
            this.PathLabel.TabIndex = 2;
            this.PathLabel.Text = "게임 경로:";
            // 
            // RunButton
            // 
            this.RunButton.Location = new System.Drawing.Point(245, 250);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(150, 50);
            this.RunButton.TabIndex = 3;
            this.RunButton.Text = "실행";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // NoCdCheckBox
            // 
            this.NoCdCheckBox.AutoSize = true;
            this.NoCdCheckBox.Checked = true;
            this.NoCdCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.NoCdCheckBox.Location = new System.Drawing.Point(14, 167);
            this.NoCdCheckBox.Name = "NoCdCheckBox";
            this.NoCdCheckBox.Size = new System.Drawing.Size(60, 16);
            this.NoCdCheckBox.TabIndex = 4;
            this.NoCdCheckBox.Text = "노시디";
            this.NoCdCheckBox.UseVisualStyleBackColor = true;
            this.NoCdCheckBox.Visible = false;
            this.NoCdCheckBox.CheckedChanged += new System.EventHandler(this.WriteConfig);
            // 
            // OptionLabel
            // 
            this.OptionLabel.AutoSize = true;
            this.OptionLabel.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.OptionLabel.Location = new System.Drawing.Point(12, 74);
            this.OptionLabel.Name = "OptionLabel";
            this.OptionLabel.Size = new System.Drawing.Size(31, 12);
            this.OptionLabel.TabIndex = 5;
            this.OptionLabel.Text = "옵션";
            // 
            // WindowModeCheckBox
            // 
            this.WindowModeCheckBox.AutoSize = true;
            this.WindowModeCheckBox.Checked = true;
            this.WindowModeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.WindowModeCheckBox.Location = new System.Drawing.Point(14, 101);
            this.WindowModeCheckBox.Name = "WindowModeCheckBox";
            this.WindowModeCheckBox.Size = new System.Drawing.Size(360, 16);
            this.WindowModeCheckBox.TabIndex = 6;
            this.WindowModeCheckBox.Text = "창모드 (800X600 에러 발생시 게임 경로서 1회 직접 실행 바람)";
            this.WindowModeCheckBox.UseVisualStyleBackColor = true;
            this.WindowModeCheckBox.CheckedChanged += new System.EventHandler(this.WriteConfig);
            // 
            // HighResolutionCheckBox
            // 
            this.HighResolutionCheckBox.AutoSize = true;
            this.HighResolutionCheckBox.Checked = true;
            this.HighResolutionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.HighResolutionCheckBox.Location = new System.Drawing.Point(14, 123);
            this.HighResolutionCheckBox.Name = "HighResolutionCheckBox";
            this.HighResolutionCheckBox.Size = new System.Drawing.Size(119, 16);
            this.HighResolutionCheckBox.TabIndex = 7;
            this.HighResolutionCheckBox.Text = "1280x1024 해상도";
            this.HighResolutionCheckBox.UseVisualStyleBackColor = true;
            this.HighResolutionCheckBox.CheckedChanged += new System.EventHandler(this.WriteConfig);
            // 
            // ParameterCheckBox
            // 
            this.ParameterCheckBox.AutoSize = true;
            this.ParameterCheckBox.Location = new System.Drawing.Point(14, 189);
            this.ParameterCheckBox.Name = "ParameterCheckBox";
            this.ParameterCheckBox.Size = new System.Drawing.Size(210, 16);
            this.ParameterCheckBox.TabIndex = 8;
            this.ParameterCheckBox.Text = "맵 정보 읽기 (유닛배치, 파라미터)";
            this.ParameterCheckBox.UseVisualStyleBackColor = true;
            this.ParameterCheckBox.Visible = false;
            this.ParameterCheckBox.CheckedChanged += new System.EventHandler(this.WriteConfig);
            // 
            // CustomPakCheckBox
            // 
            this.CustomPakCheckBox.AutoSize = true;
            this.CustomPakCheckBox.Checked = true;
            this.CustomPakCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CustomPakCheckBox.Location = new System.Drawing.Point(14, 145);
            this.CustomPakCheckBox.Name = "CustomPakCheckBox";
            this.CustomPakCheckBox.Size = new System.Drawing.Size(233, 16);
            this.CustomPakCheckBox.TabIndex = 9;
            this.CustomPakCheckBox.Text = "사용자 정의 아트록스 파일(JPak) 읽기";
            this.CustomPakCheckBox.UseVisualStyleBackColor = true;
            this.CustomPakCheckBox.CheckedChanged += new System.EventHandler(this.WriteConfig);
            // 
            // JPakPathLabel
            // 
            this.JPakPathLabel.AutoSize = true;
            this.JPakPathLabel.Location = new System.Drawing.Point(6, 42);
            this.JPakPathLabel.Name = "JPakPathLabel";
            this.JPakPathLabel.Size = new System.Drawing.Size(64, 12);
            this.JPakPathLabel.TabIndex = 12;
            this.JPakPathLabel.Text = "JPak 경로:";
            // 
            // JPakPathButton
            // 
            this.JPakPathButton.Location = new System.Drawing.Point(462, 38);
            this.JPakPathButton.Name = "JPakPathButton";
            this.JPakPathButton.Size = new System.Drawing.Size(34, 21);
            this.JPakPathButton.TabIndex = 11;
            this.JPakPathButton.Text = "...";
            this.JPakPathButton.UseVisualStyleBackColor = true;
            this.JPakPathButton.Click += new System.EventHandler(this.JPakPathButton_Click);
            // 
            // JPakPathTextBox
            // 
            this.JPakPathTextBox.Location = new System.Drawing.Point(81, 38);
            this.JPakPathTextBox.Name = "JPakPathTextBox";
            this.JPakPathTextBox.Size = new System.Drawing.Size(375, 21);
            this.JPakPathTextBox.TabIndex = 10;
            this.JPakPathTextBox.Text = "C:\\Program Files\\Joymax\\AtroxLauncher\\Atrox.jpak";
            this.JPakPathTextBox.TextChanged += new System.EventHandler(this.WriteConfig);
            // 
            // CreditLabel
            // 
            this.CreditLabel.AutoSize = true;
            this.CreditLabel.Location = new System.Drawing.Point(454, 288);
            this.CreditLabel.Name = "CreditLabel";
            this.CreditLabel.Size = new System.Drawing.Size(158, 12);
            this.CreditLabel.TabIndex = 13;
            this.CreditLabel.Text = "by. wnsrn3436@gmail.com";
            // 
            // LinkLabel
            // 
            this.LinkLabel.AutoSize = true;
            this.LinkLabel.Location = new System.Drawing.Point(12, 288);
            this.LinkLabel.Name = "LinkLabel";
            this.LinkLabel.Size = new System.Drawing.Size(133, 12);
            this.LinkLabel.TabIndex = 14;
            this.LinkLabel.TabStop = true;
            this.LinkLabel.Text = "아트록스 카페 바로가기";
            this.LinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel_LinkClicked);
            // 
            // ScenarioListBox
            // 
            this.ScenarioListBox.FormattingEnabled = true;
            this.ScenarioListBox.ItemHeight = 12;
            this.ScenarioListBox.Location = new System.Drawing.Point(393, 101);
            this.ScenarioListBox.Name = "ScenarioListBox";
            this.ScenarioListBox.Size = new System.Drawing.Size(219, 136);
            this.ScenarioListBox.TabIndex = 15;
            this.ScenarioListBox.SelectedValueChanged += new System.EventHandler(this.WriteConfig);
            // 
            // ScenarioLabel
            // 
            this.ScenarioLabel.AutoSize = true;
            this.ScenarioLabel.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.ScenarioLabel.Location = new System.Drawing.Point(393, 74);
            this.ScenarioLabel.Name = "ScenarioLabel";
            this.ScenarioLabel.Size = new System.Drawing.Size(132, 12);
            this.ScenarioLabel.TabIndex = 16;
            this.ScenarioLabel.Text = "사용자 정의 시나리오";
            // 
            // OverlayInfoLabel
            // 
            this.OverlayInfoLabel.AutoSize = true;
            this.OverlayInfoLabel.Location = new System.Drawing.Point(402, 269);
            this.OverlayInfoLabel.Name = "OverlayInfoLabel";
            this.OverlayInfoLabel.Size = new System.Drawing.Size(210, 12);
            this.OverlayInfoLabel.TabIndex = 17;
            this.OverlayInfoLabel.Text = "인터페이스 깨짐 해결(게임 중! F11키)";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 311);
            this.Controls.Add(this.OverlayInfoLabel);
            this.Controls.Add(this.ScenarioLabel);
            this.Controls.Add(this.ScenarioListBox);
            this.Controls.Add(this.LinkLabel);
            this.Controls.Add(this.CreditLabel);
            this.Controls.Add(this.JPakPathLabel);
            this.Controls.Add(this.JPakPathButton);
            this.Controls.Add(this.JPakPathTextBox);
            this.Controls.Add(this.CustomPakCheckBox);
            this.Controls.Add(this.ParameterCheckBox);
            this.Controls.Add(this.HighResolutionCheckBox);
            this.Controls.Add(this.WindowModeCheckBox);
            this.Controls.Add(this.OptionLabel);
            this.Controls.Add(this.NoCdCheckBox);
            this.Controls.Add(this.RunButton);
            this.Controls.Add(this.PathLabel);
            this.Controls.Add(this.PathButton);
            this.Controls.Add(this.PathTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(640, 350);
            this.MinimumSize = new System.Drawing.Size(640, 350);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AtroxLauncher v1.2";
            this.Click += new System.EventHandler(this.MainForm_Click);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox PathTextBox;
        private System.Windows.Forms.Button PathButton;
        private System.Windows.Forms.Label PathLabel;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.CheckBox NoCdCheckBox;
        private System.Windows.Forms.Label OptionLabel;
        private System.Windows.Forms.CheckBox WindowModeCheckBox;
        private System.Windows.Forms.CheckBox HighResolutionCheckBox;
        private System.Windows.Forms.CheckBox ParameterCheckBox;
        private System.Windows.Forms.CheckBox CustomPakCheckBox;
        private System.Windows.Forms.Label JPakPathLabel;
        private System.Windows.Forms.Button JPakPathButton;
        private System.Windows.Forms.TextBox JPakPathTextBox;
        private System.Windows.Forms.Label CreditLabel;
        private System.Windows.Forms.LinkLabel LinkLabel;
        private System.Windows.Forms.ListBox ScenarioListBox;
        private System.Windows.Forms.Label ScenarioLabel;
        private System.Windows.Forms.Label OverlayInfoLabel;
    }
}

