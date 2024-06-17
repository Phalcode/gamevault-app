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
        private Wizard? _wizard;

        public RegisterUserControl()
        {
            InitializeComponent();
        }

        public RegisterUserControl(Wizard parent)
        {
            InitializeComponent();
            _wizard = parent;
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
            var t = await Task.Run<bool>(async () =>
            {
                string message = string.Empty;
                bool success = false;
                try
                {
                    if (!HasEmptyFields())
                    {
                        if (SettingsViewModel.Instance.RegistrationUser.Password != SettingsViewModel.Instance.RegistrationUser.RepeatPassword)
                        {
                            message = "Passwords must be the same";
                            success = false;
                        }
                        else
                        {
                            string jsonObject = JsonSerializer.Serialize(SettingsViewModel.Instance.RegistrationUser);
                            WebHelper.Post($"{SettingsViewModel.Instance.ServerUrl}/api/users/register", jsonObject);
                            message = "Successfully registered";
                            SettingsViewModel.Instance.RegistrationUser = new User();
                            App.Current.Dispatcher.Invoke((Action)delegate
                            {
                                uiPwReg.Password = string.Empty;
                                uiPwRegRepeat.Password = string.Empty;
                            });
                            success = true;
                        }
                    }
                    else
                    {
                        message = "All mandatory fields must be filled";
                    }
                }
                catch (Exception ex)
                {
                    message = WebExceptionHelper.TryGetServerMessage(ex);
                }

                MainWindowViewModel.Instance.AppBarText = message;
                return success;
            });
            uiBtnRegister.IsEnabled = true;

            //need to get return result from task
            if (_wizard != null && t == true)
            {
                _wizard.uiLoginRegisterPopup.IsOpen = false;
                _wizard.Login_Clicked(this, new RoutedEventArgs());
            }

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            uiLoginBox.Focus();
        }
    }
}
