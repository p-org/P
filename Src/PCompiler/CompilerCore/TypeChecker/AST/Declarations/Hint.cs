using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.Backend.PInfer;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class Hint : IPDecl, IHasScope
    {

        public Hint(string name, bool exact, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.HintDeclContext);
            // set by Hint defs in P code
            Name = name;
            Quantified = [];
            GuardPredicates = [];
            FilterPredicates = [];
            CustomFunctions = [];
            CustomPredicates = [];
            Exact = exact;
            SourceLocation = sourceNode;
            ConfigEvent = null;
            // can be set by parameter search
            PruningLevel = 3;
            ExistentialQuantifiers = 0;
            TermDepth = null;
            Arity = 1;
            NumGuardPredicates = 0;
            NumFilterPredicates = 0;
        }

        public void ShowHint()
        {
            Console.WriteLine($"Name: {Name}");
            Console.WriteLine($"Guard hints: {GuardPredicates.Count}");
            Console.WriteLine($"Filter hints: {FilterPredicates.Count}");
            Console.WriteLine($"Quantified: {string.Join(", ", Quantified.Select(x => x.EventName))}");
            if (ConfigEvent != null)
            {
                Console.WriteLine($"Config Event: {ConfigEvent.Name}");
            }
            Console.WriteLine($"Term Depth: {TermDepth}");
            Console.WriteLine($"#Existential: {ExistentialQuantifiers}");
            Console.WriteLine($"#GuardAPs: {NumGuardPredicates}");
            Console.WriteLine($"#FilterAPs: {NumFilterPredicates}");
            Console.WriteLine($"#Arity: {Arity}");
            if (CustomPredicates.Count > 0)
            {
                Console.WriteLine($"Custom predicates: {string.Join(", ", CustomPredicates.Select(x => x.Name))}");
            }
            if (CustomFunctions.Count > 0)
            {
                Console.WriteLine($"Custom functions: {string.Join(", ", CustomFunctions.Select(x => x.Name))}");
            }
        }

        public bool NextQuantifier()
        {
            ExistentialQuantifiers += 1;
            if (ExistentialQuantifiers > 1)
            {
                return false;
            }
            return true;
        }

        public bool NextNG(ICompilerConfiguration job)
        {
            NumGuardPredicates += 1;
            if (NumGuardPredicates > job.MaxGuards)
            {
                NumGuardPredicates = 0;
                return NextQuantifier();
            }
            return true;
        }

        public bool NextNF(ICompilerConfiguration job)
        {
            if (ExistentialQuantifiers == 0) return NextNG(job);
            NumFilterPredicates += 1;
            if (NumFilterPredicates > job.MaxFilters)
            {
                NumFilterPredicates = 0;
                return NextNG(job);
            }
            return true;
        }

        public bool NextArity(ICompilerConfiguration job, int maxArity)
        {
            Arity += 1;
            if (Arity > maxArity)
            {
                Arity = 1;
                return NextNF(job);
            }
            return true;
        }

        public bool Next(ICompilerConfiguration job, int maxArity)
        {
            return NextArity(job, maxArity);
        }

        public bool HasNext(ICompilerConfiguration job, int maxArity)
        {
            return Arity <= maxArity && ExistentialQuantifiers <= 1 && NumFilterPredicates <= job.MaxFilters && NumGuardPredicates <= job.MaxGuards;
        }

        public Hint Copy()
        {
            Hint h = new(Name, Exact, SourceLocation)
            {
                Quantified = Quantified,
                PruningLevel = PruningLevel,
                NumGuardPredicates = NumGuardPredicates,
                NumFilterPredicates = NumFilterPredicates,
                ExistentialQuantifiers = ExistentialQuantifiers,
                Arity = Arity,
                TermDepth = TermDepth,
                ConfigEvent = ConfigEvent,
                GuardPredicates = GuardPredicates,
                FilterPredicates = FilterPredicates,
                CustomFunctions = CustomFunctions,
                CustomPredicates = CustomPredicates,
                Scope = Scope
            };
            return h;
        }

        // DeltaGamma
        public List<PEventVariable> Quantified;
        public int PruningLevel { get; set; }
        public int NumGuardPredicates { get; set; }
        public int NumFilterPredicates { get; set; }
        public int ExistentialQuantifiers { get; set; }
        public int Arity { get; set; }
        public int? TermDepth { get; set; }
        public PEvent ConfigEvent { get; set; }
        public List<IPExpr> GuardPredicates;
        public List<IPExpr> FilterPredicates;
        public List<Function> CustomFunctions;
        public List<Function> CustomPredicates;
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public bool Exact { get; set; }
        public ParserRuleContext SourceLocation { get; set; }
    }
}