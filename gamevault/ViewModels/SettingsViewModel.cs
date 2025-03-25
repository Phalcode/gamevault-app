﻿using gamevault.Helper;
using gamevault.Helper.Integrations;
using gamevault.Models;
using gamevault.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class SettingsViewModel : ViewModelBase
    {
        #region Singleton
        private static SettingsViewModel instance = null;
        private static readonly object padlock = new object();

        public static SettingsViewModel Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new SettingsViewModel();
                    }
                    return instance;
                }
            }
        }
        #endregion
        #region PrivateMembers       
        private string m_UserName { get; set; }
        private string m_RootPath { get; set; }

        private bool m_BackgroundStart { get; set; }
        private bool m_LibStartup { get; set; }
        private bool m_AutoExtract { get; set; }
        private bool autoInstallPortable { get; set; }
        private bool autoDeletePortableGameFiles { get; set; }
        private bool retainLibarySortByAndOrderBy { get; set; }
        private bool sendAnonymousAnalytics { get; set; }
        private string m_ServerUrl { get; set; }
        private float m_ImageCacheSize { get; set; }
        private float m_OfflineCacheSize { get; set; }
        private long m_DownloadLimit { get; set; }
        private long m_DownloadLimitUIValue { get; set; }
        private string[] ignoreList { get; set; }
        private User m_RegistrationUser = new User() { Avatar = new Media(), Background = new Media() };
        private PhalcodeProduct license { get; set; }
        private ObservableCollection<ThemeItem> themes { get; set; }
        private ObservableCollection<ThemeItem> communityThemes { get; set; }
        private bool showMappedTitle { get; set; }
        private bool syncSteamShortcuts { get; set; }
        private bool syncDiscordPresence { get; set; }
        private bool cloudSaves { get; set; }
        private bool isCommunityThemeSelected { get; set; }
        private bool usePrimaryCloudSaveManifest { get; set; }
        private ObservableCollection<LudusaviManifestEntry> customCloudSaveManifests;
        private bool mountIso { get; set; }
        //DevMode
        private bool devModeEnabled { get; set; }
        private bool devTargetPhalcodeTestBackend { get; set; }
        //

        #endregion

        public SettingsViewModel()
        {
            UserName = Preferences.Get(AppConfigKey.Username, AppFilePath.UserFile);
            RootPath = Preferences.Get(AppConfigKey.RootPath, AppFilePath.UserFile);
            ServerUrl = Preferences.Get(AppConfigKey.ServerUrl, AppFilePath.UserFile, true);

            string showMappedTitleString = Preferences.Get(AppConfigKey.ShowMappedTitle, AppFilePath.UserFile);
            showMappedTitle = showMappedTitleString == "1" || showMappedTitleString == "";
            //Setting the private members to avoid writing to the user config file over and over again
            m_BackgroundStart = (Preferences.Get(AppConfigKey.BackgroundStart, AppFilePath.UserFile) == "1"); OnPropertyChanged(nameof(BackgroundStart));
            m_AutoExtract = (Preferences.Get(AppConfigKey.AutoExtract, AppFilePath.UserFile) == "1"); OnPropertyChanged(nameof(AutoExtract));
            autoDeletePortableGameFiles = Preferences.Get(AppConfigKey.AutoDeletePortable, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(AutoDeletePortableGameFiles));
            retainLibarySortByAndOrderBy = Preferences.Get(AppConfigKey.RetainLibarySortByAndOrderBy, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(RetainLibarySortByAndOrderBy));

            string analyticsPreference = Preferences.Get(AppConfigKey.SendAnonymousAnalytics, AppFilePath.UserFile);
            sendAnonymousAnalytics = (analyticsPreference == "" || analyticsPreference == "1"); OnPropertyChanged(nameof(SendAnonymousAnalytics));

            syncSteamShortcuts = Preferences.Get(AppConfigKey.SyncSteamShortcuts, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(SyncSteamShortcuts));
            syncDiscordPresence = Preferences.Get(AppConfigKey.SyncDiscordPresence, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(SyncDiscordPresence));
            cloudSaves = Preferences.Get(AppConfigKey.CloudSaves, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(CloudSaves));

            string autoInstallPortableStr = Preferences.Get(AppConfigKey.AutoInstallPortable, AppFilePath.UserFile);
            if (string.IsNullOrWhiteSpace(autoInstallPortableStr) || autoInstallPortableStr == "1")
            {
                autoInstallPortable = true; OnPropertyChanged(nameof(AutoInstallPortable));
            }

            string libstartupStr = Preferences.Get(AppConfigKey.LibStartup, AppFilePath.UserFile);
            if (libstartupStr == string.Empty)
            {
                LibStartup = true;
            }
            else
            {
                m_LibStartup = (libstartupStr == "1"); OnPropertyChanged(nameof(LibStartup));
            }
            if (long.TryParse(Preferences.Get(AppConfigKey.DownloadLimit, AppFilePath.UserFile), out long downloadLimitResult))
            {
                DownloadLimit = downloadLimitResult;
                DownloadLimitUIValue = DownloadLimit;
            }
            else
            {
                DownloadLimit = 0;
                DownloadLimitUIValue = 0;
            }
            string usePrimaryCloudSaveManifestString = Preferences.Get(AppConfigKey.UsePrimaryCloudSaveManifest, AppFilePath.UserFile);
            usePrimaryCloudSaveManifest = usePrimaryCloudSaveManifestString == "1" || usePrimaryCloudSaveManifestString == "";

            string customCloudSaveManifestsString = Preferences.Get(AppConfigKey.CustomCloudSaveManifests, AppFilePath.UserFile);
            customCloudSaveManifests = string.IsNullOrWhiteSpace(customCloudSaveManifestsString) ? null! : new ObservableCollection<LudusaviManifestEntry>(customCloudSaveManifestsString.Split(';').Select(part => new LudusaviManifestEntry { Uri = part }).ToList());

            string mountIsoString = Preferences.Get(AppConfigKey.MountIso, AppFilePath.UserFile);
            mountIso = mountIsoString == "1";

            //DevMode
            devModeEnabled = Preferences.Get(AppConfigKey.DevModeEnabled, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(DevModeEnabled));
            devTargetPhalcodeTestBackend = Preferences.Get(AppConfigKey.DevTargetPhalcodeTestBackend, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(DevTargetPhalcodeTestBackend));
            //            
        }
        public async Task InitIgnoreList()
        {
            try
            {
                if (!File.Exists(AppFilePath.IgnoreList))
                {
                    string response = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/ignorefile");
                    string[] ignoreList = JsonSerializer.Deserialize<string[]>(response);
                    if (ignoreList != null || ignoreList?.Length > 0)
                    {
                        IgnoreList = ignoreList.Where(s => !string.IsNullOrEmpty(s)).ToArray(); //Make sure server ignore list don't contain empty strings, because this will exclude any file which is compared to the ignore list
                        Preferences.Set("IL", response.Replace("\n", ""), AppFilePath.IgnoreList);
                    }
                }
                else
                {
                    string result = Preferences.Get("IL", AppFilePath.IgnoreList);
                    IgnoreList = JsonSerializer.Deserialize<string[]>(result);
                }
            }
            catch
            {
                try
                {
                    string result = Preferences.Get("IL", AppFilePath.IgnoreList);
                    IgnoreList = JsonSerializer.Deserialize<string[]>(result);
                }
                catch { }
            }
        }

        public string UserName
        {
            get { return m_UserName; }
            set { m_UserName = value; OnPropertyChanged(); }
        }
        public string RootPath
        {
            get { return m_RootPath; }
            set { m_RootPath = value; OnPropertyChanged(); }
        }
        public bool BackgroundStart
        {
            get { return m_BackgroundStart; }
            set
            {
                m_BackgroundStart = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!m_BackgroundStart)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.BackgroundStart, stringValue, AppFilePath.UserFile);
            }
        }
        public bool LibStartup
        {
            get { return m_LibStartup; }
            set
            {
                m_LibStartup = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!m_LibStartup)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.LibStartup, stringValue, AppFilePath.UserFile);
            }
        }
        public bool AutoExtract
        {
            get { return m_AutoExtract; }
            set
            {
                m_AutoExtract = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!m_AutoExtract)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.AutoExtract, stringValue, AppFilePath.UserFile);
            }
        }
        public bool AutoInstallPortable
        {
            get { return autoInstallPortable; }
            set
            {
                autoInstallPortable = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!autoInstallPortable)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.AutoInstallPortable, stringValue, AppFilePath.UserFile);
            }
        }
        public bool AutoDeletePortableGameFiles
        {
            get { return autoDeletePortableGameFiles; }
            set
            {
                autoDeletePortableGameFiles = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!autoDeletePortableGameFiles)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.AutoDeletePortable, stringValue, AppFilePath.UserFile);
            }
        }
        public bool RetainLibarySortByAndOrderBy
        {
            get { return retainLibarySortByAndOrderBy; }
            set
            {
                retainLibarySortByAndOrderBy = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!retainLibarySortByAndOrderBy)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.RetainLibarySortByAndOrderBy, stringValue, AppFilePath.UserFile);
            }
        }
        public bool SendAnonymousAnalytics
        {
            get { return sendAnonymousAnalytics; }
            set
            {
                sendAnonymousAnalytics = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!sendAnonymousAnalytics)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.SendAnonymousAnalytics, stringValue, AppFilePath.UserFile);
            }
        }

        public string ServerUrl
        {
            get { return m_ServerUrl; }
            set { m_ServerUrl = value; OnPropertyChanged(); }
        }
        public float ImageCacheSize
        {
            get { return m_ImageCacheSize; }
            set { m_ImageCacheSize = value; OnPropertyChanged(); }
        }
        public float OfflineCacheSize
        {
            get { return m_OfflineCacheSize; }
            set { m_OfflineCacheSize = value; OnPropertyChanged(); }
        }
        public long DownloadLimit
        {
            get { return m_DownloadLimit; }
            set { m_DownloadLimit = value; OnPropertyChanged(); }
        }
        public long DownloadLimitUIValue
        {
            get { return m_DownloadLimitUIValue; }
            set { m_DownloadLimitUIValue = value; OnPropertyChanged(); }
        }
        public string[] IgnoreList
        {
            get
            {
                if (ignoreList == null)
                {
                    ignoreList = new string[0];
                }
                return ignoreList;
            }
            set { ignoreList = value; OnPropertyChanged(); }
        }
        public bool ShowMappedTitle
        {
            get
            {
                return showMappedTitle;
            }
            set { showMappedTitle = value; Preferences.Set(AppConfigKey.ShowMappedTitle, showMappedTitle ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        public bool SyncSteamShortcuts
        {
            get
            {
                return syncSteamShortcuts;
            }
            set { syncSteamShortcuts = value; Preferences.Set(AppConfigKey.SyncSteamShortcuts, syncSteamShortcuts ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        public bool SyncDiscordPresence
        {
            get
            {
                return syncDiscordPresence;
            }
            set { syncDiscordPresence = value; Preferences.Set(AppConfigKey.SyncDiscordPresence, syncDiscordPresence ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        public bool CloudSaves
        {
            get
            {
                return cloudSaves;
            }
            set { cloudSaves = value; Preferences.Set(AppConfigKey.CloudSaves, cloudSaves ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        public bool IsCommunityThemeSelected
        {
            get
            {
                return isCommunityThemeSelected;
            }
            set { isCommunityThemeSelected = value; OnPropertyChanged(); }
        }
        public User RegistrationUser
        {
            get { return m_RegistrationUser; }
            set { m_RegistrationUser = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ThemeItem> Themes
        {
            get { return themes; }
            set { themes = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ThemeItem> CommunityThemes
        {
            get { return communityThemes; }
            set { communityThemes = value; OnPropertyChanged(); }
        }
        public PhalcodeProduct License
        {
            get
            {
                if (license == null) { license = new PhalcodeProduct(); }
                return license;
            }
            set { license = value; OnPropertyChanged(); }
        }

        public bool UsePrimaryCloudSaveManifest
        {
            get
            {
                return usePrimaryCloudSaveManifest;
            }
            set { usePrimaryCloudSaveManifest = value; Preferences.Set(AppConfigKey.UsePrimaryCloudSaveManifest, usePrimaryCloudSaveManifest ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        public ObservableCollection<LudusaviManifestEntry> CustomCloudSaveManifests
        {
            get
            {
                if (customCloudSaveManifests == null)
                {
                    customCloudSaveManifests = new ObservableCollection<LudusaviManifestEntry>();
                }
                return customCloudSaveManifests;
            }
            set { customCloudSaveManifests = value; OnPropertyChanged(); }
        }
        public bool MountIso
        {
            get
            {
                return mountIso;
            }
            set { mountIso = value; Preferences.Set(AppConfigKey.MountIso, mountIso ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        public System.Windows.Forms.DialogResult SelectDownloadPath()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
                {
                    try
                    {
                        File.Create(@$"{dialog.SelectedPath}\accesscheck.file").Close();
                        File.Delete(@$"{dialog.SelectedPath}\accesscheck.file");
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = $"Access to the path {dialog.SelectedPath} is denied";
                        return System.Windows.Forms.DialogResult.Cancel;
                    }
                    Preferences.Set(AppConfigKey.RootPath, dialog.SelectedPath, AppFilePath.UserFile);
                    RootPath = dialog.SelectedPath.Replace(@"\\", @"\");
                    return System.Windows.Forms.DialogResult.OK;
                }
                return System.Windows.Forms.DialogResult.Cancel;
            }
        }
        public bool SetupCompleted()
        {
            return !((m_RootPath == string.Empty) || (m_ServerUrl == string.Empty) || (m_UserName == string.Empty));
        }
        public string Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
        //DevMode
        public bool DevModeEnabled
        {
            get
            {
                return devModeEnabled;
            }
            set { devModeEnabled = value; Preferences.Set(AppConfigKey.DevModeEnabled, devModeEnabled ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        public bool DevTargetPhalcodeTestBackend
        {
            get
            {
                return devTargetPhalcodeTestBackend;
            }
            set { devTargetPhalcodeTestBackend = value; Preferences.Set(AppConfigKey.DevTargetPhalcodeTestBackend, devTargetPhalcodeTestBackend ? "1" : "0", AppFilePath.UserFile); OnPropertyChanged(); }
        }
        //
    }
    public class ThemeItem
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Path { get; set; }
        public bool IsPlus { get; set; }
    }
}
