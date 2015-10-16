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
        
        public List<P_Root.ObservesDecl> Observes
        {
            get;
            private set;
        }

        public List<P_Root.InterfaceEventDecl> InterfaceEvents
        {
            get;
            private set;
        }

        public List<P_Root.MachineImpsInterfaceDecl> MachineImpsInterface
        {
            get;
            private set;
        }

        public List<P_Root.ModuleDecl> ModuleDecl
        {
            get;
            private set;
        }

        public List<P_Root.ModuleSendsDecl> ModuleSendsDecl
        {
            get;
            private set;
        }

        public List<P_Root.ModulePrivateDecl> ModulePrivateDecl
        {
            get;
            private set;
        }

        public List<P_Root.ModuleCreatesDecl> ModuleCreatesDecl
        {
            get;
            private set;
        }

        public List<P_Root.RefinesTestDecl> RefinesTestDecl
        {
            get;
            private set;
        }

        public List<P_Root.MonitorsTestDecl> MonitorsTestDecl
        {
            get;
            private set;
        }

        public List<P_Root.NoFailureTestDecl> NoFailureTestDecl
        {
            get;
            private set;
        }

        public List<P_Root.ImplementationModules> ImplementationModules
        {
            get;
            private set;
        }

        public List<P_Root.SpecificationModules> SpecificationModules
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

        public IEnumerable<ICSharpTerm> Terms
        {
            get
            {
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

                foreach (var inter in InterfaceEvents)
                {
                    yield return inter;
                }

                foreach (var inter in MachineImpsInterface)
                {
                    yield return inter;
                }
                foreach(var m in ModuleDecl)
                {
                    yield return m;
                }
                foreach(var sm in ModuleSendsDecl)
                {
                    yield return sm;
                }
                foreach(var rm in ModulePrivateDecl)
                {
                    yield return rm;
                }
                foreach(var cm in ModuleCreatesDecl)
                {
                    yield return cm;
                }
                foreach(var rT in RefinesTestDecl)
                {
                    yield return rT;
                }
                foreach(var mt in MonitorsTestDecl)
                {
                    yield return mt;
                }
                foreach(var nft in NoFailureTestDecl)
                {
                    yield return nft;
                }
                foreach(var imp in ImplementationModules)
                {
                    yield return imp;
                }
                foreach(var spec in SpecificationModules)
                {
                    yield return spec;
                }
            }
        }

        public PProgram()
        {
            Events = new List<P_Root.EventDecl>();
            Machines = new List<P_Root.MachineDecl>();
            States = new List<P_Root.StateDecl>();
            Variables = new List<P_Root.VarDecl>();
            Transitions = new List<P_Root.TransDecl>();
            Functions = new List<P_Root.FunDecl>();
            Observes = new List<P_Root.ObservesDecl>();
            InterfaceEvents = new List<P_Root.InterfaceEventDecl>();
            MachineImpsInterface = new List<P_Root.MachineImpsInterfaceDecl>();
            ModuleDecl = new List<P_Root.ModuleDecl>();
            ModuleCreatesDecl = new List<P_Root.ModuleCreatesDecl>();
            ModulePrivateDecl = new List<P_Root.ModulePrivateDecl>();
            ModuleSendsDecl = new List<P_Root.ModuleSendsDecl>();
            ImplementationModules = new List<P_Root.ImplementationModules>();
            SpecificationModules = new List<P_Root.SpecificationModules>();
            RefinesTestDecl = new List<P_Root.RefinesTestDecl>();
            NoFailureTestDecl = new List<P_Root.NoFailureTestDecl>();
            MonitorsTestDecl = new List<P_Root.MonitorsTestDecl>();
            AnonFunctions = new List<P_Root.AnonFunDecl>();
            Dos = new List<P_Root.DoDecl>();
            Annotations = new List<P_Root.Annotation>();
        }
    }
}
