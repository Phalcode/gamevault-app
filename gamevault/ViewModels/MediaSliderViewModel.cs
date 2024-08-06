using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace gamevault.ViewModels
{
    internal class MediaSliderViewModel : ViewModelBase
    {
        private bool isPlaying { get; set; }     
        private MediaPlayer mediaPlayer { get; set; }
        private TimeSpan currentTime { get; set; }        
        private long currentTimeInMilliseconds { get; set; }        
        private long currentLenghtInMilliseconds { get; set; }        
        private TimeSpan currentLenght { get; set; }
        private Visibility videoControlBarVisibility { get; set; }
        public bool IsPlaying
        {
            get { return isPlaying; }
            set { isPlaying = value; OnPropertyChanged(); }
        }      
        public MediaPlayer MediaPlayer
        {
            get { return mediaPlayer; }
            set { mediaPlayer = value; OnPropertyChanged(); }
        }
        public TimeSpan CurrentTime
        {
            get { return currentTime; }
            set { currentTime = value; OnPropertyChanged(); }
        }
        public long CurrentTimeInMilliseconds
        {
            get { return currentTimeInMilliseconds; }
            set { currentTimeInMilliseconds = value; OnPropertyChanged(); }
        }
        public TimeSpan CurrentLenght
        {
            get { return currentLenght; }
            set { currentLenght = value; OnPropertyChanged(); }
        }
        public long CurrentLenghtInMilliseconds
        {
            get { return currentLenghtInMilliseconds; }
            set { currentLenghtInMilliseconds = value; OnPropertyChanged(); }
        }
        public Visibility VideoControlBarVisibility
        {
            get { return videoControlBarVisibility; }
            set { videoControlBarVisibility = value; OnPropertyChanged(); }
        }
    }
}
