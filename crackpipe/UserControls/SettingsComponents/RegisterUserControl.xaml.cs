using crackpipe.Helper;
using crackpipe.Models;
using crackpipe.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace crackpipe.UserControls.SettingsComponents
{
    /// <summary>
    /// Interaction logic for RegisterUserControl.xaml
    /// </summary>
    public partial class RegisterUserControl : UserControl
    {
        public RegisterUserControl()
        {
            InitializeComponent();
        }
        private async void Registration_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await Task.Run(async () =>
            {
                try
                {
                    string message = string.Empty;
                    if (!HasEmptyFields())
                    {
                        if (SettingsViewModel.Instance.RegistrationUser.Password != SettingsViewModel.Instance.RegistrationUser.RepeatPassword)
                        {
                            MainWindowViewModel.Instance.AppBarText = "Password must be equal";
                            return;
                        }
                        string jsonObject = JsonSerializer.Serialize(SettingsViewModel.Instance.RegistrationUser);
                        WebHelper.Post($"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/register", jsonObject);
                        message = "Successfully registered";
                        SettingsViewModel.Instance.RegistrationUser = new User();
                    }
                    else
                    {
                        message = "Each field must be filled";
                    }
                    MainWindowViewModel.Instance.AppBarText = message;
                }
                catch (WebException ex)
                {
                    string errMessage = WebExceptionHelper.GetServerMessage(ex);
                    if (errMessage == string.Empty) { errMessage = "Could not connect to server"; }
                    MainWindowViewModel.Instance.AppBarText = errMessage;
                }
                catch
                {
                    MainWindowViewModel.Instance.AppBarText = "Could not connect to server";
                }
            });
            ((Button)sender).IsEnabled = true;
        }
        private bool HasEmptyFields()
        {
            SettingsViewModel.Instance.RegistrationUser.Password = uiPwReg.Password.ToString();
            SettingsViewModel.Instance.RegistrationUser.RepeatPassword = uiPwRegRepeat.Password.ToString();

            if (string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.Username) || string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.FirstName)
                || string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.LastName) || string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.EMail)
                || string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.Password) || string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.RepeatPassword))
            {
                return true;
            }
            return false;
        }
    }
}
