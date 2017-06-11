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

        public List<P_Root.AnyTypeDecl> AnyTypeDecl
        {
            get;
            private set;
        }

        public List<P_Root.TypeDef> TypeDefs
        {
            get;
            private set;
        }

        public List<P_Root.DependsOn> DependsOn
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

        public List<P_Root.InterfaceTypeDef> InterfaceTypeDef
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


        public List<P_Root.FunProtoCreatesDecl> FunProtoCreates
        {
            get;
            private set;
        }

        public List<P_Root.MachineKind> MachineKinds
        {
            get;
            private set;
        }

        public List<P_Root.MachineCard> MachineCards
        {
            get;
            private set;
        }

        public List<P_Root.MachineStart> MachineStarts
        {
            get;
            private set;
        }

        public bool IgnoreDecl;

        public void Add(object item)
        {
            if (IgnoreDecl)
            {
                return;
            }
            else if (item is P_Root.AnyTypeDecl)
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
            else if (item is P_Root.ModelType)
            {
                ModelTypes.Add(item as P_Root.ModelType);
            }
            else if (item is P_Root.EnumTypeDef)
            {
                EnumTypeDefs.Add(item as P_Root.EnumTypeDef);
            }
            else if (item is P_Root.TypeDef)
            {
                TypeDefs.Add(item as P_Root.TypeDef);
            }
            else if(item is P_Root.DependsOn)
            {
                DependsOn.Add(item as P_Root.DependsOn);
            }
            else if(item is P_Root.FunProtoCreatesDecl)
            {
                FunProtoCreates.Add(item as P_Root.FunProtoCreatesDecl);
            }
            else
            {
                throw new Exception("Cannot add into the Program : " + item.ToString());
            }
        }

        public IEnumerable<ICSharpTerm> Terms
        {
            get
            {
                foreach (var at in AnyTypeDecl)
                {
                    yield return at;
                }

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

                foreach (var md in MachineCards)
                {
                    yield return md;
                }

                foreach (var md in MachineKinds)
                {
                    yield return md;
                }

                foreach (var md in MachineStarts)
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

                foreach (var inter in InterfaceTypeDef)
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

                foreach(var fp in FunProtoCreates)
                {
                    yield return fp;
                }

                foreach (var d in DependsOn)
                {
                    yield return d;
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
            MachineKinds = new List<P_Root.MachineKind>();
            MachineCards = new List<P_Root.MachineCard>();
            MachineStarts = new List<P_Root.MachineStart>();
            States = new List<P_Root.StateDecl>();
            Variables = new List<P_Root.VarDecl>();
            Transitions = new List<P_Root.TransDecl>();
            Functions = new List<P_Root.FunDecl>();
            AnonFunctions = new List<P_Root.AnonFunDecl>();
            Dos = new List<P_Root.DoDecl>();
            Annotations = new List<P_Root.Annotation>();
            Observes = new List<P_Root.ObservesDecl>();
            MachineExports = new List<P_Root.MachineExports>();
            InterfaceTypeDef = new List<P_Root.InterfaceTypeDef>();
            EventSetDecl = new List<P_Root.EventSetDecl>();
            EventSetContains = new List<P_Root.EventSetContains>();
            FunProtoDecls = new List<P_Root.FunProtoDecl>();
            MachineProtoDecls = new List<P_Root.MachineProtoDecl>();
            MachineSends = new List<P_Root.MachineSends>();
            MachineReceives = new List<P_Root.MachineReceives>();
            DependsOn = new List<P_Root.DependsOn>();
            IgnoreDecl = false;
            FunProtoCreates = new List<P_Root.FunProtoCreatesDecl>();
            AnyTypeDecl = new List<P_Root.AnyTypeDecl>();
        }
    }
}
