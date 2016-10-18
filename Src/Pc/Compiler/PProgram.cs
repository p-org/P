namespace Microsoft.Pc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Domains;
    using Microsoft.Formula.API.Generators;

    public class PProgram
    {
        public List<P_Root.TypeDef> TypeDefs
        {
            get;
            private set;
        }

        public List<P_Root.EnumTypeDef> EnumTypeDefs
        {
            get;
            private set;
        }

        public List<P_Root.ModelType> ModelTypes
        {
            get;
            private set;
        }

        public List<P_Root.EventDecl> Events
        {
            get;
            private set;
        }

        public List<P_Root.MachineDecl> Machines
        {
            get;
            private set;
        }
        
        public List<P_Root.StateDecl> States
        {
            get;
            private set;
        }

        public List<P_Root.VarDecl> Variables
        {
            get;
            private set;
        }

        public List<P_Root.TransDecl> Transitions
        {
            get;
            private set;
        }

        public List<P_Root.FunDecl> Functions
        {
            get;
            private set;
        }

        public List<P_Root.AnonFunDecl> AnonFunctions
        {
            get;
            private set;
        }

        public List<P_Root.DoDecl> Dos
        {
            get;
            private set;
        }

        public List<P_Root.Annotation> Annotations
        {
            get;
            private set;
        }

        public List<P_Root.ObservesDecl> Observes
        {
            get;
            private set;
        }

        public List<P_Root.InterfaceTypeDecl> InterfaceTypeDecl
        {
            get;
            private set;
        }

        public List<P_Root.EventSetDecl> EventSetDecl
        {
            get;
            private set;
        }

        public List<P_Root.EventSetContains> EventSetContains
        {
            get;
            private set;
        }

        public List<P_Root.MachineExports> MachineExports
        {
            get;
            private set;
        }

        public List<P_Root.FunProtoDecl> FunProtoDecls
        {
            get;
            private set;
        }

        public List<P_Root.MachineProtoDecl> MachineProtoDecls
        {
            get;
            private set;
        }

        public List<P_Root.MachineReceives> MachineReceives
        {
            get;
            private set;
        }

        public List<P_Root.MachineSends> MachineSends
        {
            get;
            private set;
        }

        public List<P_Root.MachineCreates> MachineCreates
        {
            get;
            private set;
        }

        public IEnumerable<ICSharpTerm> Terms
        {
            get
            {
                foreach (var td in TypeDefs)
                {
                    yield return td;
                }

                foreach (var etd in EnumTypeDefs)
                {
                    yield return etd;
                }

                foreach (var md in ModelTypes)
                {
                    yield return md;
                }
                
                foreach (var ed in Events)
                {
                    yield return ed;
                }

                foreach (var md in Machines)
                {
                    yield return md;
                }

                foreach (var s in States)
                {
                    yield return s;
                }

                foreach (var vd in Variables)
                {
                    yield return vd;
                }

                foreach (var td in Transitions)
                {
                    yield return td;
                }

                foreach (var fd in Functions)
                {
                    yield return fd;
                }

                foreach (var afd in AnonFunctions)
                {
                    yield return afd;
                }

                foreach (var di in Dos)
                {
                    yield return di;
                }

                foreach (var ann in Annotations)
                {
                    yield return ann;
                }

                foreach (var obs in Observes)
                {
                    yield return obs;
                }

                foreach (var evset in EventSetDecl)
                {
                    yield return evset;
                }

                foreach (var ev in EventSetContains)
                {
                    yield return ev;
                }

                foreach (var inter in InterfaceTypeDecl)
                {
                    yield return inter;
                }

                foreach (var ex in MachineExports)
                {
                    yield return ex;
                }

                foreach (var fp in FunProtoDecls)
                {
                    yield return fp;
                }

                foreach (var mp in MachineProtoDecls)
                {
                    yield return mp;

                }

                foreach (var mr in MachineReceives)
                {
                    yield return mr;
                }

                foreach (var ms in MachineSends)
                {
                    yield return ms;
                }

                foreach (var mc in MachineCreates)
                {
                    yield return mc;
                }
            }
        }

        public PProgram()
        {
            TypeDefs = new List<P_Root.TypeDef>();
            EnumTypeDefs = new List<P_Root.EnumTypeDef>();
            ModelTypes = new List<P_Root.ModelType>();
            Events = new List<P_Root.EventDecl>();
            Machines = new List<P_Root.MachineDecl>();
            States = new List<P_Root.StateDecl>();
            Variables = new List<P_Root.VarDecl>();
            Transitions = new List<P_Root.TransDecl>();
            Functions = new List<P_Root.FunDecl>();
            AnonFunctions = new List<P_Root.AnonFunDecl>();
            Dos = new List<P_Root.DoDecl>();
            Annotations = new List<P_Root.Annotation>();
            Observes = new List<P_Root.ObservesDecl>();
            MachineExports = new List<P_Root.MachineExports>();
            InterfaceTypeDecl = new List<P_Root.InterfaceTypeDecl>();
            EventSetDecl = new List<P_Root.EventSetDecl>();
            EventSetContains = new List<P_Root.EventSetContains>();
            FunProtoDecls = new List<P_Root.FunProtoDecl>();
            MachineProtoDecls = new List<P_Root.MachineProtoDecl>();
            MachineSends = new List<P_Root.MachineSends>();
            MachineReceives = new List<P_Root.MachineReceives>();
            MachineCreates = new List<P_Root.MachineCreates>();
        }
    }
}
