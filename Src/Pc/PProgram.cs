namespace Microsoft.Pc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Domains;
    using Microsoft.Formula.API.Generators;

    internal class PProgram
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

        public List<P_Root.ActionDecl> Actions
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

        public List<P_Root.SubMachDecl> SubMachines
        {
            get;
            private set;
        }

        public List<P_Root.EventSetDecl> EventSets
        {
            get;
            private set;
        }

        public List<P_Root.Install> Installers
        {
            get;
            private set;
        }

        public List<P_Root.Stable> StableMarks
        {
            get;
            private set;
        }

        public P_Root.MainDecl MainDecl
        {
            get;
            set;
        }

        public List<P_Root.InSubMach> SubMachineMembers
        {
            get;
            private set;
        }

        public List<P_Root.InEventSet> EventSetMembers
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

                foreach (var a in Actions)
                {
                    yield return a;
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

                foreach (var smd in SubMachines)
                {
                    yield return smd;
                }

                foreach (var esd in EventSets)
                {
                    yield return esd;
                }

                foreach (var inst in Installers)
                {
                    yield return inst;
                }

                foreach (var stb in StableMarks)
                {
                    yield return stb;
                }

                if (MainDecl != null)
                {
                    yield return MainDecl;
                }

                foreach (var smm in SubMachineMembers)
                {
                    yield return smm;
                }

                foreach (var esm in EventSetMembers)
                {
                    yield return esm;
                }

                foreach (var ann in Annotations)
                {
                    yield return ann;
                }
            }
        }

        public PProgram()
        {
            Events = new List<P_Root.EventDecl>();
            Machines = new List<P_Root.MachineDecl>();
            Actions = new List<P_Root.ActionDecl>();
            States = new List<P_Root.StateDecl>();
            Variables = new List<P_Root.VarDecl>();
            Transitions = new List<P_Root.TransDecl>();
            Functions = new List<P_Root.FunDecl>();
            SubMachines = new List<P_Root.SubMachDecl>();
            EventSets = new List<P_Root.EventSetDecl>();
            Installers = new List<P_Root.Install>();
            StableMarks = new List<P_Root.Stable>();
            SubMachineMembers = new List<P_Root.InSubMach>();
            EventSetMembers = new List<P_Root.InEventSet>();
            Annotations = new List<P_Root.Annotation>();
        }
    }
}
