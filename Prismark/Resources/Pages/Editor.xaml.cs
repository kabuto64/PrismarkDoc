using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using Markdig;
using Xceed.Wpf.Toolkit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using ICSharpCode.AvalonEdit.Document;

namespace Prismark.Resources.Pages
{
    /// <summary>
    /// Editor.xaml の相互作用ロジック
    /// </summary>
    public partial class Editor : Page
    {
        private int _currentLine;
        private int _currentColumn;
        private List<Color> _predefinedColors;
        public Editor()
        {

            InitializeComponent();
            InitializeAsync();
            // シンタックスハイライト定義ファイルを読み込む
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream s = assembly.GetManifestResourceStream("Prismark.Resources.Themes.MarkdownDark.xshd"))
            {
                using (XmlReader reader = new XmlTextReader(s))
                {
                    MarkDownEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            // テキスト変更時のイベントハンドラを設定
            MarkDownEditor.TextChanged += MarkDownEditor_TextChanged;
            MarkDownEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

            // ポップアップが開いたときにフォーカスを設定
            ColorPickerPopup.Opened += (s, e) => ColorCanvas.Focus();

            // 規定の色リストを初期化
            InitializePredefinedColors();


        }
        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            PreviewShow();
        }
        private void InitializePredefinedColors()
        {
            _predefinedColors = new List<Color>
            {
                Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow,
                Colors.Orange, Colors.Purple, Colors.Pink, Colors.Brown,
                Colors.Gray, Colors.Black, Colors.White, Colors.Cyan
            };
            PredefinedColorsList.ItemsSource = _predefinedColors.Select(c => new SolidColorBrush(c));
        }
        private void ToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            if (LeftColumn.Width.Value > 50)
            {
                LeftColumn.Width = new GridLength(50);
            }
            else
            {
                LeftColumn.Width = new GridLength(200);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MarkDownEditor_TextChanged(object sender, EventArgs e)
        {
            PreviewShow();
            StatusBarChange();
        }
        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            StatusBarChange();
        }
        

        private void PreviewShow()
        {
            string markdown = MarkDownEditor.Text;
            Convaeters.MarkDownToHTML conv = new Convaeters.MarkDownToHTML();
            webView.NavigateToString(conv.ToUnitHtml(markdown));
        }
        private void StatusBarChange()
        {
            int wordCount = MarkDownEditor.Text.Count(c => !char.IsWhiteSpace(c));
            _currentLine = MarkDownEditor.TextArea.Caret.Line;
            _currentColumn = MarkDownEditor.TextArea.Caret.Column;

            Words.Text = $"Words: {wordCount}";
            Lines.Text = $"Lines: {_currentLine}";
            Col.Text = $"Col: {_currentColumn}";
        }


        #region マークダウン適用ロジック
        /// <summary>
        /// 見出し（大）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditH1_Click(object sender, RoutedEventArgs e)
        {
            ApplyLineStartMarkdown("# ");
        }
        /// <summary>
        /// 見出し（中）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditH2_Click(object sender, RoutedEventArgs e)
        {
            ApplyLineStartMarkdown("## ");
        }
        /// <summary>
        /// 見出し（小）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditH3_Click(object sender, RoutedEventArgs e)
        {
            ApplyLineStartMarkdown("### ");
        }
        /// <summary>
        /// 改行挿入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditBreak_Click(object sender, RoutedEventArgs e)
        {
            ApplyInsertMarkdown("<br>");
        }
        /// <summary>
        /// 太字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditBold_Click(object sender, RoutedEventArgs e)
        {
            ApplySurroundMarkdown("**");
        }
        /// <summary>
        /// 斜体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEdititalic_Click(object sender, RoutedEventArgs e)
        {
            ApplySurroundMarkdown("*");
        }
        /// <summary>
        /// 下線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditUnderline_Click(object sender, RoutedEventArgs e)
        {
            ApplyStartEndMarkdown("<u>", "</u>");
        }
        /// <summary>
        /// 打ち消し線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditStrikethrough_Click(object sender, RoutedEventArgs e)
        {
            ApplySurroundMarkdown("~~");

        }
        /// <summary>
        /// 引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditQuote_Click(object sender, RoutedEventArgs e)
        {
            ApplyLineStartMarkdown(">");
        }
        /// <summary>
        /// インラインコード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditCode_Click(object sender, RoutedEventArgs e)
        {
            ApplySurroundMarkdown("`");
        }
        /// <summary>
        /// リンク
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditLink_Click(object sender, RoutedEventArgs e)
        {
            ApplyStartEndMarkdown("[", "](URL)");
        }
        /// <summary>
        /// 文字列を着色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditColor_Click(object sender, RoutedEventArgs e)
        {
            ApplyStartEndMarkdown($@"<span style=""color:{ColorTextBox.Text}"">", $"<span/>");
        }
        /// <summary>
        /// 箇条書き
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditList_Click(object sender, RoutedEventArgs e)
        {
            ApplyLineStartMarkdown("- ");
        }
        /// <summary>
        /// 番号付きリスト
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditListNum_Click(object sender, RoutedEventArgs e)
        {
            ApplyLineStartMarkdown("x. ");
        }
        /// <summary>
        /// グリッド挿入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditGrid_Click(object sender, RoutedEventArgs e)
        {
            ApplyInsertMarkdownToNextLine($@"| 左揃え | 中央揃え | 右揃え |{Environment.NewLine}| :-- | :-: | --: |{Environment.NewLine}| * | * | * |");
        }
        /// <summary>
        /// コードブロック挿入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditCodeBlock_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxItem item = (ComboBoxItem)cbxLanguage.SelectedItem;
            string language = item.Tag as string;
            ApplyStartEndMarkdown($"{Environment.NewLine}```{language}{Environment.NewLine}", $"{Environment.NewLine}```{Environment.NewLine}");
        }
        /// <summary>
        /// 選択範囲にマークダウン適用（前後に囲うタイプ）
        /// </summary>
        /// <param name="markdownSymbol"></param>
        private void ApplySurroundMarkdown(string markdownSymbol)
        {
            if (MarkDownEditor == null) return;

            var document = MarkDownEditor.Document;
            var selection = MarkDownEditor.TextArea.Selection;

            if (selection.IsEmpty)
            {
                // 選択がない場合、カーソル位置にマークダウンを挿入
                int offset = MarkDownEditor.CaretOffset;
                document.Insert(offset, markdownSymbol + markdownSymbol);
                MarkDownEditor.CaretOffset = offset + markdownSymbol.Length;
            }
            else
            {
                // 選択範囲の前後にマークダウンを挿入
                int startOffset = document.GetOffset(selection.StartPosition.Location);
                int endOffset = document.GetOffset(selection.EndPosition.Location);

                if (startOffset > endOffset)
                {
                    (startOffset, endOffset) = (endOffset, startOffset);

                }

                // 選択テキストを取得し、末尾の空白文字を削除
                string selectedText = document.GetText(startOffset, endOffset - startOffset).TrimEnd();

                document.BeginUpdate();
                document.Remove(startOffset, endOffset - startOffset);
                document.Insert(startOffset, markdownSymbol + selectedText + markdownSymbol);

                document.EndUpdate();
            }
        }
        /// <summary>
        /// 選択範囲にマークダウン適用（前後で違うタイプ）
        /// </summary>
        /// <param name="openTag"></param>
        /// <param name="closeTag"></param>
        private void ApplyStartEndMarkdown(string openTag, string closeTag = null)
        {
            if (MarkDownEditor == null) return;

            var document = MarkDownEditor.Document;
            var selection = MarkDownEditor.TextArea.Selection;

            if (closeTag == null) closeTag = openTag;

            if (selection.IsEmpty)
            {
                // 選択がない場合、カーソル位置にマークダウンを挿入
                int offset = MarkDownEditor.CaretOffset;
                document.Insert(offset, openTag + closeTag);
                MarkDownEditor.CaretOffset = offset + openTag.Length;
            }
            else
            {
                int startOffset = document.GetOffset(selection.StartPosition.Location);
                int endOffset = document.GetOffset(selection.EndPosition.Location);

                if (startOffset > endOffset)
                {
                    (startOffset, endOffset) = (endOffset, startOffset);
                }

                // 選択テキストを取得し、末尾の空白文字を削除
                string selectedText = document.GetText(startOffset, endOffset - startOffset).TrimEnd();

                document.BeginUpdate();
                document.Remove(startOffset, endOffset - startOffset);
                document.Insert(startOffset, openTag + selectedText + closeTag);
                document.EndUpdate();
            }
        }
        /// <summary>
        /// 選択範囲（現在行）にマークダウン適用（行先頭につけるタイプ）
        /// </summary>
        /// <param name="markdownSymbol"></param>
        private void ApplyLineStartMarkdown(string markdownSymbol)
        {
            if (MarkDownEditor == null) return;

            var document = MarkDownEditor.Document;
            var selection = MarkDownEditor.TextArea.Selection;

            // 番号付きリスト用オプション
            int num = 1; bool isNumList = (markdownSymbol == "x. ");

            // 選択範囲の開始行と終了行を取得
            int startLine = selection.IsEmpty ? MarkDownEditor.TextArea.Caret.Line : selection.StartPosition.Line;
            int endLine = selection.IsEmpty ? startLine : selection.EndPosition.Line;

            document.BeginUpdate();

            for (int line = startLine; line <= endLine; line++)
            {
                // 各行の先頭にマークダウンシンボルを挿入
                int lineStartOffset = document.GetLineByNumber(line).Offset;
                if (isNumList)
                {
                    document.Insert(lineStartOffset, markdownSymbol.Replace("x", num.ToString()));
                }
                else
                {
                    document.Insert(lineStartOffset, markdownSymbol);
                }
                num++;
            }
            document.EndUpdate();

            // 選択範囲を更新（挿入されたマークダウンシンボルを含める）
            int newStartOffset = document.GetLineByNumber(startLine).Offset;
            int newEndOffset = document.GetLineByNumber(endLine).EndOffset;
            MarkDownEditor.Select(newStartOffset, newEndOffset - newStartOffset);
        }
        /// <summary>
        /// 選択範囲（現在行）の次の行にマークダウンのテンプレートを挿入
        /// </summary>
        /// <param name="markdownTamplate"></param>
        private void ApplyInsertMarkdownToNextLine(string markdownTamplate)
        {
            if (MarkDownEditor == null) return;

            var document = MarkDownEditor.Document;
            var selection = MarkDownEditor.TextArea.Selection;

            if (selection.IsEmpty)
            {
                // 選択がない場合、カーソル位置にマークダウンを挿入
                int offset = MarkDownEditor.CaretOffset;
                int lineNumber = document.GetLineByOffset(offset).LineNumber;
                // その行のTextLineオブジェクトを取得
                DocumentLine line = document.GetLineByNumber(lineNumber);
                offset = line.EndOffset;

                document.Insert(offset, Environment.NewLine + markdownTamplate + Environment.NewLine);
                MarkDownEditor.CaretOffset = offset + markdownTamplate.Length + 1;
            }
            else
            {
                int startOffset = document.GetOffset(selection.StartPosition.Location);
                int endOffset = document.GetOffset(selection.EndPosition.Location);

                if (startOffset > endOffset)
                {
                    (startOffset, endOffset) = (endOffset, startOffset);
                }
                int lineNumber = document.GetLineByOffset(endOffset).LineNumber;
                // その行のTextLineオブジェクトを取得
                DocumentLine line = document.GetLineByNumber(lineNumber);
                endOffset = line.EndOffset;
                document.Insert(endOffset, Environment.NewLine + markdownTamplate + Environment.NewLine);
                MarkDownEditor.CaretOffset = endOffset + markdownTamplate.Length + 1;
            }
        }
        /// <summary>
        /// 選択範囲（現在位置）にマークダウンをを挿入
        /// </summary>
        /// <param name="markdownTamplate"></param>
        private void ApplyInsertMarkdown(string markdownTamplate)
        {
            if (MarkDownEditor == null) return;

            var document = MarkDownEditor.Document;
            var selection = MarkDownEditor.TextArea.Selection;

            if (selection.IsEmpty)
            {
                // 選択がない場合、カーソル位置にマークダウンを挿入
                int offset = MarkDownEditor.CaretOffset;
                document.Insert(offset, markdownTamplate);
                MarkDownEditor.CaretOffset = offset + markdownTamplate.Length;
            }
            else
            {
                int startOffset = document.GetOffset(selection.StartPosition.Location);
                int endOffset = document.GetOffset(selection.EndPosition.Location);

                if (startOffset > endOffset)
                {
                    (startOffset, endOffset) = (endOffset, startOffset);
                }
                document.Insert(endOffset, markdownTamplate);
                MarkDownEditor.CaretOffset = endOffset + markdownTamplate.Length;
            }
        }
        #endregion


        private void ColorPickerPopup_Opened(object sender, EventArgs e)
        {
            ColorCanvas.Focus();
        }


        private void ColorTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ColorPickerPopup.IsOpen = true;
        }

        private void ColorOkButton_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerPopup.IsOpen = false;
        }

        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                Color selectedColor = e.NewValue.Value;
                string hexColor = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
                ColorTextBox.Text = hexColor;
                btnEditColor.Foreground = (Brush)new BrushConverter().ConvertFrom(hexColor);
            }
        }

        private void PredefinedColorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PredefinedColorsList.SelectedItem is SolidColorBrush selectedBrush)
            {
                ColorCanvas.SelectedColor = selectedBrush.Color;
            }
        }
        
    }
   
}
