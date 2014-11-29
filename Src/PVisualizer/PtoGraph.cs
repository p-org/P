using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Pc;
using Microsoft.Pc.Domains;
using Microsoft.Formula.API;

namespace Microsoft.PVisualizer
{
    class PtoGraph
    {
        private static string QualifiedNameToString(P_Root.QualifiedName qualifiedName)
        {
            string name = (qualifiedName.name as P_Root.StringCnst).Value;
            if (qualifiedName.qualifier is P_Root.UserCnst)
                return name;
            return QualifiedNameToString((P_Root.QualifiedName)qualifiedName.qualifier) + "." + name;
        }

        public static Graph GenerateGraph(PProgram program)
        {
            Graph graph = new Graph();
            Dictionary<string, Subgraph> machineNameToSubgraph = new Dictionary<string, Subgraph>();
            Dictionary<string, string> machineNameToStartStateName = new Dictionary<string, string>();
            foreach (var machine in program.Machines)
            {
                var machineName = (machine.name as P_Root.StringCnst).Value;
                var subgraph = new Subgraph(machineName);
                graph.RootSubgraph.AddSubgraph(subgraph);
                machineNameToSubgraph[machineName] = subgraph;
                machineNameToStartStateName[machineName] = QualifiedNameToString(machine.start as P_Root.QualifiedName);
            }
            
            foreach (var state in program.States)
            {
                var stateName = QualifiedNameToString(state.name as P_Root.QualifiedName);
                Node node = new Node(stateName);
                string ownerName = ((state.owner as P_Root.MachineDecl).name as P_Root.StringCnst).Value;
                machineNameToSubgraph[ownerName].AddNode(node);
                graph.AddNode(node);
                if (machineNameToStartStateName[ownerName] == stateName)
                {
                    node.Attr.LineWidth *= 2;
                }
            }
            
            foreach (var trans in program.Transitions)
            {
                var srcStateDecl = (P_Root.StateDecl)trans.src;
                var srcStateName = QualifiedNameToString(srcStateDecl.name as P_Root.QualifiedName);
                var dstStateName = QualifiedNameToString(trans.dst as P_Root.QualifiedName);
                string trigger;
                var constant = trans.trig as P_Root.UserCnst;
                if (constant != null)
                {
                    var constantValue = (P_Root.UserCnstKind)constant.Value;
                    if (constantValue == P_Root.UserCnstKind.DEFAULT)
                    {
                        trigger = "default";
                    }
                    else
                    {
                        trigger = "halt";
                    }
                }
                else
                {
                    trigger = (trans.trig as P_Root.StringCnst).Value;
                }
                var edge = graph.AddEdge(srcStateName, trigger, dstStateName);
                if (trans.action is P_Root.UserCnst)
                {
                    edge.Attr.LineWidth *= 2;
                }
            }
            return graph;
        }
    }
}
