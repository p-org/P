using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Plang.Compiler.Backend.Rvm
{
    internal class GeneratorTools
    {
        private CompilationContext Context {get;}

        internal GeneratorTools(CompilationContext context)
        {
            Context = context;
        }

        // Checks the event payload exists and gets the actual value from the Java Optional class.
        internal void InlineEventHandlerArguments(StringWriter output, string argumentType, string argumentName)
        {
            string maybePayload = Context.Names.GetMaybePayloadArgumentName();
            Context.WriteLine(output, $"assert {maybePayload}.isPresent();");
            Context.WriteLine(output, $"{argumentType} {argumentName} = ({argumentType}){maybePayload}.get();");
            Context.WriteLine(output);
        }

        internal delegate void EventHandlerBodyDelegate(StringWriter output);

        // Writes the state entry handler
        internal void WriteTemplateEntryHandler(StringWriter output, EventHandlerBodyDelegate writeBody)
        {
            string entryHandlerName = Context.Names.GetEntryHandlerName();
            string payloadType = Context.Names.GetDefaultPayloadTypeName();
            string maybePayloadName = Context.Names.GetMaybePayloadArgumentName();
            string throwsClause = GetThrowsClause();

            Context.WriteLine(output, "@Override");
            Context.WriteLine(output, $"public void {entryHandlerName}(Optional<{payloadType}> {maybePayloadName}) {throwsClause} {{");
            writeBody(output);
            Context.WriteLine(output, "}");
        }

        // Writes the state exit handler
        internal void WriteTemplateExitHandler(StringWriter output, EventHandlerBodyDelegate writeBody)
        {
            string exitHandlerName =  Context.Names.GetExitHandlerName();
            string throwsClause = GetThrowsClause();

            Context.WriteLine(output, "@Override");
            Context.WriteLine(output, $"public void {exitHandlerName}() {throwsClause} {{");
            writeBody(output);
            Context.WriteLine(output, "}");
        }

        internal void WriteEventHandlerSignature(StringWriter output, PEvent pEvent)
        {
            string throwsClause = GetThrowsClause();
            string handlerName = Context.Names.GetStateEventHandlerName(pEvent);

            Context.Write(output, $"public void {handlerName}(");
            if (!isNullType(pEvent.PayloadType))
            {
                string payloadType = Context.Names.GetJavaTypeName(pEvent.PayloadType);
                string payloadName = Context.Names.GetPayloadArgumentName();
                Context.Write(output, $"{payloadType} {payloadName}");
            }
            Context.Write(output, $") {throwsClause}");
        }

        // Writes the state event handler
        internal void WriteTemplateEventHandler(StringWriter output, PEvent pEvent, EventHandlerBodyDelegate writeBody) {
            string throwsClause = GetThrowsClause();
            string handlerName = Context.Names.GetStateEventHandlerName(pEvent);
            WriteEventHandlerSignature(output, pEvent);
            Context.WriteLine(output, $" {{");
            writeBody(output);
            Context.WriteLine(output, "}");
        }

        internal string GetThrowsClause()
        {
            List<string> exceptions = new List<string>();
            exceptions.Add(Context.Names.GetGotoStmtExceptionName());
            exceptions.Add(Context.Names.GetRaiseStmtExceptionName());
            return $"throws {string.Join(", ", exceptions)}";
        }

        internal bool isNullType(PLanguageType type)
        {
            switch (type)
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return true;
            }
            return false;
        }
    }
}
