using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading;
using ICSharpCode.AvalonEdit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Markdig;
using Newtonsoft.Json;
using System.Reflection;
using System.Diagnostics;

namespace Prismark.UI.Pages
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

            txtFolderPath.Text = Path.Combine(_app.WorkingFolder, "Publish");
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "出力先フォルダを選択してください",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtFolderPath.Text = dialog.FileName;

            }
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Text = string.Empty;
                await WriteToLog("========== 出力開始します ==========");
                await Task.Delay(1000);

                await CreateRequiredFolders();

                await CreateJSFile();
                await ExtractEmbeddedResource("index.html", Path.Combine(txtFolderPath.Text));
                await WriteToLog($"リソースファイル: 'index.html'を {txtFolderPath.Text}にコピーしました。");
                await ExtractEmbeddedResource("style.css", Path.Combine(txtFolderPath.Text, "css"));
                await WriteToLog($"リソースファイル: 'style.css'を {Path.Combine(txtFolderPath.Text, "css")}にコピーしました。");

                await ExtractEmbeddedResource("atom-one-dark.min.css", Path.Combine(txtFolderPath.Text, "css", "highlightjs"));
                await ExtractEmbeddedResource("highlight.min.js", Path.Combine(txtFolderPath.Text, "js", "highlightjs"));
                await ExtractEmbeddedResource("highlightjs-line-numbers.js", Path.Combine(txtFolderPath.Text, "js", "highlightjs"));

                await MediaFileCopy();

                await WriteToLog("========== 出力が完了しました ==========");

                if (isOpenExportFolder.IsChecked ?? false)
                {
                    Process.Start("explorer.exe", txtFolderPath.Text);
                }
            }
            catch
            {
                await WriteToLog("========== 出力に失敗しました。処理を中断します ==========");
            }

        }

        private async Task WriteToLog(string logMessage)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Log.Text += $"{logMessage}{Environment.NewLine}";
                int lastLineNumber = Log.Document.LineCount;
                // 最終行の末尾にカーソルを移動
                Log.CaretOffset = Log.Document.GetLineByNumber(lastLineNumber).EndOffset;
                // スクロールして最終行を表示
                Log.ScrollToEnd();
            });

        }
        private async Task CreateRequiredFolders()
        {
            await CreateDirectory(Path.Combine(txtFolderPath.Text, "img"));
            await CreateDirectory(Path.Combine(txtFolderPath.Text, "css"));
            await CreateDirectory(Path.Combine(txtFolderPath.Text, "js"));
            await CreateDirectory(Path.Combine(txtFolderPath.Text, "css", "highlightjs"));
            await CreateDirectory(Path.Combine(txtFolderPath.Text, "js", "highlightjs"));
        }
        private async Task MediaFileCopy()
        {
            string mediaDir = Path.Combine(_app.WorkingFolder, "img");
            string exportDir = Path.Combine(txtFolderPath.Text, "img");
            if (Directory.Exists(mediaDir))
            {
                try
                {
                    await WriteToLog("画像ファイルのコピーを開始します。");
                    // ソースフォルダ内の全ファイルを取得
                    var files = Directory.GetFiles(mediaDir);

                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(exportDir, fileName);

                        // ファイルをコピー
                        File.Copy(file, destFile, true);
                        await WriteToLog($"メディアファイル: '{fileName}'を'img'フォルダへコピーしました。");
                    }

                    await WriteToLog("画像ファイルのコピーが完了しました。");
                }
                catch (Exception ex)
                {
                    await WriteToLog($"画像ファイルのコピーに失敗しました: {ex.Message}");
                }
            }
        }
        private async Task CreateJSFile()
        {
            try
            {
                // 現在の実行アセンブリを取得
                Assembly assembly = Assembly.GetExecutingAssembly();

                string mdDirectory = Path.Combine(_app.WorkingFolder, "md");
                string outputPath = Path.Combine(_app.WorkingFolder, "Publish", "js");
                string outputJs;

                var pages = new Dictionary<string, PageInfo>();
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string resourceName = "script.js";

                string fullCSSResourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("style.css"));

                if (string.IsNullOrEmpty(fullCSSResourceName))
                {
                    await WriteToLog($"Resource not found: {resourceName}");
                }


                // Javascriptのリソース
                string fullJSResourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("script.js"));

                if (string.IsNullOrEmpty(fullJSResourceName))
                {
                    await WriteToLog($"Resource not found: {resourceName}");
                }

                // リソースストリームを開く
                using (Stream resourceStream = assembly.GetManifestResourceStream(fullJSResourceName))
                {
                    if (resourceStream == null)
                    {
                        await WriteToLog($"Unable to open resource stream: {fullJSResourceName}");
                    }

                    foreach (var file in Directory.GetFiles(mdDirectory, "*.md"))
                    {
                        await WriteToLog($"マークダウンファイル: '{Path.GetFileName(file)}' の読み込みを開始...");

                        string content = File.ReadAllText(file);
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        string htmlContent = Markdown.ToHtml(content, pipeline);

                        var match = Regex.Match(fileName, @"^(\d+(?:-\d+)*)\s*(.*)$");
                        if (match.Success)
                        {
                            string number = match.Groups[1].Value;
                            string title = match.Groups[2].Value;
                            pages[fileName] = new PageInfo
                            {
                                Number = number,
                                Title = title.Trim('.'),
                                Content = htmlContent
                            };
                        }
                        else
                        {
                            pages[fileName] = new PageInfo
                            {
                                Number = "",
                                Title = fileName,
                                Content = htmlContent
                            };
                        }
                        await WriteToLog($"マークダウンファイル: '{Path.GetFileName(file)}' をhtmlに変換しました。");
                    }

                    AppConfig appConfig = new AppConfig
                    {
                        DocumentTitle = _app.ProjectName,
                        MarkdownDirectory = Path.Combine(_app.WorkingFolder, "md"),
                        OutputJsPath = Path.Combine(_app.WorkingFolder, "js"),
                        ShowNumbers = false
                    };
                    string appConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
                    string pagesJson = JsonConvert.SerializeObject(pages, Formatting.Indented);

                    string templateJs;
                    using (StreamReader reader = new StreamReader(resourceStream, Encoding.UTF8))
                    {
                        templateJs = reader.ReadToEnd();
                    }

                    outputJs = templateJs
                        .Replace("// PAGES_PLACEHOLDER", $"const pages = {pagesJson};")
                        .Replace("// APPCONFIG_PLACEHOLDER", $"const appConfig = {appConfigJson};");

                    // 出力ディレクトリが存在しない場合は作成
                    string outputDir = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(outputDir))
                    {
                        await WriteToLog($"フォルダ：{outputPath}が見つかりませんでした。生成します...");
                        Directory.CreateDirectory(outputDir);
                    }
                }
                string filePath = Path.Combine(outputPath, resourceName);
                File.WriteAllText(filePath, outputJs, Encoding.UTF8);
                await WriteToLog($"リソースファイル: 'script.js'を {outputPath}に生成しました。");
            }
            catch (Exception ex)
            {
                await WriteToLog($"Error extracting resource: {ex.Message}");
                throw ex;
            }

        }
        public async Task ExtractEmbeddedResource(string resourceName, string outputPath)
        {
            try
            {
                // 現在の実行アセンブリを取得
                Assembly assembly = Assembly.GetExecutingAssembly();

                // リソースのフルネームを取得（名前空間を含む）
                string fullResourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith(resourceName));

                if (string.IsNullOrEmpty(fullResourceName))
                {
                    await WriteToLog($"Resource not found: {resourceName}");
                }

                // リソースストリームを開く
                using (Stream resourceStream = assembly.GetManifestResourceStream(fullResourceName))
                {
                    if (resourceStream == null)
                    {
                        await WriteToLog($"Unable to open resource stream: {fullResourceName}");
                    }

                    // 出力ディレクトリが存在しない場合は作成
                    string outputDir = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(outputDir))
                    {
                        await WriteToLog($"フォルダ：{outputDir}が見つかりませんでした。生成します...");
                        Directory.CreateDirectory(outputDir);
                    }

                    string content;
                    using (StreamReader reader = new StreamReader(resourceStream, Encoding.UTF8))
                    {
                        content = reader.ReadToEnd();
                    }
                    string filePath = System.IO.Path.Combine(outputPath, resourceName);
                    File.WriteAllText(filePath, content);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteToLog($"アクセスが拒否されました。アプリケーションに必要な権限があることを確認してください。: {ex.Message}");
                throw ex;
            }
            catch (IOException ex)
            {
                await WriteToLog($"IOエラーが発生しました。ファイルは別のプロセスで使用されている可能性があります: {ex.Message}");
                throw ex;
            }
            catch (Exception ex)
            {
                await WriteToLog($"リソースの抽出中に予期しないエラーが発生しました。: {ex.Message}");
                await WriteToLog($"Stack Trace: {ex.StackTrace}");
                throw ex;
            }
        }
        public async Task CreateDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                await WriteToLog($"フォルダ'{directory}'が作成されました。");
                Directory.CreateDirectory(directory);
            }
        }
    }
    class PageInfo
    {
        public string Number { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
    class AppConfig
    {
        public string DocumentTitle { get; set; }
        public string MarkdownDirectory { get; set; }
        public string OutputJsPath { get; set; }
        public bool ShowNumbers { get; set; }
    }
}