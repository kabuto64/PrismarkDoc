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
using static System.Net.Mime.MediaTypeNames;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Prismark.Resources.Pages
{
    /// <summary>
    /// Editor.xaml の相互作用ロジック
    /// </summary>
    public partial class Editor : Page
    {
        private string _currentFilePath;

        private bool _isSwitchingTextChanged = false;

        private Dictionary<string, string> fileContents = new Dictionary<string, string>();
        private List<Button> fileButtons = new List<Button>();

        private int _currentLine;
        private int _currentColumn;

        private List<Color> _predefinedColors;
        private App _app = System.Windows.Application.Current as App;

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
            this.KeyDown += Editor_KeyDown;

            // ポップアップが開いたときにフォーカスを設定
            ColorPickerPopup.Opened += (s, e) => ColorCanvas.Focus();

            // 規定の色リストを初期化
            InitializePredefinedColors();

            // プロジェクト名を設定
            txtProjectName.Text = _app.ProjectName;

            // mdファイルを参照して、ボタンを配置する
            CreateFileButtons();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //InitializeFirstFile();
        }
        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            PreviewShow();
        }
        /// <summary>
        /// カラーピッカーのデフォルトカラー
        /// </summary>
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
        private void InitializeFirstFile()
        {
            if (fileButtons.Count > 0)
            {
                // 最初のボタンに対応するファイルを表示
                string firstFilePath = (string)fileButtons[0].Tag;
                SwitchFile(firstFilePath);
            }
            else
            {
                System.Windows.MessageBox.Show("表示可能なファイルがありません。");
            }
        }
        #region ファイル操作関連（ファイルボタンボタン生成も含む）
        /// <summary>
        /// mdフォルダ内のmdファイル分、ボタンを生成する
        /// </summary>
        private void CreateFileButtons()
        {
            string folderPath = System.IO.Path.Combine(_app.WorkingFolder, "md");
            if (!Directory.Exists(folderPath))
            {
                System.Windows.MessageBox.Show("指定されたフォルダが存在しません。");
                return;
            }

            string[] files = Directory.GetFiles(folderPath, "*.md");

            foreach (string file in files)
            {
                CreateFileButton(file);
            }
            SortButtons("Name");
        }
        /// <summary>
        /// ファイルボタン生成
        /// </summary>
        /// <param name="filePath"></param>
        private void CreateFileButton(string filePath)
        {
            Button button = new Button
            {
                Content = System.IO.Path.GetFileName(filePath),
                Style = (Style)FindResource("MdFileButtonStyle"),
                Tag = filePath
            };
            ButtonProperties.SetIsModified(button, false);  // 初期状態では未変更
            button.Click += (sender, e) => SwitchFile((string)((Button)sender).Tag);

            fileButtons.Add(button);
        }
        /// <summary>
        /// ファイルボタン押下時の表示切替
        /// </summary>
        /// <param name="newFilePath"></param>
        private void SwitchFile(string newFilePath)
        {
            _isSwitchingTextChanged = true;
            try
            {
                // 現在の内容を保存
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    fileContents[_currentFilePath] = MarkDownEditor.Text;
                }

                // 新しいファイルの内容を読み込む
                if (fileContents.ContainsKey(newFilePath))
                {
                    // メモリ内データが存在すればそれを表示
                    MarkDownEditor.Text = fileContents[newFilePath];
                }
                else
                {
                    // そうでなければ、ファイルから表示
                    LoadFileContent(newFilePath);
                }

                _currentFilePath = newFilePath;
                txtMdFileName.Text = $"{System.IO.Path.GetFileNameWithoutExtension(newFilePath)}";
            }
            finally
            {
                _isSwitchingTextChanged = false;
            }
        }
        /// <summary>
        /// ファイルの内容表示
        /// </summary>
        /// <param name="filePath"></param>
        private void LoadFileContent(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                MarkDownEditor.Text = content;
                fileContents[filePath] = content;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ファイルを読み込めませんでした: {ex.Message}");
            }
        }
        /// <summary>
        /// ボタンの状態切り替え
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isModified"></param>
        private void UpdateFileButtonState(string filePath, bool isModified)
        {
            var button = fileButtons.FirstOrDefault(b => (string)b.Tag == filePath);
            if (button != null)
            {
                ButtonProperties.SetIsModified(button, isModified);
            }
        }
        /// <summary>
        /// ファイルの保存
        /// </summary>
        private void SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                System.Windows.MessageBox.Show("保存するファイルが選択されていません。");
                return;
            }

            try
            {
                File.WriteAllText(_currentFilePath, MarkDownEditor.Text);
                fileContents[_currentFilePath] = MarkDownEditor.Text; // 保存した内容をディクショナリにも反映
                if (_currentFilePath != null)
                {
                    UpdateFileButtonState(_currentFilePath, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ファイルの保存に失敗しました: {ex.Message}");
            }
        }
        /// <summary>
        /// ファイルの追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNewFile(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonSaveFileDialog
            {
                Title = "新規Markdownファイルを作成",
                DefaultDirectory = System.IO.Path.Combine(_app.WorkingFolder, "md"),
                DefaultExtension = "md",
                AlwaysAppendDefaultExtension = true,
                Filters = { new CommonFileDialogFilter("Markdown ファイル", "*.md") }
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string newFilePath = dialog.FileName;

                if (File.Exists(newFilePath))
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show("同名のファイルが既に存在します。上書きしますか？", "確認", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                File.WriteAllText(newFilePath, "");

                CreateFileButton(newFilePath);
                SortButtons("Name");
                SwitchFile(newFilePath);
            }
        }
        /// <summary>
        /// ボタンをソートオプションによって配置する
        /// </summary>
        /// <param name="sortOption"></param>
        private void SortButtons(string sortOption)
        {
            IEnumerable<Button> sortedButtons;

            if (sortOption == "Name")
            {
                sortedButtons = fileButtons.OrderBy(b => b.Content.ToString());
            }
            else
            {
                sortedButtons = fileButtons.OrderByDescending(b => File.GetLastWriteTime((string)b.Tag));
            }

            pnlMdFiles.Children.Clear();
            //pnlMdFiles.Children.Add((UIElement)pnlMdFiles.Children[0]); // ComboBoxを再追加

            foreach (var button in sortedButtons)
            {
                pnlMdFiles.Children.Add(button);
            }
        }
        #endregion


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

        private void MarkDownEditor_TextChanged(object sender, EventArgs e)
        {
            PreviewShow();
            StatusBarChange();
            if (_currentFilePath != null && !_isSwitchingTextChanged)
            {
                UpdateFileButtonState(_currentFilePath, true);
            }
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
        /// 画像挿入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "画像を選択してください",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            dialog.Filters.Add(new CommonFileDialogFilter("画像", "*.png;*.jpg;*.jpeg;*.gif;*.svg;*.bmp;*.tif;*.tiff;"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ApplyInsertMarkdown($"![代替テキスト]({dialog.FileName})");
            }

        }
        /// <summary>
        /// 文字列を着色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditColor_Click(object sender, RoutedEventArgs e)
        {
            ApplyStartEndMarkdown($@"<span style=""color:{ColorTextBox.Text}"">", $"</span>");
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

        /// <summary>
        /// ショートカットキーの定義
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Editor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveFile();
                e.Handled = true; // イベントが処理されたことを示す
            }
        }

    }

    public static class ButtonProperties
    {
        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.RegisterAttached(
                "IsModified",
                typeof(bool),
                typeof(ButtonProperties),
                new PropertyMetadata(false));

        public static void SetIsModified(UIElement element, bool value)
        {
            element.SetValue(IsModifiedProperty, value);
        }

        public static bool GetIsModified(UIElement element)
        {
            return (bool)element.GetValue(IsModifiedProperty);
        }
    }

}