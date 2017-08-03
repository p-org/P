using System;
using System.Collections.Generic;
using Microsoft.Formula.API.Generators;
using Microsoft.Pc.Domains;

namespace Microsoft.Pc
{
    public class PProgram
    {
        public List<P_Root.AnyTypeDecl> AnyTypeDecl { get; } = new List<P_Root.AnyTypeDecl>();

        public List<P_Root.TypeDef> TypeDefs { get; } = new List<P_Root.TypeDef>();

        public List<P_Root.DependsOn> DependsOn { get; } = new List<P_Root.DependsOn>();

        public List<P_Root.EnumTypeDef> EnumTypeDefs { get; } = new List<P_Root.EnumTypeDef>();

        public List<P_Root.EventDecl> Events { get; } = new List<P_Root.EventDecl>();

        public List<P_Root.MachineDecl> Machines { get; } = new List<P_Root.MachineDecl>();

        public List<P_Root.StateDecl> States { get; } = new List<P_Root.StateDecl>();

        public List<P_Root.VarDecl> Variables { get; } = new List<P_Root.VarDecl>();

        public List<P_Root.TransDecl> Transitions { get; } = new List<P_Root.TransDecl>();

        public List<P_Root.FunDecl> Functions { get; } = new List<P_Root.FunDecl>();

        public List<P_Root.AnonFunDecl> AnonFunctions { get; } = new List<P_Root.AnonFunDecl>();

        public List<P_Root.DoDecl> Dos { get; } = new List<P_Root.DoDecl>();

        public List<P_Root.Annotation> Annotations { get; } = new List<P_Root.Annotation>();

        public List<P_Root.ObservesDecl> Observes { get; } = new List<P_Root.ObservesDecl>();

        public List<P_Root.InterfaceTypeDef> InterfaceTypeDef { get; } = new List<P_Root.InterfaceTypeDef>();

        public List<P_Root.EventSetDecl> EventSetDecl { get; } = new List<P_Root.EventSetDecl>();

        public List<P_Root.EventSetContains> EventSetContains { get; } = new List<P_Root.EventSetContains>();

        public List<P_Root.MachineExports> MachineExports { get; } = new List<P_Root.MachineExports>();

        public List<P_Root.FunProtoDecl> FunProtoDecls { get; } = new List<P_Root.FunProtoDecl>();

        public List<P_Root.MachineProtoDecl> MachineProtoDecls { get; } = new List<P_Root.MachineProtoDecl>();

        public List<P_Root.MachineReceives> MachineReceives { get; } = new List<P_Root.MachineReceives>();

        public List<P_Root.MachineSends> MachineSends { get; } = new List<P_Root.MachineSends>();

        public List<P_Root.FunProtoCreatesDecl> FunProtoCreates { get; } = new List<P_Root.FunProtoCreatesDecl>();

        public List<P_Root.MachineKind> MachineKinds { get; } = new List<P_Root.MachineKind>();

        public List<P_Root.MachineCard> MachineCards { get; } = new List<P_Root.MachineCard>();

        public List<P_Root.MachineStart> MachineStarts { get; } = new List<P_Root.MachineStart>();

        public IEnumerable<ICSharpTerm> Terms
        {
            get
            {
                foreach (P_Root.AnyTypeDecl at in AnyTypeDecl)
                {
                    yield return at;
                }

                foreach (P_Root.TypeDef td in TypeDefs)
                {
                    yield return td;
                }

                foreach (P_Root.EnumTypeDef etd in EnumTypeDefs)
                {
                    yield return etd;
                }

                foreach (P_Root.EventDecl ed in Events)
                {
                    yield return ed;
                }

                foreach (P_Root.MachineDecl md in Machines)
                {
                    yield return md;
                }

                foreach (P_Root.MachineCard md in MachineCards)
                {
                    yield return md;
                }

                foreach (P_Root.MachineKind md in MachineKinds)
                {
                    yield return md;
                }

                foreach (P_Root.MachineStart md in MachineStarts)
                {
                    yield return md;
                }

                foreach (P_Root.StateDecl s in States)
                {
                    yield return s;
                }

                foreach (P_Root.VarDecl vd in Variables)
                {
                    yield return vd;
                }

                foreach (P_Root.TransDecl td in Transitions)
                {
                    yield return td;
                }

                foreach (P_Root.FunDecl fd in Functions)
                {
                    yield return fd;
                }

                foreach (P_Root.AnonFunDecl afd in AnonFunctions)
                {
                    yield return afd;
                }

                foreach (P_Root.DoDecl di in Dos)
                {
                    yield return di;
                }

                foreach (P_Root.Annotation ann in Annotations)
                {
                    yield return ann;
                }

                foreach (P_Root.ObservesDecl obs in Observes)
                {
                    yield return obs;
                }

                foreach (P_Root.EventSetDecl evset in EventSetDecl)
                {
                    yield return evset;
                }

                foreach (P_Root.EventSetContains ev in EventSetContains)
                {
                    yield return ev;
                }

                foreach (P_Root.InterfaceTypeDef inter in InterfaceTypeDef)
                {
                    yield return inter;
                }

                foreach (P_Root.MachineExports ex in MachineExports)
                {
                    yield return ex;
                }

                foreach (P_Root.FunProtoDecl fp in FunProtoDecls)
                {
                    yield return fp;
                }

                foreach (P_Root.MachineProtoDecl mp in MachineProtoDecls)
                {
                    yield return mp;
                }

                foreach (P_Root.MachineReceives mr in MachineReceives)
                {
                    yield return mr;
                }

                foreach (P_Root.MachineSends ms in MachineSends)
                {
                    yield return ms;
                }

                foreach (P_Root.FunProtoCreatesDecl fp in FunProtoCreates)
                {
                    yield return fp;
                }

                foreach (P_Root.DependsOn d in DependsOn)
                {
                    yield return d;
                }
            }
        }

        public void Add(object item)
        {
            if (item is P_Root.AnyTypeDecl)
            {
                AnyTypeDecl.Add(item as P_Root.AnyTypeDecl);
            }
            else if (item is P_Root.MachineSends)
            {
                MachineSends.Add(item as P_Root.MachineSends);
            }
            else if (item is P_Root.MachineReceives)
            {
                MachineReceives.Add(item as P_Root.MachineReceives);
            }
            else if (item is P_Root.MachineProtoDecl)
            {
                MachineProtoDecls.Add(item as P_Root.MachineProtoDecl);
            }
            else if (item is P_Root.FunProtoDecl)
            {
                FunProtoDecls.Add(item as P_Root.FunProtoDecl);
            }
            else if (item is P_Root.MachineExports)
            {
                MachineExports.Add(item as P_Root.MachineExports);
            }
            else if (item is P_Root.EventSetContains)
            {
                EventSetContains.Add(item as P_Root.EventSetContains);
            }
            else if (item is P_Root.EventSetDecl)
            {
                EventSetDecl.Add(item as P_Root.EventSetDecl);
            }
            else if (item is P_Root.InterfaceTypeDef)
            {
                InterfaceTypeDef.Add(item as P_Root.InterfaceTypeDef);
            }
            else if (item is P_Root.ObservesDecl)
            {
                Observes.Add(item as P_Root.ObservesDecl);
            }
            else if (item is P_Root.Annotation)
            {
                Annotations.Add(item as P_Root.Annotation);
            }
            else if (item is P_Root.DoDecl)
            {
                Dos.Add(item as P_Root.DoDecl);
            }
            else if (item is P_Root.AnonFunDecl)
            {
                AnonFunctions.Add(item as P_Root.AnonFunDecl);
            }
            else if (item is P_Root.FunDecl)
            {
                Functions.Add(item as P_Root.FunDecl);
            }
            else if (item is P_Root.TransDecl)
            {
                Transitions.Add(item as P_Root.TransDecl);
            }
            else if (item is P_Root.VarDecl)
            {
                Variables.Add(item as P_Root.VarDecl);
            }
            else if (item is P_Root.StateDecl)
            {
                States.Add(item as P_Root.StateDecl);
            }
            else if (item is P_Root.MachineDecl)
            {
                Machines.Add(item as P_Root.MachineDecl);
            }
            else if (item is P_Root.MachineCard)
            {
                MachineCards.Add(item as P_Root.MachineCard);
            }
            else if (item is P_Root.MachineKind)
            {
                MachineKinds.Add(item as P_Root.MachineKind);
            }
            else if (item is P_Root.MachineStart)
            {
                MachineStarts.Add(item as P_Root.MachineStart);
            }
            else if (item is P_Root.EventDecl)
            {
                Events.Add(item as P_Root.EventDecl);
            }
            else if (item is P_Root.EnumTypeDef)
            {
                EnumTypeDefs.Add(item as P_Root.EnumTypeDef);
            }
            else if (item is P_Root.TypeDef)
            {
                TypeDefs.Add(item as P_Root.TypeDef);
            }
            else if (item is P_Root.DependsOn)
            {
                DependsOn.Add(item as P_Root.DependsOn);
            }
            else if (item is P_Root.FunProtoCreatesDecl)
            {
                FunProtoCreates.Add(item as P_Root.FunProtoCreatesDecl);
            }
            else
            {
                throw new Exception("Cannot add into the Program : " + item);
            }
        }
    }
}