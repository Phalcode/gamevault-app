using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class CommunityViewModel : ViewModelBase
    {
        #region PrivateMembers

        private User[]? m_Users { get; set; }
        private User? m_CurrentShownUser { get; set; }
        private List<Progress> m_UserProgresses { get; set; }
        private bool loadingUser { get; set; }

        #endregion
        public User[]? Users
        {
            get { return m_Users; }
            set { m_Users = value; OnPropertyChanged(); }
        }
        public User? CurrentShownUser
        {
            get { return m_CurrentShownUser; }
            set
            {
                m_CurrentShownUser = value;
                UserProgresses = new List<Progress>(m_CurrentShownUser.Progresses);
                m_CurrentShownUser.Progresses = null;
                OnPropertyChanged();
            }
        }

        public List<Progress> UserProgresses
        {
            get
            {
                if (m_UserProgresses == null)
                {
                    m_UserProgresses = new List<Progress>();
                }
                return m_UserProgresses;
            }
            set
            {
                m_UserProgresses = value; OnPropertyChanged();
            }
        }
        public bool LoadingUser
        {
            get { return loadingUser; }
            set { loadingUser = value; OnPropertyChanged(); }
        }
        public string[] SortBy
        {
            get
            {
                return new string[] { "State", "Time played", "Last played" };
            }
        }

    }
}

