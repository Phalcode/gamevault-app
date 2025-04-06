using gamevault.Helper;
using gamevault.Models;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Networking.NetworkOperators;

namespace gamevault.Windows
{
    /// <summary>
    /// Interaction logic for ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow
    {
        public ExceptionWindow()
        {
            InitializeComponent();
        }

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            string path = "";
            if (App.IsWindowsPackage)
            {
                path = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/Packages/{Encoding.UTF8.GetString(Convert.FromBase64String("UGhhbGNvZGUuMTc0OTUwQkQ4MUM0MV9keW1zZ24zcXBmanhj"))}/LocalCache/Roaming/GameVault/errorlog";
                path = path.Replace("/", @"\");
                path = path.Replace("\\", "/");
                path = path.Replace("/", @"\");
            }
            else
            {
                path = ProfileManager.ErrorLogDir.Replace(@"\\", @"\").Replace("/", @"\");
            }
            if (!Directory.Exists(path))
            {
                path = ProfileManager.ErrorLogDir.Replace(@"\\", @"\").Replace("/", @"\");
            }
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path);
            }
            this.Close();
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
