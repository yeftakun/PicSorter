using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PicSorter.Core.Models;
using PicSorter.Core.Services;
using PicSorter.Core.ViewModels;
using Wpf.Ui.Controls;

namespace PicSorter.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        private readonly AppSettingsService _settingsService;
        private readonly AppSettings _settings;

        public MainWindow(AppSettings settings, AppSettingsService settingsService)
        {
            _settings = settings;
            _settingsService = settingsService;

            InitializeComponent();
            _viewModel = new MainViewModel(_settings, SaveSettings);
            DataContext = _viewModel;

            // Apply window bounds
            if (!double.IsNaN(_settings.WindowWidth) && _settings.WindowWidth > 0) Width = _settings.WindowWidth;
            if (!double.IsNaN(_settings.WindowHeight) && _settings.WindowHeight > 0) Height = _settings.WindowHeight;
            if (!double.IsNaN(_settings.WindowTop)) Top = _settings.WindowTop;
            if (!double.IsNaN(_settings.WindowLeft)) Left = _settings.WindowLeft;

            this.Closing += MainWindow_Closing;

            // Use WPF-UI ContentDialog instead of MessageBox
            _viewModel.ShowMessage = async msg =>
            {
                var host = ContentDialogHost.GetForWindow(this);
                var dialog = new ContentDialog
                {
                    Title = "PicSorter",
                    Content = msg,
                    CloseButtonText = "OK",
                    DialogHostEx = host
                };
                await dialog.ShowAsync(CancellationToken.None);
            };

            _viewModel.BrowseFolderDialog = description =>
            {
                var dialog = new OpenFolderDialog
                {
                    Title = description
                };
                if (dialog.ShowDialog() == true)
                {
                    return dialog.FolderName;
                }
                return string.Empty;
            };

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Image loading is handled inside SinglePreviewView
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ignore input if focus is in a TextBox
            if (e.OriginalSource is System.Windows.Controls.TextBox)
                return;

            string keyStr = "";
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                keyStr = (e.Key - Key.D0).ToString();
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                keyStr = (e.Key - Key.NumPad0).ToString();
            }
            else if (e.Key == Key.S)
            {
                keyStr = "S";
            }
            else if (e.Key == Key.Back)
            {
                keyStr = "Back";
            }
            else if (e.Key == Key.Left)
            {
                keyStr = "Left";
            }
            else if (e.Key == Key.Right)
            {
                keyStr = "Right";
            }

            if (!string.IsNullOrEmpty(keyStr))
            {
                await _viewModel.TryGetCommandForKey(keyStr);
                e.Handled = true;
            }
        }

        private void SaveSettings()
        {
            _settings.WindowWidth = ActualWidth;
            _settings.WindowHeight = ActualHeight;
            _settings.WindowTop = Top;
            _settings.WindowLeft = Left;
            
            // Note: Mode and Destinations are updated by MainViewModel before it calls SaveSettings
            
            // Fire-and-forget save
            _ = _settingsService.SaveSettingsAsync(_settings);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            SaveSettings();
        }
    }
}