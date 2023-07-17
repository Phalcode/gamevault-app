using ControlzEx.Standard;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    internal class WebHelper
    {

        private static string m_UserName { get; set; }
        private static string m_Password { get; set; }
        internal static void SetCredentials(string username, string password)
        {
            m_UserName = username;
            m_Password = password;
        }
        internal static string[] GetCredentials()
        {
            return new string[] { m_UserName, m_Password };
        }
        internal static void OverrideCredentials(string username, string password)
        {
            m_UserName = username;
            m_Password = password;
            Preferences.Set(AppConfigKey.Username, m_UserName, AppFilePath.UserFile);
            Preferences.Set(AppConfigKey.Password, m_Password, AppFilePath.UserFile, true);
        }
        internal static string GetRequest(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = $"GameVault/{SettingsViewModel.Instance.Version}";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var authenticationString = $"{m_UserName}:{m_Password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(authenticationString));
            request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        internal static string Put(string uri, string payload, bool returnBody = false)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = $"GameVault/{SettingsViewModel.Instance.Version}";
            request.Method = "PUT";
            request.ContentType = "application/json";
            if (payload != null)
            {
                var authenticationString = $"{m_UserName}:{m_Password}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(authenticationString));
                request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
                request.ContentLength = System.Text.UTF8Encoding.UTF8.GetByteCount(payload);
                Stream dataStream = request.GetRequestStream();
                using (StreamWriter sr = new StreamWriter(dataStream))
                {
                    sr.Write(payload);
                }
                dataStream.Close();
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (returnBody)
            {
                using (StreamReader responseStreamReader = new StreamReader(response.GetResponseStream()))
                {
                    return responseStreamReader.ReadToEnd();
                }
            }
            else
            {
                return response.StatusCode.ToString();
            }
        }
        internal static void Post(string uri, string payload)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = $"GameVault/{SettingsViewModel.Instance.Version}";
            request.Method = "POST";
            request.ContentType = "application/json";
            if (payload != null)
            {
                var authenticationString = $"{m_UserName}:{m_Password}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(authenticationString));
                request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
                request.ContentLength = System.Text.UTF8Encoding.UTF8.GetByteCount(payload);
                Stream dataStream = request.GetRequestStream();
                using (StreamWriter sr = new StreamWriter(dataStream))
                {
                    sr.Write(payload);
                }
                dataStream.Close();
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string returnString = response.StatusCode.ToString();
        }
        internal static string Delete(string uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = $"GameVault/{SettingsViewModel.Instance.Version}";
            request.Method = "DELETE";
            var authenticationString = $"{m_UserName}:{m_Password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(authenticationString));
            request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        public static async Task DownloadImageFromUrlAsync(string imageUrl, string cacheFile)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes($"{m_UserName}:{m_Password}")));
                client.Headers.Add($"User-Agent: GameVault/{SettingsViewModel.Instance.Version}");
                await client.DownloadFileTaskAsync(new Uri(imageUrl), cacheFile);
            }
            //using (HttpClient client = new HttpClient())
            //{
            //    client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes($"{m_UserName}:{m_Password}")));
            //    client.DefaultRequestHeaders.Add("User-Agent", $"GameVault/{SettingsViewModel.Instance.Version}");
            //    var response = await client.GetAsync(new Uri(imageUrl));
            //    using (FileStream fs = new FileStream(cacheFile, FileMode.Create))
            //    {
            //        await response.Content.CopyToAsync(fs);
            //    }
            //}
        }
    }
}
