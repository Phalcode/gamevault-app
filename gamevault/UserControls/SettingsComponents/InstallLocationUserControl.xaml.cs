using gamevault.Helper;
using gamevault.Helper.Integrations;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;


namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for InstallLocationUserControl.xaml
    /// </summary>
    public partial class InstallLocationUserControl : UserControl
    {
        private TaskCompletionSource<string> ResultTaskSource;
        public Dictionary<DirectoryEntry, string> RootDirectories { get; set; }
        private bool loaded = false;
        public InstallLocationUserControl()
        {
            InitializeComponent();
            ResultTaskSource = new TaskCompletionSource<string>();
            RootDirectories = new Dictionary<DirectoryEntry, string>();
            PrepareInstallLocationSelection();
            this.DataContext = this;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            loaded = true;
            string lastSelectedRootDirectory = Preferences.Get(AppConfigKey.LastSelectedRootDirectory, LoginManager.Instance.GetUserProfile().UserConfigFile);
            if (AutoConfirmIfWindowIsHiddenOrOnlyOneEntry(lastSelectedRootDirectory))
                return;

            if (Directory.Exists(lastSelectedRootDirectory))
            {
                int index = RootDirectories.ToList().FindIndex(x => x.Key.Uri == lastSelectedRootDirectory);
                if (index != -1)
                {
                    ToggleButtonGroupBehavior.CheckToggleButtonByIndex("RootDirectorySelection", this, index);
                    return;
                }
            }
            ToggleButtonGroupBehavior.CheckToggleButtonByIndex("RootDirectorySelection", this, 0);//Default pre selects the first entry             
        }
        private void PrepareInstallLocationSelection()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                foreach (DirectoryEntry rootDir in SettingsViewModel.Instance.RootDirectories)
                {
                    if (rootDir.Uri.StartsWith(drive.RootDirectory.Name))
                    {
                        RootDirectories.Add(rootDir, FormatBytes(drive.TotalFreeSpace));
                    }
                }
            }
            if (RootDirectories.Count <= 1)
            {
                this.Visibility = Visibility.Collapsed;
            }
        }
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }
        public Task<string> SelectInstallLocation()
        {
            return ResultTaskSource.Task;
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
            ResultTaskSource.TrySetResult(string.Empty);
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleButton selectedButton = ToggleButtonGroupBehavior.GetCheckedToggleButton("RootDirectorySelection", this);
                if (selectedButton == null)
                    return;

                var dataContext = (KeyValuePair<DirectoryEntry, string>)selectedButton.DataContext;
                if (selectedButton != null && selectedButton.Content != null)
                {
                    Preferences.Set(AppConfigKey.LastSelectedRootDirectory, dataContext.Key.Uri, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    MainWindowViewModel.Instance.ClosePopup();
                    ResultTaskSource.TrySetResult(dataContext.Key.Uri);
                }
            }
            catch (Exception ex) { }
        }
        private bool AutoConfirmIfWindowIsHiddenOrOnlyOneEntry(string lastSelectedRootDirectory)
        {
            if (App.Instance.MainWindow.IsVisible == false && RootDirectories.Any())//possible if called by CLI
            {
                if (Directory.Exists(lastSelectedRootDirectory))
                {
                    MainWindowViewModel.Instance.ClosePopup();
                    ResultTaskSource.TrySetResult(lastSelectedRootDirectory);
                }
                else
                {
                    MainWindowViewModel.Instance.ClosePopup();
                    ResultTaskSource.TrySetResult(RootDirectories.ElementAt(0).Key.Uri);
                }
                return true;
            }
            else if (RootDirectories.Count == 1)
            {
                MainWindowViewModel.Instance.ClosePopup();
                ResultTaskSource.TrySetResult(RootDirectories.ElementAt(0).Key.Uri);
                return true;
            }
            return false;
        }
        private void DirectorySettings_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
            ResultTaskSource.TrySetResult(string.Empty);
            MainWindowViewModel.Instance.SetActiveControl(MainControl.Settings);
            MainWindowViewModel.Instance.Settings.SetTabIndex(3);
        }
    }
}
