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
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

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
        internal async Task<bool> RestoreBackup(int gameId, string installationDir)
        {
            if (!LoginManager.Instance.IsLoggedIn() || !SettingsViewModel.Instance.CloudSaves || !SettingsViewModel.Instance.License.IsActive())
                return false;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    string installationId = GetGameInstallationId(installationDir);
                    string[] auth = WebHelper.GetCredentials();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth[0]}:{auth[1]}")));
                    client.DefaultRequestHeaders.Add("User-Agent", $"GameVault/{SettingsViewModel.Instance.Version}");
                    string url = @$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}";
                    using (HttpResponseMessage response = await client.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}", HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        string fileName = response.Content.Headers.ContentDisposition.FileName.Split('_')[1].Split('.')[0];
                        if (fileName != installationId)
                        {
                            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            Directory.CreateDirectory(tempFolder);
                            string archive = Path.Combine(tempFolder, "backup.zip");
                            using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(archive, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                await contentStream.CopyToAsync(fileStream);
                            }

                            await zipHelper.ExtractArchive(archive, tempFolder);
                            var mappingFile = Directory.GetFiles(tempFolder, "mapping.yaml", SearchOption.AllDirectories);
                            string extractFolder = "";
                            if (mappingFile.Length < 1)
                                throw new Exception("no savegame extracted");

                            extractFolder = Path.GetDirectoryName(Path.GetDirectoryName(mappingFile[0]));
                            PrepareConfigFile(installationDir, Path.Combine(AppFilePath.CloudSaveConfigDir, "config.yaml"));
                            Process process = new Process();
                            ProcessShepherd.Instance.AddProcess(process);
                            process.StartInfo = CreateProcessHeader();
                            process.StartInfo.Arguments = $"--config {AppFilePath.CloudSaveConfigDir} restore --force --path \"{extractFolder}\"";
                            process.Start();
                            process.WaitForExit();
                            ProcessShepherd.Instance.RemoveProcess(process);
                            Directory.Delete(tempFolder, true);
                            return true;
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
            return false;
        }
        private string GetGameInstallationId(string installationDir)
        {
            string metadataFile = Path.Combine(installationDir, "gamevault-exec");
            string installationId = Preferences.Get(AppConfigKey.InstallationId, metadataFile);
            if (string.IsNullOrWhiteSpace(installationId))
            {
                installationId = Guid.NewGuid().ToString();
                Preferences.Set(AppConfigKey.InstallationId, installationId, metadataFile);
            }
            return installationId;
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
                    MainWindowViewModel.Instance.AppBarText = "Uploading Savegame to the Server...";
                    bool success = await BackupSaveGame(removedId);
                    if (success)
                    {
                        MainWindowViewModel.Instance.AppBarText = "Successfully synchronized the cloud saves";
                    }
                    else
                    {
                        MainWindowViewModel.Instance.AppBarText = "Failed to upload your Savegame to the Server";
                    }
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
        internal async Task<bool> BackupSaveGame(int gameId)
        {
            if (!SettingsViewModel.Instance.License.IsActive())
                return false;

            var installedGame = InstallViewModel.Instance?.InstalledGames?.FirstOrDefault(g => g.Key?.ID == gameId);
            string gameMetadataTitle = installedGame?.Key?.Metadata?.Title ?? "";
            string installationDir = installedGame?.Value ?? "";
            if (gameMetadataTitle != "" && installationDir != "")
            {
                string title = await SearchForLudusaviGameTitle(gameMetadataTitle);
                if (string.IsNullOrEmpty(title))
                    return false;
                string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);
                PrepareConfigFile(installedGame?.Value!, Path.Combine(AppFilePath.CloudSaveConfigDir, "config.yaml"));
                await CreateBackup(title, tempFolder);
                string archive = Path.Combine(tempFolder, "backup.zip");
                if (Directory.GetFiles(tempFolder, "mapping.yaml", SearchOption.AllDirectories).Length == 0)
                {
                    Directory.Delete(tempFolder, true);
                    return false;
                }
                await zipHelper.PackArchive(tempFolder, archive);

                bool success = await UploadSavegame(archive, gameId, installationDir);
                Directory.Delete(tempFolder, true);
                return success;
            }
            return false;
        }
        public void PrepareConfigFile(string installationPath, string yamlPath)
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var redirects = new
            {
                redirects = new List<object>
            {
                new { kind = "bidirectional", source = userFolder, target = "G:\\gamevault\\currentuser" },
                new { kind = "bidirectional", source = installationPath, target = "G:\\gamevault\\installation" }
            }
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            string result = serializer.Serialize(redirects);
            File.WriteAllText(yamlPath, result);
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
        private async Task CreateBackup(string lunusaviTitle, string tempFolder)
        {
            await Task.Run(() =>
           {
               Process process = new Process();
               ProcessShepherd.Instance.AddProcess(process);
               process.StartInfo = CreateProcessHeader();
               process.StartInfo.Arguments = $"--config {AppFilePath.CloudSaveConfigDir} backup --force --format \"zip\" --path \"{tempFolder}\" \"{lunusaviTitle}\"";
               process.Start();
               process.WaitForExit();
               ProcessShepherd.Instance.RemoveProcess(process);

           });
        }
        private async Task<bool> UploadSavegame(string saveFilePath, int gameId, string installationDir)
        {
            try
            {
                string installationId = GetGameInstallationId(installationDir);
                using (MemoryStream memoryStream = await FileToMemoryStreamAsync(saveFilePath))
                {
                    await WebHelper.UploadFileAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}", memoryStream, "x.zip", new KeyValuePair<string, string>("X-Installation-Id", installationId));
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
