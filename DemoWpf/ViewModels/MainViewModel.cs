﻿using System;
using System.IO;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Tyrrrz.Extensions;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace DemoWpf.ViewModels
{
    public class MainViewModel : ViewModelBase, IMainViewModel
    {
        private readonly YoutubeClient _client;

        private bool _isBusy;
        private string _videoId;
        private Video _video;
        private double _progress;
        private bool _isProgressIndeterminate;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                Set(ref _isBusy, value);
                GetVideoInfoCommand.RaiseCanExecuteChanged();
                DownloadMediaStreamCommand.RaiseCanExecuteChanged();
            }
        }

        public string VideoId
        {
            get => _videoId;
            set
            {
                Set(ref _videoId, value);
                GetVideoInfoCommand.RaiseCanExecuteChanged();
            }
        }

        public Video Video
        {
            get => _video;
            private set
            {
                Set(ref _video, value);
                RaisePropertyChanged(() => IsVideoInfoAvailable);
            }
        }

        public bool IsVideoInfoAvailable => Video != null;

        public double Progress
        {
            get => _progress;
            private set => Set(ref _progress, value);
        }

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            private set => Set(ref _isProgressIndeterminate, value);
        }

        // Commands
        public RelayCommand GetVideoInfoCommand { get; }
        public RelayCommand<MediaStreamInfo> DownloadMediaStreamCommand { get; }
        public RelayCommand<ClosedCaptionTrackInfo> DownloadClosedCaptionTrackCommand { get; }

        public MainViewModel(YoutubeClient client)
        {
            _client = client;

            // Commands
            GetVideoInfoCommand = new RelayCommand(GetVideoInfoAsync, () => !IsBusy && VideoId.IsNotBlank());
            DownloadMediaStreamCommand = new RelayCommand<MediaStreamInfo>(DownloadMediaStreamAsync, vse => !IsBusy);
            DownloadClosedCaptionTrackCommand = new RelayCommand<ClosedCaptionTrackInfo>(
                DownloadClosedCaptionTrackAsync, vse => !IsBusy);
        }

        private async void GetVideoInfoAsync()
        {
            // Check params
            if (VideoId.IsBlank())
                return;

            IsBusy = true;
            IsProgressIndeterminate = true;

            // Reset data
            Video = null;

            // Parse URL if necessary
            if (!YoutubeClient.TryParseVideoId(VideoId, out string id))
                id = VideoId;

            // Perform the request
            Video = await _client.GetVideoAsync(id);

            IsBusy = false;
            IsProgressIndeterminate = false;
        }

        private async void DownloadMediaStreamAsync(MediaStreamInfo mediaStreamInfo)
        {
            // Create dialog
            var fileExtension = mediaStreamInfo.Container.GetFileExtension();
            var defaultFileName = $"{Video.Title}.{fileExtension}";
            defaultFileName = defaultFileName.Except(Path.GetInvalidFileNameChars());
            var fileFilter =
                $"{mediaStreamInfo.Container} Files|*.{fileExtension}|" +
                "All files|*.*";
            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = fileExtension,
                FileName = defaultFileName,
                Filter = fileFilter
            };

            // Select destination
            if (sfd.ShowDialog() != true) return;
            var filePath = sfd.FileName;

            // Download and save to file
            IsBusy = true;
            Progress = 0;

            var progressHandler = new Progress<double>(p => Progress = p);
            await _client.DownloadMediaStreamAsync(mediaStreamInfo, filePath, progressHandler);

            IsBusy = false;
            Progress = 0;
        }

        private async void DownloadClosedCaptionTrackAsync(ClosedCaptionTrackInfo closedCaptionTrackInfo)
        {
            // Create dialog
            var defaultFileName = $"{Video.Title}.{closedCaptionTrackInfo.Language.Name}.srt";
            defaultFileName = defaultFileName.Except(Path.GetInvalidFileNameChars());
            var fileFilter =
                "SRT Files|*.srt|" +
                "All files|*.*";
            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "srt",
                FileName = defaultFileName,
                Filter = fileFilter
            };
            // Select destination
            if (sfd.ShowDialog() != true) return;
            var filePath = sfd.FileName;

            // Download
            IsBusy = true;
            Progress = 0;

            var progressHandler = new Progress<double>(p => Progress = p);
            await _client.DownloadClosedCaptionTrackAsync(closedCaptionTrackInfo, filePath, progressHandler);

            IsBusy = false;
            Progress = 0;
        }
    }
}