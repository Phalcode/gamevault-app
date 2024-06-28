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
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Extensions;
using gamevault.Converter;
using System.Windows.Media;
using LiveChartsCore.SkiaSharpView.Painting;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for GameSettingsUserControl.xaml
    /// </summary>
    public partial class GameSettingsUserControl : UserControl
    {
        private bool startup = true;
        private bool loaded = false;
        private GameSettingsViewModel ViewModel { get; set; }
        private string SavedExecutable { get; set; }
        private GameSizeConverter gameSizeConverter { get; set; }

        internal GameSettingsUserControl(Game game)
        {
            InitializeComponent();
            ViewModel = new GameSettingsViewModel();
            ViewModel.Game = game;
            gameSizeConverter = new GameSizeConverter();
            if (IsGameInstalled(game))
            {
                FindGameExecutables(ViewModel.Directory, true);
                if (Directory.Exists(ViewModel.Directory))
                {
                    ViewModel.LaunchParameter = Preferences.Get(AppConfigKey.LaunchParameter, $"{ViewModel.Directory}\\gamevault-exec");
                }
                InitDiskUsagePieChart();//Task
            }
            this.DataContext = ViewModel;
        }
        private void GameSettings_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                loaded = true;
                this.Focus();
            }
        }
        private void KeyBindingEscape_OnExecuted(object sender, object e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }
        private void Help_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string url = "";
                int currentIndex = 0;
                if (uiSettingsHeadersLocal.SelectedIndex == -1)
                    currentIndex = uiSettingsHeadersLocal.Items.Count + uiSettingsHeadersRemote.SelectedIndex;
                if (uiSettingsHeadersRemote.SelectedIndex == -1)
                    currentIndex = uiSettingsHeadersLocal.SelectedIndex;

                Debug.WriteLine(currentIndex);
                switch (currentIndex)
                {
                    case 0:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui#installation";
                            break;
                        }
                    case 1:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui#launch-options";
                            break;
                        }
                    case 2:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui#edit-images";
                            break;
                        }
                    case 3:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui#rawg-integration";
                            break;
                        }
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        private bool IsGameInstalled(Game game)
        {
            KeyValuePair<Game, string> result = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == game.ID).FirstOrDefault();
            if (result.Equals(default(KeyValuePair<Game, string>)))
                return false;

            ViewModel.Directory = result.Value;
            return true;
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
                if (startup && ViewModel.Directory != null)
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
                Process.Start("explorer.exe", ViewModel.Directory.Replace("\\\\", "\\"));
        }
        private async void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            await UninstallGame();
            ((FrameworkElement)sender).IsEnabled = true;
        }

        public async Task UninstallGame()
        {
            if (ViewModel.Game.Type == GameType.WINDOWS_PORTABLE)
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to uninstall '{ViewModel.Game.Title}' ?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    try
                    {
                        if (Directory.Exists(ViewModel.Directory))
                            Directory.Delete(ViewModel.Directory, true);

                        InstallViewModel.Instance.InstalledGames.Remove(InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.ID).First());
                        DesktopHelper.RemoveShotcut(ViewModel.Game);
                        MainWindowViewModel.Instance.ClosePopup();
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
                            MessageDialogResult pickResult = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to uninstall the game using '{Path.GetFileName(dialog.FileName)}' ?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                            if (pickResult != MessageDialogResult.Affirmative)
                            {
                                return;
                            }
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
                                    {
                                        //Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(ViewModel.Directory, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.DeletePermanently);                                        
                                        Directory.Delete(ViewModel.Directory, true);
                                    }

                                    InstallViewModel.Instance.InstalledGames.Remove(InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.ID).First());
                                    DesktopHelper.RemoveShotcut(ViewModel.Game);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            else if (ViewModel.Game.Type == GameType.UNDETECTABLE)
            {
                MainWindowViewModel.Instance.AppBarText = "Game Type cannot be determined";
            }
        }

        private void InitDiskUsagePieChart()
        {
            Task.Run(() =>
            {
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == Path.GetPathRoot(ViewModel.Directory));

                if (drive == null)
                {
                    // Throw error
                    return;
                }
                long totalDiskSize = drive.TotalSize;
                long currentGameSize = long.TryParse(ViewModel.Game.Size, out var size) ? size : 0;

                long otherGamesSize = InstallViewModel.Instance.InstalledGames
                    .Sum(installedGame => long.TryParse(installedGame.Key.Size, out var gameSize) ? gameSize : 0) - currentGameSize;

                long unmanagedDiskSize = totalDiskSize - currentGameSize - otherGamesSize - drive.TotalFreeSpace;

                double percentageOfAllGames = (currentGameSize * 100.0) / otherGamesSize;

                double totalFreeSpacePercentage = ((double)drive.TotalFreeSpace / totalDiskSize) * 100;
                double otherGamesPercentage = ((double)otherGamesSize / totalDiskSize) * 100;
                double currentGamePercentage = ((double)currentGameSize / totalDiskSize) * 100;
                double unmanagedSpacePercentage = ((double)unmanagedDiskSize / totalDiskSize) * 100;

                double[] percentages = { currentGamePercentage, otherGamesPercentage, unmanagedSpacePercentage, totalFreeSpacePercentage };
                for (int i = 0; i < percentages.Length; i++)
                {
                    if (percentages[i] > 5)
                        continue;

                    if (percentages[i] == 0)
                        continue;

                    totalFreeSpacePercentage -= (5 - percentages[i]);
                    percentages[i] = 5;
                }
                int index = 0;
                string[] names = { $"This Game ({ViewModel.Game.Title})", "Other installed GameVault Games", "Unmanaged Data", "Free Space" };
                long[] tooltips = { currentGameSize, otherGamesSize, unmanagedDiskSize, drive.TotalFreeSpace };
                Color[] colors = { Colors.DeepPink, Colors.LightSeaGreen, Colors.PaleVioletRed, Colors.DarkGray };
                IEnumerable<ISeries> sliceSeries = percentages.AsPieSeries((value, series) =>
                {
                    var size = tooltips[index % tooltips.Length];
                    var humanReadableSize = gameSizeConverter.Convert(size, null, null, null);
                    var color = new SolidColorPaint(new SkiaSharp.SKColor(colors[index % colors.Length].R, colors[index % colors.Length].G, colors[index % colors.Length].B));

                    series.Name = names[index % names.Length];
                    series.Fill = color;
                    series.MaxRadialColumnWidth = 50;

                    // Outer-Label
                    series.DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer;
                    series.DataLabelsSize = 15;
                    series.DataLabelsPadding = index == 1 ? new LiveChartsCore.Drawing.Padding(5, 35, 5, 0) : new LiveChartsCore.Drawing.Padding(10);
                    if (size != 0)
                    {
                        series.DataLabelsPaint = color;
                        series.DataLabelsFormatter = point => $"{humanReadableSize}";
                        series.ToolTipLabelFormatter = point => $"{point.StackedValue!.Share:P2}";
                    }

                    index++;
                });
                try
                {
                    ViewModel.DiskSize = $"{drive.VolumeLabel} ({drive.RootDirectory.ToString().Trim('\\')}) - {gameSizeConverter.Convert(drive.TotalSize, null, null, null).ToString()}";
                }
                catch
                {
                    ViewModel.DiskSize = $"{gameSizeConverter.Convert(drive.TotalSize, null, null, null).ToString()}";
                }
                SolidColorBrush legendTextPaint = (SolidColorBrush)Application.Current.TryFindResource("MahApps.Brushes.Text");
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    uiDiscUsagePieChart.LegendTextPaint = new SolidColorPaint(legendTextPaint == null ? new SkiaSharp.SKColor(0, 0, 0) : new SkiaSharp.SKColor(legendTextPaint.Color.R, legendTextPaint.Color.G, legendTextPaint.Color.B));
                    uiDiscUsagePieChart.Series = sliceSeries;
                });
            });
        }

        #endregion
        #region LAUNCH OPTIONS
        private void FindGameExecutables(string directory, bool checkForSavedExecutable)
        {
            if (!Directory.Exists(directory))
                return;

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
        public static bool TryPrepareLaunchExecutable(string directory)
        {
            foreach (string entry in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                string fileType = Path.GetExtension(entry).TrimStart('.');
                if (Globals.SupportedExecutables.Contains(fileType.ToUpper()))
                {
                    if (!ContainsValueFromIgnoreList(entry))
                    {
                        if (!File.Exists($"{directory}\\gamevault-exec"))
                        {
                            File.Create($"{directory}\\gamevault-exec").Close();
                        }
                        Preferences.Set(AppConfigKey.Executable, entry, $"{directory}\\gamevault-exec");
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool ContainsValueFromIgnoreList(string value)
        {
            return (InstallViewModel.Instance.IgnoreList != null && InstallViewModel.Instance.IgnoreList.Any(s => Path.GetFileNameWithoutExtension(value).Contains(s, StringComparison.OrdinalIgnoreCase)));
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
                    if (e.RemovedItems.Count > 0 && DesktopHelper.ShortcutExists(ViewModel.Game))
                    {
                        DesktopHelper.RemoveShotcut(ViewModel.Game);
                        DesktopHelper.CreateShortcut(ViewModel.Game, SavedExecutable, false);
                    }
                }
            }
        }
        private async void CreateDesktopShortcut_Click(object sender, RoutedEventArgs e)
        {
            await DesktopHelper.CreateShortcut(ViewModel.Game, SavedExecutable, true);
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

        private async void Image_Drop(object sender, DragEventArgs e)
        {
            string tag = ((FrameworkElement)sender).Tag as string;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (tag == "box")
                    {
                        ViewModel.BoxArtImageSource = BitmapHelper.GetBitmapImage(files[0]);
                    }
                    else
                    {
                        ViewModel.BackgroundImageSource = BitmapHelper.GetBitmapImage(files[0]);
                    }
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
                        BitmapImage bitmap = await BitmapHelper.GetBitmapImageAsync(imagePath);
                        if (tag == "box")
                        {
                            ViewModel.BoxArtImageSource = bitmap;
                        }
                        else
                        {
                            ViewModel.BackgroundImageSource = bitmap;
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

        private void ChooseImage(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string tag = ((FrameworkElement)sender).Tag as string;
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                    {
                        if (tag == "box")
                        {
                            ViewModel.BoxArtImageSource = BitmapHelper.GetBitmapImage(dialog.FileName);
                        }
                        else
                        {
                            ViewModel.BackgroundImageSource = BitmapHelper.GetBitmapImage(dialog.FileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        private async void LoadImageUrl(string url, string tag)
        {
            try
            {
                if (tag == "box")
                {
                    ViewModel.BoxArtImageSource = await BitmapHelper.GetBitmapImageAsync(url);
                }
                else
                {
                    ViewModel.BackgroundImageSource = await BitmapHelper.GetBitmapImageAsync(url);
                }
            }
            catch (Exception ex)
            {
                if (url != string.Empty)
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        private void FindImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = ((FrameworkElement)sender).Tag.ToString();
                string googleSearchUrl = $"https://www.google.com/search?q={ViewModel.Game.Title} {query}&tbm=isch";
                Process.Start(new ProcessStartInfo
                {
                    FileName = googleSearchUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        #region Generic Events       
        private InputTimer backgroundImageUrldebounceTimer { get; set; }
        private InputTimer boxImageUrldebounceTimer { get; set; }
        private void InitImageUrlTimer()
        {
            if (backgroundImageUrldebounceTimer == null)
            {
                backgroundImageUrldebounceTimer = new InputTimer() { Data = string.Empty };
                backgroundImageUrldebounceTimer.Interval = TimeSpan.FromMilliseconds(400);
                backgroundImageUrldebounceTimer.Tick += BackgroundImageDebounceTimerElapsed;
            }
            if (boxImageUrldebounceTimer == null)
            {
                boxImageUrldebounceTimer = new InputTimer() { Data = string.Empty };
                boxImageUrldebounceTimer.Interval = TimeSpan.FromMilliseconds(400);
                boxImageUrldebounceTimer.Tick += BoxImageDebounceTimerElapsed;
            }
        }
        private async void BoxImage_Save(object sender, RoutedEventArgs e)
        {
            ViewModel.BoxArtImageChanged = false;
            await SaveImage("box");
        }
        private async void BackgroundImage_Save(object sender, RoutedEventArgs e)
        {
            ViewModel.BackgroundImageChanged = false;
            await SaveImage("");
        }
        private void BackgoundImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            InitImageUrlTimer();
            backgroundImageUrldebounceTimer.Stop();
            backgroundImageUrldebounceTimer.Data = ((TextBox)sender).Text;
            backgroundImageUrldebounceTimer.Start();
        }
        private void BoxImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            InitImageUrlTimer();
            boxImageUrldebounceTimer.Stop();
            boxImageUrldebounceTimer.Data = ((TextBox)sender).Text;
            boxImageUrldebounceTimer.Start();
        }
        private void BackgroundImageDebounceTimerElapsed(object? sender, EventArgs e)
        {
            backgroundImageUrldebounceTimer.Stop();
            LoadImageUrl(backgroundImageUrldebounceTimer.Data, "");
        }
        private void BoxImageDebounceTimerElapsed(object? sender, EventArgs e)
        {
            boxImageUrldebounceTimer.Stop();
            LoadImageUrl(boxImageUrldebounceTimer.Data, "box");
        }
        #endregion

        private async Task SaveImage(string tag)
        {
            bool success = false;
            try
            {
                BitmapSource bitmapSource = tag == "box" ? (BitmapSource)ViewModel.BoxArtImageSource : (BitmapSource)ViewModel.BackgroundImageSource;
                string resp = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/images", BitmapHelper.BitmapSourceToMemoryStream(bitmapSource), "x.png", null);
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
                        success = true;
                        MainWindowViewModel.Instance.AppBarText = "Successfully updated image";
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
                    }
                });
                //Update Data Context for Library. So that the images are also refreshed there directly
                if (success)
                {
                    InstallViewModel.Instance.RefreshGame(ViewModel.Game);
                    MainWindowViewModel.Instance.Library.RefreshGame(ViewModel.Game);
                    if (MainWindowViewModel.Instance.ActiveControl.GetType() == typeof(GameViewUserControl))
                    {
                        ((GameViewUserControl)MainWindowViewModel.Instance.ActiveControl).RefreshGame(ViewModel.Game);
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
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
                                ViewModel.BoxArtImageSource = image;
                            }
                            else
                            {
                                ViewModel.BackgroundImageSource = image;
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
            this.Cursor = Cursors.Wait;
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
            this.Cursor = null;
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
                catch (Exception ex)
                {
                    string msg = WebExceptionHelper.TryGetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            });
            this.IsEnabled = true;
            this.Focus();
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
                catch (Exception ex)
                {
                    string errMessage = WebExceptionHelper.TryGetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = errMessage;
                }
            });
            InstallViewModel.Instance.RefreshGame(ViewModel.Game);
            MainWindowViewModel.Instance.Library.RefreshGame(ViewModel.Game);
            this.IsEnabled = true;
            this.Focus();
        }




        #endregion


    }
}
