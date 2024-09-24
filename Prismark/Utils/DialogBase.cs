using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Prismark.Utils
{
    public class DialogBase : Window
    {
        private Rectangle overlay;

        public DialogBase()
        {
            this.Style = (Style)FindResource("CustomModalDialogStyle");
            this.Loaded += CustomModalDialogBase_Loaded;
            this.Closed += CustomModalDialogBase_Closed;

            // ウィンドウの起動位置を中央に設定
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void CustomModalDialogBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // オーバーレイを作成
                overlay = new Rectangle
                {
                    Style = (Style)FindResource("OverlayStyle"),
                    Width = this.Owner.ActualWidth,
                    Height = this.Owner.ActualHeight
                };

                // オーバーレイを所有者ウィンドウに追加
                if (this.Owner.Content is Grid ownerGrid)
                {
                    // オーバーレイを全ての行にまたがるように設定
                    Grid.SetRow(overlay, 0);
                    Grid.SetRowSpan(overlay, ownerGrid.RowDefinitions.Count > 0 ? ownerGrid.RowDefinitions.Count : 1);

                    // オーバーレイを全ての列にまたがるように設定
                    Grid.SetColumn(overlay, 0);
                    Grid.SetColumnSpan(overlay, ownerGrid.ColumnDefinitions.Count > 0 ? ownerGrid.ColumnDefinitions.Count : 1);

                    // オーバーレイを他の要素の上に表示
                    Grid.SetZIndex(overlay, int.MaxValue);

                    ownerGrid.Children.Add(overlay);
                }
                else if (this.Owner.Content is UIElement element)
                {
                    Grid grid = new Grid();
                    grid.Children.Add(element);
                    grid.Children.Add(overlay);
                    this.Owner.Content = grid;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Owner's Content is not a UIElement");
                }

                // ダイアログを中央に配置
                this.Owner.SizeChanged += (s, args) =>
                {
                    this.Left = this.Owner.Left + (this.Owner.Width - this.ActualWidth) / 2;
                    this.Top = this.Owner.Top + (this.Owner.Height - this.ActualHeight) / 2;

                    if (overlay != null)
                    {
                        overlay.Width = this.Owner.ActualWidth;
                        overlay.Height = this.Owner.ActualHeight;
                    }
                };
            }
        }

        private void CustomModalDialogBase_Closed(object sender, System.EventArgs e)
        {
            // オーバーレイを削除
            if (this.Owner != null && overlay != null)
            {
                if (this.Owner.Content is Panel panel)
                {
                    panel.Children.Remove(overlay);
                }
                else if (this.Owner.Content is Grid grid)
                {
                    grid.Children.Remove(overlay);
                    this.Owner.Content = grid.Children[0];
                }
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var closeButton = this.Template.FindName("CloseButton", this) as Button;
            if (closeButton != null)
            {
                closeButton.Click += (s, e) => this.Close();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
