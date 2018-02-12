using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using P.Runtime;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;

namespace P.Tester
{
    public class Coverage
    {
        Microsoft.PSharp.TestingServices.Coverage.CoverageInfo CoverageInfo;

        public Coverage()
        {
            CoverageInfo = new Microsoft.PSharp.TestingServices.Coverage.CoverageInfo();
        }

        public void DeclareMachine(PrtImplMachine machine)
        {
            var machineName = machine.GetType().Name;
            if(CoverageInfo.MachinesToStates.ContainsKey(machineName))
            {
                return;
            }

            // enumerate states
            var type = machine.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach(var field in fields)
            {
                if (!field.FieldType.IsSubclassOf(typeof(PrtState)))
                    continue;
                var state = field.GetValue(machine) as PrtState;

                CoverageInfo.DeclareMachineState(machineName, state.name);
                
                foreach(var tup in state.transitions)
                {
                    var evt = tup.Key as PrtEventValue;
                    if (evt == null) continue;
                    var evtName = evt.evt.name;

                    CoverageInfo.DeclareStateEvent(machineName, state.name, evtName);
                }


            }
                
        }

        public void ReportDequeue(PrtImplMachine machine, string evName, string senderMachineName, string senderMachineStateName)
        {
            CoverageInfo.AddTransition(senderMachineName, senderMachineStateName, evName, machine.Name, machine.CurrentState.name);
        }

        public void ReportStateTransition(PrtImplMachine machine, PrtState from, PrtState to, string reason)
        {
            CoverageInfo.AddTransition(machine.Name, from.name, reason, machine.Name, to.name);
        }

        public void Dump(string fileNamePrefix)
        {
            var reporter = new Microsoft.PSharp.TestingServices.Coverage.ActivityCoverageReporter(CoverageInfo);
            reporter.EmitCoverageReport(fileNamePrefix + ".txt");
            reporter.EmitVisualizationGraph(fileNamePrefix + ".dgml");
        }

    }
}
