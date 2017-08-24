using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class ExprVisitor : PParserBaseVisitor<IPExpr>
    {
        private readonly DeclarationTable table;

        public ExprVisitor(DeclarationTable table) { this.table = table; }

        public override IPExpr VisitPrimitiveExpr(PParser.PrimitiveExprContext context)
        {
            return Visit(context.primitive());
        }

        public override IPExpr VisitUnnamedTupleExpr(PParser.UnnamedTupleExprContext context)
        {
            return Visit(context.unnamedTupleBody());
        }

        public override IPExpr VisitNamedTupleExpr(PParser.NamedTupleExprContext context)
        {
            return Visit(context.namedTupleBody());
        }

        public override IPExpr VisitParenExpr(PParser.ParenExprContext context) { return Visit(context.expr()); }

        public override IPExpr VisitNamedTupleAccessExpr(PParser.NamedTupleAccessExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            if (!(subExpr.Type is NamedTupleType))
            {
                throw new TypeException(context.expr(), "expected named tuple type");
            }
            var tuple = (NamedTupleType) subExpr.Type;
            string fieldName = context.field.Text;
            if (!tuple.LookupEntry(fieldName, out var entry))
            {
                throw new TypeException(context, "invalid named tuple field");
            }
            return new NamedTupleAccessExpr(subExpr, fieldName, entry.Type);
        }

        public override IPExpr VisitTupleAccessExpr(PParser.TupleAccessExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            if (!(subExpr.Type is TupleType))
            {
                throw new TypeException(context.expr(), "expected tuple type");
            }
            var tuple = (TupleType) subExpr.Type;
            int fieldNo = int.Parse(context.field.Text);
            if (tuple.Types.Length >= fieldNo)
            {
                throw new TypeException(context, "tuple access out of bounds");
            }
            PLanguageType type = tuple.Types[fieldNo];
            return new TupleAccessExpr(subExpr, fieldNo, type);
        }

        public override IPExpr VisitSeqAccessExpr(PParser.SeqAccessExprContext context)
        {
            IPExpr seqExpr = Visit(context.seq);
            IPExpr indexExpr = Visit(context.index);
            if (!(seqExpr.Type is SequenceType))
            {
                throw new TypeException(context.seq, "expression must be sequence type");
            }
            if (indexExpr.Type != PrimitiveType.Int)
            {
                throw new TypeException(context.index, "index must be int type");
            }

            var type = (SequenceType) seqExpr.Type;
            return new SeqAccessExpr(seqExpr, indexExpr, type.ElementType);
        }

        public override IPExpr VisitKeywordExpr(PParser.KeywordExprContext context)
        {
            IPExpr expr;
            MapType mapType;
            switch (context.fun.Text)
            {
                case "keys":
                    expr = Visit(context.expr());
                    mapType = expr.Type as MapType ??
                              throw new TypeException(context.expr(), $"{context.fun.Text} expects map type");
                    return new KeysExpr(expr, new SequenceType(mapType.KeyType));
                case "values":
                    expr = Visit(context.expr());
                    mapType = expr.Type as MapType ??
                              throw new TypeException(context.expr(), $"{context.fun.Text} expects map type");
                    return new ValuesExpr(expr, new SequenceType(mapType.ValueType));
                case "sizeof":
                    expr = Visit(context.expr());
                    if (!(expr.Type is SequenceType) && !(expr.Type is MapType))
                    {
                        throw new TypeException(context.expr(), $"{context.fun.Text} expects map or seq type");
                    }
                    return new SizeofExpr(expr);
                case "default":
                    PLanguageType type = TypeResolver.ResolveType(context.type(), table);
                    return new DefaultExpr(type);
                default:
                    throw new ArgumentException($"Unknown keyword expression {context.fun.Text}", nameof(context));
            }
        }

        public override IPExpr VisitCtorExpr(PParser.CtorExprContext context)
        {
            string machineName = context.machineName.Text;
            if (!table.Lookup(machineName, out Machine machine))
            {
                if (!table.Lookup(machineName, out MachineProto proto))
                {
                    throw new NotImplementedException($"constructing machine prototypes ({proto.Name})");
                }
                throw new MissingDeclarationException(machineName, context);
            }

            var arguments = context.rvalueList().rvalue().Select(Visit).ToArray();
            if (machine.PayloadType == PrimitiveType.Null && arguments.Length != 0)
            {
                throw new TypeException(context, $"machine {machine.Name} expects no parameters, but 1 was given");
            }

            if (machine.PayloadType != PrimitiveType.Null && arguments.Length != 1)
            {
                throw new TypeException(
                                        context,
                                        $"machine {machine.Name} expects a single parameter, but {arguments.Length} were given");
            }

            if (machine.PayloadType != PrimitiveType.Null)
            {
                // check the argument types
                PLanguageType actualType = arguments[0].Type;
                PLanguageType expectedType = machine.PayloadType;
                if (!expectedType.IsAssignableFrom(actualType))
                {
                    throw new TypeException(
                                            context,
                                            $"{actualType.OriginalRepresentation} is not a subtype of {expectedType.OriginalRepresentation}");
                }
            }

            return new CtorExpr(machine, arguments);
        }

        public override IPExpr VisitFunCallExpr(PParser.FunCallExprContext context)
        {
            string funName = context.fun.Text;
            if (!table.Lookup(funName, out Function function))
            {
                if (table.Lookup(funName, out FunctionProto proto))
                {
                    throw new NotImplementedException($"function proto calls ({proto.Name})");
                }
                throw new MissingDeclarationException(funName, context);
            }

            // Check the arguments
            // TODO: linearly typed arguments
            var arguments = context.rvalueList().rvalue().Select(Visit).ToArray();
            for (var i = 0; i < arguments.Length; i++)
            {
                IPExpr argument = arguments[i];
                PLanguageType paramType = function.Signature.Parameters[i].Type;
                if (!paramType.IsAssignableFrom(argument.Type))
                {
                    throw new TypeException(context.rvalueList().rvalue(i),
                                            $"{argument.Type.OriginalRepresentation} is not a subtype of {paramType.OriginalRepresentation}");
                }
            }

            return new FunCallExpr(function, arguments);
        }

        public override IPExpr VisitUnaryExpr(PParser.UnaryExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            switch (context.op.Text)
            {
                case "-":
                    if (subExpr.Type != PrimitiveType.Int && subExpr.Type != PrimitiveType.Float)
                    {
                        throw new TypeException(context,
                                                $"sign negation only applies to ints and floats, not to {subExpr.Type.OriginalRepresentation}");
                    }
                    return new SignNegateExpr(subExpr);
                case "!":
                    if (subExpr.Type != PrimitiveType.Bool)
                    {
                        throw new TypeException(context,
                                                $"logical negation only applies to bools, not to {subExpr.Type.OriginalRepresentation}");
                    }
                    return new LogicalNegateExpr(subExpr);
                default:
                    throw new ArgumentException($"Unknown unary op `{context.op.Text}`", nameof(context));
            }
        }

        public override IPExpr VisitBinExpr(PParser.BinExprContext context)
        {
            IPExpr lhs = Visit(context.lhs);
            IPExpr rhs = Visit(context.rhs);
            string op = context.op.Text;

            var arithCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"*", (elhs, erhs) => new MultExpr(elhs, erhs)},
                {"/", (elhs, erhs) => new DivExpr(elhs, erhs)},
                {"+", (elhs, erhs) => new AddExpr(elhs, erhs)},
                {"-", (elhs, erhs) => new SubExpr(elhs, erhs)},
                {"<", (elhs, erhs) => new LessThanExpr(elhs, erhs)},
                {"<=", (elhs, erhs) => new LessEqualsExpr(elhs, erhs)},
                {">", (elhs, erhs) => new GreaterThanExpr(elhs, erhs)},
                {">=", (elhs, erhs) => new GreaterEqualsExpr(elhs, erhs)}
            };

            var logicCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"&&", (elhs, erhs) => new LogicalAndExpr(elhs, erhs)},
                {"||", (elhs, erhs) => new LogicalOrExpr(elhs, erhs)}
            };

            var compCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"==", (elhs, erhs) => new CompareSameExpr(elhs, erhs)},
                {"!=", (elhs, erhs) => new CompareDiffExpr(elhs, erhs)}
            };

            switch (op)
            {
                case "+":
                case "*":
                case "/":
                case "-":
                case "<":
                case ">":
                case ">=":
                case "<=":
                    if (lhs.Type == PrimitiveType.Int && rhs.Type == PrimitiveType.Int ||
                        lhs.Type == PrimitiveType.Float && rhs.Type == PrimitiveType.Float)
                    {
                        return arithCtors[op](lhs, rhs);
                    }
                    throw new TypeException(context,
                                            $"operator {op} expects operands to both be either int or float; instead got ({lhs.Type.OriginalRepresentation}, {rhs.Type.OriginalRepresentation})");
                case "in":
                    var rhsMap = rhs.Type as MapType;
                    if (rhsMap == null)
                    {
                        throw new TypeException(context, $"operator {op} requires a map type on the right hand side");
                    }
                    if (!rhsMap.KeyType.IsAssignableFrom(lhs.Type))
                    {
                        throw new TypeException(context,
                                                $"{lhs.Type.OriginalRepresentation} is not a subtype of the key type of {rhs.Type.OriginalRepresentation}");
                    }
                    return new ContainsKeyExpr(lhs, rhs);
                case "==":
                case "!=":
                    if (!lhs.Type.IsAssignableFrom(rhs.Type) && !rhs.Type.IsAssignableFrom(lhs.Type))
                    {
                        throw new TypeException(context, $"{op} comparison between two unrelated types");
                    }
                    return compCtors[op](lhs, rhs);
                case "&&":
                case "||":
                    if (lhs.Type == PrimitiveType.Bool && rhs.Type == PrimitiveType.Bool)
                    {
                        return logicCtors[op](lhs, rhs);
                    }
                    throw new TypeException(context, $"operator {op} expects both operands to be bools");
                default:
                    throw new ArgumentException($"unknown binary operation {op}", nameof(context));
            }
        }

        public override IPExpr VisitCastExpr(PParser.CastExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            PLanguageType newType = TypeResolver.ResolveType(context.type(), table);
            if (context.cast.Text.Equals("as"))
            {
                if (!newType.IsAssignableFrom(subExpr.Type) && !subExpr.Type.IsAssignableFrom(newType))
                {
                    throw new TypeException(context, $"impossible cast from {subExpr.Type.OriginalRepresentation} to {newType.OriginalRepresentation}");
                }
                return new CastExpr(subExpr, newType);
            }
            if (context.cast.Text.Equals("to"))
            {
                throw new NotImplementedException("to conversions");
            }
            throw new ArgumentException(nameof(context));
        }

        public override IPExpr VisitPrimitive(PParser.PrimitiveContext context)
        {
            if (context.Iden() != null)
            {
                string symbolName = context.Iden().GetText();
                if (table.Lookup(symbolName, out Variable variable))
                {
                    return new VariableAccessExpr(variable);
                }
                if (table.Lookup(symbolName, out EnumElem enumElem))
                {
                    return new EnumElemRefExpr(enumElem);
                }
                if (table.Lookup(symbolName, out PEvent evt))
                {
                    return new EventRefExpr(evt);
                }
                throw new MissingDeclarationException(symbolName, context);
            }
            if (context.floatLiteral() != null)
            {
                return VisitFloatLiteral(context.floatLiteral());
            }
            if (context.BoolLiteral() != null)
            {
                return new BoolLiteralExpr(context.BoolLiteral().GetText().Equals("true"));
            }
            if (context.IntLiteral() != null)
            {
                return new IntLiteralExpr(int.Parse(context.IntLiteral().GetText()));
            }
            if (context.NullLiteral() != null)
            {
                return new NullLiteralExpr();
            }
            if (context.NONDET() != null)
            {
                return new NondetExpr();
            }
            if (context.FAIRNONDET() != null)
            {
                return new FairNondetExpr();
            }
            if (context.HALT() != null)
            {
                bool success = table.Lookup("halt", out PEvent haltEvent);
                Debug.Assert(success);
                return new EventRefExpr(haltEvent);
            }
            if (context.THIS() != null)
            {
                RuleContext ctx = context;
                var hasMachineParent = false;
                while (ctx != null)
                {
                    if (ctx is PParser.ImplMachineDeclContext || ctx is PParser.SpecMachineDeclContext)
                    {
                        hasMachineParent = true;
                        break;
                    }
                    ctx = ctx.Parent;
                }
                if (!hasMachineParent)
                {
                    throw new TypeException(context, "keyword `this` must appear in machine");
                }
                string machineName = (ctx as PParser.ImplMachineDeclContext)?.name.Text ??
                                     (ctx as PParser.SpecMachineDeclContext)?.name.Text;
                if (!table.Lookup(machineName, out Machine machine))
                {
                    throw new ArgumentException($"Sanity check failed. Could not find machine {machineName}",
                                                nameof(context));
                }
                return new ThisRefExpr(machine);
            }

            throw new ArgumentException("unknown primitive", nameof(context));
        }

        public override IPExpr VisitUnnamedTupleBody(PParser.UnnamedTupleBodyContext context)
        {
            var fields = context._fields.Select(Visit).ToArray();
            var type = new TupleType(fields.Select(e => e.Type).ToArray());
            return new UnnamedTupleExpr(fields, type);
        }

        public override IPExpr VisitNamedTupleBody(PParser.NamedTupleBodyContext context)
        {
            var fields = context._values.Select(Visit).ToArray();
            var entries = context._names.Zip(fields,
                                             (fieldName, value) => new NamedTupleEntry
                                             {
                                                 Name = fieldName.Text,
                                                 Type = value.Type
                                             })
                                 .ToArray();
            var type = new NamedTupleType(entries);
            return new NamedTupleExpr(fields, type);
        }

        public override IPExpr VisitExpFloat(PParser.ExpFloatContext context)
        {
            throw new NotImplementedException("float(x,y) syntax");
        }

        public override IPExpr VisitDecimalFloat(PParser.DecimalFloatContext context)
        {
            double value = double.Parse($"{context.pre.Text}.{context.post.Text}");
            return new FloatLiteralExpr(value);
        }

        public override IPExpr VisitRvalue(PParser.RvalueContext context)
        {
            if (context.linear != null)
            {
                string varName = context.Iden().GetText();
                if (!table.Lookup(varName, out Variable variable))
                {
                    throw new MissingDeclarationException(varName, context);
                }

                return context.linear.Text.Equals("move")
                           ? new LinearAccessRefExpr(variable, LinearType.Move)
                           : new LinearAccessRefExpr(variable, LinearType.Swap);
            }
            return Visit(context.expr());
        }
    }

    public class EnumElemRefExpr : IPExpr
    {
        public EnumElemRefExpr(EnumElem enumElem)
        {
            EnumElem = enumElem;
            Type = new EnumType(EnumElem.ParentEnum);
        }

        public EnumElem EnumElem { get; }
        public PLanguageType Type { get; }
    }

    public enum LinearType
    {
        Move,
        Swap
    }

    public interface IVarRef : IPExpr
    {
        Variable Variable { get; }
    }

    public interface ILinearRef : IVarRef
    {
        LinearType LinearType { get; }
    }

    public class LinearAccessRefExpr : ILinearRef
    {
        public LinearAccessRefExpr(Variable variable, LinearType linearType)
        {
            Variable = variable;
            LinearType = linearType;
            Type = variable.Type;
        }

        public Variable Variable { get; }
        public PLanguageType Type { get; }
        public LinearType LinearType { get; }
    }

    public class UnnamedTupleExpr : IPExpr
    {
        public UnnamedTupleExpr(IPExpr[] tupleFields, PLanguageType type)
        {
            TupleFields = tupleFields;
            Type = type;
        }

        public IPExpr[] TupleFields { get; }

        public PLanguageType Type { get; }
    }

    public class NamedTupleExpr : IPExpr
    {
        public NamedTupleExpr(IPExpr[] tupleFields, PLanguageType type)
        {
            TupleFields = tupleFields;
            Type = type;
        }

        public IPExpr[] TupleFields { get; }

        public PLanguageType Type { get; }
    }

    public class FloatLiteralExpr : IPExpr
    {
        public FloatLiteralExpr(double value) { Value = value; }
        public double Value { get; }
        public PLanguageType Type { get; } = PrimitiveType.Float;
    }

    public class CompareSameExpr : IPExpr
    {
        public CompareSameExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class CompareDiffExpr : IPExpr
    {
        public CompareDiffExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }


    public class LogicalOrExpr : IPExpr
    {
        public LogicalOrExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class LogicalAndExpr : IPExpr
    {
        public LogicalAndExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }


        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class ContainsKeyExpr : IPExpr
    {
        public ContainsKeyExpr(IPExpr key, IPExpr map)
        {
            Key = key;
            Map = map;
        }

        public IPExpr Key { get; }
        public IPExpr Map { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class LessThanExpr : IPExpr
    {
        public LessThanExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class LessEqualsExpr : IPExpr
    {
        public LessEqualsExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class GreaterThanExpr : IPExpr
    {
        public GreaterThanExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class GreaterEqualsExpr : IPExpr
    {
        public GreaterEqualsExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class DivExpr : IPExpr
    {
        public DivExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Debug.Assert(Lhs.Type.IsSameTypeAs(Rhs.Type));
            Type = Lhs.Type;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; }
    }

    public class AddExpr : IPExpr
    {
        public AddExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Debug.Assert(Lhs.Type.IsSameTypeAs(Rhs.Type));
            Type = Lhs.Type;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; }
    }

    public class SubExpr : IPExpr
    {
        public SubExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Debug.Assert(Lhs.Type.IsSameTypeAs(Rhs.Type));
            Type = Lhs.Type;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; }
    }

    public class MultExpr : IPExpr
    {
        public MultExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Debug.Assert(Lhs.Type.IsSameTypeAs(Rhs.Type));
            Type = Lhs.Type;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; }
    }

    public class ThisRefExpr : IPExpr
    {
        public ThisRefExpr(Machine machine) { Machine = machine; }

        public Machine Machine { get; }

        public PLanguageType Type { get; } = PrimitiveType.Machine;
    }

    public class EventRefExpr : IPExpr
    {
        public EventRefExpr(PEvent pEvent) { PEvent = pEvent; }

        public PEvent PEvent { get; }

        public PLanguageType Type { get; } = PrimitiveType.Event;
    }

    public class FairNondetExpr : IPExpr
    {
        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class NondetExpr : IPExpr
    {
        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class NullLiteralExpr : IPExpr
    {
        public PLanguageType Type { get; } = PrimitiveType.Null;
    }

    public class IntLiteralExpr : IPExpr
    {
        public IntLiteralExpr(int value) { Value = value; }

        public int Value { get; }
        public PLanguageType Type { get; } = PrimitiveType.Int;
    }

    public class BoolLiteralExpr : IPExpr
    {
        public BoolLiteralExpr(bool value) { Value = value; }

        public bool Value { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }

    public class VariableAccessExpr : IVarRef
    {
        public VariableAccessExpr(Variable variable)
        {
            Variable = variable;
            Type = variable.Type;
        }

        public Variable Variable { get; }

        public PLanguageType Type { get; }
    }

    public class CastExpr : IPExpr
    {
        public CastExpr(IPExpr subExpr, PLanguageType type)
        {
            Type = type;
            SubExpr = subExpr;
        }

        public IPExpr SubExpr { get; }
        public PLanguageType Type { get; }
    }

    public class LogicalNegateExpr : IPExpr
    {
        public LogicalNegateExpr(IPExpr subExpr)
        {
            SubExpr = subExpr;
            Type = subExpr.Type;
        }

        public IPExpr SubExpr { get; }
        public PLanguageType Type { get; }
    }

    public class SignNegateExpr : IPExpr
    {
        public SignNegateExpr(IPExpr subExpr)
        {
            SubExpr = subExpr;
            Type = subExpr.Type;
        }

        public IPExpr SubExpr { get; }

        public PLanguageType Type { get; }
    }

    public class FunCallExpr : IPExpr
    {
        public FunCallExpr(Function function, IPExpr[] arguments)
        {
            Function = function;
            Arguments = arguments;
            Type = function.Signature.ReturnType;
        }

        public Function Function { get; }
        public IPExpr[] Arguments { get; }

        public PLanguageType Type { get; }
    }

    public class CtorExpr : IPExpr
    {
        public CtorExpr(Machine machine, IPExpr[] arguments)
        {
            Machine = machine;
            Arguments = arguments;
        }

        public Machine Machine { get; }
        public IPExpr[] Arguments { get; }

        public PLanguageType Type { get; } = PrimitiveType.Machine;
    }

    public class DefaultExpr : IPExpr
    {
        public DefaultExpr(PLanguageType type) { Type = type; }

        public PLanguageType Type { get; }
    }

    public class SizeofExpr : IPExpr
    {
        public SizeofExpr(IPExpr expr) { Expr = expr; }

        public IPExpr Expr { get; }

        public PLanguageType Type { get; } = PrimitiveType.Int;
    }

    public class ValuesExpr : IPExpr
    {
        public ValuesExpr(IPExpr expr, PLanguageType type)
        {
            Expr = expr;
            Type = type;
        }

        public IPExpr Expr { get; }

        public PLanguageType Type { get; }
    }

    public class KeysExpr : IPExpr
    {
        public KeysExpr(IPExpr expr, PLanguageType type)
        {
            Expr = expr;
            Type = type;
        }

        public IPExpr Expr { get; }
        public PLanguageType Type { get; }
    }

    public class SeqAccessExpr : IPExpr
    {
        public SeqAccessExpr(IPExpr seqExpr, IPExpr indexExpr, PLanguageType type)
        {
            SeqExpr = seqExpr;
            IndexExpr = indexExpr;
            Type = type;
        }

        public IPExpr SeqExpr { get; }
        public IPExpr IndexExpr { get; }

        public PLanguageType Type { get; }
    }

    public class NamedTupleAccessExpr : IPExpr
    {
        public NamedTupleAccessExpr(IPExpr subExpr, string fieldName, PLanguageType type)
        {
            SubExpr = subExpr;
            FieldName = fieldName;
            Type = type;
        }

        public IPExpr SubExpr { get; }
        public string FieldName { get; }
        public PLanguageType Type { get; }
    }

    public class TupleAccessExpr : IPExpr
    {
        public TupleAccessExpr(IPExpr subExpr, int fieldNo, PLanguageType type)
        {
            SubExpr = subExpr;
            FieldNo = fieldNo;
            Type = type;
        }

        public IPExpr SubExpr { get; }
        public int FieldNo { get; }

        public PLanguageType Type { get; }
    }

    public class TypeException : Exception
    {
        public TypeException(ParserRuleContext location, string clarification)
        {
            Location = location;
            Clarification = clarification;
        }

        public ParserRuleContext Location { get; }
        public string Clarification { get; }
    }

    public interface IPExpr
    {
        PLanguageType Type { get; }
    }
}
