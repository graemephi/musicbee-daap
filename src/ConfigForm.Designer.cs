namespace MusicBeePlugin.src
{
    partial class ConfigForm
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
            if (disposing && (components != null)) {
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigForm));
            this.serverNameLabel = new System.Windows.Forms.Label();
            this.serverNameInput = new System.Windows.Forms.TextBox();
            this.portLabel = new System.Windows.Forms.Label();
            this.transcodeLabel = new System.Windows.Forms.Label();
            this.serverGroup = new System.Windows.Forms.GroupBox();
            this.portInput = new System.Windows.Forms.TextBox();
            this.transcodeGroup = new System.Windows.Forms.GroupBox();
            this.replayGainSmart = new System.Windows.Forms.RadioButton();
            this.replayGainAlbum = new System.Windows.Forms.RadioButton();
            this.replayGainTrack = new System.Windows.Forms.RadioButton();
            this.replayGainLabel = new System.Windows.Forms.Label();
            this.replayGainOff = new System.Windows.Forms.RadioButton();
            this.dsp = new System.Windows.Forms.CheckBox();
            this.mbAudio = new System.Windows.Forms.CheckBox();
            this.pcm = new System.Windows.Forms.CheckBox();
            this.transcodeFormats = new System.Windows.Forms.CheckedListBox();
            this.formatsGroup = new System.Windows.Forms.GroupBox();
            this.allButton = new System.Windows.Forms.Button();
            this.noneButton = new System.Windows.Forms.Button();
            this.iTunesButton = new System.Windows.Forms.Button();
            this.formatsLabel = new System.Windows.Forms.Label();
            this.optimiseGroup = new System.Windows.Forms.GroupBox();
            this.pinOptimisation = new System.Windows.Forms.CheckBox();
            this.optimiseLabel = new System.Windows.Forms.Label();
            this.optimiseDescription = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.applyButton = new System.Windows.Forms.Button();
            this.portError = new System.Windows.Forms.ErrorProvider(this.components);
            this.statusLabel = new System.Windows.Forms.Label();
            this.bonjourError = new System.Windows.Forms.ErrorProvider(this.components);
            this.serverGroup.SuspendLayout();
            this.transcodeGroup.SuspendLayout();
            this.formatsGroup.SuspendLayout();
            this.optimiseGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portError)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bonjourError)).BeginInit();
            this.SuspendLayout();
            // 
            // serverNameLabel
            // 
            this.serverNameLabel.AutoSize = true;
            this.serverNameLabel.Location = new System.Drawing.Point(6, 16);
            this.serverNameLabel.Name = "serverNameLabel";
            this.serverNameLabel.Size = new System.Drawing.Size(68, 13);
            this.serverNameLabel.TabIndex = 0;
            this.serverNameLabel.Text = "server name:";
            // 
            // serverNameInput
            // 
            this.serverNameInput.Location = new System.Drawing.Point(80, 13);
            this.serverNameInput.MaxLength = 255;
            this.serverNameInput.Name = "serverNameInput";
            this.serverNameInput.Size = new System.Drawing.Size(195, 20);
            this.serverNameInput.TabIndex = 1;
            // 
            // portLabel
            // 
            this.portLabel.AutoSize = true;
            this.portLabel.Location = new System.Drawing.Point(46, 39);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(28, 13);
            this.portLabel.TabIndex = 2;
            this.portLabel.Text = "port:";
            // 
            // transcodeLabel
            // 
            this.transcodeLabel.AutoSize = true;
            this.transcodeLabel.Location = new System.Drawing.Point(17, 149);
            this.transcodeLabel.Name = "transcodeLabel";
            this.transcodeLabel.Size = new System.Drawing.Size(0, 13);
            this.transcodeLabel.TabIndex = 6;
            // 
            // serverGroup
            // 
            this.serverGroup.Controls.Add(this.serverNameLabel);
            this.serverGroup.Controls.Add(this.serverNameInput);
            this.serverGroup.Controls.Add(this.portInput);
            this.serverGroup.Controls.Add(this.portLabel);
            this.serverGroup.Location = new System.Drawing.Point(12, 12);
            this.serverGroup.Name = "serverGroup";
            this.serverGroup.Size = new System.Drawing.Size(281, 64);
            this.serverGroup.TabIndex = 7;
            this.serverGroup.TabStop = false;
            this.serverGroup.Text = "server";
            // 
            // portInput
            // 
            this.portInput.Location = new System.Drawing.Point(80, 36);
            this.portInput.MaxLength = 255;
            this.portInput.Name = "portInput";
            this.portInput.Size = new System.Drawing.Size(195, 20);
            this.portInput.TabIndex = 3;
            this.portInput.TextChanged += new System.EventHandler(this.portInput_TextChanged);
            // 
            // transcodeGroup
            // 
            this.transcodeGroup.Controls.Add(this.replayGainSmart);
            this.transcodeGroup.Controls.Add(this.replayGainAlbum);
            this.transcodeGroup.Controls.Add(this.replayGainTrack);
            this.transcodeGroup.Controls.Add(this.replayGainLabel);
            this.transcodeGroup.Controls.Add(this.replayGainOff);
            this.transcodeGroup.Controls.Add(this.dsp);
            this.transcodeGroup.Controls.Add(this.mbAudio);
            this.transcodeGroup.Controls.Add(this.pcm);
            this.transcodeGroup.Location = new System.Drawing.Point(12, 182);
            this.transcodeGroup.Name = "transcodeGroup";
            this.transcodeGroup.Size = new System.Drawing.Size(281, 115);
            this.transcodeGroup.TabIndex = 8;
            this.transcodeGroup.TabStop = false;
            this.transcodeGroup.Text = "transcode";
            // 
            // replayGainSmart
            // 
            this.replayGainSmart.AutoSize = true;
            this.replayGainSmart.Location = new System.Drawing.Point(224, 90);
            this.replayGainSmart.Name = "replayGainSmart";
            this.replayGainSmart.Size = new System.Drawing.Size(50, 17);
            this.replayGainSmart.TabIndex = 7;
            this.replayGainSmart.TabStop = true;
            this.replayGainSmart.Text = "smart";
            this.replayGainSmart.UseVisualStyleBackColor = true;
            // 
            // replayGainAlbum
            // 
            this.replayGainAlbum.AutoSize = true;
            this.replayGainAlbum.Location = new System.Drawing.Point(169, 90);
            this.replayGainAlbum.Name = "replayGainAlbum";
            this.replayGainAlbum.Size = new System.Drawing.Size(53, 17);
            this.replayGainAlbum.TabIndex = 6;
            this.replayGainAlbum.TabStop = true;
            this.replayGainAlbum.Text = "album";
            this.replayGainAlbum.UseVisualStyleBackColor = true;
            // 
            // replayGainTrack
            // 
            this.replayGainTrack.AutoSize = true;
            this.replayGainTrack.Location = new System.Drawing.Point(116, 90);
            this.replayGainTrack.Name = "replayGainTrack";
            this.replayGainTrack.Size = new System.Drawing.Size(49, 17);
            this.replayGainTrack.TabIndex = 5;
            this.replayGainTrack.TabStop = true;
            this.replayGainTrack.Text = "track";
            this.replayGainTrack.UseVisualStyleBackColor = true;
            // 
            // replayGainLabel
            // 
            this.replayGainLabel.AutoSize = true;
            this.replayGainLabel.Location = new System.Drawing.Point(7, 90);
            this.replayGainLabel.Name = "replayGainLabel";
            this.replayGainLabel.Size = new System.Drawing.Size(61, 13);
            this.replayGainLabel.TabIndex = 4;
            this.replayGainLabel.Text = "replay gain:";
            // 
            // replayGainOff
            // 
            this.replayGainOff.AutoSize = true;
            this.replayGainOff.Location = new System.Drawing.Point(74, 90);
            this.replayGainOff.Name = "replayGainOff";
            this.replayGainOff.Size = new System.Drawing.Size(37, 17);
            this.replayGainOff.TabIndex = 3;
            this.replayGainOff.TabStop = true;
            this.replayGainOff.Text = "off";
            this.replayGainOff.UseVisualStyleBackColor = true;
            // 
            // dsp
            // 
            this.dsp.AutoSize = true;
            this.dsp.Location = new System.Drawing.Point(11, 66);
            this.dsp.Name = "dsp";
            this.dsp.Size = new System.Drawing.Size(71, 17);
            this.dsp.TabIndex = 2;
            this.dsp.Text = "apply dsp";
            this.dsp.UseVisualStyleBackColor = true;
            // 
            // mbAudio
            // 
            this.mbAudio.AutoSize = true;
            this.mbAudio.Location = new System.Drawing.Point(11, 43);
            this.mbAudio.Name = "mbAudio";
            this.mbAudio.Size = new System.Drawing.Size(167, 17);
            this.mbAudio.TabIndex = 1;
            this.mbAudio.Text = "apply musicbee audio settings";
            this.mbAudio.UseVisualStyleBackColor = true;
            // 
            // pcm
            // 
            this.pcm.AutoSize = true;
            this.pcm.Location = new System.Drawing.Point(11, 20);
            this.pcm.Name = "pcm";
            this.pcm.Size = new System.Drawing.Size(108, 17);
            this.pcm.TabIndex = 0;
            this.pcm.Text = "transcode to pcm";
            this.pcm.UseVisualStyleBackColor = true;
            // 
            // transcodeFormats
            // 
            this.transcodeFormats.FormattingEnabled = true;
            this.transcodeFormats.Items.AddRange(new object[] {
            "aac",
            "aiff",
            "alac",
            "asx",
            "dsd",
            "flac",
            "mp3",
            "mpc",
            "ogg",
            "opus",
            "spx",
            "tak",
            "wavpack",
            "wma"});
            this.transcodeFormats.Location = new System.Drawing.Point(300, 15);
            this.transcodeFormats.Name = "transcodeFormats";
            this.transcodeFormats.Size = new System.Drawing.Size(91, 259);
            this.transcodeFormats.TabIndex = 9;
            // 
            // formatsGroup
            // 
            this.formatsGroup.Controls.Add(this.allButton);
            this.formatsGroup.Controls.Add(this.noneButton);
            this.formatsGroup.Controls.Add(this.iTunesButton);
            this.formatsGroup.Controls.Add(this.formatsLabel);
            this.formatsGroup.Controls.Add(this.transcodeFormats);
            this.formatsGroup.Location = new System.Drawing.Point(301, 13);
            this.formatsGroup.Name = "formatsGroup";
            this.formatsGroup.Size = new System.Drawing.Size(397, 284);
            this.formatsGroup.TabIndex = 10;
            this.formatsGroup.TabStop = false;
            this.formatsGroup.Text = "formats";
            // 
            // allButton
            // 
            this.allButton.Location = new System.Drawing.Point(56, 250);
            this.allButton.Name = "allButton";
            this.allButton.Size = new System.Drawing.Size(75, 23);
            this.allButton.TabIndex = 13;
            this.allButton.Text = "All";
            this.allButton.UseVisualStyleBackColor = true;
            this.allButton.Click += new System.EventHandler(this.allButton_Click);
            // 
            // noneButton
            // 
            this.noneButton.Location = new System.Drawing.Point(137, 250);
            this.noneButton.Name = "noneButton";
            this.noneButton.Size = new System.Drawing.Size(75, 23);
            this.noneButton.TabIndex = 12;
            this.noneButton.Text = "None";
            this.noneButton.UseVisualStyleBackColor = true;
            this.noneButton.Click += new System.EventHandler(this.noneButton_Click);
            // 
            // iTunesButton
            // 
            this.iTunesButton.Location = new System.Drawing.Point(218, 250);
            this.iTunesButton.Name = "iTunesButton";
            this.iTunesButton.Size = new System.Drawing.Size(75, 23);
            this.iTunesButton.TabIndex = 11;
            this.iTunesButton.Text = "iTunes";
            this.iTunesButton.UseVisualStyleBackColor = true;
            this.iTunesButton.Click += new System.EventHandler(this.iTunesButton_Click);
            // 
            // formatsLabel
            // 
            this.formatsLabel.Location = new System.Drawing.Point(6, 18);
            this.formatsLabel.Name = "formatsLabel";
            this.formatsLabel.Size = new System.Drawing.Size(288, 256);
            this.formatsLabel.TabIndex = 10;
            this.formatsLabel.Text = resources.GetString("formatsLabel.Text");
            // 
            // optimiseGroup
            // 
            this.optimiseGroup.Controls.Add(this.pinOptimisation);
            this.optimiseGroup.Controls.Add(this.optimiseLabel);
            this.optimiseGroup.Controls.Add(this.optimiseDescription);
            this.optimiseGroup.Location = new System.Drawing.Point(14, 82);
            this.optimiseGroup.Name = "optimiseGroup";
            this.optimiseGroup.Size = new System.Drawing.Size(281, 96);
            this.optimiseGroup.TabIndex = 11;
            this.optimiseGroup.TabStop = false;
            this.optimiseGroup.Text = "optimise";
            // 
            // pinOptimisation
            // 
            this.pinOptimisation.AutoSize = true;
            this.pinOptimisation.Enabled = false;
            this.pinOptimisation.Location = new System.Drawing.Point(9, 70);
            this.pinOptimisation.Name = "pinOptimisation";
            this.pinOptimisation.Size = new System.Drawing.Size(236, 17);
            this.pinOptimisation.TabIndex = 3;
            this.pinOptimisation.Text = "Pin client. Prevent optimising for other clients";
            this.pinOptimisation.UseVisualStyleBackColor = true;
            // 
            // optimiseLabel
            // 
            this.optimiseLabel.Location = new System.Drawing.Point(6, 49);
            this.optimiseLabel.Margin = new System.Windows.Forms.Padding(0);
            this.optimiseLabel.Name = "optimiseLabel";
            this.optimiseLabel.Size = new System.Drawing.Size(267, 16);
            this.optimiseLabel.TabIndex = 2;
            this.optimiseLabel.Text = "Currently optimised to:";
            // 
            // optimiseDescription
            // 
            this.optimiseDescription.Location = new System.Drawing.Point(5, 16);
            this.optimiseDescription.Name = "optimiseDescription";
            this.optimiseDescription.Size = new System.Drawing.Size(275, 35);
            this.optimiseDescription.TabIndex = 0;
            this.optimiseDescription.Text = "The server optimises itself to quickly establish new connections to the last clie" +
    "nt that connected to it.\r\n";
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(623, 303);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 12;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(542, 303);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 13;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // applyButton
            // 
            this.applyButton.Location = new System.Drawing.Point(461, 303);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 14;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // portError
            // 
            this.portError.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.portError.ContainerControl = this;
            this.portError.RightToLeft = true;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(301, 308);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 13);
            this.statusLabel.TabIndex = 15;
            // 
            // bonjourError
            // 
            this.bonjourError.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.bonjourError.ContainerControl = this;
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(710, 333);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.optimiseGroup);
            this.Controls.Add(this.formatsGroup);
            this.Controls.Add(this.transcodeGroup);
            this.Controls.Add(this.serverGroup);
            this.Controls.Add(this.transcodeLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ConfigForm";
            this.Text = "MusicBeeDaap";
            this.serverGroup.ResumeLayout(false);
            this.serverGroup.PerformLayout();
            this.transcodeGroup.ResumeLayout(false);
            this.transcodeGroup.PerformLayout();
            this.formatsGroup.ResumeLayout(false);
            this.optimiseGroup.ResumeLayout(false);
            this.optimiseGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portError)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bonjourError)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label serverNameLabel;
        private System.Windows.Forms.TextBox serverNameInput;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.Label transcodeLabel;
        private System.Windows.Forms.GroupBox serverGroup;
        private System.Windows.Forms.GroupBox transcodeGroup;
        private System.Windows.Forms.CheckBox dsp;
        private System.Windows.Forms.CheckBox mbAudio;
        private System.Windows.Forms.CheckBox pcm;
        private System.Windows.Forms.CheckedListBox transcodeFormats;
        private System.Windows.Forms.GroupBox formatsGroup;
        private System.Windows.Forms.Label formatsLabel;
        private System.Windows.Forms.TextBox portInput;
        private System.Windows.Forms.GroupBox optimiseGroup;
        private System.Windows.Forms.Label optimiseDescription;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button allButton;
        private System.Windows.Forms.Button noneButton;
        private System.Windows.Forms.Button iTunesButton;
        private System.Windows.Forms.Label replayGainLabel;
        private System.Windows.Forms.RadioButton replayGainOff;
        private System.Windows.Forms.RadioButton replayGainTrack;
        private System.Windows.Forms.RadioButton replayGainSmart;
        private System.Windows.Forms.RadioButton replayGainAlbum;
        private System.Windows.Forms.ErrorProvider portError;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ErrorProvider bonjourError;
        private System.Windows.Forms.CheckBox pinOptimisation;
        private System.Windows.Forms.Label optimiseLabel;
    }
}