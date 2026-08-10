namespace Codec_Playground_H
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            groupBoxEncoders = new GroupBox();
            listViewEncoders = new ListView();
            columnHeaderEncodersName = new ColumnHeader();
            columnHeaderEncodersVersion = new ColumnHeader();
            columnHeaderEncodersDirectory = new ColumnHeader();
            splitContainerEncodersAndAudioFlies = new SplitContainer();
            groupBoxAudioFiles = new GroupBox();
            listViewAudioFiles = new ListView();
            columnHeaderAudioFilesName = new ColumnHeader();
            columnHeaderAudioFilesChannels = new ColumnHeader();
            columnHeaderAudioFilesBitDepth = new ColumnHeader();
            columnHeaderAudioFilesSamplingRate = new ColumnHeader();
            columnHeaderAudioFilesDuration = new ColumnHeader();
            columnHeaderAudioFilesDirectory = new ColumnHeader();
            groupBoxEncoderSettings = new GroupBox();
            radioButton_Hidden_ModeMP3_OFF = new RadioButton();
            radioButtonModeCBR = new RadioButton();
            radioButtonModeABR = new RadioButton();
            radioButtonModeVBR = new RadioButton();
            trackBarVBR = new TrackBar();
            trackBarABR = new TrackBar();
            trackBarCBR = new TrackBar();
            labelCBRValue = new Label();
            labelABRValue = new Label();
            labelVBRValue = new Label();
            panelAdditionalOptions_1 = new Panel();
            checkBoxParameter_q = new CheckBox();
            labelParameter_qValue = new Label();
            checkBoxChannelsModes = new CheckBox();
            radioButtonJointStereo = new RadioButton();
            radioButtonStereo = new RadioButton();
            radioButtonMono = new RadioButton();
            trackBarParameter_q = new TrackBar();
            buttonSaveUserPreset6 = new Button();
            buttonSaveUserPreset5 = new Button();
            buttonSaveUserPreset4 = new Button();
            buttonSaveUserPreset3 = new Button();
            buttonSaveUserPreset2 = new Button();
            buttonSaveUserPreset1 = new Button();
            textBoxUserPreset6 = new TextBox();
            textBoxUserPreset4 = new TextBox();
            textBoxUserPreset5 = new TextBox();
            textBoxUserPreset3 = new TextBox();
            textBoxUserPreset2 = new TextBox();
            textBoxUserPreset1 = new TextBox();
            radioButtonUserPreset6 = new RadioButton();
            radioButtonUserPreset4 = new RadioButton();
            buttonUserPreset6Clear = new Button();
            buttonUserPreset4Clear = new Button();
            radioButtonUserPreset5 = new RadioButton();
            buttonUserPreset5Clear = new Button();
            radioButtonUserPreset3 = new RadioButton();
            buttonUserPreset3Clear = new Button();
            radioButtonUserPreset2 = new RadioButton();
            radioButtonUserPreset1 = new RadioButton();
            buttonUserPreset2Clear = new Button();
            buttonUserPreset1Clear = new Button();
            labelMixBalance = new Label();
            trackBarMixBalance = new TrackBar();
            radioButtonPlayMix = new RadioButton();
            progressBarEncodingProcess = new ProgressBar();
            buttonLoopPlayback = new Button();
            radioButtonPlayDifference = new RadioButton();
            radioButtonPlayEncoded = new RadioButton();
            radioButtonPlayOriginal = new RadioButton();
            buttonPlayPause = new Button();
            buttonStop = new Button();
            buttonClearEncoders = new Button();
            buttonClearAudioFiles = new Button();
            trackBarSeek = new TrackBar();
            toolTip1 = new ToolTip(components);
            checkBoxCheckForUpdates = new CheckBox();
            timerTrackBarSeek = new System.Windows.Forms.Timer(components);
            tableLayoutPanelMain = new TableLayoutPanel();
            groupBoxPlayerControl = new GroupBox();
            panelSettings = new Panel();
            groupBox1 = new GroupBox();
            labelNoUpdates = new Label();
            radioButton_Hidden_UserPreset_OFF = new RadioButton();
            groupBoxEncoders.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerEncodersAndAudioFlies).BeginInit();
            splitContainerEncodersAndAudioFlies.Panel1.SuspendLayout();
            splitContainerEncodersAndAudioFlies.Panel2.SuspendLayout();
            splitContainerEncodersAndAudioFlies.SuspendLayout();
            groupBoxAudioFiles.SuspendLayout();
            groupBoxEncoderSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarVBR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarABR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarCBR).BeginInit();
            panelAdditionalOptions_1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarParameter_q).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarMixBalance).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarSeek).BeginInit();
            tableLayoutPanelMain.SuspendLayout();
            groupBoxPlayerControl.SuspendLayout();
            panelSettings.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoders
            // 
            groupBoxEncoders.Controls.Add(listViewEncoders);
            groupBoxEncoders.Dock = DockStyle.Fill;
            groupBoxEncoders.Location = new Point(0, 0);
            groupBoxEncoders.Name = "groupBoxEncoders";
            groupBoxEncoders.Size = new Size(423, 165);
            groupBoxEncoders.TabIndex = 0;
            groupBoxEncoders.TabStop = false;
            groupBoxEncoders.Text = "Encoders";
            // 
            // listViewEncoders
            // 
            listViewEncoders.AllowDrop = true;
            listViewEncoders.CheckBoxes = true;
            listViewEncoders.Columns.AddRange(new ColumnHeader[] { columnHeaderEncodersName, columnHeaderEncodersVersion, columnHeaderEncodersDirectory });
            listViewEncoders.Dock = DockStyle.Fill;
            listViewEncoders.FullRowSelect = true;
            listViewEncoders.Location = new Point(3, 19);
            listViewEncoders.MultiSelect = false;
            listViewEncoders.Name = "listViewEncoders";
            listViewEncoders.Size = new Size(417, 143);
            listViewEncoders.TabIndex = 0;
            listViewEncoders.UseCompatibleStateImageBehavior = false;
            listViewEncoders.View = View.Details;
            listViewEncoders.ItemChecked += ListViewEncoders_ItemChecked;
            listViewEncoders.DragDrop += ListViewEncoders_DragDrop;
            listViewEncoders.DragEnter += ListViewEncoders_DragEnter;
            // 
            // columnHeaderEncodersName
            // 
            columnHeaderEncodersName.Text = "Name";
            columnHeaderEncodersName.Width = 120;
            // 
            // columnHeaderEncodersVersion
            // 
            columnHeaderEncodersVersion.Text = "Version";
            columnHeaderEncodersVersion.Width = 120;
            // 
            // columnHeaderEncodersDirectory
            // 
            columnHeaderEncodersDirectory.Text = "Directory";
            columnHeaderEncodersDirectory.Width = 150;
            // 
            // splitContainerEncodersAndAudioFlies
            // 
            splitContainerEncodersAndAudioFlies.Dock = DockStyle.Fill;
            splitContainerEncodersAndAudioFlies.Location = new Point(6, 6);
            splitContainerEncodersAndAudioFlies.Name = "splitContainerEncodersAndAudioFlies";
            // 
            // splitContainerEncodersAndAudioFlies.Panel1
            // 
            splitContainerEncodersAndAudioFlies.Panel1.Controls.Add(groupBoxEncoders);
            // 
            // splitContainerEncodersAndAudioFlies.Panel2
            // 
            splitContainerEncodersAndAudioFlies.Panel2.Controls.Add(groupBoxAudioFiles);
            splitContainerEncodersAndAudioFlies.Size = new Size(846, 165);
            splitContainerEncodersAndAudioFlies.SplitterDistance = 423;
            splitContainerEncodersAndAudioFlies.TabIndex = 1;
            // 
            // groupBoxAudioFiles
            // 
            groupBoxAudioFiles.Controls.Add(listViewAudioFiles);
            groupBoxAudioFiles.Dock = DockStyle.Fill;
            groupBoxAudioFiles.FlatStyle = FlatStyle.Popup;
            groupBoxAudioFiles.Location = new Point(0, 0);
            groupBoxAudioFiles.Name = "groupBoxAudioFiles";
            groupBoxAudioFiles.Size = new Size(419, 165);
            groupBoxAudioFiles.TabIndex = 2;
            groupBoxAudioFiles.TabStop = false;
            groupBoxAudioFiles.Text = "Audio Files";
            // 
            // listViewAudioFiles
            // 
            listViewAudioFiles.AllowDrop = true;
            listViewAudioFiles.CheckBoxes = true;
            listViewAudioFiles.Columns.AddRange(new ColumnHeader[] { columnHeaderAudioFilesName, columnHeaderAudioFilesChannels, columnHeaderAudioFilesBitDepth, columnHeaderAudioFilesSamplingRate, columnHeaderAudioFilesDuration, columnHeaderAudioFilesDirectory });
            listViewAudioFiles.Dock = DockStyle.Fill;
            listViewAudioFiles.FullRowSelect = true;
            listViewAudioFiles.Location = new Point(3, 19);
            listViewAudioFiles.MultiSelect = false;
            listViewAudioFiles.Name = "listViewAudioFiles";
            listViewAudioFiles.Size = new Size(413, 143);
            listViewAudioFiles.TabIndex = 0;
            listViewAudioFiles.UseCompatibleStateImageBehavior = false;
            listViewAudioFiles.View = View.Details;
            listViewAudioFiles.ItemChecked += ListViewAudioFiles_ItemChecked;
            listViewAudioFiles.DragDrop += ListViewAudioFiles_DragDrop;
            listViewAudioFiles.DragEnter += ListViewAudioFiles_DragEnter;
            // 
            // columnHeaderAudioFilesName
            // 
            columnHeaderAudioFilesName.Text = "Name";
            columnHeaderAudioFilesName.Width = 120;
            // 
            // columnHeaderAudioFilesChannels
            // 
            columnHeaderAudioFilesChannels.Text = "Ch.";
            columnHeaderAudioFilesChannels.Width = 30;
            // 
            // columnHeaderAudioFilesBitDepth
            // 
            columnHeaderAudioFilesBitDepth.Text = "Bits";
            columnHeaderAudioFilesBitDepth.Width = 30;
            // 
            // columnHeaderAudioFilesSamplingRate
            // 
            columnHeaderAudioFilesSamplingRate.Text = "Samp. Rate";
            // 
            // columnHeaderAudioFilesDuration
            // 
            columnHeaderAudioFilesDuration.Text = "Duration";
            // 
            // columnHeaderAudioFilesDirectory
            // 
            columnHeaderAudioFilesDirectory.Text = "Directory";
            columnHeaderAudioFilesDirectory.Width = 150;
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(radioButton_Hidden_ModeMP3_OFF);
            groupBoxEncoderSettings.Controls.Add(radioButtonModeCBR);
            groupBoxEncoderSettings.Controls.Add(radioButtonModeABR);
            groupBoxEncoderSettings.Controls.Add(radioButtonModeVBR);
            groupBoxEncoderSettings.Controls.Add(trackBarVBR);
            groupBoxEncoderSettings.Controls.Add(trackBarABR);
            groupBoxEncoderSettings.Controls.Add(trackBarCBR);
            groupBoxEncoderSettings.Controls.Add(labelCBRValue);
            groupBoxEncoderSettings.Controls.Add(labelABRValue);
            groupBoxEncoderSettings.Controls.Add(labelVBRValue);
            groupBoxEncoderSettings.Controls.Add(panelAdditionalOptions_1);
            groupBoxEncoderSettings.Location = new Point(3, 3);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(423, 229);
            groupBoxEncoderSettings.TabIndex = 1;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
            // 
            // radioButton_Hidden_ModeMP3_OFF
            // 
            radioButton_Hidden_ModeMP3_OFF.AutoSize = true;
            radioButton_Hidden_ModeMP3_OFF.Location = new Point(6, 111);
            radioButton_Hidden_ModeMP3_OFF.Name = "radioButton_Hidden_ModeMP3_OFF";
            radioButton_Hidden_ModeMP3_OFF.Size = new Size(14, 13);
            radioButton_Hidden_ModeMP3_OFF.TabIndex = 11;
            radioButton_Hidden_ModeMP3_OFF.UseVisualStyleBackColor = true;
            radioButton_Hidden_ModeMP3_OFF.Visible = false;
            radioButton_Hidden_ModeMP3_OFF.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // radioButtonModeCBR
            // 
            radioButtonModeCBR.AutoSize = true;
            radioButtonModeCBR.Checked = true;
            radioButtonModeCBR.Location = new Point(6, 22);
            radioButtonModeCBR.Name = "radioButtonModeCBR";
            radioButtonModeCBR.Size = new Size(47, 19);
            radioButtonModeCBR.TabIndex = 0;
            radioButtonModeCBR.TabStop = true;
            radioButtonModeCBR.Text = "CBR";
            radioButtonModeCBR.UseVisualStyleBackColor = true;
            radioButtonModeCBR.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // radioButtonModeABR
            // 
            radioButtonModeABR.AutoSize = true;
            radioButtonModeABR.Location = new Point(6, 54);
            radioButtonModeABR.Name = "radioButtonModeABR";
            radioButtonModeABR.Size = new Size(47, 19);
            radioButtonModeABR.TabIndex = 0;
            radioButtonModeABR.Text = "ABR";
            radioButtonModeABR.UseVisualStyleBackColor = true;
            radioButtonModeABR.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // radioButtonModeVBR
            // 
            radioButtonModeVBR.AutoSize = true;
            radioButtonModeVBR.Location = new Point(6, 86);
            radioButtonModeVBR.Name = "radioButtonModeVBR";
            radioButtonModeVBR.Size = new Size(46, 19);
            radioButtonModeVBR.TabIndex = 0;
            radioButtonModeVBR.Text = "VBR";
            toolTip1.SetToolTip(radioButtonModeVBR, "Note!\r\n\r\nV10...V14 may be compatible with LAME v4.1");
            radioButtonModeVBR.UseVisualStyleBackColor = true;
            radioButtonModeVBR.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // trackBarVBR
            // 
            trackBarVBR.Enabled = false;
            trackBarVBR.LargeChange = 1;
            trackBarVBR.Location = new Point(82, 86);
            trackBarVBR.Maximum = 0;
            trackBarVBR.Minimum = -14;
            trackBarVBR.Name = "trackBarVBR";
            trackBarVBR.Size = new Size(274, 45);
            trackBarVBR.TabIndex = 5;
            trackBarVBR.TickStyle = TickStyle.TopLeft;
            trackBarVBR.Scroll += TrackBarVBR_Scroll;
            // 
            // trackBarABR
            // 
            trackBarABR.Enabled = false;
            trackBarABR.LargeChange = 1;
            trackBarABR.Location = new Point(82, 54);
            trackBarABR.Maximum = 16;
            trackBarABR.Name = "trackBarABR";
            trackBarABR.Size = new Size(274, 45);
            trackBarABR.TabIndex = 3;
            trackBarABR.TickStyle = TickStyle.TopLeft;
            trackBarABR.Value = 16;
            trackBarABR.Scroll += TrackBarABR_Scroll;
            // 
            // trackBarCBR
            // 
            trackBarCBR.LargeChange = 1;
            trackBarCBR.Location = new Point(82, 22);
            trackBarCBR.Maximum = 16;
            trackBarCBR.Name = "trackBarCBR";
            trackBarCBR.Size = new Size(274, 45);
            trackBarCBR.TabIndex = 1;
            trackBarCBR.TickStyle = TickStyle.TopLeft;
            trackBarCBR.Value = 16;
            trackBarCBR.Scroll += TrackBarCBR_Scroll;
            // 
            // labelCBRValue
            // 
            labelCBRValue.AutoSize = true;
            labelCBRValue.Location = new Point(362, 24);
            labelCBRValue.MinimumSize = new Size(24, 0);
            labelCBRValue.Name = "labelCBRValue";
            labelCBRValue.Size = new Size(25, 15);
            labelCBRValue.TabIndex = 2;
            labelCBRValue.Text = "320";
            labelCBRValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelABRValue
            // 
            labelABRValue.AutoSize = true;
            labelABRValue.Enabled = false;
            labelABRValue.Location = new Point(362, 56);
            labelABRValue.MinimumSize = new Size(24, 0);
            labelABRValue.Name = "labelABRValue";
            labelABRValue.Size = new Size(25, 15);
            labelABRValue.TabIndex = 4;
            labelABRValue.Text = "320";
            labelABRValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelVBRValue
            // 
            labelVBRValue.AutoSize = true;
            labelVBRValue.Enabled = false;
            labelVBRValue.Location = new Point(362, 88);
            labelVBRValue.MinimumSize = new Size(24, 0);
            labelVBRValue.Name = "labelVBRValue";
            labelVBRValue.Size = new Size(24, 15);
            labelVBRValue.TabIndex = 6;
            labelVBRValue.Text = "V0";
            labelVBRValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelAdditionalOptions_1
            // 
            panelAdditionalOptions_1.Controls.Add(checkBoxParameter_q);
            panelAdditionalOptions_1.Controls.Add(labelParameter_qValue);
            panelAdditionalOptions_1.Controls.Add(checkBoxChannelsModes);
            panelAdditionalOptions_1.Controls.Add(radioButtonJointStereo);
            panelAdditionalOptions_1.Controls.Add(radioButtonStereo);
            panelAdditionalOptions_1.Controls.Add(radioButtonMono);
            panelAdditionalOptions_1.Controls.Add(trackBarParameter_q);
            panelAdditionalOptions_1.Location = new Point(3, 148);
            panelAdditionalOptions_1.Margin = new Padding(0);
            panelAdditionalOptions_1.Name = "panelAdditionalOptions_1";
            panelAdditionalOptions_1.Size = new Size(400, 56);
            panelAdditionalOptions_1.TabIndex = 10;
            // 
            // checkBoxParameter_q
            // 
            checkBoxParameter_q.AutoSize = true;
            checkBoxParameter_q.Location = new Point(3, 3);
            checkBoxParameter_q.Name = "checkBoxParameter_q";
            checkBoxParameter_q.Size = new Size(64, 19);
            checkBoxParameter_q.TabIndex = 0;
            checkBoxParameter_q.Text = "Quality";
            toolTip1.SetToolTip(checkBoxParameter_q, "Force/override algorithm quality selection");
            checkBoxParameter_q.UseVisualStyleBackColor = true;
            checkBoxParameter_q.CheckedChanged += CheckBoxQ_CheckedChanged;
            // 
            // labelParameter_qValue
            // 
            labelParameter_qValue.AutoSize = true;
            labelParameter_qValue.Enabled = false;
            labelParameter_qValue.Location = new Point(359, 4);
            labelParameter_qValue.MinimumSize = new Size(24, 0);
            labelParameter_qValue.Name = "labelParameter_qValue";
            labelParameter_qValue.Size = new Size(24, 15);
            labelParameter_qValue.TabIndex = 2;
            labelParameter_qValue.Text = "q0";
            labelParameter_qValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // checkBoxChannelsModes
            // 
            checkBoxChannelsModes.AutoSize = true;
            checkBoxChannelsModes.Location = new Point(3, 35);
            checkBoxChannelsModes.Name = "checkBoxChannelsModes";
            checkBoxChannelsModes.Size = new Size(109, 19);
            checkBoxChannelsModes.TabIndex = 3;
            checkBoxChannelsModes.Text = "Channel Modes";
            toolTip1.SetToolTip(checkBoxChannelsModes, "Force/override channel processing mode");
            checkBoxChannelsModes.UseVisualStyleBackColor = true;
            checkBoxChannelsModes.CheckedChanged += CheckBoxChannelsMix_CheckedChanged;
            // 
            // radioButtonJointStereo
            // 
            radioButtonJointStereo.AutoSize = true;
            radioButtonJointStereo.Checked = true;
            radioButtonJointStereo.Enabled = false;
            radioButtonJointStereo.Location = new Point(118, 34);
            radioButtonJointStereo.Name = "radioButtonJointStereo";
            radioButtonJointStereo.Size = new Size(40, 19);
            radioButtonJointStereo.TabIndex = 4;
            radioButtonJointStereo.TabStop = true;
            radioButtonJointStereo.Text = "J/S";
            toolTip1.SetToolTip(radioButtonJointStereo, "-m j\tautomatic switch between L/R and M/S stereo");
            radioButtonJointStereo.UseVisualStyleBackColor = true;
            radioButtonJointStereo.CheckedChanged += RadioButtonStereoMode_CheckedChanged;
            // 
            // radioButtonStereo
            // 
            radioButtonStereo.AutoSize = true;
            radioButtonStereo.Enabled = false;
            radioButtonStereo.Location = new Point(164, 34);
            radioButtonStereo.Name = "radioButtonStereo";
            radioButtonStereo.Size = new Size(58, 19);
            radioButtonStereo.TabIndex = 5;
            radioButtonStereo.Text = "Stereo";
            toolTip1.SetToolTip(radioButtonStereo, "-m s\tforced L/R stereo");
            radioButtonStereo.UseVisualStyleBackColor = true;
            radioButtonStereo.CheckedChanged += RadioButtonStereoMode_CheckedChanged;
            // 
            // radioButtonMono
            // 
            radioButtonMono.AutoSize = true;
            radioButtonMono.Enabled = false;
            radioButtonMono.Location = new Point(228, 34);
            radioButtonMono.Name = "radioButtonMono";
            radioButtonMono.Size = new Size(57, 19);
            radioButtonMono.TabIndex = 6;
            radioButtonMono.Text = "Mono";
            toolTip1.SetToolTip(radioButtonMono, "-m m\tmono");
            radioButtonMono.UseVisualStyleBackColor = true;
            radioButtonMono.CheckedChanged += RadioButtonStereoMode_CheckedChanged;
            // 
            // trackBarParameter_q
            // 
            trackBarParameter_q.Enabled = false;
            trackBarParameter_q.LargeChange = 1;
            trackBarParameter_q.Location = new Point(79, 3);
            trackBarParameter_q.Maximum = 0;
            trackBarParameter_q.Minimum = -9;
            trackBarParameter_q.Name = "trackBarParameter_q";
            trackBarParameter_q.Size = new Size(274, 45);
            trackBarParameter_q.TabIndex = 1;
            trackBarParameter_q.TickStyle = TickStyle.TopLeft;
            trackBarParameter_q.Scroll += TrackBarQ_Scroll;
            // 
            // buttonSaveUserPreset6
            // 
            buttonSaveUserPreset6.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            buttonSaveUserPreset6.Location = new Point(6, 180);
            buttonSaveUserPreset6.Name = "buttonSaveUserPreset6";
            buttonSaveUserPreset6.Size = new Size(24, 24);
            buttonSaveUserPreset6.TabIndex = 12;
            buttonSaveUserPreset6.Text = "💾";
            toolTip1.SetToolTip(buttonSaveUserPreset6, "Save current encoder settings to this preset");
            buttonSaveUserPreset6.UseVisualStyleBackColor = true;
            buttonSaveUserPreset6.Click += ButtonSaveUserPreset_Click;
            // 
            // buttonSaveUserPreset5
            // 
            buttonSaveUserPreset5.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            buttonSaveUserPreset5.Location = new Point(6, 148);
            buttonSaveUserPreset5.Name = "buttonSaveUserPreset5";
            buttonSaveUserPreset5.Size = new Size(24, 24);
            buttonSaveUserPreset5.TabIndex = 12;
            buttonSaveUserPreset5.Text = "💾";
            toolTip1.SetToolTip(buttonSaveUserPreset5, "Save current encoder settings to this preset");
            buttonSaveUserPreset5.UseVisualStyleBackColor = true;
            buttonSaveUserPreset5.Click += ButtonSaveUserPreset_Click;
            // 
            // buttonSaveUserPreset4
            // 
            buttonSaveUserPreset4.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            buttonSaveUserPreset4.Location = new Point(6, 116);
            buttonSaveUserPreset4.Name = "buttonSaveUserPreset4";
            buttonSaveUserPreset4.Size = new Size(24, 24);
            buttonSaveUserPreset4.TabIndex = 12;
            buttonSaveUserPreset4.Text = "💾";
            toolTip1.SetToolTip(buttonSaveUserPreset4, "Save current encoder settings to this preset");
            buttonSaveUserPreset4.UseVisualStyleBackColor = true;
            buttonSaveUserPreset4.Click += ButtonSaveUserPreset_Click;
            // 
            // buttonSaveUserPreset3
            // 
            buttonSaveUserPreset3.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            buttonSaveUserPreset3.Location = new Point(6, 84);
            buttonSaveUserPreset3.Name = "buttonSaveUserPreset3";
            buttonSaveUserPreset3.Size = new Size(24, 24);
            buttonSaveUserPreset3.TabIndex = 12;
            buttonSaveUserPreset3.Text = "💾";
            toolTip1.SetToolTip(buttonSaveUserPreset3, "Save current encoder settings to this preset");
            buttonSaveUserPreset3.UseVisualStyleBackColor = true;
            buttonSaveUserPreset3.Click += ButtonSaveUserPreset_Click;
            // 
            // buttonSaveUserPreset2
            // 
            buttonSaveUserPreset2.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            buttonSaveUserPreset2.Location = new Point(6, 52);
            buttonSaveUserPreset2.Name = "buttonSaveUserPreset2";
            buttonSaveUserPreset2.Size = new Size(24, 24);
            buttonSaveUserPreset2.TabIndex = 12;
            buttonSaveUserPreset2.Text = "💾";
            toolTip1.SetToolTip(buttonSaveUserPreset2, "Save current encoder settings to this preset");
            buttonSaveUserPreset2.UseVisualStyleBackColor = true;
            buttonSaveUserPreset2.Click += ButtonSaveUserPreset_Click;
            // 
            // buttonSaveUserPreset1
            // 
            buttonSaveUserPreset1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            buttonSaveUserPreset1.Location = new Point(6, 20);
            buttonSaveUserPreset1.Name = "buttonSaveUserPreset1";
            buttonSaveUserPreset1.Size = new Size(24, 24);
            buttonSaveUserPreset1.TabIndex = 12;
            buttonSaveUserPreset1.Text = "💾";
            toolTip1.SetToolTip(buttonSaveUserPreset1, "Save current encoder settings to this preset");
            buttonSaveUserPreset1.UseVisualStyleBackColor = true;
            buttonSaveUserPreset1.Click += ButtonSaveUserPreset_Click;
            // 
            // textBoxUserPreset6
            // 
            textBoxUserPreset6.Location = new Point(101, 180);
            textBoxUserPreset6.Multiline = true;
            textBoxUserPreset6.Name = "textBoxUserPreset6";
            textBoxUserPreset6.Size = new Size(280, 23);
            textBoxUserPreset6.TabIndex = 3;
            // 
            // textBoxUserPreset4
            // 
            textBoxUserPreset4.Location = new Point(101, 116);
            textBoxUserPreset4.Multiline = true;
            textBoxUserPreset4.Name = "textBoxUserPreset4";
            textBoxUserPreset4.Size = new Size(280, 23);
            textBoxUserPreset4.TabIndex = 3;
            // 
            // textBoxUserPreset5
            // 
            textBoxUserPreset5.Location = new Point(101, 148);
            textBoxUserPreset5.Multiline = true;
            textBoxUserPreset5.Name = "textBoxUserPreset5";
            textBoxUserPreset5.Size = new Size(280, 23);
            textBoxUserPreset5.TabIndex = 3;
            // 
            // textBoxUserPreset3
            // 
            textBoxUserPreset3.Location = new Point(101, 84);
            textBoxUserPreset3.Multiline = true;
            textBoxUserPreset3.Name = "textBoxUserPreset3";
            textBoxUserPreset3.Size = new Size(280, 23);
            textBoxUserPreset3.TabIndex = 3;
            // 
            // textBoxUserPreset2
            // 
            textBoxUserPreset2.Location = new Point(101, 52);
            textBoxUserPreset2.Multiline = true;
            textBoxUserPreset2.Name = "textBoxUserPreset2";
            textBoxUserPreset2.Size = new Size(280, 23);
            textBoxUserPreset2.TabIndex = 3;
            // 
            // textBoxUserPreset1
            // 
            textBoxUserPreset1.Location = new Point(101, 20);
            textBoxUserPreset1.Multiline = true;
            textBoxUserPreset1.Name = "textBoxUserPreset1";
            textBoxUserPreset1.Size = new Size(280, 23);
            textBoxUserPreset1.TabIndex = 3;
            // 
            // radioButtonUserPreset6
            // 
            radioButtonUserPreset6.AutoSize = true;
            radioButtonUserPreset6.Location = new Point(37, 182);
            radioButtonUserPreset6.Name = "radioButtonUserPreset6";
            radioButtonUserPreset6.Size = new Size(31, 19);
            radioButtonUserPreset6.TabIndex = 1;
            radioButtonUserPreset6.Text = "6";
            toolTip1.SetToolTip(radioButtonUserPreset6, "Apply Preset (Ctrl+6)\r\n\r\n(Ctrl+Shift+6) random preset 1-6");
            radioButtonUserPreset6.UseVisualStyleBackColor = true;
            radioButtonUserPreset6.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // radioButtonUserPreset4
            // 
            radioButtonUserPreset4.AutoSize = true;
            radioButtonUserPreset4.Location = new Point(37, 118);
            radioButtonUserPreset4.Name = "radioButtonUserPreset4";
            radioButtonUserPreset4.Size = new Size(31, 19);
            radioButtonUserPreset4.TabIndex = 1;
            radioButtonUserPreset4.Text = "4";
            toolTip1.SetToolTip(radioButtonUserPreset4, "Apply Preset (Ctrl+4)\r\n\r\n(Ctrl+Shift+4) random preset 1-4");
            radioButtonUserPreset4.UseVisualStyleBackColor = true;
            radioButtonUserPreset4.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // buttonUserPreset6Clear
            // 
            buttonUserPreset6Clear.Location = new Point(387, 180);
            buttonUserPreset6Clear.Name = "buttonUserPreset6Clear";
            buttonUserPreset6Clear.Size = new Size(23, 23);
            buttonUserPreset6Clear.TabIndex = 0;
            buttonUserPreset6Clear.Text = "❌";
            toolTip1.SetToolTip(buttonUserPreset6Clear, "Clear Preset");
            buttonUserPreset6Clear.UseVisualStyleBackColor = true;
            buttonUserPreset6Clear.Click += ButtonUserPresetClear_Click;
            // 
            // buttonUserPreset4Clear
            // 
            buttonUserPreset4Clear.Location = new Point(387, 116);
            buttonUserPreset4Clear.Name = "buttonUserPreset4Clear";
            buttonUserPreset4Clear.Size = new Size(23, 23);
            buttonUserPreset4Clear.TabIndex = 0;
            buttonUserPreset4Clear.Text = "❌";
            toolTip1.SetToolTip(buttonUserPreset4Clear, "Clear Preset");
            buttonUserPreset4Clear.UseVisualStyleBackColor = true;
            buttonUserPreset4Clear.Click += ButtonUserPresetClear_Click;
            // 
            // radioButtonUserPreset5
            // 
            radioButtonUserPreset5.AutoSize = true;
            radioButtonUserPreset5.Location = new Point(37, 150);
            radioButtonUserPreset5.Name = "radioButtonUserPreset5";
            radioButtonUserPreset5.Size = new Size(31, 19);
            radioButtonUserPreset5.TabIndex = 1;
            radioButtonUserPreset5.Text = "5";
            toolTip1.SetToolTip(radioButtonUserPreset5, "Apply Preset (Ctrl+5)\r\n\r\n(Ctrl+Shift+5) random preset 1-5");
            radioButtonUserPreset5.UseVisualStyleBackColor = true;
            radioButtonUserPreset5.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // buttonUserPreset5Clear
            // 
            buttonUserPreset5Clear.Location = new Point(387, 148);
            buttonUserPreset5Clear.Name = "buttonUserPreset5Clear";
            buttonUserPreset5Clear.Size = new Size(23, 23);
            buttonUserPreset5Clear.TabIndex = 0;
            buttonUserPreset5Clear.Text = "❌";
            toolTip1.SetToolTip(buttonUserPreset5Clear, "Clear Preset");
            buttonUserPreset5Clear.UseVisualStyleBackColor = true;
            buttonUserPreset5Clear.Click += ButtonUserPresetClear_Click;
            // 
            // radioButtonUserPreset3
            // 
            radioButtonUserPreset3.AutoSize = true;
            radioButtonUserPreset3.Location = new Point(37, 86);
            radioButtonUserPreset3.Name = "radioButtonUserPreset3";
            radioButtonUserPreset3.Size = new Size(31, 19);
            radioButtonUserPreset3.TabIndex = 1;
            radioButtonUserPreset3.Text = "3";
            toolTip1.SetToolTip(radioButtonUserPreset3, "Apply Preset (Ctrl+3)\r\n\r\n(Ctrl+Shift+3) random preset 1-3");
            radioButtonUserPreset3.UseVisualStyleBackColor = true;
            radioButtonUserPreset3.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // buttonUserPreset3Clear
            // 
            buttonUserPreset3Clear.Location = new Point(387, 84);
            buttonUserPreset3Clear.Name = "buttonUserPreset3Clear";
            buttonUserPreset3Clear.Size = new Size(23, 23);
            buttonUserPreset3Clear.TabIndex = 0;
            buttonUserPreset3Clear.Text = "❌";
            toolTip1.SetToolTip(buttonUserPreset3Clear, "Clear Preset");
            buttonUserPreset3Clear.UseVisualStyleBackColor = true;
            buttonUserPreset3Clear.Click += ButtonUserPresetClear_Click;
            // 
            // radioButtonUserPreset2
            // 
            radioButtonUserPreset2.AutoSize = true;
            radioButtonUserPreset2.Location = new Point(37, 54);
            radioButtonUserPreset2.Name = "radioButtonUserPreset2";
            radioButtonUserPreset2.Size = new Size(31, 19);
            radioButtonUserPreset2.TabIndex = 1;
            radioButtonUserPreset2.Text = "2";
            toolTip1.SetToolTip(radioButtonUserPreset2, "Apply Preset (Ctrl+2)\r\n\r\n(Ctrl+Shift+2) random preset 1-2");
            radioButtonUserPreset2.UseVisualStyleBackColor = true;
            radioButtonUserPreset2.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // radioButtonUserPreset1
            // 
            radioButtonUserPreset1.AutoSize = true;
            radioButtonUserPreset1.Location = new Point(37, 22);
            radioButtonUserPreset1.Name = "radioButtonUserPreset1";
            radioButtonUserPreset1.Size = new Size(31, 19);
            radioButtonUserPreset1.TabIndex = 1;
            radioButtonUserPreset1.Text = "1";
            toolTip1.SetToolTip(radioButtonUserPreset1, "Apply Preset (Ctrl+1)\r\n\r\n(Ctrl+Shift+2) random preset 1-2");
            radioButtonUserPreset1.UseVisualStyleBackColor = true;
            radioButtonUserPreset1.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // buttonUserPreset2Clear
            // 
            buttonUserPreset2Clear.Location = new Point(387, 52);
            buttonUserPreset2Clear.Name = "buttonUserPreset2Clear";
            buttonUserPreset2Clear.Size = new Size(23, 23);
            buttonUserPreset2Clear.TabIndex = 0;
            buttonUserPreset2Clear.Text = "❌";
            toolTip1.SetToolTip(buttonUserPreset2Clear, "Clear Preset");
            buttonUserPreset2Clear.UseVisualStyleBackColor = true;
            buttonUserPreset2Clear.Click += ButtonUserPresetClear_Click;
            // 
            // buttonUserPreset1Clear
            // 
            buttonUserPreset1Clear.Location = new Point(387, 20);
            buttonUserPreset1Clear.Name = "buttonUserPreset1Clear";
            buttonUserPreset1Clear.Size = new Size(23, 23);
            buttonUserPreset1Clear.TabIndex = 0;
            buttonUserPreset1Clear.Text = "❌";
            toolTip1.SetToolTip(buttonUserPreset1Clear, "Clear Preset");
            buttonUserPreset1Clear.UseVisualStyleBackColor = true;
            buttonUserPreset1Clear.Click += ButtonUserPresetClear_Click;
            // 
            // labelMixBalance
            // 
            labelMixBalance.AutoSize = true;
            labelMixBalance.Location = new Point(246, 120);
            labelMixBalance.Name = "labelMixBalance";
            labelMixBalance.Size = new Size(42, 15);
            labelMixBalance.TabIndex = 13;
            labelMixBalance.Text = "50 / 50";
            labelMixBalance.TextAlign = ContentAlignment.TopCenter;
            toolTip1.SetToolTip(labelMixBalance, "Original / Encoded");
            labelMixBalance.Visible = false;
            // 
            // trackBarMixBalance
            // 
            trackBarMixBalance.LargeChange = 1;
            trackBarMixBalance.Location = new Point(83, 118);
            trackBarMixBalance.Maximum = 100;
            trackBarMixBalance.Name = "trackBarMixBalance";
            trackBarMixBalance.Size = new Size(157, 45);
            trackBarMixBalance.TabIndex = 12;
            trackBarMixBalance.TickStyle = TickStyle.None;
            trackBarMixBalance.Value = 50;
            trackBarMixBalance.Visible = false;
            trackBarMixBalance.Scroll += TrackBarMixBalance_Scroll;
            trackBarMixBalance.MouseDown += TrackBarMixBalance_MouseDown;
            // 
            // radioButtonPlayMix
            // 
            radioButtonPlayMix.AutoSize = true;
            radioButtonPlayMix.Location = new Point(6, 118);
            radioButtonPlayMix.Name = "radioButtonPlayMix";
            radioButtonPlayMix.Size = new Size(45, 19);
            radioButtonPlayMix.TabIndex = 11;
            radioButtonPlayMix.Text = "Mix";
            radioButtonPlayMix.UseVisualStyleBackColor = true;
            radioButtonPlayMix.CheckedChanged += RadioPlaySource_CheckedChanged;
            // 
            // progressBarEncodingProcess
            // 
            progressBarEncodingProcess.Location = new Point(83, 82);
            progressBarEncodingProcess.Name = "progressBarEncodingProcess";
            progressBarEncodingProcess.Size = new Size(157, 23);
            progressBarEncodingProcess.TabIndex = 6;
            progressBarEncodingProcess.Visible = false;
            // 
            // buttonLoopPlayback
            // 
            buttonLoopPlayback.Location = new Point(6, 200);
            buttonLoopPlayback.Name = "buttonLoopPlayback";
            buttonLoopPlayback.Size = new Size(70, 23);
            buttonLoopPlayback.TabIndex = 9;
            buttonLoopPlayback.Text = "Loop: ON";
            buttonLoopPlayback.UseVisualStyleBackColor = true;
            buttonLoopPlayback.Click += ButtonLoopPlayback_Click;
            // 
            // radioButtonPlayDifference
            // 
            radioButtonPlayDifference.AutoSize = true;
            radioButtonPlayDifference.Location = new Point(6, 150);
            radioButtonPlayDifference.Name = "radioButtonPlayDifference";
            radioButtonPlayDifference.Size = new Size(79, 19);
            radioButtonPlayDifference.TabIndex = 5;
            radioButtonPlayDifference.Text = "Difference";
            radioButtonPlayDifference.UseVisualStyleBackColor = true;
            radioButtonPlayDifference.CheckedChanged += RadioPlaySource_CheckedChanged;
            // 
            // radioButtonPlayEncoded
            // 
            radioButtonPlayEncoded.AutoSize = true;
            radioButtonPlayEncoded.Location = new Point(6, 86);
            radioButtonPlayEncoded.Name = "radioButtonPlayEncoded";
            radioButtonPlayEncoded.Size = new Size(71, 19);
            radioButtonPlayEncoded.TabIndex = 4;
            radioButtonPlayEncoded.Text = "Encoded";
            radioButtonPlayEncoded.UseVisualStyleBackColor = true;
            radioButtonPlayEncoded.CheckedChanged += RadioPlaySource_CheckedChanged;
            // 
            // radioButtonPlayOriginal
            // 
            radioButtonPlayOriginal.AutoSize = true;
            radioButtonPlayOriginal.Checked = true;
            radioButtonPlayOriginal.Location = new Point(6, 54);
            radioButtonPlayOriginal.Name = "radioButtonPlayOriginal";
            radioButtonPlayOriginal.Size = new Size(67, 19);
            radioButtonPlayOriginal.TabIndex = 3;
            radioButtonPlayOriginal.TabStop = true;
            radioButtonPlayOriginal.Text = "Original";
            radioButtonPlayOriginal.UseVisualStyleBackColor = true;
            radioButtonPlayOriginal.CheckedChanged += RadioPlaySource_CheckedChanged;
            // 
            // buttonPlayPause
            // 
            buttonPlayPause.Location = new Point(6, 22);
            buttonPlayPause.Name = "buttonPlayPause";
            buttonPlayPause.Size = new Size(25, 23);
            buttonPlayPause.TabIndex = 1;
            buttonPlayPause.Text = "▶";
            buttonPlayPause.UseVisualStyleBackColor = true;
            buttonPlayPause.Click += ButtonPlayPause_Click;
            // 
            // buttonStop
            // 
            buttonStop.Location = new Point(37, 22);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(25, 23);
            buttonStop.TabIndex = 2;
            buttonStop.Text = "■";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += ButtonStop_Click;
            // 
            // buttonClearEncoders
            // 
            buttonClearEncoders.Location = new Point(188, 200);
            buttonClearEncoders.Name = "buttonClearEncoders";
            buttonClearEncoders.Size = new Size(110, 23);
            buttonClearEncoders.TabIndex = 10;
            buttonClearEncoders.Text = "Clear Encoders";
            buttonClearEncoders.UseVisualStyleBackColor = true;
            buttonClearEncoders.Click += ButtonClear_Click;
            // 
            // buttonClearAudioFiles
            // 
            buttonClearAudioFiles.Location = new Point(304, 200);
            buttonClearAudioFiles.Name = "buttonClearAudioFiles";
            buttonClearAudioFiles.Size = new Size(110, 23);
            buttonClearAudioFiles.TabIndex = 10;
            buttonClearAudioFiles.Text = "Clear Audio Files";
            buttonClearAudioFiles.UseVisualStyleBackColor = true;
            buttonClearAudioFiles.Click += ButtonClear_Click;
            // 
            // trackBarSeek
            // 
            trackBarSeek.LargeChange = 1;
            trackBarSeek.Location = new Point(68, 22);
            trackBarSeek.Maximum = 1000;
            trackBarSeek.Name = "trackBarSeek";
            trackBarSeek.Size = new Size(346, 45);
            trackBarSeek.TabIndex = 8;
            trackBarSeek.TickStyle = TickStyle.None;
            trackBarSeek.Scroll += TrackBarSeek_Scroll;
            trackBarSeek.MouseDown += TrackBarSeek_MouseDown;
            trackBarSeek.MouseUp += TrackBarSeek_MouseUp;
            // 
            // checkBoxCheckForUpdates
            // 
            checkBoxCheckForUpdates.AutoSize = true;
            checkBoxCheckForUpdates.Location = new Point(293, 206);
            checkBoxCheckForUpdates.Name = "checkBoxCheckForUpdates";
            checkBoxCheckForUpdates.Size = new Size(122, 19);
            checkBoxCheckForUpdates.TabIndex = 13;
            checkBoxCheckForUpdates.Text = "Check for updates";
            toolTip1.SetToolTip(checkBoxCheckForUpdates, "May not work yet (development of this feature is in progress)");
            checkBoxCheckForUpdates.UseVisualStyleBackColor = true;
            checkBoxCheckForUpdates.CheckedChanged += CheckBoxCheckForUpdates_CheckedChanged;
            // 
            // timerTrackBarSeek
            // 
            timerTrackBarSeek.Interval = 300;
            timerTrackBarSeek.Tick += TimerTrackBarSeek_Tick;
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.ColumnCount = 1;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Controls.Add(groupBoxPlayerControl, 0, 2);
            tableLayoutPanelMain.Controls.Add(splitContainerEncodersAndAudioFlies, 0, 0);
            tableLayoutPanelMain.Controls.Add(panelSettings, 0, 1);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.Padding = new Padding(3);
            tableLayoutPanelMain.RowCount = 3;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 235F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 235F));
            tableLayoutPanelMain.Size = new Size(858, 647);
            tableLayoutPanelMain.TabIndex = 3;
            // 
            // groupBoxPlayerControl
            // 
            groupBoxPlayerControl.Controls.Add(buttonPlayPause);
            groupBoxPlayerControl.Controls.Add(labelMixBalance);
            groupBoxPlayerControl.Controls.Add(buttonClearAudioFiles);
            groupBoxPlayerControl.Controls.Add(radioButtonPlayMix);
            groupBoxPlayerControl.Controls.Add(buttonClearEncoders);
            groupBoxPlayerControl.Controls.Add(buttonStop);
            groupBoxPlayerControl.Controls.Add(progressBarEncodingProcess);
            groupBoxPlayerControl.Controls.Add(radioButtonPlayOriginal);
            groupBoxPlayerControl.Controls.Add(buttonLoopPlayback);
            groupBoxPlayerControl.Controls.Add(radioButtonPlayEncoded);
            groupBoxPlayerControl.Controls.Add(radioButtonPlayDifference);
            groupBoxPlayerControl.Controls.Add(trackBarSeek);
            groupBoxPlayerControl.Controls.Add(trackBarMixBalance);
            groupBoxPlayerControl.Location = new Point(6, 412);
            groupBoxPlayerControl.Name = "groupBoxPlayerControl";
            groupBoxPlayerControl.Size = new Size(420, 229);
            groupBoxPlayerControl.TabIndex = 14;
            groupBoxPlayerControl.TabStop = false;
            groupBoxPlayerControl.Text = "Player control";
            // 
            // panelSettings
            // 
            panelSettings.Controls.Add(groupBox1);
            panelSettings.Controls.Add(groupBoxEncoderSettings);
            panelSettings.Dock = DockStyle.Fill;
            panelSettings.Location = new Point(3, 174);
            panelSettings.Margin = new Padding(0);
            panelSettings.Name = "panelSettings";
            panelSettings.Size = new Size(852, 235);
            panelSettings.TabIndex = 16;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(labelNoUpdates);
            groupBox1.Controls.Add(checkBoxCheckForUpdates);
            groupBox1.Controls.Add(buttonSaveUserPreset1);
            groupBox1.Controls.Add(buttonUserPreset1Clear);
            groupBox1.Controls.Add(buttonUserPreset2Clear);
            groupBox1.Controls.Add(buttonSaveUserPreset6);
            groupBox1.Controls.Add(radioButtonUserPreset1);
            groupBox1.Controls.Add(buttonSaveUserPreset5);
            groupBox1.Controls.Add(radioButtonUserPreset2);
            groupBox1.Controls.Add(buttonSaveUserPreset4);
            groupBox1.Controls.Add(buttonUserPreset3Clear);
            groupBox1.Controls.Add(buttonSaveUserPreset3);
            groupBox1.Controls.Add(radioButtonUserPreset3);
            groupBox1.Controls.Add(buttonSaveUserPreset2);
            groupBox1.Controls.Add(buttonUserPreset5Clear);
            groupBox1.Controls.Add(radioButtonUserPreset5);
            groupBox1.Controls.Add(textBoxUserPreset6);
            groupBox1.Controls.Add(buttonUserPreset4Clear);
            groupBox1.Controls.Add(textBoxUserPreset4);
            groupBox1.Controls.Add(buttonUserPreset6Clear);
            groupBox1.Controls.Add(radioButtonUserPreset4);
            groupBox1.Controls.Add(textBoxUserPreset5);
            groupBox1.Controls.Add(radioButton_Hidden_UserPreset_OFF);
            groupBox1.Controls.Add(radioButtonUserPreset6);
            groupBox1.Controls.Add(textBoxUserPreset1);
            groupBox1.Controls.Add(textBoxUserPreset3);
            groupBox1.Controls.Add(textBoxUserPreset2);
            groupBox1.Location = new Point(432, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(417, 229);
            groupBox1.TabIndex = 14;
            groupBox1.TabStop = false;
            groupBox1.Text = "User Presets / Custom Settings";
            // 
            // labelNoUpdates
            // 
            labelNoUpdates.AutoSize = true;
            labelNoUpdates.Location = new Point(65, 207);
            labelNoUpdates.Name = "labelNoUpdates";
            labelNoUpdates.Size = new Size(166, 15);
            labelNoUpdates.TabIndex = 14;
            labelNoUpdates.Text = "ℹ️ Update 0000.00.00 available";
            labelNoUpdates.TextAlign = ContentAlignment.TopRight;
            labelNoUpdates.Visible = false;
            // 
            // radioButton_Hidden_UserPreset_OFF
            // 
            radioButton_Hidden_UserPreset_OFF.AutoSize = true;
            radioButton_Hidden_UserPreset_OFF.Checked = true;
            radioButton_Hidden_UserPreset_OFF.Location = new Point(37, 201);
            radioButton_Hidden_UserPreset_OFF.Name = "radioButton_Hidden_UserPreset_OFF";
            radioButton_Hidden_UserPreset_OFF.Size = new Size(14, 13);
            radioButton_Hidden_UserPreset_OFF.TabIndex = 1;
            radioButton_Hidden_UserPreset_OFF.TabStop = true;
            radioButton_Hidden_UserPreset_OFF.UseVisualStyleBackColor = true;
            radioButton_Hidden_UserPreset_OFF.Visible = false;
            radioButton_Hidden_UserPreset_OFF.CheckedChanged += RadioButtonMode_CheckedChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(858, 647);
            Controls.Add(tableLayoutPanelMain);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimumSize = new Size(874, 686);
            Name = "Form1";
            Text = "Codec Playground-H";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            KeyDown += Form1_KeyDown;
            groupBoxEncoders.ResumeLayout(false);
            splitContainerEncodersAndAudioFlies.Panel1.ResumeLayout(false);
            splitContainerEncodersAndAudioFlies.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerEncodersAndAudioFlies).EndInit();
            splitContainerEncodersAndAudioFlies.ResumeLayout(false);
            groupBoxAudioFiles.ResumeLayout(false);
            groupBoxEncoderSettings.ResumeLayout(false);
            groupBoxEncoderSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarVBR).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarABR).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarCBR).EndInit();
            panelAdditionalOptions_1.ResumeLayout(false);
            panelAdditionalOptions_1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarParameter_q).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarMixBalance).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarSeek).EndInit();
            tableLayoutPanelMain.ResumeLayout(false);
            groupBoxPlayerControl.ResumeLayout(false);
            groupBoxPlayerControl.PerformLayout();
            panelSettings.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxEncoders;
        private GroupBox groupBoxEncoderSettings;
        private GroupBox groupBoxAudioFiles;
        private TrackBar trackBarABR;
        private TrackBar trackBarVBR;
        private TrackBar trackBarCBR;
        private RadioButton radioButtonModeABR;
        private RadioButton radioButtonModeVBR;
        private RadioButton radioButtonModeCBR;
        private CheckBox checkBoxParameter_q;
        private TrackBar trackBarParameter_q;
        private Label labelCBRValue;
        private CheckBox checkBoxChannelsModes;
        private RadioButton radioButtonStereo;
        private Label labelParameter_qValue;
        private Label labelABRValue;
        private Label labelVBRValue;
        private RadioButton radioButtonMono;
        private RadioButton radioButtonJointStereo;
        private Panel panelAdditionalOptions_1;
        private ListView listViewAudioFiles;
        private ListView listViewEncoders;
        private Button buttonStop;
        private Button buttonPlayPause;
        private ColumnHeader columnHeaderAudioFilesName;
        private ColumnHeader columnHeaderAudioFilesChannels;
        private ColumnHeader columnHeaderAudioFilesBitDepth;
        private ColumnHeader columnHeaderAudioFilesSamplingRate;
        private ColumnHeader columnHeaderEncodersName;
        private ColumnHeader columnHeaderEncodersVersion;
        private ColumnHeader columnHeaderEncodersDirectory;
        private Button buttonClearAudioFiles;
        private ColumnHeader columnHeaderAudioFilesDirectory;
        private ToolTip toolTip1;
        private TrackBar trackBarSeek;
        private System.Windows.Forms.Timer timerTrackBarSeek;
        private RadioButton radioButtonPlayDifference;
        private RadioButton radioButtonPlayEncoded;
        private RadioButton radioButtonPlayOriginal;
        private Button buttonLoopPlayback;
        private ProgressBar progressBarEncodingProcess;
        private RadioButton radioButtonPlayMix;
        private ColumnHeader columnHeaderAudioFilesDuration;
        private Button buttonClearEncoders;
        private TrackBar trackBarMixBalance;
        private Label labelMixBalance;
        private TableLayoutPanel tableLayoutPanelMain;
        private GroupBox groupBoxPlayerControl;
        private RadioButton radioButtonUserPreset1;
        private Button buttonUserPreset1Clear;
        private TextBox textBoxUserPreset1;
        private TextBox textBoxUserPreset4;
        private TextBox textBoxUserPreset3;
        private TextBox textBoxUserPreset2;
        private RadioButton radioButtonUserPreset4;
        private Button buttonUserPreset4Clear;
        private RadioButton radioButtonUserPreset3;
        private Button buttonUserPreset3Clear;
        private RadioButton radioButtonUserPreset2;
        private Button buttonUserPreset2Clear;
        private SplitContainer splitContainerEncodersAndAudioFlies;
        private Panel panelSettings;
        private TextBox textBoxUserPreset6;
        private TextBox textBoxUserPreset5;
        private RadioButton radioButtonUserPreset6;
        private Button buttonUserPreset6Clear;
        private RadioButton radioButtonUserPreset5;
        private Button buttonUserPreset5Clear;
        private Button buttonSaveUserPreset1;
        private Button buttonSaveUserPreset6;
        private Button buttonSaveUserPreset5;
        private Button buttonSaveUserPreset4;
        private Button buttonSaveUserPreset3;
        private Button buttonSaveUserPreset2;
        private GroupBox groupBox1;
        private RadioButton radioButton_Hidden_ModeMP3_OFF;
        private RadioButton radioButton_Hidden_UserPreset_OFF;
        private CheckBox checkBoxCheckForUpdates;
        private Label labelNoUpdates;
    }
}
