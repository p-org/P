/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;
using System.IO;

namespace Plang.Compiler.Backend.Rvm
{
    // AjFileGenerator generates the template AspectJ file.
    internal class AjFileGenerator
    {
        private CompilationContext Context {get;}

        internal AjFileGenerator(CompilationContext context)
        {
            Context = context;
        }

        public IEnumerable<CompiledFile> GenerateSources(Scope globalScope)
        {
            List<CompiledFile> sources = new List<CompiledFile>();
            List<Machine> specMachines = new List<Machine>();

            foreach (Machine machine in globalScope.Machines)
            {
                if (machine.IsSpec)
                {
                    specMachines.Add(machine);
                }

            }

            sources.Add(WriteMonitors(specMachines));

            return sources;
        }

        private CompiledFile WriteMonitors(IEnumerable<Machine> specMachines) {
            CompiledFile source = new CompiledFile(Context.GetAjFileName());

            WriteSourcePrologue(source.Stream);
            WriteEventHandlerSignature(source.Stream, specMachines);
            WriteSpec(source.Stream);

            return source;
        }

        private void WriteSourcePrologue(StringWriter output) {
            Context.WriteLine(output, "package mop;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import java.lang.ref.*;");
            Context.WriteLine(output, "import org.aspectj.lang.*;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import p.runtime.values.*;");
            Context.WriteLine(output, "// add your own imports.");
            Context.WriteLine(output);

            Context.WriteLine(output, "aspect BaseAspect {");
            Context.WriteLine(output, "pointcut notwithin() :");
            Context.WriteLine(output, "    !within(sun..*) &&");
            Context.WriteLine(output, "    !within(java..*) &&");
            Context.WriteLine(output, "    !within(javax..*) &&");
            Context.WriteLine(output, "    !within(com.sun..*) &&");
            Context.WriteLine(output, "    !within(org.apache.commons..*) &&");
            Context.WriteLine(output, "    !within(org.apache.geronimo..*) &&");
            Context.WriteLine(output, "    !within(net.sf.cglib..*) &&");
            Context.WriteLine(output, "    !within(mop..*) &&");
            Context.WriteLine(output, "    !within(javamoprt..*) &&");
            Context.WriteLine(output, "    !within(rvmonitorrt..*) &&");
            Context.WriteLine(output, "    !within(com.runtimeverification..*);");
            Context.WriteLine(output, "}");
            Context.WriteLine(output);
        }

        private void WriteSpec(StringWriter output)
        {
            string declName = Context.Names.GetAspectClassName();
            Context.WriteLine(output, $"public aspect { declName } implements com.runtimeverification.rvmonitor.java.rt.RVMObject {{");
            Context.WriteLine(output, $"{declName}() {{ }}");

            Context.WriteLine(output);
            Context.WriteLine(output, "pointcut MOP_CommonPointCut() : !within(com.runtimeverification.rvmonitor.java.rt.RVMObject+) && !adviceexecution() && BaseAspect.notwithin();");

            Context.WriteLine(output);
            Context.WriteLine(output, "// Implement your code here.");

            Context.WriteLine(output);
            Context.WriteLine(output, "}");
        }

        private void WriteEventHandlerSignature(StringWriter output, IEnumerable<Machine> specMachines)
        {
            Context.WriteLine(output, "// Signatures of all the events that need dispatching.");

            foreach (Machine machine in specMachines)
            {

                // We iterate the events that the spec observes instead of all the declared events
                // because monitors only interact with the external systems through the events that they observe.
                foreach (PEvent e in machine.Observes.Events)
                {
                    string monitorClassName = Context.Names.GetJavaRuntimeMonitorName(machine);
                    string handlerName = Context.Names.GetJavaEventHandlerName(machine, e);
                    string payloadType = Context.Names.GetJavaTypeName(e.PayloadType, true);
                    string handlerSignature = $"{ monitorClassName }.{ handlerName }({payloadType})";
                    Context.WriteLine(output, $"// {handlerSignature}");
                }
            }

            Context.WriteLine(output);
        }

    }
}
