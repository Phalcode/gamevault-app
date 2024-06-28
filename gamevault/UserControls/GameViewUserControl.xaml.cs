using gamevault.Converter;
using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using HarfBuzzSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for NewGameViewUserControl.xaml
    /// </summary>
    public partial class GameViewUserControl : UserControl
    {
        private GameViewViewModel ViewModel { get; set; }
        private int gameID { get; set; }
        private bool loaded = false;
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
                        ViewModel.Game = await Task<Game>.Run(() =>
                        {
                            string game = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameID}");
                            return System.Text.Json.JsonSerializer.Deserialize<Game>(game);
                        });
                        ViewModel.UserProgresses = ViewModel.Game.Progresses.Where(p => p.User.ID != LoginManager.Instance.GetCurrentUser().ID).ToArray();
                        ViewModel.CurrentUserProgress = ViewModel.Game.Progresses.FirstOrDefault(progress => progress.User.ID == LoginManager.Instance.GetCurrentUser()?.ID) ?? new Progress { MinutesPlayed = 0, State = State.UNPLAYED.ToString() };
                    }
                    catch (Exception ex) { }
                }
                ViewModel.IsInstalled = IsGameInstalled(ViewModel.Game);
                ViewModel.IsDownloaded = IsGameDownloaded(ViewModel.Game);
                ViewModel.ShowRawgTitle = Preferences.Get(AppConfigKey.ShowRawgTitle, AppFilePath.UserFile) == "1";
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
        private void KeyBindingEscape_OnExecuted(object sender, object e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
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
                ViewModel.ShowRawgTitle = !ViewModel.ShowRawgTitle;
                Preferences.Set(AppConfigKey.ShowRawgTitle, ViewModel.ShowRawgTitle ? "1" : "0", AppFilePath.UserFile);
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
                    ViewModel.Game.BookmarkedUsers = new User[0];
                }
                else
                {
                    await WebHelper.PostAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me/bookmark/{ViewModel.Game.ID}");
                    ViewModel.Game.BookmarkedUsers = new User[] { LoginManager.Instance.GetCurrentUser() };
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
                Genre data = (Genre)((FrameworkElement)sender).DataContext;
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterGenreSelector.SetEntries(new Genre_Tag[] { new Genre_Tag() { ID = data.ID, Name = data.Name, RawgId = data.RawgId } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }

        private void Tag_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Models.Tag data = (Models.Tag)((FrameworkElement)sender).DataContext;
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterTagSelector.SetEntries(new Genre_Tag[] { new Genre_Tag() { ID = data.ID, Name = data.Name, RawgId = data.RawgId } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }
        private void GameType_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindowViewModel.Instance.Library.ClearAllFilters();
                MainWindowViewModel.Instance.Library.uiFilterGameTypeSelector.SetEntries(new Genre_Tag[] { new Genre_Tag() { OriginName = ViewModel.Game.Type.ToString(), Name = (string)new EnumDescriptionConverter().Convert(ViewModel.Game.Type, null, null, null) } });
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            catch { }
        }

        private void Share_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string shareLink = $"gamevault://show?gameid={ViewModel?.Game?.ID}";
                Clipboard.SetText(shareLink);
                MainWindowViewModel.Instance.AppBarText = "Sharelink copied to clipboard";
            }
            catch { }
        }
    }
}
