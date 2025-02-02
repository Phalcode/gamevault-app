using gamevault.Models;
using gamevault.UserControls;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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
        internal async Task BackupSaveGames(List<int> gameIds)
        {
            var removedIds = runningGameIds.Except(gameIds).ToList();

            foreach (var removedId in removedIds)
            {
                try
                {
                    string gameMetadataTitle = FindGameTitleFromInstalledGames(removedId);
                    if (gameMetadataTitle != "")
                    {
                        string title = await SearchForLudusaviGameTitle(gameMetadataTitle);
                        string tempFolder = await CreateBackup(title);
                        string archive = Directory.GetFiles(tempFolder, "*.zip", SearchOption.AllDirectories).First();
                        await UploadSavegame(archive, removedId);
                        Directory.Delete(tempFolder, true);
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
        private string FindGameTitleFromInstalledGames(int gameId)
        {
            string gameMetadataTitle = InstallViewModel.Instance?.InstalledGames?.FirstOrDefault(g => g.Key?.ID == gameId).Key?.Metadata?.Title ?? "";
            return gameMetadataTitle;
        }
        private async Task<string> SearchForLudusaviGameTitle(string title)
        {
            return await Task.Run<string>(() =>
            {
                Process process = new Process();
                ProcessShepherd.Instance.AddProcess(process);
                process.StartInfo = CreateProcessHeader();
                process.StartInfo.Arguments = $"find \"{title}\" --normalized --fuzzy --api";
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
                process.StartInfo.Arguments = $"backup --force --format \"zip\" \"{lunusaviTitle}\" --path \"{tempFolder}\"";
                process.StartInfo.RedirectStandardInput = true;
                process.Start();
                process.WaitForExit();
                ProcessShepherd.Instance.RemoveProcess(process);

                return tempFolder;
            });
        }
        private async Task UploadSavegame(string saveFilePath, int gameId)
        {
            using (MemoryStream memoryStream = await FileToMemoryStreamAsync(saveFilePath))
            {
                await WebHelper.UploadFileAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/savefiles/user/{LoginManager.Instance.GetCurrentUser()!.ID}/game/{gameId}", memoryStream, "x.zip", null);
            }
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
        private ProcessStartInfo CreateProcessHeader()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}Lib\\savegame\\ludusavi.exe";
            return info;
        }
    }
}
