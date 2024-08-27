using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        public string GetQuantifierHeader()
        {
            string result = "";
            result += string.Join(" ", Quantified.SkipLast(ExistentialQuantifiers).Select(v => $"∀{v.Name}: {v.EventName}"));
            result += string.Join(" ", Quantified.TakeLast(ExistentialQuantifiers).Select(v => $"∃{v.Name}: {v.EventName}"));
            return result;
        }

        public string GetInvariantReprHeader(string guards, string filters)
        {
            string result = "";
            result += string.Join(" ", Quantified.SkipLast(ExistentialQuantifiers).Select(v => $"∀{v.Name}: {v.EventName}"));
            if (guards.Length > 0)
            {
                result += $" :: {guards} -> ";
            }
            else if (result.Length > 0)
            {
                result += " :: ";
            }
            result += string.Join(" ", Quantified.TakeLast(ExistentialQuantifiers).Select(v => $"∃{v.Name}: {v.EventName}"));
            if (ExistentialQuantifiers > 0)
            {
                result += " :: ";
            }
            if (filters.Length > 0)
            {
                result += filters;
            }
            return result;
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
            if (CustomPredicates.Count > 0)
            {
                Console.WriteLine($"Custom predicates: {string.Join(", ", CustomPredicates.Select(x => x.Name))}");
            }
            if (CustomFunctions.Count > 0)
            {
                Console.WriteLine($"Custom functions: {string.Join(", ", CustomFunctions.Select(x => x.Name))}");
            }
            Console.WriteLine($"Term Depth: {TermDepth}");
            string formatStr = $"| {{0, -5}} | {{1, -8}} | {{2, -8}} | {{3, -8}} |";
            Console.WriteLine(string.Format(formatStr, "Arity", "#Filters", "#Guards", "#Ext"));
            Console.WriteLine(string.Format(formatStr, Arity, NumFilterPredicates, NumGuardPredicates, ExistentialQuantifiers));
        }

        public bool NextQuantifier()
        {
            if (QuantifiedSame) return false;
            ExistentialQuantifiers += 1;
            if (ExistentialQuantifiers > 1)
            {
                return false;
            }
            return true;
        }

        public bool NextNG(ICompilerConfiguration job)
        {
            // if (ExistentialQuantifiers != 0) return NextQuantifier();
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
            if (QuantifiedSame)
            {
                // for now: do not do forall-exists on a same type of events
                return Arity <= maxArity && NumFilterPredicates <= job.MaxFilters && NumGuardPredicates <= job.MaxGuards;
            }
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
        public bool QuantifiedSame => Quantified.Select(x => x.EventName).ToHashSet().Count == 1;
        public PEvent ConfigEvent { get; set; }
        public List<IPExpr> GuardPredicates;
        public List<IPExpr> FilterPredicates;
        public List<Function> CustomFunctions;
        public List<Function> CustomPredicates;
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public bool Exact { get; set; }
        public ParserRuleContext SourceLocation { get; set; }

        // needed because of the [IPExpr] in guards/filters
        public class EqualityComparer : IEqualityComparer<Hint>
        {
            private IEqualityComparer<IPExpr> astCmp = new ASTComparer();
            public bool Equals(Hint x, Hint y)
            {
                // filter first based on obvious things
                bool primitiveCmps = x.NumGuardPredicates == y.NumGuardPredicates &&
                                     x.NumFilterPredicates == y.NumFilterPredicates &&
                                     x.Arity == y.Arity &&
                                     x.ConfigEvent == y.ConfigEvent &&
                                     x.ExistentialQuantifiers == y.ExistentialQuantifiers &&
                                     x.Exact == y.Exact &&
                                     x.PruningLevel == y.PruningLevel &&
                                     x.TermDepth == y.TermDepth;
                if (!primitiveCmps) 
                {
                    return false;
                }
                // check quantifiers
                if (x.Quantified.Count != y.Quantified.Count) 
                {
                    return false;
                }
                if (!x.Quantified.Zip(y.Quantified).Select(pi => pi.First.EventName == pi.Second.EventName).All(x => x))
                {
                    return false;
                }
                // check they have the same set of custom functions/predicates
                if (!(x.CustomFunctions.ToHashSet().SetEquals(y.CustomFunctions)
                        && x.CustomPredicates.ToHashSet().SetEquals(y.CustomPredicates)))
                {
                    return false;   
                }
                // check hints
                HashSet<IPExpr> xHints = new(astCmp);
                HashSet<IPExpr> yHints = new(astCmp);
                xHints.UnionWith(x.GuardPredicates);
                yHints.UnionWith(y.GuardPredicates);
                if (!xHints.SetEquals(yHints))
                {
                    return false;
                }
                xHints.Clear();
                yHints.Clear();
                xHints.UnionWith(x.FilterPredicates);
                yHints.UnionWith(y.FilterPredicates);
                return xHints.SetEquals(yHints);
            }

            public int GetHashCode([DisallowNull] Hint obj)
            {
                List<int> guardsHashCode = obj.GuardPredicates.Select(astCmp.GetHashCode).ToList();
                List<int> filtersHashCode = obj.FilterPredicates.Select(astCmp.GetHashCode).ToList();
                List<int> customFunctionsHashCode = obj.CustomFunctions.Select(f => f.GetHashCode()).ToList();
                List<int> customPredicatesHashCode = obj.CustomPredicates.Select(f => f.GetHashCode()).ToList();
                // "canonicalize"
                guardsHashCode.Sort();
                filtersHashCode.Sort();
                customFunctionsHashCode.Sort();
                customPredicatesHashCode.Sort();
                // Note: name is not an issue here... since it does not affect the search space
                int[] quantifiedHashes = obj.Quantified.Select(x => x.EventName.GetHashCode()).ToArray();
                int[] primitives = [obj.PruningLevel,
                        obj.NumGuardPredicates,
                        obj.NumFilterPredicates,
                        obj.ExistentialQuantifiers,
                        obj.TermDepth == null ? -1 : obj.TermDepth.Value,
                        obj.Arity,
                        obj.ConfigEvent == null ? -1 : obj.ConfigEvent.Name.GetHashCode(),
                        obj.Exact ? 0 : 1];
                int[] remaining = [.. customFunctionsHashCode, .. customPredicatesHashCode, .. guardsHashCode, .. filtersHashCode];
                int[] full = [..quantifiedHashes, .. primitives, .. remaining];
                string cont = string.Join(" ", full);
                return cont.GetHashCode();
            }
        }
    }
}