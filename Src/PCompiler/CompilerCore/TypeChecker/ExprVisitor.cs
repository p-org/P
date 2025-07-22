using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Misc;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    public class ExprVisitor : PParserBaseVisitor<IPExpr>
    {
        private readonly ITranslationErrorHandler handler;
        private readonly Function method;
        private Scope table;

        public ExprVisitor(Function method, ITranslationErrorHandler handler)
        {
            table = method.Scope;
            this.method = method;
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

        public override IPExpr VisitParenExpr(PParser.ParenExprContext context)
        {
            return Visit(context.expr());
        }

        public override IPExpr VisitNamedTupleAccessExpr(PParser.NamedTupleAccessExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            var fieldName = context.field.GetText();

            switch (subExpr.Type.Canonicalize())
            {
                case NamedTupleType tuple:
                    if (!tuple.LookupEntry(fieldName, out var entry))
                    {
                        throw handler.MissingNamedTupleEntry(context.field, tuple);
                    }

                    return new NamedTupleAccessExpr(context, subExpr, entry);
                
                case PermissionType {Origin: Machine} permission:
                    var machine = (Machine) permission.Origin;
                    
                    if (!machine.LookupEntry(fieldName, out var field))
                    {
                        throw handler.MissingMachineField(context.field, machine);
                    }
                    return new MachineAccessExpr(context, machine, subExpr, field);
                
                case PermissionType {Origin: Interface} permission:
                    var pname = permission.Origin.Name;
                   
                    if (!table.Lookup(pname, out Machine m))
                    {
                        throw handler.TypeMismatch(subExpr, [TypeKind.NamedTuple, TypeKind.Base]);
                    }
                    
                    if (!m.LookupEntry(fieldName, out var mfield))
                    {
                        throw handler.MissingMachineField(context.field, m);
                    }
                    return new MachineAccessExpr(context, m, subExpr, mfield);
                
                case PermissionType {Origin: NamedEventSet} permission:

                    var pevents = ((NamedEventSet)permission.Origin).Events.ToList();

                    foreach (var pevent in pevents)
                    {
                        switch (pevent.PayloadType.Canonicalize())
                        {
                            case NamedTupleType namedTupleType:
                                if (namedTupleType.LookupEntry(fieldName, out var pentry))
                                {
                                    return new EventAccessExpr(context, pevent, subExpr, pentry);
                                }
                                break;
                        }
                    }
                    
                    throw handler.MissingEventField(context.field, pevents.First());
                
                case PrimitiveType pt when pt.IsSameTypeAs(PrimitiveType.Machine):
                    Machine spec;

                    switch (subExpr)
                    {
                        case SpecRefExpr specRefExpr:
                            spec = specRefExpr.Value;
                            break;
                        default:
                            throw handler.TypeMismatch(subExpr, [TypeKind.NamedTuple, TypeKind.Base]);
                    }
                    
                    if (!spec.LookupEntry(fieldName, out var sfield))
                    {
                        throw handler.MissingMachineField(context.field, spec);
                    }
                    return new SpecAccessExpr(context, spec, subExpr, sfield);
                
                default:
                    throw handler.TypeMismatch(subExpr, [TypeKind.NamedTuple, TypeKind.Base]);
            }
        }

        public override IPExpr VisitTupleAccessExpr(PParser.TupleAccessExprContext context)
        {
            var subExpr = Visit(context.expr());
            var fieldNo = int.Parse(context.field.GetText());
            if (!(subExpr.Type.Canonicalize() is TupleType tuple))
            {
                throw handler.TypeMismatch(subExpr, TypeKind.Tuple, TypeKind.NamedTuple);
            }

            if (fieldNo >= tuple.Types.Count)
            {
                throw handler.OutOfBoundsTupleAccess(context.field, tuple);
            }

            return new TupleAccessExpr(context, subExpr, fieldNo, tuple.Types[fieldNo]);
        }

        public override IPExpr VisitSeqAccessExpr(PParser.SeqAccessExprContext context)
        {
            var seqOrMap = Visit(context.seq);
            var indexExpr = Visit(context.index);
            switch (seqOrMap.Type.Canonicalize())
            {
                case SequenceType seqType:
                    if (!PrimitiveType.Int.IsAssignableFrom(indexExpr.Type))
                    {
                        throw handler.TypeMismatch(context.index, indexExpr.Type, PrimitiveType.Int);
                    }

                    return new SeqAccessExpr(context, seqOrMap, indexExpr, seqType.ElementType);

                case MapType mapType:
                    if (!mapType.KeyType.IsAssignableFrom(indexExpr.Type))
                    {
                        throw handler.TypeMismatch(context.index, indexExpr.Type, mapType.KeyType);
                    }

                    return new MapAccessExpr(context, seqOrMap, indexExpr, mapType.ValueType);

                case SetType setType:
                    if (!PrimitiveType.Int.IsAssignableFrom(indexExpr.Type))
                    {
                        throw handler.TypeMismatch(context.index, indexExpr.Type, PrimitiveType.Int);
                    }

                    return new SetAccessExpr(context, seqOrMap, indexExpr, setType.ElementType);
            }

            throw handler.TypeMismatch(seqOrMap, TypeKind.Sequence, TypeKind.Map);
        }

        public override IPExpr VisitKeywordExpr(PParser.KeywordExprContext context)
        {
            switch (context.fun.Text)
            {
                case "keys":
                {
                    var expr = Visit(context.expr());
                    if (!(expr.Type.Canonicalize() is MapType mapType))
                    {
                        throw handler.TypeMismatch(expr, TypeKind.Map);
                    }

                    return new KeysExpr(context, expr, new SequenceType(mapType.KeyType));
                }
                case "values":
                {
                    var expr = Visit(context.expr());
                    if (!(expr.Type.Canonicalize() is MapType mapType))
                    {
                        throw handler.TypeMismatch(expr, TypeKind.Map);
                    }

                    return new ValuesExpr(context, expr, new SequenceType(mapType.ValueType));
                }
                case "sizeof":
                {
                    var expr = Visit(context.expr());
                    if (!(expr.Type.Canonicalize() is SequenceType)
                        && !(expr.Type.Canonicalize() is MapType)
                        && !(expr.Type.Canonicalize() is SetType))
                        throw handler.TypeMismatch(expr, TypeKind.Map, TypeKind.Sequence, TypeKind.Set);
                    return new SizeofExpr(context, expr);
                }
                case "default":
                {
                    var type = TypeResolver.ResolveType(context.type(), table, handler);
                    return new DefaultExpr(context, type.Canonicalize());
                }
                default:
                {
                    throw handler.InternalError(context,
                        new ArgumentException($"Unknown keyword expression {context.fun.Text}", nameof(context)));
                }
            }
        }

        public override IPExpr VisitCtorExpr(PParser.CtorExprContext context)
        {
            var interfaceName = context.interfaceName.GetText();
            if (!table.Lookup(interfaceName, out Interface @interface))
            {
                throw handler.MissingDeclaration(context.interfaceName, "interface", interfaceName);
            }

            if (method.Owner?.IsSpec == true)
            {
                throw handler.IllegalMonitorOperation(context, context.NEW().Symbol, method.Owner);
            }

            var arguments = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), this).ToArray();
            TypeCheckingUtils.ValidatePayloadTypes(handler, context, @interface.PayloadType, arguments);

            method.CanCreate = true;

            return new CtorExpr(context, @interface, arguments);
        }

        public override IPExpr VisitFunCallExpr(PParser.FunCallExprContext context)
        {
            var funName = context.fun.GetText();
            if (table.Lookup(funName, out Function function))
            {
                // Check the arguments
                var arguments = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), this).ToArray();
                ISet<Variable> linearVariables = new System.Collections.Generic.HashSet<Variable>();

                if (function.Signature.Parameters.Count != arguments.Length)
                {
                    throw handler.IncorrectArgumentCount(context, arguments.Length, function.Signature.Parameters.Count);
                }

                for (var i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    var paramType = function.Signature.Parameters[i].Type;
                    if (!paramType.IsAssignableFrom(argument.Type))
                    {
                        throw handler.TypeMismatch(context.rvalueList().rvalue(i), argument.Type, paramType);
                    }

                }

                method.AddCallee(function);
                return new FunCallExpr(context, function, arguments);
            }
            if (table.Lookup(funName, out Pure pure))
            {
                // Check the arguments
                var arguments = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), this).ToArray();
                ISet<Variable> linearVariables = new System.Collections.Generic.HashSet<Variable>();

                if (pure.Signature.Parameters.Count != arguments.Length)
                {
                    throw handler.IncorrectArgumentCount(context, arguments.Length, pure.Signature.Parameters.Count);
                }

                for (var i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    var paramType = pure.Signature.Parameters[i].Type;
                    if (!paramType.IsAssignableFrom(argument.Type))
                    {
                        switch (paramType)
                        {
                            case PrimitiveType pt when pt.IsSameTypeAs(PrimitiveType.Event):
                                switch (argument.Type)
                                {
                                    case PermissionType {Origin: NamedEventSet} per when ((NamedEventSet)(per.Origin)).Events.Count() == 1:
                                        continue;
                                }
                                break;
                        }
                        throw handler.TypeMismatch(context.rvalueList().rvalue(i), argument.Type, paramType);
                    }

                }
                
                return new PureCallExpr(context, pure, arguments);
            }
            
            throw handler.MissingDeclaration(context.fun, "function", funName);

        }

        public override IPExpr VisitUnaryExpr(PParser.UnaryExprContext context)
        {
            var subExpr = Visit(context.expr());
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

                    return new UnaryOpExpr(context, UnaryOpType.Negate, subExpr);

                case "!":
                    if (!PrimitiveType.Bool.IsAssignableFrom(subExpr.Type))
                    {
                        throw handler.TypeMismatch(context.expr(), subExpr.Type, PrimitiveType.Bool);
                    }

                    return new UnaryOpExpr(context, UnaryOpType.Not, subExpr);

                default:
                    throw handler.InternalError(context,
                        new ArgumentException($"Unknown unary op `{context.op.Text}`", nameof(context)));
            }
        }

        public override IPExpr VisitQuantExpr(PParser.QuantExprContext context)
        {
            var oldTable = table;
            table = table.MakeChildScope();

            bool diff = context.diff != null;

            var bound = context.bound.funParam().Select(p =>
            {
                var symbolName = p.name.GetText();
                var param = table.Put(symbolName, p, VariableRole.Param);
                param.Type = TypeResolver.ResolveType(p.type(), table, handler);
                return param;
            }).Cast<Variable>().ToArray();

            if (diff && bound.ToList().Count != 1)
            {
                // we have the "new" annotation so the bound must be a single thing and it must be an event
                throw handler.InternalError(context, new ArgumentException($"Difference quantifiers must have exactly one bound variable", nameof(context)));
            }

            if (diff)
            {
                switch (bound[0].Type.Canonicalize())
                {
                    case PrimitiveType pt when pt.IsSameTypeAs(PrimitiveType.Event):
                        break;
                    case PermissionType {Origin: NamedEventSet} _:
                        break;
                    default:
                        throw handler.TypeMismatch(context.bound, bound[0].Type, PrimitiveType.Event);
                }
            }
            
            var body = Visit(context.body);

            table = oldTable;

            if (context.quant.Text == "forall")
            {
                return new QuantExpr(context, QuantType.Forall, bound.ToList(), body, diff);
            }
            
            return new QuantExpr(context, QuantType.Exists, bound.ToList(), body, diff);
        }

        public override IPExpr VisitTestExpr(PParser.TestExprContext context)
        {
            var instance = Visit(context.instance);
            string name = context.kind.GetText();
            
            if (table.Lookup(name, out Machine m))
            {
                return new TestExpr(context, instance, m);
            }
            
            if (table.Lookup(name, out Event e))
            {
                return new TestExpr(context, instance, e);
            }
            
            if (table.Lookup(name, out State s))
            {
                return new TestExpr(context, instance, s);
            }
            
            throw handler.MissingDeclaration(context, "machine, event, or state", name);
        }
        
        public override IPExpr VisitTargetsExpr(PParser.TargetsExprContext context)
        {
            var instance = Visit(context.instance);
            var target = Visit(context.target);
            
            // TODO: type check to make sure instance is an event and machine is a machine
            return new TargetsExpr(context, instance, target);
        }
        
        public override IPExpr VisitFlyingExpr(PParser.FlyingExprContext context)
        {
            var instance = Visit(context.instance);
            
            // TODO: type check to make sure instance is an event
            return new FlyingExpr(context, instance);
        }
        
        public override IPExpr VisitSentExpr(PParser.SentExprContext context)
        {
            var instance = Visit(context.instance);
            
            // TODO: type check to make sure instance is an event
            return new SentExpr(context, instance);
        }
        
        public override IPExpr VisitBinExpr(PParser.BinExprContext context)
        {
            var lhs = Visit(context.lhs);
            var rhs = Visit(context.rhs);
            var op = context.op.Text;

            var arithCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"*", (elhs, erhs) => new BinOpExpr(context, BinOpType.Mul, elhs, erhs)},
                {"/", (elhs, erhs) => new BinOpExpr(context, BinOpType.Div, elhs, erhs)},
                {"%", (elhs, erhs) => new BinOpExpr(context, BinOpType.Mod, elhs, erhs)},
                {"+", (elhs, erhs) => new BinOpExpr(context, BinOpType.Add, elhs, erhs)},
                {"-", (elhs, erhs) => new BinOpExpr(context, BinOpType.Sub, elhs, erhs)},
                {"<", (elhs, erhs) => new BinOpExpr(context, BinOpType.Lt, elhs, erhs)},
                {"<=", (elhs, erhs) => new BinOpExpr(context, BinOpType.Le, elhs, erhs)},
                {">", (elhs, erhs) => new BinOpExpr(context, BinOpType.Gt, elhs, erhs)},
                {">=", (elhs, erhs) => new BinOpExpr(context, BinOpType.Ge, elhs, erhs)}
            };

            var logicCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"&&", (elhs, erhs) => new BinOpExpr(context, BinOpType.And, elhs, erhs)},
                {"||", (elhs, erhs) => new BinOpExpr(context, BinOpType.Or, elhs, erhs)},
                {"==>", (elhs, erhs) => new BinOpExpr(context, BinOpType.Then, elhs, erhs)},
                {"<==>", (elhs, erhs) => new BinOpExpr(context, BinOpType.Iff, elhs, erhs)}
            };

            var compCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"==", (elhs, erhs) => new BinOpExpr(context, BinOpType.Eq, elhs, erhs)},
                {"!=", (elhs, erhs) => new BinOpExpr(context, BinOpType.Neq, elhs, erhs)}
            };

            switch (op)
            {
                case "+":
                    if (PrimitiveType.String.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.String.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Int.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Float.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Float.IsAssignableFrom(rhs.Type))
                    {
                        return arithCtors[op](lhs, rhs);
                    }
                    throw handler.BinOpTypeMismatch(context, lhs.Type, rhs.Type);
                case "*":
                case "/":
                case "-":
                case "<":
                case ">":
                case ">=":
                case "<=":
                    if (PrimitiveType.Int.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Float.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Float.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.String.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.String.IsAssignableFrom(rhs.Type))
                    {
                        return arithCtors[op](lhs, rhs);
                    }
                    throw handler.BinOpTypeMismatch(context, lhs.Type, rhs.Type);
                case "%":
                    if (PrimitiveType.Int.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Float.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Float.IsAssignableFrom(rhs.Type))
                    {
                        return arithCtors[op](lhs, rhs);
                    }
                    throw handler.IncomparableTypes(context, lhs.Type, rhs.Type);
                case "in":
                    var rhsType = rhs.Type.Canonicalize();
                    if (rhsType is MapType rhsMap)
                    {
                        if (!rhsMap.KeyType.IsAssignableFrom(lhs.Type))
                        {
                            throw handler.TypeMismatch(context.lhs, lhs.Type, rhsMap.KeyType);
                        }
                    }
                    else if (rhsType is SequenceType rhsSeq)
                    {
                        if (!rhsSeq.ElementType.IsAssignableFrom(lhs.Type))
                        {
                            throw handler.TypeMismatch(context.lhs, lhs.Type, rhsSeq.ElementType);
                        }
                    }
                    else if (rhsType is SetType rhsSet)
                    {
                        if (!rhsSet.ElementType.IsAssignableFrom(lhs.Type))
                        {
                            throw handler.TypeMismatch(context.lhs, lhs.Type, rhsSet.ElementType);
                        }
                    }
                    else
                    {
                        throw handler.TypeMismatch(rhs, TypeKind.Map, TypeKind.Sequence);
                    }
                    return new ContainsExpr(context, lhs, rhs);

                case "==":
                case "!=":
                    if (!lhs.Type.IsAssignableFrom(rhs.Type) && !rhs.Type.IsAssignableFrom(lhs.Type))
                    {
                        throw handler.IncomparableTypes(context, lhs.Type, rhs.Type);
                    }

                    return compCtors[op](lhs, rhs);

                case "&&":
                case "||": 
                case "==>":
                case "<==>":
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
                    throw handler.InternalError(context,
                        new ArgumentException($"unknown binary operation {op}", nameof(context)));
            }
        }

        public override IPExpr VisitChooseExpr(PParser.ChooseExprContext context)
        {
            method.IsNondeterministic = true;
            // if choose is without an argument then its a choose boolean
            if (context.expr() == null)
            {
                return new ChooseExpr(context, null, PrimitiveType.Bool);
            }

            var subExpr = Visit(context.expr());
            var subExprType = subExpr.Type;

            switch (subExprType.Canonicalize())
            {
                case SequenceType seqType:
                    return new ChooseExpr(context, subExpr, seqType.ElementType);

                case SetType setType:
                    return new ChooseExpr(context, subExpr, setType.ElementType);

                case MapType mapType:
                    return new ChooseExpr(context, subExpr, mapType.KeyType);

                case PrimitiveType primType when primType.IsSameTypeAs(PrimitiveType.Int):
                {
                    if (subExpr is IntLiteralExpr subExprAsInt && subExprAsInt.Value > 10000)
                        throw handler.IllegalChooseSubExprValue(context, subExprAsInt.Value);
                    return new ChooseExpr(context, subExpr, PrimitiveType.Int);
                }

                default:
                    throw handler.IllegalChooseSubExprType(context, subExprType);
            }

        }

        public override IPExpr VisitCastExpr(PParser.CastExprContext context)
        {
            var subExpr = Visit(context.expr());
            var oldType = subExpr.Type;
            var newType = TypeResolver.ResolveType(context.type(), table, handler);
            if (context.cast.Text.Equals("as"))
            {
                if (!newType.IsAssignableFrom(oldType) && !oldType.IsAssignableFrom(newType))
                {
                    throw handler.IncomparableTypes(context, oldType, newType);
                }

                return new CastExpr(context, subExpr, newType);
            }

            if (context.cast.Text.Equals("to"))
            {
                if (!(newType is PermissionType || newType.IsSameTypeAs(PrimitiveType.Int) ||
                      newType.IsSameTypeAs(PrimitiveType.Float)))
                {
                    throw handler.IllegalTypeInCoerceExpr(context);
                }

                if (oldType.IsSameTypeAs(PrimitiveType.Int))
                {
                    if (newType.IsSameTypeAs(PrimitiveType.Int))
                    {
                        return subExpr;
                    }

                    if (newType.IsSameTypeAs(PrimitiveType.Float) || PLanguageType.TypeIsOfKind(newType, TypeKind.Enum))
                    {
                        return new CoerceExpr(context, subExpr, newType);
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
                        return new CoerceExpr(context, subExpr, newType);
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
                        return new CoerceExpr(context, subExpr, newType);
                    }
                }
                else if (oldType.IsSameTypeAs(PrimitiveType.Machine) || oldType is PermissionType)
                {
                    if (newType.IsSameTypeAs(oldType))
                    {
                        return subExpr;
                    }

                    if (newType is PermissionType)
                    {
                        if (oldType.IsSameTypeAs(PrimitiveType.Machine))
                        {
                            return new CoerceExpr(context, subExpr, newType);
                        }

                        if (oldType is PermissionType)
                        {
                            if (newType.AllowedPermissions.Value.Any(x => !oldType.AllowedPermissions.Value.Contains(x))
                               )
                            {
                                throw handler.IllegalInterfaceCoerce(context, oldType, newType);
                            }

                            return new CoerceExpr(context, subExpr, newType);
                        }
                    }
                }

                throw handler.IncomparableTypes(context, oldType, newType);
            }

            throw handler.InternalError(context, new ArgumentOutOfRangeException(nameof(context), "invalid cast"));
        }

        public override IPExpr VisitPrimitive(PParser.PrimitiveContext context)
        {
            if (context.iden() != null)
            {
                var symbolName = context.iden().GetText();
                if (table.Lookup(symbolName, out Variable variable))
                {
                    return new VariableAccessExpr(context, variable);
                }

                if (table.Lookup(symbolName, out EnumElem enumElem))
                {
                    return new EnumElemRefExpr(context, enumElem);
                }

                if (table.Lookup(symbolName, out Event evt))
                {
                    return new EventRefExpr(context, evt);
                }

                if (table.Lookup(symbolName, out Machine mac) && mac.IsSpec)
                {
                    return new SpecRefExpr(context, mac);
                }

                if (table.Lookup(symbolName, out Invariant inv))
                {
                    return new InvariantRefExpr(inv, context);
                }

                if (table.Lookup(symbolName, out InvariantGroup invGroup))
                {
                    return new InvariantGroupRefExpr(invGroup, context);
                }

                throw handler.MissingDeclaration(context.iden(), "variable, enum element, spec machine, or event", symbolName);
            }

            if (context.floatLiteral() != null)
            {
                return Visit(context.floatLiteral());
            }

            if (context.BoolLiteral() != null)
            {
                return new BoolLiteralExpr(context, context.BoolLiteral().GetText().Equals("true"));
            }

            if (context.IntLiteral() != null)
            {
                return new IntLiteralExpr(context, int.Parse(context.IntLiteral().GetText()));
            }

            if (context.NullLiteral() != null)
            {
                return new NullLiteralExpr(context);
            }

            if (context.NONDET() != null)
            {
                if (method.Owner?.IsSpec == true)
                {
                    throw handler.IllegalMonitorOperation(context, context.NONDET().Symbol, method.Owner);
                }

                method.IsNondeterministic = true;
                return new NondetExpr(context);
            }

            if (context.FAIRNONDET() != null)
            {
                if (method.Owner?.IsSpec == true)
                {
                    throw handler.IllegalMonitorOperation(context, context.FAIRNONDET().Symbol, method.Owner);
                }

                method.IsNondeterministic = true;
                return new FairNondetExpr(context);
            }

            if (context.HALT() != null)
            {
                var success = table.Lookup("halt", out Event haltEvent);
                Debug.Assert(success);
                return new EventRefExpr(context, haltEvent);
            }

            if (context.THIS() != null)
            {
                if (method.Owner == null)
                {
                    throw handler.MisplacedThis(context);
                }

                if (method.Owner.IsSpec)
                {
                    throw handler.IllegalMonitorOperation(context, context.THIS().Symbol, method.Owner);
                }

                return new ThisRefExpr(context, method.Owner);
            }

            throw handler.InternalError(context, new ArgumentOutOfRangeException(nameof(context), "unknown primitive"));
        }

        public override IPExpr VisitUnnamedTupleBody(PParser.UnnamedTupleBodyContext context)
        {
            var fields = context._fields.Select(Visit).ToArray();
            return new UnnamedTupleExpr(context, fields);
        }

        public override IPExpr VisitNamedTupleBody(PParser.NamedTupleBodyContext context)
        {
            var fields = context._values.Select(Visit).ToArray();

            var entries = new NamedTupleEntry[fields.Length];
            var names = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < fields.Length; i++)
            {
                var entryName = context._names[i].GetText();
                if (names.Contains(entryName))
                {
                    throw handler.DuplicateNamedTupleEntry(context._names[i], entryName);
                }

                names.Add(entryName);
                entries[i] = new NamedTupleEntry { Name = entryName, FieldNo = i, Type = fields[i].Type };
            }

            var type = new NamedTupleType(entries);
            return new NamedTupleExpr(context, fields, type);
        }

        public override IPExpr VisitExpFloat(PParser.ExpFloatContext context)
        {
            throw new NotImplementedException("float(x,y) syntax");
        }

        public override IPExpr VisitDecimalFloat(PParser.DecimalFloatContext context)
        {
            var value = double.Parse($"{context.pre?.Text ?? ""}.{context.post.Text}");
            return new FloatLiteralExpr(context, value);
        }

        public override IPExpr VisitRvalue(PParser.RvalueContext context)
        {
            return Visit(context.expr());
        }

        public override IPExpr VisitFormatedString([NotNull] PParser.FormatedStringContext context)
        {
            var baseString = context.StringLiteral().GetText();
            baseString = baseString.Substring(1, baseString.Length - 2); // strip beginning / end double quote
            var numNecessaryArgs = TypeCheckingUtils.PrintStmtNumArgs(baseString);
            if (numNecessaryArgs == -1)
            {
                throw handler.InvalidStringExprFormat(context, context.StringLiteral().Symbol);
            }

            var args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), this).ToList();

            if (args.Count != numNecessaryArgs)
            {
                throw handler.IncorrectArgumentCount(context, args.Count, numNecessaryArgs);
            }

            return new StringExpr(context, baseString, args);
        }

        public override IPExpr VisitStringExpr(PParser.StringExprContext context)
        {
            return VisitFormatedString(context.formatedString());
        }

        public override IPExpr VisitVarLvalue(PParser.VarLvalueContext context)
        {
            var varName = context.name.GetText();
            if (!table.Lookup(varName, out Variable variable))
            {
                throw handler.MissingDeclaration(context, "variable", varName);
            }

            return new VariableAccessExpr(context, variable);
        }

        public override IPExpr VisitNamedTupleLvalue(PParser.NamedTupleLvalueContext context)
        {
            var lvalue = Visit(context.lvalue());
            if (!(lvalue.Type.Canonicalize() is NamedTupleType type))
            {
                throw handler.TypeMismatch(lvalue, TypeKind.NamedTuple);
            }

            var field = context.field.GetText();
            if (!type.LookupEntry(field, out var entry))
            {
                throw handler.MissingNamedTupleEntry(context.field, type);
            }

            return new NamedTupleAccessExpr(context, lvalue, entry);
        }

        public override IPExpr VisitTupleLvalue(PParser.TupleLvalueContext context)
        {
            var lvalue = Visit(context.lvalue());
            if (!(lvalue.Type.Canonicalize() is TupleType type))
            {
                throw handler.TypeMismatch(lvalue, TypeKind.Tuple);
            }

            var field = int.Parse(context.@int().GetText());
            if (field >= type.Types.Count)
            {
                throw handler.OutOfBoundsTupleAccess(context.@int(), type);
            }

            return new TupleAccessExpr(context, lvalue, field, type.Types[field]);
        }

        public override IPExpr VisitMapOrSeqLvalue(PParser.MapOrSeqLvalueContext context)
        {
            var lvalue = Visit(context.lvalue());
            var index = Visit(context.expr());
            var indexType = index.Type;
            switch (lvalue.Type.Canonicalize())
            {
                case MapType mapType:
                    if (!mapType.KeyType.IsAssignableFrom(indexType))
                    {
                        throw handler.TypeMismatch(context.expr(), indexType, mapType.KeyType);
                    }

                    return new MapAccessExpr(context, lvalue, index, mapType.ValueType);

                case SequenceType seqType:
                    if (!PrimitiveType.Int.IsAssignableFrom(indexType))
                    {
                        throw handler.TypeMismatch(context.expr(), indexType, PrimitiveType.Int);
                    }

                    return new SeqAccessExpr(context, lvalue, index, seqType.ElementType);

                default:
                    throw handler.TypeMismatch(lvalue, TypeKind.Sequence, TypeKind.Map);
            }
        }
    }
}