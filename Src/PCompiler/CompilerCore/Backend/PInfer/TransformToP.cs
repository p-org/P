using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.Backend.PInfer
{
    public class Transform
    {
        public string toP(IPExpr expr)
        {
            if (expr == null)
            {
                throw new ArgumentNullException(nameof(expr));
            }
            switch (expr)
            {
                case VariableAccessExpr varAccess: return $"{varAccess.Variable.Name}";
                case NamedTupleAccessExpr ntAccess:
                {
                    if (ntAccess.SubExpr is VariableAccessExpr && ntAccess.FieldName == "payload")
                    {
                        return toP(ntAccess.SubExpr);
                    }
                    else
                    {
                        return $"{toP(ntAccess.SubExpr)}.{ntAccess.FieldName}";
                    }
                }
                case TupleAccessExpr tAccess: return $"{toP(tAccess.SubExpr)}[{tAccess.FieldNo}]";
                case EnumElemRefExpr enumRef: return $"{enumRef.Value.Name}";
                case IntLiteralExpr intLit: return $"{intLit.Value}";
                case BoolLiteralExpr boolLit: return $"{boolLit.Value}";
                case FloatLiteralExpr floatLit: return $"{floatLit.Value}";
                case FunCallExpr funCall: {
                    if (funCall.Function.Name == "index")
                    {
                        // specifically for monitor functions
                        return $"{toP(funCall.Arguments[0])}_idx";
                    }
                    return $"{funCall.Function.Name}({string.Join(", ", funCall.Arguments.Select(toP))})";
                }
                case UnaryOpExpr unOpExpr:
                {
                    string op = unOpExpr.Operation switch
                    {
                        UnaryOpType.Negate => "-",
                        UnaryOpType.Not => "!",
                        _ => throw new System.Exception("Unknown unary operator")
                    };
                    return $"{op}({toP(unOpExpr.SubExpr)})";
                }
                case BinOpExpr binOpExpr:
                {
                    string op = binOpExpr.Operation switch
                    {
                        BinOpType.Add => "+",
                        BinOpType.Sub => "-",
                        BinOpType.Mul => "*",
                        BinOpType.Div => "/",
                        BinOpType.Mod => "%",
                        BinOpType.And => "&&",
                        BinOpType.Or => "||",
                        BinOpType.Eq => "==",
                        BinOpType.Neq => "!=",
                        BinOpType.Lt => "<",
                        BinOpType.Le => "<=",
                        BinOpType.Gt => ">",
                        BinOpType.Ge => ">=",
                        _ => throw new System.Exception("Unknown binary operator")
                    };
                    return $"({toP(binOpExpr.Lhs)} {op} {toP(binOpExpr.Rhs)})";
                }
                case SizeofExpr sizeofExpr: return $"sizeof({toP(sizeofExpr.Expr)})";
            }
            throw new Exception($"Unsupported expr: {expr}");
        }

        private bool PopulateExprs(PInferPredicateGenerator codegen, ICompilerConfiguration job, Scope globalScope, HashSet<string> reprs, List<IPExpr> list)
        {
            return reprs.Select(repr => {
                if (codegen.TryParseToExpr(job, globalScope, repr, out var expr))
                {
                    list.Add(expr);
                    return true;
                }
                job.Output.WriteError($"Failed to parse: {repr}");
                return false;
            }).All(x => x);
        }

        private void WriteHandlerFor(CompilationContext ctx, CompiledFile monitorFile, PEvent e, Hint h, bool inHotState)
        {
            void WriteLine(string line) => ctx.WriteLine(monitorFile.Stream, line);
            string MCBuff(string eventName) => $"Hist_{eventName}";
            string Counter(string varName) => $"Counter_{varName}";

            WriteLine($"on {e.Name} do (payload: {e.PayloadType.OriginalRepresentation}) {{");
            foreach (var ov in h.ForallQuantified())
            {
                WriteLine($"{Counter(ov.Name)}: int;");
            }
            WriteLine("var exists: bool;");
            WriteLine($"var combo: ({string.Join(", ", h.ForallQuantified().Select(_ => "int"))},);");
            WriteLine($"{MCBuff(e.Name)} += (sizeof({MCBuff(e.Name)}), payload);");
            var forallVars = h.Quantified.SkipLast(h.ExistentialQuantifiers);
            var existsVars = h.Quantified.TakeLast(h.ExistentialQuantifiers);
            foreach (var ov in forallVars)
            {
                WriteLine($"{Counter(ov.Name)} = 0;");
                WriteLine($"while ({Counter(ov.Name)} < sizeof({MCBuff(ov.EventName)})) {{"); // forall while
            }
            // Check guards
            var guardsCounters = forallVars.Select(x => Counter(x.Name));
            var filterCounters = existsVars.Select(x => Counter(x.Name));
            WriteLine($"if (checkGuards({string.Join(", ", guardsCounters)})) {{"); // check guards
            if (h.ExistentialQuantifiers == 0)
            {
                WriteLine($"assert checkFilters({string.Join(", ", guardsCounters)})");
            }
            else
            {
                WriteLine("exists = false");
                foreach (var ov in existsVars)
                {
                    WriteLine($"{Counter(ov.Name)} = 0;");
                    WriteLine($"while ({Counter(ov.Name)} < sizeof({MCBuff(ov.EventName)}) && !exists) {{"); // exists loop
                }
                WriteLine($"if (checkFilters({string.Join(", ", guardsCounters.Concat(filterCounters))})) {{");
                WriteLine("exists = true;");
                WriteLine("break;");
                WriteLine("}"); // end check filters
                foreach (var ov in existsVars.Reverse())
                {
                    WriteLine($"{Counter(ov.Name)} = {Counter(ov.Name)} + 1;");
                    WriteLine("}"); // end exists loop
                }
                WriteLine($"combo = ({string.Join(", ", guardsCounters)},);");
                WriteLine("if (exists) {");
                // remove from pending sets
                WriteLine("if (combo in Pending) {");
                WriteLine("Pending -= (combo);");
                WriteLine("}");
                WriteLine("} else {");
                // add to pending sets
                WriteLine("Pending += (combo);");
                WriteLine("}"); // end exists
            }
            WriteLine("}"); // end check guards
            foreach (var ov in forallVars.Reverse())
            {
                WriteLine($"{Counter(ov.Name)} = {Counter(ov.Name)} + 1;");
                WriteLine("}"); // end forall while
            }

            if (!inHotState && h.ExistentialQuantifiers > 0)
            {
                WriteLine("if (sizeof(Pending) > 0) {");
                WriteLine("goto Serving_Hot;");
                WriteLine("}");
            }
            if (inHotState && h.ExistentialQuantifiers > 0)
            {
                WriteLine("if (sizeof(Pending) == 0) {");
                WriteLine("goto Serving_Cold;");
                WriteLine("}");
            }
            WriteLine("}"); // end handler
        }

        public static string GetFunctionParameters(IEnumerable<PEventVariable> evs)
        {
            return string.Join(", ",
                    evs.Select((x, i) => $"{x.Name}: {x.EventDecl.PayloadType.OriginalRepresentation}")
                       .Concat(evs.Select((x, i) => $"{x.Name}_idx: int")));
        }

        public void WriteSpecMonitor(PInferPredicateGenerator codegen, CompilationContext ctx, ICompilerConfiguration job, Scope globalScope, Hint h, HashSet<string> p, HashSet<string> q, string inv, CompiledFile monitorFile)
        {
            List<IPExpr> guards = [];
            List<IPExpr> filters = [];
            void WriteLine(string line) => ctx.WriteLine(monitorFile.Stream, line);
            string MCBuff(string eventName) => $"Hist_{eventName}";
            if (PopulateExprs(codegen, job, globalScope, p, guards) && PopulateExprs(codegen, job, globalScope, q, filters))
            {
                WriteLine($"// Monitor for spec: {inv}");
                WriteLine($"spec {h.Name} observes {string.Join(", ", h.Quantified.Select(x => x.EventName).Distinct())} {{");

                var forallVars = h.Quantified.SkipLast(h.ExistentialQuantifiers);

                // Guards
                WriteLine($"fun checkGuardsImpl({GetFunctionParameters(forallVars)}): bool {{");
                WriteLine($"return {(guards.Count > 0 ? string.Join(" && ", guards.Select(toP)) : "true")};");
                WriteLine($"}}");

                // Filters
                WriteLine($"fun checkFiltersImpl({GetFunctionParameters(h.Quantified)}): bool {{");
                WriteLine($"return {(filters.Count > 0 ? string.Join(" && ", filters.Select(toP)) : "true")};");
                WriteLine($"}}");

                // Helper function
                WriteLine($"fun checkGuards({string.Join(", ", forallVars.Select(x => $"{x.Name}_idx: int"))}): bool {{");
                WriteLine($"return checkGuardsImpl({string.Join(", ",
                                                        forallVars.Select(x => $"{MCBuff(x.EventName)}[{x.Name}_idx]")
                                                                  .Concat(forallVars.Select(x => $"{x.Name}_idx")))})");
                WriteLine("}");

                WriteLine($"fun checkFilters({string.Join(", ", h.Quantified.Select(x => $"{x.Name}_idx: int"))}): bool {{");
                WriteLine($"return checkFiltersImpl({string.Join(", ", h.Quantified.Select(x => $"{MCBuff(x.EventName)}[{x.Name}_idx]")
                                                                                     .Concat(h.Quantified.Select(x => $"{x.Name}_idx")))})");
                WriteLine("}");

                var pendingType = $"({string.Join(", ", h.ForallQuantified().Select(_ => "int"))},)";
                foreach (var forall_e in h.ForallQuantified())
                {
                    WriteLine($"var {MCBuff(forall_e.Name)}: seq[{forall_e.PayloadType.OriginalRepresentation}];");
                }
                WriteLine($"var Pending: set[{pendingType}];");
                foreach (var exists_e in h.ExistentialQuantified())
                {
                    WriteLine($"var {MCBuff(exists_e.Name)}: {exists_e.PayloadType.OriginalRepresentation};");
                }
                WriteLine("start state Init {"); // Init
                WriteLine("entry {");
                foreach (var forall_e in h.ForallQuantified())
                {
                    WriteLine($"{MCBuff(forall_e.Name)} = default(seq[{forall_e.PayloadType.OriginalRepresentation}]);");
                }
                WriteLine($"Pending = default(set[{pendingType}]);");
                foreach (var exists_e in h.ExistentialQuantified())
                {
                    WriteLine($"{MCBuff(exists_e.Name)} = default(seq[{exists_e.PayloadType.OriginalRepresentation}]);");
                }
                WriteLine("goto Serving_Cold");
                WriteLine("}"); // End entry
                WriteLine("}"); // End Init

                WriteLine("cold state Serving_Cold {"); // Serving_Cold
                foreach (var ev in h.Quantified)
                {
                    WriteHandlerFor(ctx, monitorFile, ev.EventDecl, h, false);
                }
                WriteLine("}"); // End Serving_Cold

                if (h.ExistentialQuantifiers > 0)
                {
                    WriteLine("hot state Serving_Hot {"); // Serving_Hot
                    foreach (var ev in h.Quantified)
                    {
                        WriteHandlerFor(ctx, monitorFile, ev.EventDecl, h, true);
                    }
                    WriteLine("}"); // End Serving_Hot
                }
                WriteLine($"}} // {h.Name}");
            }
            else
            {
                return;
            }
        }
    }
}