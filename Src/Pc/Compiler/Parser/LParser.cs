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
        private PLink_Root.ModuleDecl crntModuleDecl = null;
        private LProgramTopDeclNames LinkTopDeclNames;
        private List<PLink_Root.NonNullEventName> crntEventList = new List<PLink_Root.NonNullEventName>();
        private List<PLink_Root.String> crntStringList = new List<PLink_Root.String>();
        private Stack<PLink_Root.ModuleExpr> moduleExprStack = new Stack<PLink_Root.ModuleExpr>();
        private Stack<PLink_Root.MonitorNameList> monitorNameListStack = new Stack<PLink_Root.MonitorNameList>();

        public LParser()
            : base(new Scanner())
        {

        }

        Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo;

        PLink_Root.Id MkUniqueId(Span entrySpan, Span exitSpan)
        {
            var filePath = entrySpan.Program.Uri.LocalPath;
            int nextId = 0;
            if (idToSourceInfo.ContainsKey(filePath))
            {
                nextId = idToSourceInfo[filePath].Count;
                idToSourceInfo[filePath][nextId] = new SourceInfo(entrySpan, exitSpan);
            }
            else
            {
                idToSourceInfo[filePath] = new Dictionary<int, SourceInfo>();
                idToSourceInfo[filePath][nextId] = new SourceInfo(entrySpan, exitSpan);
            }

            var fileInfo = PLink_Root.MkIdList(MkString(filePath, entrySpan), (PLink_Root.IArgType_IdList__1)MkId(entrySpan));
            var uniqueId = PLink_Root.MkIdList(MkNumeric(nextId, new Span()), fileInfo);
            return uniqueId;
        }

        PLink_Root.Id MkUniqueId(Span span)
        {
            var filePath = span.Program.Uri.LocalPath;
            int nextId = 0;
            if (idToSourceInfo.ContainsKey(filePath))
            {
                nextId = idToSourceInfo[filePath].Count;
                idToSourceInfo[filePath][nextId] = new SourceInfo(span, new Span());
            }
            else
            {
                idToSourceInfo[filePath] = new Dictionary<int, SourceInfo>();
                idToSourceInfo[filePath][nextId] = new SourceInfo(span, new Span());
            }
            var fileInfo = PLink_Root.MkIdList(MkString(span.Program.Uri.LocalPath, span), (PLink_Root.IArgType_IdList__1)MkId(span));
            var uniqueId = PLink_Root.MkIdList(MkNumeric(nextId, new Span()), fileInfo);
            return uniqueId;
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
            moduleName.id = (PLink_Root.IArgType_ModuleName__1)MkUniqueId(nameSpan);
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
            composeExpr.id = (PLink_Root.IArgType_ComposeExpr__2)MkUniqueId(span);
            moduleExprStack.Push(composeExpr);
        }

        private void PushSafeExpr(Span span)
        {
            var safeExpr = new PLink_Root.SafeExpr();
            safeExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            safeExpr.mod = (PLink_Root.IArgType_SafeExpr__0)moduleExprStack.Pop(); ;
            safeExpr.id = (PLink_Root.IArgType_SafeExpr__1)MkUniqueId(span);
            moduleExprStack.Push(safeExpr);
        }

        private PLink_Root.InterfaceType ConvertToInterfaceType(List<PLink_Root.NonNullEventName> events)
        {
            var interfaceTypeList = new Stack<PLink_Root.InterfaceType>();
            var interfaceType = PLink_Root.MkInterfaceType();
            interfaceType.ev = (PLink_Root.IArgType_InterfaceType__0)events[0];
            interfaceType.tail = MkUserCnst(PLink_Root.UserCnstKind.NIL, events[0].Span);
            interfaceTypeList.Push(interfaceType);
            crntEventList.RemoveAt(0);
            foreach (var str in events)
            {
                interfaceType = PLink_Root.MkInterfaceType();
                interfaceType.ev = (PLink_Root.IArgType_InterfaceType__0)str;
                interfaceType.tail = (PLink_Root.IArgType_InterfaceType__1)interfaceTypeList.Pop();
                interfaceTypeList.Push(interfaceType);
            }
            return interfaceTypeList.Pop();
        }

        private void PushHideExpr(Span span)
        {
            var hideExpr = new PLink_Root.HideExpr();
            hideExpr.Span = span;
            Contract.Assert(moduleExprStack.Count >= 1);
            hideExpr.mod = (PLink_Root.IArgType_HideExpr__1)moduleExprStack.Pop(); ;
            Contract.Assert(crntEventList.Count >= 1);
            //convert the string list to EventNameList
            hideExpr.evtNames = ConvertToInterfaceType(crntEventList);
            hideExpr.id = (PLink_Root.IArgType_HideExpr__2)MkUniqueId(span);
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
            assumeExpr.id = (PLink_Root.IArgType_AssumeExpr__2)MkUniqueId(span);
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
            assertExpr.id = (PLink_Root.IArgType_AssertExpr__2)MkUniqueId(span);
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
            renameExpr.id = (PLink_Root.IArgType_RenameExpr__3)MkUniqueId(span);
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
            exportExpr.id = (PLink_Root.IArgType_ExportExpr__3)MkUniqueId(span);
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

        private void AddToMachineNamesList(string name, Span span)
        {
            if (crntStringList.Where(e => ((string)e.Symbol == name)).Count() >= 1)
            {
                var errFlag = new Flag(
                                     SeverityKind.Error,
                                     span,
                                     Constants.BadSyntax.ToString(string.Format("Machine {0} listed multiple times in the list", name)),
                                     Constants.BadSyntax.Code,
                                     parseSource);
                parseFailed = true;
                parseFlags.Add(errFlag);
            }
            else
            {
                crntStringList.Add(MkString(name, span));
            }
        }

        private void AddToEventList(PLink_Root.UserCnstKind kind, Span span)
        {
            crntEventList.Add(MkUserCnst(kind, span));
        }

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
            impsDecl.id = (PLink_Root.IArgType_ImplementationDecl__1)MkUniqueId(span);
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
            refinesDecl.rhs = (PLink_Root.IArgType_RefinementDecl__2)moduleExprStack.Pop();
            refinesDecl.lhs = (PLink_Root.IArgType_RefinementDecl__1)moduleExprStack.Pop();
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

        private void AddPrivatesList(Span span = default(Span))
        {
            Contract.Assert(crntEventList.Count > 0);
            foreach (var ev in crntEventList)
            {
                var rec = PLink_Root.MkModulePrivateEvents(GetCurrentModuleDecl(span), (PLink_Root.IArgType_ModulePrivateEvents__1)ev);
                rec.Span = ev.Span;
                parseLinker.ModulePrivateEvents.Add(rec);
            }
            crntEventList.Clear();
        }

        private void AddModuleDecl(string name, Span nameSpan, Span span)
        {
            var moduleDecl = GetCurrentModuleDecl(span);
            moduleDecl.Span = span;
            moduleDecl.name = MkString(name, nameSpan);
            moduleDecl.id = (PLink_Root.IArgType_ModuleDecl__1)MkUniqueId(span);
            //add the module decl
            if (IsValidName(LProgramTopDecl.Module, name, nameSpan))
            {
                LinkTopDeclNames.moduleNames.Add(name);
            }
            parseLinker.ModuleDecl.Add(moduleDecl);

            foreach(var machine in crntStringList)
            {
                var moduleContains = PLink_Root.MkModuleContainsMachine();
                moduleContains.mod = (PLink_Root.IArgType_ModuleContainsMachine__0)moduleDecl;
                moduleContains.mach = (PLink_Root.IArgType_ModuleContainsMachine__1)machine;
                parseLinker.ModuleContainsMachine.Add(moduleContains);
            }
            crntStringList.Clear();
            crntModuleDecl = null;
        }

        private PLink_Root.ModuleDecl GetCurrentModuleDecl(Span span)
        {
            if (crntModuleDecl != null)
            {
                return crntModuleDecl;
            }

            crntModuleDecl = PLink_Root.MkModuleDecl();
            crntModuleDecl.name = MkString(string.Empty, span);
            crntModuleDecl.Span = span;
            return crntModuleDecl;
        }

        private void ResetState()
        {
            crntEventList.Clear();
            crntStringList.Clear();
            moduleExprStack.Clear();
            monitorNameListStack.Clear();
            crntModuleDecl = null;
        }

        internal bool ParseFile(
            ProgramName file,
            LProgramTopDeclNames topDeclNames,
            LProgram program,
            Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo,
            out List<Flag> flags)
        {
            flags = parseFlags = new List<Flag>();
            this.LinkTopDeclNames = topDeclNames;
            parseLinker = program;
            this.idToSourceInfo = idToSourceInfo;
            parseSource = file;
            bool result;
            try
            {
                var fi = new System.IO.FileInfo(file.Uri.LocalPath);
                if (!fi.Exists)
                {
                    var badFile = new Flag(
                        SeverityKind.Error,
                        default(Span),
                        Constants.BadFile.ToString(string.Format("The file {0} does not exist", fi.FullName)),
                        Constants.BadFile.Code,
                        file);
                    result = false;
                    flags.Add(badFile);
                    return false;
                }

                var str = new System.IO.FileStream(file.Uri.LocalPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
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
            return new Span(loc.StartLine, loc.StartColumn + 1, loc.EndLine, loc.EndColumn + 1, this.parseSource);
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


