using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using Markdig;
using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for NewsPopup.xaml
    /// </summary>
    public partial class NewsPopup : UserControl
    {
        public NewsPopup()
        {
            InitializeComponent();
        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            try
            {
                string gameVaultNews = await WebHelper.GetAsync("https://gamevau.lt/news.md");
                uiGameVaultNews.Markdown = gameVaultNews;
                string serverNews = await WebHelper.GetAsync($"{SettingsViewModel.Instance.ServerUrl}/api/config/news");
                uiServerNews.Markdown = serverNews;
            }
            catch { }
        }
        #region Markdown        
        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Uri.IsWellFormedUriString(e.Parameter.ToString(), UriKind.Absolute))
                {
                    Process.Start(new ProcessStartInfo(e.Parameter.ToString()) { UseShellExecute = true });
                }
            }
            catch { }
        }
        #endregion

        private void OnClose(object sender, object e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }
    }
}
