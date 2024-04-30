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
        private int m_GameExtractionProgress { get; set; }
        private int installationStepperProgress { get; set; } = -1;
        private string m_DownloadInfo { get; set; }
        private string m_ExtractionInfo { get; set; }
        private string m_InstallPath { get; set; }
        private bool m_IsDownloadPaused { get; set; }
        private Visibility m_DownloadUIVisibility { get; set; }
        private Visibility m_ExtractionUIVisibility { get; set; }
        private Visibility m_DownloadFailedVisibility { get; set; }       

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
        public int GameExtractionProgress
        {
            get { return m_GameExtractionProgress; }
            set { m_GameExtractionProgress = value; OnPropertyChanged(); }
        }
        public int InstallationStepperProgress
        {
            get { return installationStepperProgress; }
            set { installationStepperProgress = value; OnPropertyChanged(); }
        }
        public string DownloadInfo
        {
            get { return m_DownloadInfo; }
            set { m_DownloadInfo = value; OnPropertyChanged(); }
        }
        public string ExtractionInfo
        {
            get { return m_ExtractionInfo; }
            set { m_ExtractionInfo = value; OnPropertyChanged(); }
        }

        public Visibility DownloadUIVisibility
        {
            get { return m_DownloadUIVisibility; }
            set { m_DownloadUIVisibility = value; OnPropertyChanged(); }
        }
        public Visibility ExtractionUIVisibility
        {
            get { return m_ExtractionUIVisibility; }
            set { m_ExtractionUIVisibility = value; OnPropertyChanged(); }
        }
        public Visibility DownloadFailedVisibility
        {
            get { return m_DownloadFailedVisibility; }
            set { m_DownloadFailedVisibility = value; OnPropertyChanged(); }
        }
        public string InstallPath
        {
            get { return m_InstallPath; }
            set { m_InstallPath = value; OnPropertyChanged(); }
        }
        public bool IsDownloadPaused
        {
            get { return m_IsDownloadPaused; }
            set { m_IsDownloadPaused = value; OnPropertyChanged(); }
        }
        public string[] SupportedArchives
        {
            get
            {
                return new string[] { ".7z", ".xz", ".bz2", ".gz", ".tar", ".zip", ".wim", ".ar", ".arj", ".cab", ".chm", ".cpio", ".cramfs", ".dmg", ".ext", ".fat", ".gpt", ".hfs", ".ihex", ".iso", ".lzh", ".lzma", ".mbr", ".msi", ".nsis", ".ntfs", ".qcow2", ".rar", ".rpm", ".squashfs", ".udf", ".uefi", ".vdi", ".vhd", ".vmdk", ".wim", ".xar", ".z" };
            }
        }
    }
}
