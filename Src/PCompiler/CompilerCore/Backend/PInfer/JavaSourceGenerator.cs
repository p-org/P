using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.Backend.PInfer
{
    public class JavaCodegen : MachineGenerator
    {
        public HashSet<string> FuncNames = [];
        private readonly HashSet<IPExpr> Predicates;
        private readonly IDictionary<IPExpr, HashSet<Variable>> FreeEvents;

        public JavaCodegen(ICompilerConfiguration job, string filename, HashSet<IPExpr> predicates, IDictionary<IPExpr, HashSet<Variable>> freeEvents) : base(job, filename)
        {
            Source = new CompiledFile(filename);
            Predicates = predicates;
            FreeEvents = freeEvents;
            Job = job;
        }

        public string GenerateRawExpr(IPExpr expr)
        {
            var events = FreeEvents[expr].Select(x => {
                    var e = (PEventVariable) x;
                    return $"({e.Name}:{e.EventName})";
            });
            return GenerateCodeExpr(expr) + " where " + string.Join(" ", events);
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine("public class " + Job.ProjectName + " implements Serializable {");
            WriteLine(PreambleConstants.StaticMethods);
            foreach (var pred in PredicateStore.Store)
            {
                WriteFunction(pred.Function);
            }
            foreach (var func in FunctionStore.Store)
            {
                WriteFunction(func);
            }
            Dictionary<string, (string, int)> repr2NameArityPi = [];
            var i = 0;
            foreach (var predicate in Predicates)
            {
                var rawExpr = GenerateRawExpr(predicate);
                var fname = $"predicate_{i++}";
                WritePredicateDefn(predicate, fname);
                repr2NameArityPi[rawExpr] = (fname, FreeEvents[predicate].Count);
            }
            WritePredicateInterface(repr2NameArityPi);
            WriteLine("}");
        }

        protected void WritePredicateInterface(IDictionary<string, (string, int)> nameMap)
        {
            WriteLine("public boolean invoke(String repr, Event[] arguments) {");
            WriteLine("switch (repr) {");
            foreach (var (repr, (fname, arity)) in nameMap)
            {
                WriteLine($"case \"{repr}\": return {fname}({string.Join(", ", Enumerable.Range(0, arity).Select(i => $"arguments[{i}]"))});");
            }
            WriteLine("default: throw new RuntimeException(\"Invalid representation: \" + repr);");
            WriteLine("}");
            WriteLine("}");
        }

        protected void WritePredicateDefn(IPExpr predicate, string fname)
        {
            var parameters = FreeEvents[predicate];
            WriteLine($"private boolean {fname}({string.Join(", ", parameters.Select(x => "Event " + x.Name))}) {{");
            foreach (var param in parameters)
            {
                var casted = (PEventVariable) param;
                WriteLine(@$"assert {PreambleConstants.CheckEventType(casted.Name, casted.EventName)}:
                                {$"\"Expect {casted.Name} to be {casted.EventName} but got \" + {casted.Name}.name"};");
            }
            WriteLine("return " + GenerateCodeExpr(predicate) + ";");
            WriteLine("}");
        }

        protected override void WriteFileHeader()
        {
            WriteLine(Constants.DoNotEditWarning);
            WriteImports();
            WriteLine();
        }

        private string GenerateCodeExpr(IPExpr expr)
        {
            if (expr is VariableAccessExpr v)
            {
                return GenerateCodeVariable(v.Variable);
            }
            else if (expr is PredicateCallExpr p)
            {
                return GenerateCodePredicateCall(p);
            }
            else if (expr is FunCallExpr f)
            {
                return GenerateFuncCall(f);
            }
            else if (expr is TupleAccessExpr t)
            {
                return GenerateCodeTupleAccess(t);
            }
            else if (expr is NamedTupleAccessExpr n)
            {
                return GenerateCodeNamedTupleAccess(n);
            }
            else if (expr is BinOpExpr binOpExpr)
            {
                var lhs = GenerateCodeExpr(binOpExpr.Lhs);
                var rhs = GenerateCodeExpr(binOpExpr.Rhs);
                return binOpExpr.Operation switch
                {
                    BinOpType.Add => $"(({lhs}) + ({rhs}))",
                    BinOpType.Sub => $"(({lhs}) - ({rhs}))",
                    BinOpType.Mul => $"(({lhs}) * ({rhs}))",
                    BinOpType.Div => $"(({lhs}) / ({rhs}))",
                    BinOpType.Mod => $"(({lhs}) % ({rhs}))",
                    BinOpType.Eq => $"Objects.equals({lhs}, {rhs})",
                    BinOpType.Lt => $"(({lhs}) < ({rhs}))",
                    BinOpType.Gt => $"(({lhs}) < ({rhs}))",
                    BinOpType.And => $"(({lhs}) && ({rhs}))",
                    BinOpType.Or => $"(({lhs}) || ({rhs}))",
                    _ => throw new Exception($"Unsupported BinOp Operatoion: {binOpExpr.Operation}"),
                };
            }
            else
            {
                throw new Exception($"Unsupported expression type {expr.GetType()}");
            }
        }

        private static string GenerateCodeCall(string callee, params string[] args)
        {
            return $"{callee}({string.Join(", ", args)})";
        }

        private string GenerateCodeTupleAccess(TupleAccessExpr t)
        {
            return $"{GenerateCodeExpr(t.SubExpr)}[{t.FieldNo}]";
        }

        private string GenerateCodeNamedTupleAccess(NamedTupleAccessExpr n)
        {
            return $"{GenerateCodeExpr(n.SubExpr)}.{n.FieldName}";
        }

        private string GenerateCodePredicateCall(PredicateCallExpr p)
        {
            if (p.Predicate is BuiltinPredicate)
            {
                switch (p.Predicate.Notation)
                {
                    case Notation.Infix:
                        return $"{GenerateCodeExpr(p.Arguments[0])} {p.Predicate.Name} {GenerateCodeExpr(p.Arguments[1])}";
                }
            }
            var args = (from e in p.Arguments select GenerateCodeExpr(e)).ToArray();
            return GenerateCodeCall(p.Predicate.Name, args);
        }

        private string GenerateFuncCall(FunCallExpr funCallExpr)
        {
            if (funCallExpr.Function is BuiltinFunction builtinFun)
            {
                switch (builtinFun.Notation)
                {
                    case Notation.Infix:
                        return $"({GenerateCodeExpr(funCallExpr.Arguments[0])} {builtinFun.Name} {GenerateCodeExpr(funCallExpr.Arguments[1])})";
                    default:
                        break;
                }
            }
            return $"{funCallExpr.Function.Name}(" + string.Join(", ", (from e in funCallExpr.Arguments select GenerateCodeExpr(e)).ToArray()) + ")";
        }

        private static string GenerateCodeVariable(Variable v)
        {
            return v.Name;
        }
    }
}