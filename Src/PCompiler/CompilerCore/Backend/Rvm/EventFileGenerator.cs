/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */

using System.Collections.Generic;
using System.IO;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.Rvm
{
    // EventFileGenerator generates the Events class which defines all the events
    // in the P specification as its nested classes.
    internal class EventFileGenerator
    {
        private CompilationContext Context {get;}
        private GeneratorTools Tools {get;}

        internal EventFileGenerator(CompilationContext context)
        {
            Context = context;
            Tools = new GeneratorTools(Context);
        }

        public IEnumerable<CompiledFile> GenerateSources(Scope globalScope)
        {
            var sources = new List<CompiledFile>();

            var events = new List<PEvent>();
            foreach (var decl in globalScope.AllDecls)
            {
                switch (decl)
                {
                    case PEvent pEvent:
                        if (!pEvent.IsBuiltIn)
                        {
                            events.Add(pEvent);
                        }
                        break;
                    default:
                        // Just ignore.
                        break;
                }
            }
            sources.Add(WriteEvents(events));

            return sources;
        }

        CompiledFile WriteEvents(List<PEvent> events)
        {
            var source = new CompiledFile(Context.Names.GetEventFileName());

            WriteSourcePrologue(source.Stream);

            var separator = new BeforeSeparator(() => Context.WriteLine(source.Stream));
            foreach (var pEvent in events)
            {
                separator.beforeElement();
                WriteEventClass(source.Stream, pEvent);
            }

            WriteSourceEpilogue(source.Stream);

            return source;
        }

        private void WriteSourcePrologue(StringWriter output) {
            var eventsClass = Context.Names.GetEventsClassName();
            Context.WriteLine(output, "package pcon;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import java.util.Optional;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import p.runtime.*;");
            Context.WriteLine(output, "import p.runtime.exceptions.*;");
            Context.WriteLine(output, "import p.runtime.values.*;");
            Context.WriteLine(output);
            Context.WriteLine(output, $"class {eventsClass} {{");
        }

        private void WriteSourceEpilogue(StringWriter output) {
            Context.WriteLine(output, "}");
        }

        // Each event class implements the IEvent interface and overrides the handle function.
        // The handle function calls the correspondning event handler of the current state.
        private void WriteEventClass(StringWriter output, PEvent pEvent)
        {
            var eventClassName = Context.Names.GetUnqualifiedEventClassName(pEvent);
            var eventInterfaceName = Context.Names.GetEventInterfaceName();
            var stateInterfaceName = Context.Names.GetStateInterfaceName();
            var stateClassName = Context.Names.GetStateBaseClassName();
            Context.WriteLine(output, $"static class {eventClassName} implements {eventInterfaceName} {{");
            // Override handle function
            var handleFunctionName = Context.Names.GetEventHandleFunctionName();
            var payloadType = Context.Names.GetDefaultPayloadTypeName();
            var maybePayloadName = Context.Names.GetMaybePayloadArgumentName();
            var throwsClause = Tools.GetThrowsClause();
            var stateVariable = Context.Names.GetStateVariableName();

            Context.WriteLine(output, "@Override");
            Context.WriteLine(output, $"public void {handleFunctionName}({stateInterfaceName} {stateVariable}, Optional<{payloadType}> {maybePayloadName}) {throwsClause} {{");
            var payloadName = "";
            if (!Tools.isNullType(pEvent.PayloadType))
            {
                var actualPayloadType = Context.Names.GetJavaTypeName(pEvent.PayloadType);
                payloadName = Context.Names.GetPayloadArgumentName();
                Tools.InlineEventHandlerArguments(output, actualPayloadType, payloadName);
            }
            var handlerName = Context.Names.GetStateEventHandlerName(pEvent);
            Context.WriteLine(output, $"(({stateClassName}){stateVariable}).{handlerName}({payloadName});");
            Context.WriteLine(output, "}");
            Context.WriteLine(output, "}");
        }
    }
}
