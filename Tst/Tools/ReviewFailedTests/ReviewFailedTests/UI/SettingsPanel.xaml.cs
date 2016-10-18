using ReviewFailedTests.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReviewFailedTests.UI
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        Settings settings;

        public SettingsPanel()
        {
            InitializeComponent();
            this.Loaded += OnSettingsPanelLoaded;
        }

        private void EnableButtons()
        {
            if (SaveButton != null)
            {
                SaveButton.IsEnabled = Settings.SafeFileExists(WindiffPath.Text);
            }
        }

        private void OnSettingsPanelLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            settings = App.Instance.LoadSettings();
            if (WindiffPath != null)
            {
                WindiffPath.Text = "" + settings.WindiffPath;
                ErrorLogName.Text = "" + settings.ErrorLogName;

                EnableButtons();
            }
        }

        public event EventHandler Closed;

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (Settings.SafeFileExists(WindiffPath.Text))
            {
                settings.WindiffPath = WindiffPath.Text;
            }
            if (Closed != null)
            {
                Closed(this, EventArgs.Empty);
            }
        }

        private void OnWindiffPathChanged(object sender, TextChangedEventArgs e)
        {
            EnableButtons();
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.IsEnabled = false;

            Microsoft.Win32.OpenFileDialog fo = new Microsoft.Win32.OpenFileDialog();
            fo.Filter = "EXE Files (*.exe)|*.exe";
            fo.CheckFileExists = true;
            fo.Multiselect = false;
            if (fo.ShowDialog() == true)
            {
                settings.WindiffPath = WindiffPath.Text = fo.FileName;
            }
            button.IsEnabled = true;
        }
    }
}
