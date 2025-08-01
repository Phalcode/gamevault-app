using gamevault.Models;
using gamevault.UserControls;
using gamevault.ViewModels;
using LiveChartsCore.Kernel;
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
        private readonly string DownloadUrl;
        private readonly string DestinationFolderPath;
        private string FileName;
        private string FallbackFileName;
        private KeyValuePair<string, string>? AdditionalHeader;
        private bool Cancelled = false;
        private bool Paused = false;
        private long ResumePosition = -1;
        private long PreResumeSize = -1;
        private DateTime LastTime;
        private HttpClient HttpClient;

        public delegate void ProgressChangedHandler(long totalFileSize, long currentBytesDownloaded, long totalBytesDownloaded, double? progressPercentage, long resumePosition);

        public event ProgressChangedHandler ProgressChanged;

        public HttpClientDownloadWithProgress(string downloadUrl, string destinationFolderPath, string fallbackFileName, KeyValuePair<string, string>? additionalHeader = null)
        {
            DownloadUrl = downloadUrl;
            DestinationFolderPath = destinationFolderPath;
            FallbackFileName = fallbackFileName;
            AdditionalHeader = additionalHeader;
        }

        public async Task StartDownload(bool tryResume = false)
        {
            HttpClient = new HttpClient { Timeout = TimeSpan.FromDays(7) };
            CreateHeader();
            if (tryResume)
            {
                InitResume();
            }
            else
            {
                //Edge case where the Library download overrrides the current download. But if its was a paused download, we also have to reset the metadata
                if (File.Exists($"{DestinationFolderPath}\\gamevault-metadata"))
                    File.Delete($"{DestinationFolderPath}\\gamevault-metadata");
            }

            using (HttpResponseMessage response = await WebHelper.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }
        private void CreateHeader()
        {
            string[] auth = WebHelper.GetCredentials();
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth[0]}:{auth[1]}")));
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"GameVault/{SettingsViewModel.Instance.Version}");

            if (AdditionalHeader != null)
                HttpClient.DefaultRequestHeaders.Add(AdditionalHeader.Value.Key, AdditionalHeader.Value.Value);
        }
        private void InitResume()
        {
            string resumeData = Preferences.Get(AppConfigKey.DownloadProgress, $"{DestinationFolderPath}\\gamevault-metadata");
            if (!string.IsNullOrEmpty(resumeData))
            {
                try
                {
                    string[] resumeDataToProcess = resumeData.Split(";");
                    ResumePosition = long.Parse(resumeDataToProcess[0]);
                    PreResumeSize = long.Parse(resumeDataToProcess[1]);
                    HttpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(long.Parse(resumeDataToProcess[0]), null);
                }
                catch { }
            }
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            try
            {
                FileName = response.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
                if (string.IsNullOrEmpty(FileName))
                {
                    throw new Exception("Missing response header (Content-Disposition)");
                }
            }
            catch
            {
                FileName = FallbackFileName;
            }
            var responseContentLength = response.Content.Headers.ContentLength;
            if (responseContentLength == null || responseContentLength == 0)
            {
                if (response.Headers.TryGetValues("X-Download-Size", out var headerValues) && long.TryParse(headerValues.First(), out long length))
                {
                    responseContentLength = length;
                }
                else
                {
                    throw new Exception("Missing response header (Content-Length/X-Download-Size)");
                }
            }

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(responseContentLength.Value, contentStream);
        }

        private async Task ProcessContentStream(long currentDownloadSize, Stream contentStream)
        {
            long currentBytesRead = 0;
            byte[] buffer = new byte[8192];
            bool isMoreToRead = true;
            LastTime = DateTime.Now;
            string fullFilePath = $"{DestinationFolderPath}\\{FileName}";
            using (var fileStream = new FileStream(fullFilePath, ResumePosition == -1 ? FileMode.Create : FileMode.Open, FileAccess.Write, FileShare.None, 8192, true))
            {
                try
                {
                    if (ResumePosition != -1)
                    {
                        fileStream.Position = ResumePosition;
                    }
                    do
                    {
                        if (Cancelled)
                        {
                            if (Paused)
                            {
                                Preferences.Set(AppConfigKey.DownloadProgress, $"{fileStream.Position};{(PreResumeSize == -1 ? currentDownloadSize : PreResumeSize)}", $"{DestinationFolderPath}\\gamevault-metadata");
                                TriggerProgressChanged(currentDownloadSize, 0, fileStream.Position);
                                fileStream.Close();
                                return;
                            }
                            fileStream.Close();
                            try
                            {
                                await Task.Delay(1000);
                                File.Delete($"{DestinationFolderPath}\\gamevault-metadata");
                                File.Delete(fullFilePath);
                            }
                            catch { }
                            return;
                        }

                        var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            TriggerProgressChanged(currentDownloadSize, currentBytesRead, fileStream.Position);
                            fileStream.Close();
                            continue;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                        currentBytesRead += bytesRead;
                        if ((DateTime.Now - LastTime).TotalMilliseconds > 2000)
                        {
                            //Save checkpoints all two seconds in case the app is closed by the user, or hardly crashed
                            Preferences.Set(AppConfigKey.DownloadProgress, $"{fileStream.Position};{(PreResumeSize == -1 ? currentDownloadSize : PreResumeSize)}", $"{DestinationFolderPath}\\gamevault-metadata");
                            TriggerProgressChanged(currentDownloadSize, currentBytesRead, fileStream.Position);
                            LastTime = DateTime.Now;
                        }
                    }
                    while (isMoreToRead);
                }
                catch (Exception ex)//On exception try to save the download progress and forward the exception
                {
                    if (currentBytesRead > 0)
                    {
                        Preferences.Set(AppConfigKey.DownloadProgress, $"{fileStream.Position};{(PreResumeSize == -1 ? currentDownloadSize : PreResumeSize)}", $"{DestinationFolderPath}\\gamevault-metadata");
                    }
                    throw;
                }
            }
        }

        private void TriggerProgressChanged(long totalDownloadSize, long currentBytesRead, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            totalDownloadSize = PreResumeSize == -1 ? totalDownloadSize : PreResumeSize;
            double progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize * 100, 0);
            ProgressChanged(totalDownloadSize, currentBytesRead, totalBytesRead, progressPercentage, ResumePosition);
        }
        public void Cancel()
        {
            if (Paused)
            {
                try
                {
                    File.Delete($"{DestinationFolderPath}\\gamevault-metadata");
                    File.Delete($"{DestinationFolderPath}\\{FileName}");
                }
                catch { }
                return;
            }
            Cancelled = true;
            Dispose();
        }
        public void Pause()
        {
            Paused = true;
            Cancelled = true;
            Dispose();
        }
        public void Dispose()
        {
            HttpClient?.Dispose();
        }
    }
}
