using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class NewLibraryViewModel : ViewModelBase
    {
        #region PrivateMembers      
        private ObservableCollection<Game> m_GameCards { get; set; }
        private int m_TotalGamesCount = -1;
        #endregion
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
        public int TotalGamesCount
        {
            get { return m_TotalGamesCount; }
            set { m_TotalGamesCount = value; OnPropertyChanged(); }
        }
        public string NextPage {  get; set; }
    }
}
