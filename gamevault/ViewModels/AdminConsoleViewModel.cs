using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class AdminConsoleViewModel : ViewModelBase
    {
        private User[] m_Users { get; set; }
        private bool showDeletedUsers { get; set; }
        private KeyValuePair<string, string> m_ServerVersionInfo { get; set; }

        public User[]? Users
        {
            get { return m_Users; }
            set { m_Users = value; OnPropertyChanged(); }
        }
        public bool ShowDeletedUsers
        {
            get { return showDeletedUsers; }
            set { showDeletedUsers = value; OnPropertyChanged(); }
        }
        public Array PermissionRoleEnumTypes
        {
            get
            {
                return Enum.GetValues(typeof(PERMISSION_ROLE));
            }
        }
        public KeyValuePair<string, string> ServerVersionInfo
        {
            get { return m_ServerVersionInfo; }
            set { m_ServerVersionInfo = value; OnPropertyChanged(); }
        }
    }
}
