using crackpipe.Helper;
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
using System.Windows.Shapes;

namespace crackpipe.Windows
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow
    {
        private StoreHelper m_StoreHelper;
        public UpdateWindow()
        {
            InitializeComponent();
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                m_StoreHelper = new StoreHelper();
                if (true == await m_StoreHelper.UpdatesAvailable())
                {
                    await m_StoreHelper.DownloadAndInstallAllUpdatesAsync(this);
                }
                uiTxtStatus.Text = "Optimizing cache...";              
                await CacheHelper.OptimizeCache();
            }
            catch (Exception ex)
            {
                //No error log, because it would trigger every time, if the user is offline
            }
            this.Close();
        }
    }
}
