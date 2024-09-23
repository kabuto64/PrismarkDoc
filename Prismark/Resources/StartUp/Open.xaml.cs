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
using System.Windows.Navigation;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Prismark.Resources.StartUp
{
    /// <summary>
    /// Open.xaml の相互作用ロジック
    /// </summary>
    public partial class Open : Page
    {
        private App _app = Application.Current as App;
        StartUpWindow _startUpWindow = null;
        public Open(bool returnButtonVisible = true)
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
                txtProjectName.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                txtFolderPath.Text = dialog.FileName;
                
            }
        }
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.Combine(txtFolderPath.Text, $"{Path.GetFileNameWithoutExtension(txtFolderPath.Text)}.lnk")))
            {
                _app.WorkingFolder = txtFolderPath.Text;
                _app.ProjectName = txtProjectName.Text;

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                //_startUpWindow.NewWorkingDirectry = txtFolderPath.Text;

                _startUpWindow.Close();
            }
            else
            {
                MessageBox.Show("有効な作業フォルダではありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReturnToStart_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Start());
        }

        
    }
}
