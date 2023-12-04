using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Windows.ApplicationModel.Background;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.Collections;
using System.Drawing.Imaging;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Net;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for GameSettingsUserControl.xaml
    /// </summary>
    public partial class GameSettingsUserControl : UserControl
    {
        private bool startup = true;
        private GameSettingsViewModel ViewModel { get; set; }
        private string SavedExecutable { get; set; }
        internal GameSettingsUserControl(KeyValuePair<Game, string> gameParam)
        {
            InitializeComponent();
            ViewModel = new GameSettingsViewModel();
            ViewModel.Game = gameParam.Key;
            ViewModel.Directory = gameParam.Value;
            this.DataContext = ViewModel;
            if (Directory.Exists(gameParam.Value))
            {
                FindGameExecutables(ViewModel.Directory, true);
                if (Directory.Exists(ViewModel.Directory))
                {
                    ViewModel.LaunchParameter = Preferences.Get(AppConfigKey.LaunchParameter, $"{ViewModel.Directory}\\gamevault-exec");
                }
                InitDiscUsagePieChart();
            }
        }
        private void SettingsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TabControl)sender).SelectedIndex == -1)
                return;

            if (sender == uiSettingsHeadersLocal)
            {
                uiSettingsHeadersRemote.SelectedIndex = -1;
                uiSettingsContent.SelectedIndex = uiSettingsHeadersLocal.SelectedIndex;
            }
            else if (sender == uiSettingsHeadersRemote)
            {
                if (startup && ViewModel.Directory != "")
                {
                    startup = false;
                    uiSettingsHeadersRemote.SelectedIndex = -1;
                }
                else
                {
                    uiSettingsHeadersLocal.SelectedIndex = -1;
                    uiSettingsContent.SelectedIndex = uiSettingsHeadersRemote.SelectedIndex + uiSettingsHeadersLocal.Items.Count;
                }
            }
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }
        #region INSTALLATION
        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ViewModel.Directory))
                Process.Start("explorer.exe", ViewModel.Directory);
        }

        private async void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            if (ViewModel.Game.Type == GameType.WINDOWS_PORTABLE)
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to uninstall '{ViewModel.Game.Title}' ?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    try
                    {
                        if (Directory.Exists(ViewModel.Directory))
                            Directory.Delete(ViewModel.Directory, true);

                        NewInstallViewModel.Instance.InstalledGames.Remove(NewInstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.ID).First());
                    }
                    catch
                    {
                        MainWindowViewModel.Instance.AppBarText = "Something went wrong when deleting the files. Maybe they are opened by another process.";
                    }
                }
            }
            else if (ViewModel.Game.Type == GameType.WINDOWS_SETUP)
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to uninstall '{ViewModel.Game.Title}' ?\nAs this is a Windows setup, you will need to select an uninstall executable", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    using (var dialog = new System.Windows.Forms.OpenFileDialog())
                    {
                        dialog.InitialDirectory = ViewModel.Directory;
                        dialog.Filter = "uninstall|*.exe";
                        System.Windows.Forms.DialogResult fileResult = dialog.ShowDialog();
                        if (fileResult == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                        {
                            Process uninstProcess = null;
                            try
                            {
                                uninstProcess = ProcessHelper.StartApp(dialog.FileName);
                            }
                            catch
                            {

                                try
                                {
                                    uninstProcess = ProcessHelper.StartApp(dialog.FileName, "", true);
                                }
                                catch
                                {
                                    MainWindowViewModel.Instance.AppBarText = $"Can not execute '{dialog.FileName}'";
                                }
                            }
                            if (uninstProcess != null)
                            {
                                await uninstProcess.WaitForExitAsync();
                                try
                                {
                                    if (Directory.Exists(ViewModel.Directory))
                                        Directory.Delete(ViewModel.Directory, true);
                                }
                                catch { }
                                NewInstallViewModel.Instance.InstalledGames.Remove(NewInstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.ID).First());
                            }
                        }
                    }
                }
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private void InitDiscUsagePieChart()
        {
            long allGameSizes = 0;
            foreach (var installedGame in NewInstallViewModel.Instance.InstalledGames)
            {
                long.TryParse(installedGame.Key.Size, out long size);
                allGameSizes += size;
            }
            long.TryParse(ViewModel.Game.Size, out long currentGameSize);
            allGameSizes = allGameSizes - currentGameSize;
            double percentageOfAllGames = (currentGameSize * 100.0) / allGameSizes;
            uiTxtAllInstalledGamesSize.Text = allGameSizes.ToString();
            uiDiscUsagePieChart.Series = new ISeries[] {
                new PieSeries<double> {Values = new double[] { (double)currentGameSize } },
                new PieSeries<double> {Values = new double[] {(double)allGameSizes}}
                };
        }
        #endregion
        #region LAUNCH OPTIONS
        private void FindGameExecutables(string directory, bool checkForSavedExecutable)
        {
            string lastSelected = "";
            if (uiCbExecutables.SelectedItem != null)
            {
                lastSelected = ((KeyValuePair<string, string>)uiCbExecutables.SelectedItem).Key;
            }
            ViewModel.Executables.Clear();
            if (true == checkForSavedExecutable)
            {
                SavedExecutable = Preferences.Get(AppConfigKey.Executable, $"{ViewModel.Directory}\\gamevault-exec");
            }

            List<string> allExecutables = new List<string>();
            foreach (string entry in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                string fileType = Path.GetExtension(entry).TrimStart('.');
                if (Globals.SupportedExecutables.Contains(fileType.ToUpper()))
                {
                    allExecutables.Add(entry);
                }
            }
            for (int count = 0; count < allExecutables.Count; count++)
            {
                if (ContainsValueFromIgnoreList(allExecutables[count]))
                    continue;
                var currentItem = new KeyValuePair<string, string>(allExecutables[count], allExecutables[count].Substring(ViewModel.Directory.Length + 1));
                ViewModel.Executables.Add(currentItem);
                if (true == checkForSavedExecutable && allExecutables[count] == SavedExecutable)
                {
                    uiCbExecutables.SelectedItem = currentItem;
                }
                else if (true == checkForSavedExecutable && SavedExecutable == string.Empty)
                {
                    checkForSavedExecutable = false;
                    uiCbExecutables.SelectedItem = currentItem;
                }
                else if (lastSelected != string.Empty)
                {
                    var result = ViewModel.Executables.Where(e => e.Key == lastSelected).FirstOrDefault();
                    if (result.Key != null)
                    {
                        uiCbExecutables.SelectedItem = result;
                    }
                }
            }
        }
        private bool ContainsValueFromIgnoreList(string value)
        {
            return (NewInstallViewModel.Instance.IgnoreList != null && NewInstallViewModel.Instance.IgnoreList.Any(s => Path.GetFileNameWithoutExtension(value).Contains(s, StringComparison.OrdinalIgnoreCase)));
        }
        private void ExecutableSelection_Opened(object sender, EventArgs e)
        {
            FindGameExecutables(ViewModel.Directory, false);
        }
        private void Executable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SavedExecutable = ((KeyValuePair<string, string>)e.AddedItems[0]).Key;
                if (Directory.Exists(ViewModel.Directory))
                {
                    Preferences.Set(AppConfigKey.Executable, SavedExecutable, $"{ViewModel.Directory}\\gamevault-exec");
                }
            }
        }

        private async void CreateDesktopShortcut_Click(object sender, EventArgs e)
        {
            if (!File.Exists(SavedExecutable))
            {
                MainWindowViewModel.Instance.AppBarText = "No valid Executable set";
                return;
            }
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Do you want to create a desktop shortcut for the current selected executable?", "",
                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string shortcutPath = desktopDir + @"\\" + Path.GetFileNameWithoutExtension(SavedExecutable) + ".url";

                    using (StreamWriter writer = new StreamWriter(shortcutPath))
                    {
                        writer.Write("[InternetShortcut]\r\n");
                        writer.Write("URL=file:///" + SavedExecutable.Replace('\\', '/') + "\r\n");
                        writer.Write("IconIndex=0\r\n");
                        writer.Write("IconFile=" + SavedExecutable.Replace('\\', '/') + "\r\n");
                        writer.WriteLine($"WorkingDirectory={Path.GetDirectoryName(SavedExecutable).Replace('\\', '/')}");
                        writer.Flush();
                    }
                }
                catch { }
            }
        }

        private void LaunchParameter_Changed(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ViewModel.Directory))
            {
                Preferences.Set(AppConfigKey.LaunchParameter, ViewModel.LaunchParameter, $"{ViewModel.Directory}\\gamevault-exec");
            }
        }
        #endregion
        #region EDIT IMAGE      
        private void ImageDrop(DragEventArgs e, string tag)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    uiUploadBoxArtPreview.ImageSource = BitmapHelper.GetBitmapImage(files[0]);
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Html))
            {
                string html = (string)e.Data.GetData(DataFormats.Html);
                string imagePath = ExtractImageUrlFromHtml(html);

                if (!string.IsNullOrEmpty(imagePath))
                {
                    try
                    {
                        BitmapImage bitmap = new BitmapImage(new Uri(imagePath));
                        if (tag == "box")
                        {
                            uiUploadBoxArtPreview.ImageSource = bitmap;
                        }
                        else
                        {
                            uiUploadBackgroundPreview.ImageSource = bitmap;
                        }
                    }
                    catch
                    {
                        MainWindowViewModel.Instance.AppBarText = "Failed to download image";
                    }
                }
            }
        }
        private string ExtractImageUrlFromHtml(string html)
        {
            Regex regex = new Regex("<img[^>]+?src\\s*=\\s*['\"]([^'\"]+)['\"][^>]*>");
            Match match = regex.Match(html);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return string.Empty;
        }

        private void ChooseImage(string tag)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                    {
                        if (tag == "box")
                        {
                            uiUploadBoxArtPreview.ImageSource = BitmapHelper.GetBitmapImage(dialog.FileName);
                        }
                        else
                        {
                            uiUploadBackgroundPreview.ImageSource = BitmapHelper.GetBitmapImage(dialog.FileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        #region Events
        private void BoxImage_ChooseImage(object sender, MouseButtonEventArgs e)
        {
            ChooseImage("box");
        }
        private void BackgroundImage_ChooseImage(object sender, MouseButtonEventArgs e)
        {
            ChooseImage("");
        }
        private void BoxImage_Drop(object sender, DragEventArgs e)
        {
            ImageDrop(e, "box");
        }
        private void BackgroundImage_Drop(object sender, DragEventArgs e)
        {
            ImageDrop(e, "");
        }
        private async void BoxImage_Save(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await SaveImage("box");
            ((Button)sender).IsEnabled = true;
        }
        private async void BackgroundImage_Save(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await SaveImage("");
            ((Button)sender).IsEnabled = true;
        }
        #endregion

        private async Task SaveImage(string tag)
        {
            try
            {
                BitmapSource bitmapSource = tag == "box" ? (BitmapSource)uiUploadBoxArtPreview.ImageSource : (BitmapSource)uiUploadBackgroundPreview.ImageSource;
                string resp = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/images", BitmapHelper.BitmapSourceToMemeryStream(bitmapSource), "x.png", null);
                var newImageId = JsonSerializer.Deserialize<Models.Image>(resp).ID;
                await Task.Run(() =>
                {
                    try
                    {
                        dynamic updateObject = new System.Dynamic.ExpandoObject();
                        if (tag == "box")
                        {
                            updateObject.box_image_id = newImageId;
                        }
                        else
                        {
                            updateObject.background_image_id = newImageId;
                        }
                        string changedGame = WebHelper.Put($"{SettingsViewModel.Instance.ServerUrl}/api/games/{ViewModel.Game.ID}", JsonSerializer.Serialize(updateObject), true);
                        ViewModel.Game = JsonSerializer.Deserialize<Game>(changedGame);
                        MainWindowViewModel.Instance.AppBarText = "Successfully updated image";
                    }
                    catch (WebException ex)
                    {
                        string msg = WebExceptionHelper.GetServerMessage(ex);
                        MainWindowViewModel.Instance.AppBarText = msg;
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = ex.Message;
                    }
                });
                //Update Data Context for Library. So that the images are also refreshed there directly

                NewInstallViewModel.Instance.RefreshGame(ViewModel.Game);
                MainWindowViewModel.Instance.NewLibrary.RefreshGame(ViewModel.Game);

            }
            catch (WebException ex)
            {
                string msg = WebExceptionHelper.GetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = msg;
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        private void Image_Paste(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.V)
                {
                    try
                    {
                        if (Clipboard.ContainsImage())
                        {
                            var image = Clipboard.GetImage();
                            if (((FrameworkElement)sender).Tag != null && ((FrameworkElement)sender).Tag.ToString() == "box")
                            {
                                uiUploadBoxArtPreview.ImageSource = image;
                            }
                            else
                            {
                                uiUploadBackgroundPreview.ImageSource = image;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = ex.Message;
                    }
                }
            }
        }
        #endregion

        #region RAWG
        private InputTimer RawgGameSearchTimer { get; set; }
        private void RawgGameSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            InitRawgGameSearchTimer();
            RawgGameSearchTimer.Stop();
            RawgGameSearchTimer.Data = ((TextBox)sender).Text;
            RawgGameSearchTimer.Start();
        }
        private void InitRawgGameSearchTimer()
        {
            if (RawgGameSearchTimer != null)
                return;

            RawgGameSearchTimer = new InputTimer();
            RawgGameSearchTimer.Interval = TimeSpan.FromMilliseconds(400);
            RawgGameSearchTimer.Tick += RawgGameSearchTimerElapsed;
        }
        private async void RawgGameSearchTimerElapsed(object sender, EventArgs e)
        {
            RawgGameSearchTimer?.Stop();
            await RawgGameSearch();
        }
        private async Task RawgGameSearch()
        {
            ViewModel.RawgGames = await Task<RawgGame[]>.Run(() =>
            {
                try
                {
                    string currentShownUser = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/rawg/search?query={RawgGameSearchTimer.Data}");
                    return JsonSerializer.Deserialize<RawgGame[]>(currentShownUser);
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = $"Could not load rawg data. ({ex.Message})";
                    return null;
                }
            });
        }
        private async void Recache_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/rawg/{ViewModel.Game.ID}/recache", string.Empty);
                    MainWindowViewModel.Instance.AppBarText = $"Sucessfully re-cached {ViewModel.Game.Title}";
                }
                catch (WebException ex)
                {
                    string msg = WebExceptionHelper.GetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            });
            this.IsEnabled = true;
        }
        private async void RawgGameRemap_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            int? rawgId = ((RawgGame)((FrameworkElement)sender).DataContext).ID;
            int gameId = ViewModel.Game.ID;
            await Task.Run(() =>
            {
                try
                {
                    string remappedGame = WebHelper.Put($"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameId}", "{\n\"rawg_id\": " + rawgId + "\n}", true);
                    ViewModel.Game = JsonSerializer.Deserialize<Game>(remappedGame);

                    MainWindowViewModel.Instance.AppBarText = $"Successfully re-mapped {ViewModel.Game.Title}";
                }
                catch (WebException ex)
                {
                    string errMessage = WebExceptionHelper.GetServerMessage(ex);
                    if (errMessage == string.Empty) { errMessage = "Failed to re-map game"; }
                    MainWindowViewModel.Instance.AppBarText = errMessage;
                }
            });
            NewInstallViewModel.Instance.RefreshGame(ViewModel.Game);
            MainWindowViewModel.Instance.NewLibrary.RefreshGame(ViewModel.Game);
            this.IsEnabled = true;
        }
        #endregion
    }
}
