using gamevault.Helper;
using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace gamevault.ViewModels
{
    internal class GameViewViewModel : ViewModelBase
    {
        #region PrivateMembers
        private RawgGame[]? m_RawgGames { get; set; }
        private Game? m_Game { get; set; }
        private string[]? m_States { get; set; }
        private Progress? m_Progress { get; set; }
        private string m_RawgSearchQuery { get; set; }
        private string m_UpdatedBoxImageUrl { get; set; }
        private long? m_UpdatedBoxImageId { get; set; }
        private string m_UpdatedBackgroundImageUrl { get; set; }
        private long? m_UpdatedBackgroundImageId { get; set; }
        private Visibility m_GameRemapPopupVisibillity = Visibility.Collapsed;  
        private bool m_IsAlreadyInstalled { get; set; }

        #endregion
        public RawgGame[]? RawgGames
        {
            get { return m_RawgGames; }
            set { m_RawgGames = value; OnPropertyChanged(); }
        }
        public Game? Game
        {
            get { return m_Game; }
            set { m_Game = value; OnPropertyChanged(); }
        }
        public string[]? States
        {
            get { return m_States; }
            set { m_States = value; OnPropertyChanged(); }
        }
        public Progress? Progress
        {
            get { return m_Progress; }
            set { m_Progress = value; OnPropertyChanged(); }
        }
        public string? RawgSearchQuery
        {
            get { return m_RawgSearchQuery; }
            set { m_RawgSearchQuery = value; OnPropertyChanged(); }
        }
        public string? UpdatedBoxImageUrl
        {
            get { return m_UpdatedBoxImageUrl; }
            set { m_UpdatedBoxImageUrl = value; OnPropertyChanged(); }
        }
        public long? UpdatedBoxImageId
        {
            get { return m_UpdatedBoxImageId; }
            set { m_UpdatedBoxImageId = value; OnPropertyChanged(); }
        }
        public string? UpdatedBackgroundImageUrl
        {
            get { return m_UpdatedBackgroundImageUrl; }
            set { m_UpdatedBackgroundImageUrl = value; OnPropertyChanged(); }
        }
        public long? UpdatedBackgroundImageId
        {
            get { return m_UpdatedBackgroundImageId; }
            set { m_UpdatedBackgroundImageId = value; OnPropertyChanged(); }
        }
        public Visibility GameRemapPopupVisibillity
        {
            get { return m_GameRemapPopupVisibillity; }
            set { m_GameRemapPopupVisibillity = value; OnPropertyChanged(); }
        }
        public Visibility CanEditGame
        {
            get
            {
                if (LoginManager.Instance.IsLoggedIn() && LoginManager.Instance.GetCurrentUser().Role >= PERMISSION_ROLE.EDITOR)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }            
        }
        public bool IsAlreadyInstalled
        {
            get { return m_IsAlreadyInstalled; }
            set { m_IsAlreadyInstalled = value; OnPropertyChanged(); }
        }
    }
}
