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
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prismark.Utils;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls.Primitives;
using Prismark.UI.Modal;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Prismark.UI.Pages
{
    /// <summary>
    /// Editor.xaml の相互作用ロジック
    /// </summary>
    public partial class Editor : Page
    {
        #region フィールド変数
        private string _currentFilePath;

        private bool _isSwitchingTextChanged = false;

        private List<Button> _fileButtons = new List<Button>();
        private List<ProjectFile> _projectFiles = new List<ProjectFile>();
        private List<ProjectFile> _restoreProjectFiles;

        private DispatcherTimer saveTimer;
        private DispatcherTimer webView2Timer;

        private int _currentLine;
        private int _currentColumn;

        double scrollPercentage;

        private App _app = Application.Current as App;

        private UndoRedoManager undoRedoManager = new UndoRedoManager();

        #endregion

        public Editor()
        {
            _restoreProjectFiles = ProjectManager.ReadProjectInfo(_app.WorkingFolder)?.ProjectFiles?.ToList();
            InitializeComponent();
            InitializePage();
            SetupSaveTimer();
            SetupWebView2Timer();

            // カーソルが画面外に到達したときの自動スクロールを有効にする
            //MarkDownEditor.Options.AllowScrollBelowDocument = true;
            //MarkDownEditor.Options.ScrollBelowDocumentEnd = true;

            // 必要に応じて、スクロールのマージンを設定する
            //MarkDownEditor.ScrollToEnd();
            //MarkDownEditor.ScrollToVerticalOffset(MarkDownEditor.VerticalOffset - 100);

        }

        #region 初期処理系
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 異常セッションの復元
                if (_app.IsAbnormalClose)
                {
                    if (_restoreProjectFiles?.Any() ?? false)
                    {
                        UI.Modal.CustomOkCancelDialog dialog = new CustomOkCancelDialog("前回異常終了したセッションを復元しますか？");
                        dialog.Owner = Window.GetWindow(this);
                        if (dialog.ShowDialog() == true)
                        {
                            foreach (var restoreItem in _restoreProjectFiles)
                            {
                                ProjectFile file = GetProjectFile(restoreItem.FileName);
                                if (file != null)
                                {
                                    bool isModify = file.Content != restoreItem.Content;
                                    file.Content = restoreItem.Content;
                                    file.IsInit = true;
                                    file.IsSaved = !isModify;
                                    file.IsInit = !isModify;
                                    UpdateFileButtonState(
                                        Path.Combine(_app.WorkingFolder, "md", file.FileName + ".md"),
                                        isModify
                                        );
                                }
                            }
                            UpdateProjectFile();

                            MarkDownEditor.Text = GetProjectFile(Path.GetFileNameWithoutExtension(_currentFilePath)).Content;
                        }
                    }
                    _app.IsAbnormalClose = false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"プロジェクトのリフレッシュに失敗しました: {ex.Message}");
            }
            
        }
        private void InitializePage()
        {
            // シンタックスハイライト定義ファイルを読み込む
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream s = assembly.GetManifestResourceStream("Prismark.UI.Themes.MarkdownDark.xshd"))
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

            // プロジェクト名を設定
            txtProjectName.Text = _app.ProjectName;

            // mdファイルを参照して、ボタンを配置する
            CreateFileButtons();
            InitializeAsync();

            // すべてのマウスダウンイベントをキャプチャ
            EventManager.RegisterClassHandler(typeof(Window),
                Mouse.MouseDownEvent,
                new MouseButtonEventHandler(OnMouseDown),
                true);
        }
        async void InitializeAsync()
        {
            try
            {
                if (_app.LocalHttpServer == null)
                {
                    _app.LocalHttpServer = new LocalHttpServer(System.IO.Path.Combine(_app.WorkingFolder));
                    _app.LocalHttpServer.Start();
                }
                await webView.EnsureCoreWebView2Async(null);
                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string markdown = MarkDownEditor.Text;
                        Utils.MarkDownToHTML conv = new Utils.MarkDownToHTML();
                        string modifiedHtml = _app.LocalHttpServer.ReplaceMediaPaths(conv.ToViewHtml(markdown));

                        webView.NavigateToString(modifiedHtml);
                    });
                });
                InitializeFirstFile();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

        }
        private void InitializeFirstFile()
        {
            if (_fileButtons.Count > 0)
            {
                // 最初のボタンに対応するファイルを表示
                string firstFilePath = (string)_fileButtons[0].Tag;
                SwitchFile(firstFilePath);
                ButtonHighLightChange(_fileButtons[0]);

            }
            else
            {
                System.Windows.MessageBox.Show("表示可能なファイルがありません。");
            }
        }
        

        #endregion

        #region プロジェクトクラス管理マネジャー
        public void UpdateProjectFile(ProjectFile file)
        {
            var existingFile = _projectFiles.FirstOrDefault(f => f.FileName == file.FileName);

            if (existingFile != null)
            {
                existingFile.Section = file.Section;
                existingFile.Content = file.Content;
                existingFile.IsSaved = file.IsSaved;
            }
            else
            {
                _projectFiles.Add(file);
            }
            ProjectInfo info = ProjectManager.ReadProjectInfo(_app.WorkingFolder);
            info.ProjectFiles = _projectFiles;
            ProjectManager.WriteProjectInfo(info);
        }
        public async void UpdateProjectFile()
        {
            await Task.Run(() => {
                ProjectInfo info = ProjectManager.ReadProjectInfo(_app.WorkingFolder);
                info.ProjectFiles = _projectFiles;
                ProjectManager.WriteProjectInfo(info);
            });
        }
        public ProjectFile GetProjectFile(string fileName)
        {
            var existingFile = _projectFiles.FirstOrDefault(f => f.FileName == fileName);
            return existingFile;
        }
        private void SetupSaveTimer()
        {
            saveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10000)
            };
            saveTimer.Tick += SaveTimer_Tick;
        }
        private void SaveTimer_Tick(object sender, EventArgs e)
        {
            saveTimer.Stop();
            UpdateProjectFile();
        }
        #endregion

        #region 左ペインファイルボタン関連
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
                UpdateProjectFile(
                    new ProjectFile {
                        Content = File.ReadAllText(file),
                        FileName = Path.GetFileNameWithoutExtension(file),
                        IsInit = true,
                        IsSaved = true,
                    });
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
            button.Click += (sender, e) => { 
                SwitchFile((string)((Button)sender).Tag);
                ButtonHighLightChange(button);
            };
            _fileButtons.Add(button);
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
                ScrollViewer scrollViewer = FindScrollViewer(MarkDownEditor);
                scrollViewer.ScrollToVerticalOffset(0);

                _currentFilePath = newFilePath;
                //Undo Redo
                undoRedoManager.SetCurrentFile(newFilePath); // 現在のファイルを設定

                var file = GetProjectFile(Path.GetFileNameWithoutExtension(newFilePath));
                if (file.IsInit)
                {
                    LoadFileContent(newFilePath);
                    undoRedoManager.SetInitialState(MarkDownEditor.Text, MarkDownEditor.CaretOffset);
                }
                else
                {
                    MarkDownEditor.Text = file.Content;
                }
                
                UpdateUndoRedoButtons();

                btnFileName.Content = $"{System.IO.Path.GetFileNameWithoutExtension(newFilePath)}";
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
                ProjectFile file = GetProjectFile(Path.GetFileNameWithoutExtension(filePath));
                file.Content = content;
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
            var button = _fileButtons.FirstOrDefault(b => (string)b.Tag == filePath);
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
                File.WriteAllText(_currentFilePath, MarkDownEditor.Text, Encoding.UTF8);
                ProjectFile file = GetProjectFile(Path.GetFileNameWithoutExtension(_currentFilePath));
                file.Content = MarkDownEditor.Text;
                file.IsSaved = true;
                UpdateProjectFile();
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
        /// ボタンをソートオプションによって配置する
        /// </summary>
        /// <param name="sortOption"></param>
        private void SortButtons(string sortOption)
        {
            IEnumerable<Button> sortedButtons;

            if (sortOption == "Name")
            {
                sortedButtons = _fileButtons.OrderBy(b => b.Content.ToString());
            }
            else
            {
                sortedButtons = _fileButtons.OrderByDescending(b => File.GetLastWriteTime((string)b.Tag));
            }

            pnlMdFiles.Children.Clear();

            foreach (var button in sortedButtons)
            {
                pnlMdFiles.Children.Add(button);
            }
        }
        /// <summary>
        /// ファイル名ボタンのハイライト制御
        /// </summary>
        /// <param name="button"></param>
        private void ButtonHighLightChange(Button button)
        {
            foreach (var item in _fileButtons)
            {
                SetButtonHighLight(item, false);
            }
            SetButtonHighLight(button, true);
        }
        private void SetButtonHighLight(Button button, bool isHighlighted)
        {
            Color backcolor = (Color)ColorConverter.ConvertFromString("#0295ff");
            SolidColorBrush backbrush = new SolidColorBrush(backcolor);
            button.Background = isHighlighted ? backbrush : Brushes.Transparent;
        }
        #endregion

        #region ファイル関連
        /// <summary>
        /// 保存ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }
        /// <summary>
        /// ファイルの追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNewFile(object sender, RoutedEventArgs e)
        {
            var dialog = new Modal.FileNameInputDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.ModifiedFileName.Trim() + ".md";
                string title = dialog.ModifiedFileName.Trim();
                string content = $"# {title}";
                try
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string filePath = System.IO.Path.Combine(_app.WorkingFolder, "md", fileName);
                    File.WriteAllText(filePath, content);

                    UpdateProjectFile(new ProjectFile
                    {
                        FileName = Path.GetFileNameWithoutExtension(filePath),
                        Content = content,
                        IsSaved = true,
                        IsInit = true
                    });

                    CreateFileButton(filePath);
                    SortButtons("Name");
                    SwitchFile(filePath);

                    var button = _fileButtons.FirstOrDefault(b => (string)b.Tag == filePath);
                    ButtonHighLightChange(button);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        /// <summary>
        /// 全てのファイルを保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAllSave_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Modal.CustomOkCancelDialog("全てのファイルを保存します。よろしいですか？");
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    foreach (var item in _projectFiles.ToList())
                    {
                        string key = Path.Combine(_app.WorkingFolder, "md", item.FileName + ".md");
                        File.WriteAllText(key, item.Content);
                        UpdateFileButtonState(key, false);
                        ProjectFile file = GetProjectFile(item.FileName);
                        file.IsSaved = true;
                    }
                    UpdateProjectFile();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"ファイルの保存に失敗しました: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// プロジェクトのリフレッシュ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAllRefresh_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Modal.CustomOkCancelDialog($"プロジェクト全体を同期し、リフレッシュします。{Environment.NewLine}よろしいですか？{Environment.NewLine}※ 未保存の変更内容は破棄されます");
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    foreach (var item in _projectFiles.ToList())
                    {
                        string key = Path.Combine(_app.WorkingFolder, "md", item.FileName + ".md");
                        if (key == _currentFilePath)
                        {
                            if (File.Exists(key))
                            {
                                MarkDownEditor.Text = File.ReadAllText(key);
                            }
                        }
                        if (File.Exists(key))
                        {
                            ProjectFile file = GetProjectFile(item.FileName);
                            file.Content = File.ReadAllText(key);
                            file.IsSaved = true;
                        }
                        else
                        {
                            _projectFiles.Remove(GetProjectFile(item.FileName));
                        }
                    }
                    UpdateProjectFile();

                    pnlMdFiles.Children.Clear();
                    _fileButtons.Clear();
                    CreateFileButtons();
                    InitializeFirstFile();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"プロジェクトのリフレッシュに失敗しました: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// ファイルの場所を開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseDirectry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", _app.WorkingFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ディレクトリを開けませんでした: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// ファイル名変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFileName_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Modal.FileNameInputDialog(btnFileName.Content.ToString());
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.ModifiedFileName.Trim() + ".md";
                try
                {
                    if (dialog.InputFileName == btnFileName.Content.ToString()) return;

                    string oldFilePath = System.IO.Path.Combine(_currentFilePath);
                    string newFilePath = System.IO.Path.Combine(_app.WorkingFolder, "md", fileName);

                    File.Move(oldFilePath, newFilePath);
                    
                    _currentFilePath = newFilePath;
                    btnFileName.Content = dialog.ModifiedFileName.Trim();

                    ProjectFile file = GetProjectFile(Path.GetFileNameWithoutExtension(oldFilePath));
                    file.Content = MarkDownEditor.Text;
                    file.FileName = Path.GetFileNameWithoutExtension(newFilePath);
                    UpdateProjectFile();

                    var button = _fileButtons.FirstOrDefault(b => (string)b.Tag == oldFilePath);
                    if (button != null)
                    {
                        button.Tag = newFilePath;
                        button.Content = dialog.ModifiedFileName.Trim() + ".md";
                    }
                    SortButtons("Name");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region テキストエディター関連
        /// <summary>
        /// エディター変更時イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkDownEditor_TextChanged(object sender, EventArgs e)
        {
            saveTimer.Stop();
            saveTimer.Start();
            webView2Timer.Stop();
            webView2Timer.Start();
        }
        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            StatusBarChange();
            ScrollToCaretIfOutOfView();


        }
        private void MarkDownEditor_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer scrollViewer = FindScrollViewer(MarkDownEditor);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }
        private void ScrollToCaretIfOutOfView()
        {
            var textView = MarkDownEditor.TextArea.TextView;
            var caret = MarkDownEditor.TextArea.Caret;
            var position = caret.Position;
            var visualTop = textView.GetVisualPosition(position, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineTop);
            var visualBottom = textView.GetVisualPosition(position, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineBottom);
            ScrollViewer scrollViewer = FindScrollViewer(MarkDownEditor);

            int ScrollMargin = 2;

            double currentTop = scrollViewer.VerticalOffset;
            double currentBottom = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight;
            double lineHeight = textView.DefaultLineHeight;

            double newVerticalOffset = scrollViewer.VerticalOffset;
            bool needScroll = false;

            // カーソルが上側の表示範囲外にある場合
            if (visualTop.Y < currentTop + (ScrollMargin * lineHeight))
            {
                newVerticalOffset = visualTop.Y - (ScrollMargin * lineHeight);
                needScroll = true;
            }
            // カーソルが下側の表示範囲外にある場合
            else if (visualBottom.Y > currentBottom - (ScrollMargin * lineHeight))
            {
                newVerticalOffset = visualBottom.Y - scrollViewer.ViewportHeight + (ScrollMargin * lineHeight);
                needScroll = true;
            }

            if (needScroll)
            {
                // スクロール位置が有効な範囲内になるように調整
                newVerticalOffset = Math.Max(0, Math.Min(newVerticalOffset, scrollViewer.ScrollableHeight));
                scrollViewer.ScrollToVerticalOffset(newVerticalOffset);
            }

            // 水平方向のスクロール
            if (visualTop.X < scrollViewer.HorizontalOffset ||
                visualTop.X > scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth)
            {
                scrollViewer.ScrollToHorizontalOffset(visualTop.X);
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // スクロール位置が変更されたときの処理
            double verticalOffset = e.VerticalOffset;
            double horizontalOffset = e.HorizontalOffset;

            if (webView.CoreWebView2 != null)
            {
                // スクロール位置の計算（0から1の範囲に正規化）
                scrollPercentage = e.VerticalOffset / (e.ExtentHeight - e.ViewportHeight);

                // JavaScriptを使用してWebView2をスクロール
                onScrollChange();
            }
        }
        private async Task onUpdateHTML()
        {
            if (webView.CoreWebView2 != null)
            {
                string markdown = MarkDownEditor.Text;
                Utils.MarkDownToHTML conv = new Utils.MarkDownToHTML();
                string modifiedHtml = _app.LocalHttpServer.ReplaceMediaPaths(conv.ConvertMarkdownToHtml(markdown));
                // JavaScriptを使用してWebView2をスクロール"
                await webView.CoreWebView2.ExecuteScriptAsync($"updateContent(`{modifiedHtml.Replace("`", "\\`")}`)");
            }
        }
        private async void onScrollChange()
        {
            if (webView.CoreWebView2 != null)
            {
                // JavaScriptを使用してWebView2をスクロール
                await webView.CoreWebView2.ExecuteScriptAsync($@"
                    var contentHeight = document.body.scrollHeight;
                    var windowHeight = window.innerHeight;
                    var scrollPosition = (contentHeight - windowHeight) * {scrollPercentage};
                    window.scrollTo(0, scrollPosition);
                ");
            }
        }

        private ScrollViewer FindScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer)
                return depObj as ScrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
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
        private void SetupWebView2Timer()
        {
            webView2Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            webView2Timer.Tick += WebView2Timer_Tick;
        }
        private async void WebView2Timer_Tick(object sender, EventArgs e)
        {
            webView2Timer.Stop();

            await onUpdateHTML();
            StatusBarChange();  // Word,Line,Col

            ProjectFile file = GetProjectFile(Path.GetFileNameWithoutExtension(_currentFilePath));

            if (_currentFilePath != null && !_isSwitchingTextChanged) // ファイル切り替えによるテキストチェンジを検知
            {
                undoRedoManager.RecordState(MarkDownEditor.Text, MarkDownEditor.CaretOffset);
                file.IsInit = false;
            }
            if (File.ReadAllText(_currentFilePath) == MarkDownEditor.Text)
            {
                // 保存データとカレントデータが一致していればいれば保存済みマークを表示
                file.IsSaved = true;
                file.Content = MarkDownEditor.Text;
                UpdateFileButtonState(_currentFilePath, false);
            }
            else
            {
                file.IsSaved = false;
                file.Content = MarkDownEditor.Text;
                UpdateFileButtonState(_currentFilePath, true);
            }
            UpdateUndoRedoButtons();

            UpdateProjectFile(file);
        }

        #endregion

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
            ApplyInsertMarkdown($"<br>");
        }
        /// <summary>
        /// 段落
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditParagraph_Click(object sender, RoutedEventArgs e)
        {
            ApplyInsertMarkdownToNextLine("");
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
            ImageSelectDialog dialog = new ImageSelectDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                File.Copy(dialog.ImagePath, Path.Combine(_app.WorkingFolder, "img", Path.GetFileName(dialog.ImagePath)), true);
                ApplyInsertMarkdown($"![代替テキスト](img/{Path.GetFileName(dialog.ImagePath)}){{style=width:{dialog.ImageWidth}px;}}");
            }
        }
        /// <summary>
        /// 文字列を着色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditColor_Click(object sender, RoutedEventArgs e)
        {
            ApplyStartEndMarkdown($@"<span style=""color:{ConvertWpfColorToHtml(ColorTextBox.Text)}"">", $"</span>");
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
            TableSelectDialog dialog = new TableSelectDialog();
            dialog.Owner = Window.GetWindow(this);
            if(dialog.ShowDialog() == true)
            {
                ApplyInsertMarkdownToNextLine(dialog.InsertTableMarkDown);
            }
            
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

                MarkDownEditor.Focus();
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
            MarkDownEditor.Focus();
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

            if (startLine > endLine)
            {
                (startLine, endLine) = (endLine, startLine);
            }

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

            MarkDownEditor.Focus();
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
                MarkDownEditor.CaretOffset = offset + markdownTamplate.Length + 4;
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
            MarkDownEditor.Focus();
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
            MarkDownEditor.Focus();
        }
        #endregion

        #region ショートカットキー
        /// <summary>
        /// ショートカットキーの定義
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Editor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Ctlr+S:保存
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveFile();
                e.Handled = true; // イベントが処理されたことを示す
            }
            // Ctlr+Shift+S:すべて保存
            if (e.Key == Key.S &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                btnAllSave_Click(sender, e);
                e.Handled = true;
            }
            // Ctlr+N:新規ファイル
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                AddNewFile(sender, e);
                e.Handled = true; // イベントが処理されたことを示す
            }

        }
        private void MarkDownEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Z:戻る
            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (btnUndo.IsEnabled)
                {
                    Undo();
                }

                e.Handled = true;
            }
            // Ctrl+Y:元に戻す
            if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (btnRedo.IsEnabled)
                {
                    Redo();
                }
                e.Handled = true;
            }
            // Ctrl+B:太字
            if (e.Key == Key.B && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                btnEditBold_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+I:斜体
            if (e.Key == Key.I && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                btnEdititalic_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+1:見出し大
            if (e.Key == Key.D1 && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                btnEditH1_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+2:見出し中
            if (e.Key == Key.D2 && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                btnEditH2_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+3:見出し小
            if (e.Key == Key.D3 && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                btnEditH3_Click(sender, e);
                e.Handled = true;
            }
            // Shift+Enter 改行文字挿入
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                btnEditBreak_Click(sender, e);
                e.Handled = true;
            }
        }
        #endregion

        #region Undo/Redo関連
        /// <summary>
        /// 元に戻すボタン押下時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }
        /// <summary>
        /// やり直しボタン押下時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }
        /// <summary>
        /// 元に戻す（Undo）実行
        /// ファイルごとにUndo　Redoの情報を保持
        /// </summary>
        private void Undo()
        {
            var result = undoRedoManager.Undo();
            if (result.HasValue)
            {
                _isSwitchingTextChanged = true;
                MarkDownEditor.Text = result.Value.Text;
                MarkDownEditor.CaretOffset = result.Value.CaretPosition;
                _isSwitchingTextChanged = false;
                UpdateFileButtonState(_currentFilePath, File.ReadAllText(_currentFilePath) != MarkDownEditor.Text);
                UpdateUndoRedoButtons();
            }
        }
        /// <summary>
        /// やり直し（Redo）実行
        /// ファイルごとにUndo　Redoの情報を保持
        /// </summary>
        private void Redo()
        {
            var result = undoRedoManager.Redo();
            if (result.HasValue)
            {
                _isSwitchingTextChanged = true;
                MarkDownEditor.Text = result.Value.Text;
                MarkDownEditor.CaretOffset = result.Value.CaretPosition;
                _isSwitchingTextChanged = false;
                UpdateFileButtonState(_currentFilePath, File.ReadAllText(_currentFilePath) != MarkDownEditor.Text);
                UpdateUndoRedoButtons();
            }
        }
        private void UpdateUndoRedoButtons()
        {
            btnUndo.IsEnabled = undoRedoManager.CanUndo();
            btnRedo.IsEnabled = undoRedoManager.CanRedo();
        }

        private void UndoCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (btnUndo.IsEnabled)
            {
                Undo();
            }

            e.Handled = true; // イベントが処理されたことを示す
        }
        private void RedoCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (btnRedo.IsEnabled)
            {
                Redo();
            }
            e.Handled = true; // イベントが処理されたことを示す
        }

        #endregion

        #region カラーピッカー関連
        /// <summary>
        /// カラーピッカー関連
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ColorPickerPopup.IsOpen = true;
            ColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(ColorTextBox.Text);
        }
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ColorPickerPopup.IsOpen && !IsMouseOverPopup() && !ColorTextBox.IsMouseOver)
            {
                ColorPickerPopup.IsOpen = false;
            }
        }
        private bool IsMouseOverPopup()
        {
            return ColorPickerPopup.IsMouseOver || IsMouseOverPopupContent();
        }

        private bool IsMouseOverPopupContent()
        {
            if (ColorPickerPopup.Child == null) return false;

            var point = Mouse.GetPosition(ColorPickerPopup.Child);
            var hitTestResult = VisualTreeHelper.HitTest(ColorPickerPopup.Child, point);
            return hitTestResult != null;
        }
        private void ColorPicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            ColorTextBox.Text = ColorPicker.SelectedColor.ToString();
            btnEditColor.Foreground = new SolidColorBrush(ColorPicker.SelectedColor);
        }
        public static string ConvertWpfColorToHtml(string wpfColor)
        {
            if (wpfColor.StartsWith("#") && wpfColor.Length == 9)
            {
                return $"#{wpfColor.Substring(3, 6)}{wpfColor.Substring(1, 2)}";
            }
            else
            {
                return "#FFFFFFFF";
            }
        }
        public static string ConvertWpfColorToHtml(Color wpfColor)
        {
            return $"#{wpfColor.R:X2}{wpfColor.G:X2}{wpfColor.B:X2}{wpfColor.A:X2}";
        }
    }

    #endregion

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