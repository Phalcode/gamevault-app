using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
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

namespace gamevault.UserControls.SettingsComponents
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
            await Register();
        }
        private async void Register_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await Register();
            }
        }
        private bool HasEmptyFields()
        {
            SettingsViewModel.Instance.RegistrationUser.Password = uiPwReg.Password.ToString();
            SettingsViewModel.Instance.RegistrationUser.RepeatPassword = uiPwRegRepeat.Password.ToString();

            if (string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.Username) || string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.Password) || string.IsNullOrEmpty(SettingsViewModel.Instance.RegistrationUser.RepeatPassword))
            {
                return true;
            }
            return false;
        }
        private async Task Register()
        {
            uiBtnRegister.IsEnabled = false;
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
                        WebHelper.Post($"{SettingsViewModel.Instance.ServerUrl}/api/users/register", jsonObject);
                        message = "Successfully registered";
                        SettingsViewModel.Instance.RegistrationUser = new User();
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            uiPwReg.Password = string.Empty;
                            uiPwRegRepeat.Password = string.Empty;
                        });
                    }
                    else
                    {
                        message = "All mandatory fields must be filled";
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
            uiBtnRegister.IsEnabled = true;
        }
    }
}
