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
using static Prismark.Resources.StartUp.StartUpMode;

namespace Prismark.Resources.StartUp
{
    public static class StartUpMode
    {
        public enum Mode
        {
            Start,
            Open,
            Register
        }
    }
    /// <summary>
    /// StartUpWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class StartUpWindow : Window
    {
        public string NewWorkingDirectry {  get; set; }

        public StartUpWindow(Mode mode = Mode.Start)
        {
            InitializeComponent();

            switch (mode)
            {
                case Mode.Open:
                    Open open = new Open();
                    MainFrame.Navigate(open);
                    break;
                case Mode.Register:
                    Register register = new Register();
                    MainFrame.Navigate(register);
                    break;
                default:
                    Start start = new Start();
                    MainFrame.Navigate(start);
                    break;
            }
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
    }
}
