using System.Collections.Generic;
using Microsoft.Formula.API.Generators;
using Microsoft.Pc.Domains;

namespace Microsoft.Pc
{
    public class LProgram
    {
        public List<PLink_Root.ModuleContainsMachine> ModuleContainsMachine { get; } = new List<PLink_Root.ModuleContainsMachine>();

        public List<PLink_Root.ModuleDecl> ModuleDecl { get; } = new List<PLink_Root.ModuleDecl>();

        public List<PLink_Root.ModulePrivateEvents> ModulePrivateEvents { get; } = new List<PLink_Root.ModulePrivateEvents>();

        public List<PLink_Root.ModuleName> ModuleName { get; } = new List<PLink_Root.ModuleName>();

        public List<PLink_Root.TestDecl> TestDecl { get; } = new List<PLink_Root.TestDecl>();

        public List<PLink_Root.RefinementDecl> RefinementDecl { get; } = new List<PLink_Root.RefinementDecl>();

        public List<PLink_Root.ImplementationDecl> ImplementationDecl { get; } = new List<PLink_Root.ImplementationDecl>();

        public List<PLink_Root.ModuleDef> ModuleDef { get; } = new List<PLink_Root.ModuleDef>();

        public IEnumerable<ICSharpTerm> Terms
        {
            get
            {
                foreach (PLink_Root.ModuleContainsMachine mc in ModuleContainsMachine)
                {
                    yield return mc;
                }
                foreach (PLink_Root.ModuleName mn in ModuleName)
                {
                    yield return mn;
                }
                foreach (PLink_Root.ModulePrivateEvents mp in ModulePrivateEvents)
                {
                    yield return mp;
                }
                foreach (PLink_Root.ModuleDecl md in ModuleDecl)
                {
                    yield return md;
                }
                foreach (PLink_Root.TestDecl td in TestDecl)
                {
                    yield return td;
                }
                foreach (PLink_Root.RefinementDecl rd in RefinementDecl)
                {
                    yield return rd;
                }
                foreach (PLink_Root.ImplementationDecl id in ImplementationDecl)
                {
                    yield return id;
                }
                foreach (PLink_Root.ModuleDef md in ModuleDef)
                {
                    yield return md;
                }
            }
        }
    }
}