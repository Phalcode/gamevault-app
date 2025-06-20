using gamevault.Helper;
using gamevault.Models;
using gamevault.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace gamevault.ViewModels
{
    internal class LoginWindowViewModel : ViewModelBase
    {
        public ObservableCollection<UserProfile> UserProfiles { get; set; } = new ObservableCollection<UserProfile>();
        public LoginWindowViewModel()
        {
        }

        private int loginStepIndex { get; set; } = 0;
        public int LoginStepIndex
        {
            get { return loginStepIndex; }
            set { loginStepIndex = value; OnPropertyChanged(); }
        }
        private string statusText { get; set; }
        public string StatusText
        {
            get { return statusText; }
            set { statusText = value; OnPropertyChanged(); }
        }
        private BindableServerInfo loginServerInfo { get; set; } = new BindableServerInfo();
        public BindableServerInfo LoginServerInfo
        {
            get { return loginServerInfo; }
            set { loginServerInfo = value; OnPropertyChanged(); }
        }
        private LoginUser loginUser { get; set; }
        public LoginUser LoginUser
        {
            get
            {
                if (loginUser == null)
                {
                    loginUser = new LoginUser();
                }
                return loginUser;
            }
            set { loginUser = value; OnPropertyChanged(); }
        }
        private BindableServerInfo signUpServerInfo { get; set; } = new BindableServerInfo();
        public BindableServerInfo SignUpServerInfo
        {
            get { return signUpServerInfo; }
            set { signUpServerInfo = value; OnPropertyChanged(); }
        }
        private LoginUser signupUser { get; set; }
        public LoginUser SignupUser
        {
            get
            {
                if (signupUser == null)
                {
                    signupUser = new LoginUser();
                }
                return signupUser;
            }
            set { signupUser = value; OnPropertyChanged(); }
        }
        private LoginUser editUser { get; set; }
        public LoginUser EditUser
        {
            get
            {
                if (editUser == null)
                {
                    editUser = new LoginUser();
                }
                return editUser;
            }
            set { editUser = value; OnPropertyChanged(); }
        }
        private bool m_IsAppBarOpen { get; set; }
        public bool IsAppBarOpen
        {
            get { return m_IsAppBarOpen; }
            set { m_IsAppBarOpen = value; OnPropertyChanged(); }
        }
        private string m_AppBarText { get; set; }
        public string AppBarText
        {
            get { return m_AppBarText; }
            set { m_AppBarText = value; OnPropertyChanged(); IsAppBarOpen = true; }
        }
        private bool rememberMe { get; set; }
        public bool RememberMe
        {
            get { return rememberMe; }
            set
            {
                rememberMe = value; OnPropertyChanged();
                Preferences.Set(AppConfigKey.LoginRememberMe, rememberMe ? "1" : "0", ProfileManager.ProfileConfigFile);
            }
        }
        private ObservableCollection<RequestHeader> additionalRequestHeaders;
        public ObservableCollection<RequestHeader> AdditionalRequestHeaders
        {
            get
            {
                if (additionalRequestHeaders == null)
                {
                    additionalRequestHeaders = new ObservableCollection<RequestHeader>();
                }
                return additionalRequestHeaders;
            }
            set { additionalRequestHeaders = value; OnPropertyChanged(); }
        }
    }
}
