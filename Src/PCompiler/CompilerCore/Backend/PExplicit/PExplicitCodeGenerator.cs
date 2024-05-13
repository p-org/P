using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PExplicit
{
    class PExplicitCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            var javaSource = GenerateSource(context, globalScope);
            return new List<CompiledFile> { javaSource };
        }

        /// <summary>
        /// This compiler has a compilation stage.
        /// </summary>
        public bool HasCompilationStage => true;

        public void Compile(ICompilerConfiguration job)
        {
            var pomPath = Path.Combine(job.OutputDirectory.FullName, "pom.xml");
            var stdout = "";
            var stderr = "";
            
            // create the pom.xml file
            var pomTemplate = Constants.pomTemplate;
            pomTemplate = pomTemplate.Replace("-project-name-",job.ProjectName);
            
            string foreignInclude = "";
            var foreignFiles = job.InputForeignFiles.Where(x => x.EndsWith(".java"));
            if (foreignFiles.Any())
            {
                foreignInclude = Constants.pomForeignTemplate;
                string foreignSourceInclude = "";
                SortedSet<string> foreignFolders = new SortedSet<string>();

                foreach (var fileName in foreignFiles)
                {
                    var folderName = Path.GetDirectoryName(fileName);
                    if (folderName is not null)
                    {
                        foreignFolders.Add(folderName);
                    }
                }
                foreach (var folderName in foreignFolders)
                {
                    foreignSourceInclude += $"                                <source>{folderName}</source>\n";
                }
                foreignInclude = foreignInclude.Replace("-foreign-source-include-", foreignSourceInclude);
            }
            pomTemplate = pomTemplate.Replace("-foreign-include-", foreignInclude);
            
            File.WriteAllText(pomPath, pomTemplate);

            // compile the csproj file
            var args = new[] { "versions:use-latest-versions -DgenerateBackupPoms=false clean package -q"};

            var exitCode = Compiler.RunWithOutput(job.OutputDirectory.FullName, out stdout, out stderr, "mvn", args);
            if (exitCode != 0)
            {
                throw new TranslationException($"Compiling generated PExplicit Java code FAILED!\n" + $"{stdout}\n" + $"{stderr}\n");
            }
            else
            {
//                job.Output.WriteInfo($"{stdout}");
                job.Output.WriteInfo($"  {job.ProjectName} -> {job.OutputDirectory}/target/{job.ProjectName}-jar-with-dependencies.jar");
                job.Output.WriteInfo("Build succeeded.");
            }

            var sourceDirectory = "target/sources/pexplicit/model";

            // create source folder
            args = new[] { $"-p {sourceDirectory}" };
            exitCode = Compiler.RunWithOutput(job.OutputDirectory.FullName, out stdout, out stderr, "mkdir", args);
            if (exitCode != 0)
            {
                throw new TranslationException($"Unable to create source directory {sourceDirectory}\n" + $"{stdout}\n" + $"{stderr}\n");
            }


            // copy source files
            args = new[] { $"{job.ProjectName}PModel.java {sourceDirectory}" };
            exitCode = Compiler.RunWithOutput(job.OutputDirectory.FullName, out stdout, out stderr, "cp", args);
            if (exitCode != 0)
            {
                throw new TranslationException($"Unable to copy source file {job.ProjectName}PModel.java to source directory {sourceDirectory}\n" + $"{stdout}\n" + $"{stderr}\n");
            }
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            context.WriteLine(source.Stream);

            context.WriteLine(source.Stream);

            //WriteMachineTagDefs(context, source.Stream, globalScope.Machines);

            //context.WriteLine(source.Stream);

            IEnumerable<IPDecl> decls = TransformASTPass.GetTransformedDecls(globalScope);
            //IEnumerable<IPDecl> decls = globalScope.AllDecls;

            bool hasSafetyTest = false;
            foreach (var decl in decls) {
                hasSafetyTest |= (decl.GetType() == typeof(SafetyTest));
                WriteDecl(context, source.Stream, decl);
            }

            if (!hasSafetyTest) {
                WriteMainDriver(context, source.Stream, globalScope, decls);
            }

            context.WriteLine(source.Stream, "PTestDriver testDriver = null;");
            context.WriteLine(source.Stream, "@Generated");
            context.WriteLine(source.Stream, "public PTestDriver getTestDriver() { return testDriver; }");
            context.WriteLine(source.Stream, "@Generated");
            context.WriteLine(source.Stream, "public void setTestDriver(PTestDriver input) { testDriver = input; }");
            context.WriteLine(source.Stream);

            context.WriteLine(source.Stream, "@Generated");
            context.WriteLine(source.Stream, "public PMachine getStart() { return testDriver.getStart(); }");
            context.WriteLine(source.Stream, "@Generated");
            context.WriteLine(source.Stream, "public List<PMonitor> getMonitors() { return testDriver.getMonitors(); }");
            context.WriteLine(source.Stream, "@Generated");
            context.WriteLine(source.Stream, "public Map<PEvent, List<PMonitor>> getListeners() { return testDriver.getListeners(); }");
            context.WriteLine(source.Stream);

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteDriver(CompilationContext context, StringWriter output, String startMachine, IEnumerable<IPDecl> decls)
        {
            WriteDriverConfigure(context, output, startMachine, decls);
            context.WriteLine(output);
        }

        private void WriteDriverConfigure(CompilationContext context, StringWriter output, String startMachine, IEnumerable<IPDecl> decls)
        {
            context.WriteLine(output);
            context.WriteLine(output, "@Generated");
            context.WriteLine(output, "public void configure() {");

            context.WriteLine(output, $"    mainMachine = new {startMachine}(0);");
            context.WriteLine(output, $"    monitorList.clear();");
            context.WriteLine(output, $"    observerMap.clear();");

            foreach (var decl in decls)
            {
                switch(decl)
                {
                    case Machine machine:
                        if (machine.IsSpec)
                        {
                            context.WriteLine(output);
                            var declName = context.GetNameForDecl(machine);
                            context.WriteLine(output, $"    PMonitor instance_{declName} = new {declName}(0);");
                            context.WriteLine(output, $"    monitorList.add(instance_{declName});");
                            foreach (var pEvent in machine.Observes.Events)
                            {
                                context.WriteLine(output, $"    if(!observerMap.containsKey({pEvent.Name}))");
                                context.WriteLine(output, $"        observerMap.put({pEvent.Name}, new ArrayList<>());");
                                context.WriteLine(output, $"    observerMap.get({pEvent.Name}).add(instance_{declName});");
                            }
                        }
                        break;
                }
            }

            context.WriteLine(output, "}");
        }

        private void WriteMainDriver(CompilationContext context, StringWriter output, Scope globalScope, IEnumerable<IPDecl> decls)
        {
            // TODO: Determine how main machine should be selected.  Should the 'main' method even
            // be generated from the P program, or should it be provided externally?
            Machine mainMachine = null;
            foreach (var machine in globalScope.Machines)
            {
                if (machine.Name == "Main")
                {
                    if (mainMachine != null)
                        throw new NotImplementedException("Cannot have multiple main machines.");

                    mainMachine = machine;
                }
            }

            if (mainMachine == null)
                return;

            foreach (var decl in decls)
            {
                if ((decl is SafetyTest) && (context.GetNameForDecl(decl) == "DefaultImpl"))
                    return;
            }
            
            context.WriteLine(output, "@Generated");
            context.WriteLine(output, "public static class DefaultImpl extends PTestDriver {");
            WriteDriver(context, output, mainMachine.Name, decls);
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteEvent(CompilationContext context, StringWriter output, PEvent ev)
        {
            context.WriteLine(output, $"public static PEvent {context.GetNameForDecl(ev)} = new PEvent(\"{context.GetNameForDecl(ev)}\");");
        }

        private void WriteDecl(CompilationContext context, StringWriter output, IPDecl decl)
        {
            switch (decl)
            {
                case Function function:
                    if (function.IsForeign)
                        WriteForeignFunction(context, output, function);
                    else
                        WriteFunction(context, output, function);
                    break;
                case Machine machine:
                    if (machine.IsSpec)
                        WriteMonitor(context, output, machine);
                    else
                        WriteMachine(context, output, machine);
                    break;
                case PEvent ev:
                    WriteEvent(context, output, ev);
                    break;
                case SafetyTest safety:
                    WriteSafetyTestDecl(context, output, safety);
                    break;
                default:
                    context.WriteLine(output, $"// Skipping {decl.GetType().Name} '{decl.Name}'\n");
                    break;
            }
        }

        private void WriteSafetyTestDecl(CompilationContext context, StringWriter output, SafetyTest safety)
        {
            context.WriteLine(output, "@Generated");
            context.WriteLine(output, $"public static class {context.GetNameForDecl(safety)} extends PTestDriver {{");
            WriteDriver(context, output, safety.Main, safety.ModExpr.ModuleInfo.MonitorMap.Keys);
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteMonitor(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.GetNameForDecl(machine);
            context.WriteLine(output, $"public static class {declName} extends PMonitor {{");

            WriteMachineFields(context, output, machine);
            WriteMachineConstructor(context, output, machine);

            context.WriteLine(output);
            foreach (var method in machine.Methods)
                WriteFunction(context, output, method);

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteMachine(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.GetNameForDecl(machine);
            context.WriteLine(output, $"public static class {declName} extends PMachine {{");

            WriteMachineFields(context, output, machine);
            WriteMachineConstructor(context, output, machine);

            context.WriteLine(output);
            foreach (var method in machine.Methods)
                WriteFunction(context, output, method);

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteMachineFields(CompilationContext context, StringWriter output, Machine machine)
        {
            context.WriteLine(output);

            for (var i = 0; i < machine.States.Count(); i++)
            {
                var state = machine.States.ElementAt(i);
                context.Write(output, $"static State {context.GetNameForDecl(state)} = ");
                WriteState(context, output, state, machine);
                context.WriteLine(output, ";");
            }

            foreach (var field in machine.Fields)
                context.WriteLine(output, $"private {GetPExplicitType(field.Type)} {CompilationContext.GetVar(field.Name)} = {GetDefaultValue(field.Type)};");

            context.WriteLine(output);

            context.WriteLine(output, "@Generated");
            context.WriteLine(output, "@Override");
            context.WriteLine(output, "public void reset() {");
            context.WriteLine(output, "    super.reset();");
            foreach (var field in machine.Fields)
                context.WriteLine(output, $"    {CompilationContext.GetVar(field.Name)} = {GetDefaultValue(field.Type)};");
            context.WriteLine(output, "}");
            context.WriteLine(output);

            context.WriteLine(output, "@Generated");
            context.WriteLine(output, "@Override");
            context.WriteLine(output, "public List<String> getLocalVarNames() {");
            context.WriteLine(output, "    List<String> result = super.getLocalVarNames();");
            foreach (var field in machine.Fields)
            {
                context.WriteLine(output, $"    result.add(\"{CompilationContext.GetVar(field.Name)}\");");
            }
            context.WriteLine(output, "    return result;");
            context.WriteLine(output, "}");
            context.WriteLine(output);

            context.WriteLine(output, "@Generated");
            context.WriteLine(output, "@Override");
            context.WriteLine(output, "public List<Object> getLocalVarValues() {");
            context.WriteLine(output, "    List<Object> result = super.getLocalVarValues();");
            foreach (var field in machine.Fields)
            {
                context.WriteLine(output, $"    result.add({CompilationContext.GetVar(field.Name)});");
            }
            context.WriteLine(output, "    return result;");
            context.WriteLine(output, "}");
            context.WriteLine(output);

            context.WriteLine(output, "@Generated");
            context.WriteLine(output, "@Override");
            context.WriteLine(output, "public List<Object> copyLocalVarValues() {");
            context.WriteLine(output, "    List<Object> result = super.copyLocalVarValues();");
            foreach (var field in machine.Fields)
            {
                context.WriteLine(output, $"    result.add({CompilationContext.GetVar(field.Name)});");
            }
            context.WriteLine(output, "    return result;");
            context.WriteLine(output, "}");
            context.WriteLine(output);

            context.WriteLine(output, "@Generated");
            context.WriteLine(output, "@Override");
            context.WriteLine(output, "protected int setLocalVarValues(List<Object> values) {");
            context.WriteLine(output, "    int idx = super.setLocalVarValues(values);");
            foreach (var field in machine.Fields)
            {
                context.WriteLine(output, $"    {CompilationContext.GetVar(field.Name)} = ({GetPExplicitType(field.Type)}) values.get(idx++);");
            }
            context.WriteLine(output, "    return idx;");
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteMachineConstructor(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.GetNameForDecl(machine);
            context.WriteLine(output, $"public {declName}(int id) {{");
            context.Write(output, $"super(\"{context.GetMachineName(machine)}\", id, {context.GetNameForDecl(machine.StartState)}");

            foreach (var state in machine.States)
            {
                context.WriteLine(output, $", {context.GetNameForDecl(state)}");
            }

            context.WriteLine(output);
            context.WriteLine(output, ");");

            foreach (var state in machine.States)
            {
                WriteHandlerUpdate(context, output, state);
            }

            foreach (var method in machine.Methods)
            {
                if (method is Continuation)
                {
                    var cont = (Continuation) method;
                    context.WriteLine(output, "registerContinuation(");
                    context.WriteLine(output, $"\"{context.GetContinuationName(cont)}\"");
                    context.WriteLine(output, $", (machine, msg) -> {context.GetContinuationName(cont)}(machine, msg)");
                    context.WriteLine(output, $", () -> clear_{context.GetContinuationName(cont)}()");
                    foreach (var (caseEvent, _) in cont.Cases)
                    {
                        context.Write(output, $", \"{caseEvent.Name}\"");
                    }
                    context.WriteLine(output, ");");

                    // context.Write(output, $"continuations.put(\"{context.GetContinuationName(cont)}\", ");
                    // context.Write(output, $"(pc) -> ((continuation_outcome, msg) -> {context.GetContinuationName(cont)}(");
                    // context.Write(output, "continuation_outcome");
                    // context.WriteLine(output, $", msg)));");
                    // context.WriteLine(output, $"clearContinuationVars.add(() -> clear_{context.GetContinuationName(cont)}());");
                }
            }

            context.WriteLine(output, "}");
        }

        private void WriteHandlerUpdate(CompilationContext context, StringWriter output, State state)
        {
            context.WriteLine(output, $"{context.GetNameForDecl(state)}.registerHandlers(");
            var first = true;
            foreach (var handler in state.AllEventHandlers)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    context.WriteLine(output, ",");
                }
                WriteEventHandler(context, output, handler, state);
            }
            context.WriteLine(output, ");");
        }

        private void WriteState(CompilationContext context, StringWriter output, State state, Machine machine)
        {
            var temperature = Enum.GetName(state.Temperature.GetType(), state.Temperature);
            context.Write(output, $"new State(\"{context.GetNameForDecl(state)}\", \"{context.GetNameForDecl(machine)}\", StateTemperature.{temperature}");
            context.WriteLine(output, ") {");
            if (state.Entry != null)
            {
                context.WriteLine(output, "@Generated");
                context.WriteLine(output, "@Override");
                context.WriteLine(output, $"public void entry(PMachine machine, PValue<?> payload) {{");
                context.WriteLine(output, $"super.entry(machine, payload);");

                var entryFunc = state.Entry;
                entryFunc.Name = $"{context.GetNameForDecl(state)}_entry";
                context.Write(output, $"(({context.GetNameForDecl(entryFunc.Owner)})machine).{context.GetNameForDecl(entryFunc)}(machine");
                if (entryFunc.Signature.Parameters.Any())
                {
                    Debug.Assert(entryFunc.Signature.Parameters.Count() == 1);
                    var payloadType = entryFunc.Signature.Parameters[0].Type;
                    context.Write(output, $", ({GetPExplicitType(payloadType)}) payload");
                }
                context.WriteLine(output, ");");

                context.WriteLine(output, "}");
            }
            if (state.Exit != null)
            {
                context.WriteLine(output, "@Override public void exit(PMachine machine) {");
                context.WriteLine(output, $"super.exit(machine);");

                var exitFunc = state.Exit;
                exitFunc.Name = $"{context.GetNameForDecl(state)}_exit";
                Debug.Assert(!(exitFunc.CanChangeState ?? false));
                Debug.Assert(!(exitFunc.CanRaiseEvent ?? false));
                if (exitFunc.Signature.Parameters.Count() != 0)
                    throw new NotImplementedException("Exit functions with payloads are not yet supported");
                context.WriteLine(output, $"(({context.GetNameForDecl(exitFunc.Owner)})machine).{context.GetNameForDecl(exitFunc)}(machine);");

                context.WriteLine(output, "}");
            }
            context.Write(output, "}");
        }

        private void WriteEventHandler(CompilationContext context, StringWriter output, KeyValuePair<PEvent, IStateAction> handler, State state)
        {
            var eventTag = context.GetNameForDecl(handler.Key);
            switch (handler.Value)
            {
                case EventDefer _:
                    context.WriteLine(output, $"new DeferEventHandler({eventTag})");
                    break;
                case EventDoAction action:
                    context.WriteLine(output, $"new EventHandler({eventTag}) {{");
                    context.WriteLine(output, "@Override public void handleEvent(PMachine machine, PValue<?> payload) {");
                    var actionFunc = action.Target;
                    if (actionFunc.Name == "")
                        actionFunc.Name = $"{context.GetNameForDecl(state)}_{eventTag}";
                    context.Write(output, $"(({context.GetNameForDecl(actionFunc.Owner)})machine).{context.GetNameForDecl(actionFunc)}(machine");
                    if (actionFunc.Signature.Parameters.Count() == 1)
                    {
                        Debug.Assert(!actionFunc.Signature.Parameters[0].Type.IsSameTypeAs(PrimitiveType.Null));
                        var payloadVsType = GetPExplicitType(actionFunc.Signature.Parameters[0].Type);
                        context.Write(output, $", ({payloadVsType}) payload");
                    }
                    context.WriteLine(output, ");");
                    context.WriteLine(output, "}");
                    context.Write(output, "}");
                    break;
                case EventGotoState gotoState:
                    var destTag = $"{context.GetNameForDecl(gotoState.Target)}";
                    context.Write(output, $"new GotoEventHandler({eventTag}, {destTag}");
                    context.Write(output, ")");

                    if (gotoState.TransitionFunction != null)
                    {
                        context.WriteLine(output, " {");
                        context.WriteLine(output, "@Override public void transitionFunction(PMachine machine, PValue<?> payload) {");

                        var transitionFunc = gotoState.TransitionFunction;
                        Debug.Assert(!(transitionFunc.CanChangeState ?? false));
                        Debug.Assert(!(transitionFunc.CanRaiseEvent ?? false));
                        if (transitionFunc.Name == "")
                            transitionFunc.Name = $"{context.GetNameForDecl(state)}_{eventTag}_{destTag}";

                        context.Write(output, $"(({context.GetNameForDecl(transitionFunc.Owner)})machine).{context.GetNameForDecl(transitionFunc)}(machine");
                        if (transitionFunc.Signature.Parameters.Count() == 1)
                        {
                            Debug.Assert(!transitionFunc.Signature.Parameters[0].Type.IsSameTypeAs(PrimitiveType.Null));
                            var payloadVsType = GetPExplicitType(transitionFunc.Signature.Parameters[0].Type);
                            context.Write(output, $", ({payloadVsType}) payload");
                        }
                        context.WriteLine(output, ");");
                        context.WriteLine(output, "}");
                        context.Write(output, "}");
                    }
                    break;
                case EventIgnore _:
                    context.Write(output, $"new IgnoreEventHandler({eventTag})");
                    break;
                default:
                    throw new NotImplementedException($"Unrecognized handler type {handler.Value.GetType().Name}");
            }
        }

        internal struct ControlFlowContext
        {
            public ControlFlowContext()
            {
            }

            internal static ControlFlowContext FreshFuncContext(CompilationContext context)
            {
                return new ControlFlowContext();
            }

            internal ControlFlowContext FreshLoopContext(CompilationContext context)
            {
                return new ControlFlowContext();
            }

            internal ControlFlowContext FreshBranchSubContext(CompilationContext context)
            {
                return new ControlFlowContext();
            }
        }

        private void WriteForeignFunction(CompilationContext context, StringWriter output, Function function)
        {
            var isStatic = function.Owner == null;

            if (function.CanReceive == true)
                throw new NotImplementedException($"Async functions {context.GetNameForDecl(function)} are not supported");

            var staticKeyword = isStatic ? "static " : "";

            string returnType = null;
            var returnStatement = "";
            if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                returnType = "void";

            } else {
                returnType = "Object";
                returnStatement = "return ";
            }

            var functionName = $"ffi_{context.GetNameForDecl(function)}";

            context.Write(output, $"{staticKeyword}{returnType} ");
            context.Write(output, functionName);
            context.WriteLine(output, "(List<Object> args) { ");
            context.Write(output, $"    {returnStatement}GlobalFunctions.{context.GetNameForDecl(function)}(");
            var i = 0;
            foreach (var param in function.Signature.Parameters)
            {
                if (i > 0)
                    context.WriteLine(output, ",");
                string foreignType = GetForeignType(param.Type);
                if (foreignType == "Object") {
                    context.Write(output, $"args.get({i})");
                } else {
                    context.Write(output, $"({foreignType})args.get({i})");
                }
                i++;
            }
            context.WriteLine(output, ");");
            context.WriteLine(output, " }");
        }

        private void WriteFunction(CompilationContext context, StringWriter output, Function function)
        {
            if (function is Continuation)
            {
                WriteContinuation(context, output, (Continuation) function);
                return;
            }

            var isStatic = function.Owner == null;

            var staticKeyword = isStatic ? "static " : "";

            string returnType = null;
            if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                returnType = "void";
            } else {
                returnType = GetPExplicitType(function.Signature.ReturnType);
            }

            context.ReturnType = function.Signature.ReturnType;
            var functionName = context.GetNameForDecl(function);

            context.WriteLine(output, $"{staticKeyword}{returnType} ");

            context.Write(output, functionName);

            context.WriteLine(output, $"(");
            context.Write(output, $"PMachine {CompilationContext.CurrentMachine}");
            foreach (var param in function.Signature.Parameters)
            {
                context.WriteLine(output, ",");
                context.Write(output, $"{GetPExplicitType(param.Type)} {CompilationContext.GetVar(param.Name)}");
            }
            context.WriteLine(output);
            context.Write(output, ") ");

            context.WriteLine(output, "{");

            WriteFunctionBody(context, output, function);

            context.WriteLine(output, "}");
            context.WriteLine(output);
            context.ReturnType = null;
        }

        private void WriteFunctionBody(CompilationContext context, StringWriter output, Function function)
        {
            foreach (var local in function.LocalVariables)
            {
                context.WriteLine(output, $"{GetPExplicitType(local.Type)} {CompilationContext.GetVar(local.Name)} =");
                context.WriteLine(output, $"    {GetDefaultValue(local.Type)};");
                context.WriteLine(output);
            }

            if (!function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                context.WriteLine(output, $"{GetPExplicitType(function.Signature.ReturnType)} {CompilationContext.ReturnValue} = {GetDefaultValue(function.Signature.ReturnType)};");
            }

            bool exited = false;
            if (function is WhileFunction)
            {
                /* Loop body */
                exited = WriteStmt(function, context, output, ControlFlowContext.FreshFuncContext(context), function.Body);
            } else
            {
                exited = WriteStmt(function, context, output, ControlFlowContext.FreshFuncContext(context), function.Body);
            }
            if (!exited)
            {
                if (!function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
                {
                    context.Write(output, $"return {CompilationContext.ReturnValue};");
                }
            }
        }

        private FunCallExpr TryGetCallInAssignment(IPStmt stmt)
        {
            if (stmt is AssignStmt assign)
            {
                if (assign.Value is FunCallExpr call)
                {
                    return call;
                }
            }
            return null;
        }

        private IPExpr UnnestCloneExpr(IPExpr expr)
        {
            switch (expr)
            {
                case CloneExpr cloneExpr:
                    return UnnestCloneExpr(cloneExpr.Term);
                default:
                    return expr;
            }
        }

        private bool WriteStmt(Function function, CompilationContext context, StringWriter output, ControlFlowContext flowContext, IPStmt stmt)
        {
            var exited = false;

            if (TryGetCallInAssignment(stmt) is { } callExpr)
            {
                WriteFunCallStmt(context, output, callExpr.Function, callExpr.Arguments, dest: (stmt as AssignStmt)?.Location);
                return false;
            }

            switch (stmt)
            {
                case AssignStmt assignStmt:
                    Debug.Assert(assignStmt.Value != null);
                    Debug.Assert(assignStmt.Location != null);
                    CheckIsSupportedAssignment(assignStmt.Value.Type, assignStmt.Location.Type);

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        assignStmt.Location,
                        false,
                        locationTemp =>
                        {
                            var expr = UnnestCloneExpr(assignStmt.Value);
                            if (expr is NullLiteralExpr)
                            {
                                context.WriteLine(output, $"{locationTemp} = {GetDefaultValue(assignStmt.Location.Type)};");
                            } else
                            {
                                context.Write(output, $"{locationTemp} = ({GetPExplicitType(assignStmt.Location.Type)}) ");
                                WriteExpr(context, output, expr);
                                context.WriteLine(output, ";");
                            }
                        }
                    );

                    break;

                case MoveAssignStmt moveStmt:
                    CheckIsSupportedAssignment(moveStmt.FromVariable.Type, moveStmt.ToLocation.Type);

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        moveStmt.ToLocation,
                        false,
                        locationTemp =>
                        {
                            context.Write(output, $"{locationTemp} = ({GetPExplicitType(moveStmt.ToLocation.Type)}) ");
                            WriteExpr(context, output, new VariableAccessExpr(moveStmt.FromVariable.SourceLocation, moveStmt.FromVariable));
                            context.WriteLine(output, ";");
                        }
                    );

                    break;

                case AssertStmt assertStmt:
                    context.Write(output, "Assert.fromModel((");
                    WriteExpr(context, output, assertStmt.Assertion);
                    context.Write(output, ").getValue(), ");
                    WriteExpr(context, output, assertStmt.Message);
                    context.Write(output, $");");
                    break;

                case ReturnStmt returnStmt:
                    if (!(returnStmt.ReturnValue is null))
                    {
                        context.Write(output, $"{CompilationContext.ReturnValue} = ");
                        context.Write(output, $"({GetPExplicitType(context.ReturnType)}) ");
                        WriteExpr(context, output, returnStmt.ReturnValue);
                        context.WriteLine(output, $";");
                        context.Write(output, $"return {CompilationContext.ReturnValue};");
                    }
                    else
                    {
                        context.Write(output, "return;");
                    }
                    exited = true;
                    break;

                case GotoStmt gotoStmt:
                    context.Write(output, $"{CompilationContext.CurrentMachine}.gotoState({context.GetNameForDecl(gotoStmt.State)}");
                    if (gotoStmt.Payload == null)
                    {
                        context.Write(output, ", null");
                    }
                    else
                    {
                        context.Write(output, $", ");
                        WriteExpr(context, output, gotoStmt.Payload);
                    }
                    context.WriteLine(output, ");");

                    if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                        context.WriteLine(output, "return;");
                    } else {
                        context.WriteLine(output, "return null;");
                    }
                    exited = true;
                    break;

                case RaiseStmt raiseStmt:
                    // TODO: Add type checking for the payload!
                    context.WriteLine(output, "// NOTE (TODO): We currently perform no typechecking on the payload!");

                    context.Write(output, $"{CompilationContext.CurrentMachine}.raiseEvent(");
                    WriteExpr(context, output, raiseStmt.PEvent);
                    if (raiseStmt.Payload.Count > 0)
                    {
                        // TODO: Determine how multi-payload raise statements are supposed to work
                        Debug.Assert(raiseStmt.Payload.Count == 1);
                        context.Write(output, ", ");
                        WriteExpr(context, output, raiseStmt.Payload[0]);
                    }
                    context.WriteLine(output, ");");

                    if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                        context.WriteLine(output, "return;");
                    } else {
                        context.WriteLine(output, "return null;");
                    }
                    exited = true;
                    break;

                case PrintStmt printStmt:
                    context.Write(output, "PExplicitLogger.logModel(");
                    WriteExpr(context, output, printStmt.Message);
                    context.WriteLine(output, ".toString());");
                    break;

                case BreakStmt _:
                    context.WriteLine(output, "break;");
                    break;

                case ContinueStmt _:
                    context.WriteLine(output, "continue;");
                    break;

                case CompoundStmt compoundStmt:
                    foreach (var subStmt in compoundStmt.Statements)
                    {
                        exited |= WriteStmt(function, context, output, flowContext, subStmt);
                        context.WriteLine(output);

                        if (exited)
                        {
                            break;
                        }
                    }
                    break;

                case WhileStmt whileStmt:
                    if (!(whileStmt.Condition is BoolLiteralExpr) && ((BoolLiteralExpr)whileStmt.Condition).Value)
                    {
                        throw new ArgumentOutOfRangeException("While statement condition should always be transformed to constant 'true' during IR simplification.");
                    }

                    var loopContext = flowContext.FreshLoopContext(context);

                    /* Loop body */
                    context.WriteLine(output, $"while (true) {{");
                    exited = WriteStmt(function, context, output, loopContext, whileStmt.Body);
                    context.WriteLine(output, "}");

                    break;

                case IfStmt ifStmt:
                    /* Prologue */

                    var condTemp = context.FreshTempVar();
                    Debug.Assert(ifStmt.Condition.Type.IsSameTypeAs(PrimitiveType.Bool));
                    context.Write(output, $"{GetPExplicitType(PrimitiveType.Bool)} {condTemp} = ");
                    WriteExpr(context, output, ifStmt.Condition);
                    context.WriteLine(output, ";");

                    var thenContext = flowContext.FreshBranchSubContext(context);
                    var elseContext = flowContext.FreshBranchSubContext(context);

                    /* Body */

                    context.WriteLine(output, $"if ({condTemp}.getValue()) {{");
                    context.WriteLine(output, "// 'then' branch");
                    exited = WriteStmt(function, context, output, thenContext, ifStmt.ThenBranch);
                    context.WriteLine(output, "}");

                    if (!(ifStmt.ElseBranch is null))
                    {
                        context.WriteLine(output, $"else {{");
                        context.WriteLine(output, "// 'else' branch");
                        exited &= WriteStmt(function, context, output, elseContext, ifStmt.ElseBranch);
                        context.WriteLine(output, "}");
                    }

                    break;

                case FunCallStmt funCallStmt:
                    WriteFunCallStmt(context, output, funCallStmt.Function, funCallStmt.ArgsList);
                    break;

                case CtorStmt ctorStmt:
                    WriteCtorExpr(context, output, ctorStmt.Interface, ctorStmt.Arguments);
                    context.WriteLine(output, ";");
                    break;

                case SendStmt sendStmt:
                    context.Write(output, $"{CompilationContext.CurrentMachine}.sendEvent(");
                    WriteExpr(context, output, sendStmt.MachineExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, sendStmt.Evt);
                    context.Write(output, ", ");
                    if (sendStmt.Arguments.Count == 0)
                        context.Write(output, "null");
                    else if (sendStmt.Arguments.Count == 1)
                    {
                        WriteExpr(context, output, sendStmt.Arguments[0]);
                    }
                    else
                        throw new NotImplementedException("Send statements with more than one payload argument are not supported");
                    context.WriteLine(output, ");");
                    break;

                case InsertStmt insertStmt:
                {
                    var isMap = PLanguageType.TypeIsOfKind(insertStmt.Variable.Type, TypeKind.Map);
                    var isSet = PLanguageType.TypeIsOfKind(insertStmt.Variable.Type, TypeKind.Set);
                    PLanguageType keyType = null;
                    PLanguageType elementType;
                    if (isMap) {
                        keyType = ((MapType) insertStmt.Variable.Type.Canonicalize()).KeyType;
                        elementType = ((MapType) insertStmt.Variable.Type.Canonicalize()).ValueType;
                    } else if (isSet) {
                        elementType = ((SetType) insertStmt.Variable.Type.Canonicalize()).ElementType;
                    } else {
                        elementType = ((SequenceType) insertStmt.Variable.Type.Canonicalize()).ElementType;
                    }

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        insertStmt.Variable,
                        true,
                        (structureTemp) =>
                        {
                            context.Write(output, $"{structureTemp} = ");
                            context.Write(output, $"(({GetPExplicitType(insertStmt.Variable.Type)}) ");
                            WriteExpr(context, output, insertStmt.Variable);
                            context.Write(output, $").add(");

                            WriteExpr(context, output, insertStmt.Index);
                            context.Write(output, ", ");
                            WriteExpr(context, output, insertStmt.Value);

                            context.WriteLine(output, ");");
                        }
                    );

                    break;
                }

                case AddStmt addStmt:
                {
                    WriteWithLValueMutationContext(
                        context,
                        output,
                        addStmt.Variable,
                        true,
                        (structureTemp) =>
                        {
                            context.Write(output, $"{structureTemp} = ");
                            context.Write(output, $"(({GetPExplicitType(addStmt.Variable.Type)}) ");
                            WriteExpr(context, output, addStmt.Variable);
                            context.Write(output, $").add(");
                            WriteExpr(context, output, addStmt.Value);
                            context.WriteLine(output, ");");
                        }
                    );

                    break;
                }

                case RemoveStmt removeStmt:
                {
                    var isMap = PLanguageType.TypeIsOfKind(removeStmt.Variable.Type, TypeKind.Map);
                    var isSet = PLanguageType.TypeIsOfKind(removeStmt.Variable.Type, TypeKind.Set);

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        removeStmt.Variable,
                        true,
                        (structureTemp) =>
                        {
                            context.Write(output, $"{structureTemp} = ");
                            context.Write(output, $"(({GetPExplicitType(removeStmt.Variable.Type)}) ");
                            WriteExpr(context, output, removeStmt.Variable);

                            if (isMap || isSet)
                                context.Write(output, $").remove(");
                            else
                                context.Write(output, $").removeAt(");

                            WriteExpr(context, output, removeStmt.Value);
                            context.WriteLine(output, ");");
                        }
                    );
                    break;
                }
                case AnnounceStmt announceStmt:
                    context.Write(output, $"{CompilationContext.SchedulerVar}.announce(");
                    WriteExpr(context, output, announceStmt.PEvent);
                    context.Write(output, ", ");
                    if (announceStmt.Payload == null)
                        context.Write(output, "null");
                    else {
                        WriteExpr(context, output, announceStmt.Payload);
                    }
                    context.WriteLine(output, ");");
                    break;
                case ReceiveSplitStmt splitStmt:
                    context.WriteLine(output, $"{CompilationContext.CurrentMachine}.blockUntil(\"{context.GetContinuationName(splitStmt.Cont)}\");");
                    context.Write(output, "return;");
                    exited = true;
                    break;
                default:
                    throw new NotImplementedException($"Statement type '{stmt.GetType().Name}' is not supported, found in {function.Name}");
            }
            return exited;
        }

        private void WriteContinuation(CompilationContext context, StringWriter output, Continuation continuation)
        {
            var voidReturn = continuation.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null);
            if (!voidReturn)
            {
                throw new NotImplementedException($"Receive statement in a function with non-void return type is not supported. Found in function named {continuation.ParentFunction.Name}.");
            }

            context.Write(output, $"void clear_{context.GetContinuationName(continuation)}() ");
            context.WriteLine(output, "{");
            foreach (var param in continuation.StoreParameters)
            {
                context.WriteLine(output, $"{GetPExplicitType(param.Type)} {CompilationContext.GetVar(param.Name)} = {GetDefaultValue(param.Type)};");
            }
            context.WriteLine(output, "}");

            var continuationName = context.GetContinuationName(continuation);

            context.WriteLine(output, $"void "); // return type
            context.Write(output, continuationName);

            context.WriteLine(output, $"(");
            context.Write(output, $"PMachine {CompilationContext.CurrentMachine}");
            context.WriteLine(output, ",");
            var messageName = $"{continuationName}_msg";
            context.WriteLine(output, $"PMessage {messageName}");
            context.WriteLine(output);
            context.Write(output, ") ");

            context.WriteLine(output, "{");

            var continuationLocalParams = new HashSet<string>();
            foreach (var local in continuation.LocalParameters)
            {
                continuationLocalParams.Add(local.Name);
                context.Write(output, $"{GetPExplicitType(local.Type)} {CompilationContext.GetVar(local.Name)}");
                context.WriteLine(output, $"= {CompilationContext.GetVar(continuation.StoreForLocal[local].Name)};");
            }

            context.WriteLine(output, $"switch ({messageName}.getEvent().toString())");
            context.WriteLine(output, "{");

            bool allCasesExited = true;
            foreach (var (caseEvent, caseFun) in continuation.Cases)
            {
                context.WriteLine(output, $"case \"{caseEvent.Name}\":");
                context.WriteLine(output, "{");
                var caseContext = ControlFlowContext.FreshFuncContext(context);
                if (caseFun.Signature.Parameters.Count > 0)
                {
                    if (caseFun.Signature.Parameters.Count > 1)
                    {
                        throw new NotImplementedException($"Too many parameters ({caseFun.Signature.Parameters.Count}) in receive case");
                    }
                    var arg =caseFun.Signature.Parameters[0];
                    var argValue = new Variable($"{arg.Name}_payload", continuation.SourceLocation, VariableRole.Param);
                    argValue.Type = PrimitiveType.Any;
                    context.WriteLine(output, $"PValue<?> var_{arg.Name}_payload = {messageName}.getPayload();");
                    var assignMsg = new AssignStmt(continuation.SourceLocation, new VariableAccessExpr(continuation.SourceLocation, arg), new VariableAccessExpr(continuation.SourceLocation, argValue));
                    context.WriteLine(output, $"{GetPExplicitType(arg.Type)} {CompilationContext.GetVar(arg.Name)} = {GetDefaultValue(arg.Type)};");
                    WriteStmt(continuation, context, output, caseContext, assignMsg);
                }
                foreach (var local in caseFun.LocalVariables)
                {
                    if (!continuationLocalParams.Contains(local.Name))
                    {
                        context.WriteLine(output, $"{GetPExplicitType(local.Type)} {CompilationContext.GetVar(local.Name)} = {GetDefaultValue(local.Type)};");
                    }
                }
                bool caseExited = WriteStmt(continuation, context, output, caseContext, caseFun.Body);
                allCasesExited &= caseExited;
                context.WriteLine(output, "}");
                if (!caseExited)
                {
                    context.WriteLine(output, "break;");
                }
            }
            context.WriteLine(output, "default:");
            context.WriteLine(output, "{");
            context.WriteLine(output, "Assert.fromModel(false, \"Unexpected event received in a continuation.\");");
            context.WriteLine(output, "}");
            context.WriteLine(output, "}");
            if (continuation.After != null && !allCasesExited)
            {
                var afterCaseContext = ControlFlowContext.FreshFuncContext(context);
                WriteStmt(continuation, context, output, afterCaseContext, continuation.After);
            }
            context.WriteLine(output, "}");
            context.WriteLine(output);
            context.ReturnType = null;
        }

        private void CheckIsSupportedAssignment(PLanguageType valueType, PLanguageType locationType)
        {
            var valueIsMachineRef = valueType.IsSameTypeAs(PrimitiveType.Machine) || valueType is PermissionType;
            var locationIsMachineRef = locationType.IsSameTypeAs(PrimitiveType.Machine) || locationType is PermissionType;

            if (valueIsMachineRef && locationIsMachineRef)
                return;

            if (locationType.IsSameTypeAs(PrimitiveType.Any) || valueType.IsSameTypeAs(PrimitiveType.Any))
                return;

            if (locationType.IsSameTypeAs(PrimitiveType.Data) || valueType.IsSameTypeAs(PrimitiveType.Data))
                return;

            valueType = valueType.Canonicalize();
            locationType = locationType.Canonicalize();
            if ((valueType is NamedTupleType) && locationType is NamedTupleType)
            {
                var valueTupleType = (NamedTupleType) valueType;
                var locationTupleType = (NamedTupleType) locationType;

                if (valueTupleType.Fields.Count != locationTupleType.Fields.Count)
                    throw new NotImplementedException(
                        $"Cannot yet handle assignment to variable of type {locationType.CanonicalRepresentation} " +
                        $"from value of type {valueType.CanonicalRepresentation}");

                for(var i = 0; i < valueTupleType.Fields.Count; i++)
                {
                    if (!valueTupleType.Fields[i].Name.Equals(locationTupleType.Fields[i].Name))
                    {
                        throw new NotImplementedException(
                            $"Cannot yet handle assignment to variable of type {locationType.CanonicalRepresentation} " +
                            $"from value of type {valueType.CanonicalRepresentation}");
                    }
                    CheckIsSupportedAssignment(valueTupleType.Types[i], locationTupleType.Types[i]);
                }
                return;
            }

            if ((valueType is TupleType) && locationType is TupleType)
            {
                var valueTupleType = (TupleType) valueType;
                var locationTupleType = (TupleType) locationType;

                if (valueTupleType.Types.Count != locationTupleType.Types.Count)
                    throw new NotImplementedException(
                        $"Cannot yet handle assignment to variable of type {locationType.CanonicalRepresentation} " +
                        $"from value of type {valueType.CanonicalRepresentation}");

                for(var i = 0; i < valueTupleType.Types.Count; i++)
                {
                    CheckIsSupportedAssignment(valueTupleType.Types[i], locationTupleType.Types[i]);
                }
                return;
            }

            if (!locationType.IsAssignableFrom(valueType))
            {
                throw new NotImplementedException(
                    $"Cannot yet handle assignment to variable of type {locationType.CanonicalRepresentation} " +
                    $"from value of type {valueType.CanonicalRepresentation}");
            }
        }

        private void WriteForeignFunCallStmt(CompilationContext context, StringWriter output, Function function, IReadOnlyList<IPExpr> args, IPExpr dest=null)
        {
            string returnTemp = null;
            if (!function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                returnTemp = context.FreshTempVar();
                context.Write(output, $"{GetPExplicitType(function.Signature.ReturnType)} {returnTemp} = ({GetPExplicitType(function.Signature.ReturnType)})");
            }

            if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                context.Write(output, $"ForeignFunctionInterface.accept(");
                context.Write(output, $"x -> ffi_{context.GetNameForDecl(function)}(x)");
            } else {
                context.Write(output, $"ForeignFunctionInterface.apply(");
                context.Write(output, $"x -> ");
                context.Write(output, "{ return ");
                context.Write(output, $"ffi_{context.GetNameForDecl(function)}(x);");
                context.Write(output, " }");
            }

            for (var i = 0; i < args.Count(); i++)
            {
                var param = args.ElementAt(i);
                context.Write(output, ", ");
                WriteExpr(context, output, param);
            }
            context.WriteLine(output, ");");

            if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                Debug.Assert(dest == null);
            } else {
                if (dest != null)
                    WriteWithLValueMutationContext(context, output, dest, false, (lhs) => context.WriteLine(output, $"{lhs} = {returnTemp};"));
            }
        }

        private void WriteFunCallStmt(CompilationContext context, StringWriter output, Function function, IReadOnlyList<IPExpr> args, IPExpr dest=null)
        {
            if (function.IsForeign)
            {
                WriteForeignFunCallStmt(context, output, function, args, dest);
                return;
            }

            string returnTemp = null;
            if (!function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                returnTemp = context.FreshTempVar();
                context.Write(output, $"{GetPExplicitType(function.Signature.ReturnType)} {returnTemp} = ");
            }

            context.Write(output, $"{context.GetNameForDecl(function)}({CompilationContext.CurrentMachine}");


            for (var i = 0; i < args.Count(); i++)
            {
                var param = args.ElementAt(i);
                context.Write(output, ", ");
                WriteExpr(context, output, param);
            }

            context.WriteLine(output, ");");

            if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null)) {
                Debug.Assert(dest == null);
            } else {
                if (dest != null)
                    WriteWithLValueMutationContext(context, output, dest, false, (lhs) => context.WriteLine(output, $"{lhs} = {returnTemp};"));
            }
        }

        private void WriteWithLValueMutationContext(
            CompilationContext context,
            StringWriter output,
            IPExpr lvalue,
            bool needOrigValue,
            Action<string> writeMutator)
        {
            PLanguageType elementType;
            switch (lvalue)
            {
                case MapAccessExpr mapAccessExpr:
                    elementType = mapAccessExpr.Type;
                    var indexExpr = mapAccessExpr.IndexExpr;
                    var indexType = indexExpr.Type;

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        mapAccessExpr.MapExpr,
                        true,
                        mapTemp =>
                        {
                            var elementTemp = context.FreshTempVar();
                            var indexTemp = context.FreshTempVar();

                            context.Write(output, $"{GetPExplicitType(indexType)} {indexTemp} = ");
                            WriteExpr(context, output, indexExpr);
                            context.WriteLine(output, ";");

                            context.Write(output, $"{GetPExplicitType(elementType)} {elementTemp}");
                            if (needOrigValue)
                            {
                                context.Write(output, $" = ({GetPExplicitType(elementType)}) {mapTemp}.getOrDefault({indexTemp}, ");
                                context.Write(output, GetDefaultValue(elementType));
                                context.WriteLine(output, ");");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(elementTemp);

                            context.Write(output, $"{mapTemp} = {mapTemp}.put(");

                            PLanguageType keyType;
                            PLanguageType valueType;
                            keyType = ((MapType) mapAccessExpr.MapExpr.Type.Canonicalize()).KeyType;
                            valueType = ((MapType) mapAccessExpr.MapExpr.Type.Canonicalize()).ValueType;

                            context.Write(output, $"{indexTemp}");
                            context.Write(output, ", ");
                            context.Write(output, $"{elementTemp}");
                            context.WriteLine(output, ");");
                        }
                    );
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    WriteWithLValueMutationContext(
                        context,
                        output,
                        namedTupleAccessExpr.SubExpr,
                        true,
                        namedTupleTemp =>
                        {
                            var fieldTemp = context.FreshTempVar();
                            var fieldType = GetPExplicitType(namedTupleAccessExpr.Type);
                            context.Write(output, $"{fieldType} {fieldTemp}");
                            if (needOrigValue)
                            {
                                context.Write(output, $" = ({fieldType})");
                                context.Write(output, $"{namedTupleTemp}.getField(\"{namedTupleAccessExpr.FieldName}\");");
                                context.WriteLine(output, ";");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(fieldTemp);

                            context.WriteLine(
                                output,
                                $"{namedTupleTemp} = ({GetPExplicitType(namedTupleAccessExpr.SubExpr.Type)}) {namedTupleTemp}.setField(" +
                                $"\"{namedTupleAccessExpr.FieldName}\", " +
                                $"{fieldTemp});");
                        }
                    );
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    WriteWithLValueMutationContext(
                        context,
                        output,
                        tupleAccessExpr.SubExpr,
                        true,
                        tupleTemp =>
                        {
                            var fieldTemp = context.FreshTempVar();
                            var fieldType = GetPExplicitType(tupleAccessExpr.Type);
                            context.Write(output, $"{fieldType} {fieldTemp}");
                            if (needOrigValue)
                            {
                                context.Write(output, $" = ({fieldType})");
                                context.Write(output, $"{tupleTemp}.getField({tupleAccessExpr.FieldNo})");
                                context.WriteLine(output, ";");
                            }
                            else
                            {
                                context.Write(output, ";");
                            }

                            writeMutator(fieldTemp);

                            context.WriteLine(
                                output,
                                $"{tupleTemp} = ({GetPExplicitType(tupleAccessExpr.SubExpr.Type)}) {tupleTemp}.setField(" +
                                $"{tupleAccessExpr.FieldNo}," +
                                $"{fieldTemp});");
                        }
                    );
                    break;

                case SeqAccessExpr seqAccessExpr:
                    elementType = seqAccessExpr.Type;

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        seqAccessExpr.SeqExpr,
                        true,
                        seqTemp =>
                        {
                            var elementTemp = context.FreshTempVar();
                            var indexTemp = context.FreshTempVar();

                            context.Write(output, $"{GetPExplicitType(PrimitiveType.Int)} {indexTemp} = ");
                            WriteExpr(context, output, seqAccessExpr.IndexExpr);
                            context.WriteLine(output, ";");

                            context.Write(output, $"{GetPExplicitType(elementType)} {elementTemp}");
                            if (needOrigValue)
                            {
                                context.WriteLine(output, $" = ({GetPExplicitType(elementType)}) {seqTemp}.get({indexTemp});");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(elementTemp);

                            context.Write(output, $"{seqTemp} = {seqTemp}.set({indexTemp}, ");

                            PLanguageType valueType;
                            valueType = ((SequenceType) seqAccessExpr.SeqExpr.Type.Canonicalize()).ElementType;

                            context.Write(output, $"{elementTemp}");
                            context.WriteLine(output, ");");
                        }
                    );
                    break;

                case SetAccessExpr setAccessExpr:
                    elementType = setAccessExpr.Type;

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        setAccessExpr.SetExpr,
                        true,
                        setTemp =>
                        {
                            var elementTemp = context.FreshTempVar();
                            var indexTemp = context.FreshTempVar();

                            context.Write(output, $"{GetPExplicitType(PrimitiveType.Int)} {indexTemp} = ");
                            WriteExpr(context, output, setAccessExpr.IndexExpr);
                            context.WriteLine(output, ";");

                            context.Write(output, $"{GetPExplicitType(elementType)} {elementTemp}");
                            if (needOrigValue)
                            {
                                context.WriteLine(output, $" = ({GetPExplicitType(elementType)}) {setTemp}.get({indexTemp});");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(elementTemp);

                            context.WriteLine(output, $"{setTemp} = {setTemp}.set({indexTemp}, {elementTemp});");
                        }
                    );
                    break;
                case VariableAccessExpr variableAccessExpr:
                    var name = variableAccessExpr.Variable.Name;
                    var unguarded = CompilationContext.GetVar(name);
                    var guardedTemp = context.FreshTempVar();

                    context.Write(output, $"{GetPExplicitType(variableAccessExpr.Type)} {guardedTemp}");
                    if (needOrigValue)
                    {
                        context.WriteLine(output, $" = ({GetPExplicitType(variableAccessExpr.Type)}) {unguarded};    ");
                    }
                    else
                    {
                        context.WriteLine(output, ";");
                    }
                    writeMutator(guardedTemp);
                    context.WriteLine(output, $"{unguarded} = {guardedTemp};");
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Expression type '{lvalue.GetType().Name}' is not an lvalue");
            }
        }

        private static string TransformPrintMessage(string message)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < message.Length; i++)
            {
                if (message[i] == '\'')
                {
                    sb.Append("''");
                }
                else if (message[i] == '{')
                {
                    if (i + 1 == message.Length)
                    {
                        throw new ArgumentException("unmatched opening brace", nameof(message));
                    }

                    if (message[i + 1] == '{')
                    {
                        // handle "{{"
                        i++;
                        sb.Append("'{'");
                    }
                    else if (char.IsDigit(message[i + 1]))
                    {
                        sb.Append("{");
                        while (++i < message.Length && '0' <= message[i] && message[i] <= '9')
                        {
                            sb.Append(message[i]);
                        }

                        if (i == message.Length || message[i] != '}')
                        {
                            throw new ArgumentException("unmatched opening brace in position expression",
                                nameof(message));
                        }
                        sb.Append("}");
                    }
                    else
                    {
                        throw new ArgumentException("opening brace not followed by digits", nameof(message));
                    }
                }
                else if (message[i] == '}')
                {
                    if (i + 1 == message.Length || message[i + 1] != '}')
                    {
                        throw new ArgumentException("unmatched closing brace", nameof(message));
                    }

                    // handle "}}"
                    sb.Append("'}'");
                    i++;
                }
                else
                {
                    sb.Append(message[i]);
                }
            }

            return sb.ToString();
        }

        private void WriteExpr(CompilationContext context, StringWriter output, IPExpr expr)
        {
            PLanguageType elementType;
            switch (expr)
            {
                case CloneExpr cloneExpr:
                    WriteExpr(context, output, cloneExpr.Term);
                    break;
                case UnaryOpExpr unaryOpExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, unaryOpExpr.SubExpr);
                    context.Write(output, $").{UnOpToStr(unaryOpExpr.Operation)}()");
                    break;
                case BinOpExpr binOpExpr:
                    var isEquality = binOpExpr.Operation == BinOpType.Eq || binOpExpr.Operation == BinOpType.Neq;

                    if (isEquality)
                    {
                        context.Write(output, "new PBool(PValue.");
                        if (binOpExpr.Operation == BinOpType.Eq)
                        {
                            context.Write(output, "isEqual(");
                        }
                        else
                        {
                            context.Write(output, "notEqual(");
                        }
                        WriteExpr(context, output, binOpExpr.Lhs);
                        context.Write(output, ", ");
                        WriteExpr(context, output, binOpExpr.Rhs);
                        context.Write(output, ")");
                        context.Write(output, ")");
                    }
                    else
                    {
                        var isPrimitive = binOpExpr.Lhs.Type.Canonicalize() is PrimitiveType && binOpExpr.Rhs.Type.Canonicalize() is PrimitiveType;
                        if (!isPrimitive)
                        {
                            var str = $"lhs type: {binOpExpr.Lhs}, rhs type: {binOpExpr.Rhs}" ;
                            throw new NotImplementedException("Binary operations are currently only supported between primitive types and enums | " + str);
                        }

                        context.Write(output, "(");
                        WriteExpr(context, output, binOpExpr.Lhs);
                        context.Write(output, $").{BinOpToStr(binOpExpr.Operation)}(");
                        if (binOpExpr.Rhs is NullLiteralExpr)
                            context.Write(output, $"{GetDefaultValue(binOpExpr.Lhs.Type)}");
                        else
                            WriteExpr(context, output, binOpExpr.Rhs);
                        context.Write(output, ")");
                    }
                    break;
                case BoolLiteralExpr boolLiteralExpr:
                {
                    var unguarded = $"new { GetPExplicitType(PrimitiveType.Bool) }" + $"({boolLiteralExpr.Value})".ToLower();
                    context.Write(output, unguarded);
                    break;
                }
                case CastExpr castExpr:
                    if (castExpr.SubExpr is NullLiteralExpr)
                    {
                        context.Write(output,  GetDefaultValue(castExpr.Type));
                    } else
                    {
                        WriteExpr(context, output, castExpr.SubExpr);
                    }
                    break;
                case CoerceExpr coerceExpr:
                    switch (coerceExpr.Type.Canonicalize())
                    {
                        case PrimitiveType oldType when oldType.IsSameTypeAs(PrimitiveType.Float):
                            context.Write(output, "(");
                            WriteExpr(context, output, coerceExpr.SubExpr);
                            context.Write(output, $").toFloat()");
                            break;
                        case PrimitiveType oldType when oldType.IsSameTypeAs(PrimitiveType.Int):
                            context.Write(output, "(");
                            WriteExpr(context, output, coerceExpr.SubExpr);
                            context.Write(output, $").toInt()");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                @"unexpected coercion operation to:" + coerceExpr.Type.CanonicalRepresentation);
                    }
                    break;
                case DefaultExpr defaultExpr:
                    context.Write(output, GetDefaultValue(defaultExpr.Type));
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                {
                    var unguarded = $"new { GetPExplicitType(PrimitiveType.Float) }({floatLiteralExpr.Value}f)";
                    context.Write(output, unguarded);
                    break;
                }
                case IntLiteralExpr intLiteralExpr:
                {
                    var unguarded = $"new { GetPExplicitType(PrimitiveType.Int) }({intLiteralExpr.Value})";
                    context.Write(output, unguarded);
                    break;
                }
                case KeysExpr keyExpr:
                    WriteExpr(context, output, keyExpr.Expr);
                    context.Write(output, $".getKeys()");
                    break;
                case ValuesExpr valuesExpr:
                    WriteExpr(context, output, valuesExpr.Expr);
                    context.Write(output, $".getValues()");
                    break;
                case MapAccessExpr mapAccessExpr:
                    context.Write(output, $"(({GetPExplicitType(mapAccessExpr.MapExpr.Type)})");
                    WriteExpr(context, output, mapAccessExpr.MapExpr);
                    context.Write(output, ").get(");
                    WriteExpr(context, output, mapAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, $"(({GetPExplicitType(seqAccessExpr.SeqExpr.Type)})");
                    WriteExpr(context, output, seqAccessExpr.SeqExpr);
                    context.Write(output, ").get(");
                    WriteExpr(context, output, seqAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;
                case SetAccessExpr setAccessExpr:
                    context.Write(output, $"(({GetPExplicitType(setAccessExpr.SetExpr.Type)})");
                    WriteExpr(context, output, setAccessExpr.SetExpr);
                    context.Write(output, ").get(");
                    WriteExpr(context, output, setAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, $"(({GetPExplicitType(namedTupleAccessExpr.Type)})(");
                    context.Write(output, $"(({GetPExplicitType(namedTupleAccessExpr.SubExpr.Type)})");
                    WriteExpr(context, output, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $").getField(\"{namedTupleAccessExpr.FieldName}\")))");
                    break;
                case ThisRefExpr _:
                    context.Write(output, $"new PMachineValue(this)");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, $"({GetPExplicitType(tupleAccessExpr.Type)})(");
                    var tupleType = (tupleAccessExpr.SubExpr.Type.Canonicalize() as TupleType);
                    context.Write(output, $"(({GetPExplicitType(tupleAccessExpr.SubExpr.Type)})");
                    WriteExpr(context, output, tupleAccessExpr.SubExpr);
                    context.Write(output, $").getField({tupleAccessExpr.FieldNo}))");
                    break;
                case NamedTupleExpr namedTupleExpr:
                    context.WriteLine(output, "new PNamedTuple(");
                    var fields = (namedTupleExpr.Type.Canonicalize() as NamedTupleType).Fields;
                    var nttype = namedTupleExpr.Type as NamedTupleType;

                    context.Write(output, "List.of(");
                    for (var i = 0; i < namedTupleExpr.TupleFields.Count; i++)
                    {
                        context.Write(output, $"\"{fields[i].Name}\"");
                        if (i + 1 != namedTupleExpr.TupleFields.Count)
                            context.Write(output, ", ");
                    }
                    context.WriteLine(output, "), ");
                    
                    context.Write(output, "Arrays.asList(");
                    if (namedTupleExpr.TupleFields.Count == 1)
                    {
                        context.Write(output, "(PValue<?>) ");
                    }
                    for (var i = 0; i < namedTupleExpr.TupleFields.Count; i++)
                    {
                        var field = namedTupleExpr.TupleFields[i];
                        var castExpr = new CastExpr(field.SourceLocation, field, nttype.Types[i]);
                        WriteExpr(context, output, castExpr);
                        if (i + 1 != namedTupleExpr.TupleFields.Count)
                            context.Write(output, ", ");
                    }
                    context.WriteLine(output, ")");

                    context.WriteLine(output, ")");
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    context.Write(output, "new PTuple(");
                    var ttype = (TupleType) unnamedTupleExpr.Type;
                    for (var i = 0; i < unnamedTupleExpr.TupleFields.Count; i++)
                    {
                        var castExpr = new CastExpr(unnamedTupleExpr.SourceLocation, unnamedTupleExpr.TupleFields[i], ttype.Types[i]);
                        WriteExpr(context, output, castExpr);
                        if (i + 1 != unnamedTupleExpr.TupleFields.Count)
                            context.Write(output, ", ");
                    }
                    context.Write(output, ")");
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                {
                    var unguarded = $"new { GetPExplicitType(enumElemRefExpr.Type) }(\"{enumElemRefExpr.Type.OriginalRepresentation}\", \"{enumElemRefExpr.Value.Name}\", {enumElemRefExpr.Value.Value})";
                    context.Write(output, unguarded);
                    break;
                }
                case EventRefExpr eventRefExpr:
                {
                    var unguarded = $"new { GetPExplicitType(PrimitiveType.Event) }({context.GetNameForDecl(eventRefExpr.Value)})";
                    context.Write(output, unguarded);
                    break;
                }
                case VariableAccessExpr variableAccessExpr:
                    context.Write(output, $"{CompilationContext.GetVar(variableAccessExpr.Variable.Name)}");
                    break;
                case FunCallExpr _:
                    throw new InvalidOperationException("Compilation of call expressions should be handled as part of assignment statements");
                case ContainsExpr containsExpr:
                    var isMap = PLanguageType.TypeIsOfKind(containsExpr.Collection.Type, TypeKind.Map);
                    var isSet = PLanguageType.TypeIsOfKind(containsExpr.Collection.Type, TypeKind.Set);
                    if (isMap) {
                        elementType = ((MapType) containsExpr.Collection.Type.Canonicalize()).KeyType;
                    } else if (isSet) {
                        elementType = ((SetType) containsExpr.Collection.Type.Canonicalize()).ElementType;
                    } else {
                        elementType = ((SequenceType) containsExpr.Collection.Type.Canonicalize()).ElementType;
                    }

                    WriteExpr(context, output, containsExpr.Collection);
                    context.Write(output, ".contains(");
                    WriteExpr(context, output, containsExpr.Item);
                    context.Write(output, ")");
                    break;
                case CtorExpr ctorExpr:
                    WriteCtorExpr(context, output, ctorExpr.Interface, ctorExpr.Arguments);
                    break;
                case NondetExpr _:
                case FairNondetExpr _:
                    context.Write(output, $"{CompilationContext.SchedulerVar}.getRandomBool()");
                    break;
                case ChooseExpr chooseExpr:
                    if (chooseExpr.SubExpr == null)
                    {
                        context.Write(output, $"({CompilationContext.SchedulerVar}.getRandomBool())");
                        return;
                    }
                    switch (chooseExpr.SubExpr.Type.Canonicalize())
                    {
                        case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                            context.Write(output, $"{CompilationContext.SchedulerVar}.getRandomInt(");
                            WriteExpr(context, output, chooseExpr.SubExpr);
                            context.Write(output, ")");
                            break;
                        case SequenceType sequenceType:
                            context.Write(output, $"({GetPExplicitType(sequenceType.ElementType)}) {CompilationContext.SchedulerVar}.getRandomEntry(");
                            WriteExpr(context, output, chooseExpr.SubExpr);
                            context.Write(output, ")");
                            break;
                        case SetType setType:
                            context.Write(output, $"({GetPExplicitType(setType.ElementType)}) {CompilationContext.SchedulerVar}.getRandomEntry(");
                            WriteExpr(context, output, chooseExpr.SubExpr);
                            context.Write(output, ")");
                            break;
                        case MapType mapType:
                            context.Write(output, $"({GetPExplicitType(mapType.KeyType)}) {CompilationContext.SchedulerVar}.getRandomEntry(");
                            WriteExpr(context, output, chooseExpr.SubExpr);
                            context.Write(output, ")");
                            break;
                        default:
                            throw new NotImplementedException($"Cannot handle choose on expressions of type {chooseExpr.SubExpr.Type}.");
                    }
                    break;
                case SizeofExpr sizeOfExpr:
                    WriteExpr(context, output, sizeOfExpr.Expr);
                    context.Write(output, ".size()");
                    break;
                case StringExpr stringExpr:
                    var baseString = stringExpr.BaseString;
                    if (stringExpr.Args.Count != 0)
                    {
                        baseString = TransformPrintMessage(baseString);
                    }
                    context.Write(output, $"new { GetPExplicitType(PrimitiveType.String) }(\"{baseString}\"");
                    foreach(var arg in stringExpr.Args)
                    {
                        context.Write(output, ", ");
                        WriteExpr(context, output, arg);
                    }
                    context.Write(output, ")");
                    break;
                case NullLiteralExpr _:
                    context.Write(output, "null");
                    break;
                default:
                    context.Write(output, $"/* Skipping expr '{expr.GetType().Name}' */");
                    break;
            }
        }

        private void WriteCtorExpr(CompilationContext context, StringWriter output, Interface ctorInterface, IReadOnlyList<IPExpr> ctorArguments)
        {
            // TODO: Is it safe to take an interface's name and treat it as if it were a machine's name?
            context.Write(
                output,
                $"{CompilationContext.CurrentMachine}.create(" +
                $"{context.GetNameForDecl(ctorInterface)}.class, ");

            if (ctorArguments.Count == 1)
            {
                Debug.Assert(ctorArguments.Count == 1);
                WriteExpr(context, output, ctorArguments[0]);
                context.Write(output, ", ");
            }
            else if (ctorArguments.Count > 1)
            {
                context.Write(output, "new PTuple (");
                for (var i = 0; i < ctorArguments.Count; i++)
                {
                    WriteExpr(context, output, ctorArguments[i]);
                    if (i != ctorArguments.Count - 1) {
                        context.Write(output, ", ");
                    }
                }
                context.Write(output, "), ");
            }

            context.Write(
                output,
                $"(i) -> new {context.GetNameForDecl(ctorInterface)}(i))");
        }

        // TODO: This is copied from PSharpCodeGenerator.cs.  Should we factor this out into some common location?
        private string BinOpToStr(BinOpType binOpType)
        {
            switch (binOpType)
            {
                case BinOpType.Add:
                    return "add";
                case BinOpType.Sub:
                    return "sub";
                case BinOpType.Mul:
                    return "mul";
                case BinOpType.Div:
                    return "div";
                case BinOpType.Mod:
                    return "mod";
                case BinOpType.Lt:
                    return "lt";
                case BinOpType.Le:
                    return "le";
                case BinOpType.Gt:
                    return "gt";
                case BinOpType.Ge:
                    return "ge";
                case BinOpType.And:
                    return "and";
                case BinOpType.Or:
                    return "or";
                default:
                    throw new ArgumentOutOfRangeException(nameof(binOpType), binOpType, null);
            }
        }

        // TODO: This is copied from PSharpCodeGenerator.cs.  Should we refactor this into some common location?
        private static string UnOpToStr(UnaryOpType operation)
        {
            switch (operation)
            {
                case UnaryOpType.Negate:
                    return "negate";
                case UnaryOpType.Not:
                    return "not";
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        private string GetForeignType(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "PBool";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "PInt";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "PFloat";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return "PString";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                case PermissionType _:
                    return "PMachineValue";
                case ForeignType foreignType:
                    return foreignType.CanonicalRepresentation;
                case SequenceType _:
                    return "PSeq";
                case SetType _:
                    return "PSet";
                case MapType _:
                    return "PMap";
                case NamedTupleType _:
                    return "PNamedTuple";
                case TupleType _:
                    return "PTuple";
                case EnumType _:
                    return "PEnum";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return "void";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "? extends PValue<?>";
                default:
                    return "Object";
            }
        }

        private string GetPExplicitType(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "PBool";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "PInt";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "PFloat";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return "PString";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                case PermissionType _:
                    return "PMachineValue";
                case ForeignType foreignType:
                    return foreignType.CanonicalRepresentation;
                case SequenceType sequenceType:
                    return "PSeq";
                case SetType setType:
                    return "PSet";
                case MapType mapType:
                    return "PMap";
                case NamedTupleType _:
                    return "PNamedTuple";
                case TupleType _:
                    return "PTuple";
                case EnumType enumType:
                    return $"PEnum";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return "PEvent";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return "void";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "PValue<?>";
                default:
                    throw new NotImplementedException($"PExplicit type '{type.OriginalRepresentation}' not supported");
            }
        }

        private string GetDefaultValue(PLanguageType type) {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return $"new {GetPExplicitType(type)}(false)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return $"new {GetPExplicitType(type)}(0)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return $"new {GetPExplicitType(type)}(0.0f)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return $"new {GetPExplicitType(type)}(\"\")";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                case PermissionType _:
                    return "null";
                case ForeignType _:
                    return $"new {GetPExplicitType(type)}()";
                case SequenceType _:
                    return $"new {GetPExplicitType(type)}()";
                case SetType _:
                    return $"new {GetPExplicitType(type)}()";
                case MapType _:
                    return $"new {GetPExplicitType(type)}()";
                case NamedTupleType namedTupleType:
                {
                    var result = "new PNamedTuple(List.of(";
                    for (var i = 0; i < namedTupleType.Fields.Count; i++)
                    {
                        var field = namedTupleType.Fields[i];
                        result += $"\"{field.Name}\"";
                        if (i + 1 != namedTupleType.Fields.Count)
                            result += ", ";
                    }
                    result += "), Arrays.asList(";
                    if (namedTupleType.Fields.Count == 1)
                    {
                        result += "(PValue<?>) ";
                    }
                    for (var i = 0; i < namedTupleType.Fields.Count; i++)
                    {
                        result += $"{GetDefaultValue(namedTupleType.Fields[i].Type)}";
                        if (i + 1 != namedTupleType.Fields.Count)
                            result += ", ";
                    }
                    result += "))";
                    return result;
                }
                case TupleType tupleType:
                {
                    var allFieldDefaults = new List<string>();
                    foreach (var field in tupleType.Types)
                    {
                        var fieldDefault = GetDefaultValue(field);
                        allFieldDefaults.Add(fieldDefault);
                    }
                    return $"new {GetPExplicitType(type)}({string.Join(", ", allFieldDefaults)})";
                }
                case EnumType enumType:
                {
                    int minValue = -1;
                    string minName = "";
                    foreach (var val in  enumType.EnumDecl.Values)
                    {
                        if (minName == "" || val.Value < minValue)
                        {
                            minName = val.Name;
                            minValue = val.Value;
                        }
                    }
                    return $"new {GetPExplicitType(type)}(\"{enumType.OriginalRepresentation}\", \"{minName}\", {minValue})";
                }
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return "null";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "null";
                default:
                    throw new NotImplementedException($"PExplicit type '{type.OriginalRepresentation}' not supported");
            }
        }

        private void WriteSourcePrologue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "package pexplicit.model;");
            context.WriteLine(output);
            // context.WriteLine(output, "import pexplicit.commandline.*;");
            context.WriteLine(output, "import pexplicit.runtime.*;");
            context.WriteLine(output, "import pexplicit.runtime.logger.*;");
            context.WriteLine(output, "import pexplicit.runtime.machine.*;");
            context.WriteLine(output, "import pexplicit.runtime.machine.buffer.*;");
            context.WriteLine(output, "import pexplicit.runtime.machine.eventhandlers.*;");
            context.WriteLine(output, "import pexplicit.runtime.machine.events.*;");
            // context.WriteLine(output, "import pexplicit.runtime.scheduler.*;");
            context.WriteLine(output, "import pexplicit.values.*;");
            context.WriteLine(output, "import pexplicit.utils.*;");
            context.WriteLine(output, "import pexplicit.utils.misc.*;");
            context.WriteLine(output, "import pexplicit.utils.serialize.*;");
            context.WriteLine(output, "import java.util.List;");
            context.WriteLine(output, "import java.util.Arrays;");
            context.WriteLine(output, "import java.util.ArrayList;");
            context.WriteLine(output, "import java.util.Map;");
            context.WriteLine(output, "import java.util.HashMap;");
            context.WriteLine(output, "import java.util.function.Consumer;");
            context.WriteLine(output, "import java.util.function.Function;");
            context.WriteLine(output, "import java.text.MessageFormat;");
            context.WriteLine(output, "import lombok.Generated;");
            context.WriteLine(output);
            context.WriteLine(output, $"public class {context.MainClassName} implements PModel {{");
            context.WriteLine(output);
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "}");
        }
    }
}