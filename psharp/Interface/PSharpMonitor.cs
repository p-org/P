//-----------------------------------------------------------------------
// <copyright file="Global.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.PSharp.Monitoring
{
    public partial class PSharpMonitor : Form
    {
        private Process Scheduler;

        private int IterationsDone = 0;
        private int BugsFound = 0;

        private List<string> Info;
        private CancellationTokenSource CTS;

        public PSharpMonitor()
        {
            InitializeComponent();

            this.Info = new List<string>();
            this.CTS = new CancellationTokenSource();
        }

        private void PSharpMonitor_Load(object sender, EventArgs e)
        {
            Options.StopAtBug = this.StopAtBugCheckBox.Checked;
            Options.Debug = this.DebugCheckBox.Checked;

            this.ProgressBar.Minimum = 0;
            this.ProgressBar.Maximum = Options.IterationLimit;

            this.TestButton.ForeColor = Color.Gray;
            this.StopTestButton.ForeColor = Color.Gray;
            this.TestButton.Enabled = false;
            this.StopTestButton.Enabled = false;
        }

        private void StartTesting()
        {
            this.ModifyInterfaceDuringTesting();

            Timer.StartMeasuring();

            while (this.IterationsDone < Options.IterationLimit)
            {
                this.UpdateStatus("Testing Service - Running Iteration {0} / {1}",
                    this.IterationsDone + 1, Options.IterationLimit);

                this.CreateNewTestingProcesses();
                this.SendSchedulingConfiguration();
                this.WaitAndParseResults();
                this.KillTestingProcesses();
                this.IterationsDone++;
                this.ProgressBar.Value = this.IterationsDone;
                this.UpdateTestingInfo("Finished Iteration: {0}.\nFound {1} bugs so far.\n",
                    this.IterationsDone, this.BugsFound);
            }

            this.UpdateStatus("Testing Done.");
            this.ReportResults();
            this.CleanUpExperiments();

            this.ModifyInterfaceAfterTesting();
        }

        private void ReportResults()
        {
            Timer.StopMeasuring();

            string results = " [Results]\n\n";

            if (this.IterationsDone == 0)
                this.IterationsDone++;

            results += "Total Runtime (" + this.IterationsDone +
                "executions): " + Timer.GetResult().TotalSeconds + " (sec).\n";
            results += "Average runtime per execution: " +
                Timer.GetResult().TotalSeconds / this.IterationsDone + " (sec).\n\n";

            results += "Found " + this.BugsFound + " buggy paths out of " +
                this.IterationsDone + " total executions.\n";
            results += "Percentage of buggy execution paths: " +
                (int)Math.Round((double)(100 * this.BugsFound) / this.IterationsDone) + "%.\n";

            this.UpdateTestingInfo(results);
        }

        private void CreateNewTestingProcesses()
        {
            this.Scheduler = new Process();
            this.Scheduler.StartInfo.FileName = Options.PathToProgram;
            this.Scheduler.Start();
        }

        private void KillTestingProcesses()
        {
            if (this.Scheduler != null && !this.Scheduler.HasExited)
                this.Scheduler.Kill();
        }

        private void SendSchedulingConfiguration()
        {
            string request = (int)Options.Scheduler + " " +
                Options.OperationsBound + " " +
                Options.StopAtBug + " " +
                Options.Debug + " ";

            using (var pipe = new NamedPipeServerStream("PSharpMonitorConfigurationPipe", PipeDirection.Out))
            {
                pipe.WaitForConnection();
                byte[] buffer = Encoding.UTF8.GetBytes(request);
                pipe.Write(buffer, 0, buffer.Length);
                pipe.Flush();
                pipe.WaitForPipeDrain();
            }
        }

        private void WaitAndParseResults()
        {
            string request = "";
            string check = "false";
            using (var pipe = new NamedPipeClientStream(".", "PSharpMonitorResultsPipe", PipeDirection.In))
            {
                pipe.Connect(15000);
                if (pipe.IsConnected)
                    check = "true";
                byte[] buffer = new byte[256];
                pipe.Read(buffer, 0, buffer.Length);

                int bufferIdx = buffer.Length - 1;
                while (buffer[bufferIdx] == 0) --bufferIdx;
                byte[] finalBuffer = new byte[bufferIdx + 1];
                Array.Copy(buffer, finalBuffer, bufferIdx + 1);
                
                request = Encoding.UTF8.GetString(finalBuffer, 0, finalBuffer.Length);
            }
            this.UpdateTestingInfo(check);
            while (check.Equals("false"))
                continue;
            string[] result = request.Split(' ');
            if (Boolean.Parse(result[0]))
                this.BugsFound++;
        }

        private void UpdateTestingInfo(string s, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, s, args);
            this.TestInfoTextBox.Tag = message + "\n";
            this.TestInfoTextBox.Text = "";

            this.Info.Add(message + "\n");
            if (this.Info.Count > 2)
                this.Info.RemoveAt(0);

            foreach (var str in this.Info)
                this.TestInfoTextBox.Text += str;
        }

        private void UpdateStatus(string s, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, s, args);
            this.StatusLabel.Tag = message;
            this.StatusLabel.Text = (string)this.StatusLabel.Tag;
        }

        private void ModifyInterfaceDuringTesting()
        {
            this.AppChooseButton.ForeColor = Color.Gray;
            this.LoadManifestsButton.ForeColor = Color.Gray;
            this.IterationsOption.ForeColor = Color.Gray;
            this.TestButton.ForeColor = Color.Gray;

            this.AppChooseButton.Enabled = false;
            this.LoadManifestsButton.Enabled = false;
            this.IterationsOption.Enabled = false;
            this.TestButton.Enabled = false;

            this.StopTestButton.ForeColor = Color.Black;
            this.StopTestButton.Enabled = true;

            this.ProgressBar.Value = this.IterationsDone;
        }

        private void ModifyInterfaceAfterTesting()
        {
            this.TestButton.ForeColor = Color.Black;
            this.IterationsOption.ForeColor = Color.Black;
            this.LoadManifestsButton.ForeColor = Color.Black;
            this.AppChooseButton.ForeColor = Color.Black;

            this.TestButton.Enabled = true;
            this.IterationsOption.Enabled = true;
            this.LoadManifestsButton.Enabled = true;
            this.AppChooseButton.Enabled = true;

            this.StopTestButton.ForeColor = Color.Gray;
            this.StopTestButton.Enabled = false;
        }

        private void CleanUpExperiments()
        {
            this.IterationsDone = 0;
            this.BugsFound = 0;
            this.Info.Clear();
        }

        #region interface

        private void TestButton_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                this.StartTesting();
            }, this.CTS.Token);
        }

        private void StopTestButton_Click(object sender, EventArgs e)
        {
            this.CTS.Cancel();
            this.CTS = new CancellationTokenSource();

            this.KillTestingProcesses();
            this.ModifyInterfaceAfterTesting();

            this.ReportResults();
            this.CleanUpExperiments();

            this.StopTestButton.ForeColor = Color.Gray;
            this.StopTestButton.Enabled = false;

            this.ProgressBar.Value = this.IterationsDone;
        }

        private void AppChooseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter =
               "exe files (*.exe)|*.exe|All files (*.*)|*.*";
            dialog.InitialDirectory = Options.PathToProgram;
            dialog.Title = "Select a P# Application Executable (exe file)";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Options.PathToProgram = dialog.FileName;
                this.AppNameTextBox.Tag = Options.PathToProgram;
                this.AppNameTextBox.Text = (string)this.AppNameTextBox.Tag;
            }

            if (Options.PathToProgram == String.Empty)
                return;
        }

        private void LoadManifestsButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.UpdateStatus("Application Loaded Successfully");
                this.TestButton.ForeColor = Color.Black;
                this.TestButton.Enabled = true;
            }
            catch (Exception)
            {
                MessageBox.Show("The path to the application executable was incorrect or empty.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void IterationsOption_ValueChanged(object sender, EventArgs e)
        {
            Options.IterationLimit = (int)this.IterationsOption.Value;
            this.ProgressBar.Maximum = Options.IterationLimit;
        }

        private void OpBoundOption_ValueChanged(object sender, EventArgs e)
        {
            Options.OperationsBound = (int)this.OpBoundOption.Value;
        }

        private void StopAtBugCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Options.StopAtBug = this.StopAtBugCheckBox.Checked;
        }

        private void DebugCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Options.Debug = this.DebugCheckBox.Checked;
        }

        #endregion

        private void RandomSchedRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            Options.Scheduler = Options.SchedulerType.Random;
        }

        private void RoundRobinSchedRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            Options.Scheduler = Options.SchedulerType.RoundRobin;
        }

        private void PCTSchedRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            Options.Scheduler = Options.SchedulerType.PCT;
        }
    }
}
