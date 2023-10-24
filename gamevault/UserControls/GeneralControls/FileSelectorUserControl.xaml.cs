using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
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
using gamevault.Helper;
using System.IO;
using System.Collections;
using gamevault.ViewModels;
using gamevault.Models;
using System.Text.Json;
using Image = gamevault.Models.Image;
using System.Drawing;
using System.Drawing.Imaging;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for FileSelectorUserControl.xaml
    /// </summary>
    public partial class FileSelectorUserControl : UserControl
    {
        public static readonly DependencyProperty ImageIdProperty = DependencyProperty.Register("ImageId", typeof(long), typeof(FileSelectorUserControl));

        public long ImageId
        {
            get { return (long)GetValue(ImageIdProperty); }
            set { SetValue(ImageIdProperty, value); }
        }

        public static readonly DependencyProperty ImageUrlProperty = DependencyProperty.Register("ImageUrl", typeof(string), typeof(FileSelectorUserControl));

        public string ImageUrl
        {
            get { return (string)GetValue(ImageUrlProperty); }
            set { SetValue(ImageUrlProperty, value); }
        }
        private bool m_IsUploading = false;
        private bool IsUploading
        {
            get { return m_IsUploading; }
            set
            {
                m_IsUploading = value;
                if (m_IsUploading)
                {
                    uiUploadBlocker.Visibility = Visibility.Visible;
                }
                else
                {
                    uiUploadBlocker.Visibility = Visibility.Collapsed;
                }
            }
        }
        public FileSelectorUserControl()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();//get paste focus
        }
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.Focus();
        }

        private async void OnPaste(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.V && IsUploading == false)
                {
                    try
                    {
                        IsUploading = true;
                        if (Clipboard.ContainsImage())
                        {
                            var image = Clipboard.GetImage();

                            System.Drawing.Bitmap bitmap;
                            using (MemoryStream pasteStream = new MemoryStream())
                            {
                                using (MemoryStream outStream = new MemoryStream())
                                {
                                    BitmapEncoder enc = new PngBitmapEncoder();
                                    enc.Frames.Add(BitmapFrame.Create(image));
                                    enc.Save(outStream);
                                    bitmap = new System.Drawing.Bitmap(outStream);
                                    bitmap.Save(pasteStream, ImageFormat.Png);
                                }
                                pasteStream.Seek(0, SeekOrigin.Begin);
                                string resp = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/images", pasteStream, "x.png", null);
                                ImageId = JsonSerializer.Deserialize<Image>(resp).ID;
                                MainWindowViewModel.Instance.AppBarText = "Sucessfully uploaded image";
                            }
                        }
                        else
                        {
                            ArrayList filesArray = (ArrayList)Clipboard.GetFileDropList().SyncRoot;
                            string response = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/images", File.OpenRead(filesArray[0].ToString()), System.IO.Path.GetFileName(filesArray[0].ToString()), null);
                            ImageId = JsonSerializer.Deserialize<Image>(response).ID;
                            MainWindowViewModel.Instance.AppBarText = "Sucessfully uploaded image";
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = ex.Message;
                    }
                    IsUploading = false;
                }
            }
        }

        private async void Upload_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                    {
                        IsUploading = true;
                        string response = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/images", File.OpenRead(dialog.FileName), System.IO.Path.GetFileName(dialog.FileName), null);
                        ImageId = JsonSerializer.Deserialize<Image>(response).ID;
                        MainWindowViewModel.Instance.AppBarText = "Sucessfully uploaded image";
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
            IsUploading = false;
        }

        private async void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var file = files[0];
                    IsUploading = true;
                    string response = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/images", File.OpenRead(file), System.IO.Path.GetFileName(file), null);
                    ImageId = JsonSerializer.Deserialize<Image>(response).ID;
                    MainWindowViewModel.Instance.AppBarText = "Sucessfully uploaded image";
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
                }
                IsUploading = false;
            }
        }
    }
}
