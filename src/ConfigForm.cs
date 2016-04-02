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
        private bool optimising = false;
        
        public ConfigForm(Plugin instance)
        {
            InitializeComponent();
            this.instance = instance;
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

        private void optimiseButton_Click(object sender, EventArgs e)
        {
            if (optimising == false) {
                SetOptimiseLabels("waiting for connection...", "cancel");
                instance.OptimiseForNextRequest();
            } else {
                SetOptimiseLabels(Plugin.settings.optimisedUserAgent, "optimise");
                instance.CancelOptimisation();
            }

            optimising = !optimising;
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
            List<Plugin.FileCodec> formats = new List<Plugin.FileCodec>();
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
                optimisedMetadata = null
            };

            instance.ApplyAndSave(settings);
        }

        private void SetOptimiseLabels(string labelText, string buttonText)
        {
            MethodInvoker setter = delegate {
                optimiseLabel.Text = Plugin.TrimToCharacter(optimiseLabel.Text, ':') + ": " + labelText;
                optimiseButton.Text = buttonText;
            };

            if (InvokeRequired) {
                Invoke(setter);
            } else {
                setter();
            }
        }

        internal void SetFormSettings(Settings settings)
        {
            serverNameInput.Text = settings.serverName;
            portInput.Text = settings.serverPort.ToString();

            SetOptimiseLabels(settings.optimisedUserAgent, "optimise");

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
