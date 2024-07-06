using gamevault.Helper;
using gamevault.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
        private string m_ServerUrl { get; set; }
        private float m_ImageCacheSize { get; set; }
        private float m_OfflineCacheSize { get; set; }
        private long m_DownloadLimit { get; set; }
        private long m_DownloadLimitUIValue { get; set; }
        private User m_RegistrationUser = new User() { ProfilePicture = new Image(), BackgroundImage = new Image() };
        private PhalcodeProduct license { get; set; }
        private List<ThemeItem> themes { get; set; }
        #endregion

        public SettingsViewModel()
        {
            UserName = Preferences.Get(AppConfigKey.Username, AppFilePath.UserFile);
            RootPath = Preferences.Get(AppConfigKey.RootPath, AppFilePath.UserFile);
            ServerUrl = Preferences.Get(AppConfigKey.ServerUrl, AppFilePath.UserFile, true);
           
            m_BackgroundStart = (Preferences.Get(AppConfigKey.BackgroundStart, AppFilePath.UserFile) == "1"); OnPropertyChanged(nameof(BackgroundStart));
            m_AutoExtract = (Preferences.Get(AppConfigKey.AutoExtract, AppFilePath.UserFile) == "1"); OnPropertyChanged(nameof(AutoExtract));
            autoDeletePortableGameFiles = Preferences.Get(AppConfigKey.AutoDeletePortable, AppFilePath.UserFile) == "1"; OnPropertyChanged(nameof(AutoDeletePortableGameFiles));

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
        public User RegistrationUser
        {
            get { return m_RegistrationUser; }
            set { m_RegistrationUser = value; OnPropertyChanged(); }
        }
        public List<ThemeItem> Themes
        {
            get { return themes; }
            set { themes = value; OnPropertyChanged(); }
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
        public System.Windows.Forms.DialogResult SelectDownloadPath()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
                {
                    DirectoryInfo path = new DirectoryInfo(dialog.SelectedPath);

                    if (path.Parent.Name.Equals("GameVault", StringComparison.OrdinalIgnoreCase))
                        path = new DirectoryInfo(path.Parent.FullName);

                    if (path.Name.Equals("GameVault", StringComparison.OrdinalIgnoreCase))
                        dialog.SelectedPath = path.Parent.FullName;

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

    }
    public class ThemeItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsPlus { get; set; }
    }
}
