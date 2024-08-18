using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.Backend.PInfer;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class Hint : IPDecl, IHasScope
    {

        public Hint(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.HintDeclContext);
            Name = name;
            Quantified = [];
            GuardPredicates = [];
            FilterPredicates = [];
            CustomFunctions = [];
            CustomPredicates = [];
            SourceLocation = sourceNode;
            ForallQuantifiers = null;
            TermDepth = null;
            ConfigEvent = null;
            Arity = null;
        }

        // DeltaGamma
        public List<PEventVariable> Quantified;
        public int? ForallQuantifiers { get; set; }
        public int? Arity { get; set; }
        public int? TermDepth { get; set; }
        public PEvent ConfigEvent { get; set; }
        public List<IPExpr> GuardPredicates;
        public List<IPExpr> FilterPredicates;
        public List<Function> CustomFunctions;
        public List<Function> CustomPredicates;
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public ParserRuleContext SourceLocation { get; set; }
    }
}