using Prismark.Resources.Pages;
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
using System.Windows.Shapes;

namespace Prismark.Resources.StartUp
{
    /// <summary>
    /// Start.xaml の相互作用ロジック
    /// </summary>
    public partial class Start : Page
    {
        public Start()
        {
            InitializeComponent();
        }

        private void btnNavigateToOpan_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Open());
        }

        private void btnNavigateToRegister_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Register());
        }
    }
}
