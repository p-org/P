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
        private ProgramName parseSource;
        private List<Flag> parseFlags;
        private PProgram parseProgram;

        private bool parseFailed = false;
        private P_Root.EventDecl crntEventDecl = null;
        private Stack<P_Root.TypeExpr> typeExprStack = new Stack<P_Root.TypeExpr>();

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
                tupType.tl = MkNil(span);
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
            var tupFldName = P_Root.MkString(fieldName);

            tupType.Span = span;
            tupFld.Span = span;
            tupFldName.Span = span;
            tupFld.name = tupFldName;
            tupType.hd = tupFld;
            if (isLast)
            {
                Contract.Assert(typeExprStack.Count > 0);
                tupFld.type = (P_Root.IArgType_NmdTupTypeField__1)typeExprStack.Pop();
                tupType.tl = MkNil(span);
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

        private void AddEvent(string name, Span nameSpan, Span span)
        {
            if (crntEventDecl == null)
            {
                crntEventDecl = P_Root.MkEventDecl();
                crntEventDecl.card = MkNil(span);
                crntEventDecl.payloadType = MkNil(span);
            }

            crntEventDecl.Span = span;
            var nameNode = P_Root.MkString(name);
            nameNode.Span = nameSpan;
            crntEventDecl.name = nameNode;

            parseProgram.Events.Add(crntEventDecl);
            crntEventDecl = null;
        }

        private void SetEventCard(string cardStr, bool isAssert, Span span)
        {
            if (crntEventDecl == null)
            {
                crntEventDecl = P_Root.MkEventDecl();
                crntEventDecl.payloadType = MkNil(span);
                crntEventDecl.Span = span;
            }

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

            var cardNode = P_Root.MkNumeric(card);
            cardNode.Span = span;
            if (isAssert)
            {
                var assertNode = P_Root.MkAssertMaxInstances(cardNode);
                assertNode.Span = span;
                crntEventDecl.card = assertNode;
            }
            else
            {
                var assumeNode = P_Root.MkAssumeMaxInstances(cardNode);
                assumeNode.Span = span;
                crntEventDecl.card = assumeNode;
            }
        }

        private void SetEventType(Span span)
        {
            if (crntEventDecl == null)
            {
                crntEventDecl = P_Root.MkEventDecl();
                crntEventDecl.card = MkNil(span);
                crntEventDecl.Span = span;
            }

            Contract.Assert(typeExprStack.Count > 0);
            crntEventDecl.payloadType = (P_Root.IArgType_EventDecl__2)typeExprStack.Pop();
        }

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

        private P_Root.UserCnst MkNil(Span span)
        {
            var nil = P_Root.MkUserCnst(P_Root.UserCnstKind.NIL);
            nil.Span = span;
            return nil;
        }

        private void ResetState()
        {
            typeExprStack.Clear();
            parseFailed = false;
            crntEventDecl = null;
        }
    }
}
