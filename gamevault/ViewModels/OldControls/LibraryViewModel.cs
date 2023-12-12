using gamevault.Converter;
using gamevault.Models;
using gamevault.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace gamevault.ViewModels
{
    internal class LibraryViewModel : ViewModelBase
    {
        #region PrivateMembers       
        private string m_SearchQuery = "";
        private KeyValuePair<string, string> m_SelectedGameFilterSortBy { get; set; }
        private KeyValuePair<string, string> m_SelectedGameFilterOrderBy { get; set; }
        private KeyValuePair<string, string> m_SelectedGameFilterGameType { get; set; }
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
                    {"Title","title"},
                    {"Size","size" },
                    {"Recently Added","created_at" },
                    {"Release Date","release_date" },
                    {"Metacritic Rating","metacritic_rating" },
                    {"Average Playtime","average_playtime" },
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
        public KeyValuePair<string, string> SelectedGameFilterGameType
        {
            get { return m_SelectedGameFilterGameType; }
            set { m_SelectedGameFilterGameType = value; }
        }
        public Dictionary<string, string> GameFilterGameTypeValues
        {
            get
            {
                EnumDescriptionConverter conv = new EnumDescriptionConverter();
                var dict = new Dictionary<string, string>
                {
                    {"All","" },
                    { conv.Convert(GameType.WINDOWS_SETUP,null,null,null).ToString(),"WINDOWS_SETUP"},
                    { conv.Convert(GameType.WINDOWS_PORTABLE,null,null,null).ToString(),"WINDOWS_PORTABLE" },
                    {"Other","UNDETECTABLE" }
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
