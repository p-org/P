using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewFailedTests.Models
{
    class TestModel : INotifyPropertyChanged
    {
        string path;
        string baseline;
        bool completed;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Path
        {
            get
            {
                return path;
            }

            set
            {
                if (path != value)
                {
                    path = value;
                    OnChanged("Name");
                }
            }
        }

        public string Baseline
        {
            get
            {
                return baseline;
            }

            set
            {
                if (baseline != value)
                {
                    baseline = value;
                    OnChanged("Baseline");
                }
            }
        }
        public bool Completed
        {
            get
            {
                return completed;
            }

            set
            {
                if (completed != value)
                {
                    completed = value;
                    OnChanged("Completed");
                }
            }
        }

        private void OnChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
