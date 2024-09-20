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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IWshRuntimeLibrary;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Prismark.Resources.Pages
{
    /// <summary>
    /// StartUp.xaml の相互作用ロジック
    /// </summary>
    public partial class StartUp : Page
    {
        public string WorkingFolder { get; private set; }
        public string ProjectName { get; private set; }
        public string ProjectPath { get; private set; }
        public StartUp()
        {
            InitializeComponent();
        }

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
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text))
            {
                System.Windows.MessageBox.Show("作業フォルダを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtProjectName.Text))
            {
                System.Windows.MessageBox.Show("ドキュメント名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WorkingFolder = txtFolderPath.Text;
            ProjectName = txtProjectName.Text;

            ProjectPath = System.IO.Path.Combine(WorkingFolder, ProjectName);

            CreateRequiredFolders();
            CreateShortcut();

            try
            {
                Directory.CreateDirectory(ProjectPath);
                System.Windows.MessageBox.Show($"プロジェクトフォルダの作成が完了しました", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"プロジェクトフォルダの作成中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateRequiredFolders()
        {
            Directory.CreateDirectory(System.IO.Path.Combine(ProjectPath, "md"));
            Directory.CreateDirectory(System.IO.Path.Combine(ProjectPath, "img"));
        }

        private void CreateShortcut()
        {
            string shortcutPath = System.IO.Path.Combine(ProjectPath, $"{ProjectName}.lnk");
            string targetPath = Process.GetCurrentProcess().MainModule.FileName;

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            shortcut.TargetPath = targetPath;
            shortcut.Arguments = $"--project \"{ProjectPath}\"";
            shortcut.WorkingDirectory = ProjectPath;
            shortcut.Description = $"{ProjectName} - DocuForge Project";
            shortcut.Save();
        }

    }
}
