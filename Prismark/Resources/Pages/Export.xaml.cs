﻿using System;
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
        }
    }
}