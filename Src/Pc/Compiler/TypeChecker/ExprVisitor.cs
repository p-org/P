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
        private readonly ITranslationErrorHandler handler;

        public ExprVisitor(DeclarationTable table, ITranslationErrorHandler handler)
        {
            this.table = table;
            this.handler = handler;
        }

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
            var tuple = subExpr.Type.Canonicalize() as NamedTupleType;
            if (tuple == null)
            {
                throw handler.TypeMismatch(context.expr(), subExpr.Type, TypeKind.NamedTuple);
            }
            string fieldName = context.field.GetText();
            if (!tuple.LookupEntry(fieldName, out var entry))
            {
                throw handler.MissingNamedTupleEntry(context.field, context.field.GetText(), tuple);
            }
            return new NamedTupleAccessExpr(subExpr, entry);
        }

        public override IPExpr VisitTupleAccessExpr(PParser.TupleAccessExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            var tuple = subExpr.Type.Canonicalize() as TupleType;
            if (tuple == null)
            {
                throw handler.TypeMismatch(context.expr(), subExpr.Type, TypeKind.Tuple);
            }
            int fieldNo = int.Parse(context.field.GetText());
            if (fieldNo >= tuple.Types.Length)
            {
                throw handler.OutOfBoundsTupleAccess(context.field, fieldNo, tuple);
            }
            return new TupleAccessExpr(subExpr, fieldNo, tuple.Types[fieldNo]);
        }

        public override IPExpr VisitSeqAccessExpr(PParser.SeqAccessExprContext context)
        {
            IPExpr seqOrMap = Visit(context.seq);
            PLanguageType canonicalType = seqOrMap.Type.Canonicalize();
            if (canonicalType is SequenceType seqType)
            {
                IPExpr indexExpr = Visit(context.index);
                if (!PrimitiveType.Int.IsAssignableFrom(indexExpr.Type))
                {
                    throw handler.TypeMismatch(context.index, indexExpr.Type, PrimitiveType.Int);
                }
                return new SeqAccessExpr(seqOrMap, indexExpr, seqType.ElementType);
            }
            if (canonicalType is MapType mapType)
            {
                IPExpr indexExpr = Visit(context.index);
                if (!mapType.KeyType.IsAssignableFrom(indexExpr.Type))
                {
                    throw handler.TypeMismatch(context.index, indexExpr.Type, mapType.KeyType);
                }
                return new MapAccessExpr(seqOrMap, indexExpr, mapType.ValueType);
            }
            throw handler.TypeMismatch(context.seq, seqOrMap.Type, TypeKind.Sequence, TypeKind.Map);
        }

        public override IPExpr VisitKeywordExpr(PParser.KeywordExprContext context)
        {
            IPExpr expr;
            MapType mapType;
            switch (context.fun.Text)
            {
                case "keys":
                    expr = Visit(context.expr());
                    mapType = expr.Type.Canonicalize() as MapType;
                    if (mapType == null)
                    {
                        throw handler.TypeMismatch(context.expr(), expr.Type, TypeKind.Map);
                    }
                    return new KeysExpr(expr, new SequenceType(mapType.KeyType));
                case "values":
                    expr = Visit(context.expr());
                    mapType = expr.Type.Canonicalize() as MapType;
                    if (mapType == null)
                    {
                        throw handler.TypeMismatch(context.expr(), expr.Type, TypeKind.Map);
                    }
                    return new ValuesExpr(expr, new SequenceType(mapType.ValueType));
                case "sizeof":
                    expr = Visit(context.expr());
                    if (!(expr.Type.Canonicalize() is SequenceType) && !(expr.Type.Canonicalize() is MapType))
                    {
                        throw handler.TypeMismatch(context.expr(), expr.Type, TypeKind.Map, TypeKind.Sequence);
                    }
                    return new SizeofExpr(expr);
                case "default":
                    PLanguageType type = TypeResolver.ResolveType(context.type(), table, handler);
                    return new DefaultExpr(type.Canonicalize());
                default:
                    throw new ArgumentException($"Unknown keyword expression {context.fun.Text}", nameof(context));
            }
        }

        public override IPExpr VisitCtorExpr(PParser.CtorExprContext context)
        {
            // TODO: roll arguments into tuple automatically if that would match constructor
            string machineName = context.machineName.GetText();
            if (!table.Lookup(machineName, out Machine machine))
            {
                if (!table.Lookup(machineName, out MachineProto proto))
                {
                    throw new NotImplementedException($"constructing machine prototypes ({machineName})");
                }
                throw handler.MissingDeclaration(context.machineName, "machine", machineName);
            }

            var arguments = (context.rvalueList()?.rvalue().Select(Visit) ?? Enumerable.Empty<IPExpr>()).ToArray();
            if (PrimitiveType.Null.IsAssignableFrom(machine.PayloadType) && arguments.Length != 0)
            {
                throw handler.IncorrectArgumentCount(
                    (ParserRuleContext)context.rvalueList() ?? context,
                    arguments.Length,
                    0);
            }

            if (!PrimitiveType.Null.IsAssignableFrom(machine.PayloadType) && arguments.Length != 1)
            {
                throw handler.IncorrectArgumentCount((ParserRuleContext)context.rvalueList() ?? context,
                                                     arguments.Length,
                                                     1);
            }

            if (!PrimitiveType.Null.IsAssignableFrom(machine.PayloadType))
            {
                // check the argument types
                PLanguageType actualType = arguments[0].Type;
                PLanguageType expectedType = machine.PayloadType;
                if (!expectedType.IsAssignableFrom(actualType))
                {
                    throw handler.TypeMismatch(context.rvalueList().rvalue(0), actualType, expectedType);
                }
            }

            return new CtorExpr(machine, arguments);
        }

        public override IPExpr VisitFunCallExpr(PParser.FunCallExprContext context)
        {
            string funName = context.fun.GetText();
            if (!table.Lookup(funName, out Function function))
            {
                if (table.Lookup(funName, out FunctionProto proto))
                {
                    throw new NotImplementedException($"function proto calls ({proto.Name})");
                }
                throw handler.MissingDeclaration(context.fun, "function", funName);
            }

            // Check the arguments
            // TODO: linearly typed arguments
            var arguments = (context.rvalueList()?.rvalue().Select(Visit) ?? Enumerable.Empty<IPExpr>()).ToArray();
            for (var i = 0; i < arguments.Length; i++)
            {
                IPExpr argument = arguments[i];
                PLanguageType paramType = function.Signature.Parameters[i].Type;
                if (!paramType.IsAssignableFrom(argument.Type))
                {
                    throw handler.TypeMismatch(context.rvalueList().rvalue(i), argument.Type, paramType);
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
                    if (!PrimitiveType.Int.IsAssignableFrom(subExpr.Type) && !PrimitiveType.Float.IsAssignableFrom(subExpr.Type))
                    {
                        throw handler.TypeMismatch(context.expr(), subExpr.Type, PrimitiveType.Int, PrimitiveType.Float);
                    }
                    return new SignNegateExpr(subExpr);
                case "!":
                    if (!PrimitiveType.Bool.IsAssignableFrom(subExpr.Type))
                    {
                        throw handler.TypeMismatch(context.expr(), subExpr.Type, PrimitiveType.Bool);
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
                    if (!(PrimitiveType.Int.IsAssignableFrom(lhs.Type) && PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                          PrimitiveType.Float.IsAssignableFrom(lhs.Type) && PrimitiveType.Float.IsAssignableFrom(rhs.Type)))
                    {
                        throw handler.BinOpTypeMismatch(context, lhs.Type, rhs.Type);
                    }
                    return arithCtors[op](lhs, rhs);
                case "in":
                    var rhsMap = rhs.Type.Canonicalize() as MapType;
                    if (rhsMap == null)
                    {
                        throw handler.TypeMismatch(context.rhs, rhs.Type, TypeKind.Map);
                    }
                    if (!rhsMap.KeyType.IsAssignableFrom(lhs.Type))
                    {
                        throw handler.TypeMismatch(context.lhs, lhs.Type, rhsMap.KeyType);
                    }
                    return new ContainsKeyExpr(lhs, rhs);
                case "==":
                case "!=":
                    if (!lhs.Type.IsAssignableFrom(rhs.Type) && !rhs.Type.IsAssignableFrom(lhs.Type))
                    {
                        throw handler.IncomparableTypes(context, lhs.Type, rhs.Type);
                    }
                    return compCtors[op](lhs, rhs);
                case "&&":
                case "||":
                    if (!PrimitiveType.Bool.IsAssignableFrom(lhs.Type))
                    {
                        throw handler.TypeMismatch(context.lhs, lhs.Type, PrimitiveType.Bool);
                    }
                    if (!PrimitiveType.Bool.IsAssignableFrom(rhs.Type))
                    {
                        throw handler.TypeMismatch(context.rhs, rhs.Type, PrimitiveType.Bool);
                    }
                    return logicCtors[op](lhs, rhs);
                default:
                    throw new ArgumentException($"unknown binary operation {op}", nameof(context));
            }
        }

        public override IPExpr VisitCastExpr(PParser.CastExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            PLanguageType newType = TypeResolver.ResolveType(context.type(), table, handler);
            if (context.cast.Text.Equals("as"))
            {
                if (!newType.IsAssignableFrom(subExpr.Type) && !subExpr.Type.IsAssignableFrom(newType))
                {
                    throw handler.IncomparableTypes(context, subExpr.Type, newType);
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
            if (context.iden() != null)
            {
                string symbolName = context.iden().GetText();
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
                throw handler.MissingDeclaration(context.iden(), "variable, enum element, or event", symbolName);
            }
            if (context.floatLiteral() != null)
            {
                return Visit(context.floatLiteral());
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
                // TODO: this is somewhat inelegant.
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
                    throw handler.MisplacedThis(context);
                }
                string machineName = (ctx as PParser.ImplMachineDeclContext)?.name.GetText() ??
                                     (ctx as PParser.SpecMachineDeclContext)?.name.GetText();
                bool success = table.Lookup(machineName, out Machine machine);
                Debug.Assert(success);
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
                                                 Name = fieldName.GetText(),
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
            double value = double.Parse($"{context.pre?.Text ?? ""}.{context.post.Text}");
            return new FloatLiteralExpr(value);
        }

        public override IPExpr VisitRvalue(PParser.RvalueContext context)
        {
            if (context.linear != null)
            {
                string varName = context.iden().GetText();
                if (!table.Lookup(varName, out Variable variable))
                {
                    throw handler.MissingDeclaration(context.iden(), "variable", varName);
                }

                return context.linear.Text.Equals("move")
                           ? new LinearAccessRefExpr(variable, LinearType.Move)
                           : new LinearAccessRefExpr(variable, LinearType.Swap);
            }
            return Visit(context.expr());
        }

        public override IPExpr VisitVarLvalue(PParser.VarLvalueContext context)
        {
            string varName = context.name.GetText();
            if (!table.Lookup(varName, out Variable variable))
            {
                throw handler.MissingDeclaration(context, "variable", varName);
            }
            return new VariableAccessExpr(variable);
        }

        public override IPExpr VisitNamedTupleLvalue(PParser.NamedTupleLvalueContext context)
        {
            IPExpr lvalue = Visit(context.lvalue());
            var type = lvalue.Type.Canonicalize() as NamedTupleType;
            if (type == null)
            {
                throw handler.TypeMismatch(context.lvalue(), lvalue.Type, TypeKind.NamedTuple);
            }
            string field = context.field.GetText();
            if (!type.LookupEntry(field, out NamedTupleEntry entry))
            {
                throw handler.MissingNamedTupleEntry(context.field, field, type);
            }
            return new NamedTupleAccessExpr(lvalue, entry);
        }

        public override IPExpr VisitTupleLvalue(PParser.TupleLvalueContext context)
        {
            // TODO: adapt to named tuples. Numbers map to positions
            IPExpr lvalue = Visit(context.lvalue());
            var type = lvalue.Type.Canonicalize() as TupleType;
            if (type == null)
            {
                throw handler.TypeMismatch(context.lvalue(), lvalue.Type, TypeKind.Tuple);
            }
            int field = int.Parse(context.@int().GetText());
            if (field >= type.Types.Length)
            {
                throw handler.OutOfBoundsTupleAccess(context.@int(), field, type);
            }
            return new TupleAccessExpr(lvalue, field, type.Types[field]);
        }

        public override IPExpr VisitMapOrSeqLvalue(PParser.MapOrSeqLvalueContext context)
        {
            IPExpr lvalue = Visit(context.lvalue());
            var type = lvalue.Type.Canonicalize();
            if (type is MapType mapType)
            {
                IPExpr index = Visit(context.expr());
                if (!mapType.KeyType.IsAssignableFrom(index.Type))
                {
                    throw handler.TypeMismatch(context.expr(), index.Type, mapType.KeyType);
                }
                return new MapAccessExpr(lvalue, index, mapType.ValueType);
            }
            if (type is SequenceType seqType)
            {
                IPExpr index = Visit(context.expr());
                if (!PrimitiveType.Int.IsAssignableFrom(index.Type))
                {
                    throw handler.TypeMismatch(context.expr(), index.Type, PrimitiveType.Int);
                }
                return new SeqAccessExpr(lvalue, index, seqType.ElementType);
            }
            throw handler.TypeMismatch(context.lvalue(), lvalue.Type, TypeKind.Sequence, TypeKind.Map);
        }
    }

    public class MapAccessExpr : IPExpr
    {
        public IPExpr MapExpr { get; }
        public IPExpr IndexExpr { get; }
        public PLanguageType Type { get; }

        public MapAccessExpr(IPExpr mapExpr, IPExpr indexExpr, PLanguageType type)
        {
            MapExpr = mapExpr;
            IndexExpr = indexExpr;
            Type = type;
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
        public NamedTupleAccessExpr(IPExpr subExpr, NamedTupleEntry entry)
        {
            SubExpr = subExpr;
            Entry = entry;
        }

        public IPExpr SubExpr { get; }
        public NamedTupleEntry Entry { get; }
        public string FieldName => Entry.Name;
        public PLanguageType Type => Entry.Type;
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

    public interface IPExpr
    {
        PLanguageType Type { get; }
    }
}
