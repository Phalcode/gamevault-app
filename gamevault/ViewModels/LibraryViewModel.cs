using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace gamevault.ViewModels
{
    internal class LibraryViewModel : ViewModelBase
    {
        #region PrivateMembers      
        private ObservableCollection<Game> gameCards { get; set; }
        private int totalGamesCount = -1;
        private Visibility scrollToTopVisibility { get; set; }
        private Visibility filterVisibility = Visibility.Collapsed;
        private KeyValuePair<string, string> m_SelectedGameFilterSortBy { get; set; }
        private string filterCounter { get; set; } = string.Empty;
        private bool canLoadServerGames { get; set; } = true;
        #endregion
        public ObservableCollection<Game> GameCards
        {
            get
            {
                if (gameCards == null)
                {
                    gameCards = new ObservableCollection<Game>();
                }
                return gameCards;
            }
            set { gameCards = value; }
        }
        public int TotalGamesCount
        {
            get { return totalGamesCount; }
            set { totalGamesCount = value; OnPropertyChanged(); }
        }
        public Visibility ScrollToTopVisibility
        {
            get { return scrollToTopVisibility; }
            set { scrollToTopVisibility = value; OnPropertyChanged(); }
        }
        public Visibility FilterVisibility
        {
            get { return filterVisibility; }
            set { filterVisibility = value; OnPropertyChanged(); }
        }
        public string? NextPage { get; set; }
        public Dictionary<string, string> GameFilterSortByValues
        {
            get
            {
                var dict = new Dictionary<string, string>
                {
                    {"Title","sort_title"},
                    {"Size","size" },
                    {"Date Added","created_at" },
                    {"Release Date","metadata.release_date" },
                    {"Rating","metadata.rating" },
                    {"Download Count","download_count" },
                    {"Average Playtime","metadata.average_playtime" },
                };
                return dict;
            }
        }
        public KeyValuePair<string, string> SelectedGameFilterSortBy
        {
            get { return m_SelectedGameFilterSortBy; }
            set { m_SelectedGameFilterSortBy = value; }
        }
        public string FilterCounter
        {
            get { return filterCounter; }
            set { filterCounter = value; OnPropertyChanged(); }
        }
        public bool CanLoadServerGames
        {
            get { return canLoadServerGames; }
            set { canLoadServerGames = value; OnPropertyChanged(); }
        }
    }
}
