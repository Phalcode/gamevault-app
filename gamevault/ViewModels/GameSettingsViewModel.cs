using gamevault.Models;
using gamevault.Models.Mapping;
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
        private UpdateGameDto? updateGame { get; set; }
        private string directory { get; set; }
        private ObservableCollection<KeyValuePair<string, string>> m_Executables { get; set; }
        private string launchParameter { get; set; }
        private MinimalGame[]? remapSearchResults { get; set; }
        private bool backgroundImageChanged { get; set; }
        private bool boxArtImageChanged { get; set; }
        private ImageSource backgroundImageSource { get; set; }
        private ImageSource boxArtImageSource { get; set; }
        private string diskSize { get; set; }
        private MetadataProviderDto[]? metadataProviders { get; set; }
        private bool metadataProvidersLoaded { get; set; }
        private int selectedMetadataProviderIndex { get; set; }
        private string? installedGameVersion { get; set; }
        #endregion
        public Game Game
        {
            get { return game; }
            set { game = value; OnPropertyChanged(); }
        }
        public UpdateGameDto? UpdateGame
        {
            get { return updateGame; }
            set { updateGame = value; OnPropertyChanged(); }
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
        public MetadataProviderDto[]? MetadataProviders
        {
            get { return metadataProviders; }
            set { metadataProviders = value; OnPropertyChanged(); }
        }
        public bool MetadataProvidersLoaded
        {
            get { return metadataProvidersLoaded; }
            set { metadataProvidersLoaded = value; OnPropertyChanged(); }
        }
        public int SelectedMetadataProviderIndex
        {
            get { return selectedMetadataProviderIndex; }
            set { selectedMetadataProviderIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentShownMappedGame)); RemapSearchResults = null; }
        }
        public GameMetadata? CurrentShownMappedGame
        {
            get
            {
                try
                {
                    if (MetadataProviders?.Length > 0)
                    {
                        MetadataProviderDto currentSelectedProvider = MetadataProviders?[SelectedMetadataProviderIndex];
                        return Game.ProviderMetadata.Where(m => m.ProviderSlug == currentSelectedProvider.Slug).First();
                    }
                }
                catch { }
                return new GameMetadata();
            }
            set
            {
                OnPropertyChanged();
            }
        }
        public string? InstalledGameVersion
        {
            get { return installedGameVersion; }
            set { installedGameVersion = value; OnPropertyChanged(); }
        }
    }
}
