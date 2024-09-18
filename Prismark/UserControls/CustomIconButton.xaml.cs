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

namespace Prismark.UserControls
{
    /// <summary>
    /// CustomIconButton.xaml の相互作用ロジック
    /// </summary>
    public partial class CustomIconButton : UserControl
    {
        public static readonly DependencyProperty IconTypeProperty =
            DependencyProperty.Register("IconType", typeof(IconType), typeof(CustomIconButton),
                new PropertyMetadata(IconType.Settings, OnIconTypeChanged));

        public IconType IconType
        {
            get { return (IconType)GetValue(IconTypeProperty); }
            set { SetValue(IconTypeProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(object), typeof(CustomIconButton), new PropertyMetadata(null));

        public object Icon
        {
            get { return GetValue(IconProperty); }
            private set { SetValue(IconProperty, value); }
        }

        public CustomIconButton()
        {
            InitializeComponent();
        }

        private static void OnIconTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomIconButton button)
            {
                button.UpdateIcon();
            }
        }

        private void UpdateIcon()
        {
            string resourceName = $"{IconType}Icon";
            if (TryFindResource(resourceName) is object icon)
            {
                Icon = icon;
            }
            else
            {
                Icon = null;
            }
        }
    }
    public enum IconType
    {
        EditIcon,
        Settings,
        // 他のアイコンタイプをここに追加
    }
}
