using gamevault.Models;
using gamevault.UserControls;
using gamevault.ViewModels;
using gamevault.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Gaming.Input;
using Windows.Gaming.Preview.GamesEnumeration;

namespace gamevault.Helper.Integrations
{
    internal class SaveGameHelper
    {
        #region Singleton
        private static SaveGameHelper instance = null;
        private static readonly object padlock = new object();

        public static SaveGameHelper Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new SaveGameHelper();
                    }
                    return instance;
                }
            }
        }
        #endregion

        private class SaveGameEntry
        {
            [JsonPropertyName("score")]
            public double Score { get; set; }
        }
        private List<int> runningGameIds = new List<int>();
        private SevenZipHelper zipHelper;
        internal SaveGameHelper()
        {
            zipHelper = new SevenZipHelper();
        }
        internal async Task RestoreBackup(int gameId)
        {
            if (!LoginManager.Instance.IsLoggedIn() || !SettingsViewModel.Instance.CloudSaves || !SettingsViewModel.Instance.License.IsActive())
                return;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    string deviceId = GetDeviceId();
                    string[] auth = WebHelper.GetCredentials();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth[0]}:{auth[1]}")));
                    client.DefaultRequestHeaders.Add("User-Agent", $"GameVault/{SettingsViewModel.Instance.Version}");
                    string url = @$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}";
                    using (HttpResponseMessage response = await client.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}", HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        string fileName = response.Content.Headers.ContentDisposition.FileName.Split('_')[1].Split('.')[0];
                        if (fileName != deviceId)
                        {
                            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            Directory.CreateDirectory(tempFolder);
                            string archive = Path.Combine(tempFolder, "backup.zip");
                            using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(archive, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                await contentStream.CopyToAsync(fileStream);
                            }

                            await zipHelper.ExtractArchive(archive, tempFolder);

                            Process process = new Process();
                            ProcessShepherd.Instance.AddProcess(process);
                            process.StartInfo = CreateProcessHeader();
                            process.StartInfo.Arguments = $"restore --force --path \"{tempFolder}\"";
                            process.Start();
                            process.WaitForExit();
                            ProcessShepherd.Instance.RemoveProcess(process);
                            Directory.Delete(tempFolder, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string statusCode = WebExceptionHelper.GetServerStatusCode(ex);
                    if (statusCode == "405")
                    {
                        MainWindowViewModel.Instance.AppBarText = "Cloud Saves are not enabled on this Server.";
                    }
                    else if (statusCode != "404")
                    {
                        MainWindowViewModel.Instance.AppBarText = "Failed to restore cloud save";
                    }
                }
            }
        }
        private string GetDeviceId()
        {
            string deviceId = Preferences.Get(AppConfigKey.DeviceId, AppFilePath.UserFile);
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                Preferences.Set(AppConfigKey.DeviceId, deviceId, AppFilePath.UserFile);
            }
            return deviceId;
        }
        internal async Task BackupSaveGamesFromIds(List<int> gameIds)
        {
            var removedIds = runningGameIds.Except(gameIds).ToList();

            foreach (var removedId in removedIds)
            {
                if (!LoginManager.Instance.IsLoggedIn() || !SettingsViewModel.Instance.CloudSaves)
                {
                    break;
                }
                try
                {
                    await BackupSaveGame(removedId);
                }
                catch (Exception ex)
                {
                }
            }

            // Find IDs that are new and add them to the list
            var newIds = gameIds.Except(runningGameIds).ToList();
            runningGameIds.AddRange(newIds);

            // Remove IDs that are no longer in the new list
            runningGameIds = runningGameIds.Intersect(gameIds).ToList();
        }
        internal async Task BackupSaveGame(int gameId)
        {
            if (!SettingsViewModel.Instance.License.IsActive())
                return;

            string gameMetadataTitle = FindGameTitleFromInstalledGames(gameId);
            if (gameMetadataTitle != "")
            {
                string title = await SearchForLudusaviGameTitle(gameMetadataTitle);
                string tempFolder = await CreateBackup(title);
                string archive = Path.Combine(tempFolder, "backup.zip");
                if (Directory.GetFiles(tempFolder, "mapping.yaml", SearchOption.AllDirectories).Length == 0)
                {
                    Directory.Delete(tempFolder, true);
                    return;
                }
                await zipHelper.PackArchive(tempFolder, archive);

                bool success = await UploadSavegame(archive, gameId);
                Directory.Delete(tempFolder, true);
                if (!success)
                {
                    MainWindowViewModel.Instance.AppBarText = "Failed to sync your savegame to the cloud.";
                }
            }
        }
        private string FindGameTitleFromInstalledGames(int gameId)
        {
            string gameMetadataTitle = InstallViewModel.Instance?.InstalledGames?.FirstOrDefault(g => g.Key?.ID == gameId).Key?.Metadata?.Title ?? "";
            return gameMetadataTitle;
        }
        internal async Task<string> SearchForLudusaviGameTitle(string title)
        {
            return await Task.Run<string>(() =>
            {
                Process process = new Process();
                ProcessShepherd.Instance.AddProcess(process);
                process.StartInfo = CreateProcessHeader(true);
                process.StartInfo.Arguments = $"find \"{title}\" --fuzzy --api";//--normalized
                process.EnableRaisingEvents = true;

                List<string> output = new List<string>();

                process.ErrorDataReceived += (sender, e) =>
                {
                    // Debug.WriteLine("ERROR:" + e.Data);
                };
                process.OutputDataReceived += (sender, e) =>
                {
                    output.Add(e.Data);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                ProcessShepherd.Instance.RemoveProcess(process);
                string jsonString = string.Join("", output).Trim();
                var entries = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, SaveGameEntry>>>(jsonString);
                if (entries?.Count > 0 && entries.Values.Count > 0 && entries.Values.First().Values.Count > 0 && entries.Values.First().Values.First().Score > 0.9d)//Make sure Score is set and over 0.9
                {
                    string lunusaviTitle = entries.Values.First().Keys.First();
                    return lunusaviTitle;

                }
                return "";
            });
        }
        private async Task<string> CreateBackup(string lunusaviTitle)
        {
            return await Task.Run<string>(() =>
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                Process process = new Process();
                ProcessShepherd.Instance.AddProcess(process);
                process.StartInfo = CreateProcessHeader();
                process.StartInfo.Arguments = $"backup --force --format \"zip\" --path \"{tempFolder}\" \"{lunusaviTitle}\"";                             
                process.Start();
                process.WaitForExit();
                ProcessShepherd.Instance.RemoveProcess(process);
                return tempFolder;
            });
        }
        private async Task<bool> UploadSavegame(string saveFilePath, int gameId)
        {
            try
            {
                string devideId = GetDeviceId();
                using (MemoryStream memoryStream = await FileToMemoryStreamAsync(saveFilePath))
                {
                    await WebHelper.UploadFileAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}", memoryStream, "x.zip", new KeyValuePair<string, string>("X-Device-Id", devideId));
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        private async Task<MemoryStream> FileToMemoryStreamAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            MemoryStream memoryStream = new MemoryStream();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0; // Reset position to beginning
            return memoryStream;
        }
        private ProcessStartInfo CreateProcessHeader(bool redirectConsole = false)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = redirectConsole;
            info.RedirectStandardError = redirectConsole;
            info.UseShellExecute = false;
            info.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}Lib\\savegame\\ludusavi.exe";
            return info;
        }
    }
}
