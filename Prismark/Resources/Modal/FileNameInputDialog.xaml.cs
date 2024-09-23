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

namespace Prismark.Resources.Modal
{
    /// <summary>
    /// FileNameInputDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class FileNameInputDialog : Utils.DialogBase
    {
        private App _app = System.Windows.Application.Current as App;
        public string InputFileName { get; private set; }
        public string ModifiedFileName { get; private set; }
        public FileNameInputDialog(string _fileName = "")
        {
            InitializeComponent();
            txtAddFileName.Text = _fileName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ReturnResult();
        }
        private void txtAddFileName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ReturnResult();
                this.Close();
            }
        }
        private string GetUniqueFilePath(string directory, string baseFileName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
            string extension = ".md";
            string filePath = Path.Combine(directory,"md", fileNameWithoutExtension + extension);
            string resultFileName = fileNameWithoutExtension;
            int counter = 1;

            while (File.Exists(filePath))
            {
                resultFileName = $"{fileNameWithoutExtension}_{counter}";
                filePath = Path.Combine(directory, "md", resultFileName + extension);

                counter++;
            }

            return resultFileName;
        }

        private void ReturnResult()
        {
            InputFileName = txtAddFileName.Text;
            ModifiedFileName = GetUniqueFilePath(_app.WorkingFolder, txtAddFileName.Text);
            this.DialogResult = true;
            this.Close();
        }
    }
}
