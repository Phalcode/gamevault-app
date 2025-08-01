using gamevault.Converter;
using gamevault.Helper;
using gamevault.Helper.Integrations;
using gamevault.Models;
using gamevault.ViewModels;
using LiveChartsCore.Measure;
using Markdig;
using Markdig.Wpf;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using Windows.Gaming.Input;
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
        private YoutubeClient YoutubeClient { get; set; }
        private async Task PrepareMetadataMedia(GameMetadata data)
        {
            List<Tuple<string, string>> MediaUrls = new List<Tuple<string, string>>();
            if (YoutubeClient == null)
            {
                YoutubeClient = new YoutubeClient();
            }
            //Load first video separately, as it might take a while until the first playback
            bool trailerPreloaded = false;
            bool gameplayPreloaded = false;
            if (data?.Trailers?.Count() > 0)
            {
                var preloaded = await ConvertYoutubeLinkToEmbedded(data?.Trailers[0]);
                if (preloaded != null)
                {
                    trailerPreloaded = true;
                    MediaUrls.Add(preloaded);
                    await uiMediaSlider.LoadFirstElement(preloaded);
                }
            }
            else if (data?.Gameplays?.Count() > 0)
            {
                var preloaded = await ConvertYoutubeLinkToEmbedded(data?.Gameplays[0]);
                if (preloaded != null)
                {
                    gameplayPreloaded = true;
                    MediaUrls.Add(preloaded);
                    await uiMediaSlider.LoadFirstElement(preloaded);
                }
            }

            for (int i = 0; i < data?.Trailers?.Count(); i++)
            {
                if (i == 0 && trailerPreloaded)
                {
                    continue;//Prevent the first element from being reloaded
                }
                var url = await ConvertYoutubeLinkToEmbedded(data?.Trailers[i]);
                if (url != null)
                {
                    MediaUrls.Add(url);
                }
            }
            for (int i = 0; i < data?.Gameplays?.Count(); i++)
            {
                if (i == 0 && gameplayPreloaded)
                {
                    continue;//Prevent the first element from being reloaded
                }
                var url = await ConvertYoutubeLinkToEmbedded(data?.Gameplays[i]);
                if (url != null)
                {
                    MediaUrls.Add(url);
                }
            }
            for (int i = 0; i < data?.Screenshots?.Count(); i++)
            {
                MediaUrls.Add(new Tuple<string, string>(data?.Screenshots[i], ""));
            }

            uiMediaSlider.SetMediaList(MediaUrls);
            if (trailerPreloaded == false && gameplayPreloaded == false)
            {
                await uiMediaSlider.LoadFirstElement();
            }
        }
        private async Task<Tuple<string, string>> ConvertYoutubeLinkToEmbedded(string input)
        {
            try
            {
                if (input.Contains("youtu", StringComparison.OrdinalIgnoreCase))
                {
                    var streamManifest = await YoutubeClient.Videos.Streams.GetManifestAsync(input);
                    var videoStreamInfo = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();
                    var audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();
                    return new Tuple<string, string>(videoStreamInfo.Url, audioStreamInfo.Url);
                }
                else
                {
                    return new Tuple<string, string>(input, "");
                }
            }
            catch { return null; }
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
                        string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameID}");
                        ViewModel.Game = JsonSerializer.Deserialize<Game>(result);
                        ViewModel.UserProgresses = ViewModel.Game.Progresses.Where(p => p.User.ID != LoginManager.Instance.GetCurrentUser().ID).ToArray();
                        ViewModel.CurrentUserProgress = ViewModel.Game.Progresses.FirstOrDefault(progress => progress.User.ID == LoginManager.Instance.GetCurrentUser()?.ID) ?? new Progress { MinutesPlayed = 0, State = State.UNPLAYED.ToString() };
                    }
                    catch (Exception ex) { }
                }
                ViewModel.IsInstalled = IsGameInstalled(ViewModel.Game);
                ViewModel.IsDownloaded = IsGameDownloaded(ViewModel.Game);
                PrepareMarkdownElements();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        SaveGameHelper.Instance.PrepareConfigFile("", Path.Combine(LoginManager.Instance.GetUserProfile().CloudSaveConfigDir, "config.yaml"));
                        string gameMetadataTitle = ViewModel?.Game?.Metadata?.Title ?? "";
                        if (gameMetadataTitle == "")
                        {
                            gameMetadataTitle = ViewModel?.Game?.Title ?? "";
                        }
                        ViewModel.CloudSaveMatchTitle = await SaveGameHelper.Instance.SearchForLudusaviGameTitle(gameMetadataTitle);
                    }
                    catch { }
                });
                //MediaSlider
                try
                {
                    await uiMediaSlider.InitVideoPlayer();
                    await PrepareMetadataMedia(ViewModel?.Game?.Metadata);
                }
                catch { }
                //###########              
            }
            if (!this.IsVisible && loaded && !uiMediaSlider.IsWebViewNull())
            {
                //Set this to unload event, so it will dispose even if the main control changes
                uiMediaSlider.Dispose();
            }
        }
        private async void ReloadGameView_Click(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.F5)
                return;

            await ReloadGameView();
        }
        private async Task ReloadGameView()
        {
            this.IsEnabled = false;
            try
            {
                string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameID}");
                ViewModel.Game = JsonSerializer.Deserialize<Game>(result);
                ViewModel.UserProgresses = ViewModel.Game.Progresses.Where(p => p.User.ID != LoginManager.Instance.GetCurrentUser().ID).ToArray();
                ViewModel.CurrentUserProgress = ViewModel.Game.Progresses.FirstOrDefault(progress => progress.User.ID == LoginManager.Instance.GetCurrentUser()?.ID) ?? new Progress { MinutesPlayed = 0, State = State.UNPLAYED.ToString() };
            }
            catch (Exception ex) { }
            ViewModel.IsInstalled = IsGameInstalled(ViewModel.Game);
            ViewModel.IsDownloaded = IsGameDownloaded(ViewModel.Game);
            PrepareMarkdownElements();
            this.IsEnabled = true;
        }
        public void RefreshGame(Game game)
        {
            ViewModel.Game = game;
            PrepareMarkdownElements();
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
        private async void GamePlay_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            await InstallUserControl.PlayGame(ViewModel.Game.ID);
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private void GameSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Game == null)
                return;

            MainWindowViewModel.Instance.OpenPopup(new GameSettingsUserControl(ViewModel.Game) { Width = 1200, Height = 800, Margin = new Thickness(50) });
        }
        private async void GameDownload_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Game == null)
                return;

            if (IsGameDownloaded(ViewModel.Game))
            {
                uiMediaSlider.UnloadMediaSlider();
            }
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
                try
                {
                    await WebHelper.PutAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameID}", System.Text.Json.JsonSerializer.Serialize(new Progress() { State = ViewModel.CurrentUserProgress.State }));
                }
                catch (Exception ex)
                {
                    string msg = WebExceptionHelper.TryGetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
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
        private void GameTitle_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SettingsViewModel.Instance.ShowMappedTitle = !SettingsViewModel.Instance.ShowMappedTitle;
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
                    await WebHelper.PostAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me/bookmark/{ViewModel.Game.ID}", "");
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
                MainWindowViewModel.Instance.Library.uiFilterTagSelector.SetEntries(new Pill[] { new Pill() { ID = data.ID, Name = data.Name, ProviderDataId = data.ProviderDataId } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }
        private void Developer_Clicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DeveloperMetadata data = (DeveloperMetadata)((FrameworkElement)sender).DataContext;
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterDeveloperSelector.SetEntries(new Pill[] { new Pill() { ID = (int)data.ID!, Name = data.Name, ProviderDataId = data.ProviderDataId } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }
        private void Publisher_Clicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                PublisherMetadata data = (PublisherMetadata)((FrameworkElement)sender).DataContext;
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterPublisherSelector.SetEntries(new Pill[] { new Pill() { ID = (int)data.ID!, Name = data.Name, ProviderDataId = data.ProviderDataId } });
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
        private void Share_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string shareLink = $"gamevault://show?gameid={ViewModel?.Game?.ID}";
                System.Windows.Clipboard.SetText(shareLink);
                MainWindowViewModel.Instance.AppBarText = "Sharelink copied to clipboard";
            }
            catch { }
        }
        private async void BackupCloudSaves_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SettingsViewModel.Instance.License.IsActive())
                {
                    MainWindowViewModel.Instance.SetActiveControl(MainControl.Settings);
                    MainWindowViewModel.Instance.Settings.SetTabIndex(4);
                    return;
                }
                if (!LoginManager.Instance.IsLoggedIn())
                {
                    MainWindowViewModel.Instance.AppBarText = CloudSaveStatus.Offline;
                    return;
                }
                MainWindowViewModel.Instance.AppBarText = "Uploading Savegame to the Server...";
                ((FrameworkElement)sender).IsEnabled = false;
                string status = await SaveGameHelper.Instance.BackupSaveGame(ViewModel!.Game!.ID);
                MainWindowViewModel.Instance.AppBarText = status;
            }
            catch
            {
                MainWindowViewModel.Instance.AppBarText = CloudSaveStatus.BackupFailed;
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private async void RestoreCloudSaves_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SettingsViewModel.Instance.License.IsActive())
                {
                    MainWindowViewModel.Instance.SetActiveControl(MainControl.Settings);
                    MainWindowViewModel.Instance.Settings.SetTabIndex(4);
                    return;
                }
                MainWindowViewModel.Instance.AppBarText = $"Syncing cloud save...";
                ((FrameworkElement)sender).IsEnabled = false;
                string installationDir = InstallViewModel.Instance.InstalledGames.First(g => g.Key.ID == ViewModel!.Game!.ID).Value;
                string status = await SaveGameHelper.Instance.RestoreBackup(ViewModel!.Game!.ID, installationDir);
                MainWindowViewModel.Instance.AppBarText = status;
            }
            catch
            {
                MainWindowViewModel.Instance.AppBarText = CloudSaveStatus.RestoreFailed;
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        #region Markdown        
        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Uri.IsWellFormedUriString(e.Parameter.ToString(), UriKind.Absolute))
                {
                    Process.Start(new ProcessStartInfo(e.Parameter.ToString()) { UseShellExecute = true });
                }
            }
            catch { }
        }
        private void PrepareMarkdownElements()
        {
            try
            {
                if (ViewModel?.Game?.Metadata?.Description != null)
                {
                    ViewModel.DescriptionMarkdown = ViewModel.Game.Metadata.Description;
                }
            }
            catch { }
            try
            {
                if (ViewModel?.Game?.Metadata?.Notes != null)
                {
                    ViewModel.NotesMarkdown = ViewModel.Game.Metadata.Notes;
                }
            }
            catch { }
        }

        #endregion

    }
}