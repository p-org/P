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
                    return ex.Function.Equals(ey.Function) && ex.Arguments.Count == ey.Arguments.Count && ex.Arguments.Zip(ey.Arguments, Equals).All(b => b);
                default:
                    if (x.GetType().Equals(y.GetType()))
                    {
                        throw new NotImplementedException();
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
                case BoolLiteralExpr ex:
                    return ex.Value.GetHashCode();
                case FloatLiteralExpr ex:
                    return ex.Value.GetHashCode();
                case TupleAccessExpr ex:
                    return (ex.FieldNo, ex.SubExpr).GetHashCode();
                case BinOpExpr ex:
                    return (ex.Operation, ex.Lhs, ex.Rhs).GetHashCode();
                case UnaryOpExpr ex:
                    return (ex.Operation, ex.SubExpr).GetHashCode();
                case NamedTupleAccessExpr ex:
                    return (ex.FieldName, ex.SubExpr).GetHashCode();
                case FunCallExpr ex:
                    return (ex.Function, ex.Arguments).GetHashCode();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class PEventVariable : Variable
    {
        public PEventVariable(string name, string eventName) : base(name, null, VariableRole.Temp)
        {
            EventName = eventName;
        }

        public string EventName { get; }
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
        public PredicateCallExpr(IPredicate predicate, IReadOnlyList<IPExpr> arguments) : base(null, predicate.Function, arguments)
        {
            Predicate = predicate;
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

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return otherType is Index;
        }
    }


    public static class PredicateStore
    {
        public static List<IPredicate> Store = MkBuiltin();

        public static void AddBuiltinPredicate(string name, Notation notation, params PLanguageType[] argTypes)
        {
            Store.Add(new BuiltinPredicate(name, notation, argTypes));
        }

        private static IPredicate BinaryPredicate(string name, PLanguageType type)
        {
            return new BuiltinPredicate(name, Notation.Prefix, type, type);
        }

        private static List<IPredicate> MkBuiltin() {
            List<PLanguageType> numericTypes = [PrimitiveType.Int, PrimitiveType.Float, PInferBuiltinTypes.Index];
            var ltInst = from type in numericTypes
                            select BinaryPredicate("<", type);
            var gtInst = from type in numericTypes
                            select BinaryPredicate(">", type);
            return ltInst.Concat(gtInst).ToList();
        }
    }

    public static class FunctionStore
    {
        public static List<Function> Store = MkBuiltin();

        public static Function BinaryFunction(string name, PLanguageType type)
        {
            return new BuiltinFunction(name, Notation.Prefix, type, type, type);
        }

        private static List<Function> MkBuiltin() {
            List<string> funcs = ["+", "-", "*", "/", "%"];
            List<PLanguageType> numericTypes = [PrimitiveType.Int, PrimitiveType.Float];
            var arith = from ty in numericTypes
                                from op in funcs
                                    select BinaryFunction(op, ty);
            return arith.ToList();
        }
    }
}