using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

// Todo: display error when no zeroconf service is running

namespace MusicBeePlugin
{
    public struct Settings {
        public string serverName;
        public ushort serverPort;
        public List<string> artworkPatterns;
        public AudioStream.TranscodeOptions transcode;
    }

    public partial class Plugin
    {
        public static MusicBeeApiInterface mbApi;
        public static Settings settings;
        
        private DAAP.Server server;
        private DAAP.MusicBeeDatabase db;
        private MusicBeeRevisionManager revisionManager;

        // mbTracks maintains a unique id for each file in the library
        public static TrackList mbTracks;
        private PluginInfo about = new PluginInfo();
        
        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApi = new MusicBeeApiInterface();
            mbApi.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "DAAP Server";
            about.Description = "Stream MusicBee's library over DAAP.";
            about.Author = "Graeme Phillips";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.Upnp;
            about.VersionMajor = 0;  // your plugin version
            about.VersionMinor = 1;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags)0xFFFF;
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApi.Setting_GetPersistentStoragePath();

            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "first";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApi.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            revisionManager.Stop();
            server.Stop();
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    
                    settings.serverName = "MusicBee";
                    settings.serverPort = 3690;
                    
                    // iTunes
                    AudioStream.SetDirectStreamFormats(new FileCodec[]
                    {
                        FileCodec.Mp3,
                        FileCodec.Aiff,
                        FileCodec.Wave,
                        FileCodec.Aac,
                        FileCodec.Alac
                    });

                    settings.transcode = new AudioStream.TranscodeOptions {
                        usePcm = true,
                        enableDSP = false,
                        useMusicBeeSettings = false,
                        replayGainMode = ReplayGainMode.Off
                    };

                    string dataPath = mbApi.Setting_GetPersistentStoragePath();
                    settings.artworkPatterns = new List<string>();

                    try {
                        using (XmlReader mbConfig = XmlReader.Create(Path.Combine(dataPath, "MusicBeeSettings.ini"), new XmlReaderSettings { IgnoreWhitespace = true })) {
                            mbConfig.ReadToFollowing("TagArtworkScanFilter");

                            using (XmlReader filterReader = mbConfig.ReadSubtree()) {
                                while (filterReader.Read()) {
                                    if (filterReader.NodeType == XmlNodeType.Text && mbConfig.Value != "*.*") {
                                        settings.artworkPatterns.Add(mbConfig.Value);
                                    }
                                }
                            }
                        };
                    } catch (Exception) { }

                    InitialiseServer();
                    break;
                case NotificationType.FileAddedToInbox:
                case NotificationType.FileAddedToLibrary:
                    revisionManager.Notify(MusicBeeRevisionManager.NotificationType.FileAdded);
                    break;
                case NotificationType.FileDeleted:
                    revisionManager.Notify(MusicBeeRevisionManager.NotificationType.FileRemoved);
                    break;
                case NotificationType.TagsChanged:
                    revisionManager.Notify(MusicBeeRevisionManager.NotificationType.FileChanged);
                    break;
                case NotificationType.LibrarySwitched:
                    server.Stop();
                    revisionManager.Reset();
                    server.Start();
                    break;
                default:
                    break;
            }

        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return null;
        }

        private void InitialiseServer()
        {
            mbTracks = new TrackList();
            db = new DAAP.MusicBeeDatabase(settings.serverName);
            revisionManager = new MusicBeeRevisionManager(db);
            mbTracks.RevisionManager = revisionManager;

            server = new DAAP.Server(settings.serverName, revisionManager);
            server.Port = settings.serverPort;

            server.Collision += (o, args) =>
            {
                if (server.Name.Length > settings.serverName.Length) {
                    int next = int.Parse(server.Name.Substring(server.Name.Length + 1)) + 1;
                    server.Name = settings.serverName + " " + next.ToString();
                } else {
                    server.Name += " 2";
                }
            };

            server.AddDatabase(db);
            server.Start();
        }

   }
}