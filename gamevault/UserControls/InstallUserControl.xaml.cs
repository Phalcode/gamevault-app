using gamevault.Helper;
using gamevault.Helper.Integrations;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace gamevault.UserControls
{
    public partial class InstallUserControl : UserControl
    {
        private InputTimer? inputTimer { get; set; }
        private List<FileSystemWatcher> m_FileWatcherList = new List<FileSystemWatcher>();
        private bool gamesRestored = false;
        public InstallUserControl()
        {
            InitializeComponent();
            this.DataContext = InstallViewModel.Instance;
            InitTimer();
            uiInstalledGames.IsExpanded = Preferences.Get(AppConfigKey.InstalledGamesOpen, LoginManager.Instance.GetUserProfile().UserConfigFile) == "1" ? true : false;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible == true && gamesRestored && InstallViewModel.Instance.InstalledGames.Count > 0 && string.IsNullOrEmpty(inputTimer?.Data))
            {
                InstallViewModel.Instance.InstalledGames = await SortInstalledGamesByLastPlayed(InstallViewModel.Instance.InstalledGames);
                InstallViewModel.Instance.InstalledGamesFilter = CollectionViewSource.GetDefaultView(InstallViewModel.Instance.InstalledGames);
            }
        }
        public async Task RestoreInstalledGames(bool fromCLI = false)
        {
            if (gamesRestored)
            {
                InstallViewModel.Instance.InstalledGames.Clear();// Clear here because the double entry code won't allow to refresh
                //File watchers are already protected against double entries and dont care, if the same code runs again
            }
            Dictionary<int, string> foundGames = new Dictionary<int, string>();
            InstallViewModel.Instance.InstalledGamesDuplicates.Clear();
            Game[]? games = await Task<Game[]>.Run(async () =>
            {
                if (SettingsViewModel.Instance.RootDirectories.Count > 0)
                {


                    List<string> allDirectoriesFromRootDirectories = new List<string>();
                    foreach (DirectoryEntry dirEntry in SettingsViewModel.Instance.RootDirectories)
                    {
                        if (Directory.Exists(Path.Combine(dirEntry.Uri, "GameVault", "Installations")))
                            allDirectoriesFromRootDirectories.AddRange(Directory.GetDirectories(Path.Combine(dirEntry.Uri, "GameVault", "Installations")));
                    }


                    foreach (string dir in allDirectoriesFromRootDirectories)
                    {
                        var dirInf = new DirectoryInfo(dir);
                        if (dirInf.GetFiles().Length == 0 && dirInf.GetDirectories().Length == 0)
                        {
                            if (GetGameIdByDirectory(dir) == -1) continue;
                            AddSystemFileWatcher(dir);
                        }
                        else
                        {
                            int id = GetGameIdByDirectory(dir);
                            if (id == -1) continue;
                            //if (InstallViewModel.Instance.InstalledGames.Where(x => x.Key.ID == id).Count() > 0)
                            //    continue;
                            if (!foundGames.ContainsKey(id))
                            {
                                foundGames.Add(id, dir);
                            }
                            else
                            {
                                if (!InstallViewModel.Instance.InstalledGamesDuplicates.ContainsKey(id))
                                {
                                    InstallViewModel.Instance.InstalledGamesDuplicates.Add(id, dir);
                                }
                            }
                        }
                    }
                    try
                    {
                        if (foundGames.Count > 0)
                        {
                            string gameIds = string.Join(",", foundGames.Keys.Where(key => !string.IsNullOrEmpty(foundGames[key])));
                            if (LoginManager.Instance.IsLoggedIn())
                            {
                                string gameList = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games?filter.id=$in:{gameIds}&limit=-1");
                                return JsonSerializer.Deserialize<PaginatedData<Game>>(gameList).Data;
                            }
                            else
                            {
                                List<Game> offlineCacheGames = new List<Game>();
                                foreach (KeyValuePair<int, string> entry in foundGames)
                                {
                                    string objectFromFile = Preferences.Get(entry.Key.ToString(), LoginManager.Instance.GetUserProfile().OfflineCache);
                                    if (objectFromFile != string.Empty)
                                    {
                                        try
                                        {
                                            string decompressedObject = StringCompressor.DecompressString(objectFromFile);
                                            Game? deserializedObject = JsonSerializer.Deserialize<Game>(decompressedObject);
                                            if (deserializedObject != null)
                                            {
                                                offlineCacheGames.Add(deserializedObject);
                                            }
                                        }
                                        catch (FormatException exFormat) { }
                                    }
                                    else
                                    {
                                        string gameTitle = Path.GetFileName(entry.Value);
                                        offlineCacheGames.Add(new Game() { ID = entry.Key, Title = gameTitle.Substring(gameTitle.IndexOf(')') + 1) });
                                    }
                                }
                                return offlineCacheGames.ToArray();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
                        return null;
                    }
                }
                return null;
            });
            if (games != null)
            {
                ObservableCollection<KeyValuePair<Game, string>> TempInstalledGames = new ObservableCollection<KeyValuePair<Game, string>>();
                for (int count = 0; count < foundGames.Count; count++)
                {
                    try
                    {
                        Game? game = games.Where(x => x.ID == foundGames.ElementAt(count).Key).FirstOrDefault();
                        if (game != null)
                        {
                            TempInstalledGames.Add(new KeyValuePair<Game, string>(game, foundGames.ElementAt(count).Value));
                            if (LoginManager.Instance.IsLoggedIn())
                            {
                                if (!Preferences.Exists(game.ID.ToString(), LoginManager.Instance.GetUserProfile().OfflineCache))
                                {
                                    await CacheHelper.CreateOfflineCacheAsync(game);
                                }
                                else
                                {
                                    string offlineCacheGameString = Preferences.Get(game.ID.ToString(), LoginManager.Instance.GetUserProfile().OfflineCache);
                                    offlineCacheGameString = StringCompressor.DecompressString(offlineCacheGameString);
                                    Game offlineCacheGame = JsonSerializer.Deserialize<Game>(offlineCacheGameString);
                                    if (game.EntityVersion != offlineCacheGame?.EntityVersion)
                                    {
                                        await CacheHelper.CreateOfflineCacheAsync(game);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                InstallViewModel.Instance.InstalledGames = await SortInstalledGamesByLastPlayed(TempInstalledGames);
                InstallViewModel.Instance.InstalledGamesFilter = CollectionViewSource.GetDefaultView(InstallViewModel.Instance.InstalledGames);
                if (gamesRestored)
                {
                    SearchFilterInputTimerElapsed(null, null);//Keep the filter
                }
            }
            if (!fromCLI)
                gamesRestored = true;

            App.Instance.SetJumpListGames();
        }
        private async Task<ObservableCollection<KeyValuePair<Game, string>>> SortInstalledGamesByLastPlayed(ObservableCollection<KeyValuePair<Game, string>> collection)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string lastTimePlayed = Preferences.Get(AppConfigKey.LastPlayed, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    List<string> lastPlayedDates = lastTimePlayed.Split(';').ToList();

                    collection = new System.Collections.ObjectModel.ObservableCollection<KeyValuePair<Game, string>>(collection.OrderByDescending(item =>
                    {
                        int key = item.Key.ID;
                        return lastPlayedDates.Contains(key.ToString()) ? lastPlayedDates.IndexOf(key.ToString()) : int.MaxValue;
                    }).Reverse());
                }
                catch { }
                return collection;
            });
        }
        public void SetLastPlayedGame(int gameID)
        {
            try
            {
                string lastTimePlayed = Preferences.Get(AppConfigKey.LastPlayed, LoginManager.Instance.GetUserProfile().UserConfigFile);
                if (lastTimePlayed.Contains($"{gameID}"))
                {
                    lastTimePlayed = lastTimePlayed.Replace($"{gameID};", "");
                }
                lastTimePlayed = lastTimePlayed.Insert(0, $"{gameID};");
                Preferences.Set(AppConfigKey.LastPlayed, lastTimePlayed, LoginManager.Instance.GetUserProfile().UserConfigFile);
            }
            catch { }
        }

        public void AddSystemFileWatcher(string path)
        {
            if (m_FileWatcherList.Where(x => x.Path == path).Count() > 0)
                return;
            FileSystemWatcher watcher;
            watcher = new FileSystemWatcher();
            watcher.Path = path;
            //watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
            //                      | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.EnableRaisingEvents = true;
            //watcher.IncludeSubdirectories = true;
            m_FileWatcherList.Add(watcher);
        }
        private async void OnCreated(object sender, FileSystemEventArgs e)
        {
            string dir = ((FileSystemWatcher)sender).Path;
            ((FileSystemWatcher)sender).Created -= new FileSystemEventHandler(OnCreated);
            m_FileWatcherList.Remove((FileSystemWatcher)sender);
            int id = GetGameIdByDirectory(dir);
            if (id == -1)
                return;

            if (InstallViewModel.Instance.InstalledGames.Where(x => x.Key.ID == id).Count() > 0)
                return;

            try
            {
                Game? game = null;
                if (LoginManager.Instance.IsLoggedIn())
                {
                    string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{id}");
                    game = JsonSerializer.Deserialize<Game>(result);
                }
                else
                {
                    string compressedStringObject = Preferences.Get(id.ToString(), LoginManager.Instance.GetUserProfile().OfflineCache);
                    if (compressedStringObject != string.Empty)
                    {
                        string decompressedObject = StringCompressor.DecompressString(compressedStringObject);
                        Game? deserializedObject = JsonSerializer.Deserialize<Game>(decompressedObject);
                        game = deserializedObject;
                    }
                }

                if (game != null)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        InstallViewModel.Instance.InstalledGames.Insert(0, new KeyValuePair<Game, string>(game, dir));
                        SetLastPlayedGame(game.ID);
                    });
                }
            }
            catch { }
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
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                string url = e.Uri.OriginalString;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex) { MainWindowViewModel.Instance.AppBarText = ex.Message; }
        }
        private void GameCard_Clicked(object sender, RoutedEventArgs e)
        {
            if (((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key == null)
            {
                MainWindowViewModel.Instance.AppBarText = "Cannot open game";
                return;
            }
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key, LoginManager.Instance.IsLoggedIn()));
        }
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputTimer.Stop();
            inputTimer.Data = ((TextBox)sender).Text;
            inputTimer.Start();
        }
        private void SearchFilterInputTimerElapsed(object sender, EventArgs e)
        {
            inputTimer.Stop();
            if (InstallViewModel.Instance.InstalledGamesFilter == null) return;
            InstallViewModel.Instance.InstalledGamesFilter.Filter = item =>
            {
                return ((KeyValuePair<Game, string>)item).Key.Title.Contains(inputTimer.Data ?? "", StringComparison.OrdinalIgnoreCase);
            };
        }

        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ((FrameworkElement)sender).IsEnabled = false;
            await PlayGame(((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key.ID);
            ((FrameworkElement)sender).IsEnabled = true;
        }

        public static async Task PlayGame(int gameId)
        {

            string path = "";
            KeyValuePair<Game, string> result = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == gameId).FirstOrDefault();
            if (SettingsViewModel.Instance.CloudSaves)
            {
                MainWindowViewModel.Instance.AppBarText = $"Syncing cloud save...";
                await SaveGameHelper.Instance.RestoreBackup(gameId, result.Value);
            }

            if (!result.Equals(default(KeyValuePair<Game, string>)))
            {
                path = result.Value;
            }
            if (!Directory.Exists(path))
            {
                MainWindowViewModel.Instance.AppBarText = $"Can not find part of '{path}'";
                return;
            }
            string savedExecutable = Preferences.Get(AppConfigKey.Executable, $"{path}\\gamevault-exec");
            string parameter = Preferences.Get(AppConfigKey.LaunchParameter, $"{path}\\gamevault-exec");
            if (savedExecutable == string.Empty)
            {
                if (GameSettingsUserControl.TryPrepareLaunchExecutable(path))
                {
                    savedExecutable = Preferences.Get(AppConfigKey.Executable, $"{path}\\gamevault-exec");
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"No valid Executable found";
                    return;
                }
            }
            if (File.Exists(savedExecutable))
            {
                try
                {
                    ProcessHelper.StartApp(savedExecutable, parameter);
                }
                catch
                {

                    try
                    {
                        ProcessHelper.StartApp(savedExecutable, parameter, true);
                    }
                    catch
                    {
                        MainWindowViewModel.Instance.AppBarText = $"Can not execute '{savedExecutable}'";
                    }
                }
                MainWindowViewModel.Instance.Library.GetGameInstalls().SetLastPlayedGame(result.Key.ID);
            }
            else
            {
                MainWindowViewModel.Instance.AppBarText = $"Could not find Executable '{savedExecutable}'";
            }
        }
        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                int ID = ((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key.ID;
                string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{ID}");
                Game resultGame = JsonSerializer.Deserialize<Game>(result);
                MainWindowViewModel.Instance.OpenPopup(new GameSettingsUserControl(resultGame) { Width = 1200, Height = 800, Margin = new Thickness(50) });
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
            }
        }
        private void InitTimer()
        {
            inputTimer = new InputTimer();
            inputTimer.Interval = TimeSpan.FromMilliseconds(400);
            inputTimer.Tick += SearchFilterInputTimerElapsed;
        }

        private void InstalledGames_Toggled(object sender, RoutedEventArgs e)
        {
            Preferences.Set(AppConfigKey.InstalledGamesOpen, uiInstalledGames.IsExpanded ? "1" : "0", LoginManager.Instance.GetUserProfile().UserConfigFile);
        }

        private void RestoreRows()
        {
            string result = Preferences.Get(AppConfigKey.InstalledGamesRows, LoginManager.Instance.GetUserProfile().UserConfigFile);
            if (int.TryParse(result, out int rows) && rows > 0)
            {
                uiRowsUpDown.Value = rows;
            }
            else
            {
                uiRowsUpDown.Value = 1;
            }
        }
        private void Rows_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            try
            {
                if (InstallViewModel.Instance.InstalledGames.Count == 0)
                    return;
                if (uiRowsUpDown.Value == null)
                {
                    RestoreRows();
                    return;
                }
                int calcRows = (InstallViewModel.Instance.InstalledGames.Count / 10);
                if ((int)uiRowsUpDown.Value > calcRows)
                {

                    if (InstallViewModel.Instance.InstalledGames.Count % 10 > 0)
                    {
                        calcRows += 1;
                        InstallViewModel.Instance.Colums = 10;
                    }
                    else
                    {
                        InstallViewModel.Instance.Colums = 0;
                    }
                    InstallViewModel.Instance.Rows = calcRows;
                }
                else
                {
                    InstallViewModel.Instance.Rows = (int)uiRowsUpDown.Value;
                    InstallViewModel.Instance.Colums = 0;
                }
                Preferences.Set(AppConfigKey.InstalledGamesRows, uiRowsUpDown.Value, LoginManager.Instance.GetUserProfile().UserConfigFile);
            }
            catch { }
        }

        private async void Collection_Updated(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Rows_ValueChanged(null, null);
            if (e.OldValue != null && e.OldValue != e.NewValue && SettingsViewModel.Instance.SyncSteamShortcuts)//Make sure that a game has really been added or removed
            {
                await SteamHelper.SyncGamesWithSteamShortcuts(InstallViewModel.Instance.InstalledGames.ToDictionary(pair => pair.Key, pair => pair.Value));
            }
        }
    }
}
