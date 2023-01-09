/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */

using System.Collections.Generic;
using System.IO;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.Rvm
{
    internal class StateBaseFileGenerator
    {
        private CompilationContext Context {get;}
        private GeneratorTools Tools {get;}

        internal StateBaseFileGenerator(CompilationContext context)
        {
            Context = context;
            Tools = new GeneratorTools(Context);
        }

        public CompiledFile GenerateSource(Scope globalScope)
        {
            var source = new CompiledFile(Context.Names.GetStateBaseFileName());

            WriteSourcePrologue(source.Stream);

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
            WriteStateBaseClass(source.Stream, events);

            return source;
        }

        private void WriteSourcePrologue(StringWriter output) {
            Context.WriteLine(output, "package pcon;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import java.util.Optional;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import p.runtime.*;");
            Context.WriteLine(output, "import p.runtime.exceptions.*;");
            Context.WriteLine(output, "import p.runtime.values.*;");
            Context.WriteLine(output);
        }

        private void WriteStateBaseClass(StringWriter output, List<PEvent> events)
        {
            var stateClassName = Context.Names.GetStateBaseClassName();
            var stateInterfaceName = Context.Names.GetStateInterfaceName();
            var throwsClause = Tools.GetThrowsClause();
            var stateVariable = Context.Names.GetStateVariableName();

            Context.WriteLine(
                output,
                $"public abstract class {stateClassName} implements {stateInterfaceName} {{");
            Tools.WriteTemplateEntryHandler(output, (_) => {});

            Context.WriteLine(output);
            Tools.WriteTemplateExitHandler(output, (_) => {});

            foreach (var pEvent in events)
            {
                var eventName = Context.Names.GetRvmEventName(pEvent);
    
                Context.WriteLine(output);
                Tools.WriteTemplateEventHandler(
                    output,
                    pEvent,
                    (output) => Context.WriteLine(output,
                        $"System.out.println(\"No event handler for state '\" + getName() + \"' and event '{eventName}'.\");"));
            }
            Context.WriteLine(output, "}");
        }
    }
}
