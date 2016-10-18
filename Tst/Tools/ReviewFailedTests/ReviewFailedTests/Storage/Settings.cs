using ReviewFailedTests.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReviewFailedTests.Utilities
{ 
    public class Settings : INotifyPropertyChanged
    {
        const string SettingsFileName = "settings.xml";
        string fileName;
        string windiffPath;
        string errorLogName = "check-output.log";
        string baselineName = "acc_0.txt";
        string quickTipsShown;
        Point windowLocation;
        Size windowSize;

        static Settings _instance;

        public Settings()
        {
            _instance = this;
        }
        
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new Settings();
                }
                return _instance;
            }
        }

        public Point WindowLocation
        {
            get { return this.windowLocation; }
            set { this.windowLocation  = value; }
        }

        public Size WindowSize
        {
            get { return this.windowSize; }
            set { this.windowSize = value; }
        }

        public string LastLogFile
        {
            get
            {
                return this.fileName;
            }
            set
            {
                if (this.fileName != value)
                {
                    this.fileName = value;
                    OnPropertyChanged("LastLogFile");
                }
            }
        }

        public string WindiffPath
        {
            get
            {
                return this.windiffPath;
            }
            set
            {
                if (this.windiffPath != value)
                {
                    this.windiffPath = value;
                    OnPropertyChanged("WindiffPath");
                }
            }
        }

        public string BaselineName
        {
            get
            {
                return baselineName;
            }

            set
            {
                if (this.baselineName != value)
                {
                    this.baselineName = value;
                    OnPropertyChanged("BaselineName");
                }
            }
        }

        public string ErrorLogName
        {
            get
            {
                return errorLogName;
            }

            set
            {
                if (this.errorLogName != value)
                {
                    this.errorLogName = value;
                    OnPropertyChanged("ErrorLogName");
                }
            }
        }

        public string QuickTipsShown
        {
            get
            {
                return quickTipsShown;
            }

            set
            {
                if (this.quickTipsShown != value)
                {
                    this.quickTipsShown = value;
                    OnPropertyChanged("QuickTipsShown");
                }
            }
        }

        public static bool SafeFileExists(string path)
        {
            try
            {
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }
            catch
            {
                return false;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                UiDispatcher.RunOnUIThread(() =>
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                });
            }
        }

        public static Settings Load()
        {
            var store = new IsolatedStorage<Settings>();
            Settings result = null;
            try
            {
                result = store.LoadFromFile(SettingsFileName);
            }
            catch
            {
            }
            if (result == null)
            {
                result = new Settings();
                result.Save();
            }
            return result;
        }

        bool saving;

        public void Save()
        {
            var store = new IsolatedStorage<Settings>();
            if (!saving)
            {
                saving = true;
                try
                {
                    store.SaveToFile(SettingsFileName, this);
                }
                finally
                {
                    saving = false;
                }
            }
        }

        internal bool ContainsQuickTip(string key)
        {
            if (this.quickTipsShown == null)
            {
                return false;
            }
            return this.quickTipsShown.Contains(key);

        }

        internal void AddQuickTip(string key)
        {
            if (this.quickTipsShown == null)
            {
                this.QuickTipsShown = key;
            }
            else
            {
                this.QuickTipsShown += ";" + key;
            }
        }
    }


}
