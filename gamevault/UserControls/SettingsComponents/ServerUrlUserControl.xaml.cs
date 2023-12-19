using ABI.System;
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
            SaveServerURL();
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
            SettingsViewModel.Instance.ServerUrl = "https://neo.arparec.dev"; // USE HTTPS FOR CLOUDFLARE

            Preferences.Set(AppConfigKey.ServerUrl, SettingsViewModel.Instance.ServerUrl, AppFilePath.UserFile, true);
        }
    }
}
