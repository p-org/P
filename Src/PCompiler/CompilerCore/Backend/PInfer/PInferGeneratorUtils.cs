using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Antlr4.Runtime;
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
            if (!x.Type.Equals(y.Type))
            {
                return false;
            }
            switch ((x, y))
            {
                case (VariableAccessExpr ex, VariableAccessExpr ey):
                    return ex.Variable.Name.Equals(ey.Variable.Name);
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
                            && ex.Arguments.Select(x => x.Type).Zip(ey.Arguments.Select(x => x.Type), Equals).All(b => b) && ex.Function.Signature.ReturnType.Equals(ey.Function.Signature.ReturnType);
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
                    return (ex.Variable.Name, PInferPredicateGenerator.ShowType(ex.Type)).GetHashCode();
                case IntLiteralExpr ex:
                    return ex.Value.GetHashCode();
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
                    return (ex.FieldName, GetHashCode(ex.SubExpr)).GetHashCode();
                case FunCallExpr ex:
                    return (ex.Function.Name,
                            string.Join(" ", ex.Arguments.Select(x => PInferPredicateGenerator.ShowType(x.Type))) 
                                      + " " + PInferPredicateGenerator.ShowType(ex.Function.Signature.ReturnType),
                            string.Join(" ", ex.Arguments.Select(GetHashCode))).GetHashCode();
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

        public static bool MkEqualityComparison(IPExpr lhs, IPExpr rhs, out PredicateCallExpr predicateCallExpr)
        {
            if (lhs.Type is Index && rhs.Type is Index) {
                predicateCallExpr = null;
                return false;
            }
            predicateCallExpr = new PredicateCallExpr(PredicateStore.EqPredicate, [lhs, rhs]);
            return true;
        }

        public static bool MkPredicateCall(string predicateName, IReadOnlyList<IPExpr> arguments, out PredicateCallExpr predicateCall)
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

        public static bool MkPredicateCall(IPredicate predicate, IReadOnlyList<IPExpr> arguments, out PredicateCallExpr predicateCall)
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
            predicateCall = new PredicateCallExpr(predicate, arguments);
            return true;
        }

        public IPredicate Predicate { get; }
    }

    public class BuiltinPredicate : IPredicate
    {
        public BuiltinPredicate(string name, Notation notation, params PLanguageType[] signature)
        {
            Notation = notation;
            int i = 0;
            Function = new Function(name, null);
            foreach (var type in signature)
            {
                Function.Signature.Parameters.Add(new Variable($"x{i++}", null, VariableRole.Param) { Type = type });
            }
            Function.Signature.ReturnType = PrimitiveType.Bool;
        }
        public string Name => Function.Name;
        public Notation Notation { get; }
        public FunctionSignature Signature => Function.Signature;
        public Function Function { get; }
        public PLanguageType Type => PrimitiveType.Bool;
        public ParserRuleContext SourceLocation => null;
    }

    public class PInferBuiltinTypes
    {
        public static readonly PLanguageType Index = new Index();
        public static readonly PLanguageType CollectionSize = new CollectionSize();
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
                if (!x[i].Equals(y[i]))
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
        public static readonly IPredicate EqPredicate = new BuiltinPredicate("==", Notation.Infix, PrimitiveType.Any, PrimitiveType.Any);
        private static readonly Dictionary<IPredicate, HashSet<IPredicate>> ContraditionsMap = new Dictionary<IPredicate, HashSet<IPredicate>>(new ASTComparer());

        private static void MarkContradition(IPredicate p1, IPredicate p2)
        {
            if (!ContraditionsMap.ContainsKey(p1))
            {
                ContraditionsMap.Add(p1, new HashSet<IPredicate>());
            }
            if (!ContraditionsMap.ContainsKey(p2))
            {
                ContraditionsMap.Add(p2, new HashSet<IPredicate>());
            }
            ContraditionsMap[p1].Add(p2);
            ContraditionsMap[p2].Add(p1);
        }
        
        public static IEnumerable<IPredicate> GetContradictions(IPredicate predicate)
        {
            if (ContraditionsMap.TryGetValue(predicate, out var contradictions))
            {
                return contradictions;
            }
            return [];
        }

        public static void AddBuiltinPredicate(string name, Notation notation, IEnumerable<IPredicate> contraditions, params PLanguageType[] argTypes)
        {
            var parameterTypes = argTypes.ToList();
            if (!_Store.ContainsKey(parameterTypes))
            {
                _Store.Add(parameterTypes, []);
            }
            var pred = new BuiltinPredicate(name, notation, argTypes);
            _Store[parameterTypes].Add(name, pred);
            foreach (var c in contraditions)
            {
                MarkContradition(pred, c);
            }
        }

        public static void AddPredicate(IPredicate predicate, IEnumerable<IPredicate> contraditions)
        {
            var parameterTypes = predicate.Signature.Parameters.Select(p => p.Type).ToList();
            if (!_Store.ContainsKey(parameterTypes))
            {
                _Store.Add(parameterTypes, []);
            }
            _Store[parameterTypes].Add(predicate.Name, predicate);
            foreach (var c in contraditions)
            {
                MarkContradition(predicate, c);
            }
        }

        public static void Initialize() {
            List<PLanguageType> numericTypes = [PrimitiveType.Int, PrimitiveType.Float, PInferBuiltinTypes.Index];
            List<PLanguageType> containerTypes = [new SequenceType(PrimitiveType.Any),
                                                new SetType(PrimitiveType.Any),
                                                new MapType(PrimitiveType.Any, PrimitiveType.Any)];
            foreach (var numType in numericTypes)
            {
                AddBuiltinPredicate("<", Notation.Infix, [EqPredicate], numType, numType);
                // AddBuiltinPredicate("==", Notation.Infix, numType, numType);
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

        public static void AddFunction(Function func)
        {
            var parameterTypes = func.Signature.ParameterTypes.ToList();
            if (!_Store.ContainsKey(parameterTypes))
            {
                _Store.Add(parameterTypes, []);
            }
            _Store[parameterTypes].Add(func.Name, func);
        }

        public static void Initialize() {
            List<string> funcs = ["+", "-", "*", "/", "%"];
            List<PLanguageType> numericTypes = [PrimitiveType.Int, PrimitiveType.Float];
            foreach (var numTypes in numericTypes)
            {
                foreach (var func in funcs)
                {
                    AddFunction(new BuiltinFunction(func, Notation.Infix, numTypes, numTypes, numTypes));
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
}