using gamevault.Models;
using gamevault.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace gamevault.ViewModels
{
    internal class LibraryViewModel : ViewModelBase
    {
        #region PrivateMembers       
        private string m_SearchQuery = "";
        private KeyValuePair<string, string> m_SelectedGameFilterSortBy { get; set; }
        private KeyValuePair<string, string> m_SelectedGameFilterOrderBy { get; set; }
        private ObservableCollection<Game> m_GameCards { get; set; }
        private bool m_IsSearchEnabled = true;
        private bool m_EarlyAccessOnly = false;
        private int m_YearFilterUpper { get; set; } 
        private int m_YearFilterLower { get; set; }
        private int m_TotalGamesCount = -1;

        #endregion

        public string SearchQuery
        {
            get { return m_SearchQuery; }
            set { m_SearchQuery = value; OnPropertyChanged(); }
        }
        public KeyValuePair<string, string> SelectedGameFilterSortBy
        {
            get { return m_SelectedGameFilterSortBy; }
            set { m_SelectedGameFilterSortBy = value; }
        }
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
        public KeyValuePair<string, string> SelectedGameFilterOrderBy
        {
            get { return m_SelectedGameFilterOrderBy; }
            set { m_SelectedGameFilterOrderBy = value; }
        }
        public Dictionary<string, string> GameFilterOrderByValues
        {
            get
            {
                var dict = new Dictionary<string, string>
                {
                    {"Ascending","ASC"},
                    {"Descending","DESC" }
                };
                return dict;
            }
        }
        public ObservableCollection<Game> GameCards
        {
            get
            {
                if (m_GameCards == null)
                {
                    m_GameCards = new ObservableCollection<Game>();
                }
                return m_GameCards;
            }
            set { m_GameCards = value; }
        }
        public bool IsSearchEnabled
        {
            get { return m_IsSearchEnabled; }
            set { m_IsSearchEnabled = value; OnPropertyChanged(); }
        }
        public bool EarlyAccessOnly
        {
            get { return m_EarlyAccessOnly; }
            set { m_EarlyAccessOnly = value; OnPropertyChanged(); }
        }
        public int YearFilterUpper
        {
            get { return m_YearFilterUpper; }
            set { m_YearFilterUpper = value; OnPropertyChanged(); }
        }
        public int YearFilterLower
        {
            get { return m_YearFilterLower; }
            set { m_YearFilterLower = value; OnPropertyChanged(); }
        }
        public int TotalGamesCount
        {
            get { return m_TotalGamesCount; }
            set { m_TotalGamesCount = value; OnPropertyChanged(); }
        }
    }
}
