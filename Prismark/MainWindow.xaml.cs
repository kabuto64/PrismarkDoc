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

namespace Prismark
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentPage;
        private string _currentProjectPath;

        private string workingDirectory;

        private Dictionary<string, Page> _pageCache = new Dictionary<string, Page>();
        public MainWindow()
        {
            InitializeComponent();
            this.StateChanged += MainWindow_StateChanged;

            //SelectWorkingDirectory();
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


        private void SelectWorkingDirectory()
        {
            // 前回の作業フォルダを取得
            string lastWorkingDir = ConfigurationManager.AppSettings["LastWorkingDirectory"];

            if (string.IsNullOrEmpty(lastWorkingDir) || !Directory.Exists(lastWorkingDir))
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "作業フォルダを選択してください";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    workingDirectory = dialog.SelectedPath;
                }
                else
                {
                    MessageBox.Show("作業フォルダが選択されていません。アプリケーションを終了します。");
                    Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                workingDirectory = lastWorkingDir;
            }

            // 必要なフォルダを作成
            CreateRequiredFolders();

            // ショートカットを作成
            CreateShortcut();

            // 作業フォルダを設定ファイルに保存
            SaveWorkingDirectory();
        }

        private void CreateRequiredFolders()
        {
            Directory.CreateDirectory(System.IO.Path.Combine(workingDirectory, "md"));
            Directory.CreateDirectory(System.IO.Path.Combine(workingDirectory, "img"));
        }

        private void CreateShortcut()
        {
            string shortcutPath = System.IO.Path.Combine(workingDirectory, "DocuForge.lnk");
            string targetPath = Process.GetCurrentProcess().MainModule.FileName;

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.Description = "DocuForge Shortcut";
            shortcut.Save();
        }

        private void SaveWorkingDirectory()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["LastWorkingDirectory"].Value = workingDirectory;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }


    }
}
