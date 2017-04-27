namespace Microsoft.Pc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Domains;
    using Microsoft.Formula.API.Generators;

    public class LProgram
    {
        public List<PLink_Root.ModuleContainsMachine> ModuleContainsMachine
        {
            get;
            private set;
        }

        public List<PLink_Root.ModuleDecl> ModuleDecl
        {
            get;
            private set;
        }

        public List<PLink_Root.ModulePrivateEvents> ModulePrivateEvents
        {
            get;
            private set;
        }

        public List<PLink_Root.ModuleName> ModuleName
        {
            get;
            private set;
        }

        public List<PLink_Root.TestDecl> TestDecl
        {
            get;
            private set;
        }
        public List<PLink_Root.RefinementDecl> RefinementDecl
        {
            get;
            private set;
        }
        public List<PLink_Root.ImplementationDecl> ImplementationDecl
        {
            get;
            private set;
        }

        public List<PLink_Root.ModuleDef> ModuleDef
        {
            get;
            private set;
        }


        public IEnumerable<ICSharpTerm> Terms
        {
            get
            {
                foreach(var mc in ModuleContainsMachine)
                {
                    yield return mc;
                }
                foreach (var mn in ModuleName)
                {
                    yield return mn;
                }
                foreach (var mp in ModulePrivateEvents)
                {
                    yield return mp;
                }
                foreach (var md in ModuleDecl)
                {
                    yield return md;
                }
                foreach (var td in TestDecl)
                {
                    yield return td;
                }
                foreach (var rd in RefinementDecl)
                {
                    yield return rd;
                }
                foreach (var id in ImplementationDecl)
                {
                    yield return id;
                }
                foreach (var md in ModuleDef)
                {
                    yield return md;
                }
            }
        }

        public LProgram()
        {
            ModuleContainsMachine = new List<PLink_Root.ModuleContainsMachine>();
            ModuleDecl = new List<PLink_Root.ModuleDecl>();
            ModulePrivateEvents = new List<PLink_Root.ModulePrivateEvents>();
            ModuleName = new List<PLink_Root.ModuleName>();
            TestDecl = new List<PLink_Root.TestDecl>();
            RefinementDecl = new List<PLink_Root.RefinementDecl>();
            ImplementationDecl = new List<PLink_Root.ImplementationDecl>();
            ModuleDef = new List<PLink_Root.ModuleDef>();
        }
    }
}
