using gamevault.Converter;
using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
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
    public partial class GameDownloadUserControl : UserControl
    {
        private DownloadSpeedCalculator downloadSpeedCalc { get; set; }
        private GameDownloadViewModel ViewModel { get; set; }
        private bool IsDownloadActive = false;

        private string m_DownloadPath { get; set; }
        private bool extractionCancelled = false;
        private HttpClientDownloadWithProgress client { get; set; }
        private DateTime startTime;

        private SevenZipHelper sevenZipHelper { get; set; }

        private GameSizeConverter gameSizeConverter { get; set; }
        private InputTimer downloadRetryTimer { get; set; }
        private bool isGameTypeForced = false;
        private double downloadRetryTimerTickValue = 10;
        private string mountedDrive = "";

        public GameDownloadUserControl(Game game, string rootDirectory, bool download)
        {
            InitializeComponent();
            ViewModel = new GameDownloadViewModel();
            this.DataContext = ViewModel;
            ViewModel.Game = game;
            ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.DownloadFailedVisibility = System.Windows.Visibility.Hidden;

            m_DownloadPath = $"{rootDirectory}\\GameVault\\Downloads\\({ViewModel.Game.ID}){ViewModel.Game.Title}";
            m_DownloadPath = m_DownloadPath.Replace(@"\\", @"\");
            ViewModel.InstallPath = $"{rootDirectory}\\GameVault\\Installations\\({ViewModel.Game.ID}){ViewModel.Game.Title}";
            ViewModel.InstallPath = ViewModel.InstallPath.Replace(@"\\", @"\");
            sevenZipHelper = new SevenZipHelper();
            gameSizeConverter = new GameSizeConverter();
            InitRetryTimer();
            if (download)
            {
                _ = DownloadGame();
            }
            else
            {
                UpdateDataSizeUI();
                if (File.Exists($"{m_DownloadPath}\\Extract\\gamevault-metadata") && Preferences.Get(AppConfigKey.ExtractionFinished, $"{m_DownloadPath}\\Extract\\gamevault-metadata") == "1")
                {
                    ViewModel.State = "Extracted";
                    uiBtnExtract.IsEnabled = true;
                    uiBtnInstall.IsEnabled = true;
                    uiBtnExtract.Text = "Re-Extract";
                    ViewModel.InstallationStepperProgress = 1;
                }
                else
                {
                    //Try resume paused UI
                    if (TryRecreatePausedUI())
                        return;
                    //If no valid pause data, its downloaded
                    ViewModel.State = "Downloaded";
                    uiBtnExtract.IsEnabled = true;
                    ViewModel.InstallationStepperProgress = 0;
                }
            }
        }
        public void Refresh(Game game)
        {
            ViewModel.Game = game;
        }
        private void UpdateDataSizeUI()
        {
            Task.Run(() =>
            {
                try
                {
                    double size = 0;
                    foreach (FileInfo file in new DirectoryInfo(m_DownloadPath).GetFiles("*", SearchOption.AllDirectories))
                    {
                        file.Refresh();
                        size += file.Length;
                    }
                    ViewModel.TotalDataSize = size;
                }
                catch { }
            });

        }
        private bool TryRecreatePausedUI()
        {
            try
            {
                string resumeData = Preferences.Get(AppConfigKey.DownloadProgress, $"{m_DownloadPath}\\gamevault-metadata");
                if (!string.IsNullOrEmpty(resumeData))
                {
                    string[] resumeDataToProcess = resumeData.Split(";");
                    var resumePos = long.Parse(resumeDataToProcess[0]);
                    var preResumeSize = long.Parse(resumeDataToProcess[1]);
                    double progressPercentage = Math.Round((double)resumePos / preResumeSize * 100, 0);
                    DownloadProgress(preResumeSize, 0, resumePos, progressPercentage, resumePos);
                    ViewModel.IsDownloadPaused = true;
                    ViewModel.DownloadUIVisibility = Visibility.Visible;
                    ViewModel.State = "Download Paused";
                    return true;
                }
            }
            catch { }

            return false;
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
        public int GetCoverID()
        {
            return ViewModel.Game.Metadata.Cover.ID;
        }
        public int GetDownloadProgress()
        {
            return ViewModel.GameDownloadProgress;
        }
        public void CancelDownload()
        {
            if (client == null)
            {
                try
                {
                    File.Delete($"{m_DownloadPath}\\gamevault-metadata");
                    File.Delete($"{m_DownloadPath}\\{Path.GetFileName(ViewModel.Game.Path)}");
                }
                catch { }
            }
            else
            {
                client.Cancel();
            }
            //client.Dispose();
            IsDownloadActive = false;
            ViewModel.IsDownloadPaused = false;
            ViewModel.State = "Download Cancelled";
            ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.DownloadFailedVisibility = System.Windows.Visibility.Visible;

            MainWindowViewModel.Instance.UpdateTaskbarProgress();
        }
        private async Task DownloadGame(bool tryResume = false)
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
            client = new HttpClientDownloadWithProgress($"{SettingsViewModel.Instance.ServerUrl}/api/games/{ViewModel.Game.ID}/download", m_DownloadPath, Path.GetFileName(ViewModel.Game.Path), header);
            client.ProgressChanged += DownloadProgress;
            startTime = DateTime.Now;
            downloadSpeedCalc = new DownloadSpeedCalculator();
            try
            {
                await client.StartDownload(tryResume);
                await CacheHelper.CreateOfflineCacheAsync(ViewModel.Game);
            }
            catch (Exception ex)
            {
                IsDownloadActive = false;
                client.Dispose();
                ViewModel.State = $"Error: '{ex.Message}'";
                ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
                ViewModel.DownloadFailedVisibility = System.Windows.Visibility.Visible;

                if (downloadRetryTimer.Data != "error")
                {
                    if (!App.Instance.IsWindowActiveAndControlInFocus(MainControl.Downloads))
                        ToastMessageHelper.CreateToastMessage("Download Failed", ViewModel.Game.Title, $"{LoginManager.Instance.GetUserProfile().ImageCacheDir}/gbox/{ViewModel.Game.ID}.{ViewModel.Game.Metadata.Cover?.ID}");
                }
                StartRetryTimer();
                MainWindowViewModel.Instance.UpdateTaskbarProgress();
            }
        }
        private void StartRetryTimer()
        {
            downloadRetryTimer.Interval = TimeSpan.FromSeconds(downloadRetryTimerTickValue);
            downloadRetryTimerTickValue *= 2;
            downloadRetryTimer.Start();
        }
        private void InitRetryTimer()
        {
            downloadRetryTimer = new InputTimer();
            downloadRetryTimer.Tick += AutoRetryDownload_Tick;
        }
        private void AutoRetryDownload_Tick(object sender, EventArgs e)
        {
            downloadRetryTimer?.Stop();
            downloadRetryTimer.Data = "error";
            RetryDownload();
        }
        private void RetryDownload_Click(object sender, RoutedEventArgs e)
        {
            downloadRetryTimer?.Stop();
            downloadRetryTimer.Data = "";
            RetryDownload();
        }
        private void RetryDownload()
        {
            if (IsDownloading())
                return;

            ViewModel.DownloadInfo = string.Empty;
            ViewModel.GameDownloadProgress = 0;
            _ = DownloadGame(true);
        }
        private void PauseResume_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsDownloadPaused)
            {
                ViewModel.IsDownloadResumed = false;
                ViewModel.IsDownloadPaused = false;
                _ = DownloadGame(true);
            }
            else
            {
                if (client == null)
                    return;

                ViewModel.IsDownloadPaused = true;
                client.Pause();
                IsDownloadActive = false;
                ViewModel.State = "Download Paused";
            }
        }
        public void PauseDownload()
        {
            if (client == null || ViewModel.IsDownloadPaused || !IsDownloadActive)
                return;

            ViewModel.IsDownloadPaused = true;
            client.Pause();
            IsDownloadActive = false;
            ViewModel.State = "Download Paused";
        }
        private void DownloadProgress(long totalFileSize, long currentBytesDownloaded, long totalBytesDownloaded, double? progressPercentage, long resumePosition)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                bool isResume = resumePosition != -1;
                var numerator = isResume ? currentBytesDownloaded : totalBytesDownloaded;
                var denumirator = isResume ? totalFileSize - resumePosition : totalFileSize;
                if (isResume)
                {
                    ViewModel.IsDownloadResumed = true;
                }
                if (currentBytesDownloaded == 0)
                {
                    ViewModel.DownloadInfo = $"{FormatBytesHumanReadable(totalBytesDownloaded)}" + $" of {FormatBytesHumanReadable((double)totalFileSize)}";
                }
                else
                {
                    downloadSpeedCalc.UpdateSpeed(currentBytesDownloaded);
                    ViewModel.DownloadInfo = $"{$"{FormatBytesHumanReadable(downloadSpeedCalc.GetCurrentSpeed(), 1, 1000)}/s"}" +
               $" - {FormatBytesHumanReadable(totalBytesDownloaded)}" +
               $" of {FormatBytesHumanReadable((double)totalFileSize)}" +
               $" | Time left: {CalculateTimeLeft(denumirator, numerator, (DateTime.Now - startTime).TotalMilliseconds)}";
                }

                if (ViewModel.GameDownloadProgress == (int)progressPercentage)
                {
                    return;
                }
                ViewModel.GameDownloadProgress = (int)progressPercentage;
                if (ViewModel.GameDownloadProgress == 100)
                {
                    DownloadCompleted();
                }
                MainWindowViewModel.Instance.UpdateTaskbarProgress();
            });
        }

        private void DownloadCompleted()
        {
            if (client == null)
                return;

            UpdateDataSizeUI();
            ViewModel.DownloadUIVisibility = System.Windows.Visibility.Hidden;
            client.Dispose();
            IsDownloadActive = false;
            ViewModel.State = "Downloaded";
            uiBtnExtract.IsEnabled = true;
            ViewModel.InstallationStepperProgress = 0;
            try
            {
                if (File.Exists($"{m_DownloadPath}\\gamevault-metadata"))
                    File.Delete($"{m_DownloadPath}\\gamevault-metadata");
            }
            catch { }
            if (!Directory.Exists(ViewModel.InstallPath))
            {
                Directory.CreateDirectory(ViewModel.InstallPath);
            }
            MainWindowViewModel.Instance.Library.GetGameInstalls().AddSystemFileWatcher(ViewModel.InstallPath);

            if (!App.Instance.IsWindowActiveAndControlInFocus(MainControl.Downloads))
                ToastMessageHelper.CreateToastMessage("Download Complete", ViewModel.Game.Title, $"{LoginManager.Instance.GetUserProfile().ImageCacheDir}/gbox/{ViewModel.Game.ID}.{ViewModel.Game.Metadata?.Cover?.ID}");

            if (SettingsViewModel.Instance.AutoExtract)
            {
                App.Current.Dispatcher.Invoke((Action)async delegate
                {
                    uiBtnExtract.IsEnabled = false;
                    await Task.Delay(3000);
                    await Extract();
                    uiBtnExtract.IsEnabled = true;
                });
            }
        }

        private void CancelDownload_Click(object sender, RoutedEventArgs e)
        {
            CancelDownload();
        }
        private void CancelExtraction_Click(object sender, RoutedEventArgs e)
        {
            extractionCancelled = true;
            sevenZipHelper.Cancel();
        }
        private string FormatBytesHumanReadable(double size, double tspan = 1, int baseVal = 1024)
        {
            try
            {
                double value = size / tspan;
                return (string)gameSizeConverter.Convert(value.ToString(), null, baseVal, null);
            }
            catch (Exception ex)
            {
                return "ERR";
            }
        }

        private string CalculateTimeLeft(long? totalFileSize, long totalBytesRead, double tspanMilliseconds)
        {
            double timeLeftSeconds = (double)((totalFileSize - totalBytesRead) / downloadSpeedCalc.GetCurrentSpeed());
            TimeSpan t = TimeSpan.FromSeconds(0);
            if (timeLeftSeconds > 0 && !double.IsInfinity(timeLeftSeconds) && !double.IsNaN(timeLeftSeconds))
            {
                t = TimeSpan.FromSeconds(timeLeftSeconds);
            }
            return string.Format("{0:00}:{1:00}:{2:00}", ((int)t.TotalHours), t.Minutes, t.Seconds);
        }

        private async void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            await DeleteFile(confirm: true);
        }

        public async Task DeleteFile(bool confirm = true)
        {
            if (IsDownloadActive)
            {
                MainWindowViewModel.Instance.AppBarText = "Can not delete during the download, extraction, installing process";
                return;
            }

            bool doDelete = true;

            if (confirm)
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to delete '{(ViewModel.Game == null ? "this Game" : ViewModel.Game.Title)}' ?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });

                doDelete = result == MessageDialogResult.Affirmative;
            }

            if (doDelete)
            {
                try
                {
                    downloadRetryTimer?.Stop();

                    if (Directory.Exists(m_DownloadPath))
                        Directory.Delete(m_DownloadPath, true);

                    DownloadsViewModel.Instance.DownloadedGames.Remove(this);

                    //Delete Installation Directory if it is empty (Because GV should not create a filewatcher for it anymore)
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
        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(m_DownloadPath))
                Process.Start("explorer.exe", m_DownloadPath);
        }

        private void GoToGame_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(ViewModel.Game, LoginManager.Instance.IsLoggedIn()));
        }

        private void ExtractionProgress(object sender, SevenZipProgressEventArgs e)
        {
            try
            {
                ViewModel.GameExtractionProgress = e.PercentageDone;
                long totalBytesDownloaded = (Convert.ToInt64(ViewModel.Game.Size) / 100) * e.PercentageDone;
                downloadSpeedCalc.UpdateSpeed(totalBytesDownloaded);
                ViewModel.ExtractionInfo = $"{$"{FormatBytesHumanReadable(totalBytesDownloaded, (DateTime.Now - startTime).TotalSeconds, 1000)}/s"} - {FormatBytesHumanReadable(totalBytesDownloaded)} of {FormatBytesHumanReadable(Convert.ToInt64(ViewModel.Game.Size))} | Time left: {CalculateTimeLeft(Convert.ToInt64(ViewModel.Game.Size), totalBytesDownloaded, (DateTime.Now - startTime).TotalMilliseconds)}";
            }
            catch { }
        }
        private async void Extract_Click(object sender, RoutedEventArgs e)
        {
            await Extract();
        }
        private async Task<string> MountISO(string ISOPath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -NoProfile -Command \"$diskImage = Mount-DiskImage -ImagePath '{ISOPath}' -PassThru; ($diskImage | Get-Volume).DriveLetter\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(psi))
                {
                    await process.WaitForExitAsync();
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    return string.IsNullOrEmpty(output) ? string.Empty : output + @":\";
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
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

            //Mount ISO if possible
            if (SettingsViewModel.Instance.MountIso && Path.GetExtension(Path.Combine(m_DownloadPath, files[0].Name)).Equals(".iso", StringComparison.OrdinalIgnoreCase))
            {
                uiBtnExtract.IsEnabled = false;
                mountedDrive = await MountISO(Path.Combine(m_DownloadPath, files[0].Name));
                if (Directory.Exists(mountedDrive))
                {
                    ViewModel.State = $"ISO mounted at {mountedDrive}";
                    ViewModel.InstallationStepperProgress = 1;
                    uiBtnExtract.IsEnabled = true;
                    uiBtnInstall.IsEnabled = true;
                    return;
                }
                ViewModel.State = "Failed to Mount ISO";
                uiBtnExtract.IsEnabled = true;
                return;
            }
            //

            ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;
            ViewModel.State = "Extracting...";
            ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Visible;
            downloadSpeedCalc = new DownloadSpeedCalculator();//Reuse download speed calculator as extraction speed calculator and set new instance to reset it
            sevenZipHelper.Process += ExtractionProgress;
            startTime = DateTime.Now;
            int result;
            bool isEncrypted = await sevenZipHelper.IsArchiveEncrypted($"{m_DownloadPath}\\{files[0].Name}");
            if (isEncrypted)
            {
                string extractionPassword = Preferences.Get(AppConfigKey.ExtractionPassword, LoginManager.Instance.GetUserProfile().UserConfigFile, true);
                if (string.IsNullOrEmpty(extractionPassword))
                {
                    extractionPassword = await ((MetroWindow)App.Current.MainWindow).ShowInputAsync("Exctraction Message", "Your Archive reqires a Password to extract");
                    result = await sevenZipHelper.ExtractArchive($"{m_DownloadPath}\\{files[0].Name}", $"{m_DownloadPath}\\Extract", extractionPassword);
                }
                else
                {
                    result = await sevenZipHelper.ExtractArchive($"{m_DownloadPath}\\{files[0].Name}", $"{m_DownloadPath}\\Extract", extractionPassword);
                    if (result == 69)//Error code for wrong password
                    {
                        extractionPassword = await ((MetroWindow)App.Current.MainWindow).ShowInputAsync("Exctraction Message", "Your Archive reqires a Password to extract");
                        result = await sevenZipHelper.ExtractArchive($"{m_DownloadPath}\\{files[0].Name}", $"{m_DownloadPath}\\Extract", extractionPassword);
                    }
                }
            }
            else
            {
                result = await sevenZipHelper.ExtractArchive($"{m_DownloadPath}\\{files[0].Name}", $"{m_DownloadPath}\\Extract");
            }
            if (result == 0)
            {
                if (!File.Exists($"{m_DownloadPath}\\Extract\\gamevault-metadata"))
                {
                    File.Create($"{m_DownloadPath}\\Extract\\gamevault-metadata").Close();
                }
                Preferences.Set(AppConfigKey.ExtractionFinished, "1", $"{m_DownloadPath}\\Extract\\gamevault-metadata");
                ViewModel.State = "Extracted";
                uiBtnExtract.Text = "Re-Extract";

                ViewModel.InstallationStepperProgress = 1;
                ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;

                if (!App.Instance.IsWindowActiveAndControlInFocus(MainControl.Downloads))
                    ToastMessageHelper.CreateToastMessage("Extraction Complete", ViewModel.Game.Title, $"{LoginManager.Instance.GetUserProfile().ImageCacheDir}/gbox/{ViewModel.Game?.ID}.{ViewModel.Game?.Metadata?.Cover?.ID}");

                if (SettingsViewModel.Instance.AutoInstallPortable && (ViewModel.Game?.Type == GameType.WINDOWS_PORTABLE || ViewModel.Game?.Type == GameType.LINUX_PORTABLE))
                {
                    await Task.Delay(1000);//Just to be sure the extraction stream is closed and the files are ready to copy
                    await Install();
                }
                else
                {
                    uiBtnInstall.IsEnabled = true;
                }

                UpdateDataSizeUI();
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
                else if (result == 69)
                {
                    ViewModel.State = "Error: Wrong password";
                }
                else
                {
                    ViewModel.State = "Something went wrong during extraction";
                    if (!App.Instance.IsWindowActiveAndControlInFocus(MainControl.Downloads))
                        ToastMessageHelper.CreateToastMessage("Extraction Failed", ViewModel.Game.Title, $"{LoginManager.Instance.GetUserProfile().ImageCacheDir}/gbox/{ViewModel.Game?.ID}.{ViewModel.Game?.Metadata?.Cover?.ID}");
                }
                ViewModel.ExtractionUIVisibility = System.Windows.Visibility.Hidden;
            }
        }
        private void OpenInstallOptions_Click(object sender, RoutedEventArgs e)
        {
            uiInstallOptions.Visibility = System.Windows.Visibility.Visible;
            LoadSetupExecutables();
        }
        private void InstallOptionCancel_Click(object sender, RoutedEventArgs e)
        {
            uiInstallOptions.Visibility = System.Windows.Visibility.Collapsed;
        }
        private void LoadSetupExecutables()
        {
            string targedDir = (SettingsViewModel.Instance.MountIso && Directory.Exists(mountedDrive)) ? mountedDrive : $"{m_DownloadPath}\\Extract";
            if (Directory.Exists(targedDir))
            {
                Dictionary<string, string> allExecutables = new Dictionary<string, string>();
                foreach (string fileType in Globals.SupportedExecutables)
                {
                    foreach (string entry in Directory.GetFiles(targedDir, $"*.{fileType}", SearchOption.AllDirectories))
                    {
                        string keyToAdd = Path.GetFileName(entry);
                        if (!allExecutables.ContainsKey(keyToAdd))
                        {
                            allExecutables.Add(keyToAdd, entry);
                        }
                        else
                        {
                            allExecutables.Add(entry.Replace(targedDir, ""), entry); ;
                        }
                    }
                }
                uiCbSetupExecutable.ItemsSource = allExecutables;
                if (!string.IsNullOrWhiteSpace(ViewModel.Game?.Metadata?.InstallerExecutable))
                {
                    var entry = allExecutables.Select((kv, index) => new { kv.Key, kv.Value, Index = index }).FirstOrDefault(kv => kv.Key.Contains(ViewModel.Game?.Metadata?.InstallerExecutable.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase));
                    if (entry != null)
                        uiCbSetupExecutable.SelectedIndex = entry.Index;
                }
                else if (allExecutables.Count == 1)
                {
                    uiCbSetupExecutable.SelectedIndex = 0;
                }
            }
            else
            {
                uiCbSetupExecutable.ItemsSource = null;
            }
        }
        private async void Install_Click(object s, RoutedEventArgs e)
        {
            await Install();
        }
        private async Task Install()
        {
            if (InstallViewModel.Instance.InstalledGames.Any(game => game.Key.ID == ViewModel.Game.ID))
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"The Game {ViewModel.Game.Title} is already installed at \n'{InstallViewModel.Instance.InstalledGames.First(game => game.Key.ID == ViewModel.Game.ID).Value}'" +
                       $"\nWarning: Overwriting an existing installation with a new one may cause data corruption.", "",
                       MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Continue", NegativeButtonText = "Cancel", AnimateHide = false });

                if (result == MessageDialogResult.Negative)
                    return;
            }
            string targedDir = (SettingsViewModel.Instance.MountIso && Directory.Exists(mountedDrive)) ? mountedDrive : $"{m_DownloadPath}\\Extract";

            uiBtnInstallPortable.IsEnabled = false;
            uiBtnInstallSetup.IsEnabled = false;
            uiBtnExtract.IsEnabled = false;
            try
            {
                if (!Directory.Exists(ViewModel.InstallPath))//make sure install path exists with file watcher attached
                {
                    Directory.CreateDirectory(ViewModel.InstallPath);
                }
                MainWindowViewModel.Instance.Library.GetGameInstalls().AddSystemFileWatcher(ViewModel.InstallPath);
            }
            catch { }
            if (ViewModel.Game.Type == GameType.WINDOWS_PORTABLE || ViewModel.Game.Type == GameType.LINUX_PORTABLE)
            {
                bool error = false;
                uiProgressRingInstall.IsActive = true;
                await Task.Run(() =>
                {
                    try
                    {
                        if (!Directory.Exists(ViewModel.InstallPath))
                        {
                            Directory.CreateDirectory(ViewModel.InstallPath);
                        }
                        else if (Directory.Exists($"{ViewModel.InstallPath}\\Files"))
                        {
                            Directory.Delete($"{ViewModel.InstallPath}\\Files", true);
                        }
                        if (Path.GetPathRoot(targedDir) == targedDir)
                        {
                            MoveFromRootPath(targedDir, $"{ViewModel.InstallPath}\\Files");
                        }
                        else
                        {
                            Directory.Move(targedDir, $"{ViewModel.InstallPath}\\Files");
                        }
                    }
                    catch { error = true; }
                });
                uiBtnInstall.IsEnabled = false;
                uiProgressRingInstall.IsActive = false;

                uiBtnInstallPortable.IsEnabled = true;
                uiBtnInstallSetup.IsEnabled = true;

                ViewModel.State = "Downloaded";
                uiBtnExtract.Text = "Extract";
                if (error)
                {
                    MainWindowViewModel.Instance.AppBarText = "Something wen't wrong during installation";
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"Successfully installed '{ViewModel.Game.Title}'";
                    ViewModel.InstallationStepperProgress = 2;
                    ViewModel.State = "Installed";

                    //Auto delete files of portable games after successful installation
                    if (SettingsViewModel.Instance.AutoDeletePortableGameFiles)
                    {
                        await DeleteFile(false);
                    }
                }
            }
            else if (ViewModel.Game.Type == GameType.WINDOWS_SETUP)
            {
                string setupEexecutable = string.Empty;
                if (!Directory.Exists(targedDir))
                    return;
                uiProgressRingInstall.IsActive = true;
                setupEexecutable = ((KeyValuePair<string, string>)uiCbSetupExecutable.SelectedValue).Value;
                if (File.Exists(setupEexecutable))
                {
                    Process setupProcess = null;
                    try
                    {
                        setupProcess = ProcessHelper.StartApp(setupEexecutable, ViewModel.Game?.Metadata?.InstallerParameters?.Replace("%INSTALLDIR%", ViewModel.InstallPath));
                    }
                    catch
                    {
                        try
                        {
                            setupProcess = ProcessHelper.StartApp(setupEexecutable, ViewModel.Game?.Metadata?.InstallerParameters?.Replace("%INSTALLDIR%", ViewModel.InstallPath), true);
                        }
                        catch
                        {
                            MainWindowViewModel.Instance.AppBarText = $"Can not execute '{setupEexecutable}'";
                        }
                    }
                    if (setupProcess != null)
                    {
                        await setupProcess.WaitForExitAsync();
                        ViewModel.InstallationStepperProgress = 2;
                        if (InstallViewModel.Instance.InstalledGames.Any(g => g.Key.ID == ViewModel.Game.ID))
                        {
                            ViewModel.InstallationStepperProgress = 2;
                            ViewModel.State = "Installed";
                        }
                    }
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"Could not find executable '{setupEexecutable}'";
                }
                uiBtnInstallPortable.IsEnabled = true;
                uiBtnInstallSetup.IsEnabled = true;
            }
            uiInstallOptions.Visibility = System.Windows.Visibility.Collapsed;
            uiProgressRingInstall.IsActive = false;
            uiBtnExtract.IsEnabled = true;
            try
            {
                Preferences.Set(AppConfigKey.InstalledGameVersion, ViewModel?.Game?.Version, $"{ViewModel.InstallPath}\\gamevault-exec");
            }
            catch { }
            //Save forced install type for uninstallation
            if (isGameTypeForced && Directory.Exists(ViewModel.InstallPath) && ViewModel?.Game?.Type != null)
            {
                try
                {
                    Preferences.Set(AppConfigKey.ForcedInstallationType, ViewModel?.Game?.Type, $"{ViewModel.InstallPath}\\gamevault-exec");
                }
                catch { }
            }
            //Set default launch parameter if available
            if (!string.IsNullOrWhiteSpace(ViewModel.Game?.Metadata?.LaunchParameters) && Directory.Exists(ViewModel.InstallPath))
            {
                try
                {
                    Preferences.Set(AppConfigKey.LaunchParameter, ViewModel.Game?.Metadata?.LaunchParameters, $"{ViewModel.InstallPath}\\gamevault-exec");
                }
                catch { }
            }
            //Set default launch executable if available
            if (!string.IsNullOrWhiteSpace(ViewModel.Game?.Metadata?.LaunchExecutable) && Directory.Exists(ViewModel.InstallPath))
            {
                try
                {
                    string extension = Path.GetExtension(ViewModel.Game?.Metadata?.LaunchExecutable);
                    var files = Directory.GetFiles(ViewModel.InstallPath, $"*{extension}", SearchOption.AllDirectories);
                    var targetFile = files.FirstOrDefault(file => file.Contains(ViewModel.Game?.Metadata?.LaunchExecutable.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase));
                    if (targetFile != null)
                    {
                        Preferences.Set(AppConfigKey.Executable, targetFile, $"{ViewModel.InstallPath}\\gamevault-exec");
                    }
                }
                catch { }
            }

            if (ViewModel.CreateShortcut == true)
            {
                await Task.Delay(1000);
                var game = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.ID).FirstOrDefault();
                if (game.Key == null || !Directory.Exists(game.Value))
                    return;

                if (!File.Exists(Preferences.Get(AppConfigKey.Executable, $"{game.Value}\\gamevault-exec")))
                {
                    if (!GameSettingsUserControl.TryPrepareLaunchExecutable(game.Value))
                    {
                        MainWindowViewModel.Instance.AppBarText = $"Can not create shortcut. No valid Executable found";
                        return;
                    }
                }
                await DesktopHelper.CreateShortcut(game.Key, Preferences.Get(AppConfigKey.Executable, $"{game.Value}\\gamevault-exec"), false);
            }
        }
        public void MoveFromRootPath(string sourceDir, string destinationDir)
        {
            // Create the destination directory if it doesn't exist.
            Directory.CreateDirectory(destinationDir);

            // Copy all files.
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destinationDir, fileName);
                // You can use overwrite option if necessary
                File.Copy(filePath, destFilePath, overwrite: true);
            }

            // Recursively copy subdirectories.
            foreach (string dirPath in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dirPath);
                string destSubDir = Path.Combine(destinationDir, dirName);
                MoveFromRootPath(dirPath, destSubDir);
            }

            // Do not attempt to delete source since it is on a read-only ISO.
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

        private void ContinueOverwriteGameType_Click(object sender, RoutedEventArgs e)
        {
            if (uiCbOverwriteGameType.SelectedValue != null)
            {
                var temp = ViewModel.Game;
                temp.Type = (GameType)uiCbOverwriteGameType.SelectedValue;
                ViewModel.Game = null;
                ViewModel.Game = temp;
                isGameTypeForced = true;
            }
            else
            {
                MainWindowViewModel.Instance.AppBarText = "No gametype selected for overwriting";
            }
        }

        private void InitOverwriteGameType_Click(object sender, RoutedEventArgs e)
        {
            var temp = ViewModel.Game;
            temp.Type = GameType.UNDETECTABLE;
            ViewModel.Game = null;
            ViewModel.Game = temp;
        }
    }
}
