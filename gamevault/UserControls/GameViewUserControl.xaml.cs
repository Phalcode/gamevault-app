using gamevault.Converter;
using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using LiveChartsCore.Measure;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

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
        private void UnloadMediaSlider()
        {
            uiWebView.Visibility = Visibility.Hidden;
            uiWebView.CoreWebView2?.NavigateToString("<html><body></body></html>");
        }
        private void ReloadMediaSlider()
        {
            uiWebView.Visibility = Visibility.Visible;
            uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex]);
        }
        private void ReloadMediaSlider_Click(object sender, RoutedEventArgs e)
        {
            ReloadMediaSlider();
        }
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
            NextMedia_Click(null, null); //Show first element
        }
        private async Task<string> ConvertYoutubeLinkToEmbedded(string input)
        {
            if (input.Contains("youtube", StringComparison.OrdinalIgnoreCase))
            {
                var streamManifest = await YoutubeClient.Videos.Streams.GetManifestAsync(input);
                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                var streamUrl = streamInfo.Url;
                return streamUrl;
            }
            else
            {
                return input;
            }
        }

        private async Task ResizeMediaSlider()
        {
            string resizescript = @"
 var video = document.querySelector('video[name=""media""]');
if(video)
{
           ";


            resizescript += @" video.style.width = 16000 + 'px';
            video.style.height = 9000 + 'px';";


            resizescript += @"
            video.style.position = 'fixed'; // Ensure it is positioned to cover the viewport
            video.style.top = '0';
            video.style.left = '0';
            video.style.zIndex = '1000'; // Ensure it is on top of other elements
}
   ";
            await uiWebView.CoreWebView2.ExecuteScriptAsync(resizescript);
        }
        private async Task<string> GetCurrentMediaVolume()
        {
            string resizescript = @"document.querySelector('video[name=""media""]').volume;";
            string result = await uiWebView.CoreWebView2.ExecuteScriptAsync(resizescript);
            return result;
        }
        private string GetLastMediaVolume()
        {
            string result = Preferences.Get(AppConfigKey.MediaSliderVolume, AppFilePath.UserFile);
            return string.IsNullOrWhiteSpace(result) ? "0.0" : result;
        }
        private async Task SaveMediaVolume()
        {
            try
            {
                string result = await GetCurrentMediaVolume();
                if (!string.IsNullOrWhiteSpace(result) && result != "null")
                {
                    Preferences.Set(AppConfigKey.MediaSliderVolume, result, AppFilePath.UserFile);
                }
            }
            catch { }
        }
        private void InitVideoPlayer()
        {
            uiWebView.NavigationCompleted += async (s, e) =>
            {
                try
                {
                    string mutescript = @"var video = document.querySelector('video[name=""media""]');if(video){video.volume=" +
                    GetLastMediaVolume() +
                    ";}";

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
                    await uiWebView.CoreWebView2.ExecuteScriptAsync(mutescript);
                    await ResizeMediaSlider();
                    await uiWebView.CoreWebView2.ExecuteScriptAsync(cssscript);
                }
                catch { }
            };
        }
        private bool isMediaSliderFullscreen = false;
        private Grid webViewAnchor;
        private void ToggleFullscreen()
        {
            try
            {
                if (!isMediaSliderFullscreen)
                {
                    isMediaSliderFullscreen = true;
                    webViewAnchor = (Grid)uiWebViewControlHost.Parent;
                    webViewAnchor.Children.Remove(uiWebViewControlHost);
                    uiWebViewControlHost.Margin = new Thickness(15);
                    MainWindowViewModel.Instance.OpenPopup(uiWebViewControlHost);
                }
                else
                {
                    isMediaSliderFullscreen = false;
                    MainWindowViewModel.Instance.ClosePopup();
                    uiWebViewControlHost.Margin = new Thickness(0);
                    webViewAnchor.Children.Add(uiWebViewControlHost);
                }
            }
            catch (Exception ex)
            {
            }//Probably is the Visual not disconnected from its Parent
        }
        private void MediaSliderFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }
        private void MediaSliderFullscreen_Escape_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (isMediaSliderFullscreen && e.Key == Key.Escape)
            {
                ToggleFullscreen();
            }
        }
        private async Task MediaSliderNavigate(string url)
        {
            await SaveMediaVolume();
            uiWebView.CoreWebView2.Navigate(url);
        }
        private async void NextMedia_Click(object sender, RoutedEventArgs e)
        {
            if (MediaUrls.Count < 1)
                return;

            if (mediaIndex < MediaUrls.Count - 1)
            {
                mediaIndex++;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = 0;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
            }
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }
        private async void PrevMedia_Click(object sender, RoutedEventArgs e)
        {
            if (MediaUrls.Count < 1)
                return;

            if (mediaIndex > 0)
            {
                mediaIndex--;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = MediaUrls.Count - 1;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
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
                try
                {
                    var options = new CoreWebView2EnvironmentOptions
                    {
                        AdditionalBrowserArguments = "--disk-cache-size=1000000"
                    };                  
                    var env = await CoreWebView2Environment.CreateAsync(null, AppFilePath.WebConfigDir, options);
                    await uiWebView.EnsureCoreWebView2Async(env);
                    InitVideoPlayer();
                    await PrepareMetadataMedia(ViewModel.Game.Metadata);
                }
                catch { }
                //###########
            }
            if (!this.IsVisible && loaded)
            {
                await SaveMediaVolume();//Set this to unload event, so it will dispose even if the main control changes
                uiWebView.Dispose();
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
            Back();
        }
        private void KeyBindingEscape_OnExecuted(object sender, object e)
        {
            Back();
        }
        private void Back()
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

            UnloadMediaSlider();
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
