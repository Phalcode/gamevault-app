using gamevault.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace gamevault.Helper
{
    internal class WebHelper
    {
        private static readonly SSOHttpClient HttpClient = new SSOHttpClient();
        private static readonly HttpClient BaseHttpClient = new HttpClient()
        {
            DefaultRequestHeaders = { { "User-Agent", "GameVault" } }
        };
        private static RequestHeader[] AdditionalRequestHeaders;
        static WebHelper() { }
        internal static void SetCredentials(string serverUrl, string username, string password)
        {
            HttpClient.Reset();
            HttpClient.ServerUrl = serverUrl;
            HttpClient.UserName = username;
            HttpClient.Password = password;
        }
        internal static string[] GetCredentials() => new[] { HttpClient.UserName, HttpClient.Password };
        internal static void OverrideCredentials(string username, string password)
        {
            HttpClient.UserName = username;
            Preferences.Set(AppConfigKey.Username, HttpClient.UserName, LoginManager.Instance.GetUserProfile().UserConfigFile);
            if (!string.IsNullOrWhiteSpace(password))
            {
                HttpClient.Password = password;
                Preferences.Set(AppConfigKey.Password, HttpClient.Password, LoginManager.Instance.GetUserProfile().UserConfigFile, true);
            }
        }
        internal static void InjectTokens(string accessToken, string refreshToken)
        {
            HttpClient.InjectTokens(accessToken, refreshToken);
        }
        internal static string GetRefreshToken()
        {
            return HttpClient.GetRefreshToken();
        }
        internal static void SetAdditionalRequestHeaders(RequestHeader[] additionalRequestHeaders)
        {
            AdditionalRequestHeaders = additionalRequestHeaders;

            BaseHttpClient.DefaultRequestHeaders.Clear();
            BaseHttpClient.DefaultRequestHeaders.Add("User-Agent", "GameVault");
            foreach (var header in AdditionalRequestHeaders)
            {
                BaseHttpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
            }
        }
        #region BASE REQUESTS
        internal static async Task<string> BaseGetAsync(string url)
        {
            var response = await BaseHttpClient.GetAsync(url);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return await response.Content.ReadAsStringAsync();
        }
        internal static async Task<string> BasePostAsync(string url, string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await BaseHttpClient.PostAsync(url, content);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return await response.Content.ReadAsStringAsync();
        }
        internal static async Task<string> BaseSendRequest(HttpRequestMessage request)
        {
            var response = await BaseHttpClient.SendAsync(request);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return await response.Content.ReadAsStringAsync();
        }
        #endregion
        internal static async Task<string> GetAsync(string url)
        {
            var response = await HttpClient.GetAsync(url, AdditionalRequestHeaders);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return await response.Content.ReadAsStringAsync();
        }
        internal static async Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
        {
            var response = await HttpClient.GetAsync(url, AdditionalRequestHeaders, option);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return response;
        }
        internal static async Task<string> PostAsync(string url, string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(url, content, AdditionalRequestHeaders);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return await response.Content.ReadAsStringAsync();
        }

        internal static async Task<string> PutAsync(string url, string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await HttpClient.PutAsync(url, content, AdditionalRequestHeaders);
            await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return await response.Content.ReadAsStringAsync();
        }
        internal static async Task<string> DeleteAsync(string url)
        {
            var response = await HttpClient.DeleteAsync(url, AdditionalRequestHeaders);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task DownloadImageFromUrlAsync(string imageUrl, string cacheFile)
        {
            var response = await HttpClient.GetAsync(imageUrl, AdditionalRequestHeaders);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(cacheFile, imageBytes);
        }
        public static async Task<BitmapImage> DownloadImageFromUrlAsync(string imageUrl)
        {
            var response = await HttpClient.GetAsync(imageUrl, AdditionalRequestHeaders);
           await WebExceptionHelper.EnsureSuccessStatusCode(response);
            var imageData = await response.Content.ReadAsByteArrayAsync();
            using (var memoryStream = new MemoryStream(imageData))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = memoryStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
        public static async Task<string> UploadFileAsync(string apiUrl, Stream imageStream, string fileName, RequestHeader[]? additionalHeaders = null)
        {
            //Mix request headers
            RequestHeader[]? mixedHeaders = null;
            if (additionalHeaders != null && AdditionalRequestHeaders != null)
            {
                mixedHeaders = AdditionalRequestHeaders.Concat(additionalHeaders).ToArray();
            }
            else if (additionalHeaders == null)
            {
                mixedHeaders = AdditionalRequestHeaders;
            }
            else if (AdditionalRequestHeaders == null)
            {
                mixedHeaders = additionalHeaders;
            }
            using (var formData = new MultipartFormDataContent())
            {
                var imageContent = new StreamContent(imageStream);
                string mimeType = MimeTypeHelper.GetMimeType(fileName);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                formData.Add(imageContent, "file", fileName);
                var response = await HttpClient.PostAsync(apiUrl, formData, mixedHeaders);
               await WebExceptionHelper.EnsureSuccessStatusCode(response);
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
        }
        public static string RemoveSpecialCharactersFromUrl(string url)
        {
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(7);
            }
            else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(8);
            }

            StringBuilder sb = new StringBuilder();
            foreach (char c in url)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        
    }
}


