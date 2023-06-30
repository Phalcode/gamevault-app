using ABI.System;
using crackpipe.Models;
using crackpipe.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;


namespace crackpipe.UserControls.SettingsComponents
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
            if (SettingsViewModel.Instance.ServerUrl.EndsWith("/"))
            {
                SettingsViewModel.Instance.ServerUrl = SettingsViewModel.Instance.ServerUrl.Substring(0, SettingsViewModel.Instance.ServerUrl.Length - 1);
            }
            if (!SettingsViewModel.Instance.ServerUrl.Contains(System.Uri.UriSchemeHttp))
            {
                SettingsViewModel.Instance.ServerUrl = $"{System.Uri.UriSchemeHttps}://{SettingsViewModel.Instance.ServerUrl}";
            }
            Preferences.Set(AppConfigKey.ServerUrl, SettingsViewModel.Instance.ServerUrl, AppFilePath.UserFile, true);
            MainWindowViewModel.Instance.AppBarText = "Server URL saved";
        }
    }
}
