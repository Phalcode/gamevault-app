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
using gamevault.Models.Mapping;
using IO.Swagger.Model;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;


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
            ViewModel.UpdateGame = new UpdateGameDto() { UserMetadata = new UpdateGameUserMetadataDto() };
            gameSizeConverter = new GameSizeConverter();
            if (IsGameInstalled(game))
            {
                FindGameExecutables(ViewModel.Directory, true);
                if (Directory.Exists(ViewModel.Directory))
                {
                    ViewModel.LaunchParameter = Preferences.Get(AppConfigKey.LaunchParameter, $"{ViewModel.Directory}\\gamevault-exec");
                    string installedVersion = Preferences.Get(AppConfigKey.InstalledGameVersion, $"{ViewModel.Directory}\\gamevault-exec");
                    ViewModel.InstalledGameVersion = installedVersion == string.Empty ? null : installedVersion;
                }
                InitDiskUsagePieChart();//Task
            }
            this.DataContext = ViewModel;
        }
        private async void GameSettings_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                loaded = true;
                this.Focus();
                await LoadGameMedatataProviders();
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
                            url = "https://gamevau.lt/docs/client-docs/gui/#edit-game-images";
                            break;
                        }
                    case 3:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui#metadata";
                            break;
                        }
                    case 4:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui#custom-metadata";
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
            //Check for forced installation type
            try
            {
                if (Directory.Exists(ViewModel.Directory) && int.TryParse(Preferences.Get(AppConfigKey.ForcedInstallationType, $"{ViewModel.Directory}\\gamevault-exec"), out int intValue))
                {
                    if (Enum.IsDefined(typeof(GameType), intValue))
                    {
                        ViewModel.Game.Type = (GameType)intValue;
                    }
                }
            }
            catch { }

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
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to uninstall '{ViewModel.Game.Title}' ?\nAs this is a Windows Setup Game, you will need to select an uninstall executable manually", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    string selectedUninstallerExecutablePath = "";
                    if (!string.IsNullOrWhiteSpace(ViewModel.Game?.Metadata?.UninstallerExecutable))
                    {
                        var entry = Directory.GetFiles(ViewModel.Directory, "*", SearchOption.AllDirectories)
                                    .Select((file) => new { Key = file.Substring(ViewModel.Directory.Length + 1), Value = file })
                                    .FirstOrDefault(item => item.Key.Contains(ViewModel.Game?.Metadata?.UninstallerExecutable.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase));
                        if (entry != null)
                        {
                            selectedUninstallerExecutablePath = entry.Value;
                            if (!File.Exists(selectedUninstallerExecutablePath))
                            {
                                selectedUninstallerExecutablePath = "";
                            }
                        }
                    }
                    if (selectedUninstallerExecutablePath == "")
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
                                selectedUninstallerExecutablePath = dialog.FileName;
                            }
                        }
                    }
                    if (!File.Exists(selectedUninstallerExecutablePath))
                    {
                        MainWindowViewModel.Instance.AppBarText = "No valid uninstall executable selected";
                        return;
                    }
                    Process uninstProcess = null;
                    try
                    {
                        uninstProcess = ProcessHelper.StartApp(selectedUninstallerExecutablePath, ViewModel.Game?.Metadata?.UninstallerParameters);
                    }
                    catch
                    {

                        try
                        {
                            uninstProcess = ProcessHelper.StartApp(selectedUninstallerExecutablePath, ViewModel.Game?.Metadata?.UninstallerParameters, true);
                        }
                        catch
                        {
                            MainWindowViewModel.Instance.AppBarText = $"Can not execute '{selectedUninstallerExecutablePath}'";
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
                            MainWindowViewModel.Instance.ClosePopup();
                        }
                        catch { }
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
            return (SettingsViewModel.Instance.IgnoreList != null && SettingsViewModel.Instance.IgnoreList.Any(s => Path.GetFileNameWithoutExtension(value).Contains(s, StringComparison.OrdinalIgnoreCase)));
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
                        ViewModel.GameCoverImageSource = BitmapHelper.GetBitmapImage(files[0]);
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
                            ViewModel.GameCoverImageSource = bitmap;
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
                            ViewModel.GameCoverImageSource = BitmapHelper.GetBitmapImage(dialog.FileName);
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
                    ViewModel.GameCoverImageSource = await BitmapHelper.GetBitmapImageAsync(url);
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
        private async Task SaveImage(string tag)
        {
            bool success = false;
            try
            {
                BitmapSource bitmapSource = tag == "box" ? (BitmapSource)ViewModel.GameCoverImageSource : (BitmapSource)ViewModel.BackgroundImageSource;
                string resp = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/media", BitmapHelper.BitmapSourceToMemoryStream(bitmapSource), "x.jpg", null);
                Media? newImage = JsonSerializer.Deserialize<Media>(resp);

                try
                {
                    UpdateGameDto updateGame = new UpdateGameDto() { UserMetadata = new UpdateGameUserMetadataDto() };
                    if (tag == "box")
                    {
                        updateGame.UserMetadata.Cover = newImage;
                    }
                    else
                    {
                        updateGame.UserMetadata.Background = newImage;
                    }

                    string changedGame = await WebHelper.PutAsync($"{SettingsViewModel.Instance.ServerUrl}/api/games/{ViewModel.Game.ID}", JsonSerializer.Serialize(updateGame));
                    ViewModel.Game = JsonSerializer.Deserialize<Game>(changedGame);
                    success = true;
                    MainWindowViewModel.Instance.AppBarText = "Successfully updated image";
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
                }

                //Update Data Context for Library. So that the images are also refreshed there directly
                if (success)
                {
                    InstallViewModel.Instance.RefreshGame(ViewModel.Game);
                    MainWindowViewModel.Instance.Library.RefreshGame(ViewModel.Game);
                    MainWindowViewModel.Instance.Downloads.RefreshGame(ViewModel.Game);
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
                                ViewModel.GameCoverImageSource = image;
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
        private void CopyImageToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapSource bitmapSource = null;
                if (((FrameworkElement)sender).Tag?.ToString() == "CurrentShownMappedGame")
                {
                    bitmapSource = (BitmapSource)uiImgCurrentShownMappedGame.GetImageSource();
                }
                else
                {
                    bitmapSource = (BitmapSource)uiImgCurrentMergedGame.GetImageSource();
                }
                Clipboard.SetImage(bitmapSource);
                MainWindowViewModel.Instance.AppBarText = "Image copied to Clipboard";
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
            ViewModel.GameCoverImageChanged = false;
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
        #endregion
        #region Metadata
        private InputTimer GameMetadataSearchTimer { get; set; }
        private void ProviderGameSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            InitGameMetadataSearchTimer();
            GameMetadataSearchTimer.Stop();
            GameMetadataSearchTimer.Data = ((TextBox)sender).Text;
            GameMetadataSearchTimer.Start();
        }
        private void InitGameMetadataSearchTimer()
        {
            if (GameMetadataSearchTimer != null)
                return;

            GameMetadataSearchTimer = new InputTimer();
            GameMetadataSearchTimer.Interval = TimeSpan.FromMilliseconds(400);
            GameMetadataSearchTimer.Tick += GameMetadataSearchTimerElapsed!;
        }
        private async void GameMetadataSearchTimerElapsed(object sender, EventArgs e)
        {
            GameMetadataSearchTimer?.Stop();
            await GameMetadataSearch();
        }
        private async Task GameMetadataSearch()
        {
            this.Cursor = Cursors.Wait;
            try
            {
                string currentShownUser = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/metadata/providers/{ViewModel.MetadataProviders?[ViewModel.SelectedMetadataProviderIndex]?.Slug}/search?query={GameMetadataSearchTimer.Data}");
                ViewModel.RemapSearchResults = JsonSerializer.Deserialize<MinimalGame[]>(currentShownUser);
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = $"Could not load metadata provider data. ({ex.Message})";
                ViewModel.RemapSearchResults = null;
            }
            this.Cursor = null;
        }
        private async void Recache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MetadataProviderDto currentSelectedProvider = ViewModel.MetadataProviders?[ViewModel.SelectedMetadataProviderIndex];
                string? currentProviderSlug = currentSelectedProvider?.Slug;
                string? providerId = ViewModel.CurrentShownMappedGame?.ProviderDataId;
                int gameId = ViewModel.Game.ID;
                await RemapGame(providerId, currentProviderSlug, gameId);
            }
            catch { }
        }
        private async void GameRemap_Click(object sender, RoutedEventArgs e)
        {
            var providerId = ((MinimalGame)((FrameworkElement)sender).DataContext).ProviderDataId;
            MetadataProviderDto? currentSelectedProvider = ViewModel.MetadataProviders?[ViewModel.SelectedMetadataProviderIndex];
            string? currentProviderSlug = currentSelectedProvider?.Slug;
            int gameId = ViewModel.Game.ID;
            await RemapGame(providerId, currentProviderSlug, gameId);
        }
        private async void Unmap_Click(object sender, RoutedEventArgs e)
        {
            MetadataProviderDto? currentSelectedProvider = ViewModel.MetadataProviders?[ViewModel.SelectedMetadataProviderIndex];
            string? currentProviderSlug = currentSelectedProvider?.Slug;
            int gameId = ViewModel.Game.ID;
            await RemapGame(null, currentProviderSlug, gameId);
        }
        private async void SavePriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MetadataProviderDto currentSelectedProvider = ViewModel.MetadataProviders?[ViewModel.SelectedMetadataProviderIndex];
                string? currentProviderSlug = currentSelectedProvider?.Slug;
                string? providerId = ViewModel.CurrentShownMappedGame?.ProviderDataId;
                int gameId = ViewModel.Game.ID;
                await RemapGame(providerId, currentProviderSlug, gameId, (int?)uiProviderPriority.Value);
                await LoadGameMedatataProviders();
            }
            catch { }
        }
        private async Task RemapGame(string? providerId, string? providerSlug, int gameId, int? priority = null)
        {
            bool success = false;
            this.IsEnabled = false;
            try
            {
                UpdateGameDto updateGame = new UpdateGameDto() { MappingRequests = new List<MapGameDto>() { new MapGameDto() { ProviderSlug = providerSlug, ProviderDataId = providerId, ProviderPriority = priority } } };
                string remappedGame = await WebHelper.PutAsync($"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameId}", JsonSerializer.Serialize(updateGame));
                ViewModel.Game = JsonSerializer.Deserialize<Game>(remappedGame);
                success = true;
                MainWindowViewModel.Instance.AppBarText = $"Successfully re-mapped {ViewModel.Game.Title}";
            }
            catch (Exception ex)
            {
                string errMessage = WebExceptionHelper.TryGetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = errMessage;
            }
            InstallViewModel.Instance.RefreshGame(ViewModel.Game);
            MainWindowViewModel.Instance.Library.RefreshGame(ViewModel.Game);
            if (success)
            {
                if (MainWindowViewModel.Instance.ActiveControl.GetType() == typeof(GameViewUserControl))
                {
                    ((GameViewUserControl)MainWindowViewModel.Instance.ActiveControl).RefreshGame(ViewModel.Game);
                }
                ViewModel.CurrentShownMappedGame = ViewModel.CurrentShownMappedGame;
            }
            this.IsEnabled = true;
            this.Focus();
        }
        private async Task LoadGameMedatataProviders()
        {
            try
            {
                ViewModel.MetadataProvidersLoaded = false;
                string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/metadata/providers");
                var providers = JsonSerializer.Deserialize<MetadataProviderDto[]?>(result);
                foreach (GameMetadata gmd in ViewModel.Game.ProviderMetadata)
                {
                    if (gmd.ProviderPriority != null)
                    {
                        var provider = providers?.FirstOrDefault(p => p.Slug == gmd.ProviderSlug);
                        if (provider != null)
                        {
                            provider.Priority = gmd.ProviderPriority;
                        }
                    }
                }
                providers = providers?.OrderByDescending(p => p.Priority).ToArray();
                ViewModel.MetadataProviders = providers;
                ViewModel.SelectedMetadataProviderIndex = 0;
                ViewModel.MetadataProvidersLoaded = true;
            }
            catch (Exception ex)
            {
                string message = WebExceptionHelper.TryGetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = message;
            }
        }

        #endregion
        #region Edit Game Details
        private async void ClearUserData_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to wipe all manually edited custom metadata and images?\n\nAll fields will revert to the merged provider metadata (if available).\n\nThis action cannot be undone.", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
            if (result == MessageDialogResult.Affirmative)
            {
                int gameId = ViewModel.Game.ID;
                await RemapGame(null, "user", gameId);
            }
        }
        private async void SaveGameDetails_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            bool success = false;

            try
            {
                string remappedGame = await WebHelper.PutAsync($"{SettingsViewModel.Instance.ServerUrl}/api/games/{ViewModel.Game.ID}", JsonSerializer.Serialize(ViewModel.UpdateGame));
                ViewModel.Game = JsonSerializer.Deserialize<Game>(remappedGame);
                success = true;
                ViewModel.UpdateGame = new UpdateGameDto() { UserMetadata = new UpdateGameUserMetadataDto() };
                MainWindowViewModel.Instance.AppBarText = $"Successfully edited {ViewModel.Game.Title}";
            }
            catch (Exception ex)
            {
                string errMessage = WebExceptionHelper.TryGetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = errMessage;
            }
            if (success)
            {
                if (MainWindowViewModel.Instance.ActiveControl.GetType() == typeof(GameViewUserControl))
                {
                    ((GameViewUserControl)MainWindowViewModel.Instance.ActiveControl).RefreshGame(ViewModel.Game);
                }
                MainWindowViewModel.Instance.Downloads.RefreshGame(ViewModel.Game);
            }
            this.IsEnabled = true;
            this.Focus();
        }
        private void KeepData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tag = ((FrameworkElement)sender).Tag.ToString();
                if (tag == "description")
                {
                    ViewModel.UpdateGame.UserMetadata.Description = ViewModel.Game.Metadata.Description;
                }
                else if (tag == "notes")
                {
                    ViewModel.UpdateGame.UserMetadata.Notes = ViewModel.Game.Metadata.Notes;
                }
                else if (tag == "genre")
                {
                    ViewModel.UpdateGame.UserMetadata.Genres = ViewModel.Game.Metadata.Genres.Select(genre => genre.Name).ToArray();
                }
                else if (tag == "tag")
                {
                    ViewModel.UpdateGame.UserMetadata.Tags = ViewModel.Game.Metadata.Tags.Select(genre => genre.Name).ToArray();
                }
                else if (tag == "publisher")
                {
                    ViewModel.UpdateGame.UserMetadata.Publishers = ViewModel.Game.Metadata.Publishers.Select(genre => genre.Name).ToArray();
                }
                else if (tag == "developer")
                {
                    ViewModel.UpdateGame.UserMetadata.Developers = ViewModel.Game.Metadata.Developers.Select(genre => genre.Name).ToArray();
                }
                else if (tag == "trailer")
                {
                    ViewModel.UpdateGame.UserMetadata.UrlTrailers = ViewModel.Game.Metadata.Trailers;
                }
                else if (tag == "gameplays")
                {
                    ViewModel.UpdateGame.UserMetadata.UrlGameplays = ViewModel.Game.Metadata.Gameplays;
                }
                else if (tag == "screenshots")
                {
                    ViewModel.UpdateGame.UserMetadata.UrlScreenshots = ViewModel.Game.Metadata.Screenshots;
                }
                //Cheap update the UI without adding Notify property changed to the Model
                var temp = ViewModel.UpdateGame;
                ViewModel.UpdateGame = null;
                ViewModel.UpdateGame = temp;
            }
            catch { }
        }

        #endregion

        #region EarlyAccessToggleFix
        private bool needEarlyAccessToggleFix = false;
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (needEarlyAccessToggleFix)
                {
                    needEarlyAccessToggleFix = false;
                    var checkBox = sender as CheckBox;
                    checkBox.IsChecked = true;
                }
            }
            catch { }
        }

        private void CheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ViewModel.UpdateGame?.UserMetadata?.EarlyAccess == null && ViewModel.Game?.Metadata?.EarlyAccess == false)
                {
                    needEarlyAccessToggleFix = true;
                }
            }
            catch { }
        }
        #endregion
    }
}
