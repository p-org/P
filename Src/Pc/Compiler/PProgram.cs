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

        public List<P_Root.FileInfo> FileInfos
        {
            get;
            private set;
        }

        public List<P_Root.LineInfo> LineInfos
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

                foreach (var info in FileInfos)
                {
                    yield return info;
                }

                foreach (var info in LineInfos)
                {
                    yield return info;
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
            FileInfos = new List<P_Root.FileInfo>();
            LineInfos = new List<P_Root.LineInfo>();
        }
    }
}
