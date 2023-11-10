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
    internal class NewLibraryViewModel : ViewModelBase
    {
        #region PrivateMembers      
        private ObservableCollection<Game> gameCards { get; set; }
        private int totalGamesCount = -1;
        private Visibility scrollToTopVisibility { get; set; }
        private Visibility filterVisibility = Visibility.Collapsed;
        private KeyValuePair<string, string> m_SelectedGameFilterSortBy { get; set; }
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
                    {"Game title","title"},
                    {"Game size","size" },
                    {"Game release date","rawg_release_date" },
                    {"Game rating","metacritic_rating" },
                    {"Game average playtime","average_playtime" },
                    {"Recently added","created_at" }
                };
                return dict;
            }
        }
        public KeyValuePair<string, string> SelectedGameFilterSortBy
        {
            get { return m_SelectedGameFilterSortBy; }
            set { m_SelectedGameFilterSortBy = value; }
        }
        public string OrderByValue = "ASC";
    }
}
