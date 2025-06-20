using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


namespace gamevault.UserControls.SettingsComponents
{
    /// <summary>
    /// Interaction logic for BackupRestoreUserControl.xaml
    /// </summary>
    public partial class BackupRestoreUserControl : UserControl
    {
        public BackupRestoreUserControl()
        {
            InitializeComponent();

        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                (uiBackupDirectoryScrollViewer.Template.FindName("PART_HorizontalScrollBar", uiBackupDirectoryScrollViewer) as ScrollBar).Height = 7;
            }
            catch { }
        }
        private void BackupRestorePopup_Close(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }

        private void ChooseBackupDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
                {
                    uiBackupDirectory.Text = dialog.SelectedPath;
                    Backup_PasswordChanged(null, null);
                }
            }
        }
        private void Backup_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (uiBackupDatabasePassword.Password.Length > 0 && uiBackupDirectory.Text != string.Empty)
            {
                uiBtnStartBackup.IsEnabled = true;
            }
            else
            {
                uiBtnStartBackup.IsEnabled = false;
            }
        }

        private async void StartBackup_Click(object sender, RoutedEventArgs e)
        {
            uiBtnStartBackup.IsEnabled = false;
            this.IsEnabled = false;
            try
            {
                HttpClientDownloadWithProgress httpClientDownloadWithProgress = new HttpClientDownloadWithProgress(@$"{SettingsViewModel.Instance.ServerUrl}/api/admin/database/backup",
                    uiBackupDirectory.Text, $"DB_Backup_{DateTime.Now.ToString()}.db", new KeyValuePair<string, string>("X-Database-Password", uiBackupDatabasePassword.Password));

                await httpClientDownloadWithProgress.StartDownload();
                MainWindowViewModel.Instance.AppBarText = "Successfully performed database backup.";
            }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.StatusCode == HttpStatusCode.Unauthorized)
                {
                    MainWindowViewModel.Instance.AppBarText = "Unauthorized. Either the database password is wrong or you are not logged in.";
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"Http Error: {httpEx.Message}";
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = $"Error: {ex.Message}";
            }
            uiBtnStartBackup.IsEnabled = true;
            this.IsEnabled = true;
        }
        //### RESTORE ###
        private void ChooseRestoreFile_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Filter = "Database File|*.db";
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                {
                    uiRestoreFile.Tag = dialog.FileName;
                    uiRestoreFile.Text = Path.GetFileName(dialog.FileName);
                    Restore_PasswordChanged(null, null);
                }
            }
        }

        private void Restore_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (uiRestoreDatabasePassword.Password.Length > 0 && uiRestoreFile.Text != string.Empty)
            {
                uiBtnStartRestore.IsEnabled = true;
            }
            else
            {
                uiBtnStartRestore.IsEnabled = false;
            }
        }

        private async void StartRestore_Click(object sender, RoutedEventArgs e)
        {
            uiBtnStartRestore.IsEnabled = false;
            this.IsEnabled = false;
            try
            {
                await WebHelper.UploadFileAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/admin/database/restore", File.OpenRead(uiRestoreFile.Tag.ToString()), uiRestoreFile.Text, new RequestHeader[] { new RequestHeader() { Name = "X-Database-Password", Value = uiRestoreDatabasePassword.Password } });
                MainWindowViewModel.Instance.AppBarText = "Successfully uploaded database file";
            }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.StatusCode == HttpStatusCode.Unauthorized)
                {
                    MainWindowViewModel.Instance.AppBarText = "Unauthorized. Either the database password is wrong or you are not logged in.";
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"Http Error: {httpEx.Message}";
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = $"Error: {ex.Message}";
            }
            uiBtnStartRestore.IsEnabled = true;
            this.IsEnabled = true;
        }
    }
}
