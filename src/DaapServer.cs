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
    public enum PluginError
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
        public string optimisedUserAgent = "None";
        public string optimisedMetadata = null;
        public bool optimisationPinned = false;
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
            SetThreadExecutionState(ExecutionState.Continous | ExecutionState.SystemRequired | ExecutionState.AwayModeRequired);
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

        private ConfigForm configForm;

        private PluginInfo about = new PluginInfo();

        public string SettingsFilePath {
            get {
                return Path.Combine(mbApi.Setting_GetPersistentStoragePath(), SETTINGS_FILE);
            }
        }
        
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
                configForm = new ConfigForm(this, settings, errors);

                configForm.FormClosed += (sender, e) => {
                    configForm = null;
                };

                configForm.Show();
            } else {
                configForm.SetMessages(errors);
                configForm.Focus();
            }

            return true;
        }

        internal void ApplyAndSave(Settings newSettings)
        {
            bool settingsModified = false;
            bool serverRestartRequired = false;
            bool formatsToTranscodeChanged = false;

            settingsModified |= UpdateField(ref settings.serverName, newSettings.serverName);
            settingsModified |= UpdateField(ref settings.serverPort, newSettings.serverPort);
            serverRestartRequired = settingsModified;

            settingsModified |= UpdateField(ref settings.optimisationPinned, newSettings.optimisationPinned);

            if ((settings.transcode.useMusicBeeSettings || settings.transcode.enableDSP || settings.transcode.replayGainMode != ReplayGainMode.Off)
                != (newSettings.transcode.useMusicBeeSettings || newSettings.transcode.enableDSP || newSettings.transcode.replayGainMode != ReplayGainMode.Off)) {
                formatsToTranscodeChanged = true;
            }

            settingsModified |= UpdateField(ref settings.transcode.usePCM, newSettings.transcode.usePCM);
            settingsModified |= UpdateField(ref settings.transcode.useMusicBeeSettings, newSettings.transcode.useMusicBeeSettings);
            settingsModified |= UpdateField(ref settings.transcode.enableDSP, newSettings.transcode.enableDSP);
            settingsModified |= UpdateField(ref settings.transcode.replayGainMode, newSettings.transcode.replayGainMode);

            if (settings.transcode.formats.Count == newSettings.transcode.formats.Count) {
                settings.transcode.formats.Sort();
                newSettings.transcode.formats.Sort();

                for (int index = 0; index < settings.transcode.formats.Count; index++) {
                    if (settings.transcode.formats[index] != newSettings.transcode.formats[index]) {
                        settings.transcode.formats = newSettings.transcode.formats;
                        settingsModified = formatsToTranscodeChanged = true;
                        break;
                    }
                }
            } else {
                settings.transcode.formats = newSettings.transcode.formats;
                settingsModified = formatsToTranscodeChanged = true;
            }

            serverRestartRequired = serverRestartRequired || formatsToTranscodeChanged || errors != PluginError.None;

            if (serverRestartRequired) {
                RestartServer();
            }

            if (settingsModified && errors == PluginError.None) {
                WriteSettings();
            }
        }

        private void WriteSettings()
        {
            try {
                using (XmlWriter settingsFile = XmlWriter.Create(SettingsFilePath, new XmlWriterSettings { Indent = true })) {
                    XmlSerializer serialiser = new XmlSerializer(typeof(Settings));
                    serialiser.Serialize(settingsFile, settings);
                }
            } catch { }
        }

        private Settings LoadSettings()
        {
            Settings result = null;

            try {
                using (XmlReader settingsFile = XmlReader.Create(SettingsFilePath)) {
                    XmlSerializer serialiser = new XmlSerializer(typeof(Settings));
                    result = (Settings)serialiser.Deserialize(settingsFile);
                }
            } catch { }

            if (result == null) {
                result = new Settings { };
            }

            return result;
        }

        internal void RefreshConfigForm()
        {
            if (configForm != null) {
                configForm.SetFormSettings(settings);
            }
        }

        private void OnDatabaseRequest(object o, DAAP.DatabaseRequestedArgs info)
        {
            bool entered = false;

            try {
                Monitor.TryEnter(db, ref entered);

                if (entered && info.userAgent != null && (settings.optimisationPinned == false || settings.optimisedMetadata == null)) {
                    settings.optimisedUserAgent = info.userAgent;
                    settings.optimisedMetadata = info.daapMetadata;

                    WriteSettings();
                    db.CacheContentNodes(info.daapMetadata, info.response);
                    RefreshConfigForm();
                }
            } finally {
                if (entered) {
                    Monitor.Exit(db);
                }
            }

        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            if (reason != PluginCloseReason.MusicBeeClosing) {
                StopPlugin();
            }
        }

        private void StopPlugin()
        {
            try {
                configForm?.Close();
                configForm = null;
                server?.Stop();
                server = null;
                revisionManager?.Stop();
                revisionManager = null;
            } finally {
                ThreadExecutionState.AllowSleep();
            }
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
            try {
                File.Delete(SettingsFilePath);
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
                server.DatabaseRequested += OnDatabaseRequest;

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
                StopPlugin();

                mbApi.MB_SendNotification(CallbackType.DisablePlugin);

            }
            
            configForm?.SetMessages(errors);
        }

        private void RestartServer()
        {
            ThreadExecutionState.AllowSleep();

            try {
                server?.Stop();
                revisionManager?.Reset();
            } finally {
                InitialiseServer();
            }
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

        private static bool UpdateField<T>(ref T oldField, T newField)
        {
            bool updated = false;

            if (oldField.Equals(newField) == false) {
                oldField = newField;
                updated = true;
            }

            return updated;
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


        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return null;
        }
        
        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
        }
    }
}