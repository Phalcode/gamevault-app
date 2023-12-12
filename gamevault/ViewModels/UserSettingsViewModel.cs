using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class UserSettingsViewModel : ViewModelBase
    {
        #region Privates
        private User user { get; set; }   
        private bool userDetailsChanged { get; set; }   
        #endregion
        public User User
        {
            get { return user; }
            set { user = value; OnPropertyChanged(); }
        }
        public bool UserDetailsChanged
        {
            get { return userDetailsChanged; }
            set { userDetailsChanged = value; OnPropertyChanged(); }
        }
    }
}
