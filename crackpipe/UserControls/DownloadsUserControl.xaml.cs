using crackpipe.Helper;
using crackpipe.Models;
using crackpipe.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace crackpipe.UserControls
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
                 string downloadPath = $"{SettingsViewModel.Instance.RootPath}\\Crackpipe\\Downloads";
                 if (SettingsViewModel.Instance.RootPath == string.Empty || !Directory.Exists(downloadPath))
                     return null;

                 List<string> allIds = new List<string>();
                 foreach (string dir in Directory.GetDirectories(downloadPath))
                 {
                     if (new DirectoryInfo(dir).GetFiles().Length == 0)
                         continue;

                     string dirName = dir.Substring(dir.LastIndexOf('\\'));
                     string gameId = dirName.Substring(2, dirName.IndexOf(')') - 2);

                     if (int.TryParse(gameId, out int id))
                         allIds.Add(id.ToString());
                 }
                 if (allIds.Count == 0)
                     return null;
                 try
                 {
                     if (LoginManager.Instance.IsLoggedIn())
                     {
                         string gameList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/games?filter.id=$in:{string.Join(',', allIds)}");
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
                 catch (WebException exWeb)
                 {
                     string webMsg = WebExceptionHelper.GetServerMessage(exWeb);
                     if (webMsg == string.Empty) webMsg = "Could not connect to server";
                     MainWindowViewModel.Instance.AppBarText = webMsg;
                 }
                 catch (JsonException exJson)
                 {
                     MainWindowViewModel.Instance.AppBarText = exJson.Message;
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
    }
}
