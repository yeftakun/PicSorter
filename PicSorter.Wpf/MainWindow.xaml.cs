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
            // Removed image loading here; it is now handled by SinglePreviewView
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
    }
}