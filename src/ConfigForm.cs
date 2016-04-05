using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusicBeePlugin.src
{
    public partial class ConfigForm : Form
    {
        Plugin instance; 
        private ushort port;
        
        public ConfigForm(Plugin instance, Settings settings, PluginError errors)
        {
            InitializeComponent();
            this.instance = instance;

            SetFormSettingsInternal(settings);
            SetMessagesInternal(errors);
        }

        internal void SetMessages(PluginError errors)
        {
            if (InvokeRequired) {
                Invoke(new MethodInvoker(() => SetMessagesInternal(errors)));
            } else {
                SetMessagesInternal(errors);
            }
        }

        internal void SetFormSettings(Settings settings)
        {
            if (InvokeRequired) {
                Invoke(new MethodInvoker(() => SetFormSettingsInternal(settings)));
            } else {
                SetFormSettingsInternal(settings);
            }            
        }

        private void SetFormSettingsInternal(Settings settings)
        {
            serverNameInput.Text = settings.serverName;
            portInput.Text = settings.serverPort.ToString();

            pinOptimisation.Checked = settings.optimisationPinned;
            pinOptimisation.Enabled = settings.optimisedMetadata != null;
            optimiseLabel.Text = Plugin.TrimToCharacter(optimiseLabel.Text, ':') + ": " + (settings.optimisedUserAgent ?? "None");

            pcm.Checked = settings.transcode.usePCM;
            mbAudio.Checked = settings.transcode.useMusicBeeSettings;
            dsp.Checked = settings.transcode.enableDSP;

            switch (settings.transcode.replayGainMode) {
                case Plugin.ReplayGainMode.Album:
                    replayGainAlbum.Checked = true;
                    break;
                case Plugin.ReplayGainMode.Track:
                    replayGainTrack.Checked = true;
                    break;
                case Plugin.ReplayGainMode.Smart:
                    replayGainSmart.Checked = true;
                    break;
                default:
                    replayGainOff.Checked = true;
                    break;
            }

            for (int i = 0; i < transcodeFormats.Items.Count; i++) {
                var item = transcodeFormats.Items[i];
                Plugin.FileCodec codec = AudioStream.GetFileCodec(transcodeFormats.GetItemText(item));
                Debug.Assert(codec != Plugin.FileCodec.Unknown);
                if (settings.transcode.formats.Contains(codec)) {
                    transcodeFormats.SetItemChecked(i, true);
                }
            }
        }
        
        private void SetMessagesInternal(PluginError errors)
        {
            if (errors.HasFlag(PluginError.Initialising)) {
                statusLabel.Text = "Server is still setting up...";
                bonjourError.Clear();
            } else if (errors.HasFlag(PluginError.BonjourNotFound)) {
                statusLabel.Text = "Apple\'s Bonjour is required.";
                bonjourError.SetError(statusLabel, "Unable to connect to bonjour");
            } else {
                statusLabel.Text = "";
                bonjourError.Clear();
            }

            if (errors.HasFlag(PluginError.PortTaken)) {
                portError.SetError(portLabel, "This port is taken");
            } else {
                portError.Clear();
            }
        }

        private void portInput_TextChanged(object sender, EventArgs e)
        {
            ushort validPort = port;

            if (portInput.Text != String.Empty && ushort.TryParse(portInput.Text, out port) == false) {
                int selection = portInput.SelectionStart;
                portInput.Text = validPort.ToString();
                portInput.SelectionStart = Math.Max(0, Math.Min(selection, portInput.Text.Length - 1) - 1);
            }
        }
        
        private void allButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < transcodeFormats.Items.Count; i++) {
                transcodeFormats.SetItemChecked(i, true);
            }
        }

        private void noneButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < transcodeFormats.Items.Count; i++) {
                transcodeFormats.SetItemChecked(i, false);
            }
        }

        private void iTunesButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < transcodeFormats.Items.Count; i++) {
                string format = transcodeFormats.GetItemText(transcodeFormats.Items[i]);
                transcodeFormats.SetItemChecked(i, Plugin.iTunesFormats.Contains(format) == false);
            }
        }
        
        private void applyButton_Click(object sender, EventArgs e)
        {
            List<Plugin.FileCodec> formats = new List<Plugin.FileCodec> { Plugin.FileCodec.Unknown };
            foreach (var item in transcodeFormats.CheckedItems) {
                Plugin.FileCodec codec = AudioStream.GetFileCodec(transcodeFormats.GetItemText(item));
                Debug.Assert(codec != Plugin.FileCodec.Unknown);
                formats.Add(codec);
            }

            ushort validPort;
            if (ushort.TryParse(portInput.Text, out validPort)) {
                if (port != validPort && port > 1) {
                    validPort = Plugin.settings.serverPort;
                }
            }

            Settings settings = new Settings
            {
                serverName = serverNameInput.Text,
                serverPort = validPort,
                transcode = new AudioStream.TranscodeOptions
                {
                    usePCM = pcm.Checked,
                    useMusicBeeSettings = mbAudio.Checked,
                    enableDSP = dsp.Checked,
                    replayGainMode = replayGainTrack.Checked ? Plugin.ReplayGainMode.Track
                                   : replayGainAlbum.Checked ? Plugin.ReplayGainMode.Album
                                   : replayGainSmart.Checked ? Plugin.ReplayGainMode.Smart
                                   : Plugin.ReplayGainMode.Off,
                    formats = formats
                },
                optimisedUserAgent = null,
                optimisedMetadata = null,
                optimisationPinned = pinOptimisation.Checked
            };

            instance.ApplyAndSave(settings);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            applyButton_Click(sender, e);
            closeButton_Click(sender, e);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
