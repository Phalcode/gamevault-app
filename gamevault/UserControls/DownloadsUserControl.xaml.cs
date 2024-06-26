using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;

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
            Game[]? games = await Task<Game[]>.Run(() =>
             {
                 string downloadPath = $"{SettingsViewModel.Instance.RootPath}\\GameVault\\Downloads";
                 if (SettingsViewModel.Instance.RootPath == string.Empty || !Directory.Exists(downloadPath))
                     return null;

                 List<string> allIds = new List<string>();
                 foreach (string dir in Directory.GetDirectories(downloadPath))
                 {
                     try
                     {
                         if (new DirectoryInfo(dir).GetFiles().Length == 0)
                             continue;

                         string dirName = dir.Substring(dir.LastIndexOf('\\'));
                         string gameId = dirName.Substring(2, dirName.IndexOf(')') - 2);

                         if (int.TryParse(gameId, out int id))
                             allIds.Add(id.ToString());
                     }
                     catch { continue; }
                 }
                 if (allIds.Count == 0)
                     return null;
                 try
                 {
                     if (LoginManager.Instance.IsLoggedIn())
                     {
                         string gameList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games?filter.id=$in:{string.Join(',', allIds)}");
                         return JsonSerializer.Deserialize<PaginatedData<Game>>(gameList)?.Data;
                     }
                     List<Game> offlineCacheGames = new List<Game>();
                     foreach (string id in allIds)
                     {
                         string objectFromFile = Preferences.Get(id, AppFilePath.OfflineCache);
                         if (objectFromFile == string.Empty)
                             continue;

                         string decompressedObject = StringCompressor.DecompressString(objectFromFile);
                         Game? deserializedObject = JsonSerializer.Deserialize<Game>(decompressedObject);
                         if (deserializedObject != null)
                             offlineCacheGames.Add(deserializedObject);
                     }
                     return offlineCacheGames.ToArray();
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

            foreach (Game game in games)
            {
                DownloadsViewModel.Instance.DownloadedGames.Add(new GameDownloadUserControl(game, false));
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
            if (LoginManager.Instance.IsLoggedIn() == false)
            {
                MainWindowViewModel.Instance.AppBarText = "You are not logged in or offline";
                return;
            }
            if (SettingsViewModel.Instance.RootPath == string.Empty)
            {
                MainWindowViewModel.Instance.AppBarText = "Root path is not set! Go to ⚙️Settings->Data";
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
            if (IsEnoughDriveSpaceAvailable(Convert.ToInt64(game.Size)))
            {
                GameDownloadUserControl? oldDownloadEntry = DownloadsViewModel.Instance.DownloadedGames.Where(g => g.GetGameId() == game.ID).FirstOrDefault();
                if (oldDownloadEntry != null)
                {
                    DownloadsViewModel.Instance.DownloadedGames.Remove(oldDownloadEntry);
                }
                DownloadsViewModel.Instance.DownloadedGames.Insert(0, new GameDownloadUserControl(game, true));
                MainWindowViewModel.Instance.AppBarText = $"'{game.Title}' has been added to the download queue";
            }
            else
            {
                FileInfo f = new FileInfo(SettingsViewModel.Instance.RootPath);
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
        private bool IsEnoughDriveSpaceAvailable(long gameSize)
        {
            FileInfo f = new FileInfo(SettingsViewModel.Instance.RootPath);
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

        private async void DeleteAllDownloads_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
