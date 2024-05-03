using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    // This class handles downloading files using HttpClient with progress tracking.
    public class HttpClientDownloadWithProgress : IDisposable
    {
        // Fields to store necessary information for download process.
        private readonly string _downloadUrl;
        private readonly string _destinationFolderPath;
        private readonly string _fallbackFileName;
        private readonly KeyValuePair<string, string>? _additionalHeader;
        private readonly HttpClient _httpClient;
        private bool _cancelled = false;
        private bool _paused = false;
        private long _resumePosition = -1;
        private long _preResumeSize = -1;
        private DateTime _lastProgressUpdate;

        // Delegate and event for progress tracking.
        public delegate void ProgressChangedHandler(long preResumeSize, long? totalFileSize, long currentBytesDownloaded, long totalBytesDownloaded, double? progressPercentage);
        public event ProgressChangedHandler ProgressChanged;

        // Constructor to initialize HttpClient and set download parameters.
        public HttpClientDownloadWithProgress(string downloadUrl, string destinationFolderPath, string fallbackFileName, KeyValuePair<string, string>? additionalHeader = null)
        {
            _downloadUrl = downloadUrl;
            _destinationFolderPath = destinationFolderPath;
            _fallbackFileName = fallbackFileName;
            _additionalHeader = additionalHeader;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(7) }; // Set timeout to 7 days.
        }

        // Starts the download process.
        public async Task StartDownload(bool tryResume = false)
        {
            string[] auth = WebHelper.GetCredentials(); // Get authentication credentials.
            // Set authorization header.
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes($"{auth[0]}:{auth[1]}")));
            // Set user-agent header.
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"GameVault/{SettingsViewModel.Instance.Version}");
            if (tryResume)
                TryResumeDownload(); // Try to resume download if requested.

            // Send GET request to download the file.
            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }

        // Tries to resume a partially completed download.
        private void TryResumeDownload()
        {
            // Get resume data from preferences.
            string resumeData = Preferences.Get(AppConfigKey.DownloadProgress, $"{_destinationFolderPath}\\gamevault-metadata");
            if (!string.IsNullOrEmpty(resumeData))
            {
                string[] resumeDataToProcess = resumeData.Split(";");
                if (resumeDataToProcess.Length >= 2)
                {
                    if (long.TryParse(resumeDataToProcess[0], out _resumePosition) && long.TryParse(resumeDataToProcess[1], out _preResumeSize))
                    {
                        // Set range header for resuming download.
                        _httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(_resumePosition, null);
                        TriggerProgressChanged(_preResumeSize, _resumePosition); // Trigger progress change event.
                    }
                }
            }
        }

        // Downloads the file from the HTTP response.
        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode(); // Ensure the response is successful.

            long? contentLength = response.Content.Headers.ContentLength; // Get content length.
            if (!contentLength.HasValue || contentLength.Value == 0)
                throw new Exception("Missing response header (Content-Length)"); // Throw exception if content length is missing.

            // Read content stream and process it.
            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(contentLength.Value, contentStream);
        }

        // Processes the content stream.
        private async Task ProcessContentStream(long currentDownloadSize, Stream contentStream)
        {
            var totalBytesRead = _resumePosition == -1 ? 0 : _resumePosition;
            var buffer = new byte[8192];
            var isMoreToRead = true;
            _lastProgressUpdate = DateTime.Now;
            string fullFilePath = Path.Combine(_destinationFolderPath, _fallbackFileName);

            // Create or open file stream for writing the downloaded content.
            using (var fileStream = new FileStream(fullFilePath, _resumePosition == -1 ? FileMode.Create : FileMode.Open, FileAccess.Write, FileShare.None, 8192, true))
            {
                if (_resumePosition != -1)
                    fileStream.Position = _resumePosition;

                // Read from content stream and write to file stream.
                do
                {
                    if (_cancelled)
                    {
                        if (_paused)
                        {
                            SaveProgress(fileStream, currentDownloadSize); // Save download progress if paused.
                            return;
                        }
                        DeleteIncompleteDownload(fullFilePath); // Delete incomplete download if cancelled.
                        return;
                    }

                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        SaveProgress(fileStream, currentDownloadSize); // Save progress if download is completed.
                        return;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead); // Write to file stream.

                    totalBytesRead += bytesRead;
                    // Trigger progress change event periodically.
                    if ((DateTime.Now - _lastProgressUpdate).TotalSeconds > 2)
                    {
                        TriggerProgressChanged(currentDownloadSize, totalBytesRead);
                        _lastProgressUpdate = DateTime.Now;
                    }
                } while (isMoreToRead);
            }
        }

        // Saves download progress.
        private void SaveProgress(FileStream fileStream, long currentDownloadSize)
        {
            long position = fileStream.Position;
            // Store download progress in preferences.
            Preferences.Set(AppConfigKey.DownloadProgress, $"{position};{(_preResumeSize == -1 ? currentDownloadSize : _preResumeSize)}", $"{_destinationFolderPath}\\gamevault-metadata");
            TriggerProgressChanged(currentDownloadSize, position); // Trigger progress change event.
            fileStream.Flush(); // Flush any pending writes to ensure data is written to disk.
            fileStream.Close(); // Close the file stream after all operations are done.
        }

        // Deletes incomplete download.
        private void DeleteIncompleteDownload(string filePath)
        {
            try
            {
                // Delete downloaded file and progress metadata.
                File.Delete(filePath);
                File.Delete(Path.Combine(_destinationFolderPath, "gamevault-metadata"));
            }
            catch { }
        }

        // Triggers progress change event.
        private void TriggerProgressChanged(long totalDownloadSize, long totalBytesRead)
        {
            double? progressPercentage = totalDownloadSize > 0 ? Math.Round((double)totalBytesRead / (_preResumeSize == -1 ? totalDownloadSize : _preResumeSize) * 100, 0) : null;
            ProgressChanged?.Invoke(_preResumeSize, totalDownloadSize, _resumePosition == -1 ? totalBytesRead : (totalBytesRead - _resumePosition), totalBytesRead, progressPercentage);
        }

        // Cancels the download process.
        public void Cancel()
        {
            if (_paused)
            {
                DeleteIncompleteDownload(Path.Combine(_destinationFolderPath, _fallbackFileName)); // Delete incomplete download if paused.
                return;
            }
            _cancelled = true; // Set download cancellation flag.
            Dispose(); // Dispose HttpClient.
        }

        // Pauses the download process.
        public void Pause()
        {
            _paused = true; // Set download pause flag.
            _cancelled = true; // Set download cancellation flag.
            Dispose(); // Dispose HttpClient.
        }

        // Disposes HttpClient.
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
