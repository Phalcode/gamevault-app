using gamevault.Helper;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace gamevault.UserControls.SettingsComponents
{
    /// <summary>
    /// Interaction logic for LoginUserControl.xaml
    /// </summary>
    public partial class LoginUserControl : UserControl
    {
        public LoginUserControl()
        {
            InitializeComponent();
        }
        private async void Login_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            if (SettingsViewModel.Instance.UserName != string.Empty && uiPwBox.Password != string.Empty)
            {
                if (LoginManager.Instance.IsLoggedIn() && LoginManager.Instance.GetCurrentUser().Username == SettingsViewModel.Instance.UserName)
                {
                    MainWindowViewModel.Instance.AppBarText = $"You are already logged in as '{SettingsViewModel.Instance.UserName}'";
                }
                else
                {
                    LoginState state = await LoginManager.Instance.ManualLogin(SettingsViewModel.Instance.UserName, uiPwBox.Password);
                    if (LoginState.Success == state)
                    {
                        MainWindowViewModel.Instance.AppBarText = $"Successfully logged in as '{SettingsViewModel.Instance.UserName}'";
                        await MainWindowViewModel.Instance.Community.InitUserList();

                    }
                    else if (LoginState.Unauthorized == state)
                    {
                        MainWindowViewModel.Instance.AppBarText = "Login failed. Username or password was not correct";
                    }
                    else if (LoginState.Forbidden == state)
                    {
                        MainWindowViewModel.Instance.AppBarText = "Login failed. User is not activated. Contact an Administrator to activate the User.";
                    }
                    else if (LoginState.Error == state)
                    {
                        MainWindowViewModel.Instance.AppBarText = "Could not connect to server";
                    }
                    MainWindowViewModel.Instance.UserIcon = LoginManager.Instance.GetCurrentUser();
                }
            }
            else
            {
                MainWindowViewModel.Instance.AppBarText = "Username or password are not set";
            }
            ((Button)sender).IsEnabled = true;
        }
    }
}
