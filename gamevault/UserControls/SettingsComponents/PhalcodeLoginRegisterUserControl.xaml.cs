using gamevault.ViewModels;
using System;
using System.Collections.Generic;
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

        private void Help_Click(object sender, MouseButtonEventArgs e)
        {
            //Open Help Website
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

                string tokenString = jwtToken.ToString();
                MainWindowViewModel.Instance.AppBarText = jwtToken.Claims.Where(c => c.Type == "name").FirstOrDefault().Value;

            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
            ((Button)sender).IsEnabled = true;
        }
    }
}
