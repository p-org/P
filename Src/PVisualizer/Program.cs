using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.Routing.Spline.Bundling;
using Microsoft.Msagl.Routing.Visibility;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace Microsoft.PVisualizer
{
    internal class Program
    {
        static bool bundling;
        const string QuietOption = "-quiet";
        const string FileOption = "-file";
        const string BundlingOption = "-bundling";
        const string ListOfFilesOption = "-listoffiles";
        const string TestCdtOption = "-tcdt";
        const string TestCdtOption2 = "-tcdt2";
        const string TestCdtOption0 = "-tcdt0";
        const string TestCdtOption1 = "-tcdt1";
        const string ReverseXOption = "-rx";
        const string MdsOption = "-mds";
        const string FdOption = "-fd";
        const string EdgeSeparationOption = "-es";
        const string RecoverSugiyamaTestOption = "-rst";
        const string InkImportanceOption = "-ink";
        const string ConstraintsTestOption = "-tcnstr";
        const string TightPaddingOption = "-tpad";
        const string LoosePaddingOption = "-lpad";
        const string CapacityCoeffOption = "-cc";
        const string PolygonDistanceTestOption = "-pd";
        const string PolygonDistanceTestOption3 = "-pd3";
        const string RandomBundlingTest = "-rbt";
        const string TestCdtThreaderOption = "-tth";
        const string AsyncLayoutOption = "-async";


        [STAThread]
        static void Main(string[] args)
        {

            var gviewer = new GViewer();
            gviewer.MouseMove += Draw.GviewerMouseMove;
            Form form = CreateForm(null, gviewer);

            Application.Run(form);

        }

       

        static void RouteEdges(GeometryGraph graph, FastIncrementalLayoutSettings settings)
        {
            if (graph.Edges.Count < 1000)
            {
                var router = new SplineRouter(graph, settings.EdgeRoutingSettings.Padding,
                                              settings.EdgeRoutingSettings.PolylinePadding,
                                              settings.EdgeRoutingSettings.ConeAngle,
                                              settings.EdgeRoutingSettings.BundlingSettings);

                router.Run();
            }
            else
            {
                var sr = new StraightLineEdges(graph.Edges, 1);
                sr.Run();
            }

        }

        static void MoveN0ToTheLeft(Node n0, GeometryGraph graph, FastIncrementalLayoutSettings settings)
        {
            n0.Center += new Point(-10, 0);
            LockPosition lockPosition = settings.CreateLock(n0, n0.BoundingBox);

            settings.IncrementalRun(graph);
            RouteEdges(graph, settings);
            //LayoutAlgorithmSettings.ShowGraph(graph);
            settings.ClearLocks();
            settings.RemoveLock(lockPosition);
        }

        static GeometryGraph CreateGeometryGraphForFD()
        {
            var g = new GeometryGraph();

            for (int i = 0; i < 50; i++)
            {
                var a = new Node(CreateCurveAt(0, 0, 50));
                g.Nodes.Add(a);
                g.RootCluster.AddChild(a);
            }
            for (int i = 0; i < g.Nodes.Count; i++)
                for (int j = i + g.Nodes.Count / 2; j < g.Nodes.Count; j++)
                    g.Edges.Add(NewEdge(g, i, j));


            return g;
        }

        static Edge NewEdge(GeometryGraph g, int i, int j)
        {
            var e = new Edge(g.Nodes[i], g.Nodes[j]) { LineWidth = 0.01 };
            return e;
        }

        static ICurve CreateCurveAt(double x, double y, double size)
        {

            return CurveFactory.CreateRectangleWithRoundedCorners(size, size, size / 10, size / 10, new Point(x, y));
        }

       
        static Form CreateForm(Graph graph, GViewer gviewer)
        {
            Form form = FormStuff.CreateForm(gviewer);
            form.SuspendLayout();
            SetEdgeSeparationBar(form);

            gviewer.GraphChanged += GviewerGraphChanged;

            if (graph != null)
                gviewer.Graph = graph;
            return form;
        }

        static void GviewerGraphChanged(object sender, EventArgs e)
        {
            var gviewer = (GViewer)sender;
            Graph drawingGraph = gviewer.Graph;
            if (drawingGraph != null)
            {
                var form = (Form)gviewer.Parent;
                CheckBox checkBox = null;
                foreach (object c in form.Controls)
                {
                    checkBox = c as CheckBox;
                    if (checkBox != null)
                        break;
                }
                if (bundling)
                {
                    drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode =
                        EdgeRoutingMode.SplineBundling;
                    SetTransparency(drawingGraph);

                    if (drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings == null)
                        drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings =
                            new BundlingSettings();
                }
            }
        }

        static TrackBar edgeSeparationTrackBar;

        static void SetEdgeSeparationBar(Form form)
        {
            edgeSeparationTrackBar = new TrackBar();
            form.Controls.Add(edgeSeparationTrackBar);
            edgeSeparationTrackBar.Location = new System.Drawing.Point(form.MainMenuStrip.Location.X + 400,
                                                                       form.MainMenuStrip.Location.Y);
            edgeSeparationTrackBar.Maximum = 20;
            edgeSeparationTrackBar.Value = (int)(0.5 * (edgeSeparationTrackBar.Minimum + edgeSeparationTrackBar.Maximum));
            edgeSeparationTrackBar.ValueChanged += EdgeSeparationTrackBarValueChanged;


            edgeSeparationTrackBar.BringToFront();
            form.ResumeLayout();
        }

        static void EdgeSeparationTrackBarValueChanged(object sender, EventArgs e)
        {
            var edgeSeparationTruckBar = (TrackBar)sender;
            GViewer gviewer = GetGviewer(edgeSeparationTruckBar);

            Graph drawingGraph = gviewer.Graph;
            if (drawingGraph == null)
                return;


            EdgeRoutingSettings edgeRoutingSettings = drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings;
            edgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
            if (edgeRoutingSettings.BundlingSettings == null)
                edgeRoutingSettings.BundlingSettings = new BundlingSettings();
            edgeRoutingSettings.BundlingSettings.EdgeSeparation = GetEdgeSeparation(edgeSeparationTruckBar);
            var br = new SplineRouter(drawingGraph.GeometryGraph, 1, 1, Math.PI / 6, edgeRoutingSettings.BundlingSettings);
            br.Run();

            IViewer iv = gviewer;
            foreach (IViewerObject edge in iv.Entities)
            {
                if (edge is IViewerEdge)
                    iv.Invalidate(edge);
            }
        }

        static void SetTransparency(Graph drawingGraph)
        {
            foreach (Microsoft.Msagl.Drawing.Edge edge in drawingGraph.Edges)
            {
                Color color = edge.Attr.Color;
                edge.Attr.Color = new Color(100, color.R, color.G, color.B);
            }
        }

        static double GetEdgeSeparation(TrackBar edgeSeparationTruckBar)
        {
            double max = edgeSeparationTruckBar.Maximum;
            double min = edgeSeparationTruckBar.Minimum;
            double val = edgeSeparationTruckBar.Value;
            double alpha = (val - min) / (max - min);
            const double sepMaxMult = 2;
            const double sepMinMult = 0.1;
            const double span = sepMaxMult - sepMinMult;
            return (alpha - 0.5) * span + 0.5; //0.5 is the default edge separation
        }

        static GViewer GetGviewer(Control edgeSeparationTruckBar)
        {
            Control form = edgeSeparationTruckBar.Parent;
            return GetGViewerFromForm(form);
        }

        static GViewer GetGViewerFromForm(Control form)
        {
            GViewer gv = null;
            foreach (object g in form.Controls)
            {
                gv = g as GViewer;
                if (gv != null)
                    break;
            }
            return gv;
        }

    }
}
