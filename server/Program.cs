using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string url = "http://localhost:3002/";

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);

        listener.Start();
        Console.WriteLine($"Listening on {url}...");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            Console.WriteLine("Request received!");

            if (context.Request.IsWebSocketRequest)
            {
                _ = HandleWebSocketRequest(context);
            }
            else
            {
                _ = Task.Run(() => HandleRequest(context));
            }
        }
    }

    static async Task HandleWebSocketRequest(HttpListenerContext context)
    {
        WebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
        WebSocket webSocket = wsContext.WebSocket;

        string exePath = @"C:\Program Files\Tencent\WeChat\WeChat.exe";
        IntPtr hwnd = IntPtr.Zero;
        Process process = null;

        Process[] runningProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
        if (runningProcesses.Length > 0)
        {
            process = runningProcesses[0];
            hwnd = process.MainWindowHandle;
        }
        else
        {
            process = Process.Start(exePath);
            while (hwnd == IntPtr.Zero)
            {
                process.Refresh();
                hwnd = process.MainWindowHandle;
            }
        }

        while (webSocket.State == WebSocketState.Open)
        {
            byte[] screenshotBytes = CaptureWindow(hwnd);
            string base64Screenshot = Convert.ToBase64String(screenshotBytes);

            byte[] buffer = Encoding.UTF8.GetBytes(base64Screenshot);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

            await Task.Delay(50); // 调整延迟以平衡性能与视频流质量
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        webSocket.Dispose();
    }

    static void HandleRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        if (request.HttpMethod == "GET")
        {
            string exePath = @"C:\Program Files\Tencent\WeChat\WeChat.exe";
            IntPtr hwnd = IntPtr.Zero;
            Process process = null;

            Process[] runningProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
            if (runningProcesses.Length > 0)
            {
                process = runningProcesses[0];
                hwnd = process.MainWindowHandle;
            }
            else
            {
                process = Process.Start(exePath);
                while (hwnd == IntPtr.Zero)
                {
                    process.Refresh();
                    hwnd = process.MainWindowHandle;
                }
            }

            byte[] screenshotBytes = CaptureWindow(hwnd);
            string base64Screenshot = Convert.ToBase64String(screenshotBytes);

            response.ContentType = "text/plain";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = Encoding.UTF8.GetByteCount(base64Screenshot);

            using (var output = response.OutputStream)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(base64Screenshot);
                output.Write(buffer, 0, buffer.Length);
            }

            Console.WriteLine("Response sent.");
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            response.Close();
        }
    }

    static byte[] CaptureWindow(IntPtr hwnd)
    {
        RECT rect;
        GetWindowRect(hwnd, out rect);

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        Bitmap bmp = new Bitmap(width * 125 / 100, height * 125 / 100, PixelFormat.Format32bppArgb);
        using (Graphics gfx = Graphics.FromImage(bmp))
        {
            IntPtr hdc = gfx.GetHdc();
            PrintWindow(hwnd, hdc, 0);
            gfx.ReleaseHdc(hdc);
        }

        using (MemoryStream ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
