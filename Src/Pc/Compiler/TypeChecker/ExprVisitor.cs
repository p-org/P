using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public class ExprVisitor : PParserBaseVisitor<IPExpr>
    {
        private readonly ITranslationErrorHandler handler;
        private readonly Scope table;

        public ExprVisitor(Scope table, ITranslationErrorHandler handler)
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
            if (!(subExpr.Type.Canonicalize() is NamedTupleType tuple))
            {
                throw handler.TypeMismatch(context.expr(), subExpr.Type, TypeKind.NamedTuple);
            }
            string fieldName = context.field.GetText();
            if (!tuple.LookupEntry(fieldName, out NamedTupleEntry entry))
            {
                throw handler.MissingNamedTupleEntry(context.field, context.field.GetText(), tuple);
            }
            return new NamedTupleAccessExpr(subExpr, entry);
        }

        public override IPExpr VisitTupleAccessExpr(PParser.TupleAccessExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            int fieldNo = int.Parse(context.field.GetText());
            if (!(subExpr.Type.Canonicalize() is TupleType tuple))
            {
                throw handler.TypeMismatch(context.expr(), subExpr.Type, TypeKind.Tuple, TypeKind.NamedTuple);
            }
            if (fieldNo >= tuple.Types.Count)
            {
                throw handler.OutOfBoundsTupleAccess(context.field, fieldNo, tuple);
            }
            return new TupleAccessExpr(subExpr, fieldNo, tuple.Types[fieldNo]);
        }

        public override IPExpr VisitSeqAccessExpr(PParser.SeqAccessExprContext context)
        {
            IPExpr seqOrMap = Visit(context.seq);
            IPExpr indexExpr = Visit(context.index);
            switch (seqOrMap.Type.Canonicalize())
            {
                case SequenceType seqType:
                    if (!PrimitiveType.Int.IsAssignableFrom(indexExpr.Type))
                    {
                        throw handler.TypeMismatch(context.index, indexExpr.Type, PrimitiveType.Int);
                    }
                    return new SeqAccessExpr(seqOrMap, indexExpr, seqType.ElementType);
                case MapType mapType:
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
            switch (context.fun.Text)
            {
                case "keys":
                {
                    IPExpr expr = Visit(context.expr());
                    if (!(expr.Type.Canonicalize() is MapType mapType))
                    {
                        throw handler.TypeMismatch(context.expr(), expr.Type, TypeKind.Map);
                    }
                    return new KeysExpr(expr, new SequenceType(mapType.KeyType));
                }
                case "values":
                {
                    IPExpr expr = Visit(context.expr());
                    if (!(expr.Type.Canonicalize() is MapType mapType))
                    {
                        throw handler.TypeMismatch(context.expr(), expr.Type, TypeKind.Map);
                    }
                    return new ValuesExpr(expr, new SequenceType(mapType.ValueType));
                }
                case "sizeof":
                {
                    IPExpr expr = Visit(context.expr());
                    if (!(expr.Type.Canonicalize() is SequenceType) && !(expr.Type.Canonicalize() is MapType))
                    {
                        throw handler.TypeMismatch(context.expr(), expr.Type, TypeKind.Map, TypeKind.Sequence);
                    }
                    return new SizeofExpr(expr);
                }
                case "default":
                {
                    PLanguageType type = TypeResolver.ResolveType(context.type(), table, handler);
                    return new DefaultExpr(type.Canonicalize());
                }
                default:
                {
                    throw new ArgumentException($"Unknown keyword expression {context.fun.Text}", nameof(context));
                }
            }
        }

        private static bool ArgListMatchesTupleType(PLanguageType expected, IReadOnlyList<IPExpr> arguments)
        {
            if (arguments.Count == 1 && expected.IsAssignableFrom(arguments[0].Type))
            {
                return true;
            }
            return expected.IsAssignableFrom(new TupleType(arguments.Select(arg => arg.Type).ToArray()));
        }

        public override IPExpr VisitCtorExpr(PParser.CtorExprContext context)
        {
            // TODO: roll arguments into tuple automatically if that would match constructor
            string machineName = context.machineName.GetText();
            if (!table.Lookup(machineName, out Machine machine))
            {
                throw handler.MissingDeclaration(context.machineName, "machine", machineName);
            }

            IPExpr[] arguments = (context.rvalueList()?.rvalue().Select(Visit) ?? Enumerable.Empty<IPExpr>()).ToArray();

            PLanguageType expectedType = machine.PayloadType;
            if (PrimitiveType.Null.IsAssignableFrom(expectedType))
            {
                // expect no arguments
                if (arguments.Length > 0)
                {
                    throw handler.IncorrectArgumentCount(context, arguments.Length, 0);
                }
            }
            else if (PLanguageType.TypeIsOfKind(expectedType, TypeKind.Tuple) /* || PLanguageType.TypeIsOfKind(machine.PayloadType, TypeKind.NamedTuple) */)
            {
                // Can either be agreeing tuple, or list that becomes an agreeing tuple
                var tuple = (TupleType) expectedType.Canonicalize();
                if (!ArgListMatchesTupleType(tuple, arguments))
                {
                    throw handler.TypeMismatch(context, new TupleType(arguments.Select(arg => arg.Type).ToArray()), expectedType);
                }
            }
            else
            {
                if (arguments.Length != 1)
                {
                    throw handler.IncorrectArgumentCount(context, arguments.Length, 1);
                }

                PLanguageType actualType = arguments[0].Type;
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
                    if (!PrimitiveType.Int.IsAssignableFrom(subExpr.Type) &&
                        !PrimitiveType.Float.IsAssignableFrom(subExpr.Type))
                    {
                        throw handler.TypeMismatch(context.expr(),
                                                   subExpr.Type,
                                                   PrimitiveType.Int,
                                                   PrimitiveType.Float);
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
                    if (!(PrimitiveType.Int.IsAssignableFrom(lhs.Type) &&
                          PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                          PrimitiveType.Float.IsAssignableFrom(lhs.Type) &&
                          PrimitiveType.Float.IsAssignableFrom(rhs.Type)))
                    {
                        throw handler.BinOpTypeMismatch(context, lhs.Type, rhs.Type);
                    }
                    return arithCtors[op](lhs, rhs);
                case "in":
                    if (!(rhs.Type.Canonicalize() is MapType rhsMap))
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
            PLanguageType oldType = subExpr.Type;
            PLanguageType newType = TypeResolver.ResolveType(context.type(), table, handler);
            if (context.cast.Text.Equals("as"))
            {
                if (!newType.IsAssignableFrom(oldType) && !oldType.IsAssignableFrom(newType))
                {
                    throw handler.IncomparableTypes(context, oldType, newType);
                }
                return new CastExpr(subExpr, newType);
            }
            if (context.cast.Text.Equals("to"))
            {
                if (oldType.IsSameTypeAs(PrimitiveType.Int))
                {
                    if (newType.IsSameTypeAs(PrimitiveType.Int))
                    {
                        return subExpr;
                    }
                    if (newType.IsSameTypeAs(PrimitiveType.Float) || PLanguageType.TypeIsOfKind(newType, TypeKind.Enum))
                    {
                        return new CoerceExpr(subExpr, newType);
                    }
                }
                else if (oldType.IsSameTypeAs(PrimitiveType.Float))
                {
                    if (newType.IsSameTypeAs(PrimitiveType.Float))
                    {
                        return subExpr;
                    }
                    if (newType.IsSameTypeAs(PrimitiveType.Int))
                    {
                        return new CoerceExpr(subExpr, newType);
                    }
                }
                else if (PLanguageType.TypeIsOfKind(oldType, TypeKind.Enum))
                {
                    // TODO: are enum-to-enum coersions allowed?
                    if (newType.IsSameTypeAs(oldType))
                    {
                        return subExpr;
                    }
                    if (newType.IsSameTypeAs(PrimitiveType.Int))
                    {
                        return new CoerceExpr(subExpr, newType);
                    }
                }
                throw handler.IncomparableTypes(context, oldType, newType);
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
                var implParent = context.GetParent<PParser.ImplMachineDeclContext>();
                var specParent = context.GetParent<PParser.SpecMachineDeclContext>();
                if (!(implParent != null || specParent != null))
                {
                    throw handler.MisplacedThis(context);
                }
                // TODO: this is somewhat inelegant
                string machineName = implParent?.name.GetText() ?? specParent?.name.GetText();
                Debug.Assert(machineName != null);
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
            if (context.linear == null)
            {
                return Visit(context.expr());
            }

            string varName = context.iden().GetText();
            if (!table.Lookup(varName, out Variable variable))
            {
                throw handler.MissingDeclaration(context.iden(), "variable", varName);
            }

            return context.linear.Text.Equals("move")
                       ? new LinearAccessRefExpr(variable, LinearType.Move)
                       : new LinearAccessRefExpr(variable, LinearType.Swap);
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
            if (!(lvalue.Type.Canonicalize() is NamedTupleType type))
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
            IPExpr lvalue = Visit(context.lvalue());
            if (!(lvalue.Type.Canonicalize() is TupleType type))
            {
                throw handler.TypeMismatch(context.lvalue(), lvalue.Type, TypeKind.Tuple);
            }
            int field = int.Parse(context.@int().GetText());
            if (field >= type.Types.Count)
            {
                throw handler.OutOfBoundsTupleAccess(context.@int(), field, type);
            }
            return new TupleAccessExpr(lvalue, field, type.Types[field]);
        }

        public override IPExpr VisitMapOrSeqLvalue(PParser.MapOrSeqLvalueContext context)
        {
            IPExpr lvalue = Visit(context.lvalue());
            IPExpr index = Visit(context.expr());
            PLanguageType indexType = index.Type;
            switch (lvalue.Type.Canonicalize())
            {
                case MapType mapType:
                    if (!mapType.KeyType.IsAssignableFrom(indexType))
                    {
                        throw handler.TypeMismatch(context.expr(), indexType, mapType.KeyType);
                    }
                    return new MapAccessExpr(lvalue, index, mapType.ValueType);
                case SequenceType seqType:
                    if (!PrimitiveType.Int.IsAssignableFrom(indexType))
                    {
                        throw handler.TypeMismatch(context.expr(), indexType, PrimitiveType.Int);
                    }
                    return new SeqAccessExpr(lvalue, index, seqType.ElementType);
                default:
                    throw handler.TypeMismatch(context.lvalue(), lvalue.Type, TypeKind.Sequence, TypeKind.Map);
            }
        }
    }
}
