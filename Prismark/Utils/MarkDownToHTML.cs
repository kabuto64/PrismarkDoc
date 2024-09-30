using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows;
using System.Collections.Generic;
using Markdig;

namespace Prismark.Utils
{
    internal class MarkDownToHTML
    {
        public MarkDownToHTML() { }
        public string ToViewHtml(string md)
        {
            string htmlContent = ConvertMarkdownToHtml(md);
            string css = GetEmbeddedResourceContent("Prismark.Resources.style.css");
            string highlightcss = GetEmbeddedResourceContent("Prismark.Resources.highlightjs.atom-one-dark.min.css");
            string highlightjs = GetEmbeddedResourceContent("Prismark.Resources.highlightjs.highlight.min.js");
            string highlightjslinenumbers = GetEmbeddedResourceContent("Prismark.Resources.highlightjs.highlightjs-line-numbers.js");
            htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        {css}
    </style>
    <style>
        {highlightcss}
    </style>
    <script>
        {highlightjs}
    </script>    
    <script>
        {highlightjslinenumbers}
    </script>    
</head>
<body>
    <div class=""content-pane"" id=""content"">
        {htmlContent}
    </div>
    <script>
        function updateContent(html) {{
            document.getElementById('content').innerHTML = html;
            hljs.highlightAll();
            document.querySelectorAll('pre code').forEach(function (block) {{
                const languageClass = Array.from(block.classList).find(cls => cls.startsWith('language-'));
                if (languageClass) {{
                    const language = languageClass.replace('language-', '');
                    const label = document.createElement('span');
                    label.className = 'language-label';
                    label.textContent = language;
                    block.parentNode.insertBefore(label, block);
                }}
            }});
        }}
    </script>
</body>
</html>";

            return htmlContent;
        }
        private string GetEmbeddedResourceContent(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public string ConvertMarkdownToHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            // 改行コードを正規化
            markdown = Regex.Replace(markdown, "\r\n?", "\n");

            StringBuilder result = new StringBuilder();
            bool inCodeBlock = false;
            bool inTable = false;
            string[] lines = markdown.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string nextLine = i < lines.Length - 1 ? lines[i + 1] : "";

                // コードブロックの開始/終了をチェック
                if (line.StartsWith("```") || line.StartsWith("~~~"))
                {
                    inCodeBlock = !inCodeBlock;
                    result.AppendLine(line);
                    continue;
                }

                // コードブロック内の場合は変更せずにそのまま追加
                if (inCodeBlock)
                {
                    result.AppendLine(line);
                    continue;
                }

                // テーブルの開始をチェック
                if (!inTable && line.Contains("|") && nextLine.Replace(" ", "").Replace("|", "").Replace("-", "").Length == 0)
                {
                    inTable = true;
                    result.AppendLine(line);
                    continue;
                }

                // テーブル内の処理
                if (inTable)
                {
                    result.AppendLine(line);
                    // テーブルの終了をチェック（次の行が空行またはテーブル形式でない場合）
                    if (string.IsNullOrWhiteSpace(nextLine) || !nextLine.Contains("|"))
                    {
                        inTable = false;
                    }
                    continue;
                }

                // 箇条書きや引用のチェック
                if (Regex.IsMatch(line, @"^(\s*[-*+]|\d+\.|\>)\s"))
                {
                    result.AppendLine(line);
                    continue;
                }

                // 空行のチェック
                if (string.IsNullOrWhiteSpace(line))
                {
                    result.AppendLine(line);
                    continue;
                }

                // 単一の改行を<br>に変換
                if (i < lines.Length - 1 && !string.IsNullOrWhiteSpace(lines[i + 1]))
                {
                    result.AppendLine(line + "  ");
                }
                else
                {
                    result.AppendLine(line);
                }
            }

            // マークダウンをHTMLに変換
            string html = Markdown.ToHtml(result.ToString(), pipeline);

            // 結果をHTMLドキュメントにラップ
            return html;
        }
    }
}