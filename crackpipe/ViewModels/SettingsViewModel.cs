using crackpipe.Helper;
using crackpipe.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace crackpipe.ViewModels
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
        private bool m_IsOnIdle = true;
        private bool m_BackgroundStart { get; set; }
        private bool m_LibStartup { get; set; }
        private bool m_AppAutostart { get; set; }
        private string m_ServerUrl { get; set; }
        private User m_RegistrationUser = new User() { ProfilePicture = new Image(), BackgroundImage = new Image() };
        #endregion

        public SettingsViewModel()
        {
            UserName = Preferences.Get(AppConfigKey.Username, AppFilePath.UserFile);
            RootPath = Preferences.Get(AppConfigKey.RootPath, AppFilePath.UserFile);
            ServerUrl = Preferences.Get(AppConfigKey.ServerUrl, AppFilePath.UserFile, true);
            m_BackgroundStart = (Preferences.Get(AppConfigKey.BackgroundStart, AppFilePath.UserFile) == "1"); OnPropertyChanged(nameof(BackgroundStart));

            string libstartup = Preferences.Get(AppConfigKey.LibStartup, AppFilePath.UserFile);
            if (libstartup == string.Empty)
            {
                LibStartup = true;
            }
            else
            {
                m_LibStartup = (libstartup == "1"); OnPropertyChanged(nameof(LibStartup));
            }

            m_AppAutostart = RegistryHelper.AutoStartKeyExists(); OnPropertyChanged(nameof(AppAutostart));
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
        public bool IsOnIdle
        {
            get { return m_IsOnIdle; }
            set { m_IsOnIdle = value; OnPropertyChanged(); }
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
        public bool AppAutostart
        {
            get { return m_AppAutostart; }
            set
            {
                m_AppAutostart = value;
                if (m_AppAutostart == true)
                {
                    RegistryHelper.CreateAutostartKey();
                }
                else
                {
                    RegistryHelper.DeleteAutostartKey();
                }
                OnPropertyChanged();
            }
        }
        public string ServerUrl
        {
            get { return m_ServerUrl; }
            set { m_ServerUrl = value; OnPropertyChanged(); }
        }
        public User RegistrationUser
        {
            get { return m_RegistrationUser; }
            set { m_RegistrationUser = value; OnPropertyChanged(); }
        }
        public System.Windows.Forms.DialogResult SelectDownloadPath()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
                {
                    Preferences.Set(AppConfigKey.RootPath, dialog.SelectedPath, AppFilePath.UserFile);
                    RootPath = dialog.SelectedPath;
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
                return "1.1.0";
            }
        }

    }
}
