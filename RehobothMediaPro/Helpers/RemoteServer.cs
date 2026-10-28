using EmbedIO;
using EmbedIO.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace RehobothMediaPro.Helpers
{
    public static class RemoteServer
    {
        private static WebServer _server;
        public static string ServerUrl { get; private set; }
        public static bool IsRunning => _server != null && _server.State == WebServerState.Listening;

        // An event that tells the Main Window when the phone clicks a lyric!
        public static event Action<string> OnSongSelected;
        public static event Action<int> OnLyricClicked;

        public static void Start()
        {
            if (IsRunning) return;

            string ip = GetLocalIPAddress();
            ServerUrl = $"http://{ip}:8080/";

            _server = new WebServer(o => o.WithUrlPrefix(ServerUrl).WithMode(HttpListenerMode.EmbedIO))
                // 1. API: Send the full list of songs to the phone!
                .WithAction("/api/songs", HttpVerbs.Get, async ctx =>
                {
                    var fileHelper = new FileHelper();
                    var songs = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
                    string json = JsonSerializer.Serialize(songs);
                    await ctx.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
                })
                // FIX: Synchronized loading! The phone waits for the PC to physically finish building the buttons!
                // FIX: Blazing fast background processing! Reads the file instantly!
                // FIX: 100% Non-Blocking Async Request. The phone gets the data instantly!
                // FIX: Forceful File Reader with Diagnostic Output!
                // FIX: Safely grabs the ACTUAL MainWindow, completely ignoring Visual Studio's hidden testing tools!
                // FIX: Flawless PC-to-Phone Sync! The phone asks the PC to do the math and copies the EXACT text from the 42 buttons!
                .WithAction("/api/loadsong", HttpVerbs.Get, async ctx =>
                {
                    string songName = ctx.Request.QueryString["song"];
                    List<string> activeLyrics = new List<string>();

                    if (!string.IsNullOrEmpty(songName))
                    {
                        try
                        {
                            // Use InvokeAsync to prevent the server from hanging!
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                // 1. Tell the PC to load the song, apply the checkboxes, and hit Submit!
                                OnSongSelected?.Invoke(songName);

                                // 2. Read the exact text physically sitting on the 42 PC buttons!
                                foreach (Window window in Application.Current.Windows)
                                {
                                    if (window is MainWindow mainWin && mainWin.SongLyricsbuttons != null)
                                    {
                                        foreach (var btn in mainWin.SongLyricsbuttons)
                                        {
                                            if (btn.Visibility == Visibility.Visible && btn.Content is System.Windows.Controls.TextBlock tb)
                                            {
                                                // Grabs the clean text so the phone screen matches the PC screen 1-to-1!
                                                activeLyrics.Add(System.Net.WebUtility.HtmlEncode(tb.Text));
                                            }
                                        }
                                        break;
                                    }
                                }
                            });
                        }
                        catch { }
                    }

                    // 3. Send the perfect array back to the phone!
                    string json = JsonSerializer.Serialize(activeLyrics);
                    await ctx.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
                })
                // NEW API: The Phone tells the PC to toggle Presentation Mode!
                // NEW API: The Phone tells the PC to toggle Presentation Mode!
                .WithAction("/action/togglepres", HttpVerbs.Get, async ctx =>
                {
                    string mode = ctx.Request.QueryString["mode"]; // "true" or "false"
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin)
                            {
                                // Physically check or uncheck the box on the main laptop!
                                mainWin.chkPresentation.IsChecked = (mode == "true");

                                // FIX: Force the PC to physically rebuild the 42 buttons with the new paragraph formatting!
                                if (!string.IsNullOrEmpty(mainWin.LyricsInputBox.Text))
                                {
                                    mainWin.BtnSubmit_Click(null, null);
                                }

                                // Updates the background colors and UI
                                mainWin.ChkPresentation_Click(null, null);
                                break;
                            }
                        }
                    });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // NEW API: Toggles the 3 Dual-Language Checkboxes from the phone!
                .WithAction("/action/togglelang", HttpVerbs.Get, async ctx =>
                {
                    string target = ctx.Request.QueryString["target"]; // "op", "multi", or "dual"
                    string mode = ctx.Request.QueryString["mode"];     // "true" or "false"

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin)
                            {
                                bool isChecked = (mode == "true");

                                // Physically flip the boxes on the PC!
                                if (target == "op") mainWin.chkOperatorMode.IsChecked = isChecked;
                                else if (target == "multi") mainWin.chkMultilingual.IsChecked = isChecked;
                                else if (target == "dual") { mainWin.chkDualEditor.IsChecked = isChecked; mainWin.ChkDualEditor_Click(null, null); }

                                // Force the PC to rebuild the 42 buttons with the new language rules!
                                if (!string.IsNullOrEmpty(mainWin.LyricsInputBox.Text))
                                {
                                    mainWin.BtnSubmit_Click(null, null);
                                }
                                break;
                            }
                        }
                    });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // NEW API: Triggers the Green Screen Wipe!
                .WithAction("/action/greenscreen", HttpVerbs.Get, async ctx =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin)
                            {
                                // Simulates clicking the Green Screen button on the PC!
                                mainWin.BtnGreenScreen_Click(null, null);
                                break;
                            }
                        }
                    });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // --- NEW API: BIBLE REMOTE CONTROLS ---
                .WithAction("/api/biblesearch", HttpVerbs.Get, async ctx =>
                {
                    string query = ctx.Request.QueryString["q"];
                    List<string> chapterList = new List<string>();

                    if (!string.IsNullOrEmpty(query))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window is MainWindow mainWin)
                                {
                                    if (mainWin.BibleDashboard.Visibility != Visibility.Visible) mainWin.BtnNavBible_Click(null, null);
                                    mainWin.BibleSearchBox.Text = query;
                                    mainWin.BibleSearchBox_KeyDown(null, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(mainWin), 0, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent });

                                    // Rip the generated Chapter Preview list straight from the PC!
                                    if (mainWin.ChapterVerseList.Items != null)
                                    {
                                        foreach (var item in mainWin.ChapterVerseList.Items)
                                        {
                                            if (item is MainWindow.VersePreviewItem vp) chapterList.Add(System.Net.WebUtility.HtmlEncode(vp.PrimaryText));
                                        }
                                    }
                                    break;
                                }
                            }
                        });
                    }
                    string json = JsonSerializer.Serialize(chapterList);
                    await ctx.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
                })
                .WithAction("/action/bibleprev", HttpVerbs.Get, async ctx =>
                {
                    Application.Current.Dispatcher.Invoke(() => { foreach (Window w in Application.Current.Windows) if (w is MainWindow m) { m.BtnPrevVerse_Click(null, null); break; } });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                .WithAction("/action/biblenext", HttpVerbs.Get, async ctx =>
                {
                    Application.Current.Dispatcher.Invoke(() => { foreach (Window w in Application.Current.Windows) if (w is MainWindow m) { m.BtnNextVerse_Click(null, null); break; } });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                .WithAction("/action/biblejump", HttpVerbs.Get, async ctx =>
                {
                    string idxStr = ctx.Request.QueryString["index"];
                    if (int.TryParse(idxStr, out int idx))
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            foreach (Window w in Application.Current.Windows) if (w is MainWindow m)
                            {
                                m.ChapterVerseList.SelectedIndex = idx;
                                m.ChapterVerseList_SelectionChanged(null, null); break;
                            }
                        });
                    }
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // NEW API: Toggles the Bible Languages from the phone!
                // NEW API: Toggles the Bible Languages via the safe bridge!
                .WithAction("/action/biblelang", HttpVerbs.Get, async ctx =>
                {
                    string lang = ctx.Request.QueryString["lang"];
                    bool isChecked = ctx.Request.QueryString["state"] == "true";

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin)
                            {
                                // FIX: Use the public bridge instead of trying to touch the private checkboxes!
                                mainWin.ToggleBibleLanguageFromRemote(lang, isChecked);
                                break;
                            }
                        }
                    });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // NEW API: Triggers the Theme Loop Wipe!
                .WithAction("/action/theme", HttpVerbs.Get, async ctx =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin)
                            {
                                // Simulates clicking the Theme button on the PC!
                                mainWin.BtnThemeLogo_Click(null, null);
                                break;
                            }
                        }
                    });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // NEW API: Triggers the PC's Submit button!
                .WithAction("/action/submit", HttpVerbs.Get, async ctx =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin)
                            {
                                // Simulates clicking the Submit button on the PC!
                                mainWin.BtnSubmit_Click(null, null);
                                break;
                            }
                        }
                    });
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // FIX: Triggers the PC to load an existing song!
                .WithAction("/action/selectsong", HttpVerbs.Get, async ctx =>
                {
                    string songName = ctx.Request.QueryString["song"];
                    if (!string.IsNullOrEmpty(songName))
                    {
                        Application.Current.Dispatcher.Invoke(() => OnSongSelected?.Invoke(songName));
                    }
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // NEW API: Sends the raw text of a saved song to the Phone's edit boxes!
                .WithAction("/api/rawsong", HttpVerbs.Get, async ctx =>
                {
                    string songName = ctx.Request.QueryString["song"];
                    var result = new { primary = "", secondary = "" };

                    if (!string.IsNullOrEmpty(songName))
                    {
                        string fullPath = System.IO.Path.Combine(FileHelper.GetAppFolder(), $@"WebTemplate\SongDataBase\{songName}.txt");
                        if (System.IO.File.Exists(fullPath))
                        {
                            string rawText = System.IO.File.ReadAllText(fullPath).Replace("\r\n", "\n").Replace("\r", "\n");
                            string[] parts = rawText.Split(new string[] { "\n===TANGLISH===\n" }, StringSplitOptions.None);

                            // Splits it cleanly for the two mobile boxes!
                            result = new
                            {
                                primary = parts[0],
                                secondary = parts.Length > 1 ? parts[1] : ""
                            };
                        }
                    }
                    string json = JsonSerializer.Serialize(result);
                    await ctx.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
                })
                // NEW API: Grabs whatever lyrics are currently loaded on the PC screen (Even unsaved ones!)
                // FIX: Grabs the exact text off the physical PC buttons so it strips HTML and respects the "I Don't Read Tamil" toggle!
                .WithAction("/api/current_lyrics", HttpVerbs.Get, async ctx =>
                {
                    List<string> activeLyrics = new List<string>();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin && mainWin.SongLyricsbuttons != null)
                            {
                                foreach (var btn in mainWin.SongLyricsbuttons)
                                {
                                    // Reads the clean text exactly as it appears on the PC operator's screen!
                                    if (btn.Visibility == Visibility.Visible && btn.Content is System.Windows.Controls.TextBlock tb)
                                    {
                                        activeLyrics.Add(System.Net.WebUtility.HtmlEncode(tb.Text));
                                    }
                                }
                                break;
                            }
                        }
                    });
                    string json = JsonSerializer.Serialize(activeLyrics);
                    await ctx.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
                })
                // NEW API: Receives raw lyrics pasted from the phone!
                // FIX: Flawlessly splits the pasted payload into the correct Dual Editor boxes!
                // FIX: Flawlessly splits the pasted payload into the correct Dual Editor boxes!
                .WithAction("/action/pastelyrics", HttpVerbs.Post, async ctx =>
                {
                    using (var reader = new System.IO.StreamReader(ctx.Request.InputStream))
                    {
                        string rawPayload = await reader.ReadToEndAsync();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window is MainWindow mainWin)
                                {
                                    if (mainWin.SongDashboard.Visibility != Visibility.Visible)
                                        mainWin.BtnNavSongs_Click(null, null);

                                    mainWin.SongSearchBox.Text = "";

                                    // SPLIT THE PAYLOAD JUST LIKE THE MASTER LOADER DOES!
                                    string[] parts = rawPayload.Split(new string[] { "\n===TANGLISH===\n" }, StringSplitOptions.None);

                                    // Primary Box (Tamil)
                                    mainWin.LyricsInputBox.Text = parts[0].Replace("\r\n", "\n").Replace("\n", "\r\n");

                                    // Secondary Box (Tanglish) - if it exists!
                                    if (parts.Length > 1)
                                    {
                                        mainWin.SecondaryLyricsBox.Text = parts[1].Replace("\r\n", "\n").Replace("\n", "\r\n");
                                        mainWin.chkDualEditor.IsChecked = true;
                                    }
                                    else
                                    {
                                        mainWin.SecondaryLyricsBox.Text = "";
                                        mainWin.chkDualEditor.IsChecked = false;
                                    }

                                    // Force the PC's UI to physically update to match the new check state!
                                    mainWin.ChkDualEditor_Click(null, null);
                                    break;
                                }
                            }
                        });
                    }
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                .WithAction("/action/clicklyric", HttpVerbs.Get, async ctx =>
                {
                    string indexStr = ctx.Request.QueryString["index"];
                    if (int.TryParse(indexStr, out int index)) Application.Current.Dispatcher.Invoke(() => OnLyricClicked?.Invoke(index));
                    await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
                })
                // 4. Serve the actual Web App HTML to the phone!
                .WithAction("/", HttpVerbs.Get, async ctx => { await ctx.SendStringAsync(GetHtmlInterface(), "text/html", System.Text.Encoding.UTF8); });

            _server.RunAsync();
        }

        public static void Stop()
        {
            if (_server != null) { _server.Dispose(); _server = null; }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) { if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString(); }
            return "127.0.0.1";
        }

        // The True Mobile Web App (HTML/CSS/JS)
        // The True Mobile Web App (HTML/CSS/JS)
        private static string GetHtmlInterface()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1, user-scalable=0'/>
    <style>
        body { background-color: #0F111A; color: white; font-family: 'Segoe UI', sans-serif; margin: 0; padding: 20px; padding-bottom: 80px; display: flex; flex-direction: column; align-items: center; }
        h2 { color: #6C5DD3; margin-top: 0; }
        
        .tab-view { width: 100%; max-width: 400px; display: none; flex-direction: column; align-items: center; }
        .active-tab { display: flex; }
        
        .bottom-nav { position: fixed; bottom: 0; left: 0; width: 100%; height: 60px; background: #1C1E30; display: flex; box-shadow: 0 -5px 15px rgba(0,0,0,0.5); z-index: 100; }
        .nav-btn { flex: 1; background: transparent; color: gray; border: none; font-size: 16px; font-weight: bold; cursor: pointer; transition: 0.2s; }
        .nav-btn.active { color: #00E5FF; border-top: 3px solid #00E5FF; background: rgba(0, 229, 255, 0.1); }

        input { width: 100%; max-width: 400px; padding: 15px; border-radius: 10px; border: none; background: #1C1E30; color: white; font-size: 16px; margin-bottom: 20px; font-weight: bold; box-sizing: border-box; }
        #songList { width: 100%; max-height: 200px; overflow-y: auto; background: #1C1E30; border-radius: 10px; display: none; margin-bottom: 20px; }
        .song-item { padding: 15px; border-bottom: 1px solid #2D3047; cursor: pointer; }
        .song-item:active { background: #6C5DD3; }

        .switch-container { display: flex; align-items: center; justify-content: space-between; width: 100%; background: transparent; padding: 10px 0; box-sizing: border-box; }
        .switch-label { font-size: 16px; font-weight: bold; color: white; }
        .switch { position: relative; display: inline-block; width: 50px; height: 28px; }
        .switch input { opacity: 0; width: 0; height: 0; }
        .slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: #2D3047; transition: .4s; border-radius: 34px; }
        .slider:before { position: absolute; content: ''; height: 20px; width: 20px; left: 4px; bottom: 4px; background-color: white; transition: .4s; border-radius: 50%; }
        input:checked + .slider { background-color: #6C5DD3; }
        input:checked + .slider:before { transform: translateX(22px); }

        #lyricsGrid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; width: 100%; max-width: 400px; margin-bottom: 50px; }
        .lyric-btn { background-color: #2D3047; color: white; border: none; border-radius: 10px; padding: 15px; font-size: 14px; font-weight: bold; box-shadow: 0 4px 6px rgba(0,0,0,0.3); transition: 0.1s; white-space: pre-wrap; word-break: break-word; }
        .lyric-btn:active { background-color: #00B478; transform: scale(0.95); }
        .lyric-btn-pres { grid-column: span 2; font-size: 16px; padding: 20px; background-color: #0085FF; } 
        .active-lyric { background-color: #00B478 !important; }

        .wipe-btn { width: 48%; padding: 15px; border-radius: 10px; font-weight: bold; border: none; color: white; cursor: pointer; transition: 0.1s; box-sizing: border-box; }
        .wipe-btn:active { transform: scale(0.95); }

        #pasteArea { display: none; flex-direction: column; width: 100%; margin-bottom: 20px; }
        textarea { width: 100%; height: 150px; background: #1C1E30; color: white; font-family: 'Segoe UI', sans-serif; font-size: 14px; padding: 15px; border-radius: 10px; border: 1px solid #6C5DD3; resize: none; box-sizing: border-box; margin-bottom: 10px; }
        .toggle-btn { background: transparent; color: #00E5FF; border: none; font-size: 14px; cursor: pointer; margin-bottom: 20px; font-weight: bold; text-decoration: underline; }

        .bible-control-row { display: flex; width: 100%; gap: 10px; margin-bottom: 20px; }
        .bible-btn { flex: 1; background: #2D3047; color: white; border: none; border-radius: 10px; padding: 15px; font-weight: bold; font-size: 16px; }
        .bible-btn:active { background: #00B478; }
        #chapterList { width: 100%; display: flex; flex-direction: column; gap: 10px; }
        .verse-item { background: #1C1E30; padding: 15px; border-radius: 10px; border-left: 4px solid #2D3047; color: white; font-size: 15px; cursor: pointer; }
        .verse-item.active-verse { border-left-color: #6C5DD3; background: #2D3047; }
    </style>
</head>
<body>
    <h2>Rehoboth Remote</h2>
    
    <div style='display: flex; justify-content: space-between; width: 100%; max-width: 400px; margin-bottom: 20px;'>
        <button class='wipe-btn' style='background-color: #00B478;' onclick='fetch(""/action/greenscreen"")'>🟩 Green Screen</button>
        <button class='wipe-btn' style='background-color: #6C5DD3;' onclick='fetch(""/action/theme"")'>✨ Theme Loop</button>
    </div>

    <!-- SONG DASHBOARD TAB -->
    <div id='tabSongs' class='tab-view active-tab'>
        <input type='text' id='searchInput' placeholder='🔍 Search Songs...' onkeyup='filterSongs()'/>
        <div id='songList'></div>

        <button class='toggle-btn' onclick='togglePasteBox()'>+ Paste New Song Lyrics</button>
        
        <div id='pasteArea'>
            <textarea id='lyricsInput' placeholder='Paste Primary (Tamil) lyrics here...'></textarea>
            <textarea id='secondaryLyricsInput' placeholder='Paste Secondary (Tanglish) lyrics here...' style='display: none;'></textarea>
            
            <!-- FIX: One Single Unified Button! -->
            <button class='wipe-btn' style='background-color: #6C5DD3; width: 100%;' onclick='submitPastedSong()'>🚀 SEND &amp; SUBMIT</button>
        </div>

        <div style='width: 100%; background: #1C1E30; border-radius: 10px; margin-bottom: 20px; padding: 5px 15px; box-sizing: border-box;'>
            <div class='switch-container' style='border-bottom: 1px solid #2D3047;'>
                <span class='switch-label'>Presentation Mode</span>
                <label class='switch'><input type='checkbox' id='presToggle' onchange='togglePresMode()'><span class='slider'></span></label>
            </div>
            <div class='switch-container' style='border-bottom: 1px solid #2D3047;'>
                <span class='switch-label' style='color: #00E5FF;'>Multilingual Live</span>
                <label class='switch'><input type='checkbox' id='multiToggle' onchange='toggleLangMode(""multi"")'><span class='slider'></span></label>
            </div>
            <div class='switch-container' style='border-bottom: 1px solid #2D3047;'>
                <span class='switch-label' style='color: #FF9F43;'>I Don't Read Tamil</span>
                <label class='switch'><input type='checkbox' id='opToggle' onchange='toggleLangMode(""op"")'><span class='slider'></span></label>
            </div>
            <div class='switch-container'>
                <span class='switch-label' style='color: #A0A5C0;'>Dual Editor</span>
                <label class='switch'><input type='checkbox' id='dualToggle' onchange='toggleLangMode(""dual"")'><span class='slider'></span></label>
            </div>
        </div>

        <h3 id='currentSongLabel' style='color: #00E5FF; display: none; width: 100%;'></h3>
        <div id='lyricsGrid'></div>
    </div>

    <!-- BIBLE DASHBOARD TAB -->
    <div id='tabBible' class='tab-view'>
        <input type='text' id='bibleSearch' placeholder='🔍 Search Verses (e.g. Gen 1 1)' onkeypress='handleBibleSearch(event)'/>
        
        <div style='width: 100%; background: #1C1E30; border-radius: 10px; margin-bottom: 20px; padding: 5px 15px; box-sizing: border-box;'>
            <div class='switch-container' style='border-bottom: 1px solid #2D3047;'>
                <span class='switch-label'>Tamil Bible</span>
                <label class='switch'><input type='checkbox' id='chkMobTamil' checked onchange='toggleBibleLang(""tamil"")'><span class='slider'></span></label>
            </div>
            <div class='switch-container' style='border-bottom: 1px solid #2D3047;'>
                <span class='switch-label'>English Bible</span>
                <label class='switch'><input type='checkbox' id='chkMobEnglish' onchange='toggleBibleLang(""english"")'><span class='slider'></span></label>
            </div>
            <div class='switch-container' style='border-bottom: 1px solid #2D3047;'>
                <span class='switch-label'>Hindi Bible</span>
                <label class='switch'><input type='checkbox' id='chkMobHindi' onchange='toggleBibleLang(""hindi"")'><span class='slider'></span></label>
            </div>
            <div class='switch-container'>
                <span class='switch-label'>Marathi Bible</span>
                <label class='switch'><input type='checkbox' id='chkMobMarathi' onchange='toggleBibleLang(""marathi"")'><span class='slider'></span></label>
            </div>
        </div>

        <div id='chapterList'></div>
    </div>

    <div class='bottom-nav'>
        <button id='navSongs' class='nav-btn active' onclick='switchTab(""Songs"")'>🎵 SONGS</button>
        <button id='navBible' class='nav-btn' onclick='switchTab(""Bible"")'>📖 BIBLE</button>
    </div>

    <script>
        function switchTab(tab) {
            document.getElementById('tabSongs').classList.remove('active-tab');
            document.getElementById('tabBible').classList.remove('active-tab');
            document.getElementById('navSongs').classList.remove('active');
            document.getElementById('navBible').classList.remove('active');
            document.getElementById('tab' + tab).classList.add('active-tab');
            document.getElementById('nav' + tab).classList.add('active');
        }

        let currentBibleBtn = null;
        async function handleBibleSearch(e) {
            if (e.key === 'Enter') {
                let query = document.getElementById('bibleSearch').value;
                if (!query) return;
                let list = document.getElementById('chapterList');
                list.innerHTML = '<p style=""color:gray;"">Loading chapter...</p>';
                let res = await fetch('/api/biblesearch?q=' + encodeURIComponent(query));
                let verses = await res.json();
                list.innerHTML = '';
                verses.forEach((v, index) => {
                    let div = document.createElement('div');
                    div.className = 'verse-item';
                    div.innerHTML = v;
                    div.onclick = () => {
                        if (currentBibleBtn) currentBibleBtn.classList.remove('active-verse');
                        div.classList.add('active-verse');
                        currentBibleBtn = div;
                        fetch('/action/biblejump?index=' + index);
                    };
                    list.appendChild(div);
                });
            }
        }

        let allSongs = [];
        let currentActiveBtn = null;
        let currentLoadedSong = '';

        window.onload = async () => {
            let res = await fetch('/api/songs');
            allSongs = await res.json();
        };

        function filterSongs() {
            let query = document.getElementById('searchInput').value.toLowerCase();
            let list = document.getElementById('songList');
            list.innerHTML = '';
            if (query.length === 0) { list.style.display = 'none'; return; }
            let matches = allSongs.filter(s => s.toLowerCase().includes(query)).slice(0, 10);
            if (matches.length > 0) {
                list.style.display = 'block';
                matches.forEach(m => {
                    let div = document.createElement('div');
                    div.className = 'song-item';
                    div.innerText = m;
                    div.onclick = () => loadSong(m);
                    list.appendChild(div);
                });
            } else { list.style.display = 'none'; }
        }

        function togglePasteBox() {
            let box = document.getElementById('pasteArea');
            box.style.display = box.style.display === 'flex' ? 'none' : 'flex';
        }

        // FIX: Flawless Single-Button Submit Engine
        async function submitPastedSong() {
            let primaryText = document.getElementById('lyricsInput').value;
            let secondaryText = document.getElementById('secondaryLyricsInput').value;
            let isDual = document.getElementById('dualToggle').checked;
            
            if (primaryText.trim().length === 0) return;
            
            let payload = primaryText;
            if (isDual && secondaryText.trim().length > 0) {
                payload += ""\n===TANGLISH===\n"" + secondaryText;
            }

            // 1. Send text to PC
            await fetch('/action/pastelyrics', { method: 'POST', body: payload });
            
            // 2. Command the PC to process it and return the buttons!
            submitOnPC();
        }

        async function submitOnPC() {
            await fetch('/action/submit');
            currentLoadedSong = ''; 
            document.getElementById('songList').style.display = 'none';
            document.getElementById('currentSongLabel').innerText = 'Unsaved Song (Live)';
            document.getElementById('currentSongLabel').style.display = 'block';
            let grid = document.getElementById('lyricsGrid');
            grid.innerHTML = ''; 
            setTimeout(async () => {
                let res = await fetch('/api/current_lyrics');
                let stanzas = await res.json();
                let isPres = document.getElementById('presToggle').checked;
                buildButtons(stanzas, isPres, grid);
            }, 500);
        }

        async function togglePresMode() {
            let isPres = document.getElementById('presToggle').checked;
            await fetch('/action/togglepres?mode=' + isPres);
            if (currentLoadedSong) loadSong(currentLoadedSong); 
            else submitOnPC();
        }

        async function toggleLangMode(target) {
            let isChecked = document.getElementById(target + 'Toggle').checked;
            if (target === 'dual') {
                document.getElementById('secondaryLyricsInput').style.display = isChecked ? 'block' : 'none';
            }
            await fetch('/action/togglelang?target=' + target + '&mode=' + isChecked);
            if (currentLoadedSong) loadSong(currentLoadedSong); 
            else submitOnPC();
        }

        async function toggleBibleLang(lang) {
            let checkboxId = 'chkMob' + lang.charAt(0).toUpperCase() + lang.slice(1);
            let isChecked = document.getElementById(checkboxId).checked;
            await fetch('/action/biblelang?lang=' + lang + '&state=' + isChecked);
            let searchBox = document.getElementById('bibleSearch');
            if (searchBox.value.trim() !== '') handleBibleSearch({ key: 'Enter' });
        }

        // FIX: Loads the raw lyrics into your phone's text boxes so you can edit them instantly!
        async function loadSong(name) {
            currentLoadedSong = name;
            document.getElementById('searchInput').value = '';
            document.getElementById('songList').style.display = 'none';
            document.getElementById('currentSongLabel').innerText = 'Loading...';
            document.getElementById('currentSongLabel').style.display = 'block';
            let grid = document.getElementById('lyricsGrid');
            grid.innerHTML = ''; 

            try {
                // 1. Tell PC to load it
                await fetch('/action/selectsong?song=' + encodeURIComponent(name));
                
                // 2. Fetch the RAW text to populate the mobile edit boxes!
                let rawRes = await fetch('/api/rawsong?song=' + encodeURIComponent(name));
                let rawData = await rawRes.json();
                
                document.getElementById('lyricsInput').value = rawData.primary;
                document.getElementById('secondaryLyricsInput').value = rawData.secondary;
                
                // Automatically show the second box if the song has Tanglish!
                let dualToggle = document.getElementById('dualToggle');
                if (rawData.secondary.trim().length > 0) {
                    dualToggle.checked = true;
                    document.getElementById('secondaryLyricsInput').style.display = 'block';
                } else {
                    dualToggle.checked = false;
                    document.getElementById('secondaryLyricsInput').style.display = 'none';
                }

                // 3. Download the 42 buttons from the PC!
                setTimeout(async () => {
                    let res = await fetch('/api/current_lyrics');
                    let stanzas = await res.json();
                    document.getElementById('currentSongLabel').innerText = name;
                    let isPres = document.getElementById('presToggle').checked;
                    buildButtons(stanzas, isPres, grid);
                }, 500);
            } catch (err) {
                document.getElementById('currentSongLabel').innerText = 'Error loading song.';
            }
        }

        function buildButtons(stanzas, isPres, grid) {
            stanzas.forEach((stanza, index) => {
                let btn = document.createElement('button');
                btn.className = isPres ? 'lyric-btn lyric-btn-pres' : 'lyric-btn';
                
                let parser = new DOMParser();
                btn.innerText = parser.parseFromString(stanza, 'text/html').body.textContent; 
                
                btn.onclick = () => {
                    if (currentActiveBtn) currentActiveBtn.classList.remove('active-lyric');
                    btn.classList.add('active-lyric');
                    currentActiveBtn = btn;
                    fetch('/action/clicklyric?index=' + index); 
                };
                grid.appendChild(btn);
            });
        }
    </script>
</body>
</html>";
        }
    }
}