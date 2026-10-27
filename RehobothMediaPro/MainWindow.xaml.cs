using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ExcelDataReader;
using RehobothMediaPro.Helpers;
using RehobothMediaPro.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using QRCoder;

namespace RehobothMediaPro
{
    public partial class MainWindow : Window
    {
        // THE 42 RULE VARIABLES
        public List<Button> SongLyricsbuttons;
        public int songCounter = 0;
        public int currentlyLiveSongCounter = -1; // FIX: Tracks exactly which box is on the screen!
        private readonly string memGreen = System.IO.Path.Combine(RehobothMediaPro.Helpers.FileHelper.GetAppFolder(), "font_green.txt");
        private readonly string memPres = System.IO.Path.Combine(RehobothMediaPro.Helpers.FileHelper.GetAppFolder(), "font_presentation.txt");
        // THE ENGINE VARIABLES
        public ChromeHelper chromeHelper = new ChromeHelper();
        public FileHelper fileHelper = new FileHelper();
        public List<HtmlModel> FinalHtmlModels;
        public string templateName = "song";
        private string currentlyActiveScreen = ""; // Tracks if we are on Song, Bible, or Theme!
        public string autoFilledTitle = "";

        private List<string> songDatabaseMemory = new List<string>();
        private bool isSongLoading = false; // Prevents recursive loops

        public MainWindow()
        {
            InitializeComponent();
            // FIX: Ensure the font file is physically extracted for the HTML engines to use!
            try
            {
                string targetFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate");
                if (!System.IO.Directory.Exists(targetFolder)) System.IO.Directory.CreateDirectory(targetFolder);

                string fontPath = System.IO.Path.Combine(targetFolder, "BalooThambi.ttf");

                // If it's not already in the WebTemplate folder, extract it from the .exe!
                if (!System.IO.File.Exists(fontPath))
                {
                    // Note: "RehobothMediaPro" is the namespace. Match the exact filename!
                    using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("RehobothMediaPro.BalooThambi.ttf"))
                    {
                        if (stream != null)
                        {
                            using (var fileStream = System.IO.File.Create(fontPath))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }
            }
            catch { }
            InitializationOfbuttons();
            // FIX: Load the raw list once, and enable the internal filtering engine!
            var songData = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
            songDatabaseMemory = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
            // FIX: Load the correct memory file when the app starts! (Defaults to Green Screen)
            if (System.IO.File.Exists(memGreen)) FontSizeBox.Text = System.IO.File.ReadAllText(memGreen);
            else FontSizeBox.Text = "45"; // Default if no memory file exists

            // FIX: Load the correct memory file for the secondary font when the app starts! (Defaults to Green Screen)
            if (System.IO.File.Exists(memSecGreen)) SecFontSizeBox.Text = System.IO.File.ReadAllText(memSecGreen);
            else SecFontSizeBox.Text = "35"; // Default if no memory file exist
            LoadBibleDropdowns();
            LoadBibleBackgrounds(); // FIX: Now the gallery instantly loads when the app opens!
            LoadLowerThirds(); // Populates Pastor Names and MP4 Lists!
            InitMiniMonitor();
            LoadDatePickerLists();
            LoadBibleLanguageList(); // NEW: Populates the drag-and-drop language list!


            string animFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), "anim_settings.txt");
            if (System.IO.File.Exists(animFile))
            {
                string[] parts = System.IO.File.ReadAllText(animFile).Split('|');
                if (parts.Length >= 2) { chkAnimBible.IsChecked = parts[0] == "1"; chkAnimSong.IsChecked = parts[1] == "1"; }
                if (parts.Length >= 4) { BibleAnimSpeed.Value = double.Parse(parts[2]); SongAnimSpeed.Value = double.Parse(parts[3]); }
            }

            // Load Gradient Memory into the Buttons!
            string gradFile = System.IO.Path.Combine(RehobothMediaPro.Helpers.FileHelper.GetAppFolder(), "gradient_memory.txt");
            if (System.IO.File.Exists(gradFile))
            {
                string[] parts = System.IO.File.ReadAllText(gradFile).Split('|');
                if (parts.Length == 2)
                {
                    // FIX: Explicitly forces System.Windows.Media.Brush to avoid compiler confusion!
                    BtnGrad1.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(parts[0]);
                    BtnGrad1.Tag = parts[0];

                    BtnGrad2.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(parts[1]);
                    BtnGrad2.Tag = parts[1];
                }
            }
            string bgFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), "bible_bg_memory.txt");
            if (System.IO.File.Exists(bgFile)) customBgPath = System.IO.File.ReadAllText(bgFile);

            // NEW: Listen for commands coming from the Mobile Phone!
            Helpers.RemoteServer.OnSongSelected += RemoteServer_OnSongSelected;
            Helpers.RemoteServer.OnLyricClicked += RemoteServer_OnLyricClicked;
        }

        private DateTime lastShiftPress = DateTime.MinValue;

        // Generate the 42 Buttons with perfect rounded corners!
        private void InitializationOfbuttons()
        {
            SongLyricsbuttons = new List<Button>();
            LyricsButtonGrid.Children.Clear(); // Empties the visual grid

            for (int i = 0; i < 40; i++)
            {
                Button btn = new Button();
                btn.Margin = new Thickness(6); // Perfect spacing
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2D3047");
                btn.Foreground = Brushes.White;
                btn.FontSize = 13;
                btn.FontWeight = FontWeights.Bold;
                btn.Visibility = Visibility.Collapsed; // Hidden until you hit Submit
                btn.Tag = i;
                btn.Cursor = Cursors.Hand;
                btn.Click += LyricButton_Click;

                // MAGIC: This injects a perfectly rounded border into the button!
                ControlTemplate template = new ControlTemplate(typeof(Button));
                FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8)); // Smooth Corners!

                FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
                content.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
                content.SetValue(ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
                content.SetValue(ContentPresenter.MarginProperty, new Thickness(5));

                border.AppendChild(content);
                template.VisualTree = border;
                btn.Template = template;

                SongLyricsbuttons.Add(btn);
                LyricsButtonGrid.Children.Add(btn); // Puts them in the UI
            }
        }

        private void LyricButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = sender as Button;
            songCounter = (int)clickedBtn.Tag;
            ChoosLyrics();
        }

        // THE 42 RULE: Submit Button Logic
        // THE 42 RULE: Submit Button Logic (Crash-Proof Edition)
        public void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LyricsInputBox.Text)) return;
            if (SongLyricsbuttons == null || SongLyricsbuttons.Count == 0) InitializationOfbuttons();

            FinalHtmlModels = new List<HtmlModel>();

            foreach (var btn in SongLyricsbuttons) btn.Visibility = Visibility.Hidden;

            bool isPresMode = chkPresentation.IsChecked == true;
            bool isOpMode = chkOperatorMode.IsChecked == true;
            bool isMultiMode = chkMultilingual.IsChecked == true;
            string fontSizePx = FontSizeBox.Text + "px";
            templateName = isPresMode ? "song3" : "song";

            // Split Primary (Tamil) and Secondary (Tanglish)
            string[] priStanzas = LyricsInputBox.Text.Replace("\r\n", "\n").Replace("\r", "\n").Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] secStanzas = SecondaryLyricsBox.Text.Replace("\r\n", "\n").Replace("\r", "\n").Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            int buttonIndex = 0;
            bool isBlue = true;

            for (int i = 0; i < priStanzas.Length; i++)
            {
                if (buttonIndex >= SongLyricsbuttons.Count) break;

                System.Windows.Media.Brush baseColor = isPresMode ?
                    (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#6C5DD3") :
                    (isBlue ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#0085FF") : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2D3047"));

                string priStanza = priStanzas[i].Trim();
                string secStanza = i < secStanzas.Length ? secStanzas[i].Trim() : "";

                if (isPresMode)
                {
                    // PRESENTATION HTML BUILDER
                    string btnText = (isOpMode && !string.IsNullOrWhiteSpace(secStanza)) ? secStanza : priStanza;
                    string presHtml = priStanza.Replace("\n", "<br/>");

                    if (isMultiMode && !string.IsNullOrWhiteSpace(secStanza))
                        presHtml += $"<br/><span style='color:FFD700; font-size:0.5em;'>★</span><br/><span style='color:E5FF00; font-size:0.9em;'>{secStanza.Replace("\n", "<br/>")}</span>";

                    SetupLyricButton(buttonIndex, btnText, baseColor);
                    FinalHtmlModels.Add(new HtmlModel() { Lyrics = presHtml, PresentationLyrics = presHtml, FontSize = fontSizePx });
                    buttonIndex++;
                }
                else
                {
                    // GREEN SCREEN HTML BUILDER
                    string[] priLines = priStanza.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] secLines = secStanza.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    string presHtml = priStanza.Replace("\n", "<br/>");
                    if (isMultiMode && !string.IsNullOrWhiteSpace(secStanza))
                        presHtml += $"<br/><span style='color:FFD700; font-size:0.5em;'>★</span><br/><span style='color:E5FF00; font-size:0.9em;'>{secStanza.Replace("\n", "<br/>")}</span>";

                    for (int l = 0; l < priLines.Length; l++)
                    {
                        if (buttonIndex >= SongLyricsbuttons.Count) break;
                        string priLine = priLines[l].Trim();
                        string secLine = l < secLines.Length ? secLines[l].Trim() : "";

                        string btnText = (isOpMode && !string.IsNullOrWhiteSpace(secLine)) ? secLine : priLine;
                        string gsHtml = priLine;
                        if (isMultiMode && !string.IsNullOrWhiteSpace(secLine))
                            gsHtml += $"<br/><span style='color:white; font-size:0.9em;'>{secLine}</span>";

                        SetupLyricButton(buttonIndex, btnText, baseColor);
                        FinalHtmlModels.Add(new HtmlModel() { Lyrics = gsHtml, PresentationLyrics = presHtml, FontSize = fontSizePx });
                        buttonIndex++;
                    }
                }
                isBlue = !isBlue;
            }
        }
        public void ChkDualEditor_Click(object sender, RoutedEventArgs e)
        {
            if (chkDualEditor.IsChecked == true)
            {
                SecondaryEditorBorder.Visibility = Visibility.Visible;
                SecEditorRowDef.Height = new GridLength(1, GridUnitType.Star); // Splits 50/50
            }
            else
            {
                SecondaryEditorBorder.Visibility = Visibility.Collapsed;
                SecEditorRowDef.Height = new GridLength(0); // Hides it
            }
        }
        private void ChkMode_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrEmpty(LyricsInputBox.Text)) BtnSubmit_Click(null, null); }
        private void SetupLyricButton(int index, string text, Brush bgColor)
        {
            Button btn = SongLyricsbuttons[index];
            btn.Content = new TextBlock()
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4),
                FontSize = 13,
                // FIX: Explicitly target the WPF FontFamily!
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };
            btn.Background = bgColor;
            btn.Uid = new BrushConverter().ConvertToString(bgColor); // FIX: Stores color safely here!
            // Do NOT touch btn.Tag here, it holds your number!
            btn.Visibility = Visibility.Visible;
        }
        // ----------------------------------------------------------------
        // THE 42 RULE: CORE ENGINE LOGIC
        // ----------------------------------------------------------------

        // --- NEW: SUNDAY SETLIST QUEUE ---
        // --- NEW: SUNDAY SETLIST QUEUE ---
        private void BtnAddToSetlist_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SongSearchBox.Text)) return;

            string songName = SongSearchBox.Text.Trim();

            // Prevents adding the same song twice!
            if (!SetlistBox.Items.Contains(songName))
            {
                SetlistBox.Items.Add(songName);
                LblSetlistEmpty.Visibility = Visibility.Collapsed;
            }
        }

        private void SetlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SetlistBox.SelectedItem != null)
            {
                // Send the queued song directly to the Master Loader!
                LoadSongIntoEditor(SetlistBox.SelectedItem.ToString());
                SetlistBox.SelectedIndex = -1; // Clear selection
            }
        }

        public async void BtnThemeLogo_Click(object sender, RoutedEventArgs e)
        {
            // 1. Fade out smoothly
            chromeHelper.ExecuteJavascriptOnLive("document.body.style.transition = 'opacity 0.5s ease'; document.body.style.opacity = '0';");
            await System.Threading.Tasks.Task.Delay(500);

            // 2. Build the Theme
            CreateDefaultThemeHtml();

            // 3. Forcefully push the browser back to the Live screen!
            chromeHelper.NavigateLiveDisplay("SongLive");
            if (chromeHelper.IsPresentationOpen()) chromeHelper.NavigatePresentationDisplay("SongLive");

            currentlyActiveScreen = "Theme";
        }

        public async void BtnGreenScreen_Click(object sender, RoutedEventArgs e)
        {
            chromeHelper.ExecuteJavascriptOnLive("document.body.style.transition = 'opacity 0.5s ease'; document.body.style.opacity = '0';");
            await System.Threading.Tasks.Task.Delay(500);

            string baseDir = FileHelper.GetAppFolder();
            System.IO.File.WriteAllText(System.IO.Path.Combine(baseDir, @"WebTemplate\SongLive.html"), "<html style='background:#00ff00;'></html>");

            chromeHelper.NavigateLiveDisplay("SongLive");
            if (chromeHelper.IsPresentationOpen()) chromeHelper.NavigatePresentationDisplay("SongLive");

            currentlyActiveScreen = "GreenScreen";
        }

        // NEW: Generates the looping video background!
        private void CreateDefaultThemeHtml()
        {
            string baseDir = FileHelper.GetAppFolder();

            // Looks for a file named "DefaultTheme.mp4" in your Video folder!
            string html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ margin: 0; background-color: #000; overflow: hidden; }} 
        video {{ width: 100vw; height: 100vh; object-fit: cover; }}
    </style>
</head>
<body>
    <!-- Auto-plays your loop video! -->
    <video autoplay loop muted>
        <source src='Video/DefaultTheme.mp4' type='video/mp4'>
    </video>
</body>
</html>";

            System.IO.File.WriteAllText(System.IO.Path.Combine(baseDir, @"WebTemplate\SongLive.html"), html);
        }
        private void BtnCloseLive_Click(object sender, RoutedEventArgs e)
        {
            chromeHelper.CloseChrome();
            currentlyActiveScreen = "";
        }
        // --- MOBILE REMOTE SYNC METHODS ---

        // 1. Phone user tapped a song in the Search List
        private void RemoteServer_OnSongSelected(string songName)
        {
            // If the Song Dashboard isn't open, open it so the operator can see what the phone is doing!
            if (SongDashboard.Visibility != Visibility.Visible) BtnNavSongs_Click(null, null);

            // Directly injects the song into your Master Loader, identical to using the Dropdown!
            LoadSongIntoEditor(songName);
            SongSearchBox.Text = songName;

            // Instantly Submit it so the 42 buttons populate and are ready to be pushed!
            BtnSubmit_Click(null, null);
        }

        // 2. Phone user tapped a Lyric Button
        private void RemoteServer_OnLyricClicked(int index)
        {
            if (FinalHtmlModels == null || FinalHtmlModels.Count == 0) return;

            // Ensures we don't try to click a button that doesn't exist
            if (index >= 0 && index < FinalHtmlModels.Count)
            {
                songCounter = index;
                ChoosLyrics(); // Pushes to the Live Screen instantly!
            }
        }
        // FIX: Smart Arrow Keys (WITH 42 RULE VERTICAL JUMPING RESTORED!)
        // FIX: Flawless Double-Shift, ESC wipe, and protected Dropdown Navigation!
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // ----------------------------------------------------------------
            // GLOBAL DASHBOARD SHORTCUTS (F1 - F5)
            // ----------------------------------------------------------------
            if (e.Key == Key.F1) { BtnNavSongs_Click(null, null); e.Handled = true; return; }
            if (e.Key == Key.F2) { BtnNavBible_Click(null, null); e.Handled = true; return; }
            if (e.Key == Key.F3) { BtnNavLowerThird_Click(null, null); e.Handled = true; return; }
            if (e.Key == Key.F4) { BtnNavBirthdays_Click(null, null); e.Handled = true; return; }
            if (e.Key == Key.F5) { BtnNavSettings_Click(null, null); e.Handled = true; return; }

            // 1. ESC KEY FIX: Wipes screen based on Dashboard and Presentation Mode!
            // 1. ESC KEY FIX: Wipes screen based exactly on Presentation Mode checkbox!
            // 1. ESC KEY FIX: Wipes screen based on Dashboard and Presentation Mode!
            if (e.Key == Key.Escape)
            {
                if (SongDashboard.Visibility == Visibility.Visible)
                {
                    if (chkPresentation.IsChecked == true) BtnThemeLogo_Click(null, null); // Theme for Presentation
                    else BtnGreenScreen_Click(null, null); // Green Screen for standard lyrics
                    e.Handled = true; return;
                }
                else if (BibleDashboard.Visibility == Visibility.Visible)
                {
                    // FIX: Always default to the Theme Loop when Esc is hit on the Bible tab!
                    BtnThemeLogo_Click(null, null);
                    e.Handled = true; return;
                }
            }

            // ... Leave the rest of OnPreviewKeyDown (Double-Shift, Arrows, etc) exactly as it is! ...

            // 2. True Double-Tap Shift to Search
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                if (!e.IsRepeat)
                {
                    if ((DateTime.Now - lastShiftPress).TotalMilliseconds < 400)
                    {
                        if (SongDashboard.Visibility == Visibility.Visible)
                        {
                            SongSearchBox.Focus();
                            SongSearchBox.SelectAll();
                        }
                        else if (BibleDashboard.Visibility == Visibility.Visible)
                        {
                            BibleSearchBox.Focus();
                            BibleSearchBox.SelectAll();
                        }
                        e.Handled = true;
                    }
                    lastShiftPress = DateTime.Now;
                }
            }

            // 3. FIX: Protect the Custom Floating Suggestion List!
            if (SongSuggestionPopup.IsOpen)
            {
                // If you are typing and hit Down, force focus directly into the list items!
                if (e.Key == Key.Down && Keyboard.FocusedElement == SongSearchBox)
                {
                    SongSuggestionList.Focus();
                    if (SongSuggestionList.Items.Count > 0)
                    {
                        SongSuggestionList.SelectedIndex = 0;
                        var item = (ListBoxItem)SongSuggestionList.ItemContainerGenerator.ContainerFromIndex(0);
                        item?.Focus();
                    }
                    e.Handled = true;
                    return;
                }

                // If you are navigating the list, let native WPF handle Up/Down/Enter without hijacking it!
                if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Enter)
                {
                    return;
                }
            }

            // Don't steal arrows if typing in ANY text box
            if (Keyboard.FocusedElement is TextBox)
            {
                base.OnPreviewKeyDown(e); return;
            }

            // BIBLE NAVIGATION
            if (BibleDashboard.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Right || e.Key == Key.Down) { BtnNextVerse_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.Left || e.Key == Key.Up) { BtnPrevVerse_Click(null, null); e.Handled = true; }
                return;
            }

            // SONG NAVIGATION (42 RULE SAFE)
            if (SongDashboard.Visibility == Visibility.Visible)
            {
                if (FinalHtmlModels == null || FinalHtmlModels.Count == 0) return;
                bool isCtrltHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

                if (e.Key == Key.Right) { if (songCounter < FinalHtmlModels.Count - 1) { songCounter++; if (!isCtrltHeld) ChoosLyrics(); else UpdateGridHighlightOnly(); e.Handled = true; } }
                else if (e.Key == Key.Left) { if (songCounter > 0) { songCounter--; if (!isCtrltHeld) ChoosLyrics(); else UpdateGridHighlightOnly(); e.Handled = true; } }
                else if (e.Key == Key.Down) { if (songCounter + 5 < FinalHtmlModels.Count) { songCounter += 5; if (!isCtrltHeld) ChoosLyrics(); else UpdateGridHighlightOnly(); e.Handled = true; } }
                else if (e.Key == Key.Up) { if (songCounter - 5 >= 0) { songCounter -= 5; if (!isCtrltHeld) ChoosLyrics(); else UpdateGridHighlightOnly(); e.Handled = true; } }
            }
            base.OnPreviewKeyDown(e);
        }

        // Detects when you let go of the Shift key!
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (SongDashboard.Visibility == Visibility.Visible && (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl))
            {
                if (songCounter != currentlyLiveSongCounter)
                {
                    ChoosLyrics();
                }
                e.Handled = true;
            }
            base.OnPreviewKeyUp(e);
        }

        // Moves the Green Box visually WITHOUT sending it to the Live Display!
        private void UpdateGridHighlightOnly()
        {
            for (int i = 0; i < SongLyricsbuttons.Count; i++)
            {
                if (SongLyricsbuttons[i].Visibility != Visibility.Visible) continue;
                if (i == songCounter) SongLyricsbuttons[i].Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF9F43"); // Orange while jumping!
                else SongLyricsbuttons[i].Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(SongLyricsbuttons[i].Uid);
            }
        }

        // FIX: Font Size Buttons wired up!
        // FIX: Smart Font Adjusters. Only pushes to the Live Display if a song is actually live!
        private void BtnIncreaseFont_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FontSizeBox.Text, out int size)) FontSizeBox.Text = (size + 5).ToString();

            string activeMemory = chkPresentation.IsChecked == true ? memPres : memGreen;
            System.IO.File.WriteAllText(activeMemory, FontSizeBox.Text);

            if (!string.IsNullOrEmpty(LyricsInputBox.Text))
            {
                int savedCounter = songCounter;
                BtnSubmit_Click(null, null); // Recalculates the internal math
                songCounter = savedCounter;

                // Only push to the display if the audience is already looking at lyrics!
                if (currentlyActiveScreen == "Song" || currentlyActiveScreen == "Presentation")
                    ChoosLyrics();
                else
                    UpdateGridHighlightOnly(); // Just visually updates your 42-grid
            }
        }

        private void BtnDecreaseFont_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FontSizeBox.Text, out int size)) FontSizeBox.Text = (size - 5).ToString();

            string activeMemory = chkPresentation.IsChecked == true ? memPres : memGreen;
            System.IO.File.WriteAllText(activeMemory, FontSizeBox.Text);

            if (!string.IsNullOrEmpty(LyricsInputBox.Text))
            {
                int savedCounter = songCounter;
                BtnSubmit_Click(null, null);
                songCounter = savedCounter;

                if (currentlyActiveScreen == "Song" || currentlyActiveScreen == "Presentation")
                    ChoosLyrics();
                else
                    UpdateGridHighlightOnly();
            }
        }

        private void ChoosLyrics()
        {
            if (FinalHtmlModels == null || FinalHtmlModels.Count <= songCounter) return;
            currentlyLiveSongCounter = songCounter;

            bool isPresMode = chkPresentation.IsChecked == true;
            string fontSizeStr = string.IsNullOrWhiteSpace(FontSizeBox.Text) ? "80" : FontSizeBox.Text;

            if (isPresMode)
            {
                chromeHelper.NavigateLiveDisplay("SongPresentation");
                if (chkDualDisplay.IsChecked == true) chromeHelper.NavigatePresentationDisplay("SongPresentation");
                currentlyActiveScreen = "Presentation";

                // NEW: Flawless sync! Directly uses the pre-calculated paragraph we built in BtnSubmit! No searching needed!
                string fullParagraph = FinalHtmlModels[songCounter].PresentationLyrics;

                bool animEnabled = chkAnimSong.IsChecked == true;
                int animSpeed = (int)SongAnimSpeed.Value;

                string g1 = "#0f2027"; string g2 = "#2c5364";
                string gradFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), "gradient_memory.txt");
                if (System.IO.File.Exists(gradFile)) { string[] parts = System.IO.File.ReadAllText(gradFile).Split('|'); if (parts.Length == 2) { g1 = parts[0]; g2 = parts[1]; } }

                fileHelper.SaveasHtmlForPresentation(new HtmlModel() { Lyrics = fullParagraph, FontSize = fontSizeStr + "px" }, g1, g2, "SongPresentation", animEnabled, animSpeed);

                string jsLyrics = fullParagraph.Replace("\r", "").Replace("\n", "").Replace("'", "\\'");
                string jsAnim = animEnabled ? "true" : "false";

                chromeHelper.ExecuteJavascriptOnLive($"if(typeof updateLyrics === 'function') {{ updateLyrics('{jsLyrics}', {jsAnim}, {animSpeed}, '{fontSizeStr}px'); }} else {{ location.reload(); }}");
            }
            else
            {
                currentlyActiveScreen = "Song";
                fileHelper.SaveasHtmlForSong(templateName, FinalHtmlModels[songCounter]);

                chromeHelper.NavigateLiveDisplay("SongLive");
                if (chromeHelper.IsPresentationOpen()) chromeHelper.NavigatePresentationDisplay("SongLive");
            }

            for (int i = 0; i < SongLyricsbuttons.Count; i++)
            {
                if (SongLyricsbuttons[i].Visibility != Visibility.Visible) continue;
                if (i == songCounter) SongLyricsbuttons[i].Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                else SongLyricsbuttons[i].Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(SongLyricsbuttons[i].Uid);
            }
        }



        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SongSearchBox.Text)) return;

            string safeFileName = SongSearchBox.Text.Trim();
            MessageBoxResult dialogResult = MessageBox.Show($"Are you sure you want to delete '{safeFileName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (dialogResult == MessageBoxResult.Yes)
            {
                // FIX: Clean, non-redundant dynamic path generation!
                string filePath = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\SongDataBase");
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

                string listPath = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList");
                if (System.IO.File.Exists(listPath))
                {
                    var songs = System.IO.File.ReadAllText(listPath).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList();
                    songs.RemoveAll(s => s.Equals(safeFileName, StringComparison.OrdinalIgnoreCase));
                    if (songs.Count == 0) songs.Add(" ");
                    System.IO.File.WriteAllText(listPath, string.Join(";", songs));
                }

                songDatabaseMemory = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
                LyricsInputBox.Text = "";
                SongSearchBox.Text = "";
                MessageBox.Show("Song deleted successfully!", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var song = LyricsInputBox.Text;
            if (!string.IsNullOrEmpty(song))
            {
                string currentSelection = SongSearchBox.Text.Trim();

                string defaultTitle = "";
                if (!string.IsNullOrEmpty(autoFilledTitle)) defaultTitle = autoFilledTitle;
                else if (!string.IsNullOrEmpty(currentSelection)) defaultTitle = currentSelection;

                string songName = ShowModernSaveDialog(defaultTitle);
                if (songName == null) return;

                if (!string.IsNullOrWhiteSpace(songName))
                {
                    string safeFileName = string.Join("_", songName.Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
                    string baseDir = FileHelper.GetAppFolder();

                    // FIX: Rebuilds the entire list directly from the physical hard drive folder!
                    // This guarantees we never lose a song, even if the memory file gets corrupted!
                    string dbFolder = System.IO.Path.Combine(baseDir, @"WebTemplate\SongDataBase");
                    if (!System.IO.Directory.Exists(dbFolder)) System.IO.Directory.CreateDirectory(dbFolder);

                    // Actually write the new song to the hard drive FIRST
                    string finalSaveContent = song;
                    if (chkDualEditor.IsChecked == true && !string.IsNullOrWhiteSpace(SecondaryLyricsBox.Text))
                        finalSaveContent += "\n===TANGLISH===\n" + SecondaryLyricsBox.Text;

                    fileHelper.CreateFileSong(safeFileName, finalSaveContent, "txt");

                    // Now scan the folder and rebuild the Master Memory List!
                    var nameList = new List<string>();
                    foreach (string file in System.IO.Directory.GetFiles(dbFolder, "*.txt"))
                    {
                        nameList.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                    }

                    // Save the Master List
                    fileHelper.CreateFile(@"DropDownList\SongDataBaseList", string.Join(";", nameList), "txt");

                    // Update memory instantly
                    songDatabaseMemory = nameList.OrderBy(x => x).ToList();

                    SongSearchBox.Text = safeFileName;
                    autoFilledTitle = "";
                    MessageBox.Show("Song saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Song Cannot be empty", "Invalid Song", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnUndoEventSave_Click(object sender, RoutedEventArgs e)
        {
            bool restored = false;

            // Only restores actual Database Saves and Deletes!
            if (!string.IsNullOrEmpty(backupBdayJson))
            {
                System.IO.File.WriteAllText(bdayDbPath, backupBdayJson);
                backupBdayJson = ""; // Clears undo memory
                restored = true;
            }
            if (!string.IsNullOrEmpty(backupAnnivJson))
            {
                System.IO.File.WriteAllText(annivDbPath, backupAnnivJson);
                backupAnnivJson = "";
                restored = true;
            }

            if (restored)
            {
                LoadDatabaseToUI(isShowingThisWeekOnly);
                MessageBox.Show("Your last Save/Delete action has been reversed!", "Undo Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Nothing to undo! (No recent saves or deletes).", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        // A sleek, modern Dark-Mode Save Box!
        private string ShowModernSaveDialog(string defaultText)
        {
            Window prompt = new Window()
            {
                Width = 400,
                Height = 200,
                Title = "Save Song",
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#0F111A"),
                Foreground = Brushes.White,
                WindowStyle = WindowStyle.ToolWindow
            };

            StackPanel panel = new StackPanel() { Margin = new Thickness(20) };
            TextBlock label = new TextBlock() { Text = "Enter Song Name (Keep same to Overwrite):", Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White };
            TextBox input = new TextBox() { Text = defaultText, FontSize = 14, Padding = new Thickness(5), Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#1C1E30"), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            Button btn = new Button() { Content = "Save", Width = 100, Height = 35, Margin = new Thickness(0, 20, 0, 0), Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478"), Foreground = Brushes.White, Cursor = Cursors.Hand, BorderThickness = new Thickness(0) };

            btn.Click += (s, e) => prompt.DialogResult = true;
            panel.Children.Add(label); panel.Children.Add(input); panel.Children.Add(btn);
            prompt.Content = panel;

            return prompt.ShowDialog() == true ? input.Text.Trim() : null;
        }
        private void BtnFetchWeb_Click(object sender, RoutedEventArgs e)
        {
            SongFetcherForm fetcher = new SongFetcherForm();
            if (fetcher.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(fetcher.FetchedLyrics))
                {
                    LyricsInputBox.Text = fetcher.FetchedLyrics.Replace("\n", "\r\n");
                    autoFilledTitle = fetcher.FetchedTitle;
                    MessageBox.Show("Lyrics imported! You can now edit them, add paragraph spacing, and click Save.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (!string.IsNullOrEmpty(fetcher.FetchedTitle))
                {
                    string searchTitle = fetcher.FetchedTitle.ToLower();

                    // Look for a partial match in our memory list!
                    string match = songDatabaseMemory.FirstOrDefault(x => x.ToLower().Contains(searchTitle) || searchTitle.Contains(x.ToLower()));

                    if (match != null)
                    {
                        LoadSongIntoEditor(match); // Instantly loads it!
                    }
                    else
                    {
                        MessageBox.Show($"Could not find a song named '{fetcher.FetchedTitle}' in your local database.\n\nYou may need to Auto-Import the lyrics first!", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        // Allows you to click and drag the custom dark title bar!
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        // Reads dropdown files safely!
        public List<string> FiletoList(string filename)
        {
            string baseDir = FileHelper.GetAppFolder();
            string filePath = System.IO.Path.Combine(baseDir, $@"WebTemplate\DropDownList\{filename}.txt");

            if (!System.IO.File.Exists(filePath))
                return new List<string>();

            return System.IO.File.ReadAllText(filePath).Replace("\r\n", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        public void ChkPresentation_Click(object sender, RoutedEventArgs e)
        {
            if (chkPresentation.IsChecked == true)
            {
                if (System.IO.File.Exists(memPres)) FontSizeBox.Text = System.IO.File.ReadAllText(memPres);
                else FontSizeBox.Text = "80"; // Default Presentation Size is larger!
            }
            else
            {
                if (System.IO.File.Exists(memGreen)) FontSizeBox.Text = System.IO.File.ReadAllText(memGreen);
                else FontSizeBox.Text = "45"; // Default Green Screen Size
            }

            // Instantly refresh the live display to show the new size!
            if (!string.IsNullOrEmpty(LyricsInputBox.Text)) BtnSubmit_Click(null, null);
        }
        // ----------------------------------------------------------------
        // NAVIGATION & GLOBAL CONTROLS
        // ----------------------------------------------------------------
        private void ClearAllSidebarHighlights()
        {
            // FIX: Clears the highlight from ALL buttons so they don't get stuck!
            BtnMenuSongs.Tag = "";
            BtnMenuBible.Tag = "";
            BtnMenuLowerThird.Tag = "";
            BtnMenuBirthdays.Tag = "";
            BtnMenuRemote.Tag = "";
            BtnMenuSettings.Tag = "";
        }

        public void BtnNavSongs_Click(object sender, RoutedEventArgs e)
        {
            SongDashboard.Visibility = Visibility.Visible; SettingsDashboard.Visibility = Visibility.Collapsed; BibleDashboard.Visibility = Visibility.Collapsed; RemoteDashboard.Visibility = Visibility.Collapsed; LowerThirdDashboard.Visibility = Visibility.Collapsed; BirthdayDashboard.Visibility = Visibility.Collapsed;
            ClearAllSidebarHighlights(); BtnMenuSongs.Tag = "Active";
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        public void BtnNavBible_Click(object sender, RoutedEventArgs e)
        {
            SongDashboard.Visibility = Visibility.Collapsed; SettingsDashboard.Visibility = Visibility.Collapsed; BibleDashboard.Visibility = Visibility.Visible; RemoteDashboard.Visibility = Visibility.Collapsed; LowerThirdDashboard.Visibility = Visibility.Collapsed; BirthdayDashboard.Visibility = Visibility.Collapsed;
            ClearAllSidebarHighlights(); BtnMenuBible.Tag = "Active";
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        private void BtnNavLowerThird_Click(object sender, RoutedEventArgs e)
        {
            SongDashboard.Visibility = Visibility.Collapsed; SettingsDashboard.Visibility = Visibility.Collapsed; BibleDashboard.Visibility = Visibility.Collapsed; RemoteDashboard.Visibility = Visibility.Collapsed; LowerThirdDashboard.Visibility = Visibility.Visible; BirthdayDashboard.Visibility = Visibility.Collapsed;
            ClearAllSidebarHighlights(); BtnMenuLowerThird.Tag = "Active";
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        private void BtnNavBirthdays_Click(object sender, RoutedEventArgs e)
        {
            SongDashboard.Visibility = Visibility.Collapsed; SettingsDashboard.Visibility = Visibility.Collapsed; BibleDashboard.Visibility = Visibility.Collapsed; RemoteDashboard.Visibility = Visibility.Collapsed; LowerThirdDashboard.Visibility = Visibility.Collapsed; BirthdayDashboard.Visibility = Visibility.Visible;   
            ClearAllSidebarHighlights(); BtnMenuBirthdays.Tag = "Active";
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        private void BtnNavRemote_Click(object sender, RoutedEventArgs e)
        {
            SongDashboard.Visibility = Visibility.Collapsed; BibleDashboard.Visibility = Visibility.Collapsed; RemoteDashboard.Visibility = Visibility.Visible; LowerThirdDashboard.Visibility = Visibility.Collapsed; BirthdayDashboard.Visibility = Visibility.Collapsed; SettingsDashboard.Visibility = Visibility.Collapsed;
            ClearAllSidebarHighlights(); BtnMenuRemote.Tag = "Active";
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
        {
            SongDashboard.Visibility = Visibility.Collapsed; BibleDashboard.Visibility = Visibility.Collapsed;
            RemoteDashboard.Visibility = Visibility.Collapsed; LowerThirdDashboard.Visibility = Visibility.Collapsed;
            BirthdayDashboard.Visibility = Visibility.Collapsed; SettingsDashboard.Visibility = Visibility.Visible; // ACTIVE

            ClearAllSidebarHighlights(); BtnMenuSettings.Tag = "Active";
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        // ----------------------------------------------------------------
        // VERSEVIEW BIBLE ENGINE
        // ----------------------------------------------------------------
        private string currentBook = "";
        private string currentChapter = "";
        private int currentVerse = 1;
        private string customBgPath = "";
        private bool isUpdatingList = false;
        // --- NEW BIBLE STYLING VARIABLES & METHODS ---
        private double bibleFontSizeOffset = 0;

        private void LoadBibleDropdowns()
        {
            string[] fonts = { "BalooThambi", "Nirmala UI", "Segoe UI", "Arial", "Times New Roman", "Georgia", "Verdana", "Tahoma", "Courier New" };
            CbVerseFont.ItemsSource = fonts; CbVerseFont.SelectedIndex = 0;
            CbRefFont.ItemsSource = fonts; CbRefFont.SelectedIndex = 0;

            string[] colors = { "White", "Gold", "Yellow", "MediumSeaGreen", "LightSkyBlue", "Orange", "LightGray" };
            CbVerseColor.ItemsSource = colors; CbVerseColor.SelectedIndex = 0;

            
        }

        // FIX: The offset math is now perfect so Reset goes exactly back to default!
        private void BtnBibleFontUp_Click(object sender, RoutedEventArgs e) { bibleFontSizeOffset += 1.0; DisplayVerse(); }
        private void BtnBibleFontDown_Click(object sender, RoutedEventArgs e) { bibleFontSizeOffset -= 1.0; DisplayVerse(); }

        // FIX: Instantly updates the UI when you click any Language Checkbox!
        private List<string> activeLangs = new List<string> { "tamilBible" };

        // FIX: The order you click the boxes is the exact order they appear on screen!
        private System.Collections.ObjectModel.ObservableCollection<BibleLangItem> bibleLangs;

        private void LoadBibleLanguageList()
        {
            // Initial default order
            bibleLangs = new System.Collections.ObjectModel.ObservableCollection<BibleLangItem>
            {
                new BibleLangItem { DisplayName = "Tamil Bible", CodeName = "tamilBible", IsActive = true },
                new BibleLangItem { DisplayName = "English Bible", CodeName = "englishBible", IsActive = true },
                new BibleLangItem { DisplayName = "Hindi Bible", CodeName = "hindiBible", IsActive = false },
                new BibleLangItem { DisplayName = "Marathi Bible", CodeName = "marathiBible", IsActive = false }
            };
            LanguageOrderList.ItemsSource = bibleLangs;
        }

        private void BtnMoveLangUp_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            BibleLangItem item = btn.Tag as BibleLangItem;
            int index = bibleLangs.IndexOf(item);

            if (index > 0)
            {
                bibleLangs.Move(index, index - 1); // Physically moves it UP in the visual list!
                if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) { PopulateChapterPreview(); DisplayVerse(); }
            }
        }

        private void BtnMoveLangDown_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            BibleLangItem item = btn.Tag as BibleLangItem;
            int index = bibleLangs.IndexOf(item);

            if (index >= 0 && index < bibleLangs.Count - 1)
            {
                bibleLangs.Move(index, index + 1); // Physically moves it DOWN in the visual list!
                if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) { PopulateChapterPreview(); DisplayVerse(); }
            }
        }

        private void LanguageCheckbox_Click(object sender, RoutedEventArgs e)
        {
            // Instantly refreshes the screen when a box is checked/unchecked
            if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook))
            {
                PopulateChapterPreview();
                DisplayVerse();
            }
        }
        // Instantly zeros out the offset and refreshes the exact default size!
        private void BtnBibleFontReset_Click(object sender, RoutedEventArgs e)
        {
            bibleFontSizeOffset = 0.0;
            if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) DisplayVerse();
        }
        // FIX: Now accepts both ComboBox changes AND CheckBox clicks!
        private void BibleStyle_Changed(object sender, RoutedEventArgs e)
        {
            if (BibleDashboard != null && BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) DisplayVerse();
        }        // ----------------------------------------------

        private readonly string[] BibleBooksList = { "Genesis", "Exodus", "Leviticus", "Numbers", "Deuteronomy", "Joshua", "Judges", "Ruth", "1 Samuel", "2 Samuel", "1 Kings", "2 Kings", "1 Chronicles", "2 Chronicles", "Ezra", "Nehemiah", "Esther", "Job", "Psalms", "Proverbs", "Ecclesiastes", "Song of Solomon", "Isaiah", "Jeremiah", "Lamentations", "Ezekiel", "Daniel", "Hosea", "Joel", "Amos", "Obadiah", "Jonah", "Micah", "Nahum", "Habakkuk", "Zephaniah", "Haggai", "Zechariah", "Malachi", "Matthew", "Mark", "Luke", "John", "Acts", "Romans", "1 Corinthians", "2 Corinthians", "Galatians", "Ephesians", "Philippians", "Colossians", "1 Thessalonians", "2 Thessalonians", "1 Timothy", "2 Timothy", "Titus", "Philemon", "Hebrews", "James", "1 Peter", "2 Peter", "1 John", "2 John", "3 John", "Jude", "Revelation" };

        // FIX: Flawless translated book names for the Dual-Language Reference Banner!
        private readonly string[] TamilBooksList = { "ஆதியாகமம்", "யாத்திராகமம்", "லேவியராகமம்", "எண்ணாகமம்", "உபாகமம்", "யோசுவா", "நியாயாதிபதிகள்", "ரூத்", "1 சாமுவேல்", "2 சாமுவேல்", "1 இராஜாக்கள்", "2 இராஜாக்கள்", "1 நாளாகமம்", "2 நாளாகமம்", "எஸ்றா", "நெகேமியா", "எஸ்தர்", "யோபு", "சங்கீதம்", "நீதிமொழிகள்", "பிரசங்கி", "உன்னதப்பாட்டு", "ஏசாயா", "எரேமியா", "புலம்பல்", "எசேக்கியேல்", "தானியேல்", "ஓசியா", "யோவேல்", "ஆமோஸ்", "ஒபதியா", "யோனா", "மீகா", "நாகூம்", "ஆபகூக்", "செப்பனியா", "ஆகாய்", "சகரியா", "மல்கியா", "மத்தேயு", "மாற்கு", "லூக்கா", "யோவான்", "அப்போஸ்தலர்", "ரோமர்", "1 கொரிந்தியர்", "2 கொரிந்தியர்", "கலாத்தியர்", "எபேசியர்", "பிலிப்பியர்", "கொலோசெயர்", "1 தெசலோனிக்கேயர்", "2 தெசலோனிக்கேயர்", "1 தீமோத்தேயு", "2 தீமோத்தேயு", "தீத்து", "பிலேமோன்", "எபிரெயர்", "யாக்கோபு", "1 பேதுரு", "2 பேதுரு", "1 யோவான்", "2 யோவான்", "3 யோவான்", "யூதா", "வெளிப்படுத்தின விசேஷம்" };
        private readonly string[] HindiBooksList = { "उत्पत्ति", "निर्गमन", "लैव्यव्यवस्था", "गिनती", "व्यवस्थाविवरण", "यहोशू", "न्यायियों", "रूत", "1 शमूएल", "2 शमूएल", "1 राजा", "2 राजा", "1 इतिहास", "2 इतिहास", "एज्रा", "नहेमायाह", "एस्तेर", "अय्यूब", "भजन संहिता", "नीतिवचन", "सभोपदेशक", "श्रेष्ठगीत", "यशायाह", "यिर्मयाह", "विलापगीत", "यहेजकेल", "दानिय्येल", "होशे", "योएल", "आमोस", "ओबद्याह", "योना", "मीका", "नहूम", "हबक्कूक", "सपन्याह", "हाग्गै", "जकर्याह", "मलाकी", "मत्ती", "मरकुस", "लूका", "यूहन्ना", "प्रेरितों के काम", "रोमियों", "1 कुरिन्थियों", "2 कुरिन्थियों", "गलातियों", "इफिसियों", "फिलिप्पियों", "कुलुस्सियों", "1 थिस्सलुनीकियों", "2 थिस्सलुनीकियों", "1 तीमुथियुस", "2 तीमुथियुस", "तीतुस", "फिलेमोन", "इब्रानियों", "याकूब", "1 पतरस", "2 पतरस", "1 यूहन्ना", "2 यूहन्ना", "3 यूहन्ना", "यहूदा", "प्रकाशितवाक्य" };
        private readonly string[] MarathiBooksList = { "उत्पत्ती", "निर्गम", "लेवीय", "गणना", "अनुवाद", "यहोशवा", "शास्ते", "रूथ", "1 शमुवेल", "2 शमुवेल", "1 राजे", "2 राजे", "1 इतिहास", "2 इतिहास", "एज्रा", "नहेम्या", "एस्तेर", "ईयोब", "स्तोत्रसंहिता", "नीतिसूत्रे", "उपदेशक", "गीतरत्न", "यशया", "यिर्मया", "विलापगीत", "यहेज्केल", "दानीएल", "होशेय", "योएल", "आमोस", "ओबद्या", "योना", "मीखा", "नहूम", "हबक्कूक", "सफन्या", "हाग्गय", "जखऱ्या", "मलाखी", "मत्तय", "मार्क", "लूक", "योहान", "प्रेषितांची कृत्ये", "रोमकर", "1 करिंथकर", "2 करिंथकर", "गलतीकर", "इफिसकर", "फिलिप्पैकर", "कलस्सैकर", "1 थेस्सलनीकाकर", "2 थेस्सलनीकाकर", "1 तीमथ्य", "2 तीमथ्य", "तीत", "फिलेमोन", "इब्री", "याकोब", "1 पेत्र", "2 पेत्र", "1 योहान", "2 योहान", "3 योहान", "यहूदा", "प्रकटीकरण" };
        public void BibleSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ParseBibleSearch(BibleSearchBox.Text);
                ChapterVerseList.Focus();
            }
        }

        private void BibleSearchBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 3) { BibleSearchBox.SelectAll(); e.Handled = true; }
        }

        public void BtnNextVerse_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentBook)) return;

            // Check if there's actually a verse to jump to!
            if (currentVerse < ChapterVerseList.Items.Count)
            {
                currentVerse++;
                BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";

                // FIX: Force the highlight to jump!
                isUpdatingList = true;
                ChapterVerseList.SelectedIndex = currentVerse - 1;
                ChapterVerseList.ScrollIntoView(ChapterVerseList.SelectedItem);
                isUpdatingList = false;

                DisplayVerse();
            }
        }

        public void BtnPrevVerse_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentBook) || currentVerse <= 1) return;

            currentVerse--;
            BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";

            // FIX: Force the highlight to jump!
            isUpdatingList = true;
            ChapterVerseList.SelectedIndex = currentVerse - 1;
            ChapterVerseList.ScrollIntoView(ChapterVerseList.SelectedItem);
            isUpdatingList = false;

            DisplayVerse();
        }

        // When you click a verse in the Middle Column preview list!
        // FIX: Still handles keyboard navigation normally
        public void ChapterVerseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingList || ChapterVerseList.SelectedIndex == -1) return;

            currentVerse = ChapterVerseList.SelectedIndex + 1; // Index 0 is Verse 1
            BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";
            DisplayVerse();
        }

        // FIX: Forces a push if you physically CLICK a verse that is already selected!
        private void ChapterVerseList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isUpdatingList) return;

            var item = ItemsControl.ContainerFromElement(ChapterVerseList, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null && item.IsSelected)
            {
                // If they clicked the item that was ALREADY selected, force it live again!
                DisplayVerse();
            }
        }

        // FIX: Ultra-forgiving Smart Parser (Allows "Est", "Est 2", etc.)
        // FIX: Ultra-Smart Parser. Handles multi-word books (Song of Solomon) AND abbreviations (1 Sam, Gen)!
        private void ParseBibleSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            string q = query.Replace(":", " ").Trim().ToLower();
            if (q.Length == 0) return;

            currentBook = null;
            string matchedPrefix = "";

            // 1. FIRST PASS: Try to match the EXACT full book name (Fixes "Song of Solomon 3 4")
            foreach (string book in BibleBooksList)
            {
                if (q.StartsWith(book.ToLower()))
                {
                    currentBook = book;
                    matchedPrefix = book.ToLower();
                    break;
                }
            }

            // 2. SECOND PASS: If no full match, try abbreviation (e.g. "son 3 4", "1 sam 2 3")
            if (currentBook == null)
            {
                string[] qParts = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (qParts.Length == 0) return;

                string prefix = qParts[0];
                if (int.TryParse(qParts[0], out _) && qParts.Length > 1)
                {
                    prefix = qParts[0] + " " + qParts[1];
                }

                currentBook = BibleBooksList.FirstOrDefault(b => b.ToLower().StartsWith(prefix));
                if (currentBook != null) matchedPrefix = prefix;
            }

            // 3. EXTRACT NUMBERS: Strip the matched book name away, leaving only the Chapter & Verse!
            // 3. EXTRACT NUMBERS: Strip the matched book name away, leaving only the Chapter & Verse!
            if (currentBook != null)
            {
                string remainder = q.Substring(matchedPrefix.Length).Trim();
                string[] numParts = remainder.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                currentChapter = numParts.Length > 0 ? numParts[0] : "1";
                currentVerse = 1; // Default
                if (numParts.Length > 1) int.TryParse(numParts[1], out currentVerse);

                // --- NEW: CHAPTER SNAPPER ---
                // FIX: Uses the top priority active language to fetch the chapter preview list!
                string folderName = bibleLangs.FirstOrDefault(l => l.IsActive)?.CodeName ?? "tamilBible"; int bookIndex = Array.IndexOf(BibleBooksList, currentBook) + 1;
                string folderNum = bookIndex < 10 ? $"0{bookIndex}" : bookIndex.ToString();
                string bookFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), "BibleDataBase", folderName, folderNum);

                if (System.IO.Directory.Exists(bookFolder))
                {
                    if (int.TryParse(currentChapter, out int requestedChapter))
                    {
                        // Count backwards until we find the highest chapter file that physically exists!
                        while (requestedChapter > 1 && !System.IO.File.Exists(System.IO.Path.Combine(bookFolder, $"{requestedChapter}.htm")))
                        {
                            requestedChapter--;
                        }
                        currentChapter = requestedChapter.ToString();
                    }
                }

                // Must populate the list BEFORE verse snapping so we know how many verses exist!
                PopulateChapterPreview();

                // --- NEW: VERSE SNAPPER ---
                if (ChapterVerseList.Items.Count > 0)
                {
                    if (currentVerse > ChapterVerseList.Items.Count)
                    {
                        currentVerse = ChapterVerseList.Items.Count; // Snaps to the very last verse!
                    }
                    if (currentVerse < 1) currentVerse = 1;
                }

                // Output the perfectly snapped numbers to the UI
                BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";
                BibleSearchBox.CaretIndex = BibleSearchBox.Text.Length;

                DisplayVerse();
            }
            else
            {
                MessageBox.Show("Could not find a book matching that abbreviation.", "Search Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // NEW: Click a verse in History to instantly jump back to it!
        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryList.SelectedItem != null && !isUpdatingList)
            {
                ParseBibleSearch(HistoryList.SelectedItem.ToString());
                HistoryList.SelectedIndex = -1; // Deselect so you can click it again later
            }
        }

        // Reads the Primary language folder and grabs every single verse in the chapter!
        // Reads an ENTIRE chapter instantly and returns a Dictionary of all verses
        // Reads an ENTIRE chapter instantly and returns a Dictionary of all verses
        private Dictionary<int, string> FetchFullChapter(string book, string chapter, string folderName)
        {
            var dict = new Dictionary<int, string>();
            try
            {
                // FIX: No more Roman Numeral translation! Directly matches the exact book name!
                int bookIndex = Array.IndexOf(BibleBooksList, book) + 1;
                string folderNum = bookIndex < 10 ? $"0{bookIndex}" : bookIndex.ToString();

                string filePath = System.IO.Path.Combine(FileHelper.GetAppFolder(), "BibleDataBase", folderName, folderNum, $"{chapter}.htm");

                if (!System.IO.File.Exists(filePath)) return dict;

                // ... (leave the HtmlAgilityPack stuff below this exactly the same) ...

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.Load(filePath, System.Text.Encoding.UTF8);

                var nodes = doc.DocumentNode.SelectNodes("//*[@id]");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        if (!int.TryParse(node.Id, out int vNum)) continue;

                        HtmlAgilityPack.HtmlNode txtNode = node.NextSibling;
                        string text = "";
                        while (txtNode != null && !(txtNode.NodeType == HtmlAgilityPack.HtmlNodeType.Element && txtNode.Attributes.Contains("id")))
                        {
                            if (txtNode.NodeType == HtmlAgilityPack.HtmlNodeType.Text) text += txtNode.InnerText;
                            txtNode = txtNode.NextSibling;
                        }
                        dict[vNum] = System.Net.WebUtility.HtmlDecode(text).Trim();
                    }
                }
            }
            catch { }
            return dict;
        }

        // Populates the UI List using the new activeLangs Cycling Array!
        private void PopulateChapterPreview()
        {
            isUpdatingList = true;
            ChapterVerseList.ItemsSource = null;
            LblChapterTitle.Text = $"{currentBook} {currentChapter}";

            if (bibleLangs == null) return;

            // FIX: Grabs the active languages from the reorder list!
            List<string> activeLangs = bibleLangs.Where(l => l.IsActive).Select(l => l.CodeName).ToList();
            if (activeLangs.Count == 0) activeLangs.Add("tamilBible"); // Failsafe

            if (activeLangs.Count == 1)
            {
                ChapterVerseList.ItemTemplate = (DataTemplate)this.FindResource("SingleLangTemplate");
            }
            else
            {
                ChapterVerseList.ItemTemplate = (DataTemplate)this.FindResource("DualLangTemplate");
            }

            var priDict = FetchFullChapter(currentBook, currentChapter, activeLangs[0]);
            var secDict = activeLangs.Count > 1 ? FetchFullChapter(currentBook, currentChapter, activeLangs[1]) : new Dictionary<int, string>();

            int maxVerse = 0;
            if (priDict.Count > 0) maxVerse = priDict.Keys.Max();
            if (secDict.Count > 0) maxVerse = Math.Max(maxVerse, secDict.Keys.Max());

            var previewList = new List<VersePreviewItem>();
            for (int i = 1; i <= maxVerse; i++)
            {
                string p = priDict.ContainsKey(i) ? $"{i}. {priDict[i]}" : $"{i}. [Not Found]";
                string s = secDict.ContainsKey(i) ? $"{i}. {secDict[i]}" : "";
                previewList.Add(new VersePreviewItem { PrimaryText = p, SecondaryText = s });
            }

            ChapterVerseList.ItemsSource = previewList;
            isUpdatingList = false;
        }

        private void DisplayVerse()
        {
            if (string.IsNullOrEmpty(currentBook) || bibleLangs == null) return;

            // 1. Grab ONLY the languages that are currently checked, in the exact order they appear on screen!
            List<string> activeLangs = bibleLangs.Where(l => l.IsActive).Select(l => l.CodeName).ToList();
            if (activeLangs.Count == 0) return; // Failsafe

            List<string> verseTexts = new List<string>();
            foreach (string lang in activeLangs)
            {
                verseTexts.Add(FetchVerseText(currentBook, currentChapter, currentVerse, lang));
            }

            // 2. Exact VerseView Translation Format (No repeating numbers!)
            string finalHeader = "";
            int bIdx = Array.IndexOf(BibleBooksList, currentBook);

            // Primary Name
            string priName = currentBook;
            if (bIdx >= 0)
            {
                if (activeLangs[0] == "tamilBible") priName = TamilBooksList[bIdx];
                else if (activeLangs[0] == "hindiBible") priName = HindiBooksList[bIdx];
                else if (activeLangs[0] == "marathiBible") priName = MarathiBooksList[bIdx];
            }
            finalHeader = $"{priName} {currentChapter}:{currentVerse}";

            // Secondary Name (BRACKETS ONLY)
            if (activeLangs.Count >= 2 && bIdx >= 0)
            {
                string secName = currentBook;
                if (activeLangs[1] == "tamilBible") secName = TamilBooksList[bIdx];
                else if (activeLangs[1] == "hindiBible") secName = HindiBooksList[bIdx];
                else if (activeLangs[1] == "marathiBible") secName = MarathiBooksList[bIdx];

                finalHeader += $" [{secName}]";
            }

            // 3. Update the UI Highlight
            if (currentVerse - 1 >= 0 && currentVerse - 1 < ChapterVerseList.Items.Count)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    isUpdatingList = true;
                    ChapterVerseList.SelectedIndex = currentVerse - 1;
                    ChapterVerseList.ScrollIntoView(ChapterVerseList.SelectedItem);
                    isUpdatingList = false;
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }

            // 4. Gather Formatting Settings
            string vFont = CbVerseFont.SelectedItem?.ToString() ?? "Segoe UI";
            string vColor = CbVerseColor.SelectedItem?.ToString() ?? "White";
            string rFont = CbRefFont.SelectedItem?.ToString() ?? "Segoe UI";
            bool animEnabled = chkAnimBible.IsChecked == true;
            int animSpeed = (int)BibleAnimSpeed.Value;

            string verseHtml = "";
            for (int i = 0; i < verseTexts.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(verseTexts[i]))
                {
                    string fw = (i == 0) ? (chkPriBold.IsChecked == true ? "bold" : "normal") : (chkSecBold.IsChecked == true ? "bold" : "normal");
                    verseHtml += $"<div class='verse-block' style='font-weight: {fw};'>{verseTexts[i]}</div>";
                }
            }

            // 5. Send to Engine!
            // 5. Send to Engine!
            // 5. Send to Engine! (Cleaned up to 11 arguments!)
            fileHelper.SaveasHtmlForVerseViewBible(finalHeader, verseTexts, customBgPath, bibleFontSizeOffset, vFont, vColor, rFont, chkPriBold.IsChecked == true, chkSecBold.IsChecked == true, animEnabled, animSpeed);
            // FIX: Force a hard reload if ANY Lower Third (Text, Image, or Video) is actively taking up the screen!
            if (currentlyActiveScreen == "Bible" && !isLtTextLive && !isLtImageLive && !isLtVideoLive)
            {
                string jsHtml = verseHtml.Replace("\r", "").Replace("\n", "").Replace("'", "\\'");
                string jsHeader = finalHeader.Replace("'", "\\'");
                string jsAnim = animEnabled ? "true" : "false";

                chromeHelper.ExecuteJavascriptOnLive($"if(typeof updateVerse === 'function') {{ updateVerse('{jsHeader}', '{jsHtml}', {bibleFontSizeOffset}, {jsAnim}, {animSpeed}); }} else {{ location.reload(); }}");
            }
            else
            {
                chromeHelper.NavigateLiveDisplay("SongLive");
                if (chkDualDisplay.IsChecked == true) chromeHelper.NavigatePresentationDisplay("SongLive");
                currentlyActiveScreen = "Bible";

                // Reset all Lower Third toggle buttons visually so they don't get stuck on "Remove"!
                if (isLtTextLive) { BtnSubmitLtText.Content = "🚀 GO LIVE!"; BtnSubmitLtText.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478"); isLtTextLive = false; }
                if (isLtImageLive) { BtnSubmitLtImage.Content = "🚀 PUSH LIVE"; BtnSubmitLtImage.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478"); isLtImageLive = false; }
                if (isLtVideoLive) { BtnSubmitLtVideo.Content = "🚀 PUSH LIVE"; BtnSubmitLtVideo.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478"); isLtVideoLive = false; }
            }

            // 6. Update History
            string historyString = $"{currentBook} {currentChapter}:{currentVerse}";
            if (HistoryList.Items.Contains(historyString)) HistoryList.Items.Remove(historyString);
            HistoryList.Items.Insert(0, historyString);
            if (HistoryList.Items.Count > 20) HistoryList.Items.RemoveAt(20);
        }

        // Updated to extract the <title> tag for translated book names!
        private string FetchVerseText(string book, string chapter, int verse, string folderName)
        {
            try
            {
                // FIX: Removed the Roman Numeral translation here too!
                int bookIndex = Array.IndexOf(BibleBooksList, book) + 1;
                string folderNum = bookIndex < 10 ? $"0{bookIndex}" : bookIndex.ToString();

                string filePath = System.IO.Path.Combine(FileHelper.GetAppFolder(), "BibleDataBase", folderName, folderNum, $"{chapter}.htm");
                if (!System.IO.File.Exists(filePath)) return "[File Not Found]";

                // ... (leave the HtmlAgilityPack stuff below this exactly the same) ...

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.Load(filePath, System.Text.Encoding.UTF8);

                HtmlAgilityPack.HtmlNode startNode = doc.DocumentNode.SelectSingleNode($"//*[@id='{verse}']");
                if (startNode != null)
                {
                    string text = ""; HtmlAgilityPack.HtmlNode curr = startNode.NextSibling;
                    while (curr != null)
                    {
                        if (curr.NodeType == HtmlAgilityPack.HtmlNodeType.Element && curr.Attributes.Contains("id")) break;
                        if (curr.NodeType == HtmlAgilityPack.HtmlNodeType.Text) text += curr.InnerText;
                        curr = curr.NextSibling;
                    }
                    return System.Net.WebUtility.HtmlDecode(text).Trim();
                }
                return "";
            }
            catch { return ""; }
        }

        // --- BIBLE BACKGROUND GALLERY ENGINE ---
        private System.Collections.ObjectModel.ObservableCollection<GalleryItem> galleryItems = new System.Collections.ObjectModel.ObservableCollection<GalleryItem>();

        private void LoadBibleBackgrounds()
        {
            string galleryFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\BibleBackgrounds");
            if (!System.IO.Directory.Exists(galleryFolder)) System.IO.Directory.CreateDirectory(galleryFolder);

            var images = System.IO.Directory.GetFiles(galleryFolder).Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)).ToList();

            string bgFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), "bible_bg_memory.txt");
            if (System.IO.File.Exists(bgFile)) customBgPath = System.IO.File.ReadAllText(bgFile);

            galleryItems.Clear();
            foreach (var imgPath in images)
            {
                // FIX: Load the image entirely into RAM and release the file lock instantly!
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // This is the magic lock-breaker!
                bitmap.UriSource = new Uri(imgPath);
                bitmap.EndInit();
                bitmap.Freeze(); // Optimizes performance

                galleryItems.Add(new GalleryItem
                {
                    FilePath = imgPath,
                    ImageSource = bitmap, // Use the unlocked bitmap!
                    BorderColor = imgPath == customBgPath ? (Brush)new BrushConverter().ConvertFrom("#00B478") : Brushes.Transparent
                });
            }
            BgGalleryList.ItemsSource = galleryItems;
        }

        private void BtnAddBibleBg_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Image Files|*.jpg;*.jpeg;*.png" };
            if (ofd.ShowDialog() == true)
            {
                string targetFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\BibleBackgrounds");
                if (!System.IO.Directory.Exists(targetFolder)) System.IO.Directory.CreateDirectory(targetFolder);

                string safeName = System.IO.Path.GetFileName(ofd.FileName).Replace(" ", "_");
                string destFile = System.IO.Path.Combine(targetFolder, safeName);
                System.IO.File.Copy(ofd.FileName, destFile, true);

                LoadBibleBackgrounds();
            }
        }

        private void BgGalleryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BgGalleryList.SelectedItem is GalleryItem selected)
            {
                customBgPath = selected.FilePath;
                System.IO.File.WriteAllText(System.IO.Path.Combine(FileHelper.GetAppFolder(), "bible_bg_memory.txt"), customBgPath);

                foreach (var item in galleryItems) item.BorderColor = item == selected ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478") : System.Windows.Media.Brushes.Transparent;

                if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) DisplayVerse();
                BgGalleryList.SelectedIndex = -1;
            }
        }

        

        private void BtnClearBibleBg_Click(object sender, RoutedEventArgs e)
        {
            customBgPath = "";
            System.IO.File.WriteAllText(System.IO.Path.Combine(FileHelper.GetAppFolder(), "bible_bg_memory.txt"), "");
            if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) DisplayVerse();
        }

        private void BtnDeleteBibleBg_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string imgPath = btn.Tag.ToString();
            if (System.IO.File.Exists(imgPath)) System.IO.File.Delete(imgPath);

            if (customBgPath == imgPath) BtnClearBibleBg_Click(null, null);
            LoadBibleBackgrounds();
        }

        // ----------------------------------------------------------------
        // TEMPORARY JSON TO BIBLE DATABASE CONVERTER
        // ----------------------------------------------------------------

        // ----------------------------------------------------------------
        // UI ANIMATIONS
        // ----------------------------------------------------------------
        private bool isSidebarOpen = true;

        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            var mainBorder = this.Content as Border;
            if (mainBorder != null && mainBorder.Effect != null) mainBorder.Effect = null;

            // FIX: Hard-lock the width of the Bible Dashboard so it CANNOT recalculate its layout!
            if (BibleDashboard.Visibility == Visibility.Visible)
            {
                BibleDashboard.Width = BibleDashboard.ActualWidth;
            }

            System.Windows.Media.Animation.DoubleAnimation animation = new System.Windows.Media.Animation.DoubleAnimation();
            animation.To = isSidebarOpen ? 0 : 255;
            animation.Duration = TimeSpan.FromMilliseconds(200);
            animation.EasingFunction = new System.Windows.Media.Animation.QuadraticEase() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut };

            animation.Completed += (s, ev) =>
            {
                if (mainBorder != null)
                {
                    mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect()
                    { BlurRadius = 15, ShadowDepth = 5, Opacity = 0.5, Color = Colors.Black };
                }

                // UNLOCK the width so it snaps to the new screen size perfectly!
                if (BibleDashboard.Visibility == Visibility.Visible)
                {
                    BibleDashboard.Width = double.NaN; // double.NaN means "Auto" in WPF
                }
            };

            SidebarWrapper.BeginAnimation(WidthProperty, animation);
            isSidebarOpen = !isSidebarOpen;
        }
        // Used to hold the two languages side-by-side in the UI!
        public class VersePreviewItem
        {
            public string PrimaryText { get; set; }
            public string SecondaryText { get; set; }
        }
        // ----------------------------------------------------------------
        // LOWER THIRDS ENGINE
        // ----------------------------------------------------------------
        public bool isLtTextLive = false;
        public bool isLtImageLive = false;
        public bool isLtVideoLive = false;
        private string currentImagePath = "";
        private void FadeOutLowerThird()
        {
            chromeHelper.ExecuteJavascriptOnLive("document.body.className = 'animate-out';");
        }

        private void LoadLowerThirds()
        {
            CbLtText.ItemsSource = fileHelper.FiletoList("LowerThird").OrderBy(x => x).ToList();
            CbLtVideo.ItemsSource = fileHelper.FiletoList("AnimatedVideoLibrary").OrderBy(x => x).ToList();
            CbLtTheme.SelectedIndex = 0;
            CbLtAccent.SelectedIndex = 0;
            CbLtFont.SelectedIndex = 0;
            CbLtDesign.SelectedIndex = 0;
        }
        private async void InitMiniMonitor()
        {
            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.GetTempPath());
            await MiniPreviewMonitor.EnsureCoreWebView2Async(env);

            // Create a blank green screen for the monitor initially
            string previewFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\LtPreview.html");
            if (!System.IO.File.Exists(previewFile)) System.IO.File.WriteAllText(previewFile, "<html style='background:#00ff00;'></html>");

            MiniPreviewMonitor.CoreWebView2.Navigate(new Uri(previewFile).AbsoluteUri);
        }

        // --- 1. TEXT & TITLES ---
        private void BtnAddLtText_Click(object sender, RoutedEventArgs e)
        {
            string newName = CbLtText.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName)) return;

            var list = fileHelper.FiletoList("LowerThird");
            if (!list.Contains(newName, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(newName);
                string listPath = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList\LowerThird.txt");
                System.IO.File.WriteAllText(listPath, string.Join(";", list));
                LoadLowerThirds();
                CbLtText.Text = newName;
                MessageBox.Show("Added to list!");
            }
        }

        private void BtnRemoveLtText_Click(object sender, RoutedEventArgs e)
        {
            string nameToRemove = CbLtText.Text.Trim();
            if (string.IsNullOrWhiteSpace(nameToRemove)) return;

            var list = fileHelper.FiletoList("LowerThird").Select(x => x.Trim()).ToList(); // STRIPS HIDDEN SPACES!

            if (list.RemoveAll(x => x.Equals(nameToRemove, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                if (list.Count == 0) list.Add(" ");
                string listPath = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList\LowerThird.txt");
                System.IO.File.WriteAllText(listPath, string.Join(";", list));
                LoadLowerThirds();
                CbLtText.Text = "";
                MessageBox.Show("Name deleted permanently!");
            }
            else
            {
                MessageBox.Show("Could not find that exact name in the database.", "Not Found");
            }
        }

        private void BtnPreviewLtText_Click(object sender, RoutedEventArgs e)
        {
            string name = CbLtText.Text.Trim(); string role = TxtLtRole.Text.Trim(); if (string.IsNullOrWhiteSpace(name)) return;
            string theme = ((ComboBoxItem)CbLtTheme.SelectedItem).Tag.ToString(); string accent = ((ComboBoxItem)CbLtAccent.SelectedItem).Tag.ToString();
            string font = ((ComboBoxItem)CbLtFont.SelectedItem).Content.ToString(); int design = CbLtDesign.SelectedIndex;

            fileHelper.SaveasHtmlForModernLowerThird(name, role, theme, accent, font, design, "LtPreview");
            MiniPreviewMonitor.CoreWebView2.Reload();
        }

        private void BtnSubmitLtText_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            if (!isLtTextLive)
            {
                string name = CbLtText.Text.Trim(); string role = TxtLtRole.Text.Trim(); if (string.IsNullOrWhiteSpace(name)) return;
                string theme = ((ComboBoxItem)CbLtTheme.SelectedItem).Tag.ToString(); string accent = ((ComboBoxItem)CbLtAccent.SelectedItem).Tag.ToString();
                string font = ((ComboBoxItem)CbLtFont.SelectedItem).Content.ToString(); int design = CbLtDesign.SelectedIndex;

                fileHelper.SaveasHtmlForModernLowerThird(name, role, theme, accent, font, design, "SongLive");
                chromeHelper.RefreshChrome();

                btn.Content = "❌ REMOVE TITLE";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF4757");
                isLtTextLive = true;
            }
            else
            {
                FadeOutLowerThird();
                btn.Content = "🚀 GO LIVE!";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                isLtTextLive = false;
            }

        }


        // --- 2. IMAGES & LOGOS ---

        // The memory list for the new Lower Thirds image queue!
        private System.Collections.ObjectModel.ObservableCollection<GalleryItem> ltImageGalleryItems = new System.Collections.ObjectModel.ObservableCollection<GalleryItem>();

        private void BtnBrowseLtImage_Click(object sender, RoutedEventArgs e)
        {
            // FIX: Multiselect is enabled! You can highlight 5 images at once!
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Images|*.png;*.jpg;*.jpeg", Multiselect = true };
            if (ofd.ShowDialog() == true)
            {
                foreach (string file in ofd.FileNames) AddImageToLtGallery(file);
            }
        }

        private void ImageCard_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void ImageCard_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        AddImageToLtGallery(file);
                    }
                }
            }
        }

        // Helper method that securely loads the image into RAM without locking the file
        private void AddImageToLtGallery(string imgPath)
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imgPath);
            bitmap.EndInit();
            bitmap.Freeze();

            // FIX: If it's the first image added, make it active and Green immediately!
            bool isFirst = ltImageGalleryItems.Count == 0;
            if (isFirst) currentImagePath = imgPath;
            {
                currentImagePath = imgPath;
                // FIX: Instantly preview the very first image you add to an empty list!
                AutoPreviewImage();
            }

            ltImageGalleryItems.Add(new GalleryItem
            {
                FilePath = imgPath,
                ImageSource = bitmap,
                BorderColor = isFirst ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478") : System.Windows.Media.Brushes.Transparent
            });

            if (LtImageGalleryList.ItemsSource == null)
                LtImageGalleryList.ItemsSource = ltImageGalleryItems;
        }



        private void BtnDeleteLtImage_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string imgPath = btn.Tag.ToString();

            var item = ltImageGalleryItems.FirstOrDefault(x => x.FilePath == imgPath);
            if (item != null) ltImageGalleryItems.Remove(item);

            // FIX: If you deleted the active image, auto-select the next available one!
            if (currentImagePath == imgPath)
            {
                currentImagePath = "";
                if (ltImageGalleryItems.Count > 0)
                {
                    currentImagePath = ltImageGalleryItems[0].FilePath;
                    ltImageGalleryItems[0].BorderColor = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                }
            }
        }
        // FIX: Translates standard vertical Mouse-Wheel scrolling into Horizontal scrolling!
        // FIX: Flawless Mouse Wheel Scrolling!
        private void LtImageGalleryList_PreviewMouseWheel (object sender, MouseWheelEventArgs e)
        {
            ScrollViewer sv = sender as ScrollViewer;
            if (sv != null)
            {
                if (e.Delta > 0) sv.LineLeft();
                else sv.LineRight();
                e.Handled = true; // Stops the rest of the app from scrolling
            }
        }

        // FIX: Direct Click Handling. No blue boxes, no glitches!
        private void GalleryImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag != null)
            {
                currentImagePath = clickedBorder.Tag.ToString();

                // Update the Green Border instantly!
                foreach (var item in ltImageGalleryItems)
                {
                    item.BorderColor = item.FilePath == currentImagePath ?
                        (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478") :
                        System.Windows.Media.Brushes.Transparent;
                }
                AutoPreviewImage();

            }
        }

        // FIX: The silent, automatic preview engine!
        private void AutoPreviewImage()
        {
            if (string.IsNullOrEmpty(currentImagePath)) return;
            string safeName = System.IO.Path.GetFileName(currentImagePath).Replace(" ", "_");

            string destFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\images");
            if (!System.IO.Directory.Exists(destFolder)) System.IO.Directory.CreateDirectory(destFolder);
            System.IO.File.Copy(currentImagePath, System.IO.Path.Combine(destFolder, safeName), true);

            string template = chkLtPortrait.IsChecked == true ? "PotraitImage" : "Transprentlogo";
            fileHelper.SaveasHtmlForLowerThird(new HtmlModel() { Lyrics = safeName }, template);

            System.IO.File.Copy(System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\SongLive.html"), System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\LtPreview.html"), true);

            // Force the mini monitor to refresh and show the new image instantly!
            if (MiniPreviewMonitor != null && MiniPreviewMonitor.CoreWebView2 != null)
            {
                MiniPreviewMonitor.CoreWebView2.Reload();
            }
        }
        private void BtnSubmitLtImage_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            if (!isLtImageLive)
            {
                if (string.IsNullOrEmpty(currentImagePath)) return;

                string safeName = System.IO.Path.GetFileName(currentImagePath).Replace(" ", "_");
                string destFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\images");
                if (!System.IO.Directory.Exists(destFolder)) System.IO.Directory.CreateDirectory(destFolder);

                System.IO.File.Copy(currentImagePath, System.IO.Path.Combine(destFolder, safeName), true);

                string template = chkLtPortrait.IsChecked == true ? "PotraitImage" : "Transprentlogo";

                HtmlModel imgModel = new HtmlModel() { Lyrics = safeName };
                fileHelper.SaveasHtmlForLowerThird(imgModel, template);

                chromeHelper.RefreshChrome();

                btn.Content = "❌ REMOVE IMAGE";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF4757");
                isLtImageLive = true;
            }
            else
            {
                // FIX: Forcefully writes a blank green screen to wipe the image!
                string baseDir = FileHelper.GetAppFolder();
                System.IO.File.WriteAllText(System.IO.Path.Combine(baseDir, @"WebTemplate\SongLive.html"), "<html style='background:#00ff00;'></html>");
                chromeHelper.RefreshChrome();

                btn.Content = "🚀 PUSH LIVE";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                isLtImageLive = false;
            }
        }

        // --- 3. VIDEO ANIMATIONS ---
        private void BtnBrowseLtVideo_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "MP4 Videos|*.mp4" };
            if (ofd.ShowDialog() == true)
            {
                string safeName = System.IO.Path.GetFileName(ofd.FileName).Replace(" ", "_");
                string destFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\AnimatedVideoLibrary");
                if (!System.IO.Directory.Exists(destFolder)) System.IO.Directory.CreateDirectory(destFolder);

                string destFile = System.IO.Path.Combine(destFolder, safeName);
                System.IO.File.Copy(ofd.FileName, destFile, true);

                string cleanName = safeName.Replace(".mp4", "");
                var list = fileHelper.FiletoList("AnimatedVideoLibrary");
                if (!list.Contains(cleanName))
                {
                    list.Add(cleanName);
                    string listPath = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList\AnimatedVideoLibrary.txt");
                    System.IO.File.WriteAllText(listPath, string.Join(";", list));
                    LoadLowerThirds();
                }
                CbLtVideo.Text = cleanName;
            }
        }

        private void BtnRemoveLtVideo_Click(object sender, RoutedEventArgs e)
        {
            string nameToRemove = CbLtVideo.Text.Trim();
            string destFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\AnimatedVideoLibrary", nameToRemove + ".mp4");
            if (System.IO.File.Exists(destFile)) System.IO.File.Delete(destFile); // Deletes the MP4

            var list = fileHelper.FiletoList("AnimatedVideoLibrary");
            list.RemoveAll(x => x.Equals(nameToRemove, StringComparison.OrdinalIgnoreCase));
            if (list.Count == 0) list.Add(" ");
            System.IO.File.WriteAllText(System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList\AnimatedVideoLibrary.txt"), string.Join(";", list));
            LoadLowerThirds(); CbLtVideo.Text = ""; MessageBox.Show("Video deleted permanently!");
        }

        // NEW: Video Preview!
        private void BtnPreviewLtVideo_Click(object sender, RoutedEventArgs e)
        {
            string videoName = CbLtVideo.Text.Trim();
            if (string.IsNullOrWhiteSpace(videoName)) return;

            // Build the HTML specifically for the Preview Monitor
            string html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>body {{ margin: 0; background-color: #00ff00; overflow: hidden; }} video {{ width: 100vw; height: 100vh; object-fit: cover; }}</style>
</head>
<body>
    <video autoplay loop muted>
        <source src='Video/{videoName}.mp4' type='video/mp4'>
    </video>
</body>
</html>";

            System.IO.File.WriteAllText(System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\LtPreview.html"), html);
            MiniPreviewMonitor.CoreWebView2.Reload();
        }

        // --- UNIFIED MEDIA CONTROLS ---
        private bool isLtVideoPlaying = true;
        private bool isPreviewVideoPlaying = true;

        private void BtnLtPlayPause_Click(object sender, RoutedEventArgs e)
        {
            // Controls the actual external Live Display
            isLtVideoPlaying = !isLtVideoPlaying;
            if (isLtVideoPlaying)
            {
                BtnLtPlayPause.Content = "⏸ PAUSE LIVE";
                BtnLtPlayPause.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#6C5DD3");
                chromeHelper.ToggleVideoPlayback(true);
            }
            else
            {
                BtnLtPlayPause.Content = "▶ PLAY LIVE";
                BtnLtPlayPause.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                chromeHelper.ToggleVideoPlayback(false);
            }
        }

        private void BtnPreviewPlayPause_Click(object sender, RoutedEventArgs e)
        {
            // Controls the Mini Monitor inside the app!
            isPreviewVideoPlaying = !isPreviewVideoPlaying;
            string script = isPreviewVideoPlaying ? "var v = document.querySelector('video'); if(v) v.play();" : "var v = document.querySelector('video'); if(v) v.pause();";

            if (MiniPreviewMonitor != null && MiniPreviewMonitor.CoreWebView2 != null)
            {
                MiniPreviewMonitor.CoreWebView2.ExecuteScriptAsync(script);
            }

            if (isPreviewVideoPlaying)
            {
                BtnPreviewPlayPause.Content = "⏸ PAUSE PREVIEW";
                BtnPreviewPlayPause.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF9F43");
            }
            else
            {
                BtnPreviewPlayPause.Content = "▶ PLAY PREVIEW";
                BtnPreviewPlayPause.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
            }
        }

        private void VideoSeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Instantly syncs BOTH the Preview Monitor and the Live Display to the exact same frame!
            double percent = e.NewValue;

            if (MiniPreviewMonitor != null && MiniPreviewMonitor.CoreWebView2 != null)
            {
                string script = $"var v = document.querySelector('video'); if(v) v.currentTime = (v.duration * ({percent} / 100));";
                MiniPreviewMonitor.CoreWebView2.ExecuteScriptAsync(script);
            }

            chromeHelper.SeekVideo(percent);
        }

        private void BtnSubmitLtVideo_Click(object sender, RoutedEventArgs e)
        {
            string videoName = CbLtVideo.Text.Trim();
            if (string.IsNullOrWhiteSpace(videoName)) return;

            HtmlModel vidModel = new HtmlModel() { Lyrics = videoName + ".mp4" };
            fileHelper.SaveasHtmlForLowerThird(vidModel, "AnimatedVideoLibrary");
            chromeHelper.RefreshChrome();
        }

        // --- MEDIA CONTROLS (JavaScript Injection!) ---
        private void BtnLtPause_Click(object sender, RoutedEventArgs e)
        {
            chromeHelper.ToggleVideoPlayback(false);
        }

        private void BtnLtPlay_Click(object sender, RoutedEventArgs e)
        {
            chromeHelper.ToggleVideoPlayback(true);
        }

        // ----------------------------------------------------------------
        // BIRTHDAYS & ANNIVERSARIES ENGINE (PERMANENT DATABASE)
        // ----------------------------------------------------------------
        public class EventItem
        {
            public string Name { get; set; }
            public string Date { get; set; }
            [System.Text.Json.Serialization.JsonIgnore]
            public string OriginalName { get; set; } // FIX: Tracks the name so edits overwrite instead of duplicating!
        }

        private System.Collections.ObjectModel.ObservableCollection<EventItem> bdayList = new System.Collections.ObjectModel.ObservableCollection<EventItem>();
        private System.Collections.ObjectModel.ObservableCollection<EventItem> annivList = new System.Collections.ObjectModel.ObservableCollection<EventItem>();

        private string bdayDbPath => System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList\BirthdaysDB.json");
        private string annivDbPath => System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList\AnnivsDB.json");

        private void BtnClearBdays_Click(object sender, RoutedEventArgs e) { bdayList.Clear(); }
        private List<EventItem> loadedBdaysMemory = new List<EventItem>();
        // FIX: Deep-copy backups for the 1-level Undo feature!
        private string backupBdayJson = "";
        private string backupAnnivJson = "";
        private void BtnClearAnnivs_Click(object sender, RoutedEventArgs e) { annivList.Clear(); }
        private List<EventItem> loadedAnnivsMemory = new List<EventItem>();

        // Tracks the Toggle State for removing the graphic smoothly!
        // Tracks the Toggle State
        private bool isBdayLive = false;
        private bool isAnnivLive = false;

        private void BtnSubmitBdays_Click(object sender, RoutedEventArgs e)
        {
            if (bdayList.Count == 0) return;
            Button btn = sender as Button;

            if (!isBdayLive)
            {
                fileHelper.SaveasHtmlForEventList("Birthday", bdayList.Select(b => b.Name).ToList(), bdayList.Select(b => b.Date).ToList(), false);
                chromeHelper.RefreshChrome();
                btn.Content = "❌ REMOVE FROM LIVE";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF4757");
                isBdayLive = true;
            }
            else
            {
                // FIX: Use classList.add so we don't erase the entry class and trigger a hard cut!
                chromeHelper.ExecuteJavascriptOnLive("document.body.classList.add('animate-out');");
                btn.Content = "🚀 PUSH LIVE";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                isBdayLive = false;
            }
            currentlyActiveScreen = "Events";

        }

        private void BtnSubmitAnnivs_Click(object sender, RoutedEventArgs e)
        {
            if (annivList.Count == 0) return;
            Button btn = sender as Button;

            if (!isAnnivLive)
            {
                fileHelper.SaveasHtmlForEventList("Anniversary", annivList.Select(a => a.Name).ToList(), annivList.Select(a => a.Date).ToList(), true);
                chromeHelper.RefreshChrome();
                btn.Content = "❌ REMOVE FROM LIVE";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF4757");
                isAnnivLive = true;
            }
            else
            {
                chromeHelper.ExecuteJavascriptOnLive("document.body.classList.add('animate-out');");
                btn.Content = "🚀 PUSH LIVE";
                btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                isAnnivLive = false;
            }
            currentlyActiveScreen = "Events";

        }

        // --- CUSTOM FONT UPLOADERS ---
        private void BtnUploadTitleFont_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Font Files|*.ttf;*.otf" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string targetFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate");
                    if (!System.IO.Directory.Exists(targetFolder)) System.IO.Directory.CreateDirectory(targetFolder);

                    // Deletes the old font (if it exists) to prevent conflicts, then copies the new one!
                    string ext = System.IO.Path.GetExtension(ofd.FileName);
                    string targetFile = System.IO.Path.Combine(targetFolder, "titlefont" + ext);

                    if (System.IO.File.Exists(System.IO.Path.Combine(targetFolder, "titlefont.ttf"))) System.IO.File.Delete(System.IO.Path.Combine(targetFolder, "titlefont.ttf"));
                    if (System.IO.File.Exists(System.IO.Path.Combine(targetFolder, "titlefont.otf"))) System.IO.File.Delete(System.IO.Path.Combine(targetFolder, "titlefont.otf"));

                    System.IO.File.Copy(ofd.FileName, targetFile, true);
                    MessageBox.Show("Title Font updated successfully! Hit 'Push Live' to see the changes.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error updating font: " + ex.Message, "Error"); }
            }
        }

        private void BtnUploadNameFont_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Font Files|*.ttf;*.otf" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string targetFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate");
                    if (!System.IO.Directory.Exists(targetFolder)) System.IO.Directory.CreateDirectory(targetFolder);

                    string ext = System.IO.Path.GetExtension(ofd.FileName);
                    string targetFile = System.IO.Path.Combine(targetFolder, "namefont" + ext);

                    if (System.IO.File.Exists(System.IO.Path.Combine(targetFolder, "namefont.ttf"))) System.IO.File.Delete(System.IO.Path.Combine(targetFolder, "namefont.ttf"));
                    if (System.IO.File.Exists(System.IO.Path.Combine(targetFolder, "namefont.otf"))) System.IO.File.Delete(System.IO.Path.Combine(targetFolder, "namefont.otf"));

                    System.IO.File.Copy(ofd.FileName, targetFile, true);
                    MessageBox.Show("Name Font updated successfully! Hit 'Push Live' to see the changes.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error updating font: " + ex.Message, "Error"); }
            }
        }

        private bool isEventScrolling = false;

        private void BtnEventScroll_Click(object sender, RoutedEventArgs e)
        {
            isEventScrolling = !isEventScrolling;

            if (isEventScrolling)
            {
                BtnEventScroll.Content = "⏸ PAUSE SCROLL";
                BtnEventScroll.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF9F43");

                double speedVal = 11 - EventScrollSpeed.Value;

                // FIX: Crash-proof Scroll Engine. Calculates using standard JS pixels instead of CSS Matrices!
                string script = $@"
                    var wrap = document.getElementById('scrollWrap');
                    var cont = document.getElementById('listCont');
                    if (wrap.scrollHeight > cont.clientHeight) {{
                        var currentY = wrap.style.transform ? parseFloat(wrap.style.transform.replace(/[^\d.-]/g, '')) : 0;
                        var targetY = -(wrap.scrollHeight - cont.clientHeight);
                        var dist = Math.abs(targetY - currentY);
                        
                        if (dist > 0) {{
                            var time = (dist / 100) * {speedVal}; 
                            wrap.style.transition = 'transform ' + time + 's linear';
                            wrap.style.transform = 'translateY(' + targetY + 'px)';
                        }}
                    }}
                ";
                chromeHelper.ExecuteJavascriptOnLive(script);
            }
            else
            {
                BtnEventScroll.Content = "▶ START SCROLL";
                BtnEventScroll.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2D3047");

                // Freeze by reading the exact pixel location it is currently at!
                chromeHelper.ExecuteJavascriptOnLive(@"
                    var wrap = document.getElementById('scrollWrap'); 
                    var rect = wrap.getBoundingClientRect();
                    var contRect = document.getElementById('listCont').getBoundingClientRect();
                    var currentY = rect.top - contRect.top;
                    wrap.style.transition = 'none'; 
                    wrap.style.transform = 'translateY(' + currentY + 'px)';
                ");
            }
        }

        // --- DATABASE CONTROLS ---
        // --- DATABASE CONTROLS ---
        private bool isShowingThisWeekOnly = false; // Remembers what view you are currently in!

        private void BtnLoadWeek_Click(object sender, RoutedEventArgs e)
        {
            isShowingThisWeekOnly = true;
            LoadDatabaseToUI(true);
        }

        private void BtnLoadAll_Click(object sender, RoutedEventArgs e)
        {
            isShowingThisWeekOnly = false;
            LoadDatabaseToUI(false);
        }

        private void LoadDatabaseToUI(bool thisWeekOnly)
        {
            bdayList.Clear();
            annivList.Clear();

            if (System.IO.File.Exists(bdayDbPath))
            {
                var db = System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(System.IO.File.ReadAllText(bdayDbPath));
                foreach (var item in db)
                {
                    // FIX: Locks the original name into memory so edits overwrite instead of duplicating!
                    item.OriginalName = item.Name;
                    if (!thisWeekOnly || IsEventThisWeek(item.Date)) bdayList.Add(item);
                }
            }

            if (System.IO.File.Exists(annivDbPath))
            {
                var db = System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(System.IO.File.ReadAllText(annivDbPath));
                foreach (var item in db)
                {
                    item.OriginalName = item.Name;
                    if (!thisWeekOnly || IsEventThisWeek(item.Date)) annivList.Add(item);
                }
            }

            // Sync the memory lists for the Search Box!
            loadedBdaysMemory = bdayList.ToList();
            loadedAnnivsMemory = annivList.ToList();

            GridBdays.ItemsSource = bdayList;
            GridAnnivs.ItemsSource = annivList;
        }
        // --- MANUAL ADD BUTTONS ---
        private void BtnAddBday_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtBdayName.Text) && !string.IsNullOrWhiteSpace(TxtBdayDate.Text) && TxtBdayDate.Text != "Date...")
            {
                // Instantly drops it into the editable Grid!
                bdayList.Add(new EventItem { Name = TxtBdayName.Text.Trim(), Date = TxtBdayDate.Text.Trim(), OriginalName = TxtBdayName.Text.Trim() });
                TxtBdayName.Text = "";
                TxtBdayDate.Text = "Date...";
                TxtBdayName.Focus();
            }
        }

        private void BtnAddAnniv_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtAnnivName.Text) && !string.IsNullOrWhiteSpace(TxtAnnivDate.Text) && TxtAnnivDate.Text != "Date...")
            {
                annivList.Add(new EventItem { Name = TxtAnnivName.Text.Trim(), Date = TxtAnnivDate.Text.Trim(), OriginalName = TxtAnnivName.Text.Trim() });
                TxtAnnivName.Text = "";
                TxtAnnivDate.Text = "Date...";
                TxtAnnivName.Focus();
            }
        }

        // --- MANUAL DB EDITING & SEARCHING ---

        // Allows user to hit Enter while typing in search box
        private void EventSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnEventSearch_Click(null, null);
        }

        // Executes the search
        private void BtnEventSearch_Click(object sender, RoutedEventArgs e)
        {
            string query = EventSearchBox.Text.ToLower().Trim();

            if (loadedBdaysMemory == null || loadedBdaysMemory.Count == 0) loadedBdaysMemory = bdayList.ToList();
            if (loadedAnnivsMemory == null || loadedAnnivsMemory.Count == 0) loadedAnnivsMemory = annivList.ToList();

            bdayList.Clear();
            foreach (var b in loadedBdaysMemory.Where(x => string.IsNullOrEmpty(query) || x.Name.ToLower().Contains(query))) bdayList.Add(b);

            annivList.Clear();
            foreach (var a in loadedAnnivsMemory.Where(x => string.IsNullOrEmpty(query) || x.Name.ToLower().Contains(query))) annivList.Add(a);
        }

        // Only shows the Delete button if a row is selected!
        private void GridBdays_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnDelBdayRow.Visibility = GridBdays.SelectedItem != null ? Visibility.Visible : Visibility.Hidden;
        }

        private void GridAnnivs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnDelAnnivRow.Visibility = GridAnnivs.SelectedItem != null ? Visibility.Visible : Visibility.Hidden;
        }

        private void BtnDeleteBdayRow_Click(object sender, RoutedEventArgs e)
        {
            if (GridBdays.SelectedItem is EventItem selectedItem)
            {
                // FIX: Take a snapshot BEFORE deleting so Undo actually works!
                if (System.IO.File.Exists(bdayDbPath)) backupBdayJson = System.IO.File.ReadAllText(bdayDbPath);

                bdayList.Remove(selectedItem);
                if (loadedBdaysMemory.Contains(selectedItem)) loadedBdaysMemory.Remove(selectedItem);

                // Permanently delete from JSON
                if (System.IO.File.Exists(bdayDbPath))
                {
                    var db = System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(System.IO.File.ReadAllText(bdayDbPath));
                    db.RemoveAll(x => x.Name.Equals(selectedItem.Name, StringComparison.OrdinalIgnoreCase));
                    System.IO.File.WriteAllText(bdayDbPath, System.Text.Json.JsonSerializer.Serialize(db));
                }
            }
        }

        private void BtnDeleteAnnivRow_Click(object sender, RoutedEventArgs e)
        {
            if (GridAnnivs.SelectedItem is EventItem selectedItem)
            {
                // FIX: Take a snapshot BEFORE deleting!
                if (System.IO.File.Exists(annivDbPath)) backupAnnivJson = System.IO.File.ReadAllText(annivDbPath);

                annivList.Remove(selectedItem);
                if (loadedAnnivsMemory.Contains(selectedItem)) loadedAnnivsMemory.Remove(selectedItem);

                if (System.IO.File.Exists(annivDbPath))
                {
                    var db = System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(System.IO.File.ReadAllText(annivDbPath));
                    db.RemoveAll(x => x.Name.Equals(selectedItem.Name, StringComparison.OrdinalIgnoreCase));
                    System.IO.File.WriteAllText(annivDbPath, System.Text.Json.JsonSerializer.Serialize(db));
                }
            }
        }

        private void BtnSaveBdayDb_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(bdayDbPath)) backupBdayJson = System.IO.File.ReadAllText(bdayDbPath);
            var db = string.IsNullOrEmpty(backupBdayJson) ? new List<EventItem>() : System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(backupBdayJson);

            foreach (var b in bdayList)
            {
                if (string.IsNullOrWhiteSpace(b.Name)) continue;

                string searchName = string.IsNullOrEmpty(b.OriginalName) ? b.Name : b.OriginalName;
                var existing = db.FirstOrDefault(x => x.Name.Equals(searchName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Name = b.Name;
                    existing.Date = b.Date;
                }
                else db.Add(b);

                b.OriginalName = b.Name;
            }

            db = db.OrderBy(x => ParseDateForSorting(x.Date)).ThenBy(x => x.Name).ToList();
            System.IO.File.WriteAllText(bdayDbPath, System.Text.Json.JsonSerializer.Serialize(db));
            LoadDatabaseToUI(isShowingThisWeekOnly);
            MessageBox.Show("Birthdays updated and sorted!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaveAnnivDb_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(annivDbPath)) backupAnnivJson = System.IO.File.ReadAllText(annivDbPath);
            var db = string.IsNullOrEmpty(backupAnnivJson) ? new List<EventItem>() : System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(backupAnnivJson);

            foreach (var a in annivList)
            {
                if (string.IsNullOrWhiteSpace(a.Name)) continue;

                string searchName = string.IsNullOrEmpty(a.OriginalName) ? a.Name : a.OriginalName;
                var existing = db.FirstOrDefault(x => x.Name.Equals(searchName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Name = a.Name;
                    existing.Date = a.Date;
                }
                else db.Add(a);

                a.OriginalName = a.Name;
            }

            db = db.OrderBy(x => ParseDateForSorting(x.Date)).ThenBy(x => x.Name).ToList();
            System.IO.File.WriteAllText(annivDbPath, System.Text.Json.JsonSerializer.Serialize(db));
            LoadDatabaseToUI(isShowingThisWeekOnly);
            MessageBox.Show("Anniversaries updated and sorted!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Helper method to convert "26th Apr" into a math-friendly format for flawless sorting
        // Helper method to convert "26th Apr" into a math-friendly format for flawless sorting
        private DateTime ParseDateForSorting(string dateStr)
        {
            try
            {
                // FIX: RemoveEmptyEntries completely ignores accidental double spaces from Excel!
                string[] parts = dateStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return DateTime.MaxValue;

                int day = int.Parse(new string(parts[0].Where(char.IsDigit).ToArray()));
                string monthStr = parts[1].Trim();

                string[] monthNames = { "", "Jan", "Feb", "March", "Apr", "May", "June", "July", "Aug", "Sep", "Oct", "Nov", "Dec" };
                int month = Array.IndexOf(monthNames, monthStr);

                // Extra failsafe just in case there are hidden invisible characters attached to the month
                if (month < 1)
                {
                    for (int i = 1; i < monthNames.Length; i++)
                    {
                        if (monthStr.Contains(monthNames[i])) { month = i; break; }
                    }
                }

                if (month < 1) month = 1;

                // Uses Year 2000 so the sort is purely based on Month/Day
                return new DateTime(2000, month, day);
            }
            catch { return DateTime.MaxValue; } // Pushes genuinely bad dates to the bottom
        }
        private bool IsEventThisWeek(string formattedDate)
        {
            try
            {
                string[] parts = formattedDate.Split(' ');
                if (parts.Length < 2) return true;

                int day = int.Parse(new string(parts[0].Where(char.IsDigit).ToArray()));
                string[] monthNames = { "", "Jan", "Feb", "March", "Apr", "May", "June", "July", "Aug", "Sep", "Oct", "Nov", "Dec" };
                int month = Array.IndexOf(monthNames, parts[1]);
                if (month < 1) return true;

                DateTime eventDate = new DateTime(DateTime.Now.Year, month, day);
                DateTime today = DateTime.Today;

                int diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
                DateTime startOfWeek = today.AddDays(-1 * diff).Date;
                DateTime endOfWeek = startOfWeek.AddDays(6).Date;

                return eventDate >= startOfWeek && eventDate <= endOfWeek;
            }
            catch { return true; }
        }

        // --- 1-CLICK EXCEL IMPORTER ---
        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Excel Files|*.xlsx;*.xls" };
            if (ofd.ShowDialog() != true) return;

            string filePath = ofd.FileName;
            List<EventItem> tempBdays = new List<EventItem>();
            List<EventItem> tempAnnivs = new List<EventItem>();
            string sheetName = "";

            try
            {
                // ==========================================================
                // MODERN EXCEL (.xlsx)
                // ==========================================================
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    using (var workbook = new ClosedXML.Excel.XLWorkbook(filePath))
                    {
                        var sheet = workbook.Worksheets.FirstOrDefault(ws => ws.TabActive) ?? workbook.Worksheet(1);
                        sheetName = sheet.Name;
                        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
                        int dateCol = 1; int nameCol = 2;

                        for (int i = 1; i <= Math.Min(20, lastRow); i++)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(sheet.Cell(i, 1).GetString().Trim(), @"\d{2}/\d{2}/\*+")) { dateCol = 1; nameCol = 2; break; }
                            if (System.Text.RegularExpressions.Regex.IsMatch(sheet.Cell(i, 2).GetString().Trim(), @"\d{2}/\d{2}/\*+")) { dateCol = 2; nameCol = 3; break; }
                        }

                        HashSet<int> processedRows = new HashSet<int>();

                        for (int r = 1; r <= lastRow; r++)
                        {
                            string cellName = sheet.Cell(r, nameCol).GetString().Trim();
                            if (cellName == "&")
                            {
                                if (r >= 3 && r + 1 <= lastRow)
                                {
                                    string husband = sheet.Cell(r - 2, nameCol).GetString().Trim();
                                    string rawDate = sheet.Cell(r, dateCol).GetString().Trim();
                                    string wife = sheet.Cell(r + 1, nameCol).GetString().Trim();

                                    if (!string.IsNullOrWhiteSpace(husband) && !string.IsNullOrWhiteSpace(wife))
                                    {
                                        string fullName = $"{husband} & {wife}";
                                        // FIX: Memorize OriginalName immediately on import!
                                        tempAnnivs.Add(new EventItem { Name = fullName, OriginalName = fullName, Date = FormatCustomDate(rawDate) });
                                        for (int i = r - 2; i <= r + 2; i++) processedRows.Add(i);
                                    }
                                }
                            }
                        }

                        for (int r = 1; r <= lastRow; r++)
                        {
                            if (processedRows.Contains(r)) continue;

                            string cellDate = sheet.Cell(r, dateCol).GetString().Trim();
                            if (System.Text.RegularExpressions.Regex.IsMatch(cellDate, @"\d{2}/\d{2}/\*+"))
                            {
                                if (r + 1 <= lastRow)
                                {
                                    string bdayName = sheet.Cell(r + 1, nameCol).GetString().Trim();
                                    if (!string.IsNullOrWhiteSpace(bdayName))
                                    {
                                        // FIX: Memorize OriginalName immediately on import!
                                        tempBdays.Add(new EventItem { Name = bdayName, OriginalName = bdayName, Date = FormatCustomDate(cellDate) });
                                        processedRows.Add(r);
                                        processedRows.Add(r + 1);
                                    }
                                }
                            }
                        }
                    }
                }
                // ==========================================================
                // ANCIENT EXCEL (.xls)
                // ==========================================================
                else
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using (var stream = System.IO.File.Open(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();

                        int selectedSheetIndex = 0;
                        if (result.Tables.Count > 1)
                        {
                            List<string> sheetNames = new List<string>();
                            foreach (System.Data.DataTable table in result.Tables) sheetNames.Add(table.TableName);

                            int? userChoice = ShowSheetSelectionDialog(sheetNames);
                            if (userChoice == null) return;
                            selectedSheetIndex = userChoice.Value;
                        }

                        var sheet = result.Tables[selectedSheetIndex];
                        sheetName = sheet.TableName;

                        int dateCol = 0; int nameCol = 1;
                        for (int i = 0; i < Math.Min(20, sheet.Rows.Count); i++)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(Convert.ToString(sheet.Rows[i][0]), @"\d{2}/\d{2}/\*+")) { dateCol = 0; nameCol = 1; break; }
                            if (sheet.Columns.Count > 1 && System.Text.RegularExpressions.Regex.IsMatch(Convert.ToString(sheet.Rows[i][1]), @"\d{2}/\d{2}/\*+")) { dateCol = 1; nameCol = 2; break; }
                        }

                        HashSet<int> processedRows = new HashSet<int>();

                        for (int r = 0; r < sheet.Rows.Count; r++)
                        {
                            string cellName = Convert.ToString(sheet.Rows[r][nameCol]).Trim();
                            if (cellName == "&")
                            {
                                if (r >= 2 && r + 1 < sheet.Rows.Count)
                                {
                                    string husband = Convert.ToString(sheet.Rows[r - 2][nameCol]).Trim();
                                    string rawDate = Convert.ToString(sheet.Rows[r][dateCol]).Trim();
                                    string wife = Convert.ToString(sheet.Rows[r + 1][nameCol]).Trim();

                                    if (!string.IsNullOrWhiteSpace(husband) && !string.IsNullOrWhiteSpace(wife))
                                    {
                                        string fullName = $"{husband} & {wife}";
                                        // FIX: Memorize OriginalName
                                        tempAnnivs.Add(new EventItem { Name = fullName, OriginalName = fullName, Date = FormatCustomDate(rawDate) });
                                        for (int i = r - 2; i <= r + 2; i++) processedRows.Add(i);
                                    }
                                }
                            }
                        }

                        for (int r = 0; r < sheet.Rows.Count; r++)
                        {
                            if (processedRows.Contains(r)) continue;

                            string cellDate = Convert.ToString(sheet.Rows[r][dateCol]).Trim();
                            if (System.Text.RegularExpressions.Regex.IsMatch(cellDate, @"\d{2}/\d{2}/\*+"))
                            {
                                if (r + 1 < sheet.Rows.Count)
                                {
                                    string bdayName = Convert.ToString(sheet.Rows[r + 1][nameCol]).Trim();
                                    if (!string.IsNullOrWhiteSpace(bdayName))
                                    {
                                        // FIX: Memorize OriginalName
                                        tempBdays.Add(new EventItem { Name = bdayName, OriginalName = bdayName, Date = FormatCustomDate(cellDate) });
                                        processedRows.Add(r);
                                        processedRows.Add(r + 1);
                                    }
                                }
                            }
                        }
                    }
                }

                // ==========================================================
                // SAVE TO PERMANENT DATABASE (WITH SMART UPDATING!)
                // ==========================================================
                var finalBdays = System.IO.File.Exists(bdayDbPath) ? System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(System.IO.File.ReadAllText(bdayDbPath)) : new List<EventItem>();
                var finalAnnivs = System.IO.File.Exists(annivDbPath) ? System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(System.IO.File.ReadAllText(annivDbPath)) : new List<EventItem>();

                foreach (var b in tempBdays)
                {
                    var existing = finalBdays.FirstOrDefault(x => x.Name.Equals(b.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) existing.Date = b.Date;
                    else finalBdays.Add(b);
                }

                foreach (var a in tempAnnivs)
                {
                    var existing = finalAnnivs.FirstOrDefault(x => x.Name.Equals(a.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) existing.Date = a.Date;
                    else finalAnnivs.Add(a);
                }

                System.IO.File.WriteAllText(bdayDbPath, System.Text.Json.JsonSerializer.Serialize(finalBdays));
                System.IO.File.WriteAllText(annivDbPath, System.Text.Json.JsonSerializer.Serialize(finalAnnivs));

                // Instantly load this week!
                isShowingThisWeekOnly = true;
                LoadDatabaseToUI(true);

                MessageBox.Show($"Imported {tempBdays.Count} Birthdays and {tempAnnivs.Count} Anniversaries from '{sheetName}'!\n\nThe Master Database has been permanently updated.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading Excel file. Make sure it is closed in Excel!\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sleek Dark-Mode Dialog for Ancient .xls files
        private int? ShowSheetSelectionDialog(List<string> sheetNames)
        {
            Window prompt = new Window() { Width = 350, Height = 200, Title = "Select Sheet", WindowStartupLocation = WindowStartupLocation.CenterScreen, Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#0F111A"), Foreground = Brushes.White, WindowStyle = WindowStyle.ToolWindow };
            StackPanel panel = new StackPanel() { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock() { Text = "This .xls file has multiple tabs.\nWhich one do you want to import from?", Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });

            ComboBox cbSheets = new ComboBox() { FontSize = 14, Padding = new Thickness(5), Background = Brushes.White, Foreground = Brushes.Black };
            foreach (string name in sheetNames) cbSheets.Items.Add(name);
            cbSheets.SelectedIndex = 0;
            panel.Children.Add(cbSheets);

            Button btn = new Button() { Content = "IMPORT", Height = 35, Margin = new Thickness(0, 20, 0, 0), Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#6C5DD3"), Foreground = Brushes.White, Cursor = Cursors.Hand, BorderThickness = new Thickness(0) };
            btn.Click += (s, e) => prompt.DialogResult = true;
            panel.Children.Add(btn);

            prompt.Content = panel;

            return prompt.ShowDialog() == true ? cbSheets.SelectedIndex : (int?)null;
        }

        private string FormatCustomDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return "";
            try
            {
                string[] parts = dateStr.Split('/');
                if (parts.Length < 2) return dateStr;
                int day = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);
                string suffix = "th";
                if (day % 10 == 1 && day != 11) suffix = "st";
                else if (day % 10 == 2 && day != 12) suffix = "nd";
                else if (day % 10 == 3 && day != 13) suffix = "rd";
                string[] monthNames = { "", "Jan", "Feb", "March", "Apr", "May", "June", "July", "Aug", "Sep", "Oct", "Nov", "Dec" };
                return $"{day}{suffix} {monthNames[month]}";
            }
            catch { return dateStr; }
        }

        // ----------------------------------------------------------------
        // MOBILE REMOTE SERVER
        // ----------------------------------------------------------------
        // ----------------------------------------------------------------
        // MOBILE REMOTE SERVER
        // ----------------------------------------------------------------
        private void BtnToggleServer_Click(object sender, RoutedEventArgs e)
        {
            if (!RemoteServer.IsRunning)
            {
                try
                {
                    RemoteServer.Start();
                    BtnToggleServer.Content = "🛑 STOP SERVER";
                    BtnToggleServer.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF4757");
                    ServerUrlBox.Text = RemoteServer.ServerUrl;

                    // --- NEW: OFFLINE QR CODE GENERATOR ---
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(RemoteServer.ServerUrl, QRCodeGenerator.ECCLevel.Q);

                    // We use BitmapByteQRCode to avoid WPF/WinForms image clashing!
                    BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData);
                    byte[] qrBytes = qrCode.GetGraphic(20);

                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(qrBytes))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Optimize for WPF

                        QrCodeImage.Source = bitmap;
                    }
                    // --------------------------------------

                    ServerInfoPanel.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not start the server. Make sure port 8080 is not in use by another app.\n\nError: " + ex.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                RemoteServer.Stop();
                BtnToggleServer.Content = "🚀 START SERVER";
                BtnToggleServer.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#00B478");
                ServerInfoPanel.Visibility = Visibility.Collapsed;
            }
        }
        // ----------------------------------------------------------------
        // SETTINGS ENGINE
        // ----------------------------------------------------------------
        private void BtnUploadThemeVideo_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "MP4 Videos|*.mp4" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string targetFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\Video");
                    if (!System.IO.Directory.Exists(targetFolder)) System.IO.Directory.CreateDirectory(targetFolder);

                    string targetFile = System.IO.Path.Combine(targetFolder, "DefaultTheme.mp4");

                    // Overwrite the existing default theme video
                    System.IO.File.Copy(ofd.FileName, targetFile, true);

                    // Reload the background instantly if it's currently showing!
                    CreateDefaultThemeHtml();
                    chromeHelper.RefreshChrome();

                    MessageBox.Show("Theme Video updated successfully!\nIt will now play automatically when you open the Live Display.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error updating theme video: " + ex.Message, "Error"); }
            }
        }

        private void BtnRestoreBlackTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string targetFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\Video\DefaultTheme.mp4");
                if (System.IO.File.Exists(targetFile)) System.IO.File.Delete(targetFile);

                // Forces it to fall back to the black background color
                CreateDefaultThemeHtml();
                chromeHelper.RefreshChrome();

                MessageBox.Show("Restored to Default Black Background.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }
        // --- NEW FEATURES LOGIC ---
        private void MainContent_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Auto-hides menu when you click the dashboard!
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        // Opens the native Windows Color Wheel!
        // Explicitly forces System.Drawing for the conversion so WPF doesn't get confused!
        // Opens the native Windows Color Wheel!
        private void BtnPickGrad1_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string hex = System.Drawing.ColorTranslator.ToHtml(cd.Color);
                BtnGrad1.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(hex);
                BtnGrad1.Tag = hex;
            }
        }

        private void BtnPickGrad2_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string hex = System.Drawing.ColorTranslator.ToHtml(cd.Color);
                BtnGrad2.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(hex);
                BtnGrad2.Tag = hex;
            }
        }

        // FIX: The Save Button actually writes to the hard drive now!
        private void BtnSaveGradient_Click(object sender, RoutedEventArgs e)
        {
            string g1 = BtnGrad1.Tag?.ToString() ?? "#0f2027";
            string g2 = BtnGrad2.Tag?.ToString() ?? "#2c5364";
            System.IO.File.WriteAllText(System.IO.Path.Combine(FileHelper.GetAppFolder(), "gradient_memory.txt"), $"{g1}|{g2}");

            // Instantly refresh the screen if a song is playing!
            if (FinalHtmlModels != null && FinalHtmlModels.Count > 0) ChoosLyrics();

            MessageBox.Show("Presentation Gradient Saved! The app will remember this forever.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        // --- CLOUD SYNC SETTINGS ---

        // FIX: Saves your Animation preferences to memory so it doesn't throw a build error!
        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string animB = chkAnimBible.IsChecked == true ? "1" : "0";
            string animS = chkAnimSong.IsChecked == true ? "1" : "0";
            string speedB = BibleAnimSpeed.Value.ToString();
            string speedS = SongAnimSpeed.Value.ToString();

            // Only saves the 4 animation settings!
            System.IO.File.WriteAllText(System.IO.Path.Combine(FileHelper.GetAppFolder(), "anim_settings.txt"), $"{animB}|{animS}|{speedB}|{speedS}");
            MessageBox.Show("Settings Saved Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ----------------------------------------------------------------
        // SMART SONG SEARCH ENGINE
        // ----------------------------------------------------------------
        // FIX: Instantly shows the suggestion list when you click inside the empty search box!
        private void SongSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SongSearchBox.Text) && songDatabaseMemory != null && songDatabaseMemory.Count > 0)
            {
                SongSuggestionList.ItemsSource = songDatabaseMemory;
                SongSuggestionPopup.IsOpen = true;
            }
        }
        private void SongSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSongLoading) return;

            string query = SongSearchBox.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(query))
            {
                SongSuggestionPopup.IsOpen = false;
                return;
            }

            // FIX: Failsafe! If memory is empty, reload the list from the hard drive instantly!
            if (songDatabaseMemory == null || songDatabaseMemory.Count == 0)
            {
                songDatabaseMemory = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
            }

            var matches = songDatabaseMemory.Where(x => x.ToLower().Contains(query)).ToList();

            if (matches.Count > 0)
            {
                SongSuggestionList.ItemsSource = matches;
                SongSuggestionPopup.IsOpen = true;
            }
            else
            {
                SongSuggestionPopup.IsOpen = false;
            }
        }
        // Passes Arrow Keys from the TextBox into the Suggestion List!
        private void SongSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (SongSuggestionPopup.IsOpen && e.Key == Key.Down)
            {
                SongSuggestionList.Focus();
                SongSuggestionList.SelectedIndex = 0; // Highlights the first song automatically
                e.Handled = true;
            }
        }

        // Handles Hitting Enter inside the Suggestion List
        // Handles Hitting Enter inside the Suggestion List
        private void SongSuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SongSuggestionList.SelectedItem != null)
            {
                LoadSongIntoEditor(SongSuggestionList.SelectedItem.ToString());
                e.Handled = true;
            }
        }

        // NEW: Handles physically clicking the item with your mouse!
        private void SongSuggestionList_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(SongSuggestionList, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null && item.DataContext != null)
            {
                LoadSongIntoEditor(item.DataContext.ToString());
                e.Handled = true;
            }
        }

        // Handles Mouse Clicking inside the Suggestion List

        // The Master Loader
        private void LoadSongIntoEditor(string safeFileName)
        {
            isSongLoading = true; // Lock the text changed event!

            SongSearchBox.Text = safeFileName;
            SongSuggestionPopup.IsOpen = false; // Close the menu
            SongSuggestionList.SelectedIndex = -1;

            string baseDir = FileHelper.GetAppFolder();
            string fullPath = System.IO.Path.Combine(baseDir, $@"WebTemplate\SongDataBase\{safeFileName}.txt");

            if (System.IO.File.Exists(fullPath))
            {
                string rawText = System.IO.File.ReadAllText(fullPath).Replace("\r\n", "\n").Replace("\r", "\n");

                // NEW: Splits the file if it has our secret Tanglish separator!
                string[] parts = rawText.Split(new string[] { "\n===TANGLISH===\n" }, StringSplitOptions.None);

                LyricsInputBox.Text = parts[0].Replace("\n", "\r\n");

                if (parts.Length > 1)
                {
                    SecondaryLyricsBox.Text = parts[1].Replace("\n", "\r\n");
                    chkDualEditor.IsChecked = true;
                }
                else
                {
                    SecondaryLyricsBox.Text = "";
                    chkDualEditor.IsChecked = false;
                }
                ChkDualEditor_Click(null, null); // Applies the UI layout
            }
            else LyricsInputBox.Text = string.Empty;

            isSongLoading = false; // Unlock
        }
        // Deletes a song from the Setlist
        private void BtnRemoveFromSetlist_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string songToRemove = btn.Tag.ToString();
            SetlistBox.Items.Remove(songToRemove);
            if (SetlistBox.Items.Count == 0) LblSetlistEmpty.Visibility = Visibility.Visible;
            e.Handled = true; // Prevents the ListBox from accidentally selecting the item!
        }

        // Applies custom scroll sensitivity to the Chapter List!

        // Helper to find the hidden ScrollViewer inside the ListBox

        public class GalleryItem : System.ComponentModel.INotifyPropertyChanged
        {
            public string FilePath { get; set; }

            // NEW: Holds the unlocked image!
            public System.Windows.Media.ImageSource ImageSource { get; set; }

            private Brush _borderColor = Brushes.Transparent;
            public Brush BorderColor { get { return _borderColor; } set { _borderColor = value; OnPropertyChanged("BorderColor"); } }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
        // FIX: The Safe Database Purger! Only deletes files that have 0 text inside them.
        // FIX: Purges the physical files AND permanently removes their names from the Dropdown list!
        private void BtnPurgeEmptySongs_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirm = MessageBox.Show("This will scan your entire Song Database and permanently delete any text files that are completely empty. Are you sure?", "Confirm Purge", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            string dbFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\SongDataBase");
            string listPath = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\DropDownList");

            if (!System.IO.Directory.Exists(dbFolder)) return;

            int deletedCount = 0;
            string[] allFiles = System.IO.Directory.GetFiles(dbFolder, "*.txt");

            // We will collect the names of the deleted files to remove them from memory later
            List<string> deletedNames = new List<string>();

            // 1. Destroy the empty physical files
            foreach (string file in allFiles)
            {
                try
                {
                    string content = System.IO.File.ReadAllText(file).Trim();

                    if (string.IsNullOrEmpty(content))
                    {
                        string songName = System.IO.Path.GetFileNameWithoutExtension(file);
                        deletedNames.Add(songName); // Mark it for removal from the list!

                        System.IO.File.Delete(file);
                        deletedCount++;
                    }
                }
                catch { } // Silently skip locked files
            }

            // 2. Scrub the names from the Master Dropdown List!
            if (deletedNames.Count > 0 && System.IO.File.Exists(listPath))
            {
                try
                {
                    var songs = System.IO.File.ReadAllText(listPath).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList();

                    // Forcefully remove every name we just deleted
                    songs.RemoveAll(s => deletedNames.Any(d => d.Equals(s, StringComparison.OrdinalIgnoreCase)));

                    if (songs.Count == 0) songs.Add(" "); // Failsafe
                    System.IO.File.WriteAllText(listPath, string.Join(";", songs));

                    // 3. Instantly refresh the UI Dropdown Memory!
                    songDatabaseMemory = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
                }
                catch { }
            }

            MessageBox.Show($"Purge Complete! Safely deleted {deletedCount} empty ghost files and completely scrubbed them from your Search List.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        // ----------------------------------------------------------------
        // CUSTOM DATE PICKER ENGINE
        // ----------------------------------------------------------------
        private TextBox activeDateBox; // Remembers which box you clicked (Bday or Anniv)

        private void LoadDatePickerLists()
        {
            for (int i = 1; i <= 31; i++) ListDays.Items.Add(i.ToString());
            string[] months = { "Jan", "Feb", "March", "Apr", "May", "June", "July", "Aug", "Sep", "Oct", "Nov", "Dec" };
            foreach (string m in months) ListMonths.Items.Add(m);
        }

        private void TxtBdayDate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            activeDateBox = TxtBdayDate;
            DateSelectorPopup.IsOpen = true;
            e.Handled = true;
        }

        private void TxtAnnivDate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            activeDateBox = TxtAnnivDate;
            DateSelectorPopup.IsOpen = true;
            e.Handled = true;
        }

        private void DateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Auto-highlights so you can see what you clicked
            if (sender is ListBox lb && lb.SelectedItem != null) lb.ScrollIntoView(lb.SelectedItem);
        }

        private void BtnApplyDate_Click(object sender, RoutedEventArgs e)
        {
            if (ListDays.SelectedItem == null || ListMonths.SelectedItem == null)
            {
                MessageBox.Show("Please select both a Day and a Month!", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int day = int.Parse(ListDays.SelectedItem.ToString());
            string month = ListMonths.SelectedItem.ToString();

            // Calculate the exact suffix math!
            string suffix = "th";
            if (day % 10 == 1 && day != 11) suffix = "st";
            else if (day % 10 == 2 && day != 12) suffix = "nd";
            else if (day % 10 == 3 && day != 13) suffix = "rd";

            string formattedDate = $"{day}{suffix} {month}";

            if (activeDateBox != null)
            {
                activeDateBox.Text = formattedDate;
            }

            DateSelectorPopup.IsOpen = false; // Close the menu!
        }
        // Helper class for the Language Reorder List
        public class BibleLangItem
        {
            public string DisplayName { get; set; }
            public string CodeName { get; set; }
            public bool IsActive { get; set; }
        }
        // Helper to find hidden elements inside WPF controls (like ScrollViewers inside ListBoxes)
        public static T GetDescendantByType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = (child as T) ?? GetDescendantByType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
        // Universal Drag-to-Scroll for any ScrollViewer (like on your phone!)
        
    }
}