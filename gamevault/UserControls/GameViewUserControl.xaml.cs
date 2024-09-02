using gamevault.Converter;
using gamevault.Helper;
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
            List<string> MediaUrls = new List<string>();
            if (YoutubeClient == null)
            {
                YoutubeClient = new YoutubeClient();
            }
            //Load first video separately, as it might take a while until the first playback
            bool trailerPreloaded = false;
            bool gameplayPreloaded = false;
            if (data?.Trailers?.Count() > 0)
            {
                trailerPreloaded = true;
                string preloaded = await ConvertYoutubeLinkToEmbedded(data?.Trailers[0]);
                MediaUrls.Add(preloaded);
                uiMediaSlider.LoadFirstElement(preloaded);
            }
            else if (data?.Gameplays?.Count() > 0)
            {
                gameplayPreloaded = true;
                string preloaded = await ConvertYoutubeLinkToEmbedded(data?.Gameplays[0]);
                MediaUrls.Add(preloaded);
                uiMediaSlider.LoadFirstElement(preloaded);
            }

            for (int i = 0; i < data?.Trailers?.Count(); i++)
            {
                if (i == 0 && trailerPreloaded)
                {
                    continue;//Prevent the first element from being reloaded
                }
                MediaUrls.Add(await ConvertYoutubeLinkToEmbedded(data?.Trailers[i]));
            }
            for (int i = 0; i < data?.Gameplays?.Count(); i++)
            {
                if (i == 0 && gameplayPreloaded)
                {
                    continue;//Prevent the first element from being reloaded
                }
                MediaUrls.Add(await ConvertYoutubeLinkToEmbedded(data?.Gameplays[i]));
            }
            for (int i = 0; i < data?.Screenshots?.Count(); i++)
            {
                MediaUrls.Add(data?.Screenshots[i]);
            }

            uiMediaSlider.SetMediaList(MediaUrls);
            if (trailerPreloaded == false && gameplayPreloaded == false)
            {
                uiMediaSlider.LoadFirstElement();
            }
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
                string result = await WebHelper.GetRequestAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameID}");
                ViewModel.Game = JsonSerializer.Deserialize<Game>(result);
                ViewModel.UserProgresses = ViewModel.Game.Progresses.Where(p => p.User.ID != LoginManager.Instance.GetCurrentUser().ID).ToArray();
                ViewModel.CurrentUserProgress = ViewModel.Game.Progresses.FirstOrDefault(progress => progress.User.ID == LoginManager.Instance.GetCurrentUser()?.ID) ?? new Progress { MinutesPlayed = 0, State = State.UNPLAYED.ToString() };
            }
            catch (Exception ex) { }
            ViewModel.IsInstalled = IsGameInstalled(ViewModel.Game);
            ViewModel.IsDownloaded = IsGameDownloaded(ViewModel.Game);
            ViewModel.ShowMappedTitle = Preferences.Get(AppConfigKey.ShowMappedTitle, AppFilePath.UserFile) == "1";
            this.IsEnabled = true;
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
                    await uiMediaSlider.InitVideoPlayer();
                    await PrepareMetadataMedia(ViewModel.Game.Metadata);
                }
                catch { }
                //###########
                PrepareMarkdownElements();
            }
            if (!this.IsVisible && loaded && !uiMediaSlider.IsWebViewNull())
            {
                await uiMediaSlider.SaveMediaVolume();//Set this to unload event, so it will dispose even if the main control changes
                uiMediaSlider.Dispose();
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

            uiMediaSlider.UnloadMediaSlider();
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
        #region Markdown
        private static MarkdownPipeline BuildPipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseSupportedExtensions()
                .Build();
        }
        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (Uri.IsWellFormedUriString(e.Parameter.ToString(), UriKind.Absolute))
            {
                Process.Start(new ProcessStartInfo(e.Parameter.ToString()) { UseShellExecute = true });
            }
        }
        private FlowDocument LoadMarkdown(string content)
        {
            var xaml = Markdig.Wpf.Markdown.ToXaml(content, BuildPipeline());
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                // Directly load the XAML without custom schema context
                if (XamlReader.Load(stream) is FlowDocument document)
                {
                    return document;
                }
                return null;
            }
        }
        private void PrepareMarkdownElements()
        {
            if (!string.IsNullOrWhiteSpace(ViewModel?.Game?.Metadata?.Description))
            {
                try
                {
                    ViewModel.DescriptionMarkdown = LoadMarkdown(ViewModel.Game.Metadata.Description);
                }
                catch { }
            }
            if (!string.IsNullOrWhiteSpace(ViewModel?.Game?.Metadata?.Notes))
            {
                try
                {
                    ViewModel.NotesMarkdown = LoadMarkdown(ViewModel.Game.Metadata.Notes);
                }
                catch { }
            }
        }
        #endregion
    }
}