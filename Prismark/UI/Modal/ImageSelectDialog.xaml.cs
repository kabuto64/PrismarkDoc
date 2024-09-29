using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Prismark.UI.Modal
{
    /// <summary>
    /// ImageSelectDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ImageSelectDialog : Utils.DialogBase
    {
        private BitmapImage _currentImage;
        private bool _isUpdatingControls = false;
        private readonly string[] allowedExtensions = { ".png", ".jpeg", ".jpg", ".gif", ".bmp" };

        public string ImagePath { get; private set; }
        public string ImageWidth { get; private set; }

        public ImageSelectDialog()
        {
            InitializeComponent();
        }

        private void Brawse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "画像を選択してください",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            dialog.Filters.Add(new CommonFileDialogFilter("画像", "*.png;*.jpg;*.jpeg;*.gif;*.bmp;"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    LoadImage(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"画像の読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadImage(string filePath)
        {
            try
            {
                txtImagePath.Text = filePath;

                _currentImage = new BitmapImage();
                _currentImage.BeginInit();
                _currentImage.UriSource = new Uri(filePath);
                _currentImage.CacheOption = BitmapCacheOption.OnLoad;
                _currentImage.EndInit();

                ImageArea.Visibility = Visibility.Visible;
                EmptyImage.Visibility = Visibility.Collapsed;

                ImageView.Source = _currentImage;

                _isUpdatingControls = true;
                WidthSlider.Value = 100;
                txtWidthPixel.Text = _currentImage.PixelWidth.ToString();
                _isUpdatingControls = false;

                UpdateImageSize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"画像の読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DialogBase_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string extension = Path.GetExtension(files[0]).ToLower();
                    if (allowedExtensions.Contains(extension))
                    {
                        LoadImage(files[0]);
                    }
                    else
                    {
                        MessageBox.Show("サポートされていないファイル形式です。\n許可される拡張子: " + string.Join(", ", allowedExtensions), 
                            "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                }
            }
        }

        private void ScrollViewer_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DialogBase_Drop(sender, e);
        }

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingControls)
            {
                UpdateImageSize();
            }
        }
        private void txtWidthPixel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdatingControls && int.TryParse(txtWidthPixel.Text, out int width))
            {
                UpdateImageSizeFromWidth(width);
            }
        }

        private void UpdateImageSize()
        {
            if (_currentImage != null)
            {
                double scaleFactor = WidthSlider.Value / 100;
                double newWidth = _currentImage.PixelWidth * scaleFactor;
                double newHeight = _currentImage.PixelHeight * scaleFactor;

                UpdateImageAndControls(newWidth);
            }
        }
        private void UpdateImageSizeFromWidth(int width)
        {
            if (_currentImage != null)
            {
                double scaleFactor = (double)width / _currentImage.PixelWidth;

                UpdateImageAndControls(width);
            }
        }
        private void UpdateImageAndControls(double width)
        {
            _isUpdatingControls = true;

            ImageView.Width = width;

            WidthSlider.Value = (width / _currentImage.PixelWidth) * 100;
            txtWidthPercent.Text = $"{(int)WidthSlider.Value}";
            txtWidthPixel.Text = ((int)width).ToString();

            _isUpdatingControls = false;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtImagePath.Text))
            {
                ImagePath = txtImagePath.Text;
                ImageWidth = txtWidthPixel.Text;
                this.DialogResult = true;
            }
        }
    }
}
