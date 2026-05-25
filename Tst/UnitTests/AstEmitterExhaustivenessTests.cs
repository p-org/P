using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Plang.Compiler.TypeChecker.AST;

namespace UnitTests
{
    /// <summary>
    /// Guards that the imperative backends' emitter contracts stay exhaustive over the AST.
    /// The IExpressionEmitter/IStatementEmitter interfaces force every backend to implement a
    /// method per node at compile time, but adding a new AST node + a forgotten dispatch arm
    /// would only fail at runtime. This test fails the build instead: a new IPExpr/IPStmt node
    /// must either get a Write* method in the emitter contract or be added (with justification)
    /// to the exclusion set below.
    /// </summary>
    [TestFixture]
    public class AstEmitterExhaustivenessTests
    {
        private static readonly Assembly CompilerAsm = typeof(IPExpr).Assembly;

        // Expression kinds handled outside the imperative IExpressionEmitter contract: PVerifier
        // emits these functionally (its own ExprToString), and they are never produced for the
        // PChecker/PEx/PObserve backends.
        private static readonly HashSet<string> ExprExclusions = new()
        {
            "EventAccessExpr", "FlyingExpr", "InvariantGroupRefExpr", "InvariantRefExpr",
            "MachineAccessExpr", "PureCallExpr", "QuantExpr", "SentExpr", "SpecAccessExpr",
            "SpecRefExpr", "TargetsExpr", "TestExpr",
            // Only produced by ParamVisitor for param/config values; never reaches backend
            // codegen (no backend has ever handled it).
            "SeqLiteralExpr",
        };

        // Statement kinds outside the shared IStatementEmitter contract.
        private static readonly HashSet<string> StmtExclusions = new()
        {
            "ReceiveSplitStmt", // PEx-internal node, handled in PEx's own WriteStmt wrapper
        };

        [Test]
        public void EveryExpressionNodeIsInTheEmitterContractOrExcluded()
        {
            AssertExhaustive(typeof(IPExpr), "Plang.Compiler.Backend.IExpressionEmitter`1", ExprExclusions);
        }

        [Test]
        public void EveryStatementNodeIsInTheEmitterContractOrExcluded()
        {
            AssertExhaustive(typeof(IPStmt), "Plang.Compiler.Backend.IStatementEmitter`2", StmtExclusions);
        }

        private static void AssertExhaustive(Type nodeBase, string emitterTypeName, ISet<string> exclusions)
        {
            var emitter = CompilerAsm.GetType(emitterTypeName);
            Assert.NotNull(emitter, $"Could not find emitter interface '{emitterTypeName}'.");

            var handled = emitter.GetMethods().Select(m => m.Name).ToHashSet();

            var missing = CompilerAsm.GetTypes()
                .Where(t => nodeBase.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => t.Name)
                .Where(n => !handled.Contains("Write" + n) && !exclusions.Contains(n))
                .OrderBy(n => n)
                .ToList();

            Assert.IsEmpty(missing,
                $"These {nodeBase.Name} nodes are neither covered by '{emitterTypeName}' nor excluded. " +
                "Add a Write* method to the emitter contract (and a dispatch arm), or add them to the " +
                $"exclusion set with justification: {string.Join(", ", missing)}");
        }
    }
}
