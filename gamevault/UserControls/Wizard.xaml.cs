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
using Microsoft.IdentityModel.Tokens;
using gamevault.Helper;

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
        }

        private async void Next_Clicked(object sender, RoutedEventArgs e)
        {
            if (uiTabControl.SelectedIndex == 2 && !(await LoginManager.Instance.IsServerAvailable()))
            {
                MessageDialogResult result = await((MetroWindow)App.Current.MainWindow).ShowMessageAsync("", $"The server URL is incorrect. Do you still want to continue?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false, DialogMessageFontSize = 30 });
                if (result != MessageDialogResult.Affirmative)
                {
                    return;
                }
            }

            uiTabControl.SelectedIndex += 1;
        }

        private void Back_Clicked(object sender, RoutedEventArgs e)
        {
            uiTabControl.SelectedIndex -= 1;
        }

        public void Login_Clicked(object sender, RoutedEventArgs e)
        {
            uiLoginRegisterPopup.Child = new LoginUserControl(this);
            uiLoginRegisterPopup.IsOpen = true;
        }

        private void Register_Clicked(object sender, RoutedEventArgs e)
        {
            uiLoginRegisterPopup.Child = new RegisterUserControl(this);
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
                MainWindowViewModel.Instance.SetActiveControl(null);
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
        }
    }
}
