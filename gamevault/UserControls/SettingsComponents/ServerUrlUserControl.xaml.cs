using ABI.System;
using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;


namespace gamevault.UserControls.SettingsComponents
{
    /// <summary>
    /// Interaction logic for ServerUrlUserControl.xaml
    /// </summary>
    public partial class ServerUrlUserControl : UserControl
    {
        public ServerUrlUserControl()
        {
            InitializeComponent();
        }
        private void SaveServerUrl_Click(object sender, RoutedEventArgs e)
        {
            SaveServerURL();
        }

        private void Save_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SaveServerURL();
            }
        }
        private void SaveServerURL()
        {
            if (SettingsViewModel.Instance.ServerUrl.EndsWith("/"))
            {
                SettingsViewModel.Instance.ServerUrl = SettingsViewModel.Instance.ServerUrl.Substring(0, SettingsViewModel.Instance.ServerUrl.Length - 1);
            }
            if (!SettingsViewModel.Instance.ServerUrl.Contains(System.Uri.UriSchemeHttp))
            {
                SettingsViewModel.Instance.ServerUrl = $"{System.Uri.UriSchemeHttps}://{SettingsViewModel.Instance.ServerUrl}";
            }
            Preferences.Set(AppConfigKey.ServerUrl, SettingsViewModel.Instance.ServerUrl,LoginManager.Instance.GetUserProfile().UserConfigFile, true);
            MainWindowViewModel.Instance.AppBarText = "Server URL saved";
        }
    }
}
