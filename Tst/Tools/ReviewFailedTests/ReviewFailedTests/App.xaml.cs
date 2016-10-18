using ReviewFailedTests.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReviewFailedTests
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Settings settings;
        static App instance;

        public App()
        {
            instance = this;
        }

        public Settings LoadSettings()
        {
            if (this.settings == null)
            {
                this.settings = Settings.Load();
            }
            return this.settings;
        }

        public static App Instance {  get { return instance; } }
    }
}
