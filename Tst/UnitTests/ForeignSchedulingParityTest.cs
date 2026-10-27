using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using NUnit.Framework;
using PChecker;
using ControlledRuntime = PChecker.SystematicTesting.ControlledRuntime;
using TestingEngine = PChecker.SystematicTesting.TestingEngine;
using UnitTests.Core;
using UnitTests.Runners;

namespace UnitTests
{
    /// <summary>
    /// Foreign code cannot be suspended, so its sends and creates are deferred until it returns to
    /// P code, where each takes the scheduling point of a P <c>send</c> or <c>new</c>. The checker
    /// must then explore exactly the schedules of the equivalent P code (as it did when foreign code
    /// ran on its own thread): same seed, same strategy, same order of events in every schedule.
    /// Without these scheduling points, Other could never send before the first foreign send.
    /// </summary>
    [TestFixture]
    public class ForeignSchedulingParityTest
    {
        private const uint Iterations = 100;

        private const string Common = @"
event eA: int;
event eB: int;
event eC;

fun Record(trace: string);

machine Main {
  start state Init {
    entry {
      var s: Server;
      var w: Worker;
      s = new Server();
      new Other(s);
      w = Work(s);
      send s, eA, 3;
      // Puts Worker and eC in the creates and sends sets of Main, for the foreign Work.
      send w, eC;
      new Worker(s);
    }
  }
}

machine Other {
  start state Init {
    entry (s: Server) {
      send s, eB, 10;
      send s, eB, 11;
    }
  }
}

machine Worker {
  start state Init {
    entry (s: Server) {
      send s, eB, 20;
    }
    ignore eC;
  }
}

machine Server {
  var trace: string;
  var n: int;
  start state Init {
    on eA do (v: int) { Add(v); }
    on eB do (v: int) { Add(v); }
  }
  fun Add(v: int) {
    trace = format(""{0} {1}"", trace, v);
    n = n + 1;
    if (n == 7) {
      Record(trace);
    }
  }
}
";

        // Work sends eA(1), creates a Worker, sends eA(2), and pings the Worker.
        private const string ForeignWork = @"
fun Work(s: Server): Worker;
";

        private const string PWork = @"
fun Work(s: Server): Worker {
  var w: Worker;
  send s, eA, 1;
  w = new Worker(s);
  send s, eA, 2;
  send w, eC;
  return w;
}
";

        private const string RecordCode = @"
using System.Collections.Generic;
using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static readonly List<string> Traces = new List<string>();

        public static void Record(PString trace, StateMachine machine)
        {
            Traces.Add(trace.ToString());
        }
";

        private const string ForeignWorkCode = @"
        public static PMachineValue Work(PMachineValue s, StateMachine machine)
        {
            machine.SendEvent(s, new eA(1));
            var w = machine.CreateInterface<I_Worker>(machine, s);
            machine.SendEvent(s, new eA(2));
            machine.SendEvent(w, new eC());
            return w;
        }
";

        private static string foreignDll;
        private static string pDll;

        [OneTimeSetUp]
        public void Compile()
        {
            foreignDll = Build("ParityForeign", Common + ForeignWork, RecordCode + ForeignWorkCode + "}}");
            pDll = Build("ParityP", Common + PWork, RecordCode + "}}");
        }

        private static IEnumerable<TestCaseData> Configurations()
        {
            foreach (var seed in new uint[] { 0, 1, 7, 42 })
            {
                yield return new TestCaseData("random", seed).SetName($"ForeignSendsMatchPSends_random_{seed}");
                yield return new TestCaseData("pct", seed).SetName($"ForeignSendsMatchPSends_pct_{seed}");
            }
        }

        [TestCaseSource(nameof(Configurations))]
        public void ForeignSendsMatchPSends(string strategy, uint seed)
        {
            var foreignTraces = Run(foreignDll, strategy, seed);
            var pTraces = Run(pDll, strategy, seed);

            Assert.That(foreignTraces, Has.Count.EqualTo((int)Iterations));
            Assert.That(foreignTraces, Is.EqualTo(pTraces),
                "foreign sends and creates must explore the same schedules as P sends and creates");
        }

        [Test]
        public void OtherMachinesRunBeforeTheFirstForeignSend()
        {
            var traces = Run(foreignDll, "random", 0).Concat(Run(foreignDll, "random", 1)).ToList();
            Assert.That(traces, Has.Some.StartsWith(" 10"),
                "Other must be able to send before the first foreign send is performed");
            Assert.That(traces, Has.Some.StartsWith(" 1 "));
        }

        private static List<string> Run(string dll, string strategy, uint seed)
        {
            var configuration = CheckerConfiguration.Create()
                .WithTestingIterations(Iterations)
                .WithMaxSchedulingSteps(1000)
                .WithRandomGeneratorSeed(seed);
            configuration = strategy == "pct"
                ? configuration.WithPCTStrategy(false, 3)
                : configuration.WithRandomStrategy();

            lock (PCheckerRunner.CheckerLock)
            {
                var context = new AssemblyLoadContext($"ALC_{Guid.NewGuid():N}", isCollectible: true);
                try
                {
                    var assembly = context.LoadFromAssemblyPath(dll);
                    var execute = assembly.GetType("PImplementation.DefaultImpl")
                        .GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                    var traces = (List<string>)assembly.GetType("PImplementation.GlobalFunctions")
                        .GetField("Traces").GetValue(null);
                    var engine = TestingEngine.Create(configuration,
                        (Action<ControlledRuntime>)Delegate.CreateDelegate(typeof(Action<ControlledRuntime>), execute));
                    engine.Run();

                    Assert.That(engine.TestReport.BugReports, Is.Empty);
                    Assert.That(engine.TestReport.InternalErrors, Is.Empty);
                    return traces.ToList();
                }
                finally
                {
                    context.Unload();
                }
            }
        }

        private static string Build(string name, string model, string foreignCode)
        {
            var sourceDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(ForeignSchedulingParityTest), "src", name));
            var pFile = new FileInfo(Path.Combine(sourceDir.FullName, name + ".p"));
            File.WriteAllText(pFile.FullName, model + "\ntest DefaultImpl [main=Main]: { Main, Other, Worker, Server };\n");
            var csFile = new FileInfo(Path.Combine(sourceDir.FullName, "ForeignFunctions.cs"));
            File.WriteAllText(csFile.FullName, foreignCode);

            var scratchDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(ForeignSchedulingParityTest), name));
            var generatedDir = Directory.CreateDirectory(Path.Combine(scratchDir.FullName, "PChecker"));
            File.Copy(csFile.FullName, Path.Combine(generatedDir.FullName, csFile.Name), true);

            var runner = new PCheckerRunner(new[] { pFile }, new[] { csFile });
            Assert.That(runner.DoCompile(scratchDir), Is.EqualTo(0), $"{name} failed to compile");
            return Path.Combine(generatedDir.FullName, "net8.0", name + ".dll");
        }
    }
}
