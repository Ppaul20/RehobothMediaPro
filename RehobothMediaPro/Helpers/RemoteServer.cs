using EmbedIO;
using EmbedIO.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;

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
                .WithAction("/api/loadsong", HttpVerbs.Get, async ctx =>
                {
                    string songName = ctx.Request.QueryString["song"];
                    List<string> activeLyrics = new List<string>();

                    if (string.IsNullOrEmpty(songName))
                    {
                        activeLyrics.Add("ERROR: Song name was empty.");
                    }
                    else
                    {
                        try
                        {
                            // 1. Tell PC to load it immediately
                            Application.Current.Dispatcher.Invoke(() => OnSongSelected?.Invoke(songName));

                            // 2. Read the file
                            string fullPath = System.IO.Path.Combine(FileHelper.GetAppFolder(), $@"WebTemplate\SongDataBase\{songName}.txt");

                            if (System.IO.File.Exists(fullPath))
                            {
                                string rawText = System.IO.File.ReadAllText(fullPath).Replace("\r\n", "\n").Replace("\r", "\n");
                                string[] stanzas = rawText.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                                // THE MAGIC FIX: We loop through ALL windows to find the specific Rehoboth Window!
                                bool isPresMode = false;
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    foreach (Window window in Application.Current.Windows)
                                    {
                                        if (window is MainWindow mainWin)
                                        {
                                            isPresMode = mainWin.chkPresentation.IsChecked == true;
                                            break;
                                        }
                                    }
                                });

                                foreach (string stanza in stanzas)
                                {
                                    if (isPresMode) activeLyrics.Add(stanza.Trim());
                                    else
                                    {
                                        string[] lines = stanza.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (string line in lines) activeLyrics.Add(line.Trim());
                                    }
                                }
                            }
                            else
                            {
                                activeLyrics.Add($"FILE NOT FOUND");
                            }
                        }
                        catch (Exception ex)
                        {
                            activeLyrics.Add($"CRASH: {ex.Message}");
                        }
                    }

                    // Send the clean JSON payload!
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
                // NEW API: Grabs whatever lyrics are currently loaded on the PC screen (Even unsaved ones!)
                .WithAction("/api/current_lyrics", HttpVerbs.Get, async ctx =>
                {
                    List<string> activeLyrics = new List<string>();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin && mainWin.FinalHtmlModels != null)
                            {
                                foreach (var model in mainWin.FinalHtmlModels)
                                {
                                    activeLyrics.Add(System.Net.WebUtility.HtmlEncode(model.Lyrics.Replace("<br/>", "\n").Replace("</br>", "\n").Replace("<b>", "").Replace("</b>", "").Trim()));
                                }
                                break;
                            }
                        }
                    });
                    string json = JsonSerializer.Serialize(activeLyrics);
                    await ctx.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
                })
                // NEW API: Receives raw lyrics pasted from the phone!
                .WithAction("/action/pastelyrics", HttpVerbs.Post, async ctx =>
                {
                    // Read the payload from the phone
                    using (var reader = new System.IO.StreamReader(ctx.Request.InputStream))
                    {
                        string rawLyrics = await reader.ReadToEndAsync();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window is MainWindow mainWin)
                                {
                                    // Switch the PC to the Song Dashboard if it isn't already!
                                    if (mainWin.SongDashboard.Visibility != Visibility.Visible)
                                        mainWin.BtnNavSongs_Click(null, null);

                                    // Clear the current selection and inject the lyrics into the text box!
                                    mainWin.SongSearchBox.Text = "";
                                    mainWin.LyricsInputBox.Text = rawLyrics.Replace("\r\n", "\n") .Replace("\n", "\r\n"); // Fix line breaks
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
        private static string GetHtmlInterface()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1, user-scalable=0'/>
    <style>
        body { background-color: #0F111A; color: white; font-family: 'Segoe UI', sans-serif; margin: 0; padding: 20px; display: flex; flex-direction: column; align-items: center; }
        h2 { color: #6C5DD3; margin-top: 0; }
        
        input { width: 100%; max-width: 400px; padding: 15px; border-radius: 10px; border: none; background: #1C1E30; color: white; font-size: 16px; margin-bottom: 20px; font-weight: bold; box-sizing: border-box; }
        
        #songList { width: 100%; max-width: 400px; max-height: 200px; overflow-y: auto; background: #1C1E30; border-radius: 10px; display: none; margin-bottom: 20px; }
        .song-item { padding: 15px; border-bottom: 1px solid #2D3047; cursor: pointer; }
        .song-item:active { background: #6C5DD3; }

        .switch-container { display: flex; align-items: center; justify-content: space-between; width: 100%; max-width: 400px; background: #1C1E30; padding: 15px; border-radius: 10px; margin-bottom: 20px; box-sizing: border-box; }
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

        #pasteArea { display: none; flex-direction: column; width: 100%; max-width: 400px; margin-bottom: 20px; }
        textarea { width: 100%; height: 150px; background: #1C1E30; color: white; font-family: 'Segoe UI', sans-serif; font-size: 14px; padding: 15px; border-radius: 10px; border: 1px solid #6C5DD3; resize: none; box-sizing: border-box; margin-bottom: 10px; }
        .toggle-btn { background: transparent; color: #00E5FF; border: none; font-size: 14px; cursor: pointer; margin-bottom: 20px; font-weight: bold; text-decoration: underline; }
    </style>
</head>
<body>
    <h2>Rehoboth Remote Pro</h2>
    
    <div style='display: flex; justify-content: space-between; width: 100%; max-width: 400px; margin-bottom: 20px;'>
        <button class='wipe-btn' style='background-color: #00B478;' onclick='fetch(""/action/greenscreen"")'>🟩 Green Screen</button>
        <button class='wipe-btn' style='background-color: #6C5DD3;' onclick='fetch(""/action/theme"")'>✨ Theme Loop</button>
    </div>

    <input type='text' id='searchInput' placeholder='🔍 Search Songs...' onkeyup='filterSongs()'/>
    <div id='songList'></div>

    <button class='toggle-btn' onclick='togglePasteBox()'>+ Paste New Song Lyrics</button>
    <div id='pasteArea'>
        <textarea id='lyricsInput' placeholder='Paste new lyrics here...'></textarea>
        <!-- FIX: One single button to do it all! -->
        <button class='wipe-btn' style='background-color: #6C5DD3; width: 100%;' onclick='submitPastedSong()'>🚀 SEND &amp; SUBMIT</button>
    </div>

    <div class='switch-container'>
        <span class='switch-label'>Presentation Mode</span>
        <label class='switch'>
            <input type='checkbox' id='presToggle' onchange='toggleMode()'>
            <span class='slider'></span>
        </label>
    </div>

    <h3 id='currentSongLabel' style='color: #00E5FF; display: none;'></h3>
    <div id='lyricsGrid'></div>

    <script>
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

        // FIX: The unified Send & Submit engine!
        async function submitPastedSong() {
            let text = document.getElementById('lyricsInput').value;
            if (text.trim().length === 0) return;

            // 1. Send text to PC
            await fetch('/action/pastelyrics', { method: 'POST', body: text });
            
            // 2. Clear mobile UI
            document.getElementById('lyricsInput').value = '';
            togglePasteBox();
            
            // 3. Tell PC to hit Submit
            await fetch('/action/submit');
            
            currentLoadedSong = ''; 
            document.getElementById('songList').style.display = 'none';
            document.getElementById('currentSongLabel').innerText = 'Unsaved Song (Live)';
            document.getElementById('currentSongLabel').style.display = 'block';
            
            let grid = document.getElementById('lyricsGrid');
            grid.innerHTML = ''; 

            // 4. Wait for PC to build the buttons, then download them!
            setTimeout(async () => {
                let res = await fetch('/api/current_lyrics');
                let stanzas = await res.json();
                let isPres = document.getElementById('presToggle').checked;
                buildButtons(stanzas, isPres, grid);
            }, 500);
        }

        // FIX: Flawlessly rebuilds the phone UI whether the song is saved OR unsaved!
        async function toggleMode() {
            let isPres = document.getElementById('presToggle').checked;
            
            // 1. Tell the PC to flip its checkbox and rebuild its internal 42 buttons
            await fetch('/action/togglepres?mode=' + isPres);
            
            // 2. Fetch the newly built buttons back to the phone!
            if (currentLoadedSong) {
                // It's a saved song from the database
                loadSong(currentLoadedSong); 
            } else {
                // It's an unsaved pasted song. We just fetch the current raw memory array!
                document.getElementById('lyricsGrid').innerHTML = ''; 

                setTimeout(async () => {
                    let res = await fetch('/api/current_lyrics');
                    let stanzas = await res.json();
                    let grid = document.getElementById('lyricsGrid');
                    
                    buildButtons(stanzas, isPres, grid);
                }, 500); // 500ms delay gives the PC time to finish rebuilding the 42 buttons
            }
        }

        async function loadSong(name) {
            currentLoadedSong = name;
            document.getElementById('searchInput').value = '';
            document.getElementById('songList').style.display = 'none';
            document.getElementById('currentSongLabel').innerText = 'Loading...';
            document.getElementById('currentSongLabel').style.display = 'block';
            let grid = document.getElementById('lyricsGrid');
            grid.innerHTML = ''; 

            try {
                let isPres = document.getElementById('presToggle').checked;
                let response = await fetch('/api/loadsong?song=' + encodeURIComponent(name));
                let stanzas = await response.json();
                document.getElementById('currentSongLabel').innerText = name;
                buildButtons(stanzas, isPres, grid);
            } catch (err) {
                document.getElementById('currentSongLabel').innerText = 'Error loading song.';
            }
        }

        // FIX: The Submit button now correctly asks for the newly generated lyrics from the PC!

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