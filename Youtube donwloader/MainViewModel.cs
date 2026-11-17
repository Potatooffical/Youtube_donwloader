using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Youtube_donwloader.Models;
using Youtube_donwloader.Services;

namespace Youtube_donwloader.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly YtDlpService _service;
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private string _videoUrl = string.Empty;

        [ObservableProperty]
        private string _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        [ObservableProperty]
        private string _selectedFormat = "Max resolution (4K)";

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private string _logText = "Waiting for a download...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private VideoMetadata? _currentMetadata;

        [ObservableProperty]
        private bool _hasMetadata;

        public ObservableCollection<string> Formats { get; } = new()
        {
            "Max resolution (4K)",
            "1080p",
            "720p",
            "Audio Only (MP3)"
        };

        public MainViewModel()
        {
            _service = new YtDlpService();
            _service.LogReceived += (msg) => LogText += $"{msg}\n";
            _service.ProgressChanged += (val) => ProgressValue = val;
        }

        partial void OnVideoUrlChanged(string value)
        {
            _ = FetchMetadata(value);
        }

        private async Task FetchMetadata(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("http")) 
            {
                CurrentMetadata = null;
                HasMetadata = false;
                return;
            }

            try
            {
      
                LogText += $"[System] Fetching metadata for: {value}...\n";
                
                var meta = await _service.GetVideoMetadataAsync(value);
                if (meta != null)
                {
                    CurrentMetadata = meta;
                    HasMetadata = true;
                    LogText += $"[System] Video identified: {meta.Title}\n";
                }
            }
            catch (Exception ex)
            {
                LogText += $"[Error] Metadata fetch failed: {ex.Message}\n";
            }
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            
            var dialog = new OpenFolderDialog(); 
            if (dialog.ShowDialog() == true)
            {
                DownloadPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private async Task Download()
        {
            if (string.IsNullOrEmpty(VideoUrl)) return;

            IsBusy = true;
            ProgressValue = 0;
            LogText = "[System] Starting download...\n";
            _cts = new CancellationTokenSource();

            try
            {
                await _service.DownloadVideoAsync(VideoUrl, DownloadPath, SelectedFormat, _cts.Token);
                LogText += "[System] Done!\n";
                MessageBox.Show("Download completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogText += $"[Error] {ex.Message}\n";
            }
            finally
            {
                IsBusy = false;
                _cts = null;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cts?.Cancel();
        }
    }
}