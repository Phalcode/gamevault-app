using gamevault.Models;
using gamevault.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace gamevault.ViewModels
{
    internal class GameDownloadViewModel : ViewModelBase
    {
        #region PrivateMembers
        private Game m_Game { get; set; }
        private string m_State { get; set; }
        private int m_GameDownloadProgress { get; set; }
        private Visibility m_DownloadUIVisibility { get; set; }
        private string m_DownloadRate { get; set; }
        private string m_TimeLeft { get; set; }
        private long? m_TotalBytesDownloaded { get; set; } = 0;
        #endregion

        public Game Game
        {
            get { return m_Game; }
            set { m_Game = value; OnPropertyChanged(); }
        }
        public string State
        {
            get { return m_State; }
            set { m_State = value; OnPropertyChanged(); }
        }
        public int GameDownloadProgress
        {
            get { return m_GameDownloadProgress; }
            set { m_GameDownloadProgress = value; OnPropertyChanged(); }
        }
        public Visibility DownloadUIVisibility
        {
            get { return m_DownloadUIVisibility; }
            set { m_DownloadUIVisibility = value; OnPropertyChanged(); }
        }
        public string DownloadRate
        {
            get { return m_DownloadRate; }
            set { m_DownloadRate = value; OnPropertyChanged(); }
        }
        public string TimeLeft
        {
            get { return m_TimeLeft; }
            set { m_TimeLeft = value; OnPropertyChanged(); }
        }
        public long? TotalBytesDownloaded
        {
            get { return m_TotalBytesDownloaded; }
            set { m_TotalBytesDownloaded = value; OnPropertyChanged(); }
        }
    }
}
