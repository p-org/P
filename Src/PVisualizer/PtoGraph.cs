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
            Dictionary<string, Subgraph> machineNameToSubgraph = new Dictionary<string, Subgraph>();
            Graph graph = new Graph();
            foreach (var state in program.States)
            {
                Node node = new Node(QualifiedNameToString(state.name as P_Root.QualifiedName));
                string ownerName = ((state.owner as P_Root.MachineDecl).name as P_Root.StringCnst).Value;
                if (!machineNameToSubgraph.ContainsKey(ownerName))
                {
                    machineNameToSubgraph[ownerName] = new Subgraph(ownerName);
                }
                machineNameToSubgraph[ownerName].AddNode(node);
                graph.AddNode(node);
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
                var isPush = trans.action is P_Root.UserCnst;
                graph.AddEdge(srcStateName, trigger, dstStateName);
            }
            return graph;
        }
    }
}
