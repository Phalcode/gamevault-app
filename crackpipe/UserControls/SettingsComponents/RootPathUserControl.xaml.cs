using crackpipe.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace crackpipe.UserControls.SettingsComponents
{
    /// <summary>
    /// Interaction logic for RootPathUserControl.xaml
    /// </summary>
    public partial class RootPathUserControl : UserControl
    {
        public RootPathUserControl()
        {
            InitializeComponent();
        }
        private void RootPath_Click(object sender, RoutedEventArgs e)
        {
            SettingsViewModel.Instance.SelectDownloadPath();
        }
    }
}
