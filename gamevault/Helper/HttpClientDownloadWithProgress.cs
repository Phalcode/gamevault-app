using gamevault.UserControls;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    public class HttpClientDownloadWithProgress : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly string _destinationFolderPath;
        private string _fileName;
        private bool _Cancelled = false;
        private DateTime lastTime;
        private HttpClient _httpClient;

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler ProgressChanged;

        public HttpClientDownloadWithProgress(string downloadUrl, string destinationFolderPath)
        {
            _downloadUrl = downloadUrl;
            _destinationFolderPath = destinationFolderPath;
        }

        public async Task StartDownload()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(7) };
            string[] auth = WebHelper.GetCredentials();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes($"{auth[0]}:{auth[1]}")));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"GameVault/{SettingsViewModel.Instance.Version}");
            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            _fileName = response.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
            if(string.IsNullOrEmpty(_fileName))
            {
                throw new Exception("Incomplete request header");
            }
            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;
            lastTime = DateTime.Now;
            string fullFilePath = $"{_destinationFolderPath}\\{_fileName}";
            using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    if (_Cancelled)
                    {
                        fileStream.Close();
                        try
                        {
                            File.Delete(fullFilePath);
                        }
                        catch { }
                        return;
                    }

                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    //readCount += 1;                    
                    if ((DateTime.Now - lastTime).TotalSeconds > 2)
                    {
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        lastTime = DateTime.Now;
                    }
                    //if (readCount % 100 == 0)
                    //    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 0);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }
        public void Cancel()
        {
            _Cancelled = true;
        }
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
