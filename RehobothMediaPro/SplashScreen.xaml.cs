using System;
using System.Threading.Tasks;
using System.Windows;

namespace RehobothMediaPro
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            LoadAppAsync();
        }

        private async void LoadAppAsync()
        {
            // Simulate loading phases to make the progress bar look realistic!
            TxtStatus.Text = "Booting WebView2 Engine...";
            await SimulateProgress(0, 30, 800);

            TxtStatus.Text = "Loading Local Databases...";
            await SimulateProgress(30, 60, 600);

            TxtStatus.Text = "Initializing UI...";
            await SimulateProgress(60, 90, 500);

            TxtStatus.Text = "Ready.";
            await SimulateProgress(90, 100, 200);

            // Hide the splash screen, open the main app, and close the splash!
            MainWindow mainApp = new MainWindow();
            mainApp.Show();
            this.Close();
        }

        private async Task SimulateProgress(int start, int target, int delayMs)
        {
            int steps = 10;
            int stepDelay = delayMs / steps;
            double increment = (target - start) / (double)steps;

            for (int i = 1; i <= steps; i++)
            {
                ProgressBar.Value = start + (increment * i);
                await Task.Delay(stepDelay);
            }
            ProgressBar.Value = target;
        }
    }
}