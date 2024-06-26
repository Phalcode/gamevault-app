using gamevault.Helper;
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
            uiInstalledGames.IsExpanded = Preferences.Get(AppConfigKey.InstalledGamesOpen, AppFilePath.UserFile) == "1" ? true : false;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible == true && gamesRestored && InstallViewModel.Instance.InstalledGames.Count > 0 && string.IsNullOrEmpty(inputTimer?.Data))
            {
                InstallViewModel.Instance.InstalledGames = await SortInstalledGamesByLastPlayed(InstallViewModel.Instance.InstalledGames);
                InstallViewModel.Instance.InstalledGamesFilter = CollectionViewSource.GetDefaultView(InstallViewModel.Instance.InstalledGames);
            }
        }
        public async Task RestoreInstalledGames()
        {
            InstallViewModel.Instance.IgnoreList = GetIgnoreList();
            Dictionary<int, string> foundGames = new Dictionary<int, string>();
            Game[]? games = await Task<Game[]>.Run(() =>
            {
                string installationPath = Path.Combine(SettingsViewModel.Instance.RootPath, "GameVault\\Installations");
                if (SettingsViewModel.Instance.RootPath != string.Empty && Directory.Exists(installationPath))
                {
                    foreach (string dir in Directory.GetDirectories(installationPath))
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
                            if (InstallViewModel.Instance.InstalledGames.Where(x => x.Key.ID == id).Count() > 0)
                                continue;
                            if (!foundGames.ContainsKey(id))
                            {
                                foundGames.Add(id, dir);
                            }
                        }
                    }
                    try
                    {
                        if (foundGames.Count > 0)
                        {
                            string gameIds = string.Empty;
                            foreach (KeyValuePair<int, string> kv in foundGames)
                            {
                                if (gameIds == string.Empty)
                                {
                                    gameIds += kv.Key;
                                    continue;
                                }
                                gameIds += "," + kv.Key;
                            }
                            if (LoginManager.Instance.IsLoggedIn())
                            {
                                string gameList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games?filter.id=$in:{gameIds}");
                                return JsonSerializer.Deserialize<PaginatedData<Game>>(gameList).Data;
                            }
                            else
                            {
                                string[] seperatedIds = gameIds.Split(',');
                                List<Game> offlineCacheGames = new List<Game>();
                                foreach (string id in seperatedIds)
                                {
                                    string objectFromFile = Preferences.Get(id, AppFilePath.OfflineCache);
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
                                if (!Preferences.Exists(game.ID.ToString(), AppFilePath.OfflineCache))
                                {
                                    string gameToSave = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{game.ID}");
                                    await CacheHelper.CreateOfflineCacheAsync(JsonSerializer.Deserialize<Game>(gameToSave));
                                }
                            }
                        }
                    }
                    catch { }
                }
                InstallViewModel.Instance.InstalledGames = await SortInstalledGamesByLastPlayed(TempInstalledGames);
                InstallViewModel.Instance.InstalledGamesFilter = CollectionViewSource.GetDefaultView(InstallViewModel.Instance.InstalledGames);
            }
            gamesRestored = true;
            App.Instance.SetJumpListGames();
        }
        private async Task<ObservableCollection<KeyValuePair<Game, string>>> SortInstalledGamesByLastPlayed(ObservableCollection<KeyValuePair<Game, string>> collection)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string lastTimePlayed = Preferences.Get(AppConfigKey.LastPlayed, AppFilePath.UserFile);
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
                string lastTimePlayed = Preferences.Get(AppConfigKey.LastPlayed, AppFilePath.UserFile);
                if (lastTimePlayed.Contains($"{gameID}"))
                {
                    lastTimePlayed = lastTimePlayed.Replace($"{gameID};", "");
                }
                lastTimePlayed = lastTimePlayed.Insert(0, $"{gameID};");
                Preferences.Set(AppConfigKey.LastPlayed, lastTimePlayed, AppFilePath.UserFile);
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
        private void OnCreated(object sender, FileSystemEventArgs e)
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
                    string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{id}");
                    game = JsonSerializer.Deserialize<Game>(result);
                }
                else
                {
                    string compressedStringObject = Preferences.Get(id.ToString(), AppFilePath.OfflineCache);
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
        private string[]? GetIgnoreList()
        {
            try
            {
                string result = Preferences.Get("IL", AppFilePath.IgnoreList);
                return JsonSerializer.Deserialize<string[]>(result);
            }
            catch { return null; }
        }
        private void GameCard_Clicked(object sender, RoutedEventArgs e)
        {
            if (((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key == null)
                return;
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key, LoginManager.Instance.IsLoggedIn()));
        }
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputTimer.Stop();
            inputTimer.Data = ((TextBox)sender).Text;
            inputTimer.Start();
        }
        private void InputTimerElapsed(object sender, EventArgs e)
        {
            inputTimer.Stop();
            if (InstallViewModel.Instance.InstalledGamesFilter == null) return;
            InstallViewModel.Instance.InstalledGamesFilter.Filter = item =>
            {
                return ((KeyValuePair<Game, string>)item).Key.Title.Contains(inputTimer.Data, StringComparison.OrdinalIgnoreCase);
            };
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            PlayGame(((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key.ID);
        }

        public static void PlayGame(int gameId)
        {
            string path = "";
            KeyValuePair<Game, string> result = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == gameId).FirstOrDefault();
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
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MainWindowViewModel.Instance.OpenPopup(new GameSettingsUserControl(((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key) { Width = 1200, Height = 800, Margin = new Thickness(50) });
        }
        private void InitTimer()
        {
            inputTimer = new InputTimer();
            inputTimer.Interval = TimeSpan.FromMilliseconds(400);
            inputTimer.Tick += InputTimerElapsed;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (((ScrollViewer)sender).ComputedHorizontalScrollBarVisibility == Visibility.Visible)
            {
                e.Handled = true;
                if (e.Delta > 0)
                    ((ScrollViewer)sender).LineLeft();
                else
                    ((ScrollViewer)sender).LineRight();
            }
            else
            {
                e.Handled = true;
                ScrollViewer parent = VisualHelper.FindNextParentByType<ScrollViewer>((ScrollViewer)sender);
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                parent.RaiseEvent(eventArg);
            }
        }

        private void InstalledGames_Toggled(object sender, RoutedEventArgs e)
        {
            Preferences.Set(AppConfigKey.InstalledGamesOpen, uiInstalledGames.IsExpanded ? "1" : "0", AppFilePath.UserFile);
        }

        private void RestoreRows()
        {
            string result = Preferences.Get(AppConfigKey.InstalledGamesRows, AppFilePath.UserFile);
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
                Preferences.Set(AppConfigKey.InstalledGamesRows, uiRowsUpDown.Value, AppFilePath.UserFile);
            }
            catch { }
        }

        private void Collection_Updated(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Rows_ValueChanged(null, null);
        }
    }
}
