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

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for GameSettingsUserControl.xaml
    /// </summary>
    public partial class GameSettingsUserControl : UserControl
    {
        private bool startup = true;
        private GameSettingsViewModel ViewModel { get; set; }
        private string[] IgnoreList { get; set; }
        private string SavedExecutable { get; set; }
        internal GameSettingsUserControl(ObservableKeyValuePair gameParam, string[] ignoreList)
        {
            InitializeComponent();
            ViewModel = new GameSettingsViewModel();
            ViewModel.Game = gameParam;
           
            IgnoreList = ignoreList;
            this.DataContext = ViewModel;
            FindGameExecutables(ViewModel.Game.Value, true);
            if (Directory.Exists(ViewModel.Game.Value))
            {
                ViewModel.LaunchParameter = Preferences.Get(AppConfigKey.LaunchParameter, $"{ViewModel.Game.Value}\\gamevault-exec");
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
                if (startup)
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
            var parent = VisualHelper.FindNextParentByType<GameSettingsUserControl>(((FrameworkElement)sender)).Parent;
            if (parent.GetType() == typeof(Popup))
            {
                ((Popup)parent).IsOpen = false;
            }
        }

        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ViewModel.Game.Value))
                Process.Start("explorer.exe", ViewModel.Game.Value);
        }

        private async void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            if (ViewModel.Game.Key.Type == GameType.WINDOWS_PORTABLE)
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to uninstall '{ViewModel.Game.Key.Title}' ?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    try
                    {
                        if (Directory.Exists(ViewModel.Game.Value))
                            Directory.Delete(ViewModel.Game.Value, true);

                        NewInstallViewModel.Instance.InstalledGames.Remove(NewInstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.Key.ID).First());
                    }
                    catch
                    {
                        MainWindowViewModel.Instance.AppBarText = "Something went wrong when deleting the files. Maybe they are opened by another process.";
                    }
                }
            }
            else if (ViewModel.Game.Key.Type == GameType.WINDOWS_SETUP)
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to uninstall '{ViewModel.Game.Key.Title}' ?\nAs this is a Windows setup, you will need to select an uninstall executable", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    using (var dialog = new System.Windows.Forms.OpenFileDialog())
                    {
                        dialog.InitialDirectory = ViewModel.Game.Value;
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
                                    if (Directory.Exists(ViewModel.Game.Value))
                                        Directory.Delete(ViewModel.Game.Value, true);
                                }
                                catch { }
                                NewInstallViewModel.Instance.InstalledGames.Remove(NewInstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.Key.ID).First());
                            }
                        }
                    }
                }
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }

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
                SavedExecutable = Preferences.Get(AppConfigKey.Executable, $"{ViewModel.Game.Value}\\gamevault-exec");
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
                var currentItem = new KeyValuePair<string, string>(allExecutables[count], allExecutables[count].Substring(ViewModel.Game.Value.Length + 1));
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
            return (IgnoreList != null && IgnoreList.Any(s => Path.GetFileNameWithoutExtension(value).Contains(s, StringComparison.OrdinalIgnoreCase)));
        }
        private void ExecutableSelection_Opened(object sender, EventArgs e)
        {
            FindGameExecutables(ViewModel.Game.Value, false);
        }
        private void Executable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SavedExecutable = ((KeyValuePair<string, string>)e.AddedItems[0]).Key;
                if (Directory.Exists(ViewModel.Game.Value))
                {
                    Preferences.Set(AppConfigKey.Executable, SavedExecutable, $"{ViewModel.Game.Value}\\gamevault-exec");
                }
            }
        }

        private async void CreateDesktopShortcut_Click(object sender, MouseButtonEventArgs e)
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
            if (Directory.Exists(ViewModel.Game.Value))
            {
                Preferences.Set(AppConfigKey.LaunchParameter, ViewModel.LaunchParameter, $"{ViewModel.Game.Value}\\gamevault-exec");
            }
        }
        #region EDIT IMAGE
        private void BoxImage_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    uiUploadBoxArtPreview.Source = BitmapHelper.GetBitmapImage(files[0]);
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
                        uiUploadBoxArtPreview.Source = bitmap;
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
        private void BoxImage_ChooseImage(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).Focus();
            try
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                    {
                        uiUploadBoxArtPreview.Source = BitmapHelper.GetBitmapImage(dialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }

        private async void BoxImage_Save(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            try
            {
                string resp = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/images", BitmapHelper.BitmapSourceToMemeryStream((BitmapSource)uiUploadBoxArtPreview.Source), "x.png", null);
                var newImageId = JsonSerializer.Deserialize<gamevault.Models.Image>(resp).ID;
                await Task.Run(() =>
                {
                    try
                    {
                        dynamic updateObject = new System.Dynamic.ExpandoObject();
                        updateObject.box_image_id = newImageId;

                        WebHelper.Put($"{SettingsViewModel.Instance.ServerUrl}/api/games/{ViewModel.Game.Key.ID}", JsonSerializer.Serialize(updateObject));                       
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
                var temp = ViewModel.Game.Key;
                temp.BoxImage = new Models.Image() { ID = newImageId };
                ViewModel.Game.Key = null;
                ViewModel.Game.Key = temp;
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
            ((Button)sender).IsEnabled = true;
        }
        #endregion
    }
}
