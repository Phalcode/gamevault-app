using gamevault.Helper.Integrations;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace gamevault.Helper
{

    public class GameTimeTracker
    {
        private Timer? m_Timer { get; set; }
        public async Task Start()
        {
            await SendOfflineProgess();
            await SettingsViewModel.Instance.InitIgnoreList();

            m_Timer = new Timer();
            m_Timer.AutoReset = true;
            m_Timer.Interval = 60000;
            m_Timer.Elapsed += TimerCallback;
            m_Timer.Start();
        }
        public void Stop()
        {
            m_Timer?.Stop();
        }
        private void TimerCallback(object sender, ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                //string installationPath = Path.Combine(SettingsViewModel.Instance.RootPath, "GameVault\\Installations");

                if (SettingsViewModel.Instance.RootDirectories.Count == 0)
                    return;

                List<string> allDirectoriesFromRootDirectories = new List<string>();
                foreach (DirectoryEntry dirEntry in SettingsViewModel.Instance.RootDirectories)
                {
                    if (Directory.Exists(Path.Combine(dirEntry.Uri, "GameVault", "Installations")))
                        allDirectoriesFromRootDirectories.AddRange(Directory.GetDirectories(Path.Combine(dirEntry.Uri, "GameVault", "Installations")));
                }
                Dictionary<int, string> foundGames = new Dictionary<int, string>();

                foreach (string dir in allDirectoriesFromRootDirectories)
                {
                    var dirInf = new DirectoryInfo(dir);
                    if (dirInf.GetFiles().Length != 0 || dirInf.GetDirectories().Length != 0)
                    {
                        int id = GetGameIdByDirectory(dir);
                        if (id == -1) continue;
                        if (foundGames.Where(x => x.Key == id).Count() > 0)
                            continue;
                        foundGames.Add(id, dir);
                    }
                }
                List<int> gamesToCountUp = new List<int>();
                var processes = Process.GetProcesses().Where(x => x.MainWindowHandle != IntPtr.Zero).ToArray();
                for (int x = 0; x < processes.Length; x++)
                {

                    for (int y = 0; y < foundGames.Count; y++)
                    {
                        try
                        {
                            if (processes[x].MainModule.FileName.Contains(foundGames.ElementAt(y).Value))
                            {
                                if (!ContainsValueFromIgnoreList(Path.GetFileNameWithoutExtension(processes[x].MainModule.FileName)) && !gamesToCountUp.Contains(foundGames.ElementAt(y).Key))
                                {
                                    gamesToCountUp.Add(foundGames.ElementAt(y).Key);
                                }
                            }
                        }
                        catch
                        {

                            string[] allExecutables = Directory.GetFiles(foundGames.ElementAt(y).Value, "*.EXE", SearchOption.AllDirectories);

                            foreach (string executable in allExecutables)
                            {
                                if (Path.GetFileNameWithoutExtension(executable) == processes[x].ProcessName)
                                {
                                    if (!ContainsValueFromIgnoreList(processes[x].ProcessName) && !gamesToCountUp.Contains(foundGames.ElementAt(y).Key))
                                    {
                                        gamesToCountUp.Add(foundGames.ElementAt(y).Key);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                if (LoginManager.Instance.IsLoggedIn())
                {
                    try
                    {
                        if (AnyOfflineProgressToSend())
                        {
                            await SendOfflineProgess();
                        }
                        foreach (int gameid in gamesToCountUp)
                        {
                            await WebHelper.PutAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameid}/increment", string.Empty);
                        }
                        DiscordHelper.Instance.SyncGameWithDiscordPresence(gamesToCountUp, foundGames);
                        await SaveGameHelper.Instance.BackupSaveGamesFromIds(gamesToCountUp);//Check which games are were closed and backup them
                    }
                    catch (Exception ex)
                    {
                        SaveToOfflineProgress(gamesToCountUp);
                    }
                }
                else
                {
                    SaveToOfflineProgress(gamesToCountUp);
                }
            });
        }
        private bool AnyOfflineProgressToSend()
        {
            try
            {
                return new FileInfo(LoginManager.Instance.GetUserProfile().OfflineProgress).Length > 0;
            }
            catch
            {
                return false;
            }
        }
        private void SaveToOfflineProgress(List<int> progress)
        {
            foreach (int gameid in progress)
            {
                try
                {
                    string timeString = Preferences.Get(gameid.ToString(), LoginManager.Instance.GetUserProfile().OfflineProgress, true);
                    int result = int.TryParse(timeString, out result) ? result : 0;
                    result++;
                    Preferences.Set(gameid.ToString(), result, LoginManager.Instance.GetUserProfile().OfflineProgress, true);
                }
                catch { }
            }
        }
        private async Task SendOfflineProgess()
        {
            if (LoginManager.Instance.IsLoggedIn())
            {
                foreach (string key in GetAllOfflineCacheKeys())
                {
                    try
                    {
                        string value = Preferences.Get(key, LoginManager.Instance.GetUserProfile().OfflineProgress, true);
                        int.Parse(value);
                        await WebHelper.PutAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{key}/increment/{value}", string.Empty);
                        Preferences.DeleteKey(key, LoginManager.Instance.GetUserProfile().OfflineProgress);
                    }
                    catch { }
                }
            }
        }
        private int GetGameIdByDirectory(string dir)
        {
            try
            {
                string dirName = dir.Substring(dir.LastIndexOf('\\'));
                string gameId = dirName.Substring(2, dirName.IndexOf(')') - 2);
                int id = int.Parse(gameId);
                return id;
            }
            catch { }
            return -1;
        }
        private string[] GetAllOfflineCacheKeys()
        {
            if (File.Exists(LoginManager.Instance.GetUserProfile().OfflineProgress))
            {
                List<string> keys = new List<string>();
                foreach (string line in File.ReadAllLines(LoginManager.Instance.GetUserProfile().OfflineProgress))
                {
                    try
                    {
                        string gameId = line.Substring(0, line.IndexOf('='));
                        int id = int.Parse(gameId);
                        keys.Add(gameId);
                    }
                    catch { }
                }
                return keys.ToArray();
            }
            return new string[0];
        }
        private bool ContainsValueFromIgnoreList(string value)
        {
            return SettingsViewModel.Instance.IgnoreList.Any(x => x.Contains(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
