using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for NewGameViewUserControl.xaml
    /// </summary>
    public partial class NewGameViewUserControl : UserControl
    {
        private NewGameViewViewModel ViewModel { get; set; }
        private int gameID { get; set; }
        private bool loaded = false;
        public NewGameViewUserControl(Game game, bool reloadGameObject = true)
        {
            InitializeComponent();
            ViewModel = new NewGameViewViewModel();
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
                        ViewModel.Game = await Task<Game>.Run(() =>
                        {
                            string gameList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameID}");
                            return System.Text.Json.JsonSerializer.Deserialize<Game>(gameList);
                        });
                        ViewModel.Progress = await Task<Progress>.Run(() =>
                        {
                            string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameID}");
                            return System.Text.Json.JsonSerializer.Deserialize<Progress>(result);
                        });

                        ViewModel.UserProgress = await Task<Progress[]>.Run(() =>
                        {
                            string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/game/{gameID}");
                            return System.Text.Json.JsonSerializer.Deserialize<Progress[]>(result);
                        });
                    }
                    catch (Exception ex) { }
                }
            }
        }

        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
        }
        private void GamePlay_Click(object sender, MouseButtonEventArgs e)
        {
            string path = "";
            KeyValuePair<Game, string> result = NewInstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.ID).FirstOrDefault();
            if (!result.Equals(default(KeyValuePair<Game, string>)))
            {
                path = result.Value;
            }
            string savedExecutable = Preferences.Get(AppConfigKey.Executable, $"{path}\\gamevault-exec");
            string parameter = Preferences.Get(AppConfigKey.LaunchParameter, $"{path}\\gamevault-exec");
            if (savedExecutable == string.Empty)
            {
                MainWindowViewModel.Instance.AppBarText = $"No Executable set";
            }
            else if (File.Exists(savedExecutable))
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
            }
            else
            {
                MainWindowViewModel.Instance.AppBarText = $"Could not find Executable '{savedExecutable}'";
            }
        }
        private void GameSettings_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.OpenPopup(new GameSettingsUserControl(ViewModel.Game) { Width = 1200, Height = 800, Margin = new Thickness(50) });
        }
        private void GameDownload_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.Downloads.TryStartDownload(ViewModel.Game);
        }
        private void KeyBindingEscape_OnExecuted(object sender, object e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
        }

        private void Website_Navigate(object sender, RequestNavigateEventArgs e)
        {
            string url = e.Uri.OriginalString;
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            e.Handled = true;
        }

        private async void GameState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 0)
                return;

            if (e.AddedItems.Count > 0)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameID}", System.Text.Json.JsonSerializer.Serialize(new Progress() { State = ViewModel.Progress.State }));
                    }
                    catch (WebException webEx)
                    {
                        string msg = WebExceptionHelper.GetServerMessage(webEx);
                        if (msg == string.Empty)
                        {
                            msg = "Could not connect to server";
                        }
                        MainWindowViewModel.Instance.AppBarText = msg;
                    }
                    catch { }
                });
            }
        }
    }
}
