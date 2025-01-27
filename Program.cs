using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace HostMe
{
    public static class Program
    {
        public static int Port = 0;
        public static string Url = "";
        public static string Root = "";

        [STAThread]
        public static void Main()
        {
            try
            {
                Root = Environment.CurrentDirectory;
                if (!Root.EndsWith("\\")) { Root += "\\"; }
                Root = "D:\\ImportantData\\Coding\\EzMusic\\";

                // Try port 8080 and if it's not availible use a random port.
                try
                {
                    TcpListener tcpListener = new TcpListener(IPAddress.Any, 8080);
                    tcpListener.Start();
                    Port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
                    tcpListener.Stop();
                }
                catch
                {
                    TcpListener tcpListener = new TcpListener(IPAddress.Any, 0);
                    tcpListener.Start();
                    Port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
                    tcpListener.Stop();
                }

                Url = $"http://localhost:{Port}/";

                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(Url);
                listener.Start();

                CrossPlatformOpenUrl(Url);

                Console.WriteLine($"Hosting \"{Root}\" at \"{Url}\"...");
                Console.WriteLine();

                while (true)
                {
                    try
                    {
                        HttpListenerContext context = listener.GetContext();
                        ProcessRequest(context);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: \"{ex.Message}\".");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: \"{ex.Message}\".");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        public static void CrossPlatformOpenUrl(string url)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    // Linux or unix
                    Process.Start("xdg-open", url);
                }
                else
                {
                    // Windows
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Url;
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        public static void ProcessRequest(HttpListenerContext context)
        {
            // Reject requests which have a body or are not a get request
            if (context.Request.HasEntityBody
                || context.Request.HttpMethod.ToUpper() != "GET"
                || context.Request.Url.Query != ""
                || context.Request.Url.LocalPath.Contains("/../")
                || context.Request.Url.LocalPath.Contains("/./")
                || context.Request.Url.LocalPath.Contains("\\")
                || !context.Request.Url.LocalPath.StartsWith("/"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
                return;
            }

            // Auto downgrade https requests as encryption is not yet supported
            if (context.Request.Url.Scheme == Uri.UriSchemeHttps)
            {
                context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                string httpUrl = context.Request.Url.AbsoluteUri.Replace("https://", "http://");
                context.Response.Redirect(httpUrl);
                context.Response.Close();
                return;
            }

            string filePath = context.Request.Url.LocalPath;
            if (filePath == "/")
            {
                filePath = "/index.html";
            }
            if (filePath.StartsWith("/"))
            {
                filePath = filePath.Substring(1);
            }
            filePath = filePath.Replace("/", "\\");
            filePath = Path.Combine(Root, filePath);

            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            FileInfo fileInfo = new FileInfo(filePath);

            string rangeHeader = context.Request.Headers["Range"];
            if (rangeHeader == null || rangeHeader == "")
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                context.Response.StatusCode = 200;
                context.Response.ContentType = GetContentType(filePath);
                context.Response.ContentLength64 = fileBytes.LongLength;
                context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                context.Response.Close();
                return;
            }

            Match rangeMatch = Regex.Match(rangeHeader, @"bytes=(\d+)-(\d+)?");
            if (!rangeMatch.Success)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }
            int start = int.Parse(rangeMatch.Groups[1].Value);
            int end = rangeMatch.Groups[2].Success ? int.Parse(rangeMatch.Groups[2].Value) : (int)fileInfo.Length - 1;
            if (start >= fileInfo.Length)
            {
                context.Response.StatusCode = 416;
                context.Response.Close();
                return;
            }

            {
                end = Math.Min(end, (int)fileInfo.Length - 1);
                int contentLength = end - start + 1;
                byte[] fileBytes = File.ReadAllBytes(filePath);
                context.Response.StatusCode = 206;
                context.Response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileInfo.Length}");
                context.Response.ContentType = GetContentType(filePath);
                context.Response.ContentLength64 = contentLength;
                context.Response.OutputStream.Write(fileBytes, start, contentLength);
                context.Response.Close();
                return;
            }
        }
        public static string GetContentType(string filePath)
        {
            Tuple<string, string>[] lookupTable = new Tuple<string, string>[] {
            new Tuple<string, string>(".aac", "audio/aac"),
            new Tuple<string, string>(".abw", "application/x-abiword"),
            new Tuple<string, string>(".apng", "image/apng"),
            new Tuple<string, string>(".arc", "application/x-freearc"),
            new Tuple<string, string>(".avif", "image/avif"),
            new Tuple<string, string>(".avi", "video/x-msvideo"),
            new Tuple<string, string>(".azw", "application/vnd.amazon.ebook"),
            new Tuple<string, string>(".bin", "application/octet-stream"),
            new Tuple<string, string>(".bmp", "image/bmp"),
            new Tuple<string, string>(".bz", "application/x-bzip"),
            new Tuple<string, string>(".bz2", "application/x-bzip2"),
            new Tuple<string, string>(".cda", "application/x-cdf"),
            new Tuple<string, string>(".csh", "application/x-csh"),
            new Tuple<string, string>(".css", "text/css"),
            new Tuple<string, string>(".csv", "text/csv"),
            new Tuple<string, string>(".doc", "application/msword"),
            new Tuple<string, string>(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            new Tuple<string, string>(".eot", "application/vnd.ms-fontobject"),
            new Tuple<string, string>(".epub", "application/epub+zip"),
            new Tuple<string, string>(".gz", "application/gzip"),
            new Tuple<string, string>(".gif", "image/gif"),
            new Tuple<string, string>(".htm", "text/html"),
            new Tuple<string, string>(".html", "text/html"),
            new Tuple<string, string>(".ico", "image/vnd.microsoft.icon"),
            new Tuple<string, string>(".ics", "text/calendar"),
            new Tuple<string, string>(".jar", "application/java-archive"),
            new Tuple<string, string>(".jpeg", "image/jpeg"),
            new Tuple<string, string>(".jpg", "image/jpeg"),
            new Tuple<string, string>(".js", "text/javascript"),
            new Tuple<string, string>(".json", "application/json"),
            new Tuple<string, string>(".jsonld", "application/ld+json"),
            new Tuple<string, string>(".mid", "audio/midi"),
            new Tuple<string, string>(".midi", "audio/midi"),
            new Tuple<string, string>(".mjs", "text/javascript"),
            new Tuple<string, string>(".mp3", "audio/mpeg"),
            new Tuple<string, string>(".mp4", "video/mp4"),
            new Tuple<string, string>(".mpeg", "video/mpeg"),
            new Tuple<string, string>(".mpkg", "application/vnd.apple.installer+xml"),
            new Tuple<string, string>(".odp", "application/vnd.oasis.opendocument.presentation"),
            new Tuple<string, string>(".ods", "application/vnd.oasis.opendocument.spreadsheet"),
            new Tuple<string, string>(".odt", "application/vnd.oasis.opendocument.text"),
            new Tuple<string, string>(".oga", "audio/ogg"),
            new Tuple<string, string>(".ogv", "video/ogg"),
            new Tuple<string, string>(".ogx", "application/ogg"),
            new Tuple<string, string>(".opus", "audio/ogg"),
            new Tuple<string, string>(".otf", "font/otf"),
            new Tuple<string, string>(".png", "image/png"),
            new Tuple<string, string>(".pdf", "application/pdf"),
            new Tuple<string, string>(".php", "application/x-httpd-php"),
            new Tuple<string, string>(".ppt", "application/vnd.ms-powerpoint"),
            new Tuple<string, string>(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
            new Tuple<string, string>(".rar", "application/vnd.rar"),
            new Tuple<string, string>(".rtf", "application/rtf"),
            new Tuple<string, string>(".sh", "application/x-sh"),
            new Tuple<string, string>(".svg", "image/svg+xml"),
            new Tuple<string, string>(".tar", "application/x-tar"),
            new Tuple<string, string>(".tif", "image/tiff"),
            new Tuple<string, string>(".tiff", "image/tiff"),
            new Tuple<string, string>(".ts", "video/mp2t"),
            new Tuple<string, string>(".ttf", "font/ttf"),
            new Tuple<string, string>(".txt", "text/plain"),
            new Tuple<string, string>(".vsd", "application/vnd.visio"),
            new Tuple<string, string>(".wav", "audio/wav"),
            new Tuple<string, string>(".weba", "audio/webm"),
            new Tuple<string, string>(".webm", "video/webm"),
            new Tuple<string, string>(".webp", "image/webp"),
            new Tuple<string, string>(".woff", "font/woff"),
            new Tuple<string, string>(".woff2", "font/woff2"),
            new Tuple<string, string>(".xhtml", "application/xhtml+xml"),
            new Tuple<string, string>(".xls", "application/vnd.ms-excel"),
            new Tuple<string, string>(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            new Tuple<string, string>(".xml", "application/xml"),
            new Tuple<string, string>(".xul", "application/vnd.mozilla.xul+xml"),
            new Tuple<string, string>(".zip", "application/zip."),
            new Tuple<string, string>(".3gp", "video/3gpp"),
            new Tuple<string, string>(".3g2", "video/3gpp2"),
            new Tuple<string, string>(".7z", "application/x-7z-compressed"),
            };

            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".webm")
            {
                return "audio/webm";
            }
            foreach (Tuple<string, string> entry in lookupTable)
            {
                if (entry.Item1.ToLower() == ext)
                {
                    return entry.Item2;
                }
            }
            return "application/octet-stream";
        }
    }
}