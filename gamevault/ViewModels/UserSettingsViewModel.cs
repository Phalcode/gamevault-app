using gamevault.Models;
using gamevault.Models.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace gamevault.ViewModels
{
    internal class UserSettingsViewModel : ViewModelBase
    {
        #region Privates
        private User originUser { get; set; }
        private UpdateUserDto updateUser { get; set; }
        private bool userDetailsChanged { get; set; }
        private bool backgroundImageChanged { get; set; }
        private bool avatarImageChanged { get; set; }
        private ImageSource backgroundImageSource { get; set; }
        private string avatarImageUrl { get; set; }
        #endregion
        public User OriginUser
        {
            get { return originUser; }
            set { originUser = value; OnPropertyChanged(); }
        }
        public UpdateUserDto UpdateUser
        {
            get { return updateUser; }
            set { updateUser = value; OnPropertyChanged(); }
        }
        public bool UserDetailsChanged
        {
            get { return userDetailsChanged; }
            set { userDetailsChanged = value; OnPropertyChanged(); }
        }
        public bool BackgroundImageChanged
        {
            get { return backgroundImageChanged; }
            set { backgroundImageChanged = value; OnPropertyChanged(); }
        }
        public bool AvatarImageChanged
        {
            get { return avatarImageChanged; }
            set { avatarImageChanged = value; OnPropertyChanged(); }
        }
        public ImageSource BackgroundImageSource
        {
            get { return backgroundImageSource; }
            set { backgroundImageSource = value; OnPropertyChanged(); BackgroundImageChanged = true; }
        }
        public string AvatarImageUrl
        {
            get { return avatarImageUrl; }
            set { avatarImageUrl = value; OnPropertyChanged(); AvatarImageChanged = true; }
        }
    }
}
