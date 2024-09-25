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
using System.Windows.Shapes;

namespace Prismark.UI.Modal
{
    /// <summary>
    /// CustomOkCancelDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class CustomOkCancelDialog : Utils.DialogBase
    {
        public CustomOkCancelDialog(string message)
        {
            InitializeComponent();

            this.SizeToContent = SizeToContent.WidthAndHeight;

            // 最小サイズを設定（オプション）
            this.MinWidth = 200;
            this.MinHeight = 100;

            txtMessage.Text = message;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
