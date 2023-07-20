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

        public User[]? Users
        {
            get { return m_Users; }
            set { m_Users = value; OnPropertyChanged(); }
        }       
        public Array PermissionRoleEnumTypes
        {
            get
            {
                return Enum.GetValues(typeof(PERMISSION_ROLE));
            }          
        }
    }
}
