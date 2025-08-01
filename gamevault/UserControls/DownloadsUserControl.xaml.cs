using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using gamevault.Helper.Integrations;
using AngleSharp.Io;
using System.IO;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for DownloadsUserControl.xaml
    /// </summary>
    public partial class DownloadsUserControl : UserControl
    {

        public DownloadsUserControl()
        {
            InitializeComponent();

            this.DataContext = DownloadsViewModel.Instance;
        }
        public async Task RestoreDownloadedGames()
        {
            Dictionary<Game, string>? games = await Task.Run<Dictionary<Game, string>?>(async () =>
             {

                 if (SettingsViewModel.Instance.RootDirectories.Count == 0)
                     return null;

                 List<string> allDirectoriesFromRootDirectories = new List<string>();
                 foreach (DirectoryEntry dirEntry in SettingsViewModel.Instance.RootDirectories)
                 {
                     if (Directory.Exists(Path.Combine(dirEntry.Uri, "GameVault", "Downloads")))
                         allDirectoriesFromRootDirectories.AddRange(Directory.GetDirectories(Path.Combine(dirEntry.Uri, "GameVault", "Downloads")));
                 }
                 Dictionary<int, string> foundPathsById = new Dictionary<int, string>();
                 foreach (string dir in allDirectoriesFromRootDirectories)
                 {
                     try
                     {
                         if (new DirectoryInfo(dir).GetFiles().Length == 0)
                             continue;

                         string dirName = dir.Substring(dir.LastIndexOf('\\'));
                         string gameId = dirName.Substring(2, dirName.IndexOf(')') - 2);

                         if (int.TryParse(gameId, out int id))
                             foundPathsById.Add(id, SettingsViewModel.Instance.RootDirectories
                            .Where(x => dir.Contains(x.Uri))
                            .OrderByDescending(x => x.Uri.Length)
                            .First().Uri);

                     }
                     catch { continue; }
                 }
                 if (foundPathsById.Count == 0)
                     return null;
                 try
                 {
                     if (LoginManager.Instance.IsLoggedIn())
                     {
                         string gameList = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games?filter.id=$in:{string.Join(',', foundPathsById.Keys)}");
                         Dictionary<Game, string> foundGames = new Dictionary<Game, string>();
                         foreach (Game game in JsonSerializer.Deserialize<PaginatedData<Game>>(gameList)?.Data)
                         {
                             if (foundPathsById.TryGetValue(game.ID, out string path))
                             {
                                 foundGames.Add(game, path);
                             }
                         }
                         return foundGames;
                     }
                     Dictionary<Game, string> offlineCacheGames = new Dictionary<Game, string>();
                     foreach (int id in foundPathsById.Keys)
                     {
                         string objectFromFile = Preferences.Get(id.ToString(), LoginManager.Instance.GetUserProfile().OfflineCache);
                         if (objectFromFile == string.Empty)
                             continue;

                         string decompressedObject = StringCompressor.DecompressString(objectFromFile);
                         Game? deserializedObject = JsonSerializer.Deserialize<Game>(decompressedObject);
                         if (deserializedObject != null)
                         {
                             if (foundPathsById.TryGetValue(deserializedObject.ID, out string path))
                             {
                                 offlineCacheGames.Add(deserializedObject, path);
                             }
                         }
                         return offlineCacheGames;
                     }
                 }
                 catch (FormatException exFormat)
                 {
                     MainWindowViewModel.Instance.AppBarText = "The offline cache is corrupted";
                 }
                 catch (Exception ex)
                 {
                     string webMsg = WebExceptionHelper.TryGetServerMessage(ex);
                     MainWindowViewModel.Instance.AppBarText = webMsg;
                 }
                 return null;
             });
            if (games == null)
                return;

            var validGameIds = new HashSet<int>(games.Keys.Select(g => g.ID));
            // Remove controls not in the dictionary, unless they're downloading
            for (int i = DownloadsViewModel.Instance.DownloadedGames.Count - 1; i >= 0; i--)
            {
                var control = DownloadsViewModel.Instance.DownloadedGames[i];
                int gameId = control.GetGameId();

                bool existsInDict = validGameIds.Contains(gameId);
                
                if (control.IsDownloading())
                {
                    control.PauseDownload();
                }
                if (!existsInDict)
                {
                    DownloadsViewModel.Instance.DownloadedGames.RemoveAt(i);
                }
            }
            var existingIds = new HashSet<int>(DownloadsViewModel.Instance.DownloadedGames.Select(c => c.GetGameId()));

            foreach (var game in games)
            {
                if (!existingIds.Contains(game.Key.ID))
                {
                    DownloadsViewModel.Instance.DownloadedGames.Add(new GameDownloadUserControl(game.Key, game.Value, false));
                }
            }
        }
        public void RefreshGame(Game game)
        {
            for (int i = 0; i < DownloadsViewModel.Instance.DownloadedGames.Count; i++)
            {
                if (DownloadsViewModel.Instance.DownloadedGames[i].GetGameId() == game.ID)
                {
                    DownloadsViewModel.Instance.DownloadedGames[i].Refresh(game);
                    return;
                }
            }
        }
        public void CancelAllDownloads()
        {
            foreach (var download in DownloadsViewModel.Instance.DownloadedGames)
            {
                download.CancelDownload();
            }
        }

        public async Task TryStartDownload(Game game)
        {
            if (SettingsViewModel.Instance.RootDirectories.Count == 0)
            {
                MainWindowViewModel.Instance.AppBarText = "No Root Directory configured! Go to ⚙️Settings->Data";
                return;
            }
            var installLocationPicker = new InstallLocationUserControl();
            MainWindowViewModel.Instance.OpenPopup(installLocationPicker);
            string selectedDirectory = await installLocationPicker.SelectInstallLocation();
            if (selectedDirectory == string.Empty)
                return;

            if (!Directory.Exists(selectedDirectory))
            {
                MainWindowViewModel.Instance.AppBarText = "Selected directory does not exist";
                return;
            }
            if (LoginManager.Instance.IsLoggedIn() == false)
            {
                MainWindowViewModel.Instance.AppBarText = "You are not logged in or offline";
                return;
            }
            if (IsAlreadyDownloading(game.ID))
            {
                MainWindowViewModel.Instance.AppBarText = $"'{game.Title}' is already in the download queue";
                return;
            }
            if (await IsAlreadyDownloaded(game.ID))
            {
                return;
            }
            if (IsEnoughDriveSpaceAvailable(Convert.ToInt64(game.Size), selectedDirectory))
            {
                GameDownloadUserControl? oldDownloadEntry = DownloadsViewModel.Instance.DownloadedGames.Where(g => g.GetGameId() == game.ID).FirstOrDefault();
                if (oldDownloadEntry != null)
                {
                    DownloadsViewModel.Instance.DownloadedGames.Remove(oldDownloadEntry);
                }
                DownloadsViewModel.Instance.DownloadedGames.Insert(0, new GameDownloadUserControl(game, selectedDirectory, true));
                MainWindowViewModel.Instance.AppBarText = $"'{game.Title}' has been added to the download queue";
            }
            else
            {
                FileInfo f = new FileInfo(selectedDirectory);
                string? driveName = Path.GetPathRoot(f.FullName);
                MainWindowViewModel.Instance.AppBarText = $"Not enough space available for drive {driveName}";
            }
        }
        private async Task<bool> IsAlreadyDownloaded(int id)
        {
            if (DownloadsViewModel.Instance.DownloadedGames.Where(gameUC => gameUC.GetGameId() == id).Count() > 0)
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"This game was already downloaded. Do you want to overwrite this file?",
                    "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        private bool IsAlreadyDownloading(int id)
        {
            if (DownloadsViewModel.Instance.DownloadedGames.Where(gameUC => gameUC.IsGameIdDownloading(id) == true).Count() > 0)
            {
                return true;
            }
            return false;
        }
        private bool IsEnoughDriveSpaceAvailable(long gameSize, string directory)
        {
            FileInfo f = new FileInfo(directory);
            string? driveName = Path.GetPathRoot(f.FullName);
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    if ((drive.AvailableFreeSpace - 1000) > gameSize)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        private async void DeleteAllDownloads_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to delete all canceled and completed downloads?\n\nThis cannot be undone.", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });

            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    for (int count = DownloadsViewModel.Instance.DownloadedGames.Count - 1; count >= 0; count--)
                    {
                        await DownloadsViewModel.Instance.DownloadedGames[count].DeleteFile(false);
                    }
                }
                catch { }
            }
        }
    }
}
