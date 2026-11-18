using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plang.Compiler;
using Plang.Compiler.Backend;

namespace UnitTests
{
    /// <summary>
    /// Foreign code cannot be suspended, so the sends and creates it requests take their
    /// scheduling points right after the foreign call returns: generated code brackets each
    /// foreign call with BeginForeignCall / EndForeignCallAsync, which makes its callers async.
    /// Monitor code cannot send or create, so it stays synchronous and is not bracketed.
    /// </summary>
    [TestFixture]
    public class ForeignCallCodegenTest
    {
        private const string Model = @"
event eA: int;

fun Foreign(): int;

fun GlobalCallsForeign(): int {
  var x: int;
  x = Foreign();
  return x;
}

spec Watch observes eA {
  var y: int;
  start state S {
    on eA do (v: int) {
      y = GlobalCallsForeign();
      Foreign();
    }
  }
}

machine Main {
  var z: int;
  start state Init {
    entry {
      CallsForeign();
      z = CallsGlobal();
    }
  }
  fun CallsForeign() { Foreign(); }
  fun CallsGlobal(): int { return GlobalCallsForeign(); }
}

test tc [main=Main]: assert Watch in { Main };
";

        private static string generated;

        [OneTimeSetUp]
        public void Compile()
        {
            var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ForeignCall_{Guid.NewGuid():N}"));
            try
            {
                var inputFile = Path.Combine(dir.FullName, "model.p");
                File.WriteAllText(inputFile, Model);
                var job = new CompilerConfiguration(new DiscardOutput(), dir, new[] { CompilerOutput.PChecker },
                    new[] { inputFile }, "ForeignCall");
                generated = string.Join("\n", new Compiler().GenerateCodeInMemory(job).Select(f => f.Contents));
            }
            finally
            {
                try { dir.Delete(recursive: true); } catch (IOException) { }
            }
        }

        [TestCase(@"public\s+async\s+Task\s+CallsForeign\(\)")]
        [TestCase(@"public\s+async\s+Task<PInt>\s+CallsGlobal\(\)")]
        [TestCase(@"public\s+static\s+async\s+Task<PInt>\s+GlobalCallsForeign\(StateMachine currentMachine\)")]
        public void FunctionsThatCallForeignCodeAreAsync(string signature)
        {
            Assert.That(generated, Does.Match(signature));
        }

        [Test]
        public void ForeignCallsTakeTheirSchedulingPointsBeforeUnwinding()
        {
            // Statement call, then a call whose value is assigned.
            Assert.That(generated, Does.Match(
                @"currentMachine\.BeginForeignCall\(\);\s*GlobalFunctions\.Foreign\(currentMachine\);\s*" +
                @"await currentMachine\.EndForeignCallAsync\(\);\s*if \(currentMachine\.HasPendingTransition\)"));
            Assert.That(generated, Does.Match(
                @"currentMachine\.BeginForeignCall\(\);\s*\S+ = \(PInt\)\(GlobalFunctions\.Foreign\(currentMachine\)\);\s*" +
                @"await currentMachine\.EndForeignCallAsync\(\);\s*if \(currentMachine\.HasPendingTransition\)"));
            StringAssert.Contains("await GlobalFunctions.GlobalCallsForeign(currentMachine)", generated);
        }

        [Test]
        public void MonitorCodeCallingForeignCodeIsSynchronous()
        {
            Assert.That(generated, Does.Match(@"public\s+static\s+PInt\s+GlobalCallsForeign\(Monitor currentMachine\)"));

            // The monitor overload and the monitor handler are not bracketed and do not await.
            var monitorOverload = generated.Substring(generated.IndexOf("GlobalCallsForeign(Monitor currentMachine)",
                StringComparison.Ordinal));
            monitorOverload = monitorOverload.Substring(0, monitorOverload.IndexOf("return ", StringComparison.Ordinal));
            StringAssert.DoesNotContain("ForeignCall", monitorOverload);

            var monitor = generated.Substring(generated.IndexOf("class Watch", StringComparison.Ordinal));
            monitor = monitor.Substring(0, monitor.IndexOf("class Main", StringComparison.Ordinal));
            StringAssert.DoesNotContain("ForeignCall", monitor);
            StringAssert.DoesNotContain("await", monitor);
            StringAssert.DoesNotContain("async", monitor);
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
