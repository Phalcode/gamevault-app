using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;
using gamevault.Models;
using System.Diagnostics;

namespace gamevault.Helper
{
    public class SSOHttpClient
    {
        private readonly HttpClient _httpClient;
        private string _accessToken;
        private string _refreshToken;

        public string ServerUrl;
        public string UserName;
        public string Password;

        public SSOHttpClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"GameVault/{SettingsViewModel.Instance.Version}");
            _httpClient.Timeout = new TimeSpan(0, 0, 20);
        }
        public void InjectTokens(string accessToken, string refreshToken)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
        }
        public string GetRefreshToken()
        {
            return _refreshToken;
        }
        public void Reset()
        {
            _accessToken = "";
            _refreshToken = "";
        }
        public async Task<bool> LoginBasicAuthAsync(string username, string password)
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var response = await _httpClient.GetAsync($"{ServerUrl}/api/auth/basic/login");
            await WebExceptionHelper.EnsureSuccessStatusCode(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<AuthResponse>(responseContent);

            _accessToken = json?.AccessToken;
            _refreshToken = json?.RefreshToken;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return true;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, RequestHeader[]? additionalHeaders = null, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                await LoginBasicAuthAsync(UserName, Password);
            }

            if (IsTokenExpired() && !await RefreshTokenAsync())
                throw new InvalidOperationException("Failed to refresh token.");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }
            }

            return await _httpClient.SendAsync(request, option);
        }

        public Task<HttpResponseMessage> GetAsync(string url, RequestHeader[]? additionalHeaders = null, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Get, url), additionalHeaders, option);

        public Task<HttpResponseMessage> PostAsync(string url, HttpContent content, RequestHeader[]? additionalHeaders = null) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = content }, additionalHeaders);

        public Task<HttpResponseMessage> PutAsync(string url, HttpContent content, RequestHeader[]? additionalHeaders = null) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Put, url) { Content = content }, additionalHeaders);

        public Task<HttpResponseMessage> DeleteAsync(string url, RequestHeader[]? additionalHeaders = null) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Delete, url), additionalHeaders);

        private async Task<bool> RefreshTokenAsync()
        {
            var content = new StringContent("", Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _refreshToken);

            var response = await _httpClient.PostAsync($"{ServerUrl}/api/auth/refresh", content);
            if (!response.IsSuccessStatusCode) return false;

            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<AuthResponse>(responseContent);

            _accessToken = json?.AccessToken;
            _refreshToken = json?.RefreshToken;
            nextTokenRefresh = GetNextTokenRefresh(_accessToken);
            //Debug.WriteLine($"Token refreshed. Next refresh at: {nextTokenRefresh}");
            return true;
        }
        DateTimeOffset nextTokenRefresh;
        private bool IsTokenExpired()
        {
            return nextTokenRefresh <= DateTimeOffset.UtcNow.AddMinutes(1);
        }
        private DateTimeOffset GetNextTokenRefresh(string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return DateTimeOffset.UtcNow;

            var payload = parts[1];
            var jsonBytes = Convert.FromBase64String(Base64UrlDecode(payload));
            var json = JsonSerializer.Deserialize<JwtPayload>(Encoding.UTF8.GetString(jsonBytes));            
            if (json?.Exp == null)
            {
                return DateTimeOffset.UtcNow;
            }
            var expTimestamp = DateTimeOffset.FromUnixTimeSeconds(json.Exp);
            var creationTimestamp = DateTimeOffset.FromUnixTimeSeconds(json.Creation);
            return DateTimeOffset.UtcNow + (expTimestamp - creationTimestamp);
        }

        private string Base64UrlDecode(string input)
        {
            input = input.Replace('-', '+').Replace('_', '/');
            return input.PadRight(input.Length + (4 - input.Length % 4) % 4, '=');
        }
    }
    public class AuthResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class JwtPayload
    {
        [JsonPropertyName("iat")]
        public long Creation { get; set; }
        [JsonPropertyName("exp")]
        public long Exp { get; set; }
    }
}
