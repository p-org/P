using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using NUnit.Framework;
using PChecker;
using PChecker.Exceptions;
using PChecker.Runtime.StateMachines.Exceptions;
using PChecker.SystematicTesting;
using UnitTests.Core;
using UnitTests.Runners;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace UnitTests
{
    /// <summary>
    /// A handler that only announces is compiled as a synchronous method (see
    /// <see cref="SchedulingPointCodegenTest"/>), so an exception it throws reaches
    /// <see cref="ControlledRuntime.OnFailure"/> from the exception filter of the action, before the
    /// stack unwinds (e.g. to fail fast with a full dump). An async handler loses that: its exception
    /// is only rethrown at the await, after its frames are gone. The program is the
    /// Feature2Stmts/DynamicError/announceOnly regression test, which only checks that the failure
    /// is a bug, as it is either way.
    /// </summary>
    [TestFixture]
    public class AnnounceOnlyFailureTest
    {
        [Test]
        public void FailureOfAHandlerThatOnlyAnnouncesIsReportedBeforeTheStackUnwinds()
        {
            var testDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, "RegressionTests", "Feature2Stmts",
                "DynamicError", "announceOnly"));
            var scratchDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(AnnounceOnlyFailureTest)));
            var runner = new PCheckerRunner(testDir.GetFiles("*.p"));
            Assert.That(runner.DoCompile(scratchDir), Is.EqualTo(0), "the test program must compile");
            var dll = Path.Combine(scratchDir.FullName, "PChecker", "net8.0", "announceOnly.dll");

            var failures = new List<(Exception Exception, string Stack)>();
            TestReport report;
            lock (PCheckerRunner.CheckerLock)
            {
                var context = new AssemblyLoadContext($"ALC_{Guid.NewGuid():N}", isCollectible: true);
                try
                {
                    var execute = (Action<ControlledRuntime>)Delegate.CreateDelegate(typeof(Action<ControlledRuntime>),
                        context.LoadFromAssemblyPath(dll).GetType("PImplementation.DefaultImpl")
                            .GetMethod("Execute", BindingFlags.Public | BindingFlags.Static));
                    var engine = TestingEngine.Create(CheckerConfiguration.Create().WithTestingIterations(1),
                        runtime =>
                        {
                            runtime.OnFailure += ex => failures.Add((ex, Environment.StackTrace));
                            execute(runtime);
                        });
                    engine.Run();
                    report = engine.TestReport;
                }
                finally
                {
                    context.Unload();
                }
            }

            Assert.That(report.NumOfFoundBugs, Is.EqualTo(1), "the failing handler is a bug");
            // The failing schedule and its replay, which traces it, each report the failure.
            Assert.That(failures.Select(f => f.Exception.GetType()), Is.EqualTo(new[]
            {
                typeof(ActionExceptionFilterException), typeof(AssertionFailureException),
                typeof(ActionExceptionFilterException), typeof(AssertionFailureException)
            }), "OnFailure must first be called from the exception filter of the synchronous handler, " +
                "and then for the bug");
            var (filtered, stack) = failures[0];
            Assert.That(filtered.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(stack, Does.Contain("AnnounceAgain").And.Contain("AnnounceTwice"),
                "the frames of the failing handler must still be on the stack");
        }
    }
}
