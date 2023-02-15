/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */

using System.Collections.Generic;
using System.IO;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

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
            var maybePayload = Context.Names.GetMaybePayloadArgumentName();
            Context.WriteLine(output, $"assert {maybePayload}.isPresent();");
            Context.WriteLine(output, $"{argumentType} {argumentName} = ({argumentType}){maybePayload}.get();");
            Context.WriteLine(output);
        }

        internal delegate void EventHandlerBodyDelegate(StringWriter output);

        // Writes the state entry handler
        internal void WriteTemplateEntryHandler(StringWriter output, EventHandlerBodyDelegate writeBody)
        {
            var entryHandlerName = Context.Names.GetEntryHandlerName();
            var payloadType = Context.Names.GetDefaultPayloadTypeName();
            var maybePayloadName = Context.Names.GetMaybePayloadArgumentName();
            var throwsClause = GetThrowsClause();

            Context.WriteLine(output, "@Override");
            Context.WriteLine(output, $"public void {entryHandlerName}(Optional<{payloadType}> {maybePayloadName}) {throwsClause} {{");
            writeBody(output);
            Context.WriteLine(output, "}");
        }

        // Writes the state exit handler
        internal void WriteTemplateExitHandler(StringWriter output, EventHandlerBodyDelegate writeBody)
        {
            var exitHandlerName =  Context.Names.GetExitHandlerName();
            var throwsClause = GetThrowsClause();

            Context.WriteLine(output, "@Override");
            Context.WriteLine(output, $"public void {exitHandlerName}() {throwsClause} {{");
            writeBody(output);
            Context.WriteLine(output, "}");
        }

        internal void WriteEventHandlerSignature(StringWriter output, PEvent pEvent)
        {
            var throwsClause = GetThrowsClause();
            var handlerName = Context.Names.GetStateEventHandlerName(pEvent);

            Context.Write(output, $"public void {handlerName}(");
            if (!isNullType(pEvent.PayloadType))
            {
                var payloadType = Context.Names.GetJavaTypeName(pEvent.PayloadType);
                var payloadName = Context.Names.GetPayloadArgumentName();
                Context.Write(output, $"{payloadType} {payloadName}");
            }
            Context.Write(output, $") {throwsClause}");
        }

        // Writes the state event handler
        internal void WriteTemplateEventHandler(StringWriter output, PEvent pEvent, EventHandlerBodyDelegate writeBody) {
            var throwsClause = GetThrowsClause();
            var handlerName = Context.Names.GetStateEventHandlerName(pEvent);
            WriteEventHandlerSignature(output, pEvent);
            Context.WriteLine(output, $" {{");
            writeBody(output);
            Context.WriteLine(output, "}");
        }

        internal string GetThrowsClause()
        {
            var exceptions = new List<string>();
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
