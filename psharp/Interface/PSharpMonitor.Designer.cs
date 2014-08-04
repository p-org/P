namespace PSharpMonitor
{
    partial class PSharpMonitor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.AppChooseButton = new System.Windows.Forms.Button();
            this.AppNameTextBox = new System.Windows.Forms.TextBox();
            this.ApplicationPathLabel = new System.Windows.Forms.Label();
            this.TestButton = new System.Windows.Forms.Button();
            this.LoadManifestsButton = new System.Windows.Forms.Button();
            this.StatusBar = new System.Windows.Forms.StatusStrip();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ManifestPanel = new System.Windows.Forms.Panel();
            this.OptionsPanel = new System.Windows.Forms.Panel();
            this.StopAtBugCheckBox = new System.Windows.Forms.CheckBox();
            this.DebugCheckBox = new System.Windows.Forms.CheckBox();
            this.OpBoundLabel = new System.Windows.Forms.Label();
            this.OpBoundOption = new System.Windows.Forms.NumericUpDown();
            this.IterationsOption = new System.Windows.Forms.NumericUpDown();
            this.IterationsLabel = new System.Windows.Forms.Label();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.TestInfoTextBox = new System.Windows.Forms.RichTextBox();
            this.StopTestButton = new System.Windows.Forms.Button();
            this.RandomSchedRadioButton = new System.Windows.Forms.RadioButton();
            this.RoundRobinSchedRadioButton = new System.Windows.Forms.RadioButton();
            this.PCTSchedRadioButton = new System.Windows.Forms.RadioButton();
            this.SchedulerOptionsBox = new System.Windows.Forms.GroupBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.TestingOptionsBox = new System.Windows.Forms.GroupBox();
            this.StatusBar.SuspendLayout();
            this.ManifestPanel.SuspendLayout();
            this.OptionsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.OpBoundOption)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.IterationsOption)).BeginInit();
            this.SchedulerOptionsBox.SuspendLayout();
            this.panel1.SuspendLayout();
            this.TestingOptionsBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // AppChooseButton
            // 
            this.AppChooseButton.Location = new System.Drawing.Point(37, 109);
            this.AppChooseButton.Name = "AppChooseButton";
            this.AppChooseButton.Size = new System.Drawing.Size(102, 28);
            this.AppChooseButton.TabIndex = 2;
            this.AppChooseButton.Text = "Choose";
            this.AppChooseButton.UseVisualStyleBackColor = true;
            this.AppChooseButton.Click += new System.EventHandler(this.AppChooseButton_Click);
            // 
            // AppNameTextBox
            // 
            this.AppNameTextBox.Location = new System.Drawing.Point(7, 36);
            this.AppNameTextBox.Multiline = true;
            this.AppNameTextBox.Name = "AppNameTextBox";
            this.AppNameTextBox.ReadOnly = true;
            this.AppNameTextBox.Size = new System.Drawing.Size(368, 63);
            this.AppNameTextBox.TabIndex = 3;
            // 
            // ApplicationPathLabel
            // 
            this.ApplicationPathLabel.AutoSize = true;
            this.ApplicationPathLabel.Location = new System.Drawing.Point(3, 12);
            this.ApplicationPathLabel.Name = "ApplicationPathLabel";
            this.ApplicationPathLabel.Size = new System.Drawing.Size(196, 13);
            this.ApplicationPathLabel.TabIndex = 4;
            this.ApplicationPathLabel.Text = "Path to the Application\'s Executable File";
            // 
            // TestButton
            // 
            this.TestButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TestButton.BackColor = System.Drawing.SystemColors.Control;
            this.TestButton.FlatAppearance.BorderColor = System.Drawing.Color.Green;
            this.TestButton.Location = new System.Drawing.Point(402, 298);
            this.TestButton.Name = "TestButton";
            this.TestButton.Size = new System.Drawing.Size(276, 36);
            this.TestButton.TabIndex = 5;
            this.TestButton.Text = "Search for Bugs";
            this.TestButton.UseVisualStyleBackColor = false;
            this.TestButton.Click += new System.EventHandler(this.TestButton_Click);
            // 
            // LoadManifestsButton
            // 
            this.LoadManifestsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LoadManifestsButton.Location = new System.Drawing.Point(165, 109);
            this.LoadManifestsButton.Name = "LoadManifestsButton";
            this.LoadManifestsButton.Size = new System.Drawing.Size(195, 28);
            this.LoadManifestsButton.TabIndex = 9;
            this.LoadManifestsButton.Text = "Load Application";
            this.LoadManifestsButton.UseVisualStyleBackColor = true;
            this.LoadManifestsButton.Click += new System.EventHandler(this.LoadManifestsButton_Click);
            // 
            // StatusBar
            // 
            this.StatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabel});
            this.StatusBar.Location = new System.Drawing.Point(0, 344);
            this.StatusBar.Name = "StatusBar";
            this.StatusBar.Size = new System.Drawing.Size(762, 22);
            this.StatusBar.SizingGrip = false;
            this.StatusBar.TabIndex = 12;
            this.StatusBar.Text = "StatusBar";
            // 
            // StatusLabel
            // 
            this.StatusLabel.BackColor = System.Drawing.SystemColors.Control;
            this.StatusLabel.ForeColor = System.Drawing.Color.DarkGreen;
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // ManifestPanel
            // 
            this.ManifestPanel.BackColor = System.Drawing.SystemColors.Control;
            this.ManifestPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ManifestPanel.Controls.Add(this.AppChooseButton);
            this.ManifestPanel.Controls.Add(this.AppNameTextBox);
            this.ManifestPanel.Controls.Add(this.ApplicationPathLabel);
            this.ManifestPanel.Controls.Add(this.LoadManifestsButton);
            this.ManifestPanel.Location = new System.Drawing.Point(12, 12);
            this.ManifestPanel.Name = "ManifestPanel";
            this.ManifestPanel.Size = new System.Drawing.Size(384, 152);
            this.ManifestPanel.TabIndex = 13;
            // 
            // OptionsPanel
            // 
            this.OptionsPanel.BackColor = System.Drawing.SystemColors.Control;
            this.OptionsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.OptionsPanel.Controls.Add(this.TestingOptionsBox);
            this.OptionsPanel.Location = new System.Drawing.Point(12, 170);
            this.OptionsPanel.Name = "OptionsPanel";
            this.OptionsPanel.Size = new System.Drawing.Size(200, 164);
            this.OptionsPanel.TabIndex = 14;
            // 
            // StopAtBugCheckBox
            // 
            this.StopAtBugCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StopAtBugCheckBox.AutoSize = true;
            this.StopAtBugCheckBox.Location = new System.Drawing.Point(7, 127);
            this.StopAtBugCheckBox.Name = "StopAtBugCheckBox";
            this.StopAtBugCheckBox.Size = new System.Drawing.Size(81, 17);
            this.StopAtBugCheckBox.TabIndex = 28;
            this.StopAtBugCheckBox.Text = "Stop at bug";
            this.StopAtBugCheckBox.UseVisualStyleBackColor = true;
            this.StopAtBugCheckBox.CheckedChanged += new System.EventHandler(this.StopAtBugCheckBox_CheckedChanged);
            // 
            // DebugCheckBox
            // 
            this.DebugCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DebugCheckBox.AutoSize = true;
            this.DebugCheckBox.Location = new System.Drawing.Point(95, 127);
            this.DebugCheckBox.Name = "DebugCheckBox";
            this.DebugCheckBox.Size = new System.Drawing.Size(87, 17);
            this.DebugCheckBox.TabIndex = 27;
            this.DebugCheckBox.Text = "Debug mode";
            this.DebugCheckBox.UseVisualStyleBackColor = true;
            this.DebugCheckBox.CheckedChanged += new System.EventHandler(this.DebugCheckBox_CheckedChanged);
            // 
            // OpBoundLabel
            // 
            this.OpBoundLabel.AutoSize = true;
            this.OpBoundLabel.Location = new System.Drawing.Point(86, 78);
            this.OpBoundLabel.Name = "OpBoundLabel";
            this.OpBoundLabel.Size = new System.Drawing.Size(92, 13);
            this.OpBoundLabel.TabIndex = 22;
            this.OpBoundLabel.Text = "Operations Bound";
            // 
            // OpBoundOption
            // 
            this.OpBoundOption.Location = new System.Drawing.Point(10, 76);
            this.OpBoundOption.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.OpBoundOption.Name = "OpBoundOption";
            this.OpBoundOption.Size = new System.Drawing.Size(70, 20);
            this.OpBoundOption.TabIndex = 18;
            this.OpBoundOption.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.OpBoundOption.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.OpBoundOption.ValueChanged += new System.EventHandler(this.OpBoundOption_ValueChanged);
            // 
            // IterationsOption
            // 
            this.IterationsOption.Location = new System.Drawing.Point(10, 33);
            this.IterationsOption.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.IterationsOption.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.IterationsOption.Name = "IterationsOption";
            this.IterationsOption.Size = new System.Drawing.Size(70, 20);
            this.IterationsOption.TabIndex = 13;
            this.IterationsOption.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.IterationsOption.ThousandsSeparator = true;
            this.IterationsOption.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.IterationsOption.ValueChanged += new System.EventHandler(this.IterationsOption_ValueChanged);
            // 
            // IterationsLabel
            // 
            this.IterationsLabel.AutoSize = true;
            this.IterationsLabel.Location = new System.Drawing.Point(86, 35);
            this.IterationsLabel.Name = "IterationsLabel";
            this.IterationsLabel.Size = new System.Drawing.Size(50, 13);
            this.IterationsLabel.TabIndex = 12;
            this.IterationsLabel.Text = "Iterations";
            // 
            // ProgressBar
            // 
            this.ProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgressBar.Location = new System.Drawing.Point(402, 269);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(348, 23);
            this.ProgressBar.TabIndex = 16;
            // 
            // TestInfoTextBox
            // 
            this.TestInfoTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TestInfoTextBox.Location = new System.Drawing.Point(402, 12);
            this.TestInfoTextBox.Name = "TestInfoTextBox";
            this.TestInfoTextBox.ReadOnly = true;
            this.TestInfoTextBox.Size = new System.Drawing.Size(348, 251);
            this.TestInfoTextBox.TabIndex = 17;
            this.TestInfoTextBox.Text = "";
            // 
            // StopTestButton
            // 
            this.StopTestButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StopTestButton.BackColor = System.Drawing.SystemColors.Control;
            this.StopTestButton.Location = new System.Drawing.Point(684, 298);
            this.StopTestButton.Name = "StopTestButton";
            this.StopTestButton.Size = new System.Drawing.Size(66, 36);
            this.StopTestButton.TabIndex = 18;
            this.StopTestButton.Text = "Cancel";
            this.StopTestButton.UseVisualStyleBackColor = false;
            this.StopTestButton.Click += new System.EventHandler(this.StopTestButton_Click);
            // 
            // RandomSchedRadioButton
            // 
            this.RandomSchedRadioButton.AutoSize = true;
            this.RandomSchedRadioButton.Checked = true;
            this.RandomSchedRadioButton.Location = new System.Drawing.Point(22, 35);
            this.RandomSchedRadioButton.Name = "RandomSchedRadioButton";
            this.RandomSchedRadioButton.Size = new System.Drawing.Size(65, 17);
            this.RandomSchedRadioButton.TabIndex = 30;
            this.RandomSchedRadioButton.TabStop = true;
            this.RandomSchedRadioButton.Text = "Random";
            this.RandomSchedRadioButton.UseVisualStyleBackColor = true;
            this.RandomSchedRadioButton.CheckedChanged += new System.EventHandler(this.RandomSchedRadioButton_CheckedChanged);
            // 
            // RoundRobinSchedRadioButton
            // 
            this.RoundRobinSchedRadioButton.AutoSize = true;
            this.RoundRobinSchedRadioButton.Location = new System.Drawing.Point(22, 66);
            this.RoundRobinSchedRadioButton.Name = "RoundRobinSchedRadioButton";
            this.RoundRobinSchedRadioButton.Size = new System.Drawing.Size(88, 17);
            this.RoundRobinSchedRadioButton.TabIndex = 31;
            this.RoundRobinSchedRadioButton.Text = "Round Robin";
            this.RoundRobinSchedRadioButton.UseVisualStyleBackColor = true;
            this.RoundRobinSchedRadioButton.CheckedChanged += new System.EventHandler(this.RoundRobinSchedRadioButton_CheckedChanged);
            // 
            // PCTSchedRadioButton
            // 
            this.PCTSchedRadioButton.AutoSize = true;
            this.PCTSchedRadioButton.Location = new System.Drawing.Point(22, 95);
            this.PCTSchedRadioButton.Name = "PCTSchedRadioButton";
            this.PCTSchedRadioButton.Size = new System.Drawing.Size(46, 17);
            this.PCTSchedRadioButton.TabIndex = 32;
            this.PCTSchedRadioButton.Text = "PCT";
            this.PCTSchedRadioButton.UseVisualStyleBackColor = true;
            this.PCTSchedRadioButton.CheckedChanged += new System.EventHandler(this.PCTSchedRadioButton_CheckedChanged);
            // 
            // SchedulerOptionsBox
            // 
            this.SchedulerOptionsBox.Controls.Add(this.PCTSchedRadioButton);
            this.SchedulerOptionsBox.Controls.Add(this.RoundRobinSchedRadioButton);
            this.SchedulerOptionsBox.Controls.Add(this.RandomSchedRadioButton);
            this.SchedulerOptionsBox.Location = new System.Drawing.Point(3, 7);
            this.SchedulerOptionsBox.Name = "SchedulerOptionsBox";
            this.SchedulerOptionsBox.Size = new System.Drawing.Size(166, 152);
            this.SchedulerOptionsBox.TabIndex = 33;
            this.SchedulerOptionsBox.TabStop = false;
            this.SchedulerOptionsBox.Text = "Choose Scheduler";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.SchedulerOptionsBox);
            this.panel1.Location = new System.Drawing.Point(218, 170);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(178, 164);
            this.panel1.TabIndex = 29;
            // 
            // TestingOptionsBox
            // 
            this.TestingOptionsBox.Controls.Add(this.IterationsLabel);
            this.TestingOptionsBox.Controls.Add(this.StopAtBugCheckBox);
            this.TestingOptionsBox.Controls.Add(this.DebugCheckBox);
            this.TestingOptionsBox.Controls.Add(this.IterationsOption);
            this.TestingOptionsBox.Controls.Add(this.OpBoundOption);
            this.TestingOptionsBox.Controls.Add(this.OpBoundLabel);
            this.TestingOptionsBox.Location = new System.Drawing.Point(3, 7);
            this.TestingOptionsBox.Name = "TestingOptionsBox";
            this.TestingOptionsBox.Size = new System.Drawing.Size(192, 152);
            this.TestingOptionsBox.TabIndex = 30;
            this.TestingOptionsBox.TabStop = false;
            this.TestingOptionsBox.Text = "Configure Testing Options";
            // 
            // PSharpMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ClientSize = new System.Drawing.Size(762, 366);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.StopTestButton);
            this.Controls.Add(this.TestInfoTextBox);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.OptionsPanel);
            this.Controls.Add(this.ManifestPanel);
            this.Controls.Add(this.StatusBar);
            this.Controls.Add(this.TestButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "PSharpMonitor";
            this.ShowIcon = false;
            this.Text = "P# Monitor - An interface for testing P# applications";
            this.Load += new System.EventHandler(this.PSharpMonitor_Load);
            this.StatusBar.ResumeLayout(false);
            this.StatusBar.PerformLayout();
            this.ManifestPanel.ResumeLayout(false);
            this.ManifestPanel.PerformLayout();
            this.OptionsPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.OpBoundOption)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.IterationsOption)).EndInit();
            this.SchedulerOptionsBox.ResumeLayout(false);
            this.SchedulerOptionsBox.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.TestingOptionsBox.ResumeLayout(false);
            this.TestingOptionsBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button AppChooseButton;
        private System.Windows.Forms.TextBox AppNameTextBox;
        private System.Windows.Forms.Label ApplicationPathLabel;
        private System.Windows.Forms.Button TestButton;
        private System.Windows.Forms.Button LoadManifestsButton;
        private System.Windows.Forms.StatusStrip StatusBar;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private System.Windows.Forms.Panel ManifestPanel;
        private System.Windows.Forms.Panel OptionsPanel;
        private System.Windows.Forms.Label IterationsLabel;
        private System.Windows.Forms.NumericUpDown IterationsOption;
        private System.Windows.Forms.NumericUpDown OpBoundOption;
        private System.Windows.Forms.Label OpBoundLabel;
        private System.Windows.Forms.CheckBox DebugCheckBox;
        private System.Windows.Forms.CheckBox StopAtBugCheckBox;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.RichTextBox TestInfoTextBox;
        private System.Windows.Forms.Button StopTestButton;
        private System.Windows.Forms.RadioButton RandomSchedRadioButton;
        private System.Windows.Forms.RadioButton RoundRobinSchedRadioButton;
        private System.Windows.Forms.RadioButton PCTSchedRadioButton;
        private System.Windows.Forms.GroupBox SchedulerOptionsBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox TestingOptionsBox;
    }
}