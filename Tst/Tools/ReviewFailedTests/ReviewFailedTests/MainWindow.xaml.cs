using ReviewFailedTests.Models;
using ReviewFailedTests.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ReviewFailedTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Settings settings;
        DelayedActions delayedActions = new DelayedActions();

        public MainWindow()
        {
            UiDispatcher.Initialize();
            InitializeComponent();
            RestorePosition();            
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
        }

        private void RestorePosition()
        {
            this.SizeChanged -= OnWindowSizeChanged;
            this.LocationChanged -= OnWindowLocationChanged;
            this.settings = App.Instance.LoadSettings();
            if (settings.WindowLocation.X != 0 && settings.WindowSize.Width != 0 && settings.WindowSize.Height != 0)
            {
                // make sure it is visible on the user's current screen configuration.
                var bounds = new System.Drawing.Rectangle(
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.X),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.Y),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Width),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Height));
                var screen = System.Windows.Forms.Screen.FromRectangle(bounds);
                bounds.Intersect(screen.WorkingArea);

                this.Left = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.X);
                this.Top = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Y);
                this.Width = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Width);
                this.Height = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Height);
            }
            this.Visibility = Visibility.Visible;
            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;
        }

        void SavePosition()
        {
            var bounds = this.RestoreBounds;

            Settings settings = App.Instance.LoadSettings();
            settings.WindowLocation = bounds.TopLeft;
            settings.WindowSize = bounds.Size;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            settings.Save();
            base.OnClosing(e);
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.IsEnabled = false;

            Microsoft.Win32.OpenFileDialog fo = new Microsoft.Win32.OpenFileDialog();
            fo.Filter = "Text files (*.txt)|*.txt";
            fo.CheckFileExists = true;
            fo.Multiselect = false;
            if (fo.ShowDialog() == true && !string.IsNullOrEmpty(fo.FileName))
            {
                OpenFile(fo.FileName);
            }
            button.IsEnabled = true;
        }

        private void OpenFile(string fileName)
        {
            try
            {
                ShowStatus("");
                using (StreamReader reader = new StreamReader(fileName))
                {
                    List<TestModel> tests = new List<TestModel>();
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        line = line.Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            tests.Add(new TestModel() { Path = line });
                        }
                        line = reader.ReadLine();
                    }
                    FailedTestList.ItemsSource = tests;

                }
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message);
            }
        }

        private void ShowStatus(string message)
        {
            UiDispatcher.RunOnUIThread(() =>
            {
                StatusText.Text = message;
            });
        }

        private void OnListItemSelected(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessSelectedItem();
            }
        }

        private void ProcessSelectedItem()
        {
            if (!Settings.SafeFileExists(settings.WindiffPath))
            {
                ShowSettings();
            }
            else
            {
                TestModel model = (TestModel)FailedTestList.SelectedItem;
                if (model != null)
                {
                    DiffBaseline(model);
                }
            }
        }
        
        private void DiffBaseline(TestModel model)
        {
            string path = model.Path;
            model.Baseline = FindBaseline(path);
            string newBaseLine = System.IO.Path.Combine(model.Path, settings.ErrorLogName);
            string oldBaseLine = System.IO.Path.Combine(model.Baseline, settings.BaselineName);
            if (File.Exists(newBaseLine) && !File.Exists(oldBaseLine))
            {
                MessageBox.Show(this, model.Path + "\n\nThis test baseline cannot be fixed because it was not supposed to pass.", "Unexpected Baseline", MessageBoxButton.OK, MessageBoxImage.Error);
                FailedTestList.SelectedIndex++;
                return;
            }
            else if (!File.Exists(newBaseLine) && File.Exists(oldBaseLine))
            {
                MessageBox.Show(this, model.Path + "\n\nThis test baseline cannot be fixed because the test couldn't run.", "Missing Baseline", MessageBoxButton.OK, MessageBoxImage.Error);
                FailedTestList.SelectedIndex++;
                return;
            }


            Task.Run(() =>
            {
                string args = string.Format("\"{0}\" \"{1}\"", newBaseLine, oldBaseLine);
                Process windiff = Process.Start(new ProcessStartInfo(settings.WindiffPath, args));
                windiff.WaitForExit();
                UiDispatcher.RunOnUIThread(() =>
                {
                    PromptUser(model);
                });
            });
        }

        private void PromptUser(TestModel model)
        {
            // prompt user to "update baseline"
            if (MessageBoxResult.Yes == MessageBox.Show(this, model.Path + "\n\nDo you want to copy this new baseline?", "Update Baseline", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                // copy the file
                try
                {
                    string newBaseLine = System.IO.Path.Combine(model.Path, settings.ErrorLogName);
                    string oldBaseLine = System.IO.Path.Combine(model.Baseline, settings.BaselineName);
                    File.Copy(newBaseLine, oldBaseLine, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                model.Completed = true;
            }
            FailedTestList.SelectedIndex++;
        }

        private string FindBaseline(string path)
        {
            StringBuilder sb = new StringBuilder();
            string[] dirs = path.Split(System.IO.Path.DirectorySeparatorChar);
            bool skipNext = false;
            bool foundIt = true;
            for (int i = 0, n = dirs.Length; i<n;i++)
            {
                string dir = dirs[i];
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append(System.IO.Path.DirectorySeparatorChar);
                }
                sb.Append(dir);
                if (!foundIt && string.Compare(dir, "Tst", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // found test root.
                    // so now remove the TestResult directory combing up next.
                    skipNext = true;
                    foundIt = true;
                }
            }
            return sb.ToString();
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        void ShowSettings() { 

            SettingsPanel.Visibility = Visibility.Visible;

            TranslateTransform transform = new TranslateTransform(300, 0);
            SettingsPanel.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)))
                {
                    EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
                });
        }

        private void OnSettingsPanelClosed(object sender, EventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Hidden;
        }
    }
}
