using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace RehobothMediaPro
{
    public class LiveDisplay : Form
    {
        public WebView2 webView;
        private int _monitorIndex;

        // NEW: Accepts a monitor index! (1 for Green Screen, 2 for Presentation)
        public LiveDisplay(string url, int monitorIndex = 1)
        {
            _monitorIndex = monitorIndex;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;

            // FIX: It MUST have a Title to show up on the Taskbar!
            this.Text = monitorIndex == 1 ? "Live Display - Main" : "Live Display - Presentation";
            this.ShowInTaskbar = true;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            InitializeAsync(url);
            SetupMonitor();
        }

        private async void InitializeAsync(string url)
        {
            var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.GetTempPath());
            await webView.EnsureCoreWebView2Async(env);
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Navigate(url);
        }

        private void SetupMonitor()
        {
            Screen[] screens = Screen.AllScreens;
            // If you have 3 screens plugged in (Laptop, OBS, Projector), it perfectly maps them!
            if (screens.Length > _monitorIndex)
            {
                Rectangle bounds = screens[_monitorIndex].Bounds;
                this.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            else if (screens.Length > 1) // Fallback if only 2 screens are plugged in
            {
                Rectangle bounds = screens[1].Bounds;
                this.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            else
            {
                Rectangle bounds = screens[0].Bounds;
                this.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            this.WindowState = FormWindowState.Maximized;
        }

        public void RefreshBrowser()
        {
            if (webView != null && webView.CoreWebView2 != null) webView.CoreWebView2.Reload();
        }
    }
}