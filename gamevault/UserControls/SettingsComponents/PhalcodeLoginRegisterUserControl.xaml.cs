using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
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
using Windows.Media.Protection.PlayReady;

namespace gamevault.UserControls.SettingsComponents
{
    /// <summary>
    /// Interaction logic for PhalcodeLoginRegisterUserControl.xaml
    /// </summary>
    public partial class PhalcodeLoginRegisterUserControl : UserControl
    {
        private HttpClient client = new HttpClient();
        public PhalcodeLoginRegisterUserControl()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }      

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.platform.phalco.de/realms/phalcode/protocol/openid-connect/token");
            var collection = new List<KeyValuePair<string, string>>();
            collection.Add(new("username", uiTxtUsername.Text));
            collection.Add(new("password", uiTxtPassword.Password));
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


                MainWindowViewModel.Instance.AppBarText = jwtToken.Claims.Where(c => c.Type == "name").FirstOrDefault().Value;
                string productId = jwtToken.Claims.Where(c => c.Type == "sub").FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(productId))
                {
                    var getRequest = new HttpRequestMessage(HttpMethod.Get, $"https://customer-backend.platform.phalco.de/users/me/licenses/{productId}");
                    getRequest.Headers.Add("Authorization", $"Bearer {token}");
                    var licenseResponse = await client.SendAsync(getRequest);
                    if (licenseResponse.IsSuccessStatusCode)
                    {
                        string licenseResult = await licenseResponse.Content.ReadAsStringAsync();
                        dynamic licenseData = JsonSerializer.Deserialize<ExpandoObject>(licenseResult);
                        //hasLicense:bool
                        //endDate:DateTime
                    }
                }

            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
            ((Button)sender).IsEnabled = true;
        }
        private void Help_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string url = ((FrameworkElement)sender).Tag as string;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                string url = e.Uri.OriginalString;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }       
    }
}
