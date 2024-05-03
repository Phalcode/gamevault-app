using gamevault.Models;
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
        private string _fallbackFileName;
        KeyValuePair<string, string>? _additionalHeader;
        private bool _Cancelled = false;
        private bool _Paused = false;
        private long _ResumePosition = -1;
        private long PreResumeSize = -1;
        private DateTime lastTime;
        private HttpClient _httpClient;

        public delegate void ProgressChangedHandler(long preResumeSize, long? totalFileSize, long currentBytesDownloaded, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler ProgressChanged;

        public HttpClientDownloadWithProgress(string downloadUrl, string destinationFolderPath, string fallbackFileName, KeyValuePair<string, string>? additionalHeader = null)
        {
            _downloadUrl = downloadUrl;
            _destinationFolderPath = destinationFolderPath;
            _fallbackFileName = fallbackFileName;
            _additionalHeader = additionalHeader;
        }

        public async Task StartDownload(bool tryResume = false)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(7) };
            string[] auth = WebHelper.GetCredentials();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes($"{auth[0]}:{auth[1]}")));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"GameVault/{SettingsViewModel.Instance.Version}");

            if (_additionalHeader != null)
            {
                _httpClient.DefaultRequestHeaders.Add(_additionalHeader.Value.Key, _additionalHeader.Value.Value);
            }
            if (tryResume)
            {
                string resumeData = Preferences.Get(AppConfigKey.DownloadProgress, $"{_destinationFolderPath}\\gamevault-metadata");
                if (!string.IsNullOrEmpty(resumeData))
                {
                    try
                    {
                        string[] resumeDataToProcess = resumeData.Split(";");
                        _ResumePosition = long.Parse(resumeDataToProcess[0]);
                        PreResumeSize = long.Parse(resumeDataToProcess[1]);
                        _httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(long.Parse(resumeDataToProcess[0]), null);
                        TriggerProgressChanged(long.Parse(resumeDataToProcess[1]), long.Parse(resumeDataToProcess[0]));
                    }
                    catch { }
                }
            }

            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            try
            {
                _fileName = response.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
                if (string.IsNullOrEmpty(_fileName))
                {
                    throw new Exception("Missing response header (Content-Disposition)");
                }
            }
            catch
            {
                _fileName = _fallbackFileName;
            }
            var responseContentLength = response.Content.Headers.ContentLength;
            if (responseContentLength == null || responseContentLength == 0)
            {
                throw new Exception("Missing response header (Content-Length)");
            }
            //totalBytes = PreResumeSize == -1 ? totalBytes : PreResumeSize;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(responseContentLength, contentStream);
        }

        private async Task ProcessContentStream(long? currentDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;
            lastTime = DateTime.Now;
            string fullFilePath = $"{_destinationFolderPath}\\{_fileName}";
            using (var fileStream = new FileStream(fullFilePath, _ResumePosition == -1 ? FileMode.Create : FileMode.Open, FileAccess.Write, FileShare.None, 8192, true))
            {
                if (_ResumePosition != -1)
                {
                    fileStream.Position = _ResumePosition;
                    totalBytesRead = _ResumePosition;
                }
                do
                {
                    if (_Cancelled)
                    {
                        if (_Paused)
                        {
                            Preferences.Set(AppConfigKey.DownloadProgress, $"{fileStream.Position};{(PreResumeSize == -1 ? currentDownloadSize : PreResumeSize)}", $"{_destinationFolderPath}\\gamevault-metadata");
                            fileStream.Close();
                            TriggerProgressChanged(currentDownloadSize, totalBytesRead);
                            return;
                        }
                        fileStream.Close();
                        try
                        {
                            await Task.Delay(1000);
                            File.Delete(fullFilePath);
                            File.Delete($"{_destinationFolderPath}\\gamevault-metadata");
                        }
                        catch { }
                        return;
                    }

                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        fileStream.Close();
                        TriggerProgressChanged(currentDownloadSize, totalBytesRead);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    if ((DateTime.Now - lastTime).TotalSeconds > 2)
                    {
                        TriggerProgressChanged(currentDownloadSize, totalBytesRead);
                        lastTime = DateTime.Now;
                    }
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
                progressPercentage = Math.Round((double)totalBytesRead / (PreResumeSize == -1 ? totalDownloadSize.Value : PreResumeSize) * 100, 0);

            ProgressChanged(PreResumeSize, totalDownloadSize, _ResumePosition == -1 ? totalBytesRead : (totalBytesRead - _ResumePosition), totalBytesRead, progressPercentage);
        }
        public void Cancel()
        {
            if (_Paused)
            {
                try
                {
                    File.Delete($"{_destinationFolderPath}\\{_fileName}");
                    File.Delete($"{_destinationFolderPath}\\gamevault-metadata");
                }
                catch { }
                return;
            }
            _Cancelled = true;
            Dispose();
        }
        public void Pause()
        {
            _Paused = true;
            _Cancelled = true;
            Dispose();
        }
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
