using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using ImageMagick.Formats;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for GameInstallUserControl.xaml
    /// </summary>
    public partial class GameDownloadUserControl : UserControl
    {
        private GameDownloadViewModel ViewModel { get; set; }
        private bool IsDownloadActive = false;

        private string m_DownloadPath { get; set; }
        private bool extractionCancelled = false;
        private HttpClientDownloadWithProgress client { get; set; }
        private DateTime startTime;

        private SevenZipHelper sevenZipHelper { get; set; }
        public GameDownloadUserControl(Game game, bool download)
        {
            InitializeComponent();
            ViewModel = new GameDownloadViewModel();
            this.DataContext = ViewModel;
            ViewModel.Game = game;
            ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.DownloadFailedVisibility = System.Windows.Visibility.Hidden;

            m_DownloadPath = $"{SettingsViewModel.Instance.RootPath}\\GameVault\\Downloads\\({ViewModel.Game.ID}){ViewModel.Game.Title}";
            m_DownloadPath = m_DownloadPath.Replace(@"\\", @"\");
            ViewModel.InstallPath = $"{SettingsViewModel.Instance.RootPath}\\GameVault\\Installations\\({ViewModel.Game.ID}){ViewModel.Game.Title}";
            ViewModel.InstallPath = ViewModel.InstallPath.Replace(@"\\", @"\");
            sevenZipHelper = new SevenZipHelper();
            if (download)
            {
                DownloadGame();
            }
            else
            {
                if (File.Exists($"{m_DownloadPath}\\Extract\\gamevault-metadata") && Preferences.Get(AppConfigKey.ExtractionFinished, $"{m_DownloadPath}\\Extract\\gamevault-metadata") == "1")
                {
                    ViewModel.State = "Extracted";
                    uiBtnExtract.IsEnabled = true;
                    uiBtnInstall.IsEnabled = true;
                    ((TextBlock)uiBtnExtract.Child).Text = "Re-Extract";
                }
                else
                {
                    ViewModel.State = "Downloaded";
                    uiBtnExtract.IsEnabled = true;
                }
            }
        }
        public bool IsDownloading()
        {
            return IsDownloadActive;
        }
        public bool IsGameIdDownloading(int id)
        {
            if (IsDownloadActive == true && ViewModel.Game.ID == id)
            {
                return true;
            }
            return false;
        }
        public int GetGameId()
        {
            return ViewModel.Game.ID;
        }
        public int GetBoxImageID()
        {
            return ViewModel.Game.BoxImage.ID;
        }
        public int GetDownloadProgress()
        {
            return ViewModel.GameDownloadProgress;
        }
        public void CancelDownload()
        {
            if (client == null)
                return;

            client.Cancel();
            client.Dispose();
            IsDownloadActive = false;
            ViewModel.State = "Download Cancelled";
            ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.DownloadFailedVisibility = System.Windows.Visibility.Visible;

            MainWindowViewModel.Instance.UpdateTaskbarProgress();
        }
        private void DownloadGame()
        {
            Task.Run(async () =>
            {
                IsDownloadActive = true;
                ViewModel.State = "Downloading...";
                ViewModel.DownloadUIVisibility = System.Windows.Visibility.Visible;
                ViewModel.DownloadFailedVisibility = System.Windows.Visibility.Hidden;

                if (!Directory.Exists(m_DownloadPath)) { Directory.CreateDirectory(m_DownloadPath); }
                KeyValuePair<string, string>? header = null;
                if (SettingsViewModel.Instance.DownloadLimit > 0)
                {
                    header = new KeyValuePair<string, string>("X-Download-Speed-Limit", SettingsViewModel.Instance.DownloadLimit.ToString());
                }
                client = new HttpClientDownloadWithProgress($"{SettingsViewModel.Instance.ServerUrl}/api/games/{ViewModel.Game.ID}/download", m_DownloadPath, Path.GetFileName(ViewModel.Game.FilePath), header);
                client.ProgressChanged += DownloadProgress;
                startTime = DateTime.Now;

                try
                {
                    await client.StartDownload();
                    await CacheHelper.CreateOfflineCacheAsync(ViewModel.Game);
                }
                catch (Exception ex)
                {
                    client.Dispose();
                    IsDownloadActive = false;
                    ViewModel.State = $"Error: '{ex.Message}'";
                    ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
                    ViewModel.DownloadFailedVisibility = System.Windows.Visibility.Visible;
                }
            });
        }
        private void RetryDownload_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ViewModel.DownloadInfo = string.Empty;
            ViewModel.GameDownloadProgress = 0;
            DownloadGame();
        }
        private void DownloadProgress(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {

                ViewModel.DownloadInfo = $"{CalculateSpeed(totalBytesDownloaded, (DateTime.Now - startTime).TotalSeconds)} - {CalculateSize(totalBytesDownloaded)} of {CalculateSize((double)totalFileSize)} | Time left: {CalculateTimeLeft(totalFileSize, totalBytesDownloaded, (DateTime.Now - startTime).TotalSeconds)}";
                if (ViewModel.GameDownloadProgress != (int)progressPercentage)
                {
                    ViewModel.GameDownloadProgress = (int)progressPercentage;
                    if (ViewModel.GameDownloadProgress == 100)
                    {
                        DownloadCompleted();
                    }
                    MainWindowViewModel.Instance.UpdateTaskbarProgress();
                }
            });
        }

        private void DownloadCompleted()
        {
            ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
            client.Dispose();
            IsDownloadActive = false;
            ViewModel.State = "Downloaded";
            uiBtnExtract.IsEnabled = true;
            if (!Directory.Exists(ViewModel.InstallPath))
            {
                Directory.CreateDirectory(ViewModel.InstallPath);
            }
            //MainWindowViewModel.Instance.Installs.AddSystemFileWatcher(ViewModel.InstallPath);
            if (SettingsViewModel.Instance.AutoExtract)
            {
                App.Current.Dispatcher.Invoke((Action)async delegate
                {
                    await Extract();
                });
            }
        }

        private void CancelDownload_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CancelDownload();
        }
        private void CancelExtraction_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            extractionCancelled = true;
            sevenZipHelper.Cancel();
        }
        private string CalculateSpeed(double size, double tspan)
        {
            if (size / tspan > 1024 * 1024) // MB
            {
                return $"{Math.Round(size / (1024 * 1204) / tspan, 2)} MB/s";
            }
            else if (size / tspan > 1024) // KB
            {
                return $"{Math.Round(size / (1024) / tspan, 2)} KB/s";
            }
            else
            {
                return $"{Math.Round(size / tspan, 2)} B/s";
            }
        }

        private string CalculateTimeLeft(long? totalFileSize, long totalBytesDownloaded, double tspan)
        {
            var averagespeed = totalBytesDownloaded / tspan;
            var timeleft = (totalFileSize / averagespeed) - (tspan);
            TimeSpan t = TimeSpan.FromSeconds(0);
            if (!double.IsInfinity(Convert.ToDouble(timeleft)) && !double.IsNaN(Convert.ToDouble(timeleft)))
            {
                t = TimeSpan.FromSeconds(Convert.ToInt32(timeleft));
            }
            return string.Format("{0:00}:{1:00}:{2:00}", ((int)t.TotalHours), t.Minutes, t.Seconds);
        }

        private async void DeleteFile_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to delete '{ViewModel.Game.Title}' ?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    if (Directory.Exists(m_DownloadPath))
                        Directory.Delete(m_DownloadPath, true);

                    DownloadsViewModel.Instance.DownloadedGames.Remove(this);

                    if (Directory.Exists(ViewModel.InstallPath) && !Directory.EnumerateFileSystemEntries(ViewModel.InstallPath).Any())
                    {
                        Directory.Delete(ViewModel.InstallPath);
                    }
                }
                catch
                {
                    MainWindowViewModel.Instance.AppBarText = "Can not delete during the download, extraction, installing process";
                }
            }
        }
        private void OpenDirectory_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Directory.Exists(m_DownloadPath))
                Process.Start("explorer.exe", m_DownloadPath);
        }

        private void GameImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(ViewModel.Game, LoginManager.Instance.IsLoggedIn()));
        }

        private void ExtractionProgress(object sender, SevenZipProgressEventArgs e)
        {
            long totalBytesDownloaded = (Convert.ToInt64(ViewModel.Game.Size) / 100) * e.PercentageDone;
            ViewModel.ExtractionInfo = $"{CalculateSpeed(totalBytesDownloaded, (DateTime.Now - startTime).TotalSeconds)} - {CalculateSize(totalBytesDownloaded)} of {CalculateSize(Convert.ToInt64(ViewModel.Game.Size))} | Time left: {CalculateTimeLeft(Convert.ToInt64(ViewModel.Game.Size), totalBytesDownloaded, (DateTime.Now - startTime).TotalSeconds)}";
            ViewModel.GameExtractionProgress = e.PercentageDone;
        }

        private async void Extract_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await Extract();
        }
        private async Task Extract()
        {
            if (!Directory.Exists(m_DownloadPath))
            {
                ViewModel.State = "Download path not found";
                MainWindowViewModel.Instance.AppBarText = "Please report this issue on our Discord server or create a GitHub issue.";
                return;
            }
            DirectoryInfo dirInf = new DirectoryInfo(m_DownloadPath);
            FileInfo[] files = dirInf.GetFiles().Where(f => ViewModel.SupportedArchives.Contains(f.Extension.ToLower())).ToArray();
            if (files.Length <= 0)
            {
                ViewModel.State = "No archive found";
                return;
            }
            uiBtnInstall.IsEnabled = false;
            ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.State = "Extracting...";
            ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Visible;

            sevenZipHelper.Process += ExtractionProgress;
            startTime = DateTime.Now;
            int result = await sevenZipHelper.ExtractArchive($"{m_DownloadPath}\\{files[0].Name}", $"{m_DownloadPath}\\Extract");
            if (result == 0)
            {
                if (!File.Exists($"{m_DownloadPath}\\Extract\\gamevault-metadata"))
                {
                    File.Create($"{m_DownloadPath}\\Extract\\gamevault-metadata").Close();
                }
                Preferences.Set(AppConfigKey.ExtractionFinished, "1", $"{m_DownloadPath}\\Extract\\gamevault-metadata");
                ViewModel.State = "Extracted";
                ((TextBlock)uiBtnExtract.Child).Text = "Re-Extract";
                uiBtnInstall.IsEnabled = true;
                ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                if (Directory.Exists($"{m_DownloadPath}\\Extract"))
                {
                    try
                    {
                        Directory.Delete($"{m_DownloadPath}\\Extract", true);
                    }
                    catch { }
                }
                if (extractionCancelled)
                {
                    extractionCancelled = false;
                    ViewModel.State = "Extraction cancelled";
                }
                else
                {
                    ViewModel.State = "Something went wrong during extraction";
                }
                ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;
            }
        }
        private string CalculateSize(double size)
        {
            try
            {
                size = size / 1000000;
                if (size > 1000)
                {
                    size = size / 1000;
                    size = Math.Round(size, 2);
                    return $"{size} GB";
                }
                size = Math.Round(size, 2);
                return $"{size} MB";
            }
            catch (Exception ex)
            {
                return "?";
            }
        }

        private void OpenInstallOptions_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            uiInstallOptions.Visibility = System.Windows.Visibility.Visible;
            LoadSetupExecutables();

        }

        private void InstallOptionCancel_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            uiInstallOptions.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoadSetupExecutables()
        {
            if (Directory.Exists($"{m_DownloadPath}\\Extract"))
            {
                Dictionary<string, string> allExecutables = new Dictionary<string, string>();
                foreach (string fileType in Globals.SupportedExecutables)
                {
                    foreach (string entry in Directory.GetFiles(m_DownloadPath, $"*.{fileType}", SearchOption.AllDirectories))
                    {
                        string keyToAdd = Path.GetFileName(entry);
                        if (!allExecutables.ContainsKey(keyToAdd))
                        {
                            allExecutables.Add(keyToAdd, entry);
                        }
                        else
                        {
                            allExecutables.Add(entry.Replace($"{m_DownloadPath}\\Extract", ""), entry); ;
                        }
                    }
                }
                uiCbSetupExecutable.ItemsSource = allExecutables;
                if (allExecutables.Count == 1)
                {
                    uiCbSetupExecutable.SelectedIndex = 0;
                }
            }
            else
            {
                uiCbSetupExecutable.ItemsSource = null;
            }
        }

        private async void Install_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            uiBtnExtract.IsEnabled = false;
            if (ViewModel.Game.Type == GameType.WINDOWS_PORTABLE || ViewModel.Game.Type == GameType.LINUX_PORTABLE)
            {
                bool error = false;
                uiProgressRingInstall.IsActive = true;
                await Task.Run(async () =>
                {
                    try
                    {
                        if (!Directory.Exists(ViewModel.InstallPath))
                        {
                            Directory.CreateDirectory(ViewModel.InstallPath);
                            //MainWindowViewModel.Instance.Installs.AddSystemFileWatcher(ViewModel.InstallPath);
                        }
                        else if (Directory.Exists($"{ViewModel.InstallPath}\\Files"))
                        {
                            Directory.Delete($"{ViewModel.InstallPath}\\Files", true);
                        }
                        Directory.Move($"{m_DownloadPath}\\Extract", $"{ViewModel.InstallPath}\\Files");
                    }
                    catch { error = true; }
                });
                uiBtnInstall.IsEnabled = false;
                uiProgressRingInstall.IsActive = false;
                ((FrameworkElement)sender).IsEnabled = true;
                ViewModel.State = "Downloaded";
                ((TextBlock)uiBtnExtract.Child).Text = "Extract";
                if (error)
                {
                    MainWindowViewModel.Instance.AppBarText = "Something wen't wrong during installation";
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"Successfully installed '{ViewModel.Game.Title}'";
                }
            }
            else if (ViewModel.Game.Type == GameType.WINDOWS_SETUP)
            {
                string setupEexecutable = string.Empty;
                if (!Directory.Exists($"{m_DownloadPath}\\Extract"))
                    return;
                uiProgressRingInstall.IsActive = true;
                setupEexecutable = ((KeyValuePair<string, string>)uiCbSetupExecutable.SelectedValue).Value;
                if (File.Exists(setupEexecutable))
                {
                    Process setupProcess = null;
                    try
                    {
                        setupProcess = ProcessHelper.StartApp(setupEexecutable);
                    }
                    catch
                    {
                        try
                        {
                            setupProcess = ProcessHelper.StartApp(setupEexecutable, "",true);
                        }
                        catch
                        {
                            MainWindowViewModel.Instance.AppBarText = $"Can not execute '{setupEexecutable}'";
                        }
                    }
                    if (setupProcess != null)
                    {
                        await setupProcess.WaitForExitAsync();
                    }
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"Could not find executable '{setupEexecutable}'";
                }
                 ((FrameworkElement)sender).IsEnabled = true;
            }
            uiInstallOptions.Visibility = System.Windows.Visibility.Collapsed;
            uiProgressRingInstall.IsActive = false;
            uiBtnExtract.IsEnabled = true;
        }

        private void CopyInstallPathToClipboard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Clipboard.SetText(ViewModel.InstallPath);
                MainWindowViewModel.Instance.AppBarText = "Copied Installation Directory to Clipboard";
            }
            catch { }
        }
    }
}
