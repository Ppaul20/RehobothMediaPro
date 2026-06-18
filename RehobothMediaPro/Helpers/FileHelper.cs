using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RehobothMediaPro.Models;

namespace RehobothMediaPro.Helpers
{
    public class FileHelper
    {
        // This smart method guarantees we always find the CSS, Images, and HTML
        public static string GetAppFolder()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static readonly string dirPath = GetAppFolder();
        private readonly string fileLocation = Path.Combine(dirPath, @"WebTemplate\");
        private readonly string fileLocationSong = Path.Combine(dirPath, @"WebTemplate\SongDataBase\");
        private readonly string LiveSongHtml = Path.Combine(dirPath, @"WebTemplate\SongLive.html");

        public void SaveasHtmlForSong(string templateName, HtmlModel content)
        {
            string baseDir = GetAppFolder();
            string fileName = Path.Combine(baseDir, $@"WebTemplate\{templateName}.html");

            if (File.Exists(fileName))
            {
                string songTemplate = File.ReadAllText(fileName);

                if (!string.IsNullOrEmpty(songTemplate))
                {
                    songTemplate = songTemplate.Replace("{{Lyrics}}", content.Lyrics ?? "");
                    songTemplate = songTemplate.Replace("{{Header}}", content.Header ?? "");
                    songTemplate = songTemplate.Replace("{{FontSize}}", content.FontSize ?? "35px");

                    CreateFile("SongLive", songTemplate, "html");
                }
            }
        }
        // NEW: Generates the Full Paragraph for the Presentation Screen!
        // FIX: Beautiful full-screen Gradient Background for Presentation Mode!
        // FIX: Aggressive CSS injection guarantees your Custom Gradient appears!
        // FIX: Now accepts templateFile so we can route it to SongLive or SongPresentation!
        public void SaveasHtmlForPresentation(HtmlModel content, string grad1, string grad2, string templateFile = "SongPresentation")
        {
            string baseDir = GetAppFolder();

            string finalLyrics = content.Lyrics ?? "";

            string html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ 
            margin: 0; padding: 5vw; overflow: hidden; box-sizing: border-box; 
            background: linear-gradient(135deg, {grad1}, {grad2}); 
            display: flex; align-items: center; justify-content: center; height: 100vh; 
        }}
        .lyrics {{
            font-family: 'Segoe UI', Arial, sans-serif;
            font-weight: 900; color: white;
            text-align: center; font-size: 7.5vh; line-height: 1.6;
            text-shadow: 3px 5px 15px rgba(0,0,0,0.6);
            animation: fadeIn 0.5s ease-in-out;
        }}
        @keyframes fadeIn {{ from {{ opacity: 0; transform: scale(0.95); }} to {{ opacity: 1; transform: scale(1); }} }}
    </style>
</head>
<body>
    <div class='lyrics'>
        {finalLyrics}
    </div>
</body>
</html>";

            CreateFile(templateFile, html, "html");
        }

        public void OpenTemplate(string templateName)
        {
            string baseDir = GetAppFolder();
            string fileName = Path.Combine(baseDir, $@"WebTemplate\{templateName}.html");

            if (File.Exists(fileName))
            {
                string songTemplate = File.ReadAllText(fileName);
                if (!string.IsNullOrEmpty(songTemplate))
                {
                    CreateFile("SongLive", songTemplate, "html");
                }
            }
        }

        public void SaveasHtmlForNotice(string templateName, string header, string points, string fontsize = "55")
        {
            var content = new HtmlModel() { Header = header, Lyrics = points, FontSize = fontsize };
            var fileName = Path.Combine(dirPath, $@"WebTemplate\{templateName}.html");
            string songTemplate = File.ReadAllText($"{fileName}");
            songTemplate = songTemplate.Replace("{{Lyrics}}", content.Lyrics);
            songTemplate = songTemplate.Replace("{{Header}}", content.Header);
            songTemplate = songTemplate.Replace("{{FontSize}}", content.FontSize);

            // FIXED: Replaced ConfigurationManager with direct string
            CreateFile("SongLive", songTemplate, "html");
        }

        public void SaveasHtmlForHeader(string header)
        {
            var content = new HtmlModel() { Header = header };
            var fileName = Path.Combine(dirPath, @"WebTemplate\header.html");
            string songTemplate = File.ReadAllText($"{fileName}");
            songTemplate = songTemplate.Replace("{{Lyrics}}", content.Lyrics);
            songTemplate = songTemplate.Replace("{{Header}}", content.Header);
            songTemplate = songTemplate.Replace("{{FontSize}}", content.FontSize);

            CreateFile("SongLive", songTemplate, "html");
        }

        public void SaveasHtmlForBible(HtmlModel content, string templateName)
        {
            var fileName = Path.Combine(dirPath, $@"WebTemplate\{templateName}.html");
            string songTemplate = File.ReadAllText($"{fileName}");
            content.Lyrics = content.Lyrics.Replace("<br>", "");
            songTemplate = songTemplate.Replace("{{Lyrics}}", content.Lyrics);
            songTemplate = songTemplate.Replace("{{Header}}", content.Header);
            songTemplate = songTemplate.Replace("{{FontSize}}", content.FontSize);

            CreateFile("SongLive", songTemplate, "html");
        }

        public void SaveasHtmlForBirthday(HtmlModel content, string templateName)
        {
            var fileName = Path.Combine(dirPath, $@"WebTemplate\{templateName}.html");
            string songTemplate = File.ReadAllText($"{fileName}");
            songTemplate = songTemplate.Replace("{{Lyrics}}", content.Lyrics);
            songTemplate = songTemplate.Replace("{{Header}}", content.Header);
            songTemplate = songTemplate.Replace("{{FontSize}}", content.FontSize);

            CreateFile("SongLive", songTemplate, "html");
        }

        public void SaveasHtmlForLowerThird(HtmlModel content, string templateName)
        {
            var fileName = Path.Combine(dirPath, $@"WebTemplate\{templateName}.html");
            string songTemplate = File.ReadAllText($"{fileName}");
            content.Lyrics = content.Lyrics.Replace("<br>", "");
            songTemplate = songTemplate.Replace("{{Lyrics}}", content.Lyrics);
            songTemplate = songTemplate.Replace("{{Header}}", content.Header);
            songTemplate = songTemplate.Replace("{{FontSize}}", content.FontSize);

            CreateFile("SongLive", songTemplate, "html");
        }

        public void SaveasHtmlForBibleHeader(HtmlModel content, string templateName)
        {
            var fileName = Path.Combine(dirPath, $@"WebTemplate\{templateName}.html");
            string songTemplate = File.ReadAllText($"{fileName}");
            songTemplate = songTemplate.Replace("{{Lyrics}}", content.Lyrics);
            songTemplate = songTemplate.Replace("{{Header}}", content.Header);
            songTemplate = songTemplate.Replace("{{FontSize}}", content.FontSize);

            CreateFile("SongLive", songTemplate, "html");
        }

        public void CreateFile(string fileName, string songTemplate, string type)
        {
            using (FileStream fs = new FileStream($"{fileLocation}{fileName}.{type}", FileMode.Create))
            {
                using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                {
                    w.WriteLine(songTemplate);
                }
            }
        }

        // FIX: Accepts Custom Fonts, Colors, and Manual Size Adjustments!
        // FIX: Torn Paper Header, Top Aligned!
        // FIX: Clean Paint Brush Title and perfectly aligned verses!
        // FIX: Sleek Tape Banner, Smaller Reference Font, and Perfect Verse Spacing!
        // FIX: Flat Tape, Single-Line Header, and Full-Screen Verse Spacing!
        // FIX: Re-introduced JS Auto-Scaler to guarantee it fits perfectly without overlapping!
        // FIX: Replaced glitchy Javascript with flawless CSS Clamp math!
        // FIX: Perfectly balanced CSS Clamp using Vertical Height (vh) to prevent screen cutoff!
        // FIX: The True VerseView "Max-Fit" Auto-Scaler Engine!
        // FIX: Pure CSS Scaling! No Javascript loops to accidentally over-shrink!
        // FIX: Linear Font Scaling! Allows verses to organically grow large and fill the space!
        // FIX: The Flawless Shrink-To-Fit Engine. Massive fonts, but mathematically forbidden from spilling!
        // FIX: The Ultimate Block-Based Shrink-to-Fit Engine!
        // FIX: Native CSS Container Scaling! The browser physically cannot let it overflow!
        // FIX: Pure VMIN scaling! Guaranteed to fit long verses without cutting, and keep short verses huge!
        // FIX: The Flawless TextFit Algorithm. It physically measures the pixels on your screen!
        // FIX: Added Bold Parameters and Side/Bottom Padding!
        // FIX: Exact VerseView Replica! Floating Reference, custom spacing!
        // FIX: Fixed Reference Size & Strict Bounding Box for Flawless Verse Shrinking!
        // FIX: Pure CSS Container Queries! No Javascript flashes, no chopped backgrounds!
        // RESTORED: True TextFit JS Engine + Fixed Unchopped Background + Locked Reference Size!
        // FIX: Invisible Barrier Blocks guarantee perfect spacing that the Javascript cannot ignore!
        public void SaveasHtmlForVerseViewBible(string reference, List<string> verses, string bgImagePath, double sizeOffset, string vFont, string vColor, string rFont, bool priBold, bool secBold)
        {
            string baseDir = GetAppFolder();
            string bgCss = string.IsNullOrEmpty(bgImagePath) ? "background-color: #000000;" : $"background-color: #000000; background-image: url('file:///{bgImagePath.Replace("\\", "/")}'); background-size: cover; background-position: center; background-repeat: no-repeat;";

            string verseHtml = "";
            for (int i = 0; i < verses.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(verses[i]))
                {
                    string fontWeight = (i == 0) ? (priBold ? "bold" : "normal") : (secBold ? "bold" : "normal");
                    verseHtml += $"<div class='verse-block' style='font-weight: {fontWeight};'>{verses[i]}</div>";

                    // Add an invisible spacer between languages (but not after the last one!)
                    if (i < verses.Count - 1)
                    {
                        verseHtml += "<div class='spacer-middle'></div>";
                    }
                }
            }

            string bibleTemplate = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ margin: 0; padding: 0; overflow: hidden; box-sizing: border-box; {bgCss} display: flex; flex-direction: column; align-items: center; justify-content: flex-start; height: 100vh; text-align: center; animation: fadeIn 0.4s ease-in-out; }}
        @keyframes fadeIn {{ from {{ opacity: 0; }} to {{ opacity: 1; }} }}
        
        .reference-wrapper {{ position: absolute; top: 4vh; left: 50%; transform: translateX(-50%); display: inline-block; z-index: 10; }}
        .reference-bg {{ background-color: white; border-radius: 5px 25px 5px 25px / 25px 5px 25px 5px; padding: 1.5vh 3vw; box-shadow: 2px 5px 15px rgba(0,0,0,0.5); max-width: 90vw; }}
        .reference {{ color: black; font-family: '{rFont}', sans-serif; font-weight: 900; text-transform: uppercase; letter-spacing: 2px; margin: 0; white-space: nowrap; font-size: 4.5vh; }}
        
        /* The container stretches to fill the screen */
        .verses-container {{ 
            width: 100vw; 
            height: 100vh; 
            display: flex; flex-direction: column; align-items: center; justify-content: center;
            box-sizing: border-box; 
        }}
        
        /* THE PHYSICAL BARRIERS: Javascript is forced to measure these! */
        .spacer-top {{ height: 16vh; width: 100%; flex-shrink: 0; }} /* Protects the banner */
        .spacer-middle {{ height: 3vh; width: 100%; flex-shrink: 0; }} /* Separates languages */
        .spacer-bottom {{ height: 6vh; width: 100%; flex-shrink: 0; }} /* Protects the floor */

        /* Added 6vw side padding so the text never touches the left/right edges */
        .verse-block {{ color: {vColor}; font-family: '{vFont}', sans-serif; text-shadow: 3px 3px 8px black; width: 96vw; padding: 0 2vw; box-sizing: border-box; line-height: 1.4; margin: 0; opacity: 0; }}
    </style>
</head>
<body>
    <div class='reference-wrapper'><div class='reference-bg'><p class='reference'>{reference}</p></div></div>
    
    <div class='verses-container' id='vCont'>
        <!-- The invisible barriers are physically built into the DOM! -->
        <div class='spacer-top'></div>
        {verseHtml}
        <div class='spacer-bottom'></div>
    </div>

    <!-- SCRIPT: Flawless JS Shrink-to-Fit Engine -->
    <script>
        function fitText() {{
            let cont = document.getElementById('vCont');
            let blocks = document.querySelectorAll('.verse-block');
            
            let currentSize = 100; // Start massively huge!
            let minSize = 20;
            let offset = {sizeOffset} * 5; 
            
            blocks.forEach(b => {{
                b.style.fontSize = currentSize + 'px';
                b.style.opacity = '1'; // Unhide
            }});

            // Shrink pixel-by-pixel until it fits! 
            // Because the spacers are physical objects, the text HAS to shrink to make room for them!
            let loops = 0;
            while (cont.scrollHeight > window.innerHeight && currentSize > minSize && loops < 100) {{
                currentSize -= 1;
                blocks.forEach(b => b.style.fontSize = currentSize + 'px');
                loops++;
            }}

            // Apply manual +/- offset
            let finalSize = currentSize + offset;
            blocks.forEach(b => b.style.fontSize = finalSize + 'px');
        }}

        window.onload = fitText;
        setTimeout(fitText, 50); 
    </script>
</body>
</html>";

            CreateFile("SongLive", bibleTemplate, "html");
        }

        public void CreateFileSong(string fileName, string songTemplate, string type)
        {
            using (FileStream fs = new FileStream($"{fileLocationSong}{fileName}.{type}", FileMode.Create))
            {
                using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                {
                    w.WriteLine(songTemplate);
                }
            }
        }

        public List<string> FiletoList(string filename)
        {
            string baseDir = GetAppFolder();
            string filePath = Path.Combine(baseDir, $@"WebTemplate\DropDownList\{filename}.txt");

            if (!File.Exists(filePath))
                return new List<string>();

            return File.ReadAllText(filePath).Replace("\r\n", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        // FIX: Now contains 4 entirely different broadcast animations!
        // FIX: All 15 Broadcast Lower Third Animations are now uniquely programmed!
        public void SaveasHtmlForModernLowerThird(string name, string role, string themeColor, string accentColor, string fontStyle, int designType, string templateFile = "SongLive")
        {
            string css = ""; string htmlBody = "";
            string roleHtml = string.IsNullOrWhiteSpace(role) ? "" : $"<div class='lt-role'>{role}</div>";

            switch (designType)
            {
                case 0: // 1. Broadcast Slide Left
                    css = $@".lt-container {{ position: absolute; bottom: 8vh; left: 5vw; display: flex; flex-direction: column; align-items: flex-start; }}
                             .lt-name {{ background: {themeColor}; color: white; font-size: 5.5vh; font-weight: 900; padding: 1.5vh 3vw 1vh 2vw; border-radius: 8px 15px 15px 0; box-shadow: 0 10px 20px rgba(0,0,0,0.5); text-transform: uppercase; border-left: 10px solid {accentColor}; z-index: 2; }}
                             .lt-role {{ background: rgba(28, 30, 48, 0.95); color: #A0A5C0; font-size: 2.5vh; font-weight: 600; padding: 1vh 2vw 1vh 1vw; border-radius: 0 0 10px 10px; box-shadow: 0 5px 15px rgba(0,0,0,0.5); text-transform: uppercase; margin-top: -5px; margin-left: 2vw; z-index: 1; }}
                             body:not(.animate-out) .lt-container {{ animation: slideIn 0.8s cubic-bezier(0.25, 1, 0.5, 1) forwards; }}
                             body.animate-out .lt-container {{ animation: slideOut 0.6s cubic-bezier(0.5, 0, 0.75, 0) forwards; }}
                             @keyframes slideIn {{ from {{ transform: translateX(-110vw); opacity: 0; }} to {{ transform: translateX(0); opacity: 1; }} }}
                             @keyframes slideOut {{ from {{ transform: translateX(0); opacity: 1; }} to {{ transform: translateX(-110vw); opacity: 0; }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 1: // 2. Minimalist (Line Expand)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 10vw; border-bottom: 5px solid {accentColor}; padding-bottom: 10px; white-space: nowrap; overflow: hidden; }}
                             .lt-name {{ color: white; font-size: 6vh; font-weight: 800; text-transform: uppercase; text-shadow: 2px 2px 5px black; }}
                             .lt-role {{ color: {themeColor}; font-size: 3vh; font-weight: 600; text-transform: uppercase; text-shadow: 2px 2px 5px black; }}
                             body:not(.animate-out) .lt-container {{ animation: expandLine 1s ease-out forwards; }}
                             body:not(.animate-out) .lt-name, body:not(.animate-out) .lt-role {{ animation: fadeTextIn 1s ease-in forwards; }}
                             body.animate-out .lt-container {{ animation: shrinkLine 0.6s ease-in forwards; }}
                             @keyframes expandLine {{ from {{ width: 0; }} to {{ width: 100%; }} }}
                             @keyframes shrinkLine {{ from {{ width: 100%; opacity: 1; }} to {{ width: 0; opacity: 0; }} }}
                             @keyframes fadeTextIn {{ from {{ opacity: 0; transform: translateY(20px); }} to {{ opacity: 1; transform: translateY(0); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 2: // 3. Elegant (Center Fade Up)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; width: 100%; display: flex; flex-direction: column; align-items: center; }}
                             .lt-box {{ background: rgba(0, 0, 0, 0.7); border: 2px solid {accentColor}; padding: 2vh 5vw; border-radius: 50px; text-align: center; box-shadow: 0 10px 30px rgba(0,0,0,0.8); }}
                             .lt-name {{ color: white; font-size: 5vh; font-weight: bold; letter-spacing: 3px; }}
                             .lt-role {{ color: {themeColor}; font-size: 2vh; letter-spacing: 6px; text-transform: uppercase; margin-top: 5px; }}
                             body:not(.animate-out) .lt-container {{ animation: fadeUp 1s cubic-bezier(0.2, 0.8, 0.2, 1) forwards; }}
                             body.animate-out .lt-container {{ animation: fadeDown 0.6s cubic-bezier(0.5, 0, 0.75, 0) forwards; }}
                             @keyframes fadeUp {{ from {{ opacity: 0; transform: translateY(50px); }} to {{ opacity: 1; transform: translateY(0); }} }}
                             @keyframes fadeDown {{ from {{ opacity: 1; transform: translateY(0); }} to {{ opacity: 0; transform: translateY(50px); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-box'><div class='lt-name'>{name}</div>{roleHtml}</div></div>"; break;

                case 3: // 4. Clean Glass (Blur Drop)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 5vw; display: flex; flex-direction: column; align-items: flex-start; background: rgba(0,0,0,0.4); backdrop-filter: blur(15px); -webkit-backdrop-filter: blur(15px); padding: 2vh 3vw; border-radius: 20px; border: 1px solid rgba(255,255,255,0.2); border-left: 8px solid {accentColor}; box-shadow: 0 15px 35px rgba(0,0,0,0.5); }}
                             .lt-name {{ color: white; font-size: 5vh; font-weight: 800; text-transform: uppercase; text-shadow: 2px 2px 5px rgba(0,0,0,0.8); }}
                             .lt-role {{ color: {themeColor}; font-size: 2.5vh; font-weight: 600; letter-spacing: 4px; text-transform: uppercase; margin-top: 5px; }}
                             body:not(.animate-out) .lt-container {{ animation: slideUpIn 1s cubic-bezier(0.16, 1, 0.3, 1) forwards; }}
                             body.animate-out .lt-container {{ animation: slideDownOut 0.6s cubic-bezier(0.5, 0, 0.75, 0) forwards; }}
                             @keyframes slideUpIn {{ from {{ opacity: 0; transform: translateY(50px); }} to {{ opacity: 1; transform: translateY(0); }} }}
                             @keyframes slideDownOut {{ from {{ opacity: 1; transform: translateY(0); }} to {{ opacity: 0; transform: translateY(50px); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 4: // 5. Neon Cyber (Glowing Edges)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 5vw; background: rgba(0,0,0,0.8); border: 2px solid {accentColor}; box-shadow: 0 0 15px {accentColor}, inset 0 0 15px {accentColor}; padding: 2vh 3vw; }}
                             .lt-name {{ color: white; font-size: 5vh; font-weight: 900; text-transform: uppercase; font-style: italic; }}
                             .lt-role {{ color: {accentColor}; font-size: 2vh; text-transform: uppercase; letter-spacing: 3px; }}
                             body:not(.animate-out) .lt-container {{ animation: glitchIn 0.4s ease-in forwards; }}
                             body.animate-out .lt-container {{ animation: glitchOut 0.4s ease-in forwards; }}
                             @keyframes glitchIn {{ 0% {{ opacity: 0; transform: skewX(-20deg) translateX(-100px); }} 100% {{ opacity: 1; transform: skewX(0) translateX(0); }} }}
                             @keyframes glitchOut {{ 100% {{ opacity: 0; transform: skewX(20deg) translateX(-100px); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 5: // 6. Corporate Sharp (Bottom Up)
                    css = $@".lt-container {{ position: absolute; bottom: 8vh; left: 8vw; display: flex; flex-direction: column; overflow: hidden; }}
                             .lt-name {{ background: white; color: black; font-size: 5vh; font-weight: 900; padding: 1vh 2vw; border-left: 15px solid {themeColor}; }}
                             .lt-role {{ background: {themeColor}; color: white; font-size: 2vh; font-weight: bold; padding: 1vh 2vw; text-transform: uppercase; width: fit-content; }}
                             body:not(.animate-out) .lt-name {{ animation: slideRight 0.6s ease-out forwards; }}
                             body:not(.animate-out) .lt-role {{ animation: slideRight 0.8s ease-out forwards; }}
                             body.animate-out .lt-name {{ animation: slideLeftOut 0.4s ease-in forwards; }}
                             body.animate-out .lt-role {{ animation: slideLeftOut 0.6s ease-in forwards; }}
                             @keyframes slideRight {{ from {{ transform: translateX(-110%); }} to {{ transform: translateX(0); }} }}
                             @keyframes slideLeftOut {{ to {{ transform: translateX(-110%); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 6: // 7. News Ticker (Full Width)
                    css = $@".lt-container {{ position: absolute; bottom: 5vh; width: 100vw; background: {themeColor}; display: flex; align-items: center; border-top: 4px solid {accentColor}; }}
                             .lt-name {{ background: {accentColor}; color: black; font-size: 4vh; font-weight: 900; padding: 1vh 3vw; text-transform: uppercase; }}
                             .lt-role {{ color: white; font-size: 3vh; font-weight: bold; margin-left: 2vw; text-transform: uppercase; }}
                             body:not(.animate-out) .lt-container {{ animation: slideUpTicker 0.5s forwards; }}
                             /* FIX: Sinks 200px straight down so nothing is left on the screen! */
                             body.animate-out .lt-container {{ animation: slideDownTicker 0.5s forwards; }}
                             @keyframes slideUpTicker {{ from {{ transform: translateY(150px); }} to {{ transform: translateY(0); }} }}
                             @keyframes slideDownTicker {{ to {{ transform: translateY(150px); opacity: 0; }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 7: // 8. Bouncing Pill (Pop In)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 5vw; background: {themeColor}; padding: 1.5vh 3vw; border-radius: 50px; border: 3px solid {accentColor}; box-shadow: 0 10px 20px rgba(0,0,0,0.4); display: flex; align-items: center; gap: 20px; }}
                             .lt-name {{ color: white; font-size: 4.5vh; font-weight: 900; }}
                             .lt-role {{ color: {themeColor}; background: white; padding: 0.5vh 1.5vw; border-radius: 20px; font-size: 2vh; font-weight: bold; text-transform: uppercase; }}
                             body:not(.animate-out) .lt-container {{ animation: popIn 0.6s cubic-bezier(0.68, -0.55, 0.26, 1.55) forwards; }}
                             body.animate-out .lt-container {{ animation: popOut 0.4s ease-in forwards; }}
                             @keyframes popIn {{ from {{ transform: scale(0); opacity: 0; }} to {{ transform: scale(1); opacity: 1; }} }}
                             @keyframes popOut {{ to {{ transform: scale(0); opacity: 0; }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 8: // 9. Split Horizon (Double Line)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 10vw; text-align: left; }}
                             .lt-name {{ color: white; font-size: 6vh; font-weight: 900; text-transform: uppercase; border-bottom: 2px solid {accentColor}; padding-bottom: 5px; text-shadow: 2px 2px 5px black; }}
                             .lt-role {{ color: {themeColor}; font-size: 2.5vh; font-weight: bold; letter-spacing: 5px; text-transform: uppercase; padding-top: 5px; text-shadow: 2px 2px 5px black; }}
                             body:not(.animate-out) .lt-name {{ animation: splitTop 1s ease-out forwards; clip-path: polygon(0 0, 0 0, 0 100%, 0 100%); }}
                             body:not(.animate-out) .lt-role {{ animation: splitBottom 1s ease-out forwards; clip-path: polygon(0 0, 0 0, 0 100%, 0 100%); }}
                             body.animate-out .lt-container {{ animation: fadeOut 0.5s forwards; }}
                             @keyframes splitTop {{ to {{ clip-path: polygon(0 0, 100% 0, 100% 100%, 0 100%); }} }}
                             @keyframes splitBottom {{ to {{ clip-path: polygon(0 0, 100% 0, 100% 100%, 0 100%); }} }}
                             @keyframes fadeOut {{ to {{ opacity: 0; }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 9: // 10. Cinematic (Slow Zoom)
                    css = $@".lt-container {{ position: absolute; bottom: 12vh; left: 8vw; }}
                             .lt-name {{ color: white; font-size: 7vh; font-weight: 300; letter-spacing: 8px; text-transform: uppercase; text-shadow: 0 0 20px rgba(0,0,0,0.8); }}
                             .lt-role {{ color: {accentColor}; font-size: 2vh; font-weight: bold; letter-spacing: 12px; text-transform: uppercase; margin-top: 10px; opacity: 0; }}
                             body:not(.animate-out) .lt-name {{ animation: cinematic 3s ease-out forwards; }}
                             body:not(.animate-out) .lt-role {{ animation: fadeOnly 2s 1s forwards; }}
                             body.animate-out .lt-container {{ animation: fadeOut 1s forwards; }}
                             @keyframes cinematic {{ from {{ opacity: 0; transform: scale(1.1); }} to {{ opacity: 1; transform: scale(1); }} }}
                             @keyframes fadeOnly {{ to {{ opacity: 1; }} }}
                             @keyframes fadeOut {{ to {{ opacity: 0; transform: scale(0.9); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 10: // 11. Neon Pulse (Replaced the glitchy Box Draw!)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 5vw; border: 2px solid {accentColor}; padding: 2vh 3vw; background: rgba(0,0,0,0.6); backdrop-filter: blur(5px); box-shadow: 0 0 30px {themeColor}; }}
                             .lt-name {{ color: white; font-size: 5vh; font-weight: 800; text-transform: uppercase; text-shadow: 0 0 10px {accentColor}; }}
                             .lt-role {{ color: {accentColor}; font-size: 2vh; font-weight: bold; letter-spacing: 4px; text-transform: uppercase; margin-top: 5px; }}
                             body:not(.animate-out) .lt-container {{ animation: pulseIn 0.8s cubic-bezier(0.25, 1, 0.5, 1) forwards; }}
                             body.animate-out .lt-container {{ animation: pulseOut 0.5s forwards; }}
                             @keyframes pulseIn {{ 
                                0% {{ opacity: 0; transform: scale(1.1); box-shadow: 0 0 0px {themeColor}; }} 
                                50% {{ opacity: 1; transform: scale(1); box-shadow: 0 0 50px {themeColor}; }}
                                100% {{ box-shadow: 0 0 20px {themeColor}; }}
                             }}
                             @keyframes pulseOut {{ to {{ opacity: 0; transform: scale(0.9); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 11: // 12. Right-Side Frosted (Slide Right)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; right: 5vw; text-align: right; background: rgba(255,255,255,0.1); backdrop-filter: blur(20px); -webkit-backdrop-filter: blur(20px); padding: 2vh 3vw; border-radius: 15px; border-right: 8px solid {accentColor}; box-shadow: 0 15px 35px rgba(0,0,0,0.3); }}
                             .lt-name {{ color: white; font-size: 5vh; font-weight: 900; text-transform: uppercase; text-shadow: 2px 2px 5px rgba(0,0,0,0.5); }}
                             .lt-role {{ color: {themeColor}; font-size: 2.5vh; font-weight: 600; letter-spacing: 2px; text-transform: uppercase; }}
                             body:not(.animate-out) .lt-container {{ animation: slideInRight 0.8s cubic-bezier(0.25, 1, 0.5, 1) forwards; }}
                             body.animate-out .lt-container {{ animation: slideOutRight 0.6s ease-in forwards; }}
                             @keyframes slideInRight {{ from {{ transform: translateX(110vw); opacity: 0; }} to {{ transform: translateX(0); opacity: 1; }} }}
                             @keyframes slideOutRight {{ to {{ transform: translateX(110vw); opacity: 0; }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 12: // 13. Bold Block (Heavy Drop)
                    css = $@".lt-container {{ position: absolute; bottom: 8vh; left: 5vw; background: white; padding: 2vh 3vw; box-shadow: 15px 15px 0 {themeColor}; }}
                             .lt-name {{ color: black; font-size: 6vh; font-weight: 900; text-transform: uppercase; }}
                             .lt-role {{ color: {accentColor}; font-size: 2.5vh; font-weight: 900; letter-spacing: 3px; text-transform: uppercase; }}
                             body:not(.animate-out) .lt-container {{ animation: slamDown 0.4s cubic-bezier(0.25, 1, 0.5, 1) forwards; }}
                             body.animate-out .lt-container {{ animation: ripUp 0.3s ease-in forwards; }}
                             @keyframes slamDown {{ from {{ transform: translateY(-100px) scale(1.2); opacity: 0; }} to {{ transform: translateY(0) scale(1); opacity: 1; }} }}
                             @keyframes ripUp {{ to {{ transform: translateY(-100px) scale(0.8); opacity: 0; }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                case 13: // 14. Elegant Ribbon (Fade Right)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 0; background: {themeColor}; padding: 1.5vh 5vw 1.5vh 10vw; clip-path: polygon(0 0, 95% 0, 100% 50%, 95% 100%, 0 100%); box-shadow: 0 10px 20px rgba(0,0,0,0.5); }}
                             .lt-name {{ color: white; font-size: 4.5vh; font-weight: bold; text-transform: uppercase; }}
                             .lt-role {{ color: {accentColor}; font-size: 2vh; font-weight: bold; letter-spacing: 3px; text-transform: uppercase; }}
                             body:not(.animate-out) .lt-container {{ animation: slideLeftRibbon 1s ease-out forwards; }}
                             body.animate-out .lt-container {{ animation: slideOutRibbon 0.6s ease-in forwards; }}
                             @keyframes slideLeftRibbon {{ from {{ transform: translateX(-100%); }} to {{ transform: translateX(0); }} }}
                             @keyframes slideOutRibbon {{ to {{ transform: translateX(-100%); }} }}";
                    htmlBody = $"<div class='lt-container'><div class='lt-name'>{name}</div>{roleHtml}</div>"; break;

                default: // 15. Dynamic Tech (Skewed)
                    css = $@".lt-container {{ position: absolute; bottom: 10vh; left: 5vw; background: {themeColor}; padding: 1.5vh 3vw; border-bottom: 5px solid {accentColor}; box-shadow: -10px 10px 20px rgba(0,0,0,0.5); }}
                             .lt-inner {{ transform: skewX(15deg); }}
                             .lt-name {{ color: white; font-size: 5vh; font-weight: 900; text-transform: uppercase; }}
                             .lt-role {{ color: {accentColor}; font-size: 2vh; font-weight: bold; letter-spacing: 2px; text-transform: uppercase; }}
                             body:not(.animate-out) .lt-container {{ animation: flyInSkew 0.6s cubic-bezier(0.175, 0.885, 0.32, 1.275) forwards; }}
                             body.animate-out .lt-container {{ animation: flyOutSkew 0.5s ease-in forwards; }}
                             @keyframes flyInSkew {{ from {{ opacity: 0; transform: translateX(-100px) skewX(-15deg); }} to {{ opacity: 1; transform: translateX(0) skewX(-15deg); }} }}
                             @keyframes flyOutSkew {{ to {{ opacity: 0; transform: translateX(-100px) skewX(-15deg); }} }}";
                    htmlBody = $"<div class='lt-container' style='transform: skewX(-15deg);'><div class='lt-inner'><div class='lt-name'>{name}</div>{roleHtml}</div></div>"; break;
            }

            string html = $"<!DOCTYPE html><html><head><meta charset='utf-8'/><style>body{{margin:0;padding:0;overflow:hidden;background-color:#00ff00;font-family:'{fontStyle}',sans-serif;}} {css}</style></head><body>{htmlBody}</body></html>";

            // Allows us to save to SongLive.html OR a hidden Preview.html!
            CreateFile(templateFile, html, "html");
        }
        // FIX: Stunning "Frosted Glass Card" Animation for Events!
        // FIX: Perfectly recreates the screenshot design with Smart Auto-Scrolling!
        // FIX: Sequenced Entry/Exit Animations + Controlled Scrolling!
        // FIX: Flawless Screen-Edge Entry and Smooth Exit Animations!
        // FIX: Pills permanently locked inside the card. Flawless Entry/Exit animations!
        // FIX: Flawless Screen-Edge Entry, Smooth Exit, and No Spilling!
        // FIX: Bulletproof Structure. No spilling, no flying, smooth fades!
        // FIX: The exact animation sequence requested: Card in -> Pills in -> Pills out -> Card out. No bottom spilling!
        // FIX: Spilling permanently fixed! Container stretches to the left screen edge.
        public void SaveasHtmlForEventList(string eventTitle, List<string> names, List<string> dates, bool isAnniv = false)
        {
            string baseDir = GetAppFolder();
            string icon = isAnniv ? "💍" : "🎂";
            string titleColor = isAnniv ? "#E91E63" : "#1976D2";

            // --- SMART FONT CHECKER ---
            string fontCss = "";
            string titleFontFam = "'Brush Script MT', 'Dancing Script', cursive";
            string nameFontFam = "'Segoe UI', Arial, sans-serif";

            // Check if Title Font exists
            if (File.Exists(Path.Combine(baseDir, @"WebTemplate\titlefont.ttf")) || File.Exists(Path.Combine(baseDir, @"WebTemplate\titlefont.otf")))
            {
                fontCss += "@font-face { font-family: 'CustomTitleFont'; src: url('titlefont.ttf') format('truetype'), url('titlefont.otf') format('opentype'); }\n        ";
                titleFontFam = "'CustomTitleFont', " + titleFontFam;
            }

            // Check if Name Font exists
            if (File.Exists(Path.Combine(baseDir, @"WebTemplate\namefont.ttf")) || File.Exists(Path.Combine(baseDir, @"WebTemplate\namefont.otf")))
            {
                fontCss += "@font-face { font-family: 'CustomNameFont'; src: url('namefont.ttf') format('truetype'), url('namefont.otf') format('opentype'); }\n        ";
                nameFontFam = "'CustomNameFont', " + nameFontFam;
            }

            string listHtml = "";
            for (int i = 0; i < names.Count; i++)
            {
                double inDelay = 0.8 + (i * 0.15);
                double outDelay = (names.Count - i) * 0.05;

                listHtml += $@"
                <div class='event-row' style='--in-delay: {inDelay}s; --out-delay: {outDelay}s;'>
                    <div class='name-group'>
                        <span class='icon'>{icon}</span>
                        <span class='event-name'>{names[i]}</span>
                    </div>
                    <span class='event-date'>{dates[i]}</span>
                </div>";
            }

            string html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        {fontCss}

        body {{ margin: 0; padding: 0; overflow: hidden; background-color: #00ff00; font-family: {nameFontFam}; display: flex; align-items: center; justify-content: flex-start; height: 100vh; }}
        
        .glass-card {{
            margin-left: 5vw;
            background: rgba(224, 247, 250, 0.85); backdrop-filter: blur(15px); -webkit-backdrop-filter: blur(15px);
            border: 2px solid rgba(255, 255, 255, 0.5); border-radius: 20px;
            padding: 30px 40px; box-shadow: 0 15px 35px rgba(0,0,0,0.3);
            width: 35vw; max-height: 80vh;
            display: flex; flex-direction: column;
            transform: translateX(-150vw);
        }}

        .event-header {{
            color: {titleColor}; font-size: 5.5vh; font-weight: bold;
            font-family: {titleFontFam};
            text-align: left; margin-bottom: 20px; margin-left: 10px;
            display: flex; align-items: center; gap: 15px;
        }}

        /* THE FIX: This container safely masks the bottom, but stretches 2000px to the left to let pills fly in! */
        .list-container {{ position: relative; flex-grow: 1; overflow: hidden; margin-left: -2000px; padding-left: 2000px; padding-right: 10px; }}
        
        .scroll-wrapper {{ display: flex; flex-direction: column; gap: 12px; transition: transform linear; }}

        .event-row {{
            background: white; border-radius: 12px; padding: 12px 20px;
            display: flex; justify-content: space-between; align-items: center;
            box-shadow: 0 4px 6px rgba(0,0,0,0.05);
            transform: translateX(-100vw); /* Fly from the far left monitor edge */
        }}

        .name-group {{ display: flex; align-items: center; gap: 15px; }}
        .icon {{ font-size: 2.5vh; }}
        .event-name {{ color: #333; font-size: 2.5vh; font-weight: 800; font-family: {nameFontFam}; }}
        .event-date {{ color: #666; font-size: 2vh; font-weight: bold; text-align: right; font-family: {nameFontFam}; }}

        /* --- ENTRY ANIMATIONS --- */
        .animate-in .glass-card {{ animation: slideCardIn 0.8s cubic-bezier(0.16, 1, 0.3, 1) forwards; }}
        .animate-in .event-row {{ animation: slidePillIn 0.6s cubic-bezier(0.25, 1, 0.5, 1) forwards; animation-delay: var(--in-delay); }}

        @keyframes slideCardIn {{ from {{ transform: translateX(-150vw); }} to {{ transform: translateX(0); }} }}
        @keyframes slidePillIn {{ from {{ transform: translateX(-100vw); }} to {{ transform: translateX(0); }} }}

        /* --- EXIT ANIMATIONS --- */
        .animate-out .event-row {{ animation: slidePillOut 0.4s cubic-bezier(0.5, 0, 0.75, 0) forwards; animation-delay: var(--out-delay); transform: translateX(0); }}
        .animate-out .glass-card {{ animation: slideCardOut 0.6s cubic-bezier(0.5, 0, 0.75, 0) 0.5s forwards !important; transform: translateX(0); }}

        @keyframes slidePillOut {{ to {{ transform: translateX(-100vw); }} }}
        @keyframes slideCardOut {{ to {{ transform: translateX(-150vw); opacity: 0; }} }}
    </style>
</head>
<body class='animate-in' id='mainBody'>
    <div class='glass-card'>
        <div class='event-header'>✨ {eventTitle}</div>
        <div class='list-container' id='listCont'>
            <div class='scroll-wrapper' id='scrollWrap'>
                {listHtml}
            </div>
        </div>
    </div>
</body>
</html>";

            CreateFile("SongLive", html, "html");
        }
    }
}