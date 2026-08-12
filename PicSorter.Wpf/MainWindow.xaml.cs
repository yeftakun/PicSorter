using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PicSorter.Core.ViewModels;

namespace PicSorter.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _viewModel.ShowMessage = msg => MessageBox.Show(this, msg, "PicSorter", MessageBoxButton.OK, MessageBoxImage.Information);
            
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
            if (e.PropertyName == nameof(MainViewModel.CurrentImageBytes))
            {
                UpdatePreviewImage();
            }
        }

        private void UpdatePreviewImage()
        {
            var bytes = _viewModel.CurrentImageBytes;
            if (bytes == null || bytes.Length == 0)
            {
                imgPreview.Source = null;
                return;
            }

            try
            {
                using var ms = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Prevents file locking
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze(); // Makes it cross-thread safe and read-only

                imgPreview.Source = bitmap;
            }
            catch (Exception ex)
            {
                imgPreview.Source = null;
                _viewModel.StatusText = "Status: Gagal membuat BitmapImage - " + ex.Message;
            }
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Abaikan input jika fokus sedang di TextBox
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

            if (!string.IsNullOrEmpty(keyStr))
            {
                await _viewModel.TryGetCommandForKey(keyStr);
                e.Handled = true;
            }
        }
    }
}