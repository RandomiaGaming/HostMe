using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace HostMe
{
    public static class Program
    {
        public static int Port = 0;
        public static string URL = "";
        public static string Root = "";

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Root = Environment.CurrentDirectory;

                // Try port 8000 and if it's not availible use a random port.
                try
                {
                    TcpListener tcpListener = new TcpListener(IPAddress.Any, 8000);
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

                URL = $"http://localhost:{Port}/";
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(URL);
                listener.Start();

                // Try to launch index.html but if it doesn't exist use a random html file and if there are none then give up.
                if (File.Exists(Root + "/index.html"))
                {
                    Process.Start(URL);
                }
                else
                {
                    string[] htmlFiles = Directory.GetFiles(Root, "*.html");
                    if (htmlFiles != null && htmlFiles.Length > 0)
                    {
                        Process.Start(URL + htmlFiles[0].Substring(Root.Length));
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Hosting \"{Root}\" at \"{URL}\".");
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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: \"{ex.Message}\".");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal Error: \"{ex.Message}\".");
            }
        }
        public static void ProcessRequest(HttpListenerContext context)
        {
            if (context.Request.Url.Scheme == Uri.UriSchemeHttps)
            {
                string redirectUrl = $"http://{context.Request.Url.Host}{context.Request.Url.PathAndQuery}";
                context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                context.Response.Redirect(redirectUrl);
                context.Response.Close();
                return;
            }

            string requestUrl = context.Request.Url.AbsolutePath;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Got request for url \"{requestUrl}\"");

            if (requestUrl == "" || requestUrl == null || requestUrl == "/" || requestUrl == "\\")
            {
                requestUrl = "/index.html";
            }
            string filePath = Root + requestUrl;

            if (File.Exists(filePath))
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                context.Response.ContentType = GetContentType(filePath);
                context.Response.ContentLength64 = fileBytes.LongLength;
                context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Served file \"{filePath}\" to client.");
            }
            else
            {
                context.Response.StatusCode = 404;

                // Redirect to the 404 page if it exists otherwise just return the 404 status code only.
                if (File.Exists("404.html"))
                {
                    context.Response.Redirect("/404.html");
                }

                Console.WriteLine($"Unable to server file \"{filePath}\" because it does not exist.");
            }

            context.Response.Close();

            Console.WriteLine();
        }
        public static string GetContentType(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "text/javascript";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                default:
                    return "application/octet-stream";
            }
        }
    }
}