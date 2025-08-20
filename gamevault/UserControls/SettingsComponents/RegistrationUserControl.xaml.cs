using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using gamevault.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
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
    internal class RegistrationUserControlViewModel : ViewModelBase
    {
        private LoginUser signupUser { get; set; }
        public LoginUser SignupUser
        {
            get
            {
                if (signupUser == null)
                {
                    signupUser = new LoginUser();
                }
                return signupUser;
            }
            set { signupUser = value; OnPropertyChanged(); }
        }
        private BindableServerInfo signUpServerInfo { get; set; } = new BindableServerInfo();
        public BindableServerInfo SignUpServerInfo
        {
            get { return signUpServerInfo; }
            set { signUpServerInfo = value; OnPropertyChanged(); }
        }
    }
    public partial class RegistrationUserControl : UserControl
    {
        private InputTimer InputTimer { get; set; }
        private RegistrationUserControlViewModel ViewModel { get; set; }
        public RegistrationUserControl()
        {
            InitializeComponent();
            ViewModel = new RegistrationUserControlViewModel();
            this.DataContext = ViewModel;
            InputTimer = new InputTimer();
            InputTimer.Interval = TimeSpan.FromMilliseconds(400);
            InputTimer.Tick += ServerUrlInput_Tick;
            try
            {
                ViewModel.SignupUser.ServerUrl = LoginManager.Instance.GetUserProfile()!.ServerUrl;
            }
            catch { }
        }
        private async void UserRegistrationTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                //await SaveAndSignUp();
            }
        }
        private void ServerUrlInput_TextChanged(object sender, RoutedEventArgs e)
        {
            InputTimer.Stop();
            InputTimer.Data = ((TextBox)sender).Text;
            InputTimer.Start();
        }
        private async void ServerUrlInput_Tick(object sender, EventArgs e)
        {
            try
            {
                InputTimer.Stop();
                string result = await WebHelper.BaseGetAsync($"{ValidateUriScheme(InputTimer?.Data)}/api/status");
                ServerInfo serverInfo = JsonSerializer.Deserialize<ServerInfo>(result);

                ViewModel.SignUpServerInfo = new BindableServerInfo(serverInfo);
            }
            catch (Exception ex)
            {
                string message = WebExceptionHelper.TryGetServerMessage(ex);
                ViewModel.SignUpServerInfo = new BindableServerInfo(message == "" ? "Could not connect to server" : message);
            }
        }
        private string ValidateUriScheme(string uri)
        {
            if (uri.EndsWith("/"))
            {
                uri = uri.Substring(0, uri.Length - 1);
            }
            if (!uri.Contains(System.Uri.UriSchemeHttp))
            {
                uri = $"{System.Uri.UriSchemeHttps}://{uri}";
            }
            return uri;
        }

        private async void RegisterNewUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ValidateSignUpData();
                LoginState state = await LoginManager.Instance.Register(ViewModel.SignupUser, true);
                if (state != LoginState.Error)
                {
                    MainWindowViewModel.Instance.ClosePopup();
                    if (MainWindowViewModel.Instance.ActiveControl.GetType() == typeof(AdminConsoleUserControl))
                    {
                        await MainWindowViewModel.Instance.AdminConsole.InitUserList();
                    }
                    MainWindowViewModel.Instance.AppBarText = "Successfully registrated User";
                    return;
                }
                MainWindowViewModel.Instance.AppBarText = LoginManager.Instance.GetServerLoginResponseMessage();
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        private void ValidateSignUpData()
        {
            if (string.IsNullOrWhiteSpace(ViewModel.SignupUser.ServerUrl))
            {
                throw new Exception("Server URL is not set");
            }
            ViewModel.SignupUser.ServerUrl = ValidateUriScheme(ViewModel.SignupUser.ServerUrl);
            if (string.IsNullOrWhiteSpace(ViewModel.SignupUser.Password) || string.IsNullOrWhiteSpace(ViewModel.SignupUser.RepeatPassword))
            {
                throw new Exception("Password is not set");
            }
            if (ViewModel.SignupUser.Password != ViewModel.SignupUser.RepeatPassword)
            {
                throw new Exception("Password must be equal");
            }
            if (string.IsNullOrWhiteSpace(ViewModel.SignupUser.Username))
            {
                throw new Exception("Username is not set");
            }

        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }


    }
}
