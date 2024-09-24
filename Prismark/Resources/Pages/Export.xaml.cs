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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading;
using ICSharpCode.AvalonEdit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Prismark.Resources.Pages
{
    /// <summary>
    /// Export.xaml の相互作用ロジック
    /// </summary>
    public partial class Export : Page
    {
        private App _app = System.Windows.Application.Current as App;
        public Export()
        {
            InitializeComponent();

            txtFolderPath.Text = Path.Combine(_app.WorkingFolder, "Publish");
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "出力先フォルダを選択してください",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtFolderPath.Text = dialog.FileName;

            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            WriteToLog("出力を開始します...");

            CreateRequiredFolders();



            for (int i = 0; i < 100; i++)
            {
                WriteToLog($"'{_app.WorkingFolder}'に出力しました。");
            }
            WriteToLog("出力が完了しました。");
        }

        private void WriteToLog(string logMessage)
        {
            Log.Text += $"{logMessage}{Environment.NewLine}";
            int lastLineNumber = Log.Document.LineCount;
            // 最終行の末尾にカーソルを移動
            Log.CaretOffset = Log.Document.GetLineByNumber(lastLineNumber).EndOffset;
            // スクロールして最終行を表示
            Log.ScrollToEnd();
            // フォーカスを設定
            Log.Focus();
        }
        private void CreateRequiredFolders()
        {
            Directory.CreateDirectory(System.IO.Path.Combine(txtFolderPath.Text ,"img"));
            WriteToLog($"メディアフォルダ'img'が作成されました。 出力先{Path.Combine(txtFolderPath.Text,"img")}");
            Directory.CreateDirectory(System.IO.Path.Combine(txtFolderPath.Text, "css"));
            WriteToLog($"スタイルシートフォルダ'css'が作成されました。 出力先{Path.Combine(txtFolderPath.Text, "css")}");
            Directory.CreateDirectory(System.IO.Path.Combine(txtFolderPath.Text, "js"));
            WriteToLog($"Javascriptフォルダ'css'が作成されました。 出力先{Path.Combine(txtFolderPath.Text, "js")}");

        }



    }
}