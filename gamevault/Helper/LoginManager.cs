using gamevault.Models;
using gamevault.ViewModels;
using IdentityModel.OidcClient;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Threading;
using Windows.Media.Protection.PlayReady;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace gamevault.Helper
{
    public enum LoginState
    {
        Success,
        Error,
        Unauthorized,
        Forbidden
    }
    internal class LoginManager
    {
        #region Singleton
        private static LoginManager instance = null;
        private static readonly object padlock = new object();

        public static LoginManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new LoginManager();
                    }
                    return instance;
                }
            }
        }
        #endregion
        private User? m_User { get; set; }
        private LoginState m_LoginState { get; set; }
        private string m_LoginMessage { get; set; }
        public User? GetCurrentUser()
        {
            return m_User;
        }
        public bool IsLoggedIn()
        {
            return m_User != null;
        }
        public LoginState GetState()
        {
            return m_LoginState;
        }
        public string GetLoginMessage()
        {
            return m_LoginMessage;
        }
        public void SwitchToOfflineMode()
        {
            MainWindowViewModel.Instance.OnlineState = System.Windows.Visibility.Visible;
            m_User = null;
        }
        public async Task StartupLogin()
        {
            LoginState state = LoginState.Success;
            if (IsLoggedIn()) return;
            User? user = await Task<User>.Run(() =>
            {
                try
                {
                    WebHelper.SetCredentials(Preferences.Get(AppConfigKey.Username, AppFilePath.UserFile), Preferences.Get(AppConfigKey.Password, AppFilePath.UserFile, true));
                    string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me");
                    return JsonSerializer.Deserialize<User>(result);
                }
                catch (Exception ex)
                {
                    string code = WebExceptionHelper.GetServerStatusCode(ex);
                    state = DetermineLoginState(code);
                    if (state == LoginState.Error)
                        m_LoginMessage = WebExceptionHelper.TryGetServerMessage(ex);

                    return null;
                }
            });
            m_User = user;
            m_LoginState = state;
        }
        public async Task<LoginState> ManualLogin(string username, string password)
        {
            LoginState state = LoginState.Success;
            User? user = await Task<User>.Run(() =>
            {
                try
                {
                    WebHelper.OverrideCredentials(username, password);
                    string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me");
                    return JsonSerializer.Deserialize<User>(result);
                }
                catch (Exception ex)
                {
                    string code = WebExceptionHelper.GetServerStatusCode(ex);
                    state = DetermineLoginState(code);
                    if (state == LoginState.Error)
                        m_LoginMessage = WebExceptionHelper.TryGetServerMessage(ex);

                    return null;
                }
            });
            m_User = user;
            m_LoginState = state;
            return state;
        }
        public void Logout()
        {
            m_User = null;
            m_LoginState = LoginState.Error;
            WebHelper.OverrideCredentials(string.Empty, string.Empty);
            MainWindowViewModel.Instance.Community.Reset();
        }
        private WpfEmbeddedBrowser wpfEmbeddedBrowser = null;
        public async Task PhalcodeLogin(bool isStartup = false)
        {
            if (isStartup && Preferences.Get(AppConfigKey.Phalcode1, AppFilePath.UserFile, true) != "true")
            {
                return;
            }
            wpfEmbeddedBrowser = new WpfEmbeddedBrowser(isStartup);
            var options = new OidcClientOptions()
            {
                Authority = "https://auth.platform.phalco.de/realms/phalcode",
                ClientId = "gamevault-app",
                Scope = "openid profile email",
                RedirectUri = "http://127.0.0.1/gamevault",
                Browser = wpfEmbeddedBrowser,
                Policy = new Policy
                {
                    RequireIdentityTokenSignature = false
                }
            };
            var _oidcClient = new OidcClient(options);
            LoginResult loginResult = null;
            string? username = null;
            DispatcherTimer timer = new DispatcherTimer();
            try
            {
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    try
                    {
                        wpfEmbeddedBrowser.ShowWindowIfHidden();
                    }
                    catch { }
                };
                timer.Start();
                loginResult = await _oidcClient.LoginAsync();
                timer.Stop();
                //string token = loginResult.AccessToken;
                username = loginResult.User == null ? null : loginResult.User.Identity.Name;
                SettingsViewModel.Instance.License.UserName = username;
                Preferences.Set(AppConfigKey.Phalcode1, "true", AppFilePath.UserFile, true);
            }
            catch (System.Exception exception)
            {
                timer.Stop();
                MainWindowViewModel.Instance.AppBarText = exception.Message;
            }
            if (loginResult != null && loginResult.IsError)
            {
                if (loginResult.Error == "UserCancel")
                {
                    loginResult.Error = "Phalcode Sign-in aborted. You can choose to sign in later in the settings.";
                    Preferences.DeleteKey(AppConfigKey.Phalcode1.ToString(), AppFilePath.UserFile);
                }
                MainWindowViewModel.Instance.AppBarText = loginResult.Error;
            }

            //#####GET LISENCE OBJECT#####

            try
            {
                string token = loginResult.AccessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    HttpClient client = new HttpClient();

#if DEBUG
                    var getRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend-test.platform.phalco.de/api/v1/customers/me/subscriptions/prod_Papu5V64dlm12h");
#else
                    var getRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend.platform.phalco.de/api/v1/products/prod_PEZqFd8bFRNg6R");
#endif
                    getRequest.Headers.Add("Authorization", $"Bearer {token}");
                    var licenseResponse = await client.SendAsync(getRequest);
                    licenseResponse.EnsureSuccessStatusCode();
                    string licenseResult = await licenseResponse.Content.ReadAsStringAsync();
                    PhalcodeProduct[] licenseData = JsonSerializer.Deserialize<PhalcodeProduct[]>(licenseResult);
                    if (licenseData.Length == 0)
                    {
                        return;
                    }
                    licenseData[0].UserName = username;
                    SettingsViewModel.Instance.License = licenseData[0];
                    Preferences.Set(AppConfigKey.Phalcode2, JsonSerializer.Serialize(SettingsViewModel.Instance.License), AppFilePath.UserFile, true);
                }
            }
            catch (Exception ex)
            {
                //MainWindowViewModel.Instance.AppBarText = ex.Message;
                try
                {
                    string data = Preferences.Get(AppConfigKey.Phalcode2, AppFilePath.UserFile, true);
                    SettingsViewModel.Instance.License = JsonSerializer.Deserialize<PhalcodeProduct>(data);
                }
                catch
                {
                    return;
                }
            }
            return;
        }
        public void PhalcodeLogout()
        {
            SettingsViewModel.Instance.License = new PhalcodeProduct();
            Preferences.DeleteKey(AppConfigKey.Phalcode1.ToString(), AppFilePath.UserFile);
            Preferences.DeleteKey(AppConfigKey.Phalcode2.ToString(), AppFilePath.UserFile);
            try
            {
                wpfEmbeddedBrowser.ClearAllCookies();
            }
            catch (Exception ex) { }
        }
        private LoginState DetermineLoginState(string code)
        {
            switch (code)
            {
                case "401":
                    {
                        return LoginState.Unauthorized;
                    }
                case "403":
                    {
                        return LoginState.Forbidden;
                    }
            }
            return LoginState.Error;
        }
    }
}
