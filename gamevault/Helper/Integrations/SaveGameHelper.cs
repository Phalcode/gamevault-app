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
using ImageMagick.Drawing;

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
        internal async Task<string> RestoreBackup(int gameId, string installationDir)
        {
            if (!LoginManager.Instance.IsLoggedIn() || !SettingsViewModel.Instance.License.IsActive())
                return CloudSaveStatus.RestoreFailed;

            if (!SettingsViewModel.Instance.CloudSaves)
                return CloudSaveStatus.SettingDisabled;

            try
            {

                string installationId = GetGameInstallationId(installationDir);
                string[] auth = WebHelper.GetCredentials();

                string url = @$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}";
                using (HttpResponseMessage response = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}", HttpCompletionOption.ResponseHeadersRead))
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
                        PrepareConfigFile(installationDir, Path.Combine(LoginManager.Instance.GetUserProfile().CloudSaveConfigDir, "config.yaml"));
                        Process process = new Process();
                        ProcessShepherd.Instance.AddProcess(process);
                        process.StartInfo = CreateProcessHeader();
                        process.StartInfo.ArgumentList.Add("--config");
                        process.StartInfo.ArgumentList.Add(LoginManager.Instance.GetUserProfile().CloudSaveConfigDir);
                        process.StartInfo.ArgumentList.Add("restore");
                        process.StartInfo.ArgumentList.Add("--force");
                        process.StartInfo.ArgumentList.Add("--path");
                        process.StartInfo.ArgumentList.Add(extractFolder);
                        process.Start();
                        process.WaitForExit();
                        ProcessShepherd.Instance.RemoveProcess(process);
                        Directory.Delete(tempFolder, true);
                        return CloudSaveStatus.RestoreSuccess;
                    }
                    else
                    {
                        return CloudSaveStatus.UpToDate;
                    }
                }
            }
            catch (Exception ex)
            {
                string statusCode = WebExceptionHelper.GetServerStatusCode(ex);
                if (statusCode == "405")
                {
                    MainWindowViewModel.Instance.AppBarText = CloudSaveStatus.ServerSettingDisabled;
                }
                else if (statusCode != "404")
                {
                    MainWindowViewModel.Instance.AppBarText = CloudSaveStatus.RestoreFailed;
                }
            }

            return CloudSaveStatus.RestoreFailed;
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
                if (!SettingsViewModel.Instance.CloudSaves || !SettingsViewModel.Instance.License.IsActive())
                {
                    break;
                }
                if (!LoginManager.Instance.IsLoggedIn())
                {
                    MainWindowViewModel.Instance.AppBarText = CloudSaveStatus.Offline;
                    break;
                }
                try
                {
                    MainWindowViewModel.Instance.AppBarText = "Uploading Savegame to the Server...";
                    string status = await BackupSaveGame(removedId);
                    MainWindowViewModel.Instance.AppBarText = status;
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = CloudSaveStatus.BackupFailed;
                }
            }

            // Find IDs that are new and add them to the list
            var newIds = gameIds.Except(runningGameIds).ToList();
            runningGameIds.AddRange(newIds);

            // Remove IDs that are no longer in the new list
            runningGameIds = runningGameIds.Intersect(gameIds).ToList();
        }
        internal async Task<string> BackupSaveGame(int gameId)
        {
            if (!SettingsViewModel.Instance.CloudSaves)
                return CloudSaveStatus.SettingDisabled;

            if (!SettingsViewModel.Instance.License.IsActive())
                return CloudSaveStatus.BackupFailed;

            var installedGame = InstallViewModel.Instance?.InstalledGames?.FirstOrDefault(g => g.Key?.ID == gameId);
            string gameMetadataTitle = installedGame?.Key?.Metadata?.Title ?? "";
            if (gameMetadataTitle == "")
            {
                gameMetadataTitle = installedGame?.Key?.Title ?? "";
            }
            string installationDir = installedGame?.Value ?? "";
            if (gameMetadataTitle != "" && installationDir != "")
            {
                PrepareConfigFile(installedGame?.Value!, Path.Combine(LoginManager.Instance.GetUserProfile().CloudSaveConfigDir, "config.yaml"));
                string title = await SearchForLudusaviGameTitle(gameMetadataTitle);
                if (string.IsNullOrEmpty(title))
                    return CloudSaveStatus.BackupFailed;

                string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);
                await CreateBackup(title, tempFolder);
                string archive = Path.Combine(tempFolder, "backup.zip");
                if (Directory.GetFiles(tempFolder, "mapping.yaml", SearchOption.AllDirectories).Length == 0)
                {
                    Directory.Delete(tempFolder, true);
                    return CloudSaveStatus.BackupCreationFailed;
                }
                await zipHelper.PackArchive(tempFolder, archive);

                bool success = await UploadSavegame(archive, gameId, installationDir);
                Directory.Delete(tempFolder, true);
                return success ? CloudSaveStatus.BackupSuccess : CloudSaveStatus.BackupUploadFailed;
            }
            return CloudSaveStatus.BackupFailed;
        }
        public void PrepareConfigFile(string installationPath, string yamlPath)
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Base configuration with redirects (always included)
            var redirects = new List<Dictionary<string, object>>
    {
        new Dictionary<string, object>
        {
            { "kind", "bidirectional" },
            { "source", userFolder },
            { "target", "G:\\gamevault\\currentuser" }
        },
        new Dictionary<string, object>
        {
            { "kind", "bidirectional" },
            { "source", installationPath },
            { "target", "G:\\gamevault\\installation" }
        }
    };



            var roots = new List<Dictionary<string, object>>();
            foreach (DirectoryEntry rootPath in SettingsViewModel.Instance.RootDirectories)
            {
                roots.Add(new Dictionary<string, object>
        {
            { "store", "other" },
            { "path", Path.Combine(rootPath.Uri,"GameVault","Installations") }
        });
            }

            // Start with base configuration (redirects and roots always included)
            var yamlData = new Dictionary<string, object>
    {
        { "redirects", redirects },
        { "roots", roots }
    };

            // Add manifest section if custom manifests exist (optional)
            var customLudusaviManifests = SettingsViewModel.Instance.CustomCloudSaveManifests.Where(m => !string.IsNullOrWhiteSpace(m.Uri));

            if (customLudusaviManifests.Any())
            {
                var manifest = new Dictionary<string, object>
        {
            { "enable", SettingsViewModel.Instance.UsePrimaryCloudSaveManifest },
            { "secondary", new List<Dictionary<string, object>>() }
        };

                foreach (DirectoryEntry entry in customLudusaviManifests)
                {
                    ((List<Dictionary<string, object>>)manifest["secondary"]).Add(new Dictionary<string, object>
            {
                { Uri.IsWellFormedUriString(entry.Uri, UriKind.Absolute) ? "url" : "path", entry.Uri },
                { "enable", true }
            });
                }

                yamlData.Add("manifest", manifest);
            }

            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            string result = serializer.Serialize(yamlData);
            File.WriteAllText(yamlPath, result);
        }

        internal async Task<string> SearchForLudusaviGameTitle(string title)
        {
            return await Task.Run<string>(() =>
            {
                Process process = new Process();
                ProcessShepherd.Instance.AddProcess(process);
                process.StartInfo = CreateProcessHeader(true);
                //process.StartInfo.Arguments = $"find \"{title}\" --fuzzy --api";//--normalized
                process.StartInfo.ArgumentList.Add("--config");
                process.StartInfo.ArgumentList.Add(LoginManager.Instance.GetUserProfile().CloudSaveConfigDir);
                process.StartInfo.ArgumentList.Add("find");
                process.StartInfo.ArgumentList.Add(title);
                process.StartInfo.ArgumentList.Add("--fuzzy");
                process.StartInfo.ArgumentList.Add("--api");
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
               //process.StartInfo.Arguments = $"--config {LoginManager.Instance.GetUserProfile().CloudSaveConfigDir} backup --force --format \"zip\" --path \"{tempFolder}\" \"{lunusaviTitle}\"";
               process.StartInfo.ArgumentList.Add("--config");
               process.StartInfo.ArgumentList.Add(LoginManager.Instance.GetUserProfile().CloudSaveConfigDir);
               process.StartInfo.ArgumentList.Add("backup");
               process.StartInfo.ArgumentList.Add("--force");
               process.StartInfo.ArgumentList.Add("--format");
               process.StartInfo.ArgumentList.Add("zip");
               process.StartInfo.ArgumentList.Add("--path");
               process.StartInfo.ArgumentList.Add(tempFolder);
               process.StartInfo.ArgumentList.Add(lunusaviTitle);

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
                    await WebHelper.UploadFileAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}", memoryStream, "x.zip", new RequestHeader[] { new RequestHeader() { Name = "X-Installation-Id", Value = installationId } });
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
    public struct CloudSaveStatus
    {
        public static string BackupSuccess = "Successfully synchronized the cloud saves";
        public static string BackupFailed = "Something went wrong during the Backup";
        public static string BackupCreationFailed = "Failed to create a copy of your Savegame";
        public static string BackupUploadFailed = "Failed to upload your Savegame to the Server";

        public static string RestoreSuccess = "Successfully synchronized the cloud save";
        public static string RestoreFailed = "Failed to restore the Savegame";
        public static string UpToDate = "Your Savegame is up to date";

        public static string SettingDisabled = "Activate Cloud Saves under Settings -> GameVault+ -> Cloud Saves";
        public static string ServerSettingDisabled = "Cloud Saves are not enabled on this Server";
        public static string Offline = "Can not synchronize the cloud saves, because you are offline";
    }
    public class DirectoryEntry
    {
        public string Uri { get; set; }
    }
}
