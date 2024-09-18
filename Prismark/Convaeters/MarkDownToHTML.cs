using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows;
using System.Collections.Generic;
using Markdig;

namespace Prismark.Convaeters
{
    internal class MarkDownToHTML
    {
        public MarkDownToHTML() { }
        public string ToUnitHtml(string md)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string htmlContent = Markdown.ToHtml(md, pipeline);
            string css = GetEmbeddedResourceContent("Prismark.Base.style.css");
            htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        {css}
    </style>
</head>
<body>
    <div class=""content-pane"">
        {htmlContent}
    </div>
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

    }
    class PageInfo
    {
        public string Number { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }

    class AppConfig
    {
        public string MarkdownDirectory { get; set; }
        public string OutputJsPath { get; set; }
        public string TemplateJsPath { get; set; }
        public bool ShowNumbers { get; set; }
    }

}
