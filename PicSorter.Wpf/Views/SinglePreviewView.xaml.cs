using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PicSorter.Core.ViewModels;

namespace PicSorter.Wpf.Views
{
    public partial class SinglePreviewView : UserControl
    {
        private Point _origin;
        private Point _start;

        public SinglePreviewView()
        {
            InitializeComponent();
            DataContextChanged += SinglePreviewView_DataContextChanged;
        }

        private void SinglePreviewView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
            {
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            }
            if (e.NewValue is INotifyPropertyChanged newVm)
            {
                newVm.PropertyChanged += Vm_PropertyChanged;
            }
            UpdatePreviewImage();
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentImageBytes))
            {
                UpdatePreviewImage();
            }
        }

        private void UpdatePreviewImage()
        {
            ResetZoomAndPan();

            if (DataContext is MainViewModel vm)
            {
                var bytes = vm.CurrentImageBytes;
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
                    vm.StatusText = "Status: Gagal membuat BitmapImage - " + ex.Message;
                }
            }
        }

        private void ResetZoomAndPan()
        {
            if (imgScale != null)
            {
                imgScale.ScaleX = 1.0;
                imgScale.ScaleY = 1.0;
            }
            if (imgTranslate != null)
            {
                imgTranslate.X = 0.0;
                imgTranslate.Y = 0.0;
            }
        }

        private void GridPreview_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (imgPreview.Source == null) return;

            var st = imgScale;
            var tt = imgTranslate;

            double zoom = e.Delta > 0 ? .1 : -.1;
            if (!(e.Delta > 0) && (st.ScaleX < .2 || st.ScaleY < .2))
                return;

            Point relative = e.GetPosition(imgPreview);
            double absoluteX = relative.X * st.ScaleX + tt.X;
            double absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            tt.X = absoluteX - relative.X * st.ScaleX;
            tt.Y = absoluteY - relative.Y * st.ScaleY;
        }

        private void GridPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (imgPreview.Source == null) return;

            var tt = imgTranslate;
            _start = e.GetPosition(gridPreview);
            _origin = new Point(tt.X, tt.Y);
            gridPreview.CaptureMouse();
        }

        private void GridPreview_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (gridPreview.IsMouseCaptured)
            {
                gridPreview.ReleaseMouseCapture();
            }
        }

        private void GridPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if (gridPreview.IsMouseCaptured)
            {
                var tt = imgTranslate;
                Vector v = _start - e.GetPosition(gridPreview);
                tt.X = _origin.X - v.X;
                tt.Y = _origin.Y - v.Y;
            }
        }
    }
}
