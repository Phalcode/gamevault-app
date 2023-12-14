using gamevault.UserControls.SettingsComponents;
using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;
using gamevault.Models;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for Wizard.xaml
    /// </summary>
    public partial class Wizard : UserControl
    {
        public Wizard()
        {
            InitializeComponent();
            this.DataContext = SettingsViewModel.Instance;
            Preferences.Set(AppConfigKey.InstallDrive, "C", AppFilePath.UserFile);
        }

        private void Next_Clicked(object sender, RoutedEventArgs e)
        {
            uiTabControl.SelectedIndex += 1;
        }

        private void Back_Clicked(object sender, RoutedEventArgs e)
        {
            uiTabControl.SelectedIndex -= 1;
        }

        private void Login_Clicked(object sender, RoutedEventArgs e)
        {
            uiLoginRegisterPopup.Child = new LoginUserControl();
            uiLoginRegisterPopup.IsOpen = true;
        }

        private void Register_Clicked(object sender, RoutedEventArgs e)
        {
            uiLoginRegisterPopup.Child = new RegisterUserControl();
            uiLoginRegisterPopup.IsOpen = true;
        }

        private void Help_Clicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string? url = (string)((FrameworkElement)sender).Tag;
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch { }
        }

        private async void Finish_Clicked(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync("", $"Do you want to leave the setup wizard?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false, DialogMessageFontSize = 50 });
            if (result == MessageDialogResult.Affirmative)
            {
                MainWindowViewModel.Instance.SetActiveControl(null);
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
        }
    }
}
