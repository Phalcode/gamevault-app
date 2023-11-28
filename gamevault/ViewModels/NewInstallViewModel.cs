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
    internal class NewInstallViewModel : ViewModelBase
    {
        #region Singleton
        private static NewInstallViewModel instance = null;
        private static readonly object padlock = new object();

        public static NewInstallViewModel Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new NewInstallViewModel();
                    }
                    return instance;
                }
            }
        }
        #endregion
        #region PrivateMembers      
        private ObservableCollection<ObservableKeyValuePair> m_InstalledGames { get; set; }
        #endregion      
        public ObservableCollection<ObservableKeyValuePair> InstalledGames
        {
            get
            {
                if (m_InstalledGames == null)
                {
                    m_InstalledGames = new ObservableCollection<ObservableKeyValuePair>();
                }
                return m_InstalledGames;
            }
            set { m_InstalledGames = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ObservableKeyValuePair> InstalledGamesOrigin {  get; set; }
    }
}
