using Microsoft.Win32;
using MusicBeePlugin.src;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MusicBeePlugin
{
    [Flags()]
    enum PluginError
    {
        None = 0,
        Initialising = 1,
        PortTaken = 2,
        BonjourNotFound = 4
    }

    public class Settings
    {
        public string serverName = "MusicBee";
        public ushort serverPort = 3690;
        public AudioStream.TranscodeOptions transcode = AudioStream.TranscodeOptions.WithDefaultTranscodeFormats();
        public string optimisedUserAgent = "iTunes/12.3.3";
        public string optimisedMetadata = "dmap.itemid,dmap.itemname,dmap.itemkind,dmap.persistentid,daap.songalbum,daap.songgrouping,daap.songartist,daap.songalbumartist,daap.songbitrate,daap.songbeatsperminute,daap.songcomment,daap.songcodectype,daap.songcodecsubtype,daap.songcompilation,daap.songcomposer,daap.songdateadded,daap.songdatemodified,daap.songdisccount,daap.songdiscnumber,daap.songdisabled,daap.songeqpreset,daap.songformat,daap.songgenre,daap.songdescription,daap.songrelativevolume,daap.songsamplerate,daap.songsize,daap.songstarttime,daap.songstoptime,daap.songtime,daap.songtrackcount,daap.songtracknumber,daap.songuserrating,daap.songyear,daap.songdatakind,daap.songdataurl,daap.songcontentrating,com.apple.itunes.norm-volume,com.apple.itunes.itms-songid,com.apple.itunes.itms-artistid,com.apple.itunes.itms-playlistid,com.apple.itunes.itms-composerid,com.apple.itunes.itms-genreid,com.apple.itunes.itms-storefrontid,com.apple.itunes.has-videodaap.songcategory,daap.songextradata,daap.songcontentdescription,daap.songlongcontentdescription,com.apple.itunes.is-podcast,com.apple.itunes.mediakind,com.apple.itunes.extended-media-kind,com.apple.itunes.series-name,com.apple.itunes.episode-num-str,com.apple.itunes.episode-sort,com.apple.itunes.season-num,daap.songgapless,com.apple.itunes.gapless-enc-del,com.apple.itunes.gapless-heur,com.apple.itunes.gapless-enc-dr,com.apple.itunes.gapless-dur,com.apple.itunes.gapless-resy,com.apple.itunes.content-rating";
    }

    public static class ThreadExecutionState
    {
        [Flags()]
        enum ExecutionState : uint
        {
            None = 0,
            AwayModeRequired = 0x00000040,
            Continous = 0x80000000,
            DisplayRequired = 0x00000002,
            SystemRequired = 0x00000001,
            UserPresent = 0x00000004
        }

        [DllImport("kernel32.dll")]
        private extern static ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        internal static void PreventSleep()
        {
            SetThreadExecutionState(ExecutionState.Continous | ExecutionState.SystemRequired);
        }

        internal static void AllowSleep()
        {
            SetThreadExecutionState(ExecutionState.Continous);
        }

        internal static void ResetSleepTimer()
        {
            SetThreadExecutionState(ExecutionState.None);
        }
    }


    public partial class Plugin
    {
        private const string SETTINGS_FILE = "MusicBeeDaap.xml";

        public static MusicBeeApiInterface mbApi;

        public static Settings settings = new Settings { };
        public static string[] iTunesFormats = { "mp3", "aiff", "wave", "aac", "alac" };
        public static List<string> artworkPatterns = new List<string>();

        private DAAP.Server server;
        private DAAP.MusicBeeDatabase db;
        private MusicBeeRevisionManager revisionManager;
        public static TrackList mbTracks;

        private PluginError errors = PluginError.Initialising;

        private bool optimising = false;
        ConfigForm configForm;


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

             if (configForm == null) {
                configForm = new ConfigForm(this);
                configForm.SetFormSettings(settings);
                configForm.SetMessages(errors);

                configForm.FormClosed += delegate {
                    CancelOptimisation();
                    configForm = null;
                };

                configForm.Show();
            } else {
                configForm.SetMessages(errors);
                configForm.Focus();
            }

            return true;
        }

        private static bool UpdateField<T>(string name, T oldFields, T newFields)
        {
            bool updated = false;

            var field = typeof(T).GetField(name);
            var oldValue = field.GetValue(oldFields);
            var newValue = field.GetValue(newFields);

            if (oldValue.Equals(newValue) == false) {
                field.SetValue(oldFields, newValue);
                updated = true;
            }

            return updated;
        }

        internal async void ApplyAndSave(Settings newSettings)
        {
            bool settingsModified = false;
            bool serverRestartRequired = false;

            settingsModified |= UpdateField("serverName", settings, newSettings);
            settingsModified |= UpdateField("serverPort", settings, newSettings);
            serverRestartRequired = settingsModified;

            settingsModified |= UpdateField("usePCM", settings.transcode, newSettings.transcode);
            settingsModified |= UpdateField("useMusicBeeSettings", settings.transcode, newSettings.transcode);
            settingsModified |= UpdateField("enableDSP", settings.transcode, newSettings.transcode);
            settingsModified |= UpdateField("replayGainMode", settings.transcode, newSettings.transcode);

            if (settings.transcode.formats.Count == newSettings.transcode.formats.Count) {
                settings.transcode.formats.Sort();
                newSettings.transcode.formats.Sort();

                for (int index = 0; index < settings.transcode.formats.Count; index++) {
                    if (settings.transcode.formats[index] != newSettings.transcode.formats[index]) {
                        settings.transcode.formats = newSettings.transcode.formats;
                        settingsModified = true;
                        break;
                    }
                }
            } else {
                settings.transcode.formats = newSettings.transcode.formats;
                settingsModified = true;
            }
            

            if (settingsModified) {
                if (serverRestartRequired) {
                    await RestartServer();
                }

                if (errors == PluginError.None) {
                    WriteSettings();
                }
            }
        }

        private void WriteSettings()
        {
            string dataPath = mbApi.Setting_GetPersistentStoragePath();

            using (XmlWriter settingsFile = XmlWriter.Create(Path.Combine(dataPath, SETTINGS_FILE), new XmlWriterSettings { Indent = true })) {
                XmlSerializer serialiser = new XmlSerializer(typeof(Settings));
                serialiser.Serialize(settingsFile, settings);
            }
        }

        private Settings LoadSettings()
        {
            string dataPath = mbApi.Setting_GetPersistentStoragePath();
            Settings result = null;

            using (XmlReader settingsFile = XmlReader.Create(Path.Combine(dataPath, SETTINGS_FILE))) {
                XmlSerializer serialiser = new XmlSerializer(typeof(Settings));
                result = (Settings)serialiser.Deserialize(settingsFile);
            }

            if (result == null) {
                result = new Settings { };
            }

            return result;
        }

        private void OptimiseHandler(object o, DAAP.DatabaseRequestedArgs info)
        {
            lock (settings) {
                if (optimising) {
                    optimising = false;
                    server.DatabaseRequested -= OptimiseHandler;
                    
                    if (info.userAgent != null) {
                        settings.optimisedMetadata = info.daapMeta;
                        settings.optimisedUserAgent = info.userAgent;

                        WriteSettings();

                        db.CacheContentNodes(info.daapMeta);
                    }

                    if (configForm != null) {
                        configForm.SetFormSettings(settings);
                    }
                }                
            }
        }

        internal void OptimiseForNextRequest()
        {
            lock (settings) {
                optimising = true;
                server.DatabaseRequested += OptimiseHandler;
            }
        }

        internal void CancelOptimisation()
        {
            if (Monitor.TryEnter(settings)) {
                if (optimising) {
                    optimising = false;
                    server.DatabaseRequested -= OptimiseHandler;
                }

                Monitor.Exit(settings);
            }
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
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
            string dataPath = mbApi.Setting_GetPersistentStoragePath();
            try {
                File.Delete(Path.Combine(dataPath, SETTINGS_FILE));
            } catch { };
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            switch (type) {
                case NotificationType.PluginStartup:
                    settings = LoadSettings();

                    try {
                        string dataPath = mbApi.Setting_GetPersistentStoragePath();
                        using (XmlReader mbConfig = XmlReader.Create(Path.Combine(dataPath, "MusicBeeSettings.ini"), new XmlReaderSettings { IgnoreWhitespace = true })) {
                            mbConfig.ReadToFollowing("TagArtworkScanFilter");

                            using (XmlReader filterReader = mbConfig.ReadSubtree()) {
                                while (filterReader.Read()) {
                                    if (filterReader.NodeType == XmlNodeType.Text && mbConfig.Value != "*.*") {
                                        artworkPatterns.Add(mbConfig.Value);
                                    }
                                }
                            }
                        };
                    } catch (Exception) { }

                    mbTracks = new TrackList();
                    db = new DAAP.MusicBeeDatabase(settings.serverName, settings.optimisedMetadata);
                    revisionManager = new MusicBeeRevisionManager(db);

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
                    RestartServer();
                    break;
                default:
                    break;
            }
        }

        private void InitialiseServer()
        {
            errors = PluginError.Initialising;

            try {
                // Initialise bonjour provider on this thread so we can catch the exception
                Mono.Zeroconf.Providers.IZeroconfProvider provider = Mono.Zeroconf.Providers.ProviderFactory.SelectedProvider;

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

                server.TrackRequested += OnTrackRequest;

                if (BonjourWakeOnDemandEnabled() == false) {
                    server.UserLogin += OnLogin;
                    server.UserLogout += OnLogout;
                }

                server.UserLogin += revisionManager.OnLogin;
                server.UserLogout += revisionManager.OnLogout;

                server.AddDatabase(db);
                server.Start();

                errors = PluginError.None;
            } catch (SocketException) {
                errors = PluginError.PortTaken;
            } catch (Mono.Zeroconf.Providers.Bonjour.ServiceErrorException) {
                errors = PluginError.BonjourNotFound;
            } catch (Exception) {
                // Fatal.
                if (configForm != null) {
                    configForm.Close();
                }

                mbApi.MB_SendNotification(CallbackType.DisablePlugin);

            }

            if (configForm != null) {
                configForm.SetMessages(errors);
            }
        }

        private async Task<PluginError> RestartServer()
        {
            Task work = Task.Factory.StartNew(new Action(delegate {
                server.Stop();
                revisionManager.Reset();
                InitialiseServer();
            }));

            await work;

            return errors;
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return null;
        }

        private static bool BonjourWakeOnDemandEnabled()
        {
            bool result = false;

            RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey powerManagement = localMachine.OpenSubKey("SOFTWARE\\Apple Inc.\\Bonjour\\Power Management");

            if (powerManagement != null) {
                result = 1.Equals(powerManagement.GetValue("Enabled", 0));
            }
            
            return result;
        }
        
        public static string TrimToCharacter(string s, char c)
        {
            int index = 0;
            while (index < s.Length) {
                if (s[index] == c) {
                    return s.Substring(0, index);
                }

                index++;
            }

            return s;
        }

        private static void OnLogin(object sender, DAAP.UserArgs args)
        {
            ThreadExecutionState.PreventSleep();
        }

        private static void OnLogout(object sender, DAAP.UserArgs args)
        {
            ThreadExecutionState.AllowSleep();
        }

        private static void OnTrackRequest(object sender, DAAP.TrackRequestedArgs args)
        {
            ThreadExecutionState.ResetSleepTimer();
        }
    }
}