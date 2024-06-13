using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{

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
    }

    public class BuiltinPredicate : IPredicate
    {
        public BuiltinPredicate(string name, Notation notation, params PLanguageType[] signature)
        {
            Name = name;
            Notation = notation;
            Signature = new FunctionSignature();
            int i = 0;
            foreach (var type in signature)
            {
                Signature.Parameters.Add(new Variable($"x{i++}", null, VariableRole.Param) { Type = type });
            }
            Signature.ReturnType = PrimitiveType.Bool;
        }
        public string Name { get; }
        public Notation Notation { get; }
        public FunctionSignature Signature { get; }
        public PLanguageType Type => PrimitiveType.Bool;
        public ParserRuleContext SourceLocation => null;
    }

    public class DefinedPredicate : IPredicate
    {
        public DefinedPredicate(Function func)
        {
            if ((func.Role & FunctionRole.Predicate) != FunctionRole.Predicate)
            {
                throw new Exception($"Function {func.Name} is not defined as a predicate");
            }
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

        private static IPredicate BinaryPredicate(string name, PLanguageType type)
        {
            return new BuiltinPredicate(name, Notation.Infix, type, type);
        }

        private static List<IPredicate> MkBuiltin() {
            List<PLanguageType> numericTypes = [PrimitiveType.Int, PrimitiveType.Float];
            List<PLanguageType> comparisonTypes = [PrimitiveType.Int, PrimitiveType.Float, PrimitiveType.Bool, PrimitiveType.String, PrimitiveType.Machine];
            var ltInst = from type in numericTypes
                            select BinaryPredicate("<", type);
            var eqInst = from type in comparisonTypes
                            select BinaryPredicate("==", type);
            return ltInst.Concat(eqInst).Concat([BinaryPredicate("==", new TypeVar("T"))]).ToList();
        }
    }

    public static class FunctionStore
    {
        public static List<Function> Store = MkBuiltin();

        public static Function BinaryFunction(string name, PLanguageType type)
        {
            return new BuiltinFunction(name, Notation.Infix, type, type, type);
        }

        private static List<Function> MkBuiltin() {
            List<string> funcs = ["+", "-", "*", "/", "%"];
            List<PLanguageType> numericTypes = [PrimitiveType.Int, PrimitiveType.Float];
            var arith = from ty in numericTypes
                                from op in funcs
                                    select BinaryFunction(op, ty);
            return Enumerable.Concat(arith,
                            [new BuiltinFunction("index", Notation.Prefix,
                                                          PrimitiveType.Event,
                                                          new Index())]).ToList();
        }
    }
}