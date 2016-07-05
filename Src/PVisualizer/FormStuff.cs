using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Pc;
using Microsoft.Formula.API;
using System.Collections.Generic;
using Microsoft.Pc.Domains;

namespace Microsoft.PVisualizer
{
    public class FormStuff
    {
        public static bool initialLayout;
        static string lastFileName;
        public static GViewer GViewer;

        public static Form CreateForm(GViewer gviewer)
        {
            GViewer = gviewer;
            var form = new Form();
            form.SuspendLayout();
            form.Controls.Add(gviewer);
            gviewer.Dock = DockStyle.Fill;
            gviewer.SendToBack();
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Size = new System.Drawing.Size(Screen.PrimaryScreen.WorkingArea.Width,
                              Screen.PrimaryScreen.WorkingArea.Height);
            var statusStrip = new StatusStrip();
            var toolStribLbl = new ToolStripStatusLabel("test");
            statusStrip.Items.Add(toolStribLbl);
            form.Controls.Add(statusStrip);
            form.MainMenuStrip = GetMainMenuStrip();
            form.Controls.Add(form.MainMenuStrip);
            form.ResumeLayout();
            form.Load += form_Load;
            return form;
        }

        static void form_Load(object sender, EventArgs e)
        {
            ((Form)sender).Focus();
        }


        static MenuStrip GetMainMenuStrip()
        {
            var menu = new MenuStrip();
            menu.Items.Add(FileStripItem());
            return menu;

        }

        static ToolStripItem FileStripItem()
        {
            var item = new ToolStripMenuItem("File");
            item.DropDownItems.Add(OpenFileItem());
            item.DropDownItems.Add(SaveFileItem());
            item.DropDownItems.Add(ReloadFileItem());
            return item;
        }

        static ToolStripItem ReloadFileItem()
        {
            var item = new ToolStripMenuItem("Reload");
            item.ShowShortcutKeys = true;
            item.ShortcutKeys = Keys.F5;
            item.Click += ReloadFileClick;
            return item;
        }

        static void ReloadFileClick(object sender, EventArgs e)
        {
            if (lastFileName != null)
            {
                ReadFile(lastFileName, GViewer);
            }
        }

        static ToolStripItem OpenFileItem()
        {
            var item = new ToolStripMenuItem("Open file...");
            item.ShortcutKeys = Keys.Control | Keys.O;
            item.ShowShortcutKeys = true;
            item.Click += OpenFileClick;
            return item;
        }
        static ToolStripItem SaveFileItem()
        {
            var item = new ToolStripMenuItem("Save As...");
            item.ShowShortcutKeys = true;
            item.ShortcutKeys = Keys.Control | Keys.S;
            item.Click += SaveFileClick;
            return item;
        }

        static void OpenFileClick(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "P files (*.p)|*.p"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ReadFile(openFileDialog.FileName, GViewer);
            }
        }

        static void SaveFileClick(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                RestoreDirectory = true,
                Filter = "DGML files (*.dgml)|*.dgml"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveFile(saveFileDialog.FileName, GViewer);
            }
        }

        static void SaveFile(string fileName, GViewer gViewer)
        {
            if (programs != null)
            {
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    PtoGraph.GenerateDgml(programs, writer);
                }
            }
            else
            {
                MessageBox.Show("Please load a .p file first to generate the graph");
            }
        }

        private static void ReadFile(string inputFileName, GViewer gViewer)
        {
            var options = new CommandLineOptions();
            options.analyzeOnly = true;
            var compiler = new Compiler(options);
            var result = compiler.Compile(inputFileName);

            if (result)
            {
                lastFileName = inputFileName;
                GViewer.Graph = PtoGraph.GenerateGraph(compiler.ParsedPrograms);
                programs = new List<PProgram>(compiler.ParsedPrograms.Values);
            }
            else
            {
                MessageBox.Show("Compilation failed. Compile from command line to see detailed error messages.");
            }
        }

        static List<PProgram> programs;
    }
}
