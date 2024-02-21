using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Media.Protection.PlayReady;

namespace gamevault.UserControls.SettingsComponents
{
    /// <summary>
    /// Interaction logic for PhalcodeLoginRegisterUserControl.xaml
    /// </summary>
    public partial class PhalcodeLoginRegisterUserControl : UserControl
    {
        public PhalcodeLoginRegisterUserControl()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            bool success = await LoginManager.Instance.PhalcodeLogin(uiTxtUsername.Text, uiTxtPassword.Password);
            if (success)
                MainWindowViewModel.Instance.ClosePopup();

            ((Button)sender).IsEnabled = true;
        }
        private void Help_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string url = ((FrameworkElement)sender).Tag as string;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                string url = e.Uri.OriginalString;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }
    }
}
