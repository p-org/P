namespace Microsoft.Pc.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using QUT.Gppg;

    using Domains;
    using Microsoft.Formula.API;
    using Microsoft.Pc;
    using Microsoft.Formula.API.Generators;
    using Microsoft.Formula.API.Nodes;

    public enum LProgramTopDecl { Module, Test };
    public class LProgramTopDeclNames
    {
        public HashSet<string> testNames;
        public HashSet<string> moduleNames;

        public LProgramTopDeclNames()
        {
            testNames = new HashSet<string>();
            moduleNames = new HashSet<string>();
        }

        public void Reset()
        {
            testNames.Clear();
            moduleNames.Clear();
        }
    }

    internal partial class LParser : ShiftReduceParser<LexValue, LexLocation>
    {
        private List<Flag> parseFlags;
        private LProgram parseLinker;
        private ProgramName parseSource;

        private bool parseFailed = false;

        private LProgramTopDeclNames LinkTopDeclNames;
        private List<PLink_Root.EventName> crntEventList = new List<PLink_Root.EventName>();
        private List<PLink_Root.String> crntStringList = new List<PLink_Root.String>();
        private Stack<PLink_Root.ModuleExpr> moduleExprStack = new Stack<PLink_Root.ModuleExpr>();
        private Stack<PLink_Root.MonitorNameList> monitorNameListStack = new Stack<PLink_Root.MonitorNameList>();

        public LParser()
            : base(new Scanner())
        {

        }

        Dictionary<int, SourceInfo> idToSourceInfo;

        PLink_Root.Id MkIntegerId(Span entrySpan, Span exitSpan)
        {
            var nextId = idToSourceInfo.Count;
            idToSourceInfo[nextId] = new SourceInfo(entrySpan, exitSpan);
            return MkNumeric(nextId, new Span());
        }

        PLink_Root.Id MkIntegerId(Span span)
        {
            var nextId = idToSourceInfo.Count;
            idToSourceInfo[nextId] = new SourceInfo(span, new Span());
            return MkNumeric(nextId, new Span());
        }

        PLink_Root.Id MkId(Span span)
        {
            return MkUserCnst(PLink_Root.UserCnstKind.NIL, span);
        }

        PLink_Root.Id MkId(Span entrySpan, Span exitSpan)
        {
            return MkUserCnst(PLink_Root.UserCnstKind.NIL, entrySpan);
        }

        private PLink_Root.UserCnst MkUserCnst(PLink_Root.UserCnstKind kind, Span span)
        {
            var cnst = PLink_Root.MkUserCnst(kind);
            cnst.Span = span;
            return cnst;
        }

        private PLink_Root.StringCnst MkString(string s, Span span)
        {
            var str = PLink_Root.MkString(s);
            str.Span = span;
            return str;
        }

        private PLink_Root.RealCnst MkNumeric(int i, Span span)
        {
            var num = PLink_Root.MkNumeric(i);
            num.Span = span;
            return num;
        }

        #region Push Helpers
        //Module helpers
        private void PushModuleName(string name, Span nameSpan)
        {
            var moduleName = new PLink_Root.ModuleName();
            moduleName.name = (PLink_Root.IArgType_ModuleName__0)MkString(name, nameSpan);
            moduleName.Span = nameSpan;
            moduleName.id = (PLink_Root.IArgType_ModuleName__1)MkIntegerId(nameSpan);
            moduleExprStack.Push(moduleName);
        }

        private void PushComposeExpr(Span span)
        {
            var composeExpr = new PLink_Root.ComposeExpr();
            composeExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 2);
            var mod1 = moduleExprStack.Pop();
            var mod2 = moduleExprStack.Pop();
            composeExpr.left = (PLink_Root.IArgType_ComposeExpr__0)mod1;
            composeExpr.right = (PLink_Root.IArgType_ComposeExpr__1)mod2;
            composeExpr.id = (PLink_Root.IArgType_ComposeExpr__2)MkIntegerId(span);
            moduleExprStack.Push(composeExpr);
        }

        private void PushSafeExpr(Span span)
        {
            var safeExpr = new PLink_Root.SafeExpr();
            safeExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            safeExpr.mod = (PLink_Root.IArgType_SafeExpr__0)moduleExprStack.Pop(); ;
            safeExpr.id = (PLink_Root.IArgType_SafeExpr__1)MkIntegerId(span);
            moduleExprStack.Push(safeExpr);
        }

        private PLink_Root.EventNameList ConvertToEventNameList(List<PLink_Root.EventName> events)
        {
            var eventNameList = new Stack<PLink_Root.EventNameList>();
            var eventList = PLink_Root.MkEventNameList();
            eventList.hd = (PLink_Root.IArgType_EventNameList__0)events[0];
            eventList.tl = MkUserCnst(PLink_Root.UserCnstKind.NIL, events[0].Span);
            eventNameList.Push(eventList);
            crntEventList.RemoveAt(0);
            foreach (var str in events)
            {
                eventList = PLink_Root.MkEventNameList();
                eventList.hd = (PLink_Root.IArgType_EventNameList__0)str;
                eventList.tl = (PLink_Root.IArgType_EventNameList__1)eventNameList.Pop();
                eventNameList.Push(eventList);
            }
            return eventNameList.Pop();
        }

        private void PushHideExpr(Span span)
        {
            var hideExpr = new PLink_Root.HideExpr();
            hideExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            hideExpr.mod = (PLink_Root.IArgType_HideExpr__1)moduleExprStack.Pop(); ;
            Contract.Assert(crntEventList.Count >= 1);
            //convert the string list to EventNameList
            hideExpr.evtNames = ConvertToEventNameList(crntEventList);
            hideExpr.id = (PLink_Root.IArgType_HideExpr__2)MkIntegerId(span);
            moduleExprStack.Push(hideExpr);
            //clear eventList
            crntEventList.Clear();
        }

        private void PushAssumeExpr(Span span)
        {
            var assumeExpr = new PLink_Root.AssumeExpr();
            assumeExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            assumeExpr.mod = (PLink_Root.IArgType_AssumeExpr__1)moduleExprStack.Pop(); ;
            Contract.Assert(monitorNameListStack.Count >= 1);
            assumeExpr.monNames = monitorNameListStack.Pop();
            assumeExpr.id = (PLink_Root.IArgType_AssumeExpr__2)MkIntegerId(span);
            moduleExprStack.Push(assumeExpr);
        }

        private void PushAssertExpr(Span span)
        {
            var assertExpr = new PLink_Root.AssertExpr();
            assertExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            assertExpr.mod = (PLink_Root.IArgType_AssertExpr__1)moduleExprStack.Pop(); ;
            Contract.Assert(monitorNameListStack.Count >= 1);
            assertExpr.monNames = monitorNameListStack.Pop();
            assertExpr.id = (PLink_Root.IArgType_AssertExpr__2)MkIntegerId(span);
            moduleExprStack.Push(assertExpr);
        }

        private void PushRenameExpr(string oldName, Span oldNameSpan, string newName, Span newNameSpan, Span span)
        {
            var renameExpr = new PLink_Root.RenameExpr();
            renameExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            renameExpr.mod = (PLink_Root.IArgType_RenameExpr__2)moduleExprStack.Pop(); ;
            renameExpr.mNames_PRIME1 = MkString(newName, newNameSpan);
            renameExpr.mNames = MkString(oldName, oldNameSpan);
            renameExpr.id = (PLink_Root.IArgType_RenameExpr__3)MkIntegerId(span);
            moduleExprStack.Push(renameExpr);
        }

        private void PushExportExpr(string mName, string iName, Span mSpan, Span iSpan, Span span)
        {
            var exportExpr = new PLink_Root.ExportExpr();
            exportExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            exportExpr.mod = (PLink_Root.IArgType_ExportExpr__2)moduleExprStack.Pop(); ;
            exportExpr.mName = MkString(mName, mSpan);
            exportExpr.iName = MkString(iName, iSpan);
            exportExpr.id = (PLink_Root.IArgType_ExportExpr__3)MkIntegerId(span);
            moduleExprStack.Push(exportExpr);
        }

        private void PushMonitorName(string name, Span nameSpan, bool isLast)
        {
            var monNameList = PLink_Root.MkMonitorNameList();
            monNameList.Span = nameSpan;
            if (crntStringList.Where(e => (string)e.Symbol == name).Count() >= 1)
            {
                var errFlag = new Flag(
                                     SeverityKind.Error,
                                     nameSpan,
                                     Constants.BadSyntax.ToString(string.Format(" item {0} listed multiple times in the list", name)),
                                     Constants.BadSyntax.Code,
                                     parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
            }
            if (isLast)
            {
                monNameList.str = (PLink_Root.IArgType_MonitorNameList__0)MkString(name, nameSpan);
                monNameList.tl = MkUserCnst(PLink_Root.UserCnstKind.NIL, nameSpan);
                crntStringList.Clear();
            }
            else
            {
                Contract.Assert(monitorNameListStack.Count > 0);
                monNameList.str = (PLink_Root.IArgType_MonitorNameList__0)MkString(name, nameSpan);
                monNameList.tl = (PLink_Root.IArgType_MonitorNameList__1)monitorNameListStack.Pop();
            }
            monitorNameListStack.Push(monNameList);
        }

        #endregion

        private void AddToEventList(string name, Span span)
        {
            if (crntEventList.Where(e => ((string)e.Symbol == name)).Count() >= 1)
            {
                var errFlag = new Flag(
                                     SeverityKind.Error,
                                     span,
                                     Constants.BadSyntax.ToString(string.Format("Event {0} listed multiple times in the event list", name)),
                                     Constants.BadSyntax.Code,
                                     parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
            }
            else
            {
                crntEventList.Add(MkString(name, span));
            }
        }

        private void AddImplementationDecl(Span span)
        {
            Contract.Assert(moduleExprStack.Count == 1);
            var impsDecl = PLink_Root.MkImplementationDecl();
            impsDecl.Span = span;
            impsDecl.mod = (PLink_Root.IArgType_ImplementationDecl__0)moduleExprStack.Pop();
            impsDecl.id = (PLink_Root.IArgType_ImplementationDecl__1)MkIntegerId(span);
            parseLinker.ImplementationDecl.Add(impsDecl);
        }

        private void AddRefinementDeclaration(string name, Span nameSpan, Span span)
        {
            if (IsValidName(LProgramTopDecl.Test, name, nameSpan))
            {
                LinkTopDeclNames.testNames.Add(name);
            }
            Contract.Assert(moduleExprStack.Count == 2);
            var refinesDecl = PLink_Root.MkRefinementDecl();
            refinesDecl.name = (PLink_Root.IArgType_RefinementDecl__0)MkString(name, nameSpan);
            refinesDecl.Span = span;
            refinesDecl.lhs = (PLink_Root.IArgType_RefinementDecl__1)moduleExprStack.Pop();
            refinesDecl.rhs = (PLink_Root.IArgType_RefinementDecl__2)moduleExprStack.Pop();
            parseLinker.RefinementDecl.Add(refinesDecl);
        }

        private void AddTestDeclaration(string name, Span nameSpan, Span span)
        {
            if (IsValidName(LProgramTopDecl.Test, name, nameSpan))
            {
                LinkTopDeclNames.testNames.Add(name);
            }
            Contract.Assert(moduleExprStack.Count == 1);
            var testDecl = PLink_Root.MkTestDecl();
            testDecl.name = (PLink_Root.IArgType_TestDecl__0)MkString(name, nameSpan);
            testDecl.Span = span;
            testDecl.mod = (PLink_Root.IArgType_TestDecl__1)moduleExprStack.Pop();
            parseLinker.TestDecl.Add(testDecl);
        }

        private void AddModuleDef(string name, Span nameSpan, Span span)
        {
            var moduleDef = PLink_Root.MkModuleDef();
            moduleDef.Span = span;
            moduleDef.name = MkString(name, nameSpan);
            Contract.Assert(moduleExprStack.Count >= 1);
            moduleDef.mod = (PLink_Root.IArgType_ModuleDef__1)moduleExprStack.Pop();
            if (IsValidName(LProgramTopDecl.Module, name, nameSpan))
            {
                LinkTopDeclNames.moduleNames.Add(name);
            }
            parseLinker.ModuleDef.Add(moduleDef);
        }

        private void AddModuleDecl(string name, Span nameSpan, Span span)
        {
            var moduleDecl = GetCurrentModuleDecl(span);
            moduleDecl.Span = span;
            moduleDecl.name = MkString(name, nameSpan);

            //add the module decl
            if (IsValidName(LProgramTopDecl.Module, name, nameSpan))
            {
                LinkTopDeclNames.moduleNames.Add(name);
            }
            parseLinker.ModuleDecl.Add(moduleDecl);

            foreach (var e in crntPrivateList)
            {
                //add privates
                var pri = PLink_Root.MkModulePrivateEvent(moduleDecl, (PLink_Root.IArgType_ModulePrivateEvent__1)e);
                pri.Span = e.Span;
                parseProgram.ModulePrivateEventsDecl.Add(pri);
            }

            if (isPrivateListAllEvents)
            {
                var pri = PLink_Root.MkModulePrivateEventAll(moduleDecl);
                parseProgram.ModuleAllEventsPrivate.Add(pri);
            }
            //clear the machine names and static function names
            topDeclNames.machineNames.Clear();
            crntStaticFunNames.Clear();
            crntPrivateList.Clear();
            isPrivateListAllEvents = false;
            crntModuleDecl = null;
        }


        private void ResetState()
        {
            crntEventList.Clear();
            crntStringList.Clear();
            moduleExprStack.Clear();
            monitorNameListStack.Clear();
        }

        public bool IsValidName(LProgramTopDecl type, string name, Span nameSpan)
        {
            string errorMessage = "";
            bool error = false;
            switch (type)
            {
                case LProgramTopDecl.Module:
                    if (LinkTopDeclNames.moduleNames.Contains(name))
                    {
                        errorMessage = string.Format("A module with name {0} already declared", name);
                        error = true;
                    }
                    break;
                case LProgramTopDecl.Test:
                    if (LinkTopDeclNames.testNames.Contains(name))
                    {
                        errorMessage = string.Format("A test with name {0} already declared", name);
                        error = true;
                    }
                    break;
                
            }

            if (error)
            {
                var errFlag = new Flag(
                                         SeverityKind.Error,
                                         nameSpan,
                                         Constants.BadSyntax.ToString(errorMessage),
                                         Constants.BadSyntax.Code,
                                         parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
            }

            return !error;

        }
    }


    // Dummy function for Tokens.y
    internal partial class DummyTokenParser : ShiftReduceParser<LexValue, LexLocation>
    {
        public DummyTokenParser()
            : base(null)
        {

        }
    }
}


