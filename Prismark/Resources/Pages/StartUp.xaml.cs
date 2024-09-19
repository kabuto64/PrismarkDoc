using System;
using System.Collections.Generic;
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
    }
}
