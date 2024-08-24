using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{

    public class ASTComparer : IEqualityComparer<IPExpr>
    {
        public bool Equals(IPExpr x, IPExpr y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            if (!x.Type.IsAssignableFrom(y.Type) || !y.Type.IsAssignableFrom(x.Type))
            {
                return false;
            }
            switch ((x, y))
            {
                case (VariableAccessExpr ex, VariableAccessExpr ey):
                    return ex.Variable.Name.Equals(ey.Variable.Name);
                case (EnumElemRefExpr ex, EnumElemRefExpr ey):
                    return ex.Value.Name.Equals(ey.Value.Name);
                case (IntLiteralExpr ex, IntLiteralExpr ey):
                    return ex.Value == ey.Value;
                case (BoolLiteralExpr ex, BoolLiteralExpr ey):
                    return ex.Value == ey.Value;
                case (FloatLiteralExpr ex, FloatLiteralExpr ey):
                    return Math.Abs(ex.Value - ey.Value) <= 1e-7;
                case (TupleAccessExpr ex, TupleAccessExpr ey):
                    return ex.FieldNo == ey.FieldNo && Equals(ex.SubExpr, ey.SubExpr);
                case (BinOpExpr ex, BinOpExpr ey):
                    return ex.Operation.Equals(ey.Operation) && Equals(ex.Lhs, ey.Lhs) && Equals(ex.Rhs, ey.Rhs);
                case (UnaryOpExpr ex, UnaryOpExpr ey):
                    return ex.Operation.Equals(ey.Operation) && Equals(ex.SubExpr, ey.SubExpr);
                case (NamedTupleAccessExpr ex, NamedTupleAccessExpr ey):
                    return ex.FieldName.Equals(ey.FieldName) && Equals(ex.SubExpr, ey.SubExpr);
                case (FunCallExpr ex, FunCallExpr ey):
                    return ex.Function.Name.Equals(ey.Function.Name) && ex.Arguments.Count == ey.Arguments.Count
                            && ex.Arguments.Select(x => x.Type.Canonicalize()).Zip(ey.Arguments.Select(x => x.Type.Canonicalize()), Equals).All(b => b) && ex.Function.Signature.ReturnType.Equals(ey.Function.Signature.ReturnType);
                case (DefinedPredicate ex, DefinedPredicate ey):
                    return ex.Name.Equals(ey.Name) && ex.Function.Signature.ParameterTypes.Zip(ey.Function.Signature.ParameterTypes, Equals).All(x => x);
                case (BuiltinPredicate ex, BuiltinPredicate ey):
                    return ex.Name.Equals(ey.Name) && ex.Function.Signature.ParameterTypes.Zip(ey.Function.Signature.ParameterTypes, Equals).All(x => x);
                default:
                    if (x.Type.Equals(y.Type))
                    {
                        throw new NotImplementedException($"Not implemented for {x} vs {y}");
                    }
                    return false;
            }
        }

        public int GetHashCode([DisallowNull] IPExpr obj)
        {
            switch (obj)
            {
                case VariableAccessExpr ex:
                    return ex.Variable.Name.GetHashCode();
                case IntLiteralExpr ex:
                    return ex.Value.GetHashCode();
                case EnumElemRefExpr e:
                    return e.Value.GetHashCode();
                case BoolLiteralExpr ex:
                    return ex.Value.GetHashCode();
                case FloatLiteralExpr ex:
                    return ex.Value.GetHashCode();
                case TupleAccessExpr ex:
                    return (ex.FieldNo, GetHashCode(ex.SubExpr)).GetHashCode();
                case BinOpExpr ex:
                    return (ex.Operation, GetHashCode(ex.Lhs), GetHashCode(ex.Rhs)).GetHashCode();
                case UnaryOpExpr ex:
                    return (ex.Operation, GetHashCode(ex.SubExpr)).GetHashCode();
                case NamedTupleAccessExpr ex:
                    string nta_s = $"{GetHashCode(ex.SubExpr)}.{ex.FieldName.GetHashCode()}";
                    return nta_s.GetHashCode();
                case FunCallExpr ex:
                    string funCall_s = $"{ex.Function.Name}({string.Join(",", ex.Arguments.Select(GetHashCode))})";
                    return funCall_s.GetHashCode();
                case DefinedPredicate p:
                    return (p.Name, p.Function.Signature).GetHashCode();
                case BuiltinPredicate p:
                    return (p.Name, p.Function.Signature).GetHashCode();
                default:
                    throw new NotImplementedException($"Not implemented for {obj}");
            }
        }
    }

    public class PEventVariable : Variable
    {
        public PEventVariable(string name) : base(name, null, VariableRole.Temp) {}

        public string EventName => EventDecl.Name;
        public int Order { get; set; }
        public PEvent EventDecl { get; set; }
    }

    public class TypeVar : PLanguageType
    {

        public TypeVar(string name) : base(TypeKind.Base)
        {
            Name = $"{name}_{NextId++}";
            AllowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() => []);
        }
        public override string OriginalRepresentation => Name;

        public override string CanonicalRepresentation => Name;

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions { get; }

        public override PLanguageType Canonicalize()
        {
            return this;
        }

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            if (otherType is TypeVar other)
            {
                return Name == other.Name;
            }
            return false;
        }

        public string Name { get; }
        private static int NextId = 0;
    }
    public enum Notation {
        Prefix, Infix
    }
    public interface IPredicate : IPExpr
    {
        public Notation Notation { get; }
        public FunctionSignature Signature { get; }
        public string Name { get; }
        public Function Function { get; }
    }

    public class PredicateCallExpr : FunCallExpr
    {
        private PredicateCallExpr(IPredicate predicate, IReadOnlyList<IPExpr> arguments) : base(null, predicate.Function, arguments)
        {
            Predicate = predicate;
        }

        public static bool MkEqualityComparison(IPExpr lhs, IPExpr rhs, out IPExpr predicateCallExpr)
        {
            if (lhs.Type is Index && rhs.Type is Index) {
                predicateCallExpr = null;
                return false;
            }
            return MkPredicateCall(PredicateStore.EqPredicate, [lhs, rhs], out predicateCallExpr);
        }

        public static bool MkPredicateCall(string predicateName, IReadOnlyList<IPExpr> arguments, out IPExpr predicateCall)
        {
            // Console.WriteLine($"Mk predicate call with {predicateName} and {string.Join(", ", arguments.Select(x => x.Type).ToList())}");
            if (predicateName.Equals("==") && MkEqualityComparison(arguments[0], arguments[1], out predicateCall))
            {
                return true;
            }
            if (PredicateStore.TryGetPredicate(arguments.Select(x => x.Type).ToList(), predicateName, out var predicate))
            {
                return MkPredicateCall(predicate, arguments, out predicateCall);
            }
            predicateCall = null;
            return false;
        }

        public static bool MkPredicateCall(IPredicate predicate, IReadOnlyList<IPExpr> arguments, out IPExpr predicateCall)
        {
            predicateCall = null;
            if (predicate.Function.Signature.Parameters.Count != arguments.Count)
            {
                throw new Exception($"Predicate {predicate.Name} expects {predicate.Function.Signature.Parameters.Count} arguments, but got {arguments.Count}");
            }
            if (predicate is BuiltinPredicate)
            {
                if ((predicate.Function.Name == "<" || predicate.Function.Name == ">" || predicate.Function.Name == "==") && arguments[0] == arguments[1])
                {
                    return false;
                }
            }
            if (predicate is MacroPredicate macro)
            {
                predicateCall = macro.Unfold([.. arguments]);
                return true;
            }
            predicateCall = new PredicateCallExpr(predicate, arguments);
            return true;
        }

        public IPredicate Predicate { get; }
    }

    public class BuiltinPredicate : IPredicate
    {
        private List<VariableAccessExpr> funcParams;
        public BuiltinPredicate(string name, Notation notation, params PLanguageType[] signature)
        {
            Notation = notation;
            int i = 0;
            Function = new Function(name, null);
            funcParams = [];
            foreach (var type in signature)
            {
                var paramVar = new Variable($"x{i++}", null, VariableRole.Param) { Type = type };
                Function.Signature.Parameters.Add(paramVar);
                funcParams.Add(new VariableAccessExpr(null, paramVar));
            }
            Function.Signature.ReturnType = PrimitiveType.Bool;
        }
        public IPExpr ShiftCall(Func<List<VariableAccessExpr>, IPExpr> make)
        {
            return make(funcParams);
        }
        public string Name => Function.Name;
        public Notation Notation { get; }
        public FunctionSignature Signature => Function.Signature;
        public Function Function { get; }
        public PLanguageType Type => PrimitiveType.Bool;
        public ParserRuleContext SourceLocation => null;
    }

    public class MacroPredicate : BuiltinPredicate
    {
        private Func<IPExpr[], IPExpr> UnfoldMacro;
        internal MacroPredicate(string name, Notation notation, Func<IPExpr[], IPExpr> unfold, params PLanguageType[] signature) : base(name, notation, signature)
        {
            UnfoldMacro = unfold;
        }

        internal IPExpr Unfold(IPExpr[] parameters)
        {
            return UnfoldMacro(parameters);
        }
    }

    public class PInferBuiltinTypes
    {
        public static readonly PLanguageType Index = new TypeDefType(new TypeDef("Index", null) { Type = PrimitiveType.Int });
        public static readonly PLanguageType CollectionSize = new TypeDefType(new TypeDef("CollectionSize", null) { Type = PrimitiveType.Int });
    }

    public class DefinedPredicate : IPredicate
    {
        public DefinedPredicate(Function func)
        {
            if (func.Signature.ReturnType != PrimitiveType.Bool)
            {
                throw new Exception($"Function {func.Name} does not return a boolean, which is required to be a predicate");
            }
            Function = func;
        }

        public Function Function { get; }
        public PLanguageType Type => PrimitiveType.Bool;
        public ParserRuleContext SourceLocation => Function.SourceLocation;
        public FunctionSignature Signature => Function.Signature;
        public string Name => Function.Name;
        public Notation Notation => Notation.Prefix;
    }

    public class BuiltinFunction : Function
    {
        public static Function IndexOf = new BuiltinFunction("index", Notation.Prefix, PrimitiveType.Any, PInferBuiltinTypes.Index);
        public BuiltinFunction(string name, Notation notation, params PLanguageType[] types) : base(name, null)
        {
            Notation = notation;
            int i = 0;
            foreach (var type in types[..^1])
            {
                Signature.Parameters.Add(new Variable($"x{i++}", null, VariableRole.Param) { Type = type });
            }
            Signature.ReturnType = types[^1];
            // Console.WriteLine($"{name}: {types[..^1]} -> {types[^1]}");
        }

        public Notation Notation { get; }
    }

    public class Index : PLanguageType
    {

        public Index() : base(TypeKind.Data)
        {
        }

        public override string OriginalRepresentation => "index";

        public override string CanonicalRepresentation => "index";

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions => null;

        public override PLanguageType Canonicalize()
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            return obj is Index;
        }

        public override int GetHashCode()
        {
            return "$PlangType$$IndexType$".GetHashCode();
        }

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return otherType is Index;
        }
    }

    public class CollectionSize : PLanguageType
    {
        public CollectionSize() : base(TypeKind.Data)
        {
        }

        public override string OriginalRepresentation => "ContainerSize";

        public override string CanonicalRepresentation => OriginalRepresentation;

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions => null;

        public override PLanguageType Canonicalize()
        {
            return this;
        }

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return otherType is CollectionSize;
        }
    }

    internal class SignatureComparer : IEqualityComparer<List<PLanguageType>>
    {
        public bool Equals(List<PLanguageType> x, List<PLanguageType> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }
            for (int i = 0; i < x.Count; i++)
            {
                if (!PInferPredicateGenerator.IsAssignableFrom(x[i], y[i]) || !PInferPredicateGenerator.IsAssignableFrom(y[i], x[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode([DisallowNull] List<PLanguageType> obj)
        {
            return string.Join(", ", obj.Select(x => x.GetHashCode())).GetHashCode();
        }
    }

    public static class PredicateStore
    {
        private static readonly Dictionary<List<PLanguageType>, Dictionary<string, IPredicate>> _Store = new Dictionary<List<PLanguageType>, Dictionary<string, IPredicate>>(new SignatureComparer());
        public static readonly IPredicate EqPredicate = new MacroPredicate("==", Notation.Infix, args => {
            return new BinOpExpr(null, BinOpType.Eq, args[0], args[1]);
        }, [PrimitiveType.Any, PrimitiveType.Any]);
        private static readonly Dictionary<IPredicate, HashSet<IPredicate>> ContradictionsMap = new Dictionary<IPredicate, HashSet<IPredicate>>(new ASTComparer());

        private static void MarkContradition(IPredicate p1, IPredicate p2)
        {
            if (!ContradictionsMap.ContainsKey(p1))
            {
                ContradictionsMap.Add(p1, new HashSet<IPredicate>());
            }
            if (!ContradictionsMap.ContainsKey(p2))
            {
                ContradictionsMap.Add(p2, new HashSet<IPredicate>());
            }
            ContradictionsMap[p1].Add(p2);
            ContradictionsMap[p2].Add(p1);
        }

        public static void Reset()
        {
            _Store.Clear();
            ContradictionsMap.Clear();
        }
        
        public static IEnumerable<IPredicate> GetContradictions(IPredicate predicate)
        {
            if (ContradictionsMap.TryGetValue(predicate, out var contradictions))
            {
                return contradictions;
            }
            return [];
        }

        public static void AddBuiltinPredicate(BuiltinPredicate predicateInst, IEnumerable<IPredicate> contraditions)
        {
            var types = predicateInst.Signature.ParameterTypes.ToList();
            if (!_Store.ContainsKey(types))
            {
                _Store.Add(types, []);
            }
            _Store[types].Add(predicateInst.Name, predicateInst);
            foreach (var c in contraditions)
            {
                MarkContradition(predicateInst, c);
            }
        }

        public static MacroPredicate AddBuiltinPredicate(string name, Notation notation, Func<IPExpr[], IPExpr> unfold, IEnumerable<IPredicate> contraditions, params PLanguageType[] argTypes)
        {
            var parameterTypes = argTypes.ToList();
            if (!_Store.ContainsKey(parameterTypes))
            {
                _Store.Add(parameterTypes, []);
            }
            var pred = new MacroPredicate(name, notation, unfold, argTypes);
            _Store[parameterTypes].Add(name, pred);
            foreach (var c in contraditions)
            {
                MarkContradition(pred, c);
            }
            return pred;
        }

        public static void AddPredicate(IPredicate predicate, IEnumerable<IPredicate> contraditions)
        {
            var parameterTypes = predicate.Signature.Parameters.Select(p => p.Type).ToList();
            if (!_Store.ContainsKey(parameterTypes))
            {
                _Store.Add(parameterTypes, []);
            }
            // Console.WriteLine($"Add pred: {predicate.Name}, types: {string.Join(" -> ", parameterTypes)}");
            _Store[parameterTypes].Add(predicate.Name, predicate);
            foreach (var c in contraditions)
            {
                MarkContradition(predicate, c);
            }
        }

        public static void AddBinaryBuiltinPredicate(BinOpType op, PLanguageType lhs, PLanguageType rhs)
        {
            var opNameMap = new Dictionary<BinOpType, string> {
                {BinOpType.Lt, "<"},
                {BinOpType.Le, "<="},
                {BinOpType.Gt, ">"},
                {BinOpType.Ge, ">="},
                {BinOpType.Eq, "=="},
                {BinOpType.Neq, "!="},
            };
            var pred = AddBuiltinPredicate(opNameMap[op],
                                        Notation.Infix,
                                        args => new BinOpExpr(null, op, args[0], args[1]), [], lhs, rhs);
            var funLhs = new VariableAccessExpr(null, pred.Function.Signature.Parameters[0]);
            var funRhs = new VariableAccessExpr(null, pred.Function.Signature.Parameters[1]);
            foreach (var con in op.GetContradictions(funLhs, funRhs))
            {
                pred.Function.AddContradiction(con);
            }
            foreach (var equiv in op.GetEquivalences(funLhs, funRhs))
            {
                pred.Function.AddEquiv(equiv);
            }
            foreach (var prop in op.GetProperties())
            {
                pred.Function.Property |= prop;
            }
        }

        public static void Initialize(Scope globalScope) {
            List<PLanguageType> numericTypes = [PInferBuiltinTypes.Index, PInferBuiltinTypes.CollectionSize];
            
            foreach (var numType in numericTypes)
            {
                AddBinaryBuiltinPredicate(BinOpType.Lt, numType, numType);
            }

            foreach (var (op, types) in globalScope.AllowedBinOps)
            {
                if (op.GetKind() == BinOpKind.Equality || op.GetKind() == BinOpKind.Comparison)
                {
                    foreach (var sig in types)
                    {
                        AddBinaryBuiltinPredicate(op, sig.Item1, sig.Item2);
                    }
                }
            }
        }

        public static bool TryGetPredicate(List<PLanguageType> types, string predName, out IPredicate pred)
        {
            if (_Store.TryGetValue(types, out var predicates))
            {
                if (predicates.TryGetValue(predName, out pred))
                {
                    return true;
                }
                return false;
            }
            pred = null;
            return false;
        }

        public static IEnumerable<IPredicate> Store => _Store.Values.SelectMany(x => x.Values);
    }

    public static class FunctionStore
    {
        public static Dictionary<List<PLanguageType>, Dictionary<string, Function>> _Store = [];

        public static IPExpr Subst(IPExpr e, IDictionary<Variable, IPExpr> gamma)
        {
            IPExpr subst(IPExpr x) => Subst(x, gamma);
            return e switch {
                VariableAccessExpr variableAccessExpr when gamma.ContainsKey(variableAccessExpr.Variable) => gamma[variableAccessExpr.Variable],
                FunCallExpr funCall => new FunCallExpr(funCall.SourceLocation, funCall.Function, funCall.Arguments.Select(subst).ToList()),
                BinOpExpr binOpExpr => new BinOpExpr(binOpExpr.SourceLocation, binOpExpr.Operation, subst(binOpExpr.Lhs), subst(binOpExpr.Rhs)),
                NamedTupleAccessExpr ntAccess => new NamedTupleAccessExpr(ntAccess.SourceLocation, subst(ntAccess.SubExpr), ntAccess.Entry),
                TupleAccessExpr tupleAccess => new TupleAccessExpr(tupleAccess.SourceLocation, subst(tupleAccess.SubExpr), tupleAccess.FieldNo, tupleAccess.Type),
                UnaryOpExpr unaryOpExpr => new UnaryOpExpr(unaryOpExpr.SourceLocation, unaryOpExpr.Operation, subst(unaryOpExpr.SubExpr)),
                _ => e
            };
        }

        public static List<IPExpr> MakeContradictions(Function f, Func<Function, IPExpr[], IPExpr> make, params IPExpr[] parameters)
        {
            List<IPExpr> result = [];
            Dictionary<Variable, IPExpr> delta = [];
            if (f.Property.HasFlag(FunctionProperty.AntiSymmetric))
            {
                result.Add(make(f, [parameters[1], parameters[0]]));
            }
            foreach (var (x, y) in f.Signature.Parameters.Zip(parameters))
            {
                delta[x] = y;
            }
            foreach (IPExpr con in f.Contradictions)
            {
                result.Add(Subst(con, delta));
            }
            return result;
        }

        public static List<IPExpr> MakeEquivalences(Function f, Func<Function, IPExpr[], IPExpr> make, params IPExpr[] parameters)
        {
            List<IPExpr> result = [];
            Dictionary<Variable, IPExpr> delta = [];
            if (f.Property.HasFlag(FunctionProperty.Symmetric))
            {
                result.Add(make(f, [parameters[1], parameters[0]]));
            }
            foreach (var (x, y) in f.Signature.Parameters.Zip(parameters))
            {
                delta[x] = y;
            }
            foreach (IPExpr eq in f.Equivalences)
            {
                // need to subst parameters
                result.Add(Subst(eq, delta));
            }
            return result;
        }

        public static void AddFunction(Function func)
        {
            var parameterTypes = func.Signature.ParameterTypes.ToList();
            if (!_Store.ContainsKey(parameterTypes))
            {
                _Store.Add(parameterTypes, []);
            }
            // Console.WriteLine($"Add func: {func.Name}, types: {string.Join(" -> ", parameterTypes)}");
            _Store[parameterTypes].Add(func.Name, func);
        }

        public static void Reset()
        {
            _Store.Clear();
        }

        public static void Initialize(Scope globalScope) {
            List<string> funcs = ["+", "-", "*", "/", "%"];
            var opMap = new Dictionary<string, BinOpType> {
                {"+", BinOpType.Add},
                {"-", BinOpType.Sub},
                {"*", BinOpType.Mul},
                {"/", BinOpType.Div},
                {"%", BinOpType.Mod},
            };
            var allowedBinOps = globalScope.AllowedBinOps;
            foreach (var func in funcs)
            {
                if (allowedBinOps.TryGetValue(opMap[func], out var allowSet))
                {
                    foreach (var (lhs, rhs, ret) in allowSet)
                    {
                        var f = new BuiltinFunction(func, Notation.Infix, lhs, rhs, ret);
                        if (func == "+" || func == "*") f.Property |= FunctionProperty.Symmetric;
                    }
                }
            }
            List<PLanguageType> containerTypes = [new SequenceType(PrimitiveType.Any),
                                                  new SetType(PrimitiveType.Any),
                                                  new MapType(PrimitiveType.Any, PrimitiveType.Any)];
            foreach (var containerType in containerTypes)
            {
                AddFunction(new BuiltinFunction("size", Notation.Prefix, containerType, PInferBuiltinTypes.CollectionSize));
            }
        }

        public static IEnumerable<Function> Store => _Store.Values.SelectMany(x => x.Values);
    }

    public class CongruenceClosure
    {
        public enum Result
        {
            YES, NO, UNK
        }
        internal class Node(object n, int id, bool canonical = false)
        {
            internal object symbol = n;
            internal List<Node> children = [];
            internal HashSet<Node> cc_parent = [];
            internal Node parent = null;
            internal int id = id;
            internal bool canonical = canonical;
            internal IPExpr repr;

            internal bool SameNode(Node other)
            {
                return other.id == id;
            }

            internal Node Find()
            {
                if (parent == null)
                {
                    return this;
                }
                return parent.Find();
            }
        }
        private readonly Dictionary<IPExpr, Node> index;


        private Node PutExpr(IPExpr e)
        {
            if (index.TryGetValue(e, out Node value))
            {
                return value;
            }
            // Console.WriteLine($"Put {e}");
            switch (e)
            {
                case VariableAccessExpr:
                case EnumElemRefExpr:
                case NamedTupleAccessExpr:
                case TupleAccessExpr:
                    Node ground = new(e, index.Count) { repr = e };
                    index[e] = ground;
                    return ground;
                case BinOpExpr expr:
                    List<Node> exprChildren = [PutExpr(expr.Lhs), PutExpr(expr.Rhs)];
                    Node binOpNode = new(expr.Operation, index.Count) { children = exprChildren, repr = e };
                    index[expr] = binOpNode;
                    foreach (var ch in exprChildren)
                    {
                        ch.cc_parent.Add(binOpNode);
                    }
                    return binOpNode;
                case UnaryOpExpr unaryOpExpr:
                    List<Node> unOpSubExpr = [PutExpr(unaryOpExpr.SubExpr)];
                    Node unOpNode = new(unaryOpExpr.Operation, index.Count) { children = unOpSubExpr, repr = e};
                    index[unaryOpExpr] = unOpNode;
                    unOpSubExpr[0].cc_parent.Add(unOpNode);
                    return unOpNode;
                case FunCallExpr funCallExpr:
                    Function f = funCallExpr.Function;
                    List<Node> funCallArgsNode = funCallExpr.Arguments.Select(PutExpr).ToList();
                    // Console.WriteLine("Called " + funCallExpr.Function.Name);
                    Node funCallNode = new(f, index.Count)
                    {
                        children = funCallArgsNode,
                        repr = e
                    };
                    foreach (var ch in funCallArgsNode)
                    {
                        ch.cc_parent.Add(funCallNode);
                    }
                    index[funCallExpr] = funCallNode;
                    return funCallNode;
                case IntLiteralExpr intLiteral:
                    Node intLitNode = new(intLiteral.Value, index.Count) { repr = intLiteral };
                    return intLitNode;
            }
                throw new Exception($"CC not supported for {e}");
        }

        private void PropagateCongruence(Node n1, Node n2)
        {
            if (n1.symbol.Equals(n2.symbol) && n1.children.Count == n2.children.Count)
            {
                if (n1.children.Zip(n2.children, SameClosure).All(x => x))
                {
                    // all corresponding children are in the same closure
                    Union(n1, n2);
                }
            }
        }

        private bool SameClosure(Node n1, Node n2)
        {
            return n1.Find().SameNode(n2.Find());
        }

        private void Union(Node n1, Node n2)
        {
            n1 = n1.Find();
            n2 = n2.Find();
            if (n1.canonical)
            {
                // keep the root canonical
                (n1, n2) = (n2, n1);
            }
            if (!n1.SameNode(n2))
            {
                // Mark same UF
                n1.parent = n2;
                // propagate congruence
                foreach (var p1 in n1.cc_parent)
                {
                    foreach (var p2 in n2.cc_parent)
                    {
                        PropagateCongruence(p1, p2);
                    }
                }
                // propagate common congruence parents
                foreach (var cpar in n1.cc_parent)
                {
                    n2.cc_parent.Add(cpar);
                }
                n1.cc_parent.Clear();
            }
        }

        public IEnumerable<IPExpr> AddedExprs()
        {
            return index.Keys;
        }

        public void MarkEquivalence(IPExpr e1, IPExpr e2)
        {
            if (!index.TryGetValue(e1, out Node n1))
            {
                throw new Exception($"Lhs expr: {e1} has not added");
            }
            if (!index.TryGetValue(e2, out Node n2))
            {
                throw new Exception($"Rhs expr: {e2} has not added");
            }
            Union(n1, n2);
        }

        public void Reset()
        {
            index.Clear();
        }

        public Result Equivalent(IPExpr e1, IPExpr e2)
        {
            if (!index.TryGetValue(e1, out Node n1) || !index.TryGetValue(e2, out Node n2))
            {
                return Result.UNK;
            }
            if (SameClosure(n1, n2)) return Result.YES;
            return Result.NO;
        }

        public bool HasExpr(IPExpr e)
        {
            return Canonicalize(e) != null;
        }

        public IPExpr Canonicalize(IPExpr e)
        {
            if (index.TryGetValue(e, out var node))
            {
                Node par = node.Find();
                return par.repr;
            }
            return null;
        }

        public bool DefinitelyEquivalent(IPExpr e1, IPExpr e2)
        {
            return Equivalent(e1, e2) == Result.YES;
        }

        public CongruenceClosure() {
            index = new(new ASTComparer());
        }

        public void AddExpr(IPExpr e, bool canonical = false)
        {
            if (!canonical)
            {
                if (!HasExpr(e))
                {
                    PutExpr(e).canonical = false;
                }
            }
            else
            {
                PutExpr(e).canonical = true;
            }
        }
    }
}
