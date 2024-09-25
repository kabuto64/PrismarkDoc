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
using System.Windows.Shapes;

namespace Prismark.Resources.Modal
{
    /// <summary>
    /// TableSelectDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class TableSelectDialog : Utils.DialogBase
    {
        private Rectangle[,] cells = new Rectangle[10, 10];
        private int currentRows = 0;
        private int currentColumns = 0;

        // カラーコードで色を定義
        private static readonly SolidColorBrush DefaultCellColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0DFFFFFF"));
        private static readonly SolidColorBrush HighlightCellColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8DC1A1D3"));

        public string InsertTableMarkDown { get; private set; }
        public TableSelectDialog()
        {
            InitializeComponent();

            InitializeTableSelectGrid();
        }
        public void InitializeTableSelectGrid()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var cell = new Rectangle
                    {
                        Fill = DefaultCellColor,
                        Stroke = Brushes.Transparent,
                        StrokeThickness = 1
                    };
                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    TableSize.Children.Add(cell);
                    cells[i, j] = cell;

                    cell.MouseEnter += Cell_MouseEnter;
                    cell.MouseLeave += Cell_MouseLeave;
                }
            }
            TableSize.MouseLeave += TableSizeGrid_MouseLeave;
            TableSize.MouseLeftButtonDown += TableSizeGrid_MouseLeftButtonDown;
        }
        private void Cell_MouseEnter(object sender, MouseEventArgs e)
        {
            var cell = sender as Rectangle;
            int row = Grid.GetRow(cell);
            int col = Grid.GetColumn(cell);
            HighlightCells(row, col);
        }

        private void Cell_MouseLeave(object sender, MouseEventArgs e)
        {
            // Individual cell leave events are not needed
        }

        private void TableSizeGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            ResetHighlight();
        }

        private void HighlightCells(int row, int col)
        {
            ResetHighlight();
            for (int i = 0; i <= row; i++)
            {
                for (int j = 0; j <= col; j++)
                {
                    cells[i, j].Fill = HighlightCellColor;
                }
            }
            currentRows = row + 1;
            currentColumns = col + 1;
            //SizeText.Text = $"{currentRows} x {currentColumns}";
        }

        private void ResetHighlight()
        {
            foreach (var cell in cells)
            {
                cell.Fill = DefaultCellColor;
            }
            currentRows = currentColumns = 0;
            //SizeText.Text = "0 x 0";
        }

        private void TableSizeGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            InsertTable(currentRows, currentColumns);
            //TableSizePopup.IsOpen = false;
        }

        private void InsertTable(int rows, int columns)
        {
            StringBuilder tableBuilder = new StringBuilder();

            // Header row
            tableBuilder.Append("|");
            for (int i = 0; i < columns; i++)
            {
                tableBuilder.Append(" Column " + (i + 1) + " |");
            }
            tableBuilder.AppendLine();

            // Separator row
            tableBuilder.Append("|");
            for (int i = 0; i < columns; i++)
            {
                tableBuilder.Append(" -------- |");
            }
            tableBuilder.AppendLine();

            // Data rows
            for (int i = 1; i < rows; i++)
            {
                tableBuilder.Append("|");
                for (int j = 0; j < columns; j++)
                {
                    tableBuilder.Append("         |");
                }
                tableBuilder.AppendLine();
            }

            this.InsertTableMarkDown = tableBuilder.ToString();

            this.DialogResult = true;
            this.Close();
        }
    }
}

