using gamevault.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class InstallViewModel : ViewModelBase
    {
        #region Singleton
        private static InstallViewModel instance = null;
        private static readonly object padlock = new object();

        public static InstallViewModel Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new InstallViewModel();
                    }
                    return instance;
                }
            }
        }
        #endregion
        #region PrivateMembers      
        private ObservableCollection<GameInstallUserControl> m_InstalledGames { get; set; }
        #endregion
        
        public ObservableCollection<GameInstallUserControl> InstalledGames
        {
            get
            {
                if (m_InstalledGames == null)
                {
                    m_InstalledGames = new ObservableCollection<GameInstallUserControl>();
                }
                return m_InstalledGames;
            }
            set { m_InstalledGames = value; OnPropertyChanged(); }
        }
    }
}
