using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using NUnit.Framework;
using PChecker;
using PChecker.Coverage;
using PChecker.Runtime.Events;
using PChecker.Runtime.StateMachines;
using PChecker.SystematicTesting;
using UnitTests.Core;
using UnitTests.Runners;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace UnitTests
{
    /// <summary>
    /// Links of the activity coverage graph (the .sci report) between the state that sent an event
    /// and the state that handled it.
    /// </summary>
    [TestFixture]
    public class ActivityCoverageGraphTest
    {
        private class EX : Event
        {
        }

        private static GraphLink FindLink(Graph graph, string source, string target) =>
            graph.Links.SingleOrDefault(l => l.Source.Id == source && l.Target.Id == target && l.Category != "Contains");

        private static string HandledBy(GraphLink link) =>
            link.Attributes != null && link.Attributes.TryGetValue("HandledBy", out var state) ? state : null;

        /// <summary>
        /// A raised event that has been handled must not be matched with a later event of the same
        /// type. The checker used to keep it in the coverage inbox, so it took the place of the eX
        /// that Sender sends afterwards: the link from Sender was missing, and the raise link got a
        /// HandledBy attribute.
        /// </summary>
        [Test]
        public void HandledRaisedEventIsNotLinkedToALaterSend()
        {
            var testDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, "RegressionTests", "Integration",
                "Coverage", "RaisedEventCoverage"));
            var scratchDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(HandledRaisedEventIsNotLinkedToALaterSend)));
            var runner = new PCheckerRunner(testDir.GetFiles("*.p"));
            Assert.That(runner.DoCompile(scratchDir), Is.EqualTo(0), "the test program must compile");

            Graph graph;
            lock (PCheckerRunner.CheckerLock)
            {
                var context = new AssemblyLoadContext($"ALC_{Guid.NewGuid():N}", isCollectible: true);
                try
                {
                    var assembly = context.LoadFromAssemblyPath(Path.Combine(scratchDir.FullName, "PChecker",
                        "net8.0", "RaisedEventCoverage.dll"));
                    var method = assembly.GetType("PImplementation.tcRaisedEventCoverage")
                        .GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                    var test = (Action<ControlledRuntime>)Delegate.CreateDelegate(typeof(Action<ControlledRuntime>),
                        method);
                    var configuration = CheckerConfiguration.Create()
                        .WithTestingIterations(10)
                        .WithRandomGeneratorSeed(1)
                        .WithActivityCoverageEnabled();
                    var engine = TestingEngine.Create(configuration, test);
                    engine.Run();
                    Assert.That(engine.TestReport.BugReports, Is.Empty);

                    // Check the graph as the .sci report stores it.
                    var sciPath = Path.Combine(scratchDir.FullName, "RaisedEventCoverage.sci");
                    engine.TestReport.CoverageInfo.Save(sciPath);
                    graph = CoverageInfo.Load(sciPath).CoverageGraph;
                }
                finally
                {
                    context.Unload();
                }
            }

            var send = FindLink(graph, "PImplementation.Sender.S", "PImplementation.Recv.B");
            Assert.That(send, Is.Not.Null, "the eX sent by Sender must be linked to the state that handled it");
            Assert.That(HandledBy(send), Is.EqualTo("B"));

            var raise = FindLink(graph, "PImplementation.Recv.A", "PImplementation.Recv.B");
            Assert.That(raise, Is.Not.Null);
            Assert.That(HandledBy(raise), Is.Null, "the raise link must not be attributed to the later eX");

            Assert.That(FindLink(graph, "PImplementation.Recv.B", "PImplementation.Recv.C"), Is.Not.Null);
        }

        /// <summary>
        /// When events link the same two states several times, the attributes of the link come from
        /// the latest one, even though the link itself is found through a cache.
        /// </summary>
        [Test]
        public void LinkAttributesComeFromTheLatestEvent()
        {
            var builder = new ControlledRuntimeLogGraphBuilder(false);
            var sender = new StateMachineId(typeof(ActivityCoverageGraphTest), "Sender", null, useNameForHashing: true);
            var receiver = new StateMachineId(typeof(ActivityCoverageGraphTest), "Receiver", null,
                useNameForHashing: true);

            foreach (var handlingState in new[] { "H1", "H2", "H1" })
            {
                builder.OnSendEvent(receiver, sender.Name, sender.Type, "S", new EX(), false);
                builder.OnDequeueEvent(receiver, "T", new EX(), sender, null);
                builder.OnExecuteAction(receiver, handlingState, "T", "Handle");
            }

            var link = FindLink(builder.Graph, sender.Name + ".S", receiver.Name + ".T");
            Assert.That(link, Is.Not.Null);
            Assert.That(HandledBy(link), Is.EqualTo("H1"));
        }
    }
}
