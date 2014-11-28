using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

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
            item.DropDownItems.Add((ToolStripItem)OpenDotFileItem());
            item.DropDownItems.Add(ReloadDotFileItem());
            return item;
        }

        static ToolStripItem ReloadDotFileItem()
        {
            var item = new ToolStripMenuItem("Reload file");
            item.ShortcutKeys = Keys.F5;
            item.Click += ReloadFileClick;
            return item;
        }

        static void ReloadFileClick(object sender, EventArgs e)
        {
            if (lastFileName != null)
            {
             //   ReadGraphFromFile(lastFileName, GViewer, false);
            }
        }

        static ToolStripItem OpenDotFileItem()
        {
            var item = new ToolStripMenuItem("Open file");
            item.ShortcutKeys = Keys.Control | Keys.O;
            item.Click += OpenFileClick;
            return item;
        }

        static void OpenFileClick(object sender, EventArgs e)
        {

            var openFileDialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = " dot files (*.dot)|*.dot|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
             //   ReadGraphFromFile(openFileDialog.FileName, GViewer, false);
            }
            var drawingGraph = new Graph();
            drawingGraph.AddEdge("a", "b");
            GViewer.Graph = drawingGraph;
        }
    }
}
