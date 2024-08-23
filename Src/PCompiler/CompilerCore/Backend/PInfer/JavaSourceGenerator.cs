using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{
    public class JavaCodegen : MachineGenerator
    {
        public HashSet<string> FuncNames = [];
        private readonly HashSet<IPExpr> Predicates;
        private readonly IEnumerable<IPExpr> Terms;
        private readonly IDictionary<IPExpr, HashSet<PEventVariable>> FreeEvents;

        public JavaCodegen(ICompilerConfiguration job, string filename, HashSet<IPExpr> predicates, IEnumerable<IPExpr> terms, IDictionary<IPExpr, HashSet<PEventVariable>> freeEvents) : base(job, filename)
        {
            Predicates = predicates;
            Terms = terms;
            FreeEvents = freeEvents;
            Job = job;
        }

        public string GenerateTypeName(IPExpr expr) {
            return Types.SimplifiedJavaType(expr.Type);
        }

        public string GenerateRawExpr(IPExpr expr, bool simplified = false)
        {
            var result = GenerateCodeExpr(expr, simplified).Replace("\"", "");
            if (FreeEvents.ContainsKey(expr)) {
                var events = FreeEvents[expr].Select(x => {
                        var e = (PEventVariable) x;
                        return $"({e.Name}:{e.EventName})";
                });
                return result + $" => {Types.SimplifiedJavaType(expr.Type)} where " + string.Join(" ", events);
            }
            return result;
        }

        private void WriteFunctionRec(Function f, HashSet<Function> written)
        {
            if (written.Contains(f)) return;
            written.Add(f);
            foreach (var callee in f.Callees)
            {
                if (callee != f)
                {
                    WriteFunctionRec(callee, written);
                }
            }
            WriteFunction(f);
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine("public class " + Job.ProjectName + " implements Serializable {");
            WriteLine("public record PredicateWrapper (String repr, boolean negate) {}");
            Constants.PInferMode = false;
            HashSet<Function> written = [];
            foreach (var pred in PredicateStore.Store)
            {
                WriteFunctionRec(pred.Function, written);
            }
            foreach (var func in FunctionStore.Store)
            {
                WriteFunctionRec(func, written);
            }
            Constants.PInferModeOn();
            Dictionary<string, (string, List<PEventVariable>)> repr2Metadata = [];
            var i = 0;
            foreach (var predicate in Predicates)
            {
                var rawExpr = GenerateRawExpr(predicate);
                var fname = $"predicate_{i++}";
                var parameters = WritePredicateDefn(predicate, fname);
                repr2Metadata[GenerateRawExpr(predicate, true)] = (fname, parameters);
            }
            WritePredicateInterface(repr2Metadata);
            repr2Metadata = [];
            foreach (var term in Terms)
            {
                var rawExpr = GenerateRawExpr(term);
                var fname = $"term_{i++}";
                repr2Metadata[GenerateRawExpr(term, true)] = (fname, WriteTermDefn(term, fname));
            }
            WriteTermInterface(repr2Metadata);
            WriteLine("}");
        }

        protected void WriteTermInterface(IDictionary<string, (string, List<PEventVariable>)> nameMap)
        {
            WriteLine($"public static Object termOf(String repr, {Constants.PEventsClass}<?>[] arguments) {{");
            if (nameMap.Count == 0)
            {
                WriteLine("return null;");
            }
            else
            {
                WriteLine("return switch (repr) {");
                foreach (var (repr, (fname, parameters)) in nameMap)
                {
                    WriteLine($"case \"{repr}\" -> {fname}({string.Join(", ", Enumerable.Range(0, parameters.Count).Select(i => $"({Constants.EventNamespaceName}.{Names.GetNameForDecl(parameters[i].EventDecl)}) arguments[{((PEventVariable) parameters[i]).Order}]"))});");
                }
                WriteLine("default -> throw new RuntimeException(\"Invalid representation: \" + repr);");
                WriteLine("};");
            }
            WriteLine("}");
        }

        protected void WritePredicateInterface(IDictionary<string, (string, List<PEventVariable>)> nameMap)
        {
            WriteLine($"public static boolean invoke(PredicateWrapper repr, {Constants.PEventsClass}<?>[] arguments) {{");
            if (nameMap.Count == 0)
            {
                WriteLine("return false;");
            } 
            else
            {
                WriteLine("return switch (repr.repr()) {");
                foreach (var (repr, (fname, parameters)) in nameMap)
                {
                    WriteLine($"case \"{repr}\" -> {fname}({string.Join(", ", Enumerable.Range(0, parameters.Count).Select(i => $"({Constants.EventNamespaceName}.{Names.GetNameForDecl(parameters[i].EventDecl)}) arguments[{parameters[i].Order}]"))});");
                }
                WriteLine("default -> throw new RuntimeException(\"Invalid representation: \" + repr);");
                WriteLine("};");
            }
            WriteLine("}");

            WriteLine($"public static boolean conjoin(List<PredicateWrapper> repr, {Constants.PEventsClass}<?>[] arguments) {{");
            WriteLine("for (PredicateWrapper wrapper: repr) {");
            WriteLine("if (wrapper.negate() == invoke(wrapper, arguments)) return false;");
            WriteLine("}");
            WriteLine("return true;");
            WriteLine("}");
        }

        protected List<PEventVariable> WriteTermDefn(IPExpr term, string fname)
        {
            var parameters = FreeEvents[term].ToList();
            var type = term.Type.Canonicalize();
            var retType = "Object";
            if (type is PrimitiveType)
            {
                retType = Types.JavaTypeFor(type).TypeName;
            }
            if (type is EnumType || type is Index || type is CollectionSize)
            {
                retType = "long";
            }
            WriteLine($"private static {retType} {fname}({string.Join(", ", parameters.Select(x => $"{Constants.EventNamespaceName}.{Names.GetNameForDecl(x.EventDecl)} " + x.Name))}) {{");
            if (type is EnumType)
            {
                WriteLine("return " + GenerateCodeExpr(term) + ".getValue();");
            }
            else
            {
                WriteLine("return " + GenerateCodeExpr(term) + ";");
            }
            WriteLine("}");
            return parameters;
        }

        protected List<PEventVariable> WritePredicateDefn(IPExpr predicate, string fname)
        {
            var parameters = FreeEvents[predicate].ToList();
            WriteLine($"private static boolean {fname}({string.Join(", ", parameters.Select(x => $"{Constants.EventNamespaceName}.{Names.GetNameForDecl(((PEventVariable) x).EventDecl)} " + x.Name))}) {{");
            WriteLine("return " + GenerateCodeExpr(predicate) + ";");
            WriteLine("}");
            return parameters;
        }

        internal string GenerateCodeExpr(IPExpr expr, bool simplified = false)
        {
            if (expr is VariableAccessExpr v)
            {
                return GenerateCodeVariable(v.Variable, simplified);
            }
            else if (expr is PredicateCallExpr p)
            {
                return GenerateCodePredicateCall(p, simplified);
            }
            else if (expr is FunCallExpr f)
            {
                return GenerateFuncCall(f, simplified);
            }
            else if (expr is TupleAccessExpr t)
            {
                return GenerateCodeTupleAccess(t);
            }
            else if (expr is NamedTupleAccessExpr n)
            {
                return GenerateCodeNamedTupleAccess(n, simplified);
            }
            else if (expr is IPredicate)
            {
                var predicate = (IPredicate) expr;
                return $"{predicate.Name} :: {string.Join(" -> ", predicate.Signature.ParameterTypes.Select(PInferPredicateGenerator.ShowType)) + " -> bool"}";
            }
            else if (expr is BinOpExpr binOpExpr)
            {
                var lhs = GenerateCodeExpr(binOpExpr.Lhs, simplified);
                var rhs = GenerateCodeExpr(binOpExpr.Rhs, simplified);
                return binOpExpr.Operation switch
                {
                    BinOpType.Add => $"(({lhs}) + ({rhs}))",
                    BinOpType.Sub => $"(({lhs}) - ({rhs}))",
                    BinOpType.Mul => $"(({lhs}) * ({rhs}))",
                    BinOpType.Div => $"(({lhs}) / ({rhs}))",
                    BinOpType.Mod => $"(({lhs}) % ({rhs}))",
                    BinOpType.Eq => simplified ? $"({lhs} == {rhs})" : $"Objects.equals({lhs}, {rhs})",
                    BinOpType.Neq => simplified ? $"({lhs} != {rhs})" : $"(!Objects.equals({lhs}, {rhs}))",
                    BinOpType.Le => $"(({lhs}) <= ({rhs}))",
                    BinOpType.Lt => $"(({lhs}) < ({rhs}))",
                    BinOpType.Ge => $"(({lhs}) >= ({rhs}))",
                    BinOpType.Gt => $"(({lhs}) > ({rhs}))",
                    BinOpType.And => $"(({lhs}) && ({rhs}))",
                    BinOpType.Or => $"(({lhs}) || ({rhs}))",
                    _ => throw new Exception($"Unsupported BinOp Operatoion: {binOpExpr.Operation}"),
                };
            }
            else if(expr is EnumElemRefExpr refExpr)
            {
                if (simplified)
                {
                    return $"{refExpr.Value.Name}";
                }
                else
                {
                    return $"{Types.JavaTypeFor(refExpr.Type).TypeName}.{refExpr.Value.Name}";
                }
            }
            else
            {
                throw new Exception($"Unsupported expression type {expr.GetType()}");
            }
        }

        private string GenerateCodeCall(string callee, params string[] args)
        {
            return $"{callee}({string.Join(", ", args)})";
        }

        private string GenerateCodeTupleAccess(TupleAccessExpr t)
        {
            return $"{GenerateCodeExpr(t.SubExpr)}[{t.FieldNo}]";
        }

        private string GenerateCodeNamedTupleAccess(NamedTupleAccessExpr n, bool simplified = false)
        {
            // if (n.SubExpr is VariableAccessExpr v && v.Variable is PEventVariable)
            // {
            return $"{GenerateCodeExpr(n.SubExpr, simplified)}.{n.FieldName}";
            // }
            // return GenerateJSONObjectGet(GenerateCodeExpr(n.SubExpr), n.FieldName, n.Type.Canonicalize());
        }

        private string GenerateCodePredicateCall(PredicateCallExpr p, bool simplified = false)
        {
            if (p.Predicate is BuiltinPredicate)
            {
                if (p.Predicate is MacroPredicate macro)
                {
                    throw new Exception($"Unexpected MacroPredicate {macro.Name}: should be unfolded already!");
                }
                else 
                {
                    switch (p.Predicate.Notation)
                    {
                        case Notation.Infix:
                            if (p.Predicate.Name == "==")
                            {
                                if (simplified)
                                {
                                    return $"({GenerateCodeExpr(p.Arguments[0], simplified)} == {GenerateCodeExpr(p.Arguments[1], simplified)})";
                                }
                                if (p.Arguments[0].Type is SequenceType || p.Arguments[0].Type is SetType)
                                {
                                    return $"Arrays.equals({GenerateCodeExpr(p.Arguments[0], simplified)}, {GenerateCodeExpr(p.Arguments[1], simplified)})";
                                }
                                return $"Objects.equals({GenerateCodeExpr(p.Arguments[0], simplified)}, {GenerateCodeExpr(p.Arguments[1], simplified)})";
                            }
                            return $"{GenerateCodeExpr(p.Arguments[0], simplified)} {p.Predicate.Name} {GenerateCodeExpr(p.Arguments[1], simplified)}";
                    }
                }
            }
            else if (p.Predicate is DefinedPredicate)
            {
                var coercedArgs = p.Arguments.Select(x => {
                    if (simplified)
                    {
                        return GenerateCodeExpr(x, simplified);
                    }
                    switch (x.Type)
                    {
                        case SequenceType s: return $"(new ArrayList<{Types.JavaTypeFor(s.ElementType).ReferenceTypeName}>(Arrays.asList({GenerateCodeExpr(x)})))";
                        case SetType s: return $"(new HashSet<{Types.JavaTypeFor(s.ElementType).ReferenceTypeName}>(Set.of({GenerateCodeExpr(x)})))";
                        default: return GenerateCodeExpr(x);
                    }
                }).ToArray();
                return GenerateCodeCall(p.Predicate.Name, coercedArgs);
            }
            var args = (from e in p.Arguments select GenerateCodeExpr(e, simplified)).ToArray();
            return GenerateCodeCall(p.Predicate.Name, args);
        }

        private string GenerateFuncCall(FunCallExpr funCallExpr, bool simplified = false)
        {
            if (funCallExpr.Function is BuiltinFunction builtinFun)
            {
                switch (builtinFun.Notation)
                {
                    case Notation.Infix:
                        if (builtinFun.Name == "==")
                        {
                            if (simplified)
                            {
                                return $"({GenerateCodeExpr(funCallExpr.Arguments[0])} == {GenerateCodeExpr(funCallExpr.Arguments[1])})";
                            }
                            return $"Objects.equals({GenerateCodeExpr(funCallExpr.Arguments[0])}, {GenerateCodeExpr(funCallExpr.Arguments[1])})";
                        }
                        return $"({GenerateCodeExpr(funCallExpr.Arguments[0])} {builtinFun.Name} {GenerateCodeExpr(funCallExpr.Arguments[1])})";
                    default:
                        break;
                }
                if (funCallExpr.Function.Name == "index")
                {
                    if (funCallExpr.Arguments[0] is VariableAccessExpr v && v.Variable is PEventVariable pv)
                    {
                        return $"{pv.Name}.index()";
                    }
                    throw new NotImplementedException("Index is not implemented for expressions other than a variable access");
                }
                if (funCallExpr.Function.Name == "size")
                {
                    if (simplified)
                    {
                        return $"size({GenerateCodeExpr(funCallExpr.Arguments[0])})";
                    }
                    if (funCallExpr.Arguments[0].Type is SequenceType || funCallExpr.Arguments[0].Type is SetType)
                    {
                        return $"{GenerateCodeExpr(funCallExpr.Arguments[0])}.length";
                    }
                    return $"{GenerateCodeExpr(funCallExpr.Arguments[0])}.size()";
                }
            }
            return $"{funCallExpr.Function.Name}(" + string.Join(", ", (from e in funCallExpr.Arguments select GenerateCodeExpr(e)).ToArray()) + ")";
        }

        private static string GenerateCodeVariable(Variable v, bool simplified = false)
        {
            if (v is PEventVariable eVar)
            {
                if (simplified)
                {
                    return $"{eVar.Name}.payload";
                }
                return $"{eVar.Name}.getPayload()";
            }
            return $"{v.Name}.payload";
        }
    }
}
