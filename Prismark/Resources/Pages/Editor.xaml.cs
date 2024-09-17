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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Prismark.Resources.Pages
{
    /// <summary>
    /// Editor.xaml の相互作用ロジック
    /// </summary>
    public partial class Editor : Page
    {
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

        }
        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);

            string htmlContent = @"
            <html>
            <head>
                <style>
                    body { 
                        font-family: Arial, sans-serif;
                        background-color: #222;
                        color: #c4c4c4;
                    }
                </style>
            </head>
            <body>
                <h1>Hello, World!</h1>
                <p>This is a paragraph.</p>
            </body>
            </html>";

            webView.NavigateToString(htmlContent);
            PreviewShow();
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
        }

        private void PreviewShow()
        {
            string markdown = MarkDownEditor.Text;
            Convaeters.MarkDownToHTML conv = new Convaeters.MarkDownToHTML();
            webView.NavigateToString(conv.ToUnitHtml(markdown));
        }
    }
   
}
