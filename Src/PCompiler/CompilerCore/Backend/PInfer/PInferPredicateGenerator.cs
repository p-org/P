using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{
    public class PInferPredicateGenerator : ICodeGenerator
    {

        public PInferPredicateGenerator()
        {
            Terms = new Dictionary<int, List<IPExpr>>();
        }

        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            // job.Output.WriteInfo($"{job.QuantifiedEvents} | {job.TermDepth} | {job.PredicateDepth}");
            if (!job.TermDepth.HasValue)
            {
                throw new Exception("Term depth not specified for predicate enumeration");
            }
            if (!job.PredicateDepth.HasValue)
            {
                throw new Exception("Predicate depth not specified for predicate enumeration");
            }
            if (job.QuantifiedEvents == null)
            {
                throw new Exception("Quantified events not specified for predicate enumeration");
            }

            var quantifiedEvents = new List<PEvent>();
            foreach (var ename in job.QuantifiedEvents)
            {
                if (globalScope.Get(ename, out PEvent e))
                {
                    quantifiedEvents.Add(e);
                }
                else
                {
                    throw new Exception($"Event {ename} not defined in global scope");
                }
            }
            AggregateFunctions(globalScope);
            AggregateDefinedPredicates(globalScope);
            var i = 0;
            var termDepth = job.TermDepth.Value;
            var predicateDepth = job.PredicateDepth.Value;
            Terms[0] = [];
            foreach (var eventInst in quantifiedEvents) {
                var eventAtom = new Variable($"e{i}", null, VariableRole.Temp)
                {
                    Type = eventInst.PayloadType
                };
                Terms[0].Add(new VariableAccessExpr(null, eventAtom));
            }
            throw new System.NotImplementedException();
        }

        private static void AggregateDefinedPredicates(Scope globalScope) {
            foreach (var f in globalScope.Functions)
            {
                if ((f.Role & FunctionRole.Predicate) == FunctionRole.Predicate && f.Signature.ReturnType == PrimitiveType.Bool)
                {
                    PredicateStore.Store.Add(new DefinedPredicate(f));
                }
            }
        }

        private static void AggregateFunctions(Scope globalScope) {
            foreach (var f in globalScope.Functions)
            {
                if ((f.Role & FunctionRole.Function) == FunctionRole.Function)
                {
                    FunctionStore.Store.Add(f);
                }
            }
        }

        private IDictionary<int, List<IPExpr>> Terms { get; }
    }
}