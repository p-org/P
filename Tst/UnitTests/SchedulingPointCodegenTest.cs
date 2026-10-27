using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plang.Compiler;
using Plang.Compiler.Backend;

namespace UnitTests
{
    /// <summary>
    /// The checker implements scheduling points (send, new and receive) as awaits, so generated
    /// functions that reach one, directly or through callees, are async. Announcing an event to
    /// monitors is not a scheduling point: functions that only announce must stay synchronous, so
    /// they do not pay for an async state machine and keep the synchronous failure handling.
    /// </summary>
    [TestFixture]
    public class SchedulingPointCodegenTest
    {
        private const string Model = @"
event eA: int;
event eB;

spec Watch observes eA {
  start state S { on eA do (x: int) { } }
}

fun GlobalAnnounce() { announce eA, 1; }

machine Main {
  var other: machine;
  start state Init {
    entry {
      other = new Other();
      goto Run;
    }
  }
  state Run {
    entry AnnounceOnly;
    on eB do CallsAnnouncers;
  }
  fun AnnounceOnly() { announce eA, 1; }
  fun CallsAnnouncers() { AnnounceOnly(); GlobalAnnounce(); }
  fun Sends() { send other, eB; }
  fun AnnouncesAndCallsSender() { announce eA, 2; Sends(); }
  fun CallsCallerOfSender() { AnnouncesAndCallsSender(); }
}

machine Other {
  start state S { ignore eB; }
}

test tc [main=Main]: assert Watch in { Main, Other };
";

        private static string generated;

        [OneTimeSetUp]
        public void Compile()
        {
            var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"SchedPoint_{Guid.NewGuid():N}"));
            try
            {
                var inputFile = Path.Combine(dir.FullName, "model.p");
                File.WriteAllText(inputFile, Model);
                var job = new CompilerConfiguration(new DiscardOutput(), dir, new[] { CompilerOutput.PChecker },
                    new[] { inputFile }, "SchedPoint");
                generated = string.Join("\n", new Compiler().GenerateCodeInMemory(job).Select(f => f.Contents));
            }
            finally
            {
                try { dir.Delete(recursive: true); } catch (IOException) { }
            }
        }

        [TestCase(@"public\s+void\s+AnnounceOnly\(\)")]
        [TestCase(@"public\s+void\s+_AnnounceOnly\(Event currentMachine_dequeuedEvent\)")]
        [TestCase(@"public\s+void\s+CallsAnnouncers\(\)")]
        [TestCase(@"public\s+static\s+void\s+GlobalAnnounce\(StateMachine currentMachine\)")]
        public void FunctionsThatOnlyAnnounceAreSynchronous(string signature)
        {
            Assert.That(generated, Does.Match(signature));
        }

        [Test]
        public void CallsOfFunctionsThatOnlyAnnounceAreNotAwaited()
        {
            Assert.That(generated, Does.Match(@"\n\s+AnnounceOnly\(\);"));
            Assert.That(generated, Does.Match(@"\n\s+GlobalFunctions\.GlobalAnnounce\(currentMachine\);"));
            StringAssert.DoesNotContain("await AnnounceOnly()", generated);
            StringAssert.DoesNotContain("await GlobalFunctions.GlobalAnnounce", generated);
        }

        [Test]
        public void AnnouncingFunctionsHaveNoMonitorOverload()
        {
            // Monitors cannot announce, so a global function that announces is not callable from one.
            StringAssert.DoesNotContain("GlobalAnnounce(Monitor currentMachine)", generated);
        }

        [TestCase(@"public\s+async\s+Task\s+Sends\(\)", "await currentMachine.SendEventAsync(")]
        [TestCase(@"public\s+async\s+Task\s+AnnouncesAndCallsSender\(\)", "await Sends();")]
        [TestCase(@"public\s+async\s+Task\s+CallsCallerOfSender\(\)", "await AnnouncesAndCallsSender();")]
        public void FunctionsThatReachASendAreAsync(string signature, string awaitedCall)
        {
            Assert.That(generated, Does.Match(signature));
            StringAssert.Contains(awaitedCall, generated);
        }

        private sealed class DiscardOutput : ICompilerOutput
        {
            public void WriteMessage(string msg, SeverityKind severity) { }
            public void WriteFile(CompiledFile file) { }
            public void WriteError(string msg) { }
            public void WriteInfo(string msg) { }
            public void WriteWarning(string msg) { }
        }
    }
}
