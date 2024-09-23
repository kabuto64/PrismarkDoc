using IWshRuntimeLibrary;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prismark.Resources.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;

namespace Prismark.Resources.StartUp
{
    /// <summary>
    /// Register.xaml の相互作用ロジック
    /// </summary>
    public partial class Register : Page
    {
        private App _app = Application.Current as App;
        StartUpWindow _startUpWindow = null;
        public Register(bool returnButtonVisible = true)
        {
            InitializeComponent();

            btnReturnToStart.Visibility = returnButtonVisible ? Visibility.Visible : Visibility.Collapsed;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _startUpWindow = (StartUpWindow)Window.GetWindow(this);
        }
        /// <summary>
        /// 参照ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "作業フォルダを選択してください",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtFolderPath.Text = dialog.FileName;
                _app.WorkingFolder = dialog.FileName;
            }
        }
        /// <summary>
        /// 必要なフォルダを作業フォルダ内に作成
        /// </summary>
        private void CreateRequiredFolders()
        {
            Directory.CreateDirectory(System.IO.Path.Combine(_app.WorkingFolder, "md"));
            Directory.CreateDirectory(System.IO.Path.Combine(_app.WorkingFolder, "img"));
            Directory.CreateDirectory(System.IO.Path.Combine(_app.WorkingFolder, "Publish"));
        }
        /// <summary>
        /// 作業フォルダにショートカットを作成
        /// </summary>
        private void CreateShortcut()
        {
            string shortcutPath = System.IO.Path.Combine(_app.WorkingFolder, $"{_app.ProjectName}.lnk");
            string targetPath = Process.GetCurrentProcess().MainModule.FileName;

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            // コマンドライン引数を設定し、ショートカットから起動したときに自動的に作業フォルダを設定する。
            shortcut.TargetPath = targetPath;
            shortcut.Arguments = $"--project \"{_app.WorkingFolder}\"";
            shortcut.WorkingDirectory = _app.WorkingFolder;
            shortcut.Description = $"{_app.ProjectName} - Prismark Project";
            shortcut.Save();
        }
        /// <summary>
        /// 空のマークダウンファイルの作成
        /// </summary>
        private void CreateEmptyMarkdownFile()
        {
            // mdフォルダのパスを作成
            string mdFolderPath = Path.Combine(_app.WorkingFolder, "md");

            // mdフォルダが存在しない場合は作成
            if (!Directory.Exists(mdFolderPath))
            {
                Directory.CreateDirectory(mdFolderPath);
            }

            int fileNumber = 1;
            string fileName;
            do
            {
                fileName = $"{fileNumber}.NewMarkdownFile.md";
                fileNumber++;
            } while (System.IO.File.Exists(Path.Combine(mdFolderPath, fileName)));

            // 空のmdファイルを作成
            string filePath = Path.Combine(mdFolderPath, fileName);
            System.IO.File.WriteAllText(filePath, string.Empty);
        }
        /// <summary>
        /// プロジェクト作成実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text))
            {
                System.Windows.MessageBox.Show("作業フォルダを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtProjectName.Text))
            {
                System.Windows.MessageBox.Show("プロジェクト名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string projectPath = System.IO.Path.Combine(txtFolderPath.Text, txtProjectName.Text);

            try
            {
                Directory.CreateDirectory(projectPath);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"プロジェクトフォルダの作成中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _app.WorkingFolder = projectPath;
            _app.ProjectName = txtProjectName.Text;

            // 必要なフォルダを作成
            CreateRequiredFolders();
            // 空のmdファイルを作成
            CreateEmptyMarkdownFile();

            // ショートカットを作成
            CreateShortcut();

            _app.WorkingFolder = projectPath;
            _app.ProjectName = txtProjectName.Text;

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            //_startUpWindow.NewWorkingDirectry = projectPath;
            //_startUpWindow.DialogResult = true;
            _startUpWindow.Close();
        }

        private void btnReturnToStart_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Start());
        }

        
    }
}
