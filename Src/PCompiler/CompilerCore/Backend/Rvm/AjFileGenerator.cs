/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */

using System.Collections.Generic;
using System.IO;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;

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
            var sources = new List<CompiledFile>();
            var specMachines = new List<Machine>();

            foreach (var machine in globalScope.Machines)
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
            var source = new CompiledFile(Context.GetAjFileName());

            WriteSourcePrologue(source.Stream);
            foreach (var machine in specMachines)
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
            var declName = Context.Names.GetAspectClassName();
            var parentClass = Context.Names.GetParentClassName(specMachine);
            var monitorClassName = Context.Names.GetJavaRuntimeMonitorName(specMachine);

            Context.WriteLine(output, $"class { parentClass } implements RVMObject {{");
            Context.WriteLine(output, $"public { parentClass }() {{ }}");
            Context.WriteLine(output);

            Context.WriteLine(output, $"void config_suppressException() {{");
            Context.WriteLine(output, $"{ monitorClassName }.suppress();");
            Context.WriteLine(output, $"}}");
            Context.WriteLine(output);

            foreach (var e in specMachine.Observes.Events)
            {
                var handlerName = Context.Names.GetJavaEventHandlerName(specMachine, e);
                var payloadType = Context.Names.GetJavaTypeName(e.PayloadType, true);
                var eventAlias = Context.Names.GetEventAlias(specMachine, e);
                var parameterTypeAndName = payloadType.Length > 0 ? $"{ payloadType } v" : "";
                var parameterName = payloadType.Length > 0 ? "v" : "";

                Context.WriteLine(output, $"void { eventAlias }({ parameterTypeAndName }){{");
                Context.WriteLine(output, $"{ monitorClassName }.{ handlerName }({ parameterName });");
                Context.WriteLine(output, "}");
            }
            Context.WriteLine(output, "}");
            Context.WriteLine(output);
        }

        private void WriteBaseAspect(StringWriter output, IEnumerable<Machine> specMachines)
        {
            var declName = Context.Names.GetAspectClassName();

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

            foreach (var machine in specMachines)
            {
                var specName = Context.Names.GetRvmSpecName(machine);
                var monitorOn = Context.Names.GetPointCutNameForMonitorOn(machine);
                var monitorsOn = Context.Names.GetPointCutNameForMonitorsOn(machine);
                var specPointcut = Context.Names.GetPointCutNameForEnabledTestcases(machine);
                var aspectNameForSpec = Context.Names.GetAspectInputTemplateClassName(machine);
                var parentClass = Context.Names.GetParentClassName(machine);

                Context.WriteLine(output);
                Context.WriteLine(output, $"declare parents: { aspectNameForSpec } extends { parentClass };");
                Context.WriteLine(output, $"pointcut { monitorOn }(MonitorOn monitorOn) : cflow(@annotation(monitorOn)) && if(monitorOn.value().equalsIgnoreCase(\"{specName}\"));");
                Context.WriteLine(output, $"pointcut { monitorsOn }(MonitorsOn monitorsOn) : cflow(@annotation(monitorsOn)) && if(Arrays.stream(monitorsOn.value()).anyMatch(t -> t.value().equalsIgnoreCase(\"{specName}\")));");
                Context.WriteLine(output, $"pointcut { specPointcut }() : ({ monitorOn }(MonitorOn) || { monitorsOn }(MonitorsOn)) && CommonPointCut();");
            }

            Context.WriteLine(output);
            Context.WriteLine(output, "before(MonitorOn monitorOn) : @annotation(monitorOn) {");
            Context.WriteLine(output, "System.out.println(\"[Start] monitoring \" + thisJoinPoint.getSignature().getName() + \" against \" + monitorOn.value());");
            foreach (var machine in specMachines)
            {
                var monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                var specName = Context.Names.GetRvmSpecName(machine);

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
            foreach (var machine in specMachines)
            {
                var monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                var specName = Context.Names.GetRvmSpecName(machine);

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
            foreach (var machine in specMachines)
            {
                var monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                var specName = Context.Names.GetRvmSpecName(machine);

                Context.WriteLine(output, $"if (monitorOn.value().equalsIgnoreCase(\"{ specName }\")) {{");
                Context.WriteLine(output, $"{ monitorName }.disable();");
                Context.WriteLine(output, "}");
            }
            Context.WriteLine(output, "}");
            Context.WriteLine(output, "after(MonitorsOn monitorsOn) : @annotation(monitorsOn) {");
            Context.WriteLine(output, "String monitors = String.join(\",\", Arrays.stream(monitorsOn.value()).map(t -> (String)t.value()).toArray(String[]::new));");
            Context.WriteLine(output, "System.out.println(\"[End] monitoring \" + thisJoinPoint.getSignature().getName() + \" against \" + monitors);");
            Context.WriteLine(output, "for (MonitorOn monitorOn : monitorsOn.value()) {");
            foreach (var machine in specMachines)
            {
                var monitorName = Context.Names.GetJavaRuntimeMonitorName(machine);
                var specName = Context.Names.GetRvmSpecName(machine);

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
