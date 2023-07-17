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
        private Game[]? m_RawgGames { get; set; }
        private Game? m_Game { get; set; }
        private string[]? m_States { get; set; }
        private Progress? m_Progress { get; set; }
        private string m_RawgSearchQuery { get; set; }
        private string m_UpdatedBoxImage { get; set; }
        private bool m_GameRemapPopupIsOpen { get; set; }      
        private bool m_IsAlreadyInstalled { get; set; }

        #endregion
        public Game[]? RawgGames
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
        public string? UpdatedBoxImage
        {
            get { return m_UpdatedBoxImage; }
            set { m_UpdatedBoxImage = value; OnPropertyChanged(); }
        }
        public bool GameRemapPopupIsOpen
        {
            get { return m_GameRemapPopupIsOpen; }
            set { m_GameRemapPopupIsOpen = value; OnPropertyChanged(); }
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
