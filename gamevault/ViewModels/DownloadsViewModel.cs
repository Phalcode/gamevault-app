using gamevault.Models;
using gamevault.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class DownloadsViewModel : ViewModelBase
    {
        #region Singleton
        private static DownloadsViewModel instance = null;
        private static readonly object padlock = new object();

        public static DownloadsViewModel Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DownloadsViewModel();
                    }
                    return instance;
                }
            }
        }
        #endregion
        #region PrivateMembers       
        private ObservableCollection<GameDownloadUserControl> m_DownloadedGames { get; set; }

        #endregion
       
        public ObservableCollection<GameDownloadUserControl> DownloadedGames
        {
            get
            {
                if (m_DownloadedGames == null)
                {
                    m_DownloadedGames = new ObservableCollection<GameDownloadUserControl>();
                }
                return m_DownloadedGames;
            }
            set { m_DownloadedGames = value; OnPropertyChanged(); }
        }
    }
}
