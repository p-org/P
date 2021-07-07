/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;
using System.IO;

namespace Plang.Compiler.Backend.Rvm
{
    // AjFileGenerator generates the base AspectJ file that handles MonitorOn annotations and common pointcuts
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
            foreach (Machine machine in specMachines)
            {
                WriteParentClass(source.Stream, machine);
            }
            WriteBaseAspect(source.Stream, specMachines);

            return source;
        }

        private void WriteSourcePrologue(StringWriter output) {
            Context.WriteLine(output, "package pcon;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import java.lang.ref.*;");
            Context.WriteLine(output, "import org.aspectj.lang.*;");
            Context.WriteLine(output, "import java.util.Arrays;");
            Context.WriteLine(output);

            Context.WriteLine(output, "import p.runtime.values.*;");
            Context.WriteLine(output, "import com.runtimeverification.rvmonitor.java.rt.RVMObject;");
            Context.WriteLine(output, "import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;");
            Context.WriteLine(output, "import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorsOn;");
            Context.WriteLine(output);

        }

        private void WriteParentClass(StringWriter output, Machine specMachine)
        {
            string declName = Context.Names.GetAspectClassName();
            string parentClass = Context.Names.GetParentClassName(specMachine);
            string monitorClassName = Context.Names.GetJavaRuntimeMonitorName(specMachine);

            Context.WriteLine(output, $"class { parentClass } implements RVMObject {{");
            Context.WriteLine(output, $"public { parentClass }() {{ }}");
            Context.WriteLine(output);

            Context.WriteLine(output, $"void config_suppressException() {{");
            Context.WriteLine(output, $"{ monitorClassName }.suppress();");
            Context.WriteLine(output, $"}}");
            Context.WriteLine(output);

            foreach (PEvent e in specMachine.Observes.Events)
            {
                string handlerName = Context.Names.GetJavaEventHandlerName(specMachine, e);
                string payloadType = Context.Names.GetJavaTypeName(e.PayloadType, true);
                string eventAlias = Context.Names.GetEventAlias(specMachine, e);
                string parameterTypeAndName = payloadType.Length > 0 ? $"{ payloadType } v" : "";
                string parameterName = payloadType.Length > 0 ? "v" : "";

                Context.WriteLine(output, $"void { eventAlias }({ parameterTypeAndName }){{");
                Context.WriteLine(output, $"{ monitorClassName }.{ handlerName }({ parameterName });");
                Context.WriteLine(output, "}");
            }
            Context.WriteLine(output, "}");
            Context.WriteLine(output);
        }

        private void WriteBaseAspect(StringWriter output, IEnumerable<Machine> specMachines)
        {
            string declName = Context.Names.GetAspectClassName();

            Context.WriteLine(output, $"public aspect { declName } implements RVMObject {{");
            Context.WriteLine(output, $"{declName}() {{ }}");
            Context.WriteLine(output);

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
            Context.WriteLine(output, "pointcut CommonPointCut() :");
            Context.WriteLine(output, "    !within(com.runtimeverification.rvmonitor.java.rt.RVMObject+) &&");
            Context.WriteLine(output, "    !adviceexecution() &&");
            Context.WriteLine(output, "    notwithin();");

            foreach (Machine machine in specMachines)
            {
                string specName = Context.Names.GetRvmSpecName(machine);
                string monitorOn = Context.Names.GetPointCutNameForMonitorOn(machine);
                string monitorsOn = Context.Names.GetPointCutNameForMonitorsOn(machine);
                string specPointcut = Context.Names.GetPointCutNameForEnabledTestcases(machine);
                string aspectNameForSpec = Context.Names.GetAspectInputTemplateClassName(machine);
                string parentClass = Context.Names.GetParentClassName(machine);

                Context.WriteLine(output);
                Context.WriteLine(output, $"declare parents: { aspectNameForSpec } extends { parentClass };");
                Context.WriteLine(output, $"pointcut { monitorOn }(MonitorOn monitorOn) : cflow(@annotation(monitorOn)) && if(monitorOn.value().equalsIgnoreCase(\"{specName}\"));");
                Context.WriteLine(output, $"pointcut { monitorsOn }(MonitorsOn monitorsOn) : cflow(@annotation(monitorsOn)) && if(Arrays.stream(monitorsOn.value()).anyMatch(t -> t.value().equalsIgnoreCase(\"{specName}\")));");
                Context.WriteLine(output, $"pointcut { specPointcut }() : ({ monitorOn }(MonitorOn) || { monitorsOn }(MonitorsOn)) && CommonPointCut();");
            }

            Context.WriteLine(output);
            Context.WriteLine(output, "before(MonitorOn monitorOn) : @annotation(monitorOn) {");
            Context.WriteLine(output, "System.out.println(\"[Start] monitoring \" + thisJoinPoint.getSignature().getName() + \" against \" + monitorOn.value());");
            foreach (Machine machine in specMachines)
            {
                string monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                string specName = Context.Names.GetRvmSpecName(machine);

                Context.WriteLine(output, $"if (monitorOn.value().equalsIgnoreCase(\"{ specName }\")) {{");
                Context.WriteLine(output, $"{ monitorName }.resetMonitor();");
                Context.WriteLine(output, $"{ monitorName }.enable();");
                Context.WriteLine(output, "}");
            }
            Context.WriteLine(output, "}");
            Context.WriteLine(output, "before(MonitorsOn monitorsOn) : @annotation(monitorsOn) {");
            Context.WriteLine(output, "String monitors = String.join(\",\", Arrays.stream(monitorsOn.value()).map(t -> (String)t.value()).toArray(String[]::new));");
            Context.WriteLine(output, "System.out.println(\"[Start] monitoring \" + thisJoinPoint.getSignature().getName() + \" against \" + monitors);");
            Context.WriteLine(output, "for (MonitorOn monitorOn : monitorsOn.value()) {");
            foreach (Machine machine in specMachines)
            {
                string monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                string specName = Context.Names.GetRvmSpecName(machine);

                Context.WriteLine(output, $"if (monitorOn.value().equalsIgnoreCase(\"{ specName }\")) {{");
                Context.WriteLine(output, $"{ monitorName }.resetMonitor();");
                Context.WriteLine(output, $"{ monitorName }.enable();");
                Context.WriteLine(output, "}");
            }
            Context.WriteLine(output, "}");
            Context.WriteLine(output, "}");
            Context.WriteLine(output);

            Context.WriteLine(output, "after(MonitorOn monitorOn) : @annotation(monitorOn) {");
            Context.WriteLine(output, "System.out.println(\"[End] monitoring \" + thisJoinPoint.getSignature().getName() + \" against \" + monitorOn.value());");
            foreach (Machine machine in specMachines)
            {
                string monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                string specName = Context.Names.GetRvmSpecName(machine);

                Context.WriteLine(output, $"if (monitorOn.value().equalsIgnoreCase(\"{ specName }\")) {{");
                Context.WriteLine(output, $"{ monitorName }.disable();");
                Context.WriteLine(output, "}");
            }
            Context.WriteLine(output, "}");
            Context.WriteLine(output, "after(MonitorsOn monitorsOn) : @annotation(monitorsOn) {");
            Context.WriteLine(output, "String monitors = String.join(\",\", Arrays.stream(monitorsOn.value()).map(t -> (String)t.value()).toArray(String[]::new));");
            Context.WriteLine(output, "System.out.println(\"[End] monitoring \" + thisJoinPoint.getSignature().getName() + \" against \" + monitors);");
            Context.WriteLine(output, "for (MonitorOn monitorOn : monitorsOn.value()) {");
            foreach (Machine machine in specMachines)
            {
                string monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                string specName = Context.Names.GetRvmSpecName(machine);

                Context.WriteLine(output, $"if (monitorOn.value().equalsIgnoreCase(\"{ specName }\")) {{");
                Context.WriteLine(output, $"{ monitorName }.disable();");
                Context.WriteLine(output, "}");
            }
            Context.WriteLine(output, "}");
            Context.WriteLine(output, "}");

            Context.WriteLine(output, "}");
        }
    }
}
