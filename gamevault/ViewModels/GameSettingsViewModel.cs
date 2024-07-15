using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace gamevault.ViewModels
{
    internal class GameSettingsViewModel : ViewModelBase
    {
        #region Privates

        private Game game { get; set; }
        private string directory { get; set; }
        private ObservableCollection<KeyValuePair<string, string>> m_Executables { get; set; }
        private string launchParameter { get; set; }
        private MinimalGame[]? remapSearchResults { get; set; }
        private bool backgroundImageChanged { get; set; }
        private bool boxArtImageChanged { get; set; }
        private ImageSource backgroundImageSource { get; set; }
        private ImageSource boxArtImageSource { get; set; }
        private string diskSize { get; set; }
        #endregion      
        public Game Game
        {
            get { return game; }
            set { game = value; OnPropertyChanged(); }
        }
        public string Directory
        {
            get { return directory; }
            set { directory = value; OnPropertyChanged(); }
        }
        public ObservableCollection<KeyValuePair<string, string>> Executables
        {
            get
            {
                if (m_Executables == null)
                {
                    m_Executables = new ObservableCollection<KeyValuePair<string, string>>();
                }
                return m_Executables;
            }
            set { m_Executables = value; OnPropertyChanged(); }
        }
        public string LaunchParameter
        {
            get { return launchParameter; }
            set { launchParameter = value; OnPropertyChanged(); }
        }
        public MinimalGame[]? RemapSearchResults
        {
            get { return remapSearchResults; }
            set { remapSearchResults = value; OnPropertyChanged(); }
        }
        public bool BackgroundImageChanged
        {
            get { return backgroundImageChanged; }
            set { backgroundImageChanged = value; OnPropertyChanged(); }
        }
        public bool GameCoverImageChanged
        {
            get { return boxArtImageChanged; }
            set { boxArtImageChanged = value; OnPropertyChanged(); }
        }
        public ImageSource BackgroundImageSource
        {
            get { return backgroundImageSource; }
            set { backgroundImageSource = value; OnPropertyChanged(); BackgroundImageChanged = true; }
        }
        public ImageSource GameCoverImageSource
        {
            get { return boxArtImageSource; }
            set { boxArtImageSource = value; OnPropertyChanged(); GameCoverImageChanged = true; }
        }
        public string DiskSize
        {
            get { return diskSize; }
            set { diskSize = value; OnPropertyChanged(); }
        }
    }
}
