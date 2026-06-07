using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace RehobothMediaPro
{
    //...
    public class SongFetcherForm : Form
    {
        public string FetchedLyrics { get; private set; } = "";
        public string FetchedTitle { get; private set; } = ""; // NEW: Stores the web title!

        private TextBox searchBox;
        private WebView2 webView;

        public SongFetcherForm()
        {
            this.Text = "Song Fetcher";
            this.Size = new Size(1100, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ColorTranslator.FromHtml("#0F111A");
            this.ForeColor = Color.White;

            Panel topPanel = new Panel() { Height = 60, Dock = DockStyle.Top, BackColor = ColorTranslator.FromHtml("#1C1E30") };

            Label lbl = new Label() { Text = "Search:", ForeColor = Color.White, AutoSize = true, Location = new Point(15, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            searchBox = new TextBox() { Location = new Point(75, 17), Width = 260, Font = new Font("Segoe UI", 10F) };
            // Allow the user to press Enter to search!
            searchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // Stops the annoying "ding" sound
                    BtnSearch_Click(this, EventArgs.Empty);
                }
            };

            Button btnSearch = new Button() { Text = "Search", Location = new Point(345, 15), Width = 80, Height = 30, BackColor = ColorTranslator.FromHtml("#6C5DD3"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += BtnSearch_Click;

            Button btnBack = new Button() { Text = "⬅️ Back", Location = new Point(435, 15), Width = 80, Height = 30, BackColor = ColorTranslator.FromHtml("#2D3047"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => { if (webView != null && webView.CoreWebView2 != null && webView.CoreWebView2.CanGoBack) webView.CoreWebView2.GoBack(); };

            Button btnAutoImport = new Button() { Text = "✨ Auto-Import", Location = new Point(525, 15), Width = 140, Height = 30, BackColor = ColorTranslator.FromHtml("#0085FF"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAutoImport.FlatAppearance.BorderSize = 0;
            btnAutoImport.Click += BtnAutoImport_Click;

            Button btnImport = new Button() { Text = "⬇️ Import Highlighted", Location = new Point(675, 15), Width = 180, Height = 30, BackColor = ColorTranslator.FromHtml("#00B478"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnImport.FlatAppearance.BorderSize = 0;
            btnImport.Click += BtnImport_Click;

            Label instLbl = new Label() { Text = "Open song, click Auto-Import", ForeColor = Color.LightGray, AutoSize = true, Location = new Point(865, 20), Font = new Font("Segoe UI", 9F, FontStyle.Italic) };

            topPanel.Controls.Add(lbl);
            topPanel.Controls.Add(searchBox);
            topPanel.Controls.Add(btnSearch);
            topPanel.Controls.Add(btnBack);
            topPanel.Controls.Add(btnAutoImport);
            topPanel.Controls.Add(btnImport);
            topPanel.Controls.Add(instLbl);

            webView = new WebView2() { Dock = DockStyle.Fill };

            this.Controls.Add(webView);
            this.Controls.Add(topPanel);

            InitBrowser();
        }

        private async void InitBrowser()
        {
            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.GetTempPath());
            await webView.EnsureCoreWebView2Async(env);

            // Simply open the Google homepage when the window loads
            webView.CoreWebView2.Navigate("https://www.google.com/");
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null && !string.IsNullOrWhiteSpace(searchBox.Text))
            {
                string query = Uri.EscapeDataString(searchBox.Text + " song lyrics");
                webView.CoreWebView2.Navigate("https://www.google.com/search?q=" + query);
            }
        }

        private async void BtnAutoImport_Click(object sender, EventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                string script = @"
                    (function() {
                        var titleNode = document.querySelector('h1') || document.querySelector('.entry-title');
                        var title = titleNode ? titleNode.innerText : document.title;
                        title = title.replace(/lyrics|tamil|christian|songs|-|\|/gi, '').trim();

                        var lyrics = '';
                        // Broadened search to fix christsquare.com!
                        var elements = document.querySelectorAll('.entry-content p, .entry-content div, .post-content p, article p, td');
                        
                        if (elements.length > 0) {
                            var textArray = [];
                            for(var i=0; i<elements.length; i++) {
                                var txt = elements[i].innerText.trim();
                                
                                // SMART FILTER: Stop entirely if we hit the English translation section!
                                if (txt.toLowerCase().includes('english translation')) break;

                                // Ignore junk
                                if(txt.length > 0 && !txt.toLowerCase().includes('share') && !txt.toLowerCase().includes('whatsapp') && !txt.toLowerCase().includes('related')) {
                                    textArray.push(txt);
                                }
                            }
                            // Clean duplicates created by nested divs
                            var uniqueArray = [...new Set(textArray)];
                            lyrics = uniqueArray.join('\n\n');
                        }

                        return title + '|||' + lyrics;
                    })();
                ";

                string result = await webView.CoreWebView2.ExecuteScriptAsync(script);

                if (!string.IsNullOrEmpty(result) && result != "\"\"" && result != "null")
                {
                    string raw = System.Text.RegularExpressions.Regex.Unescape(result.Trim('"'));
                    string[] parts = raw.Split(new string[] { "|||" }, StringSplitOptions.None);

                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        // 42 RULE SAFE: Limit the title to exactly 4 words!
                        string rawTitle = parts[0].Trim();
                        string[] titleWords = rawTitle.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (titleWords.Length > 4)
                        {
                            // Take only the first 4 words and join them with a space
                            FetchedTitle = string.Join(" ", titleWords.Take(4));
                        }
                        else
                        {
                            FetchedTitle = rawTitle;
                        }

                        FetchedLyrics = parts[1].Trim();
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Auto-Import couldn't find the lyrics automatically on this page.\n\nPlease highlight the text with your mouse and use the 'Import Highlighted' button instead!", "Auto-Detect Failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private async void BtnImport_Click(object sender, EventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                string script = "window.getSelection().toString();";
                string result = await webView.CoreWebView2.ExecuteScriptAsync(script);

                if (!string.IsNullOrEmpty(result) && result != "\"\"" && result != "null")
                {
                    FetchedTitle = ""; // Can't auto-guess title on manual highlight
                    FetchedLyrics = System.Text.RegularExpressions.Regex.Unescape(result.Trim('"'));
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Please highlight the lyrics on the web page first with your mouse, then click Import!", "No Text Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}