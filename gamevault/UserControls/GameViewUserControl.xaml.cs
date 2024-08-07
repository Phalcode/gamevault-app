using gamevault.Converter;
using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using LiveChartsCore.Measure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;
using YoutubeExplode;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for NewGameViewUserControl.xaml
    /// </summary>
    public partial class GameViewUserControl : System.Windows.Controls.UserControl
    {
        private GameViewViewModel ViewModel { get; set; }
        private int gameID { get; set; }
        private bool loaded = false;


        #region MediaSlider
        private List<string> MediaUrls = new List<string>();
        int mediaIndex = -1;
        private YoutubeClient YoutubeClient { get; set; }
        private async Task PrepareMetadataMedia(GameMetadata data)
        {
            YoutubeClient = new YoutubeClient();
            for (int i = 0; i < data?.Trailers?.Count(); i++)
            {
                MediaUrls.Add(await ConvertYoutubeLinkToEmbedded(data?.Trailers[i]));
            }
            for (int i = 0; i < data?.Gameplays?.Count(); i++)
            {
                MediaUrls.Add(await ConvertYoutubeLinkToEmbedded(data?.Gameplays[i]));
            }
            for (int i = 0; i < data?.Screenshots?.Count(); i++)
            {
                MediaUrls.Add(data?.Screenshots[i]);
            }
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }
        private async Task<string> ConvertYoutubeLinkToEmbedded(string input)
        {
            if (input.Contains("youtube", StringComparison.OrdinalIgnoreCase))
            {
                //string pattern = @"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})";
                //string embedFormat = "https://www.youtube.com/embed/{0}";
                //string embeddedLink = Regex.Replace(input, pattern, m => string.Format(embedFormat, m.Groups[1].Value));
                //return embeddedLink;
                var streamManifest = await YoutubeClient.Videos.Streams.GetManifestAsync(input);
                var streamInfo = streamManifest.GetMuxedStreams().First();
                var streamUrl = streamInfo.Url;
                return streamUrl;
            }
            else
            {
                return input;
            }
        }
        private async void NextMedia_Click(object sender, RoutedEventArgs e)
        {
            if (mediaIndex < MediaUrls.Count - 1)
            {
                mediaIndex++;
                uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = 0;
                uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex]);
            }
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }
        private async void PrevMedia_Click(object sender, RoutedEventArgs e)
        {
            if (mediaIndex > 0)
            {
                mediaIndex--;
                uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = MediaUrls.Count - 1;
                uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex]);
            }
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }
        #endregion
        public GameViewUserControl(Game game, bool reloadGameObject = true)
        {
            InitializeComponent();
            ViewModel = new GameViewViewModel();
            if (false == reloadGameObject)
            {
                ViewModel.Game = game;
                //try
                //{
                //    ViewModel.UserProgresses = ViewModel.Game.Progresses.Where(p => p.User.ID != LoginManager.Instance.GetCurrentUser().ID).ToArray();
                //    ViewModel.CurrentUserProgress = ViewModel.Game.Progresses.Where(progress => progress.User.ID == LoginManager.Instance.GetCurrentUser().ID).FirstOrDefault();
                //}
                //catch { }
            }
            gameID = game.ID;
            this.DataContext = ViewModel;
        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            if (!loaded)
            {
                loaded = true;
                if (ViewModel.Game == null)
                {
                    try
                    {
                        string result = await WebHelper.GetRequestAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameID}");
                        ViewModel.Game = JsonSerializer.Deserialize<Game>(result);
                        ViewModel.UserProgresses = ViewModel.Game.Progresses.Where(p => p.User.ID != LoginManager.Instance.GetCurrentUser().ID).ToArray();
                        ViewModel.CurrentUserProgress = ViewModel.Game.Progresses.FirstOrDefault(progress => progress.User.ID == LoginManager.Instance.GetCurrentUser()?.ID) ?? new Progress { MinutesPlayed = 0, State = State.UNPLAYED.ToString() };
                    }
                    catch (Exception ex) { }
                }
                ViewModel.IsInstalled = IsGameInstalled(ViewModel.Game);
                ViewModel.IsDownloaded = IsGameDownloaded(ViewModel.Game);
                ViewModel.ShowMappedTitle = Preferences.Get(AppConfigKey.ShowMappedTitle, AppFilePath.UserFile) == "1";
                //MediaSlider
                await PrepareMetadataMedia(ViewModel.Game.Metadata);
                await uiWebView.EnsureCoreWebView2Async(null);
                uiWebView.NavigationCompleted += async (s, e) =>
                {
                    try
                    {
                        string script = @"
var video = document.querySelector('video[name=""media""]');
if(video)
{
video.volume = 0.0;
}
if (video.requestFullscreen) {
                    video.requestFullscreen();
                } else if (video.msRequestFullscreen) { // IE/Edge
                    video.msRequestFullscreen();
                }
   ";
                        string cssscript = @"
        var style = document.createElement('style');
        style.type = 'text/css';
        var cssRules = `video::-webkit-media-controls-fullscreen-button {
            display: none !important;
        }
        video::-moz-media-controls-fullscreen-button {
            display: none !important;
        }
        video::-ms-media-controls-fullscreen-button {
            display: none !important;
        }`;
        style.appendChild(document.createTextNode(cssRules));
        document.head.appendChild(style);
    ";
                        await uiWebView.CoreWebView2.ExecuteScriptAsync(script);
                        await uiWebView.CoreWebView2.ExecuteScriptAsync(cssscript);
                    }
                    catch { }
                };

                //###########
            }
        }
        private bool IsGameInstalled(Game? game)
        {
            if (game == null)
                return false;
            KeyValuePair<Game, string> result = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == game.ID).FirstOrDefault();
            if (result.Equals(default(KeyValuePair<Game, string>)))
                return false;
            return true;
        }
        private bool IsGameDownloaded(Game? game)
        {
            if (game == null)
                return false;
            return DownloadsViewModel.Instance.DownloadedGames.Where(gameUC => gameUC.GetGameId() == game.ID).Count() > 0;
        }
        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
        }
        private void KeyBindingEscape_OnExecuted(object sender, object e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
        }
        private void GamePlay_Click(object sender, MouseButtonEventArgs e)
        {
            InstallUserControl.PlayGame(ViewModel.Game.ID);
        }
        private void GameSettings_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.Game == null)
                return;
            MainWindowViewModel.Instance.OpenPopup(new GameSettingsUserControl(ViewModel.Game) { Width = 1200, Height = 800, Margin = new Thickness(50) });
        }
        private async void GameDownload_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.Game == null)
                return;
            await MainWindowViewModel.Instance.Downloads.TryStartDownload(ViewModel.Game);
        }
        private void Website_Navigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                string url = e.Uri.OriginalString;
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }
        private async void GameState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 0 || !LoginManager.Instance.IsLoggedIn())
                return;
            if (e.AddedItems.Count > 0)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameID}", System.Text.Json.JsonSerializer.Serialize(new Progress() { State = ViewModel.CurrentUserProgress.State }));
                    }
                    catch (Exception ex)
                    {
                        string msg = WebExceptionHelper.TryGetServerMessage(ex);
                        MainWindowViewModel.Instance.AppBarText = msg;
                    }
                });
            }
        }
        private void ShowProgressUser_Click(object sender, MouseButtonEventArgs e)
        {
            Progress selectedProgress = ((FrameworkElement)sender).DataContext as Progress;
            if (selectedProgress != null)
            {
                MainWindowViewModel.Instance.Community.ShowUser(selectedProgress.User);
            }
        }
        public void RefreshGame(Game game)
        {
            ViewModel.Game = game;
        }
        private void GameTitle_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ViewModel.ShowMappedTitle = !ViewModel.ShowMappedTitle;
                Preferences.Set(AppConfigKey.ShowMappedTitle, ViewModel.ShowMappedTitle ? "1" : "0", AppFilePath.UserFile);
            }
            catch { }
        }


        private async void Bookmark_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag == "busy")
            {
                ((ToggleButton)sender).IsChecked = !((ToggleButton)sender).IsChecked;
                return;
            }
            ((FrameworkElement)sender).Tag = "busy";
            try
            {
                if ((bool)((ToggleButton)sender).IsChecked == false)
                {
                    await WebHelper.DeleteAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me/bookmark/{ViewModel.Game.ID}");
                    ViewModel.Game.BookmarkedUsers = new List<User>();
                }
                else
                {
                    await WebHelper.PostAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me/bookmark/{ViewModel.Game.ID}");
                    ViewModel.Game.BookmarkedUsers = new List<User> { LoginManager.Instance.GetCurrentUser()! };
                }
                MainWindowViewModel.Instance.Library.RefreshGame(ViewModel.Game);
            }
            catch (Exception ex)
            {
                string message = WebExceptionHelper.TryGetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = message;
            }
            ((FrameworkElement)sender).Tag = "";
        }

        private void Genre_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                GenreMetadata data = (GenreMetadata)((FrameworkElement)sender).DataContext;
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterGenreSelector.SetEntries(new Pill[] { new Pill() { ID = data.ID, Name = data.Name, ProviderDataId = data.ProviderDataId } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }

        private void Tag_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                TagMetadata data = (TagMetadata)((FrameworkElement)sender).DataContext;
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterPillSelector.SetEntries(new Pill[] { new Pill() { ID = data.ID, Name = data.Name, ProviderDataId = data.ProviderDataId } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }
        private void GameType_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterGameTypeSelector.SetEntries(new Pill[] { new Pill() { OriginName = ViewModel.Game.Type.ToString(), Name = (string)new EnumDescriptionConverter().Convert(ViewModel.Game.Type, null, null, null) } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }

        private void Share_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string shareLink = $"gamevault://show?gameid={ViewModel?.Game?.ID}";
                System.Windows.Clipboard.SetText(shareLink);
                MainWindowViewModel.Instance.AppBarText = "Sharelink copied to clipboard";
            }
            catch { }
        }
    }
}
