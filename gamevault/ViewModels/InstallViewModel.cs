using gamevault.Models;
using gamevault.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private ObservableCollection<KeyValuePair<Game, string>> m_InstalledGames { get; set; }
        public ICollectionView? installedGamesFilter { get; set; }
        private int rows { get; set; } = 0;
        private int colums { get; set; } = 0;
        #endregion      
        public ObservableCollection<KeyValuePair<Game, string>> InstalledGames
        {
            get
            {
                if (m_InstalledGames == null)
                {
                    m_InstalledGames = new ObservableCollection<KeyValuePair<Game, string>>();
                }
                return m_InstalledGames;
            }
            set { m_InstalledGames = value; OnPropertyChanged(); }
        }
        public Dictionary<int,string> InstalledGamesDuplicates= new Dictionary<int, string>();
        public ICollectionView? InstalledGamesFilter
        {
            get { return installedGamesFilter; }
            set { installedGamesFilter = value; OnPropertyChanged(); }
        }
        public void RefreshGame(Game gameToRefreshParam)
        {
            KeyValuePair<Game, string> gameToRefresh = InstalledGames.Where(g => g.Key.ID == gameToRefreshParam.ID).FirstOrDefault();
            if (!gameToRefresh.Equals(default(KeyValuePair<Game, string>)))
            {
                int index = InstalledGames.IndexOf(gameToRefresh);
                InstalledGames[index] = new KeyValuePair<Game, string>(gameToRefreshParam, gameToRefresh.Value);
            }
        }
        public int Rows
        {
            get { return rows; }

            set
            {
                rows = value; OnPropertyChanged();
            }
        }
        public int Colums
        {
            get { return colums; }

            set
            {
                colums = value; OnPropertyChanged();
            }
        }
    }
}
