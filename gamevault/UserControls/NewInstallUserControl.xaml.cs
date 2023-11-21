using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Composition;

namespace gamevault.UserControls
{
    public partial class NewInstallUserControl : UserControl
    {
        private InputTimer inputTimer { get; set; }
        private List<FileSystemWatcher> m_FileWatcherList = new List<FileSystemWatcher>();
        private string[]? m_IgnoreList { get; set; }
        public NewInstallUserControl()
        {
            InitializeComponent();
            this.DataContext = NewInstallViewModel.Instance;
            InitTimer();
            uiInstalledGames.IsExpanded = Preferences.Get(AppConfigKey.InstalledGamesOpen, AppFilePath.UserFile) == "1" ? true : false;
        }
        public async Task RestoreInstalledGames()
        {
            m_IgnoreList = GetIgnoreList();
            Dictionary<int, string> foundGames = new Dictionary<int, string>();
            Game[]? games = await Task<Game[]>.Run(() =>
            {
                string installationPath = $"{SettingsViewModel.Instance.RootPath}\\GameVault\\Installations";
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
                            if (NewInstallViewModel.Instance.InstalledGames.Where(x => x.Key.ID == id).Count() > 0)
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
                                        string decompressedObject = StringCompressor.DecompressString(objectFromFile);
                                        Game? deserializedObject = JsonSerializer.Deserialize<Game>(decompressedObject);
                                        if (deserializedObject != null)
                                        {
                                            offlineCacheGames.Add(deserializedObject);
                                        }
                                    }
                                }
                                return offlineCacheGames.ToArray();
                            }
                        }
                    }
                    catch (WebException exWeb)
                    {
                        MainWindowViewModel.Instance.AppBarText = "Could not connect to server";
                        return null;
                    }
                    catch (JsonException exJson)
                    {
                        MainWindowViewModel.Instance.AppBarText = exJson.Message;
                        return null;
                    }
                    catch (FormatException exFormat)
                    {
                        MainWindowViewModel.Instance.AppBarText = "The offline cache is corrupted";
                    }
                }
                return null;
            });
            if (games != null)
            {
                for (int count = 0; count < foundGames.Count; count++)
                {
                    try
                    {
                        Game? game = games.Where(x => x.ID == foundGames.ElementAt(count).Key).FirstOrDefault();
                        if (game != null)
                        {
                            NewInstallViewModel.Instance.InstalledGames.Add(new KeyValuePair<Game, string>(game, foundGames.ElementAt(count).Value));
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
            }
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

            if (NewInstallViewModel.Instance.InstalledGames.Where(x => x.Key.ID == id).Count() > 0)
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
                        NewInstallViewModel.Instance.InstalledGames.Add(new KeyValuePair<Game, string>(game, dir));
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
            string url = e.Uri.OriginalString;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            e.Handled = true;
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

        private void GameCard_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(((KeyValuePair<Game, string>)((FrameworkElement)sender).DataContext).Key));
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
            if (NewInstallViewModel.Instance.InstalledGamesOrigin == null)
            {
                NewInstallViewModel.Instance.InstalledGamesOrigin = NewInstallViewModel.Instance.InstalledGames;
            }
            NewInstallViewModel.Instance.InstalledGames = new System.Collections.ObjectModel.ObservableCollection<KeyValuePair<Game, string>>(NewInstallViewModel.Instance.InstalledGamesOrigin.Where(i => i.Key.Title.Contains(inputTimer.Data, StringComparison.OrdinalIgnoreCase)));
        }

        private void Play_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MainWindowViewModel.Instance.AppBarText = "PLAY";
        }

        private void Settings_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            //MainWindowViewModel.Instance.AppBarText = "Settings";
            uiGameSettingsPopup.IsOpen = true;
        }
        private void InitTimer()
        {
            inputTimer = new InputTimer();
            inputTimer.Interval = TimeSpan.FromMilliseconds(400);
            inputTimer.Tick += InputTimerElapsed;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            ScrollViewer parent = FindParentScrollViewer((ScrollViewer)sender);
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            parent.RaiseEvent(eventArg);
        }
        public ScrollViewer FindParentScrollViewer(DependencyObject child)
        {
            DependencyObject parentDepObj = child;
            do
            {
                parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
                ScrollViewer parent = parentDepObj as ScrollViewer;
                if (parent != null) return parent;
            }
            while (parentDepObj != null);
            return null;
        }

        private void InstalledGames_Toggled(object sender, RoutedEventArgs e)
        {
            Preferences.Set(AppConfigKey.InstalledGamesOpen, uiInstalledGames.IsExpanded ? "1" : "0", AppFilePath.UserFile);
        }
    }
}
