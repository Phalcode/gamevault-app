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
        private string[]? m_IgnoreList { get; set; }
        public async Task Start()
        {
            await SendOfflineProcess();
            await GetIgnoreList();

            m_Timer = new Timer();
            m_Timer.AutoReset = true;
            m_Timer.Interval = 60000;
            m_Timer.Elapsed += TimerCallback;
            m_Timer.Start();
        }
        private void TimerCallback(object sender, ElapsedEventArgs e)
        {
            Task.Run(() =>
            {               
                string installationPath = Path.Combine(SettingsViewModel.Instance.RootPath, "GameVault\\Installations");             

                if (!Directory.Exists(installationPath))
                    return;

                Dictionary<int, string> foundGames = new Dictionary<int, string>();

                foreach (string dir in Directory.GetDirectories(installationPath))
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
                if (true == LoginManager.Instance.IsLoggedIn())
                {
                    try
                    {
                        foreach (int gameid in gamesToCountUp)
                        {
                            WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameid}/increment", string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoginManager.Instance.SwitchToOfflineMode();
                    }
                }
                else
                {
                    foreach (int gameid in gamesToCountUp)
                    {
                        try
                        {
                            string timeString = Preferences.Get(gameid.ToString(), AppFilePath.OfflineProgress, true);
                            int result = int.TryParse(timeString, out result) ? result : 0;
                            result++;
                            Preferences.Set(gameid.ToString(), result, AppFilePath.OfflineProgress, true);
                        }
                        catch { }
                    }
                }
            });
        }
        private async Task SendOfflineProcess()
        {
            await Task.Run(() =>
             {
                 if (true == LoginManager.Instance.IsLoggedIn())
                 {
                     foreach (string key in GetAllOfflineCacheKeys())
                     {
                         try
                         {
                             string value = Preferences.Get(key, AppFilePath.OfflineProgress, true);
                             int.Parse(value);
                             WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{key}/increment/{value}", string.Empty);
                             Preferences.DeleteKey(key, AppFilePath.OfflineProgress);
                         }
                         catch { }
                     }
                 }
             });
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
            if (File.Exists(AppFilePath.OfflineProgress))
            {
                List<string> keys = new List<string>();
                foreach (string line in File.ReadAllLines(AppFilePath.OfflineProgress))
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
        private async Task GetIgnoreList()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(AppFilePath.IgnoreList))
                    {
                        string response = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/ignorefile");
                        m_IgnoreList = JsonSerializer.Deserialize<string[]>(response);
                        Preferences.Set("IL", response.Replace("\n", ""), AppFilePath.IgnoreList);
                    }
                    else
                    {
                        string result = Preferences.Get("IL", AppFilePath.IgnoreList);
                        m_IgnoreList = JsonSerializer.Deserialize<string[]>(result);
                    }
                }
                catch
                {
                    try
                    {
                        string result = Preferences.Get("IL", AppFilePath.IgnoreList);
                        m_IgnoreList = JsonSerializer.Deserialize<string[]>(result);
                    }
                    catch { }
                }
                if (m_IgnoreList == null)
                {
                    m_IgnoreList = new string[0];
                }
            });
        }
        private bool ContainsValueFromIgnoreList(string value)
        {
            return m_IgnoreList.Any(x => x.Contains(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
