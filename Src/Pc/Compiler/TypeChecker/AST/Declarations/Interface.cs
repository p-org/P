using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Interface : IConstructible, IPDecl
    {
        public Interface(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.InterfaceDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public EventSet ReceivableEvents { get; set; }
        public ISet<Machine> Implementations { get; } = new HashSet<Machine>();

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;
        public IList<IPAST> Children => throw new NotImplementedException("ast children");
        public IPAST Parent => throw new NotImplementedException();
    }
}
