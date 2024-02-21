using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        public async Task<bool> PhalcodeLogin(string userName = "", string password = "")
        {
            bool saveCredencials = true;
            if (userName == "" || password == "")
            {
                saveCredencials = false;
                userName = Preferences.Get(AppConfigKey.Phalcode1, AppFilePath.UserFile, true);
                password = Preferences.Get(AppConfigKey.Phalcode2, AppFilePath.UserFile, true);
                if (userName == "" || password == "")
                {
                    return false;
                }
            }
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.platform.phalco.de/realms/phalcode/protocol/openid-connect/token");
            var collection = new List<KeyValuePair<string, string>>();
            collection.Add(new("username", userName));
            collection.Add(new("password", password));
            collection.Add(new("client_id", "customer-backend"));
            collection.Add(new("grant_type", "password"));
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await client.SendAsync(request);
            try
            {
                response.EnsureSuccessStatusCode();
                string result = await response.Content.ReadAsStringAsync();

                dynamic data = JsonSerializer.Deserialize<ExpandoObject>(result);
                JsonElement jsonToken = data.access_token;
                string token = jsonToken.GetString();

                //Parse to JWT 
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = (JwtSecurityToken)handler.ReadToken(token);


                string fullPhalcodeUserName = jwtToken.Claims.Where(c => c.Type == "name").FirstOrDefault().Value;

                //Products
                //var productRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend.platform.phalco.de/api/v1/products");              
                //var productResponse = await client.SendAsync(productRequest);
                //string productResult = await productResponse.Content.ReadAsStringAsync();
                //dynamic productData = JsonSerializer.Deserialize<ExpandoObject>(productResult);
                //#############################BUY_PRODUCT
                //var productRequest = new HttpRequestMessage(HttpMethod.Post, $"https://customer-backend-test.platform.phalco.de/api/v1/stripe/checkout");
                //string jsonContent = "{\"price_id\": \"price_1OleEqDhq2Ud7md3ig9f0p7U\"}";
                //var con = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                //productRequest.Content = con;
                //productRequest.Headers.Add("Authorization", $"Bearer {token}");
                //var productResponse = await client.SendAsync(productRequest);
                //string productResult = await productResponse.Content.ReadAsStringAsync();
                //dynamic productData = JsonSerializer.Deserialize<ExpandoObject>(productResult);
                //jwtToken.Claims.Where(c => c.Type == "sub").FirstOrDefault().Value;               

#if DEBUG
                var getRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend-test.platform.phalco.de/api/v1/customers/me/subscriptions/prod_Papu5V64dlm12h");
#else
                var getRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend.platform.phalco.de/api/v1/products/prod_PEZqFd8bFRNg6R");
#endif
                getRequest.Headers.Add("Authorization", $"Bearer {token}");
                var licenseResponse = await client.SendAsync(getRequest);
                if (licenseResponse.IsSuccessStatusCode)
                {
                    string licenseResult = await licenseResponse.Content.ReadAsStringAsync();
                    PhalcodeProduct[] licenseData = JsonSerializer.Deserialize<PhalcodeProduct[]>(licenseResult);
                    if (licenseData.Length == 0)
                    {
                        SettingsViewModel.Instance.License.UserName = fullPhalcodeUserName;
                        return true;
                    }
                    licenseData[0].UserName = fullPhalcodeUserName;
                    SettingsViewModel.Instance.License = licenseData[0];
                    if (saveCredencials)
                    {
                        Preferences.Set(AppConfigKey.Phalcode1, userName, AppFilePath.UserFile, true);
                        Preferences.Set(AppConfigKey.Phalcode2, password, AppFilePath.UserFile, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
                return false;
            }
            return true;
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
