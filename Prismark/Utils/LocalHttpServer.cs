using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Prismark.Utils
{
    public class LocalHttpServer
    {
        private HttpListener _listener;
        private string _mediaDirectory;
        private string _serverUrl;

        public LocalHttpServer(string mediaDirectory, int port = 8080)
        {
            _mediaDirectory = mediaDirectory;
            _serverUrl = $"http://localhost:{port}/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(_serverUrl);
        }

        public void Start()
        {
            _listener.Start();
            Task.Run(() => HandleRequests());
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private async Task HandleRequests()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var response = context.Response;

                    string requestedFile = context.Request.Url.LocalPath.TrimStart('/');
                    string filePath = System.IO.Path.Combine(_mediaDirectory, requestedFile);

                    if (System.IO.File.Exists(filePath))
                    {
                        string contentType = GetContentType(filePath);
                        response.ContentType = contentType;

                        if (contentType.StartsWith("video/"))
                        {
                            // 動画ファイルの場合はストリーミング
                            await StreamVideoAsync(filePath, context.Request, response);
                        }
                        else
                        {
                            // その他のファイルは通常通り送信
                            byte[] buffer = System.IO.File.ReadAllBytes(filePath);
                            response.ContentLength64 = buffer.Length;
                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        response.StatusCode = 404;
                    }

                    response.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling request: {ex.Message}");
                }
            }
        }

        private async Task StreamVideoAsync(string filePath, HttpListenerRequest request, HttpListenerResponse response)
        {
            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                response.ContentLength64 = fileStream.Length;
                response.AddHeader("Accept-Ranges", "bytes");

                if (request.Headers["Range"] != null)
                {
                    // 範囲リクエストの処理
                    string[] ranges = request.Headers["Range"].Split('=')[1].Split('-');
                    long start = long.Parse(ranges[0]);
                    long end = ranges.Length > 1 && !string.IsNullOrEmpty(ranges[1]) ? long.Parse(ranges[1]) : fileStream.Length - 1;

                    response.StatusCode = 206;
                    response.AddHeader("Content-Range", $"bytes {start}-{end}/{fileStream.Length}");
                    response.ContentLength64 = end - start + 1;

                    fileStream.Seek(start, System.IO.SeekOrigin.Begin);
                    await fileStream.CopyToAsync(response.OutputStream, (int)(end - start + 1));
                }
                else
                {
                    // 全体のストリーミング
                    await fileStream.CopyToAsync(response.OutputStream);
                }
            }
        }

        private string GetContentType(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".mp4": return "video/mp4";
                case ".webm": return "video/webm";
                case ".ogg": return "video/ogg";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                default: return "application/octet-stream";
            }
        }

        public string ReplaceMediaPaths(string html)
        {
            return Regex.Replace(html, "<(img|video)[^>]+src\\s*=\\s*['\"]([^'\"]+)['\"][^>]*>", match =>
            {
                string tag = match.Groups[1].Value;
                string originalSrc = match.Groups[2].Value;
                if (originalSrc.StartsWith("http://") || originalSrc.StartsWith("https://") || originalSrc.StartsWith("data:"))
                {
                    return match.Value; // 外部URLやデータURIはそのまま
                }
                string newSrc = _serverUrl + originalSrc;
                return match.Value.Replace(originalSrc, newSrc);
            });
        }
    }
}
