using System;
using System.IO;
using RehobothMediaPro.Helpers;

namespace RehobothMediaPro.Helpers
{
    public class ChromeHelper
    {
        private static LiveDisplay liveDisplayForm = null;
        private static LiveDisplay presentationDisplayForm = null; // SECOND SCREEN!

        public void OpenChrome(bool useDualDisplay)
        {
            string baseDir = FileHelper.GetAppFolder();
            string greenScreenUri = new Uri(Path.Combine(baseDir, @"WebTemplate\SongLive.html")).AbsoluteUri;
            string presentationUri = new Uri(Path.Combine(baseDir, @"WebTemplate\SongPresentation.html")).AbsoluteUri;

            // Always open Screen 1 (Green Screen)
            if (liveDisplayForm == null || liveDisplayForm.IsDisposed)
            {
                liveDisplayForm = new LiveDisplay(greenScreenUri, 1);
                liveDisplayForm.Show();
            }

            // Only open Screen 2 if the user explicitly checked the box!
            if (useDualDisplay)
            {
                if (presentationDisplayForm == null || presentationDisplayForm.IsDisposed)
                {
                    presentationDisplayForm = new LiveDisplay(presentationUri, 2);
                    presentationDisplayForm.Show();
                }
            }
        }

        public void CloseChrome()
        {
            if (liveDisplayForm != null && !liveDisplayForm.IsDisposed) liveDisplayForm.Close();
            if (presentationDisplayForm != null && !presentationDisplayForm.IsDisposed) presentationDisplayForm.Close();
        }

        public void RefreshChrome()
        {
            if (liveDisplayForm != null && !liveDisplayForm.IsDisposed) liveDisplayForm.RefreshBrowser();
            if (presentationDisplayForm != null && !presentationDisplayForm.IsDisposed) presentationDisplayForm.RefreshBrowser();
        }

        // We leave these empty so the app doesn't crash, but we don't need them anymore!
        public void LeftMouseClick(int xpos, int ypos) { }
        public void SetCursorPosition(int xpos, int ypos) { }

        // Magically play/pause the video on the Live Display using Javascript!
        public void ToggleVideoPlayback(bool play)
        {
            if (liveDisplayForm != null && !liveDisplayForm.IsDisposed && liveDisplayForm.webView != null && liveDisplayForm.webView.CoreWebView2 != null)
            {
                // If true, play. If false, pause.
                string script = play ? "var v = document.querySelector('video'); if(v) v.play();" : "var v = document.querySelector('video'); if(v) v.pause();";
                liveDisplayForm.webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
        // Instantly seeks the video to a specific percentage (0 to 100)
        public void SeekVideo(double percentage)
        {
            if (liveDisplayForm != null && !liveDisplayForm.IsDisposed && liveDisplayForm.webView != null && liveDisplayForm.webView.CoreWebView2 != null)
            {
                // Math: (Percentage / 100) * Total Video Duration
                string script = $"var v = document.querySelector('video'); if(v) v.currentTime = (v.duration * ({percentage} / 100));";
                liveDisplayForm.webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
        public void ExecuteJavascriptOnLive(string script)
        {
            if (liveDisplayForm != null && !liveDisplayForm.IsDisposed && liveDisplayForm.webView != null && liveDisplayForm.webView.CoreWebView2 != null)
            {
                liveDisplayForm.webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
    }
}