using gamevault.Models;
using gamevault.ViewModels;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
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
using System.Timers;
using System.Windows;
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
        private UserProfile userProfile { get; set; }
        private User? m_User { get; set; }
        private LoginState m_LoginState { get; set; }
        private string m_LoginMessage { get; set; }
        private Timer onlineTimer { get; set; }
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
        public string GetServerLoginResponseMessage()
        {
            return m_LoginMessage;
        }
        public void SwitchToOfflineMode()
        {
            MainWindowViewModel.Instance.OnlineState = System.Windows.Visibility.Visible;
            m_User = null;
        }
        public UserProfile GetUserProfile()
        {
            return userProfile;
        }
        public void SetUserProfile(UserProfile profile)
        {
            userProfile = profile;
        }
        public async Task StartupLogin()
        {
            if (IsLoggedIn()) return;

            LoginState state = LoginState.Success;

            try
            {
                //WebHelper.SetCredentials(
                //    Preferences.Get(AppConfigKey.Username,LoginManager.Instance.GetUserProfile().UserConfigFile),
                //    Preferences.Get(AppConfigKey.Password,LoginManager.Instance.GetUserProfile().UserConfigFile, true)
                //);
                string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me");
                m_User = JsonSerializer.Deserialize<User>(result);

                if (m_User == null)
                {
                    state = LoginState.Error;
                }
            }
            catch (Exception ex)
            {
                string code = WebExceptionHelper.GetServerStatusCode(ex);
                state = DetermineLoginState(code);
                if (state == LoginState.Error)
                {
                    m_LoginMessage = WebExceptionHelper.TryGetServerMessage(ex);
                }
            }

            m_LoginState = state;
            InitOnlineTimer();
        }
        public async Task<LoginState> Login(string serverUrl, string username, string password)
        {
            LoginState state = LoginState.Success;
            try
            {
                WebHelper.SetCredentials(serverUrl, username, password);
                string result = await WebHelper.GetAsync(@$"{serverUrl}/api/users/me");
                m_User = JsonSerializer.Deserialize<User>(result);
            }
            catch (Exception ex)
            {
                string code = WebExceptionHelper.GetServerStatusCode(ex);
                state = DetermineLoginState(code);
                if (state == LoginState.Error)
                {
                    m_LoginMessage = WebExceptionHelper.TryGetServerMessage(ex);
                }
            }
            m_LoginState = state;
            return state;
        }
        public async Task<LoginState> ManualLogin(string username, string password)
        {
            LoginState state = LoginState.Success;
            try
            {
                WebHelper.OverrideCredentials(username, password);
                string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me");
                m_User = JsonSerializer.Deserialize<User>(result);
            }
            catch (Exception ex)
            {
                string code = WebExceptionHelper.GetServerStatusCode(ex);
                state = DetermineLoginState(code);
                if (state == LoginState.Error)
                {
                    m_LoginMessage = WebExceptionHelper.TryGetServerMessage(ex);
                }
            }
            m_LoginState = state;
            return state;
        }
        public async void OAuthLogin()
        {
            Window win = new Window() { Height = 600, Width = 800, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            WebView2 uiWebView = new WebView2();
            win.Content = uiWebView;
            win.Show();
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--disk-cache-size=1000000"
            };

            if (!Directory.Exists(ProfileManager.PhalcodeDir))
                Directory.CreateDirectory(ProfileManager.PhalcodeDir);

            var env = await CoreWebView2Environment.CreateAsync(null, ProfileManager.PhalcodeDir, options);
            await uiWebView.EnsureCoreWebView2Async(env);
            uiWebView.Source = new Uri($"{SettingsViewModel.Instance.ServerUrl}/api/auth/oauth2/login");
            uiWebView.NavigationCompleted += async (s, e) =>
            {
                string content = await uiWebView.CoreWebView2.ExecuteScriptAsync("document.body.innerText");

                try
                {
                    string result = System.Text.Json.JsonSerializer.Deserialize<string>(content);
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(result);
                    string accessToken = authResponse?.AccessToken;
                    string refreshToken = authResponse?.RefreshToken;
                    WebHelper.InjectTokens(accessToken, refreshToken);
                    await LoginManager.Instance.ManualLogin("", "");
                    MainWindowViewModel.Instance.UserAvatar = LoginManager.Instance.GetCurrentUser();
                    win.Close();
                    uiWebView.Dispose();
                }
                catch (Exception ex)
                {

                }
            };
        }
        public void Logout()
        {
            m_User = null;
            m_LoginState = LoginState.Error;
            WebHelper.OverrideCredentials(string.Empty, string.Empty);
            MainWindowViewModel.Instance.Community.Reset();
        }
        private WpfEmbeddedBrowser wpfEmbeddedBrowser = null;
        public async Task<string> PhalcodeLogin(bool startHidden = false)
        {
            string returnMessage = "";
            string? provider = Preferences.Get(AppConfigKey.Phalcode1, ProfileManager.ProfileConfigFile, true);
            if (startHidden && provider == "")
            {
                return "";
            }
            wpfEmbeddedBrowser = new WpfEmbeddedBrowser(startHidden);
            var options = new OidcClientOptions()
            {
                Authority = "https://auth.platform.phalco.de/realms/phalcode",
                ClientId = "gamevault-app",
                Scope = "openid profile email",
                RedirectUri = "http://127.0.0.1:11121/gamevault",
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


                Parameters param = null;
                if (provider == "microsoft" || provider == "google" || provider == "discord" || provider == "github")
                {
                    param = new Parameters();
                    param.Add("kc_idp_hint", provider);
                }

                loginResult = await _oidcClient.LoginAsync(new LoginRequest() { FrontChannelExtraParameters = param });
                timer.Stop();               
                username = loginResult.User == null ? null : loginResult.User.Identity.Name;
                SettingsViewModel.Instance.License = new PhalcodeProduct() { UserName = username };

                var handler = new JwtSecurityTokenHandler();
                var t = handler.ReadJwtToken(loginResult.AccessToken);
                provider = t.Claims.FirstOrDefault(claim => claim.Type == "identity_provider")?.Value;
                if (string.IsNullOrEmpty(provider))
                {
                    provider = "phalcode";
                }
                Preferences.Set(AppConfigKey.Phalcode1, provider, ProfileManager.ProfileConfigFile, true);
            }
            catch (System.Exception exception)
            {
                timer.Stop();
                returnMessage = exception.Message;
            }
            if (loginResult != null && loginResult.IsError)
            {
                if (loginResult.Error == "UserCancel")
                {
                    returnMessage = "Phalcode Sign-in aborted. You can choose to sign in later in the settings.";
                    Preferences.DeleteKey(AppConfigKey.Phalcode1.ToString(), ProfileManager.ProfileConfigFile);
                }                
            }

            //#####GET LISENCE OBJECT#####

            try
            {
                string token = loginResult.AccessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    HttpClient client = new HttpClient();

                    var getRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend.platform.phalco.de/api/v1/customers/me/subscriptions/prod_PEZqFd8bFRNg6R");
                    if (SettingsViewModel.Instance.DevTargetPhalcodeTestBackend)
                    {
                        getRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend-test.platform.phalco.de/api/v1/customers/me/subscriptions/prod_PuyurQTh7H5uZe");
                    }
                    getRequest.Headers.Add("Authorization", $"Bearer {token}");
                    var licenseResponse = await client.SendAsync(getRequest);
                    licenseResponse.EnsureSuccessStatusCode();
                    string licenseResult = await licenseResponse.Content.ReadAsStringAsync();
                    PhalcodeProduct[] licenseData = JsonSerializer.Deserialize<PhalcodeProduct[]>(licenseResult);
                    if (licenseData.Length == 0)
                    {
                        return "";
                    }
                    licenseData[0].UserName = username;
                    SettingsViewModel.Instance.License = licenseData[0];
                    Preferences.Set(AppConfigKey.Phalcode2, JsonSerializer.Serialize(SettingsViewModel.Instance.License), ProfileManager.ProfileConfigFile, true);
                }
            }
            catch (Exception ex)
            {
                returnMessage = ex.Message;
                try
                {
                    string data = Preferences.Get(AppConfigKey.Phalcode2, ProfileManager.ProfileConfigFile, true);
                    SettingsViewModel.Instance.License = JsonSerializer.Deserialize<PhalcodeProduct>(data);
                }
                catch
                {
                    return "";
                }
            }
            try
            {
                if (!SettingsViewModel.Instance.License.IsActive())
                {
                    Preferences.DeleteKey(AppConfigKey.Theme, ProfileManager.ProfileConfigFile);
                }
            }
            catch { }
            return returnMessage;
        }
        public void PhalcodeLogout()
        {
            SettingsViewModel.Instance.License = new PhalcodeProduct();
            Preferences.DeleteKey(AppConfigKey.Phalcode1.ToString(), ProfileManager.ProfileConfigFile);
            Preferences.DeleteKey(AppConfigKey.Phalcode2.ToString(), ProfileManager.ProfileConfigFile);
            Preferences.DeleteKey(AppConfigKey.Theme, ProfileManager.ProfileConfigFile);
            try
            {
                Directory.Delete(LoginManager.Instance.GetUserProfile().WebConfigDir, true);
                //wpfEmbeddedBrowser.ClearAllCookies();
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
        private void InitOnlineTimer()
        {
            if (onlineTimer == null)
            {
                onlineTimer = new Timer(30000);//30 Seconds
                onlineTimer.AutoReset = true;
                onlineTimer.Elapsed += CheckOnlineStatus;
                onlineTimer.Start();
            }
        }
        private async void CheckOnlineStatus(object sender, EventArgs e)
        {
            try
            {
                string serverResonse = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/status");
                if (!IsLoggedIn())
                {
                    await StartupLogin();
                    if (IsLoggedIn())
                    {
                        MainWindowViewModel.Instance.OnlineState = System.Windows.Visibility.Collapsed;
                    }
                }
                else
                {
                    MainWindowViewModel.Instance.OnlineState = System.Windows.Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                SwitchToOfflineMode();
            }
        }
    }
}
