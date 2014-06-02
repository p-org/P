namespace Microsoft.Pc.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using QUT.Gppg;

    using Domains;
    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Generators;
    using Microsoft.Formula.API.Nodes;

    internal partial class Parser : ShiftReduceParser<LexValue, LexLocation>
    {
        private static readonly P_Root.Exprs TheDefaultExprs = new P_Root.Exprs();

        private ProgramName parseSource;
        private List<Flag> parseFlags;
        private PProgram parseProgram;

        private bool parseFailed = false;

        private Span crntAnnotSpan;
        private bool isTrigAnnotated = false;
        private P_Root.FunDecl crntFunDecl = null;
        private P_Root.EventDecl crntEventDecl = null;
        private P_Root.MachineDecl crntMachDecl = null;
        private P_Root.QualifiedName crntQualName = null;
        private P_Root.StateDecl crntState = null;
        private List<P_Root.VarDecl> crntVarList = new List<P_Root.VarDecl>();
        private List<P_Root.EventLabel> crntEventList = new List<P_Root.EventLabel>();
        private List<Tuple<P_Root.StringCnst, P_Root.AnnotValue>> crntAnnotList = new List<Tuple<P_Root.StringCnst, P_Root.AnnotValue>>();
        private Stack<List<Tuple<P_Root.StringCnst, P_Root.AnnotValue>>> crntAnnotStack = new Stack<List<Tuple<P_Root.StringCnst, P_Root.AnnotValue>>>();

        private Stack<P_Root.Expr> valueExprStack = new Stack<P_Root.Expr>();
        private Stack<P_Root.ExprsExt> exprsStack = new Stack<P_Root.ExprsExt>();
        private Stack<P_Root.TypeExpr> typeExprStack = new Stack<P_Root.TypeExpr>();
        private Stack<P_Root.Stmt> stmtStack = new Stack<P_Root.Stmt>();
        private Stack<P_Root.QualifiedName> groupStack = new Stack<P_Root.QualifiedName>();

        public P_Root.TypeExpr Debug_PeekTypeStack
        {
            get { return typeExprStack.Peek(); }
        }

        public Parser()
            : base(new Scanner())
        {
        }

        internal bool ParseText(
            ProgramName file, 
            string programText, 
            out List<Flag> flags,
            out PProgram program)
        {
            flags = parseFlags = new List<Flag>();
            program = parseProgram = new PProgram();
            parseSource = file;
            bool result;
            try
            {
                var str = new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes(programText));
                var scanner = ((Scanner)Scanner);
                scanner.SetSource(str);
                scanner.SourceProgram = file;
                scanner.Flags = flags;
                scanner.Failed = false;
                ResetState();
                result = (!scanner.Failed) && Parse(default(System.Threading.CancellationToken)) && !parseFailed;
                str.Close();
            }
            catch (Exception e)
            {
                var badFile = new Flag(
                    SeverityKind.Error,
                    default(Span),
                    Constants.BadFile.ToString(e.Message),
                    Constants.BadFile.Code,
                    file);
                flags.Add(badFile);
                return false;
            }

            return result;
        }

        private Span ToSpan(LexLocation loc)
        {
            return new Span(loc.StartLine, loc.StartColumn + 1, loc.EndLine, loc.EndColumn + 1);
        }

        #region Pushers
        private void PushAnnotationSet()
        {
            crntAnnotStack.Push(crntAnnotList);
            crntAnnotList = new List<Tuple<P_Root.StringCnst, P_Root.AnnotValue>>();
        }

        private void PushTypeExpr(P_Root.TypeExpr typeExpr)
        {
            typeExprStack.Push(typeExpr);
        }

        private void PushSeqType(Span span)
        {
            Contract.Assert(typeExprStack.Count > 0);
            var seqType = P_Root.MkSeqType((P_Root.IArgType_SeqType__0)typeExprStack.Pop());
            seqType.Span = span;
            typeExprStack.Push(seqType);
        }

        private void PushTupType(Span span, bool isLast)
        {
            var tupType = P_Root.MkTupType();
            tupType.Span = span;
            if (isLast)
            {
                Contract.Assert(typeExprStack.Count > 0);
                tupType.hd = (P_Root.IArgType_TupType__0)typeExprStack.Pop();
                tupType.tl = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }
            else
            {
                Contract.Assert(typeExprStack.Count > 1);
                tupType.tl = (P_Root.IArgType_TupType__1)typeExprStack.Pop();
                tupType.hd = (P_Root.IArgType_TupType__0)typeExprStack.Pop();
            }

            typeExprStack.Push(tupType);
        }

        private void PushNmdTupType(string fieldName, Span span, bool isLast)
        {
            var tupType = P_Root.MkNmdTupType();
            var tupFld = P_Root.MkNmdTupTypeField();

            tupType.Span = span;
            tupFld.Span = span;
            tupFld.name = MkString(fieldName, span);
            tupType.hd = tupFld;
            if (isLast)
            {
                Contract.Assert(typeExprStack.Count > 0);
                tupFld.type = (P_Root.IArgType_NmdTupTypeField__1)typeExprStack.Pop();
                tupType.tl = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }
            else
            {
                Contract.Assert(typeExprStack.Count > 1);
                tupType.tl = (P_Root.IArgType_NmdTupType__1)typeExprStack.Pop();
                tupFld.type = (P_Root.IArgType_NmdTupTypeField__1)typeExprStack.Pop();
            }

            typeExprStack.Push(tupType);
        }

        private void PushMapType(Span span)
        {
            Contract.Assert(typeExprStack.Count > 1);
            var mapType = P_Root.MkMapType();
            mapType.v = (P_Root.IArgType_MapType__1)typeExprStack.Pop();
            mapType.k = (P_Root.IArgType_MapType__0)typeExprStack.Pop();
            mapType.Span = span;
            typeExprStack.Push(mapType);
        }

        private void PushPush(Span span)
        {
            Contract.Assert(crntQualName != null);
            var pushStmt = P_Root.MkPush(crntQualName);
            pushStmt.Span = span;
            stmtStack.Push(pushStmt);
            crntQualName = null;
        }

        private void PushSend(bool hasArgs, Span span)
        {
            Contract.Assert(!hasArgs || exprsStack.Count > 0);
            Contract.Assert(valueExprStack.Count > 1);

            var sendStmt = P_Root.MkSend();
            sendStmt.Span = span;
            if (hasArgs)
            {
                var arg = exprsStack.Pop();
                if (arg.Symbol == TheDefaultExprs.Symbol)
                {
                    sendStmt.arg = P_Root.MkTuple((P_Root.IArgType_Tuple__0)arg);
                    sendStmt.arg.Span = arg.Span;
                }
                else
                {
                    sendStmt.arg = (P_Root.IArgType_Send__2)arg;
                }

                sendStmt.ev = (P_Root.IArgType_Send__1)valueExprStack.Pop();
                sendStmt.dest = (P_Root.IArgType_Send__0)valueExprStack.Pop();
            }
            else
            {
                sendStmt.ev = (P_Root.IArgType_Send__1)valueExprStack.Pop();
                sendStmt.dest = (P_Root.IArgType_Send__0)valueExprStack.Pop();
                sendStmt.arg = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            stmtStack.Push(sendStmt);
        }

        private void PushMonitor(bool hasArgs, string name, Span nameSpan, Span span)
        {
            Contract.Assert(!hasArgs || exprsStack.Count > 0);
            Contract.Assert(valueExprStack.Count > 0);

            var monitorStmt = P_Root.MkMonitor();
            monitorStmt.name = MkString(name, nameSpan);
            monitorStmt.Span = span;
            if (hasArgs)
            {
                var arg = exprsStack.Pop();
                if (arg.Symbol == TheDefaultExprs.Symbol)
                {
                    monitorStmt.arg = P_Root.MkTuple((P_Root.IArgType_Tuple__0)arg);
                    monitorStmt.arg.Span = arg.Span;
                }
                else
                {
                    monitorStmt.arg = (P_Root.IArgType_Monitor__2)arg;
                }

                monitorStmt.ev = (P_Root.IArgType_Monitor__1)valueExprStack.Pop();
            }
            else
            {
                monitorStmt.ev = (P_Root.IArgType_Monitor__1)valueExprStack.Pop();
                monitorStmt.arg = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            stmtStack.Push(monitorStmt);
        }

        private void PushRaise(bool hasArgs, Span span)
        {
            Contract.Assert(!hasArgs || exprsStack.Count > 0);
            Contract.Assert(valueExprStack.Count > 0);

            var raiseStmt = P_Root.MkRaise();
            raiseStmt.Span = span;
            if (hasArgs)
            {
                var arg = exprsStack.Pop();
                if (arg.Symbol == TheDefaultExprs.Symbol)
                {
                    raiseStmt.arg = P_Root.MkTuple((P_Root.IArgType_Tuple__0)arg);
                    raiseStmt.arg.Span = arg.Span;
                }
                else
                {
                    raiseStmt.arg = (P_Root.IArgType_Raise__1)arg;
                }

                raiseStmt.ev = (P_Root.IArgType_Raise__0)valueExprStack.Pop();
            }
            else
            {
                raiseStmt.ev = (P_Root.IArgType_Raise__0)valueExprStack.Pop();
                raiseStmt.arg = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            stmtStack.Push(raiseStmt);
        }

        private void PushNewStmt(string name, bool hasArgs, Span nameSpan, Span span)
        {
            Contract.Assert(!hasArgs || exprsStack.Count > 0);
            var newStmt = P_Root.MkNewStmt();
            newStmt.name = MkString(name, nameSpan);
            newStmt.Span = span;
            if (hasArgs)
            {
                var arg = exprsStack.Pop();
                if (arg.Symbol == TheDefaultExprs.Symbol)
                {
                    newStmt.arg = P_Root.MkTuple((P_Root.IArgType_Tuple__0)arg);
                    newStmt.arg.Span = arg.Span;
                }
                else
                {
                    newStmt.arg = (P_Root.IArgType_NewStmt__1)arg;
                }
            }
            else
            {
                newStmt.arg = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            stmtStack.Push(newStmt);
        }

        private void PushNewExpr(string name, bool hasArgs, Span nameSpan, Span span)
        {
            Contract.Assert(!hasArgs || exprsStack.Count > 0);
            var newExpr = P_Root.MkNew();
            newExpr.name = MkString(name, nameSpan);
            newExpr.Span = span;
            if (hasArgs)
            {
                var arg = exprsStack.Pop();
                if (arg.Symbol == TheDefaultExprs.Symbol)
                {
                    newExpr.arg = P_Root.MkTuple((P_Root.IArgType_Tuple__0)arg);
                    newExpr.arg.Span = arg.Span;
                }
                else
                {
                    newExpr.arg = (P_Root.IArgType_New__1)arg;
                }
            }
            else
            {
                newExpr.arg = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            valueExprStack.Push(newExpr);
        }

        private void PushFunStmt(string name, bool hasArgs, Span span)
        {
            Contract.Assert(!hasArgs || exprsStack.Count > 0);
            var funStmt = P_Root.MkFunStmt();
            funStmt.name = MkString(name, span);
            funStmt.Span = span;
            if (hasArgs)
            {
                funStmt.args = (P_Root.Exprs)exprsStack.Pop();
            }
            else
            {
                funStmt.args = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            stmtStack.Push(funStmt);
        }

        private void PushFunExpr(string name, bool hasArgs, Span span)
        {
            Contract.Assert(!hasArgs || exprsStack.Count > 0);
            var funExpr = P_Root.MkFunApp();
            funExpr.name = MkString(name, span);
            funExpr.Span = span;
            if (hasArgs)
            {
                funExpr.args = (P_Root.Exprs)exprsStack.Pop();
            }
            else
            {
                funExpr.args = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            valueExprStack.Push(funExpr);
        }

        private void PushTupleExpr(bool isUnaryTuple)
        {
            Contract.Assert(valueExprStack.Count > 0);
            P_Root.Exprs fullExprs;
            var arg = (P_Root.IArgType_Exprs__0)valueExprStack.Pop();
            if (isUnaryTuple)
            {
                fullExprs = P_Root.MkExprs(arg, MkUserCnst(P_Root.UserCnstKind.NIL, arg.Span));
            }
            else
            {
                fullExprs = P_Root.MkExprs(arg, (P_Root.Exprs)exprsStack.Pop());
            }
            
            var tuple = P_Root.MkTuple(fullExprs);
            fullExprs.Span = arg.Span;
            tuple.Span = arg.Span;
            valueExprStack.Push(tuple);
        }

        private void PushNmdTupleExpr(string name, Span span, bool isUnaryTuple)
        {
            Contract.Assert(valueExprStack.Count > 0);
            P_Root.NamedExprs fullExprs;
            var arg = (P_Root.IArgType_NamedExprs__1)valueExprStack.Pop();
            if (isUnaryTuple)
            {
                fullExprs = P_Root.MkNamedExprs(
                    MkString(name, span),
                    arg, 
                    MkUserCnst(P_Root.UserCnstKind.NIL, arg.Span));
            }
            else
            {
                fullExprs = P_Root.MkNamedExprs(
                    MkString(name, span),                    
                    arg,
                    (P_Root.NamedExprs)exprsStack.Pop());
            }

            var tuple = P_Root.MkNamedTuple(fullExprs);
            fullExprs.Span = span;
            tuple.Span = span;
            valueExprStack.Push(tuple);
        }

        private void PushExprs()
        {
            Contract.Assert(exprsStack.Count > 0);
            Contract.Assert(valueExprStack.Count > 0);

            var oldExprs = exprsStack.Pop();
            if (oldExprs.Symbol != TheDefaultExprs.Symbol)
            {
                var coercedExprs = P_Root.MkExprs(
                    (P_Root.IArgType_Exprs__0)oldExprs, 
                    MkUserCnst(P_Root.UserCnstKind.NIL, oldExprs.Span));
                coercedExprs.Span = oldExprs.Span;
                oldExprs = coercedExprs;
            }

            var arg = (P_Root.IArgType_Exprs__0)valueExprStack.Pop();
            var exprs = P_Root.MkExprs(arg, (P_Root.IArgType_Exprs__1)oldExprs);
            exprs.Span = arg.Span;
            exprsStack.Push(exprs);
        }

        private void PushNmdExprs(string name, Span span)
        {
            Contract.Assert(valueExprStack.Count > 0);
            Contract.Assert(exprsStack.Count > 0);
            var exprs = P_Root.MkNamedExprs(
                MkString(name, span),
                (P_Root.IArgType_NamedExprs__1)valueExprStack.Pop(),
                (P_Root.NamedExprs)exprsStack.Pop());
            exprs.Span = span;
            exprsStack.Push(exprs);
        }

        private void MoveValToNmdExprs(string name, Span span)
        {
            Contract.Assert(valueExprStack.Count > 0);
            var exprs = P_Root.MkNamedExprs(
                MkString(name, span),
                (P_Root.IArgType_NamedExprs__1)valueExprStack.Pop(),
                MkUserCnst(P_Root.UserCnstKind.NIL, span));
            exprs.Span = span;
            exprsStack.Push(exprs);
        }

        private void MoveValToExprs(bool makeIntoExprs)
        {
            Contract.Assert(valueExprStack.Count > 0);
            if (makeIntoExprs)
            {
                var arg = (P_Root.IArgType_Exprs__0)valueExprStack.Pop();
                var exprs = P_Root.MkExprs(arg, MkUserCnst(P_Root.UserCnstKind.NIL, arg.Span));
                exprs.Span = arg.Span;
                exprsStack.Push(exprs);
            }
            else
            {
                exprsStack.Push((P_Root.ExprsExt)valueExprStack.Pop());
            }
        }

        private void PushNulStmt(P_Root.UserCnstKind op, Span span)
        {
            var nulStmt = P_Root.MkNulStmt(MkUserCnst(op, span));
            nulStmt.Span = span;
            stmtStack.Push(nulStmt);
        }

        private void PushSeq()
        {
            Contract.Assert(stmtStack.Count > 1);
            var seqStmt = P_Root.MkSeq();
            seqStmt.s2 = (P_Root.IArgType_Seq__1)stmtStack.Pop();
            seqStmt.s1 = (P_Root.IArgType_Seq__0)stmtStack.Pop();
            seqStmt.Span = seqStmt.s1.Span;
            stmtStack.Push(seqStmt);
        }

        private void PushNulExpr(P_Root.UserCnstKind op, Span span)
        {
            var nulExpr = P_Root.MkNulApp(MkUserCnst(op, span));
            nulExpr.Span = span;
            valueExprStack.Push(nulExpr);
        }

        private void PushUnExpr(P_Root.UserCnstKind op, Span span)
        {
            Contract.Assert(valueExprStack.Count > 0);
            var unExpr = P_Root.MkUnApp();
            unExpr.op = MkUserCnst(op, span);
            unExpr.arg1 = (P_Root.IArgType_UnApp__1)valueExprStack.Pop();
            unExpr.Span = span;
            valueExprStack.Push(unExpr);
        }

        private void PushDefaultExpr(Span span)
        {
            Contract.Assert(typeExprStack.Count > 0);
            var defExpr = P_Root.MkDefault();
            defExpr.type = (P_Root.IArgType_Default__0)typeExprStack.Pop();
            defExpr.Span = span;
            valueExprStack.Push(defExpr);
        }

        private void PushIntExpr(string intStr, Span span)
        {
            int val;
            if (!int.TryParse(intStr, out val) || val < 0)
            {
                var errFlag = new Flag(
                                 SeverityKind.Error,
                                 span,
                                 Constants.BadSyntax.ToString(string.Format("Bad int constant {0}", intStr)),
                                 Constants.BadSyntax.Code,
                                 parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
            }

            var nulExpr = P_Root.MkNulApp(MkNumeric(val, span));
            nulExpr.Span = span;
            valueExprStack.Push(nulExpr);
        }

        private void PushCast(Span span)
        {
            Contract.Assert(valueExprStack.Count > 0);
            Contract.Assert(typeExprStack.Count > 0);
            var cast = P_Root.MkCast(
                (P_Root.IArgType_Cast__0)valueExprStack.Pop(),
                (P_Root.IArgType_Cast__1)typeExprStack.Pop());
            cast.Span = span;
            valueExprStack.Push(cast);
        }

        private void PushIte(bool hasElse, Span span)
        {
            Contract.Assert(valueExprStack.Count > 0);
            var ite = P_Root.MkIte();
            ite.Span = span;
            ite.cond = (P_Root.IArgType_Ite__0)valueExprStack.Pop();
            if (hasElse)
            {
                Contract.Assert(stmtStack.Count > 1);
                ite.@false = (P_Root.IArgType_Ite__2)stmtStack.Pop();
                ite.@true = (P_Root.IArgType_Ite__1)stmtStack.Pop();
            }
            else
            {
                Contract.Assert(stmtStack.Count > 0);
                var skip = P_Root.MkNulStmt(MkUserCnst(P_Root.UserCnstKind.SKIP, span));
                skip.Span = span;
                ite.@true = (P_Root.IArgType_Ite__1)stmtStack.Pop();
                ite.@false = skip;
            }

            stmtStack.Push(ite);
        }

        private void PushReturn(bool returnsValue, Span span)
        {
            Contract.Assert(!returnsValue || valueExprStack.Count > 0);
            var retStmt = P_Root.MkReturn();
            retStmt.Span = span;
            if (returnsValue)
            {
                retStmt.expr = (P_Root.IArgType_Return__0)valueExprStack.Pop();
            }
            else
            {
                retStmt.expr = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            stmtStack.Push(retStmt);
        }

        private void PushWhile(Span span)
        {
            Contract.Assert(valueExprStack.Count > 0 && stmtStack.Count > 0);
            var whileStmt = P_Root.MkWhile(
                (P_Root.IArgType_While__0)valueExprStack.Pop(),
                (P_Root.IArgType_While__1)stmtStack.Pop());
            whileStmt.Span = span;
            stmtStack.Push(whileStmt);
        }

        private void PushUnStmt(P_Root.UserCnstKind op, Span span)
        {
            Contract.Assert(valueExprStack.Count > 0);
            var unStmt = P_Root.MkUnStmt();
            unStmt.op = MkUserCnst(op, span);
            unStmt.arg1 = (P_Root.IArgType_UnStmt__1)valueExprStack.Pop();
            unStmt.Span = span;
            stmtStack.Push(unStmt);
        }

        private void PushBinStmt(P_Root.UserCnstKind op, Span span)
        {
            Contract.Assert(valueExprStack.Count > 1);
            var binStmt = P_Root.MkBinStmt();
            binStmt.op = MkUserCnst(op, span);
            binStmt.arg2 = (P_Root.IArgType_BinStmt__2)valueExprStack.Pop();
            binStmt.arg1 = (P_Root.IArgType_BinStmt__1)valueExprStack.Pop();
            binStmt.Span = span;
            stmtStack.Push(binStmt);
        }

        private void PushBinExpr(P_Root.UserCnstKind op, Span span)
        {
            Contract.Assert(valueExprStack.Count > 1);
            var binApp = P_Root.MkBinApp();
            binApp.op = MkUserCnst(op, span);
            binApp.arg2 = (P_Root.IArgType_BinApp__2)valueExprStack.Pop();
            binApp.arg1 = (P_Root.IArgType_BinApp__1)valueExprStack.Pop();
            binApp.Span = span;
            valueExprStack.Push(binApp);
        }

        private void PushName(string name, Span span)
        {
            var nameNode = P_Root.MkName(MkString(name, span));
            nameNode.Span = span;
            valueExprStack.Push(nameNode);
        }

        private void PushField(string name, Span span)
        {
            Contract.Assert(valueExprStack.Count > 0);
            var field = P_Root.MkField();
            field.name = MkString(name, span);
            field.arg = (P_Root.IArgType_Field__0)valueExprStack.Pop();
            field.Span = span;
            valueExprStack.Push(field);
        }

        private void PushGroup(string name, Span nameSpan, Span span)
        {
            var groupName = P_Root.MkQualifiedName(MkString(name, nameSpan));
            groupName.Span = span;
            if (groupStack.Count == 0)
            {
                groupName.qualifier = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }
            else
            {
                groupName.qualifier = groupStack.Peek();
            }

            groupStack.Push(groupName);
        }

        private void Qualify(string name, Span span)
        {
            if (crntQualName == null)
            {
                crntQualName = P_Root.MkQualifiedName(
                    MkString(name, span),
                    MkUserCnst(P_Root.UserCnstKind.NIL, span));
            }
            else
            {
                crntQualName = P_Root.MkQualifiedName(
                    MkString(name, span),
                    crntQualName);

            }

            crntQualName.Span = span;
        }

        #endregion

        #region Node setters
        private void SetEventCard(string cardStr, bool isAssert, Span span)
        {
            var evDecl = GetCurrentEventDecl(span);
            int card;
            if (!int.TryParse(cardStr, out card) || card < 0)
            {
                var errFlag = new Flag(
                                 SeverityKind.Error,
                                 span,
                                 Constants.BadSyntax.ToString(string.Format("Bad event cardinality {0}", cardStr)),
                                 Constants.BadSyntax.Code,
                                 parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
                return;
            }

            if (isAssert)
            {
                var assertNode = P_Root.MkAssertMaxInstances(MkNumeric(card, span));
                assertNode.Span = span;
                evDecl.card = assertNode;
            }
            else
            {
                var assumeNode = P_Root.MkAssumeMaxInstances(MkNumeric(card, span));
                assumeNode.Span = span;
                evDecl.card = assumeNode;
            }
        }

        private void SetMachineCard(string cardStr, bool isAssert, Span span)
        {
            var machDecl = GetCurrentMachineDecl(span);
            int card;
            if (!int.TryParse(cardStr, out card) || card < 0)
            {
                var errFlag = new Flag(
                                 SeverityKind.Error,
                                 span,
                                 Constants.BadSyntax.ToString(string.Format("Bad machine cardinality {0}", cardStr)),
                                 Constants.BadSyntax.Code,
                                 parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
                return;
            }

            if (isAssert)
            {
                var assertNode = P_Root.MkAssertMaxInstances(MkNumeric(card, span));
                assertNode.Span = span;
                machDecl.card = assertNode;
            }
            else
            {
                var assumeNode = P_Root.MkAssumeMaxInstances(MkNumeric(card, span));
                assumeNode.Span = span;
                machDecl.card = assumeNode;
            }
        }

        private void SetStateIsStable(Span span)
        {
            var state = GetCurrentStateDecl(span);
            state.isStable = MkUserCnst(P_Root.UserCnstKind.TRUE, span);
        }

        private void SetTrigAnnotated(Span span)
        {
            crntAnnotSpan = span;
            isTrigAnnotated = true;
        }

        private void SetStateEntry()
        {
            Contract.Assert(stmtStack.Count > 0);
            var entry = stmtStack.Pop();
            var state = GetCurrentStateDecl(entry.Span);

            if (state.entryFun is P_Root.NulStmt &&
                (P_Root.UserCnstKind)((P_Root.UserCnst)((P_Root.NulStmt)state.entryFun)[0]).Value == P_Root.UserCnstKind.SKIP)
            {
                state.entryFun = (P_Root.IArgType_StateDecl__2)entry;
            }
            else
            {
                var errFlag = new Flag(
                                 SeverityKind.Error,
                                 entry.Span,
                                 Constants.BadSyntax.ToString("Too many entry functions"),
                                 Constants.BadSyntax.Code,
                                 parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
            }
        }

        private void SetStateExit()
        {
            Contract.Assert(stmtStack.Count > 0);
            var exit = stmtStack.Pop();
            var state = GetCurrentStateDecl(exit.Span);

            if (state.exitFun is P_Root.NulStmt &&
                (P_Root.UserCnstKind)((P_Root.UserCnst)((P_Root.NulStmt)state.exitFun)[0]).Value == P_Root.UserCnstKind.SKIP)
            {
                state.exitFun = (P_Root.IArgType_StateDecl__3)exit;
            }
            else
            {
                var errFlag = new Flag(
                                 SeverityKind.Error,
                                 exit.Span,
                                 Constants.BadSyntax.ToString("Too many exit functions"),
                                 Constants.BadSyntax.Code,
                                 parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
            }
        }

        private void SetMachineIsMain(Span span)
        {
            var machDecl = GetCurrentMachineDecl(span);
            machDecl.isMain = MkUserCnst(P_Root.UserCnstKind.TRUE, span);
        }

        private void SetEventType(Span span)
        {
            var evDecl = GetCurrentEventDecl(span);
            Contract.Assert(typeExprStack.Count > 0);
            evDecl.type = (P_Root.IArgType_EventDecl__2)typeExprStack.Pop();
        }

        private void SetFunKind(P_Root.UserCnstKind kind, Span span)
        {
            var funDecl = GetCurrentFunDecl(span);
            funDecl.kind = MkUserCnst(kind, span);
        }

        private void SetFunParams(Span span)
        {
            Contract.Assert(typeExprStack.Count > 0);
            var funDecl = GetCurrentFunDecl(span);
            funDecl.@params = (P_Root.IArgType_FunDecl__3)typeExprStack.Pop();
        }

        private void SetFunReturn(Span span)
        {
            Contract.Assert(typeExprStack.Count > 0);
            var funDecl = GetCurrentFunDecl(span);
            funDecl.@return = (P_Root.IArgType_FunDecl__4)typeExprStack.Pop();
        }
        #endregion

        #region Adders
        private void AddGroup()
        {
            groupStack.Pop();
        }

        private void AddVarDecl(string name, Span span)
        {
            var varDecl = P_Root.MkVarDecl();
            varDecl.name = MkString(name, span);
            varDecl.owner = GetCurrentMachineDecl(span);
            varDecl.Span = span;
            crntVarList.Add(varDecl);
        }

        private void AddToEventList(string name, Span span)
        {
            crntEventList.Add(MkString(name, span));
        }

        private void AddToEventList(P_Root.UserCnstKind kind, Span span)
        {
            crntEventList.Add(MkUserCnst(kind, span));
        }

        private void AddDefersOrIgnores(bool isDefer, Span span)
        {
            Contract.Assert(crntEventList.Count > 0);
            Contract.Assert(!isTrigAnnotated || crntAnnotStack.Count > 0);
            var state = GetCurrentStateDecl(span);
            var kind = MkUserCnst(isDefer ? P_Root.UserCnstKind.DEFER : P_Root.UserCnstKind.IGNORE, span);
            var annots = isTrigAnnotated ? crntAnnotStack.Pop() : null;
            foreach (var e in crntEventList)
            {
                var defOrIgn = P_Root.MkDefIgnDecl(state, (P_Root.IArgType_DefIgnDecl__1)e, kind);
                defOrIgn.Span = span;
                parseProgram.DefersOrIgnores.Add(defOrIgn);
                if (isTrigAnnotated)
                {
                    foreach (var kv in annots)
                    {
                        var annot = P_Root.MkAnnotation(
                            defOrIgn,
                            kv.Item1,
                            (P_Root.IArgType_Annotation__2)kv.Item2);
                        annot.Span = crntAnnotSpan;
                        parseProgram.Annotations.Add(annot);
                    }
                }
            }

            isTrigAnnotated = false;
            crntEventList.Clear();
        }

        private void AddTransition(bool isPush, bool hasStmtAction, Span span)
        {
            Contract.Assert(crntEventList.Count > 0);
            Contract.Assert(crntQualName != null);
            Contract.Assert(!hasStmtAction || stmtStack.Count > 0);
            Contract.Assert(!(hasStmtAction && isPush));
            Contract.Assert(!isTrigAnnotated || crntAnnotStack.Count > 0);

            var annots = isTrigAnnotated ? crntAnnotStack.Pop() : null;
            var state = GetCurrentStateDecl(span);
            P_Root.IArgType_TransDecl__3 action;
            if (isPush)
            {
                action = MkUserCnst(P_Root.UserCnstKind.PUSH, span);
            }
            else if (hasStmtAction)
            {
                action = (P_Root.IArgType_TransDecl__3)stmtStack.Pop();
            }
            else
            {
                action = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            }

            foreach (var e in crntEventList)
            {
                var trans = P_Root.MkTransDecl(state, (P_Root.IArgType_TransDecl__1)e, crntQualName, action);
                trans.Span = span;
                parseProgram.Transitions.Add(trans);
                if (isTrigAnnotated)
                {
                    foreach (var kv in annots)
                    {
                        var annot = P_Root.MkAnnotation(
                            trans,
                            kv.Item1,
                            (P_Root.IArgType_Annotation__2)kv.Item2);
                        annot.Span = crntAnnotSpan;
                        parseProgram.Annotations.Add(annot);
                    }
                }
            }

            isTrigAnnotated = false;
            crntQualName = null;
            crntEventList.Clear();
        }

        private void AddProgramAnnots(Span span)
        {
            Contract.Assert(crntAnnotStack.Count > 0);
            var annots = crntAnnotStack.Pop();
            foreach (var kv in annots)
            {
                var annot = P_Root.MkAnnotation(
                    MkUserCnst(P_Root.UserCnstKind.NIL, span),
                    kv.Item1,
                    (P_Root.IArgType_Annotation__2)kv.Item2);
                annot.Span = span;
                parseProgram.Annotations.Add(annot);  
            }
        }

        private void AddAnnotStringVal(string keyName, string valStr, Span keySpan, Span valSpan)
        {
            crntAnnotList.Add(
                new Tuple<P_Root.StringCnst, P_Root.AnnotValue>(
                    MkString(keyName, keySpan),
                    MkString(valStr, valSpan)));
        }

        private void AddAnnotIntVal(string keyName, string intStr, Span keySpan, Span valSpan)
        {
            int val;
            if (!int.TryParse(intStr, out val) || val < 0)
            {
                var errFlag = new Flag(
                                 SeverityKind.Error,
                                 valSpan,
                                 Constants.BadSyntax.ToString(string.Format("Bad int constant {0}", intStr)),
                                 Constants.BadSyntax.Code,
                                 parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
                return;
            }

            crntAnnotList.Add(
                new Tuple<P_Root.StringCnst, P_Root.AnnotValue>(
                    MkString(keyName, keySpan),
                    MkNumeric(val, valSpan)));
        }

        private void AddAnnotUsrCnstVal(string keyName, P_Root.UserCnstKind valKind, Span keySpan, Span valSpan)
        {
            crntAnnotList.Add(
                new Tuple<P_Root.StringCnst, P_Root.AnnotValue>(
                    MkString(keyName, keySpan),
                    MkUserCnst(valKind, valSpan)));
        }

        private void AddAction(string name, Span nameSpan, Span span)
        {
            Contract.Assert(crntEventList.Count > 0);
            Contract.Assert(!isTrigAnnotated || crntAnnotStack.Count > 0);

            var state = GetCurrentStateDecl(span);
            var actName = MkString(name, nameSpan);
            var annots = isTrigAnnotated ? crntAnnotStack.Pop() : null;
            foreach (var e in crntEventList)
            {
                var action = P_Root.MkActionDecl(state, (P_Root.IArgType_ActionDecl__1)e, actName);
                action.Span = span;
                parseProgram.Actions.Add(action);
                if (isTrigAnnotated)
                {
                    foreach (var kv in annots)
                    {
                        var annot = P_Root.MkAnnotation(
                            action,
                            kv.Item1,
                            (P_Root.IArgType_Annotation__2)kv.Item2);
                        annot.Span = crntAnnotSpan;
                        parseProgram.Annotations.Add(annot);
                    }
                }
            }

            isTrigAnnotated = false;
            crntEventList.Clear();
        }

        private void AddState(string name, bool isStart, Span nameSpan, Span span)
        {
            var state = GetCurrentStateDecl(span);
            state.Span = span;
            parseProgram.States.Add(state);
            if (groupStack.Count == 0)
            {
                state.name = P_Root.MkQualifiedName(
                    MkString(name, nameSpan),
                    MkUserCnst(P_Root.UserCnstKind.NIL, span));
            }
            else
            {
                state.name = P_Root.MkQualifiedName(MkString(name, nameSpan), groupStack.Peek());
            }

            if (isStart)
            {
                var machDecl = GetCurrentMachineDecl(span);
                if (string.IsNullOrEmpty(((P_Root.StringCnst)machDecl.start[0]).Value))
                {
                    machDecl.start = (P_Root.QualifiedName)state.name;
                }
                else
                {
                    var errFlag = new Flag(
                                     SeverityKind.Error,
                                     span,
                                     Constants.BadSyntax.ToString("Too many start states"),
                                     Constants.BadSyntax.Code,
                                     parseSource);
                    parseFailed = true;
                    parseFlags.Add(errFlag);
                }
            }

            crntState = null;
        }

        private void AddVarDecls(bool hasAnnots, Span annotSpan)
        {
            Contract.Assert(typeExprStack.Count > 0);
            Contract.Assert(crntVarList.Count > 0);
            Contract.Assert(!hasAnnots || crntAnnotStack.Count > 0);
            var typeExpr = (P_Root.IArgType_VarDecl__2)typeExprStack.Pop();
            var annots = hasAnnots ? crntAnnotStack.Pop() : null;
            foreach (var vd in crntVarList)
            {
                vd.type = typeExpr;
                parseProgram.Variables.Add(vd);

                if (hasAnnots)
                {
                    foreach (var kv in annots)
                    {
                        var annot = P_Root.MkAnnotation(
                            vd,
                            kv.Item1,
                            (P_Root.IArgType_Annotation__2)kv.Item2);
                        annot.Span = annotSpan;
                        parseProgram.Annotations.Add(annot);
                    }
                }
            }

            crntVarList.Clear();
        }

        private void AddEvent(string name, Span nameSpan, Span span)
        {
            var evDecl = GetCurrentEventDecl(span);
            evDecl.Span = span;
            evDecl.name = MkString(name, nameSpan);
            parseProgram.Events.Add(evDecl);
            crntEventDecl = null;
        }

        private void AddMachine(P_Root.UserCnstKind kind, string name, Span nameSpan, Span span)
        {
            var machDecl = GetCurrentMachineDecl(span);
            machDecl.Span = span;
            machDecl.name = MkString(name, nameSpan);
            machDecl.kind = MkUserCnst(kind, span);
            parseProgram.Machines.Add(machDecl);
            crntMachDecl = null;
        }

        private void AddMachineAnnots(Span span)
        {
            Contract.Assert(crntAnnotStack.Count > 0);
            var machDecl = GetCurrentMachineDecl(span);
            var annots = crntAnnotStack.Pop();
            foreach (var kv in annots)
            {
                var annot = P_Root.MkAnnotation(
                    machDecl,
                    kv.Item1,
                    (P_Root.IArgType_Annotation__2)kv.Item2);
                annot.Span = span;
                parseProgram.Annotations.Add(annot);
            }            
        }

        private void AddStateAnnots(Span span)
        {
            Contract.Assert(crntAnnotStack.Count > 0);
            var stateDecl = GetCurrentStateDecl(span);
            var annots = crntAnnotStack.Pop();
            foreach (var kv in annots)
            {
                var annot = P_Root.MkAnnotation(
                    stateDecl,
                    kv.Item1,
                    (P_Root.IArgType_Annotation__2)kv.Item2);
                annot.Span = span;
                parseProgram.Annotations.Add(annot);
            }
        }

        private void AddEventAnnots(Span span)
        {
            Contract.Assert(crntAnnotStack.Count > 0);
            var eventDecl = GetCurrentEventDecl(span);
            var annots = crntAnnotStack.Pop();
            foreach (var kv in annots)
            {
                var annot = P_Root.MkAnnotation(
                    eventDecl,
                    kv.Item1,
                    (P_Root.IArgType_Annotation__2)kv.Item2);
                annot.Span = span;
                parseProgram.Annotations.Add(annot);
            }
        }

        private void AddFunAnnots(Span span)
        {
            Contract.Assert(crntAnnotStack.Count > 0);
            var funDecl = GetCurrentFunDecl(span);
            var annots = crntAnnotStack.Pop();
            foreach (var kv in annots)
            {
                var annot = P_Root.MkAnnotation(
                    funDecl,
                    kv.Item1,
                    (P_Root.IArgType_Annotation__2)kv.Item2);
                annot.Span = span;
                parseProgram.Annotations.Add(annot);
            }
        }

        private void AddFunction(string name, Span nameSpan, Span span)
        {
            Contract.Assert(stmtStack.Count > 0);
            var funDecl = GetCurrentFunDecl(span);
            funDecl.Span = span;
            funDecl.name = MkString(name, nameSpan);
            funDecl.owner = GetCurrentMachineDecl(span);
            funDecl.body = (P_Root.IArgType_FunDecl__5)stmtStack.Pop();
            parseProgram.Functions.Add(funDecl);
            crntFunDecl = null;
        }
        #endregion

        #region Node getters
        private P_Root.EventDecl GetCurrentEventDecl(Span span)
        {
            if (crntEventDecl != null)
            {
                return crntEventDecl;
            }
            
            crntEventDecl = P_Root.MkEventDecl();
            crntEventDecl.card = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            crntEventDecl.type = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            crntEventDecl.Span = span;
            return crntEventDecl;
        }

        private P_Root.FunDecl GetCurrentFunDecl(Span span)
        {
            if (crntFunDecl != null)
            {
                return crntFunDecl;
            }

            crntFunDecl = P_Root.MkFunDecl();
            crntFunDecl.kind = MkUserCnst(P_Root.UserCnstKind.REAL, span);
            crntFunDecl.@params = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            crntFunDecl.@return = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            crntFunDecl.Span = span;
            return crntFunDecl;
        }

        private P_Root.StateDecl GetCurrentStateDecl(Span span)
        {
            if (crntState != null)
            {
                return crntState;
            }

            crntState = P_Root.MkStateDecl();
            crntState.Span = span;
            crntState.owner = GetCurrentMachineDecl(span);

            var skipEntry = P_Root.MkNulStmt(MkUserCnst(P_Root.UserCnstKind.SKIP, span));
            skipEntry.Span = span;
            crntState.entryFun = skipEntry;

            var skipExit = P_Root.MkNulStmt(MkUserCnst(P_Root.UserCnstKind.SKIP, span));
            skipExit.Span = span;
            crntState.exitFun = skipExit;

            crntState.isStable = MkUserCnst(P_Root.UserCnstKind.FALSE, span);

            return crntState;
        }

        private P_Root.MachineDecl GetCurrentMachineDecl(Span span)
        {
            if (crntMachDecl != null)
            {
                return crntMachDecl;
            }

            crntMachDecl = P_Root.MkMachineDecl();
            crntMachDecl.name = MkString(string.Empty, span);
            crntMachDecl.kind = MkUserCnst(P_Root.UserCnstKind.REAL, span);
            crntMachDecl.card = MkUserCnst(P_Root.UserCnstKind.NIL, span);
            crntMachDecl.isMain = MkUserCnst(P_Root.UserCnstKind.FALSE, span);
            crntMachDecl.start = P_Root.MkQualifiedName(
                                        MkString(string.Empty, span),
                                        MkUserCnst(P_Root.UserCnstKind.NIL, span));
            crntMachDecl.start.Span = span;
            return crntMachDecl;
        }
        #endregion

        #region Helpers
        private P_Root.TypeExpr MkBaseType(P_Root.UserCnstKind kind, Span span)
        {
            Contract.Requires(
                kind == P_Root.UserCnstKind.NULL ||
                kind == P_Root.UserCnstKind.BOOL ||
                kind == P_Root.UserCnstKind.INT ||
                kind == P_Root.UserCnstKind.MACHINE ||
                kind == P_Root.UserCnstKind.MODEL ||
                kind == P_Root.UserCnstKind.EVENT ||
                kind == P_Root.UserCnstKind.FOREIGN ||
                kind == P_Root.UserCnstKind.ANY);

            var cnst = P_Root.MkUserCnst(kind);
            cnst.Span = span;
            var bt = P_Root.MkBaseType(cnst);
            bt.Span = span;
            return bt;
        }

        private P_Root.UserCnst MkUserCnst(P_Root.UserCnstKind kind, Span span)
        {
            var cnst = P_Root.MkUserCnst(kind);
            cnst.Span = span;
            return cnst;
        }

        private P_Root.StringCnst MkString(string s, Span span)
        {
            var str = P_Root.MkString(s);
            str.Span = span;
            return str;
        }

        private P_Root.RealCnst MkNumeric(int i, Span span)
        {
            var num = P_Root.MkNumeric(i);
            num.Span = span;
            return num;
        }

        private void ResetState()
        {
            stmtStack.Clear();
            valueExprStack.Clear();
            exprsStack.Clear();
            typeExprStack.Clear();
            crntVarList.Clear();
            groupStack.Clear();
            crntEventList.Clear();
            crntAnnotStack.Clear();
            crntAnnotList = new List<Tuple<P_Root.StringCnst, P_Root.AnnotValue>>();
            parseFailed = false;
            isTrigAnnotated = false;
            crntState = null;
            crntEventDecl = null;
            crntMachDecl = null;
            crntQualName = null;
        }
        #endregion
    }
}
