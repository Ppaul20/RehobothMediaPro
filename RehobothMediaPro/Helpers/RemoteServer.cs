using EmbedIO;
using EmbedIO.Actions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;

namespace RehobothMediaPro.Helpers
{
    public static class RemoteServer
    {
        private static WebServer _server;
        public static string ServerUrl { get; private set; }
        public static bool IsRunning => _server != null && _server.State == WebServerState.Listening;

        public static void Start()
        {
            if (IsRunning) return;

            string ip = GetLocalIPAddress();
            ServerUrl = $"http://{ip}:8080/";

            // Creates a fast, asynchronous local web server
            _server = new WebServer(o => o.WithUrlPrefix(ServerUrl).WithMode(HttpListenerMode.EmbedIO))
                .WithAction("/next", HttpVerbs.Get, async ctx => { SimulateArrowKey(Key.Right); await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8); })
                .WithAction("/prev", HttpVerbs.Get, async ctx => { SimulateArrowKey(Key.Left); await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8); })
                .WithAction("/up", HttpVerbs.Get, async ctx => { SimulateArrowKey(Key.Up); await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8); })
                .WithAction("/down", HttpVerbs.Get, async ctx => { SimulateArrowKey(Key.Down); await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8); })
                .WithAction("/", HttpVerbs.Get, async ctx => { await ctx.SendStringAsync(GetHtmlInterface(), "text/html", System.Text.Encoding.UTF8); });

            _server.RunAsync();
        }

        public static void Stop()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }
        }

        private static void SimulateArrowKey(Key key)
        {
            // Injects a fake keystroke into the main window, triggering your exact 42 Rule logic!
            Application.Current.Dispatcher.Invoke(() =>
            {
                var target = Keyboard.FocusedElement as UIElement ?? Application.Current.MainWindow;
                var eventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                target.RaiseEvent(eventArgs);
            });
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
            }
            return "127.0.0.1"; // Fallback to localhost
        }

        private static string GetHtmlInterface()
        {
            // A beautiful, dark-mode, touch-friendly UI for your mobile phone!
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1, user-scalable=0'/>
    <style>
        body { background-color: #0F111A; color: white; font-family: 'Segoe UI', sans-serif; display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100vh; margin: 0; }
        h1 { color: #6C5DD3; margin-bottom: 40px; }
        .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; width: 90%; max-width: 400px; }
        .btn { background-color: #2D3047; color: white; border: none; border-radius: 15px; padding: 30px; font-size: 24px; font-weight: bold; box-shadow: 0 5px 15px rgba(0,0,0,0.5); }
        .btn:active { background-color: #6C5DD3; transform: scale(0.95); }
        .btn-wide { grid-column: span 2; background-color: #00B478; }
    </style>
</head>
<body>
    <h1>Rehoboth Remote</h1>
    <div class='grid'>
        <button class='btn btn-wide' onclick='sendCMD(""/up"")'>⬆️ JUMP UP</button>
        <button class='btn' onclick='sendCMD(""/prev"")'>⬅️ PREV</button>
        <button class='btn' onclick='sendCMD(""/next"")'>NEXT ➡️</button>
        <button class='btn btn-wide' onclick='sendCMD(""/down"")'>⬇️ JUMP DOWN</button>
    </div>
    <script>
        function sendCMD(endpoint) { fetch(endpoint); }
    </script>
</body>
</html>";
        }
    }
}