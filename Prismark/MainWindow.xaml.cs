using Prismark.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Configuration;
using IWshRuntimeLibrary;
using System.Reflection;

namespace Prismark
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, Page> _pageCache = new Dictionary<string, Page>();
        private App _app = Application.Current as App;
        public MainWindow()
        {
            InitializeComponent();
            this.StateChanged += MainWindow_StateChanged;

            NavigateButton_Click(btnNavigateToEditor, new RoutedEventArgs());
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetButtonUnderline(btnNavigateToEditor, true);
            btnNavigateToEditor.IsEnabled = false;
        }
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.BorderThickness = new Thickness(8);
                double taskBarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.MaximizedPrimaryScreenHeight;
                this.MaxHeight = SystemParameters.PrimaryScreenHeight - taskBarHeight;
            }
            else
            {
                this.BorderThickness = new Thickness(0);
            }
        }
        #region 最小化／最大化／閉じる
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region ナビゲーション関連
        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string pageName = button.Content.ToString();
                if (_pageCache.TryGetValue(pageName, out var cachedPage))
                {
                    MainFrame.Navigate(cachedPage);
                }
                else
                {
                    Type pageType = Type.GetType($"Prismark.Resources.Pages.{pageName}");
                    if (pageType != null)
                    {
                        var newPage = (Page)Activator.CreateInstance(pageType);
                        _pageCache[pageName] = newPage;
                        MainFrame.Navigate(newPage);
                    }
                }
            }
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Resources.Pages.Setting());
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            UpdateButtonHighlight();
        }
        private void UpdateButtonHighlight()
        {
            //すべてのボタンのハイライトを解除
            SetButtonUnderline(btnNavigateToEditor, false);
            SetButtonUnderline(btnNavigateToExport, false);
            SetButtonUnderline(btnNavigateToSetting, false);
            btnNavigateToEditor.IsEnabled = true;
            btnNavigateToExport.IsEnabled = true;
            btnNavigateToSetting.IsEnabled = true;

            // 現在のページに対応するボタンをハイライト
            if (MainFrame.Content is Page currentPage)
            {
                string pageName = currentPage.GetType().Name;
                var button = FindName($"btnNavigateTo{pageName}") as Button;
                if (button != null)
                {
                    SetButtonUnderline(button, true); // ハイライト色
                    button.IsEnabled = false;
                }
            }
        }
        private void SetButtonUnderline(Button button, bool isHighlighted)
        {
            if (button.Template.FindName("UnderlineRect", button) is Rectangle underline)
            {
                Color color = (Color)ColorConverter.ConvertFromString("#0295ff");
                SolidColorBrush brush = new SolidColorBrush(color);
                underline.Fill = isHighlighted ? brush : Brushes.Transparent;
            }
        }


        #endregion

        
    }
}