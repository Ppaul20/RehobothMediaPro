using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ExcelDataReader;
using RehobothMediaPro.Helpers;
using RehobothMediaPro.Models;

namespace RehobothMediaPro
{
    public partial class MainWindow : Window
    {
        // THE 42 RULE VARIABLES
        public List<Button> SongLyricsbuttons;
        public int songCounter = 0;
        private readonly string memGreen = System.IO.Path.Combine(RehobothMediaPro.Helpers.FileHelper.GetAppFolder(), "font_green.txt");
        private readonly string memPres = System.IO.Path.Combine(RehobothMediaPro.Helpers.FileHelper.GetAppFolder(), "font_presentation.txt");
        // THE ENGINE VARIABLES
        public ChromeHelper chromeHelper = new ChromeHelper();
        public FileHelper fileHelper = new FileHelper();
        public List<HtmlModel> FinalHtmlModels;
        public string templateName = "song";
        public MainWindow()
        {
            InitializeComponent();
            InitializationOfbuttons();
            SongDropdownList.ItemsSource = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();

            // FIX: Load the correct memory file when the app starts! (Defaults to Green Screen)
            if (System.IO.File.Exists(memGreen)) FontSizeBox.Text = System.IO.File.ReadAllText(memGreen);
            else FontSizeBox.Text = "45"; // Default if no memory file exists
            LoadBibleDropdowns();
            LoadLowerThirds(); // Populates Pastor Names and MP4 Lists!
            InitMiniMonitor();

            // Load Gradient Memory
            string gradFile = System.IO.Path.Combine(FileHelper.GetAppFolder(), "gradient_memory.txt");
            if (System.IO.File.Exists(gradFile))
            {
                string[] parts = System.IO.File.ReadAllText(gradFile).Split('|');
                if (parts.Length == 2) { TxtGrad1.Text = parts[0]; TxtGrad2.Text = parts[1]; }
            }
        }

        // Generate the 42 Buttons with perfect rounded corners!
        private void InitializationOfbuttons()
        {
            SongLyricsbuttons = new List<Button>();
            LyricsButtonGrid.Children.Clear(); // Empties the visual grid

            for (int i = 0; i < 40; i++)
            {
                Button btn = new Button();
                btn.Margin = new Thickness(6); // Perfect spacing
                btn.Background = (Brush)new BrushConverter().ConvertFrom("#2D3047");
                btn.Foreground = Brushes.White;
                btn.FontSize = 13;
                btn.FontWeight = FontWeights.Bold;
                btn.BorderThickness = new Thickness(0);
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
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LyricsInputBox.Text)) return;
            if (SongLyricsbuttons == null || SongLyricsbuttons.Count == 0) InitializationOfbuttons();

            FinalHtmlModels = new List<HtmlModel>();

            // Hide all buttons first to clear the grid
            foreach (var btn in SongLyricsbuttons) btn.Visibility = Visibility.Hidden;

            string cleanText = LyricsInputBox.Text.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] stanzas = cleanText.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            int buttonIndex = 0;
            bool isBlue = true;

            // Set the correct template for the Live Display
            templateName = chkPresentation.IsChecked == true ? "song3" : "song";
            string fontSizePx = FontSizeBox.Text + "px";

            foreach (string stanza in stanzas)
            {
                // BULLETPROOF LIMIT: Automatically stops exactly when the grid is full!
                if (buttonIndex >= SongLyricsbuttons.Count) break;

                Brush baseColor = chkPresentation.IsChecked == true ?
                    (Brush)new BrushConverter().ConvertFrom("#6C5DD3") :
                    (isBlue ? (Brush)new BrushConverter().ConvertFrom("#0085FF") : (Brush)new BrushConverter().ConvertFrom("#2D3047"));

                if (chkPresentation.IsChecked == true)
                {
                    SetupLyricButton(buttonIndex, stanza.Trim(), baseColor);
                    FinalHtmlModels.Add(new HtmlModel() { Lyrics = stanza.Trim().Replace("\n", "<br/>"), FontSize = fontSizePx });
                    buttonIndex++;
                }
                else
                {
                    string[] lines = stanza.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        // BULLETPROOF LIMIT: Checks again for multi-line songs!
                        if (buttonIndex >= SongLyricsbuttons.Count) break;

                        SetupLyricButton(buttonIndex, line.Trim(), baseColor);
                        FinalHtmlModels.Add(new HtmlModel() { Lyrics = line.Trim(), FontSize = fontSizePx });
                        buttonIndex++;
                    }
                }
                isBlue = !isBlue;
            }
        }

        private void SetupLyricButton(int index, string text, Brush bgColor)
        {
            Button btn = SongLyricsbuttons[index];
            btn.Content = new TextBlock()
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4),
                FontSize = 13
            };
            btn.Background = bgColor;
            btn.Uid = new BrushConverter().ConvertToString(bgColor); // FIX: Stores color safely here!
            // Do NOT touch btn.Tag here, it holds your number!
            btn.Visibility = Visibility.Visible;
        }
        // ----------------------------------------------------------------
        // THE 42 RULE: CORE ENGINE LOGIC
        // ----------------------------------------------------------------

        private void BtnOpenLive_Click(object sender, RoutedEventArgs e)
        {
            // Instantly creates the Theme/Logo background HTML!
            CreateDefaultThemeHtml();

            // Opens the window. Because we created Theme.html above, it will show the video instead of a green screen!
            bool isDual = chkDualDisplay != null && chkDualDisplay.IsChecked == true;
            chromeHelper.OpenChrome(isDual);
        }

        private void BtnThemeLogo_Click(object sender, RoutedEventArgs e)
        {
            // Writes the Theme HTML and forces the current Live Display to refresh to it!
            CreateDefaultThemeHtml();
            chromeHelper.RefreshChrome();
        }

        private async void BtnGreenScreen_Click(object sender, RoutedEventArgs e)
        {
            // 1. Send Javascript to beautifully fade out whatever is on the screen!
            chromeHelper.ExecuteJavascriptOnLive("document.body.style.transition = 'opacity 0.5s ease'; document.body.style.opacity = '0';");

            // 2. Wait exactly half a second for the fade to finish
            await System.Threading.Tasks.Task.Delay(500);

            // 3. Now safely write the blank green screen
            string baseDir = FileHelper.GetAppFolder();
            System.IO.File.WriteAllText(System.IO.Path.Combine(baseDir, @"WebTemplate\SongLive.html"), "<html style='background:#00ff00;'></html>");
            chromeHelper.RefreshChrome();

            // Resets all the toggle buttons back to green!
            if (isLtTextLive) BtnSubmitLtText_Click(BtnSubmitLtText, null);
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
        }
        // FIX: Smart Arrow Keys (WITH 42 RULE VERTICAL JUMPING RESTORED!)
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is ComboBox)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            // BIBLE NAVIGATION (Only works if the Bible Dashboard is visible)
            if (BibleDashboard.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Right || e.Key == Key.Down)
                {
                    BtnNextVerse_Click(null, null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Left || e.Key == Key.Up)
                {
                    BtnPrevVerse_Click(null, null);
                    e.Handled = true;
                }
                return;
            }

            // SONG NAVIGATION (42 RULE SAFE)
            if (SongDashboard.Visibility == Visibility.Visible)
            {
                if (FinalHtmlModels == null || FinalHtmlModels.Count == 0) return;

                int jumpAmount = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 4 : 1;
                if (e.Key == Key.Right)
                {
                    if (songCounter + jumpAmount < FinalHtmlModels.Count) { songCounter += jumpAmount; ChoosLyrics(); e.Handled = true; }
                    else if (songCounter < FinalHtmlModels.Count - 1) { songCounter++; ChoosLyrics(); e.Handled = true; }
                }
                else if (e.Key == Key.Left)
                {
                    if (songCounter - jumpAmount >= 0) { songCounter -= jumpAmount; ChoosLyrics(); e.Handled = true; }
                    else if (songCounter > 0) { songCounter--; ChoosLyrics(); e.Handled = true; }
                }
                else if (e.Key == Key.Down)
                { // FIX: Restored vertical grid jumping!
                    if (songCounter + 5 < FinalHtmlModels.Count) { songCounter += 5; ChoosLyrics(); e.Handled = true; }
                }
                else if (e.Key == Key.Up)
                { // FIX: Restored vertical grid jumping!
                    if (songCounter - 5 >= 0) { songCounter -= 5; ChoosLyrics(); e.Handled = true; }
                }
            }

            base.OnPreviewKeyDown(e);
        }

        // FIX: Font Size Buttons wired up!
        private void BtnIncreaseFont_Click(object sender, RoutedEventArgs e)
        {
            int savedCounter = songCounter;
            if (int.TryParse(FontSizeBox.Text, out int size)) FontSizeBox.Text = (size + 5).ToString();

            string activeMemory = chkPresentation.IsChecked == true ? memPres : memGreen;
            System.IO.File.WriteAllText(activeMemory, FontSizeBox.Text);

            if (!string.IsNullOrEmpty(LyricsInputBox.Text)) { BtnSubmit_Click(null, null); songCounter = savedCounter; ChoosLyrics();
        }
        }

        private void BtnDecreaseFont_Click(object sender, RoutedEventArgs e)
        {
            int savedCounter = songCounter;
            if (int.TryParse(FontSizeBox.Text, out int size)) FontSizeBox.Text = (size - 5).ToString();

            string activeMemory = chkPresentation.IsChecked == true ? memPres : memGreen;
            System.IO.File.WriteAllText(activeMemory, FontSizeBox.Text);

            if (!string.IsNullOrEmpty(LyricsInputBox.Text))
            {
                BtnSubmit_Click(null, null); songCounter = savedCounter; ChoosLyrics();
            }
            }

        private void ChoosLyrics()
        {
            if (FinalHtmlModels == null || FinalHtmlModels.Count <= songCounter) return;

            // 1. Sends the single line to the Green Screen!
            fileHelper.SaveasHtmlForSong(templateName, FinalHtmlModels[songCounter]);

            // 2. SMART SYNC: Finds the full paragraph and sends it to the Presentation Screen!
            string cleanLine = FinalHtmlModels[songCounter].Lyrics.Replace("<br/>", "").Replace("</br>", "").Trim();
            string[] stanzas = LyricsInputBox.Text.Replace("\r\n", "\n").Replace("\r", "\n").Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            string fullParagraph = cleanLine; // Fallback
            foreach (var stanza in stanzas)
            {
                if (stanza.Replace("\n", "").Replace(" ", "").Contains(cleanLine.Replace(" ", "")))
                {
                    fullParagraph = stanza.Trim().Replace("\n", "<br/>");
                    break;
                }
            }

            fileHelper.SaveasHtmlForPresentation(new HtmlModel() { Lyrics = fullParagraph }, TxtGrad1.Text, TxtGrad2.Text); chromeHelper.RefreshChrome();

            // Updates the Green Box highlight!
            for (int i = 0; i < SongLyricsbuttons.Count; i++)
            {
                if (SongLyricsbuttons[i].Visibility != Visibility.Visible) continue;
                if (i == songCounter) SongLyricsbuttons[i].Background = (Brush)new BrushConverter().ConvertFrom("#00B478");
                else SongLyricsbuttons[i].Background = (Brush)new BrushConverter().ConvertFrom(SongLyricsbuttons[i].Uid);
            }
        }

        private void SongDropdownList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SongDropdownList.SelectedItem == null && !string.IsNullOrWhiteSpace(SongDropdownList.Text))
            {
                // FIX: "Contains" Filtering! It searches the middle of the song titles!
                string query = SongDropdownList.Text.ToLower();
                var list = fileHelper.FiletoList("SongDataBaseList");
                var matches = list.Where(x => x.ToLower().Contains(query)).OrderBy(x => x).ToList();

                SongDropdownList.ItemsSource = matches;
                SongDropdownList.IsDropDownOpen = true; // Keeps the dropdown open while you type!
                return;
            }

            if (SongDropdownList.SelectedItem == null) return;

            string safeFileName = SongDropdownList.SelectedItem.ToString();
            string baseDir = FileHelper.GetAppFolder();
            string fullPath = System.IO.Path.Combine(baseDir, $@"WebTemplate\SongDataBase\{safeFileName}.txt");

            if (System.IO.File.Exists(fullPath))
            {
                string rawText = System.IO.File.ReadAllText(fullPath);
                LyricsInputBox.Text = rawText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            }
            else LyricsInputBox.Text = string.Empty;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SongDropdownList.Text)) return;

            string safeFileName = SongDropdownList.Text;
            MessageBoxResult dialogResult = MessageBox.Show($"Are you sure you want to delete '{safeFileName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (dialogResult == MessageBoxResult.Yes)
            {
                string baseDir = FileHelper.GetAppFolder();
                string filePath = System.IO.Path.Combine(baseDir, $@"WebTemplate\SongDataBase\{safeFileName}.txt");
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

                string listPath = System.IO.Path.Combine(baseDir, @"WebTemplate\DropDownList\SongDataBaseList.txt");
                if (System.IO.File.Exists(listPath))
                {
                    var songs = System.IO.File.ReadAllText(listPath).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList();
                    songs.RemoveAll(s => s.Equals(safeFileName, StringComparison.OrdinalIgnoreCase));
                    if (songs.Count == 0) songs.Add(" ");
                    System.IO.File.WriteAllText(listPath, string.Join(";", songs));
                }

                SongDropdownList.ItemsSource = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
                LyricsInputBox.Text = "";
                MessageBox.Show("Song deleted successfully!", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var song = LyricsInputBox.Text;
            if (!string.IsNullOrEmpty(song))
            {
                string currentSelection = SongDropdownList.Text.Trim();

                // USE THE NEW MODERN SAVE DIALOG
                string songName = ShowModernSaveDialog(currentSelection);

                if (songName == null) return; // They canceled

                if (!string.IsNullOrWhiteSpace(songName))
                {
                    string safeFileName = string.Join("_", songName.Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
                    string baseDir = FileHelper.GetAppFolder();
                    var listPath = System.IO.Path.Combine(baseDir, @"WebTemplate\DropDownList\SongDataBaseList.txt");

                    var nameList = new List<string>();
                    if (System.IO.File.Exists(listPath))
                    {
                        nameList = System.IO.File.ReadAllText(listPath).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList();
                    }

                    if (!nameList.Any(n => n.Equals(safeFileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        nameList.Add(safeFileName);
                        fileHelper.CreateFile(@"DropDownList\SongDataBaseList", string.Join(";", nameList), "txt");

                        // FIX: Temporarily unplug the listener so it doesn't wipe the text box!
                        SongDropdownList.SelectionChanged -= SongDropdownList_SelectionChanged;
                        SongDropdownList.ItemsSource = fileHelper.FiletoList("SongDataBaseList").OrderBy(x => x).ToList();
                        SongDropdownList.SelectionChanged += SongDropdownList_SelectionChanged;
                    }

                    // Unplug here too just in case!
                    SongDropdownList.SelectionChanged -= SongDropdownList_SelectionChanged;
                    SongDropdownList.Text = safeFileName;
                    SongDropdownList.SelectionChanged += SongDropdownList_SelectionChanged;

                    fileHelper.CreateFileSong(safeFileName, song, "txt");
                    MessageBox.Show("Song saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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
                Background = (Brush)new BrushConverter().ConvertFrom("#0F111A"),
                Foreground = Brushes.White,
                WindowStyle = WindowStyle.ToolWindow
            };

            StackPanel panel = new StackPanel() { Margin = new Thickness(20) };
            TextBlock label = new TextBlock() { Text = "Enter Song Name (Keep same to Overwrite):", Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White };
            TextBox input = new TextBox() { Text = defaultText, FontSize = 14, Padding = new Thickness(5), Background = (Brush)new BrushConverter().ConvertFrom("#1C1E30"), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            Button btn = new Button() { Content = "Save", Width = 100, Height = 35, Margin = new Thickness(0, 20, 0, 0), Background = (Brush)new BrushConverter().ConvertFrom("#00B478"), Foreground = Brushes.White, Cursor = Cursors.Hand, BorderThickness = new Thickness(0) };

            btn.Click += (s, e) => prompt.DialogResult = true;
            panel.Children.Add(label); panel.Children.Add(input); panel.Children.Add(btn);
            prompt.Content = panel;

            return prompt.ShowDialog() == true ? input.Text.Trim() : null;
        }
        private void BtnFetchWeb_Click(object sender, RoutedEventArgs e)
        {
            SongFetcherForm fetcher = new SongFetcherForm();
            if (fetcher.ShowDialog() == System.Windows.Forms.DialogResult.OK) // Note: Using WinForms dialog result here
            {
                if (!string.IsNullOrEmpty(fetcher.FetchedLyrics))
                {
                    LyricsInputBox.Text = fetcher.FetchedLyrics.Replace("\n", "\r\n");
                    SongDropdownList.Text = fetcher.FetchedTitle;
                    MessageBox.Show("Lyrics imported! You can now edit them, add paragraph spacing, and click Save.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private void ChkPresentation_Click(object sender, RoutedEventArgs e)
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

        private void BtnNavSongs_Click(object sender, RoutedEventArgs e)
        {
            SongDashboard.Visibility = Visibility.Visible; SettingsDashboard.Visibility = Visibility.Collapsed; BibleDashboard.Visibility = Visibility.Collapsed; RemoteDashboard.Visibility = Visibility.Collapsed; LowerThirdDashboard.Visibility = Visibility.Collapsed; BirthdayDashboard.Visibility = Visibility.Collapsed;
            ClearAllSidebarHighlights(); BtnMenuSongs.Tag = "Active";
            if (isSidebarOpen) BtnToggleSidebar_Click(null, null);
        }

        private void BtnNavBible_Click(object sender, RoutedEventArgs e)
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
            string[] fonts = { "Segoe UI", "Arial", "Times New Roman", "Georgia", "Verdana", "Tahoma", "Courier New" };
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

        private void LanguageCheckbox_Click(object sender, RoutedEventArgs e)
        {
            activeLangs.Clear();
            if (chkTamil.IsChecked == true) activeLangs.Add("tamilBible");
            if (chkEnglish.IsChecked == true) activeLangs.Add("englishBible");
            if (chkHindi.IsChecked == true) activeLangs.Add("hindiBible");
            if (chkMarathi.IsChecked == true) activeLangs.Add("marathiBible");
            if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) { PopulateChapterPreview(); DisplayVerse(); }
        }
        // Instantly zeros out the offset and refreshes the exact default size!
        private void BtnBibleFontReset_Click(object sender, RoutedEventArgs e)
        {
            bibleFontSizeOffset = 0.0;
            if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) DisplayVerse();
        }
        private void BibleStyle_Changed(object sender, SelectionChangedEventArgs e) { if (BibleDashboard != null && BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) DisplayVerse(); }
        // ----------------------------------------------

        private readonly string[] BibleBooksList = { "Genesis", "Exodus", "Leviticus", "Numbers", "Deuteronomy", "Joshua", "Judges", "Ruth", "1 Samuel", "2 Samuel", "1 Kings", "2 Kings", "1 Chronicles", "2 Chronicles", "Ezra", "Nehemiah", "Esther", "Job", "Psalms", "Proverbs", "Ecclesiastes", "Song of Solomon", "Isaiah", "Jeremiah", "Lamentations", "Ezekiel", "Daniel", "Hosea", "Joel", "Amos", "Obadiah", "Jonah", "Micah", "Nahum", "Habakkuk", "Zephaniah", "Haggai", "Zechariah", "Malachi", "Matthew", "Mark", "Luke", "John", "Acts", "Romans", "1 Corinthians", "2 Corinthians", "Galatians", "Ephesians", "Philippians", "Colossians", "1 Thessalonians", "2 Thessalonians", "1 Timothy", "2 Timothy", "Titus", "Philemon", "Hebrews", "James", "1 Peter", "2 Peter", "1 John", "2 John", "3 John", "Jude", "Revelation" };

        // FIX: Flawless translated book names for the Dual-Language Reference Banner!
        private readonly string[] TamilBooksList = { "ஆதியாகமம்", "யாத்திராகமம்", "லேவியராகமம்", "எண்ணாகமம்", "உபாகமம்", "யோசுவா", "நியாயாதிபதிகள்", "ரூத்", "1 சாமுவேல்", "2 சாமுவேல்", "1 இராஜாக்கள்", "2 இராஜாக்கள்", "1 நாளாகமம்", "2 நாளாகமம்", "எஸ்றா", "நெகேமியா", "எஸ்தர்", "யோபு", "சங்கீதம்", "நீதிமொழிகள்", "பிரசங்கி", "உன்னதப்பாட்டு", "ஏசாயா", "எரேமியா", "புலம்பல்", "எசேக்கியேல்", "தானியேல்", "ஓசியா", "யோவேல்", "ஆமோஸ்", "ஒபதியா", "யோனா", "மீகா", "நாகூம்", "ஆபகூக்", "செப்பனியா", "ஆகாய்", "சகரியா", "மல்கியா", "மத்தேயு", "மாற்கு", "லூக்கா", "யோவான்", "அப்போஸ்தலர்", "ரோமர்", "1 கொரிந்தியர்", "2 கொரிந்தியர்", "கலாத்தியர்", "எபேசியர்", "பிலிப்பியர்", "கொலோசெயர்", "1 தெசலோனிக்கேயர்", "2 தெசலோனிக்கேயர்", "1 தீமோத்தேயு", "2 தீமோத்தேயு", "தீத்து", "பிலேமோன்", "எபிரெயர்", "யாக்கோபு", "1 பேதுரு", "2 பேதுரு", "1 யோவான்", "2 யோவான்", "3 யோவான்", "யூதா", "வெளிப்படுத்தின விசேஷம்" };
        private void BtnBrowseBg_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Image Files|*.jpg;*.jpeg;*.png" };
            if (ofd.ShowDialog() == true)
            {
                customBgPath = ofd.FileName;
                LblBgStatus.Text = "Image: " + System.IO.Path.GetFileName(customBgPath);
            }
        }

        private void BtnRemoveBg_Click(object sender, RoutedEventArgs e)
        {
            customBgPath = "";
            LblBgStatus.Text = "Solid Black";
            if (!string.IsNullOrEmpty(currentBook)) DisplayVerse();
        }

        private void BibleSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ParseBibleSearch(BibleSearchBox.Text);
            }
        }

        private void BibleSearchBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 3) { BibleSearchBox.SelectAll(); e.Handled = true; }
        }

        private void BtnNextVerse_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentBook)) return;
            currentVerse++;
            BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";
            DisplayVerse();
        }

        private void BtnPrevVerse_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentBook) || currentVerse <= 1) return;
            currentVerse--;
            BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";
            DisplayVerse();
        }

        // When you click a verse in the Middle Column preview list!
        private void ChapterVerseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingList || ChapterVerseList.SelectedIndex == -1) return;

            currentVerse = ChapterVerseList.SelectedIndex + 1; // Index 0 is Verse 1
            BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";
            DisplayVerse();
        }

        // FIX: Ultra-forgiving Smart Parser (Allows "Est", "Est 2", etc.)
        private void ParseBibleSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            // FIX: Replace the colon with a space so History items parse perfectly!
            string[] parts = query.Replace(":", " ").Trim().ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) return;

            string bookPrefix = "";
            currentChapter = "1";
            currentVerse = 1;

            if (int.TryParse(parts[0], out int numPrefix) && parts.Length >= 2)
            {
                bookPrefix = numPrefix + " " + parts[1];
                if (parts.Length >= 3) currentChapter = parts[2];
                if (parts.Length >= 4) int.TryParse(parts[3], out currentVerse);
            }
            else
            {
                bookPrefix = parts[0];
                if (parts.Length >= 2) currentChapter = parts[1];
                if (parts.Length >= 3) int.TryParse(parts[2], out currentVerse);
            }

            currentBook = BibleBooksList.FirstOrDefault(b => b.ToLower().StartsWith(bookPrefix));

            if (currentBook != null)
            {
                BibleSearchBox.Text = $"{currentBook} {currentChapter} {currentVerse}";
                BibleSearchBox.CaretIndex = BibleSearchBox.Text.Length;

                PopulateChapterPreview();
                DisplayVerse();
            }
            else MessageBox.Show("Could not find a book matching that abbreviation.", "Search Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                string searchBook = book.StartsWith("1 ") ? book.Replace("1 ", "I") : (book.StartsWith("2 ") ? book.Replace("2 ", "II") : (book.StartsWith("3 ") ? book.Replace("3 ", "III") : book));
                int bookIndex = Array.IndexOf(BibleBooksList, searchBook) + 1;
                string folderNum = bookIndex < 10 ? $"0{bookIndex}" : bookIndex.ToString();

                string filePath = System.IO.Path.Combine(FileHelper.GetAppFolder(), "BibleDataBase", folderName, folderNum, $"{chapter}.htm");

                if (!System.IO.File.Exists(filePath)) return dict;

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

            // Failsafe: Ensure at least one language is active
            if (activeLangs == null || activeLangs.Count == 0) activeLangs = new List<string> { "tamilBible" };

            // FIX: Uses the activeLangs array so the UI preview perfectly matches your Swapped Live Display!
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
            if (string.IsNullOrEmpty(currentBook) || activeLangs.Count == 0) return;

            List<string> verseTexts = new List<string>();
            foreach (string lang in activeLangs)
            {
                // We don't need out parameters anymore, just fetch the text!
                verseTexts.Add(FetchVerseText(currentBook, currentChapter, currentVerse, lang));
            }

            // FIX: Generate the Dual-Language Banner Text!
            string finalHeader = $"{currentBook} {currentChapter}:{currentVerse}";

            // If Tamil is one of the active languages, append the Tamil translation!
            if (activeLangs.Contains("tamilBible") && activeLangs.Count >= 2)
            {
                int bookIndex = Array.IndexOf(BibleBooksList, currentBook);
                if (bookIndex >= 0 && bookIndex < TamilBooksList.Length)
                {
                    string tamilName = TamilBooksList[bookIndex];
                    // Always put the non-Tamil language first, followed by Tamil
                    finalHeader = $"{currentBook} {currentChapter}:{currentVerse} | {tamilName} {currentChapter}:{currentVerse}";
                }
            }

            if (currentVerse - 1 >= 0 && currentVerse - 1 < ChapterVerseList.Items.Count)
            {
                isUpdatingList = true; ChapterVerseList.SelectedIndex = currentVerse - 1;
                ChapterVerseList.ScrollIntoView(ChapterVerseList.SelectedItem); isUpdatingList = false;
            }

            string vFont = CbVerseFont.SelectedItem?.ToString() ?? "Segoe UI";
            string vColor = CbVerseColor.SelectedItem?.ToString() ?? "White";
            string rFont = CbRefFont.SelectedItem?.ToString() ?? "Segoe UI";

            fileHelper.SaveasHtmlForVerseViewBible(finalHeader, verseTexts, customBgPath, bibleFontSizeOffset, vFont, vColor, rFont);
            chromeHelper.RefreshChrome();
            if (!HistoryList.Items.Contains($"{currentBook} {currentChapter}:{currentVerse}")) HistoryList.Items.Insert(0, $"{currentBook} {currentChapter}:{currentVerse}");
        }

        // Updated to extract the <title> tag for translated book names!
        private string FetchVerseText(string book, string chapter, int verse, string folderName, out string localizedTitle)
        {
            localizedTitle = book;
            try
            {
                string searchBook = book.StartsWith("1 ") ? book.Replace("1 ", "I") : (book.StartsWith("2 ") ? book.Replace("2 ", "II") : (book.StartsWith("3 ") ? book.Replace("3 ", "III") : book));
                int bookIndex = Array.IndexOf(BibleBooksList, searchBook) + 1;
                string folderNum = bookIndex < 10 ? $"0{bookIndex}" : bookIndex.ToString();

                string filePath = System.IO.Path.Combine(FileHelper.GetAppFolder(), "BibleDataBase", folderName, folderNum, $"{chapter}.htm");
                if (!System.IO.File.Exists(filePath)) return "[File Not Found]";

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

        // Swaps the order of the top two checked languages!
        private void BtnSwapLanguages_Click(object sender, RoutedEventArgs e)
        {
            if (activeLangs.Count > 1)
            {
                // Takes the first item and moves it to the back (Cycles them!)
                var first = activeLangs[0];
                activeLangs.RemoveAt(0);
                activeLangs.Add(first);

                if (BibleDashboard.Visibility == Visibility.Visible && !string.IsNullOrEmpty(currentBook)) { PopulateChapterPreview(); DisplayVerse(); }
            }
        }
        private string FetchVerseText(string book, string chapter, int verse, string folderName)
        {
            try
            {
                string searchBook = book.StartsWith("1 ") ? book.Replace("1 ", "I") : (book.StartsWith("2 ") ? book.Replace("2 ", "II") : (book.StartsWith("3 ") ? book.Replace("3 ", "III") : book));
                int bookIndex = Array.IndexOf(BibleBooksList, searchBook) + 1;
                string folderNum = bookIndex < 10 ? $"0{bookIndex}" : bookIndex.ToString();

                string filePath = System.IO.Path.Combine(FileHelper.GetAppFolder(), "BibleDataBase", folderName, folderNum, $"{chapter}.htm");
                if (!System.IO.File.Exists(filePath)) return "[File Not Found]";

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
        // ----------------------------------------------------------------
        // TEMPORARY JSON TO BIBLE DATABASE CONVERTER
        // ----------------------------------------------------------------

        // ----------------------------------------------------------------
        // UI ANIMATIONS
        // ----------------------------------------------------------------
        private bool isSidebarOpen = true;

        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            // Smoothly animates the wrapper. Because the Grid Column is "Auto", 
            // the main content will instantly expand to fill the screen!
            System.Windows.Media.Animation.DoubleAnimation animation = new System.Windows.Media.Animation.DoubleAnimation();
            animation.To = isSidebarOpen ? 0 : 255;
            animation.Duration = TimeSpan.FromMilliseconds(250);
            animation.EasingFunction = new System.Windows.Media.Animation.QuadraticEase() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut };

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
                btn.Background = (Brush)new BrushConverter().ConvertFrom("#FF4757");
                isLtTextLive = true;
            }
            else
            {
                FadeOutLowerThird();
                btn.Content = "🚀 GO LIVE!";
                btn.Background = (Brush)new BrushConverter().ConvertFrom("#00B478");
                isLtTextLive = false;
            }

        }


        // --- 2. IMAGES & LOGOS ---
        private void BtnBrowseLtImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Images|*.png;*.jpg;*.jpeg" };
            if (ofd.ShowDialog() == true)
            {
                currentImagePath = ofd.FileName;
            }
        }
        private void BtnPreviewLtImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath)) return;
            string safeName = System.IO.Path.GetFileName(currentImagePath).Replace(" ", "_");
            string template = chkLtPortrait.IsChecked == true ? "PotraitImage" : "Transprentlogo";

            HtmlModel imgModel = new HtmlModel() { Lyrics = safeName };

            // FIX: Saves to the hidden Preview monitor instead of SongLive!
            fileHelper.SaveasHtmlForLowerThird(imgModel, template);

            // Because the old method auto-saves to SongLive, we just copy it to the Preview file!
            System.IO.File.Copy(System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\SongLive.html"), System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\LtPreview.html"), true);
            MiniPreviewMonitor.CoreWebView2.Reload();
        }
        private void BtnSubmitLtImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath)) return;

            string safeName = System.IO.Path.GetFileName(currentImagePath).Replace(" ", "_");
            string destFolder = System.IO.Path.Combine(FileHelper.GetAppFolder(), @"WebTemplate\images");
            if (!System.IO.Directory.Exists(destFolder)) System.IO.Directory.CreateDirectory(destFolder);

            string destFile = System.IO.Path.Combine(destFolder, safeName);
            System.IO.File.Copy(currentImagePath, destFile, true);

            string template = "Transprentlogo"; // Removed portrait checkbox logic
            HtmlModel imgModel = new HtmlModel() { Lyrics = safeName };

            fileHelper.SaveasHtmlForLowerThird(imgModel, template);
            chromeHelper.RefreshChrome();
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
                BtnLtPlayPause.Background = (Brush)new BrushConverter().ConvertFrom("#6C5DD3");
                chromeHelper.ToggleVideoPlayback(true);
            }
            else
            {
                BtnLtPlayPause.Content = "▶ PLAY LIVE";
                BtnLtPlayPause.Background = (Brush)new BrushConverter().ConvertFrom("#00B478");
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
                BtnPreviewPlayPause.Background = (Brush)new BrushConverter().ConvertFrom("#FF9F43");
            }
            else
            {
                BtnPreviewPlayPause.Content = "▶ PLAY PREVIEW";
                BtnPreviewPlayPause.Background = (Brush)new BrushConverter().ConvertFrom("#00B478");
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
                btn.Background = (Brush)new BrushConverter().ConvertFrom("#FF4757");
                isBdayLive = true;
            }
            else
            {
                // FIX: Use classList.add so we don't erase the entry class and trigger a hard cut!
                chromeHelper.ExecuteJavascriptOnLive("document.body.classList.add('animate-out');");
                btn.Content = "🚀 PUSH LIVE";
                btn.Background = (Brush)new BrushConverter().ConvertFrom("#00B478");
                isBdayLive = false;
            }
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
                btn.Background = (Brush)new BrushConverter().ConvertFrom("#FF4757");
                isAnnivLive = true;
            }
            else
            {
                chromeHelper.ExecuteJavascriptOnLive("document.body.classList.add('animate-out');");
                btn.Content = "🚀 PUSH LIVE";
                btn.Background = (Brush)new BrushConverter().ConvertFrom("#00B478");
                isAnnivLive = false;
            }
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
                BtnEventScroll.Background = (Brush)new BrushConverter().ConvertFrom("#FF9F43");

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
                BtnEventScroll.Background = (Brush)new BrushConverter().ConvertFrom("#2D3047");

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
                foreach (var item in db) if (!thisWeekOnly || IsEventThisWeek(item.Date)) bdayList.Add(item);
            }

            if (System.IO.File.Exists(annivDbPath))
            {
                var db = System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(System.IO.File.ReadAllText(annivDbPath));
                foreach (var item in db) if (!thisWeekOnly || IsEventThisWeek(item.Date)) annivList.Add(item);
            }
            
            // Sync the memory lists for the Search Box!
            loadedBdaysMemory = bdayList.ToList();
            loadedAnnivsMemory = annivList.ToList();

            GridBdays.ItemsSource = bdayList;
            GridAnnivs.ItemsSource = annivList;
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
            if (System.IO.File.Exists(bdayDbPath)) backupBdayJson = System.IO.File.ReadAllText(bdayDbPath); // Save state for Undo!

            var db = string.IsNullOrEmpty(backupBdayJson) ? new List<EventItem>() : System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(backupBdayJson);

            foreach (var b in bdayList)
            {
                if (string.IsNullOrWhiteSpace(b.Name)) continue;
                var existing = db.FirstOrDefault(x => x.Name.Equals(b.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null) existing.Date = b.Date;
                else db.Add(b);
            }

            // AUTO-SORT: Orders by Month, then Day!
            db = db.OrderBy(x => ParseDateForSorting(x.Date)).ToList();

            System.IO.File.WriteAllText(bdayDbPath, System.Text.Json.JsonSerializer.Serialize(db));
            LoadDatabaseToUI(isShowingThisWeekOnly); // Refreshes the UI so you instantly see the sorted list!
            MessageBox.Show("Birthdays sorted and saved to Database!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaveAnnivDb_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(annivDbPath)) backupAnnivJson = System.IO.File.ReadAllText(annivDbPath); // Save state for Undo!

            var db = string.IsNullOrEmpty(backupAnnivJson) ? new List<EventItem>() : System.Text.Json.JsonSerializer.Deserialize<List<EventItem>>(backupAnnivJson);

            foreach (var a in annivList)
            {
                if (string.IsNullOrWhiteSpace(a.Name)) continue;
                var existing = db.FirstOrDefault(x => x.Name.Equals(a.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null) existing.Date = a.Date;
                else db.Add(a);
            }

            db = db.OrderBy(x => ParseDateForSorting(x.Date)).ToList();

            System.IO.File.WriteAllText(annivDbPath, System.Text.Json.JsonSerializer.Serialize(db));
            LoadDatabaseToUI(isShowingThisWeekOnly);
            MessageBox.Show("Anniversaries sorted and saved to Database!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // MODERN EXCEL (.xlsx) - ClosedXML (Auto-detects Active Tab)
                // ==========================================================
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    using (var workbook = new ClosedXML.Excel.XLWorkbook(filePath))
                    {
                        var sheet = workbook.Worksheets.FirstOrDefault(ws => ws.TabActive) ?? workbook.Worksheet(1);
                        sheetName = sheet.Name;
                        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
                        int dateCol = 1; int nameCol = 2; // ClosedXML is 1-based

                        for (int i = 1; i <= Math.Min(20, lastRow); i++)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(sheet.Cell(i, 1).GetString().Trim(), @"\d{2}/\d{2}/\*+")) { dateCol = 1; nameCol = 2; break; }
                            if (System.Text.RegularExpressions.Regex.IsMatch(sheet.Cell(i, 2).GetString().Trim(), @"\d{2}/\d{2}/\*+")) { dateCol = 2; nameCol = 3; break; }
                        }

                        HashSet<int> processedRows = new HashSet<int>();

                        // 1. EXTRACT ANNIVERSARIES
                        for (int r = 1; r <= lastRow; r++)
                        {
                            string cellName = sheet.Cell(r, nameCol).GetString().Trim();
                            if (cellName == "&")
                            {
                                if (r >= 3 && r + 1 <= lastRow)
                                {
                                    string husband = sheet.Cell(r - 2, nameCol).GetString().Trim();
                                    string rawDate = sheet.Cell(r, dateCol).GetString().Trim(); // Date on same row as &
                                    string wife = sheet.Cell(r + 1, nameCol).GetString().Trim();

                                    if (!string.IsNullOrWhiteSpace(husband) && !string.IsNullOrWhiteSpace(wife))
                                    {
                                        tempAnnivs.Add(new EventItem { Name = $"{husband} & {wife}", Date = FormatCustomDate(rawDate) });
                                        for (int i = r - 2; i <= r + 2; i++) processedRows.Add(i);
                                    }
                                }
                            }
                        }

                        // 2. EXTRACT BIRTHDAYS
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
                                        tempBdays.Add(new EventItem { Name = bdayName, Date = FormatCustomDate(cellDate) });
                                        processedRows.Add(r);
                                        processedRows.Add(r + 1);
                                    }
                                }
                            }
                        }
                    }
                }
                // ==========================================================
                // ANCIENT EXCEL (.xls) - ExcelDataReader (Asks for Tab)
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

                        int dateCol = 0; int nameCol = 1; // ExcelDataReader is 0-based
                        for (int i = 0; i < Math.Min(20, sheet.Rows.Count); i++)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(Convert.ToString(sheet.Rows[i][0]), @"\d{2}/\d{2}/\*+")) { dateCol = 0; nameCol = 1; break; }
                            if (sheet.Columns.Count > 1 && System.Text.RegularExpressions.Regex.IsMatch(Convert.ToString(sheet.Rows[i][1]), @"\d{2}/\d{2}/\*+")) { dateCol = 1; nameCol = 2; break; }
                        }

                        HashSet<int> processedRows = new HashSet<int>();

                        // 1. EXTRACT ANNIVERSARIES
                        for (int r = 0; r < sheet.Rows.Count; r++)
                        {
                            string cellName = Convert.ToString(sheet.Rows[r][nameCol]).Trim();
                            if (cellName == "&")
                            {
                                if (r >= 2 && r + 1 < sheet.Rows.Count)
                                {
                                    string husband = Convert.ToString(sheet.Rows[r - 2][nameCol]).Trim();
                                    string rawDate = Convert.ToString(sheet.Rows[r][dateCol]).Trim(); // Date on same row as &
                                    string wife = Convert.ToString(sheet.Rows[r + 1][nameCol]).Trim();

                                    if (!string.IsNullOrWhiteSpace(husband) && !string.IsNullOrWhiteSpace(wife))
                                    {
                                        tempAnnivs.Add(new EventItem { Name = $"{husband} & {wife}", Date = FormatCustomDate(rawDate) });
                                        for (int i = r - 2; i <= r + 2; i++) processedRows.Add(i);
                                    }
                                }
                            }
                        }

                        // 2. EXTRACT BIRTHDAYS
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
                                        tempBdays.Add(new EventItem { Name = bdayName, Date = FormatCustomDate(cellDate) });
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

                // Merge Birthdays: Add new ones, UPDATE existing ones!
                foreach (var b in tempBdays)
                {
                    var existing = finalBdays.FirstOrDefault(x => x.Name.Equals(b.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) existing.Date = b.Date; // Updates the date if it changed!
                    else finalBdays.Add(b); // Adds new person
                }

                // Merge Anniversaries: Add new ones, UPDATE existing ones!
                foreach (var a in tempAnnivs)
                {
                    var existing = finalAnnivs.FirstOrDefault(x => x.Name.Equals(a.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) existing.Date = a.Date;
                    else finalAnnivs.Add(a);
                }

                // Save permanently
                System.IO.File.WriteAllText(bdayDbPath, System.Text.Json.JsonSerializer.Serialize(finalBdays));
                System.IO.File.WriteAllText(annivDbPath, System.Text.Json.JsonSerializer.Serialize(finalAnnivs));

                // Instantly show ONLY what you just imported on the screen
                bdayList.Clear(); annivList.Clear();
                foreach (var b in tempBdays) bdayList.Add(b);
                foreach (var a in tempAnnivs) annivList.Add(a);
                GridBdays.ItemsSource = bdayList;
                GridAnnivs.ItemsSource = annivList;

                MessageBox.Show($"Imported {tempBdays.Count} Birthdays and {tempAnnivs.Count} Anniversaries!\n\nThe Master Database has been permanently updated.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading Excel file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sleek Dark-Mode Dialog for Ancient .xls files
        private int? ShowSheetSelectionDialog(List<string> sheetNames)
        {
            Window prompt = new Window() { Width = 350, Height = 200, Title = "Select Sheet", WindowStartupLocation = WindowStartupLocation.CenterScreen, Background = (Brush)new BrushConverter().ConvertFrom("#0F111A"), Foreground = Brushes.White, WindowStyle = WindowStyle.ToolWindow };
            StackPanel panel = new StackPanel() { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock() { Text = "This .xls file has multiple tabs.\nWhich one do you want to import from?", Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });

            ComboBox cbSheets = new ComboBox() { FontSize = 14, Padding = new Thickness(5), Background = Brushes.White, Foreground = Brushes.Black };
            foreach (string name in sheetNames) cbSheets.Items.Add(name);
            cbSheets.SelectedIndex = 0;
            panel.Children.Add(cbSheets);

            Button btn = new Button() { Content = "IMPORT", Height = 35, Margin = new Thickness(0, 20, 0, 0), Background = (Brush)new BrushConverter().ConvertFrom("#6C5DD3"), Foreground = Brushes.White, Cursor = Cursors.Hand, BorderThickness = new Thickness(0) };
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
        private void BtnToggleServer_Click(object sender, RoutedEventArgs e)
        {
            if (!RemoteServer.IsRunning)
            {
                try
                {
                    RemoteServer.Start();
                    BtnToggleServer.Content = "🛑 STOP SERVER";
                    BtnToggleServer.Background = (Brush)new BrushConverter().ConvertFrom("#FF4757"); // Red
                    ServerUrlBox.Text = RemoteServer.ServerUrl;
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
                BtnToggleServer.Background = (Brush)new BrushConverter().ConvertFrom("#00B478"); // Green
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

        private void BtnSaveGradient_Click(object sender, RoutedEventArgs e)
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(FileHelper.GetAppFolder(), "gradient_memory.txt"), $"{TxtGrad1.Text}|{TxtGrad2.Text}");
            MessageBox.Show("Presentation Gradient Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SongDropdownList_KeyUp(object sender, KeyEventArgs e)
        {
            // Ignore navigation keys so you can use arrows to select a song!
            if (e.Key == Key.Enter || e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right) return;

            TextBox tb = SongDropdownList.Template.FindName("PART_EditableTextBox", SongDropdownList) as TextBox;
            if (tb != null)
            {
                string query = tb.Text.ToLower();
                var list = fileHelper.FiletoList("SongDataBaseList");

                // Matches anywhere in the title!
                var matches = list.Where(x => x.ToLower().Contains(query)).OrderBy(x => x).ToList();

                // Temporarily unplug the event so it doesn't wipe your typing!
                SongDropdownList.SelectionChanged -= SongDropdownList_SelectionChanged;
                SongDropdownList.ItemsSource = matches;
                SongDropdownList.IsDropDownOpen = true;
                SongDropdownList.SelectionChanged += SongDropdownList_SelectionChanged;

                // Keep your cursor exactly where you left off
                tb.Text = query;
                tb.CaretIndex = query.Length;
            }
        }
    }
}