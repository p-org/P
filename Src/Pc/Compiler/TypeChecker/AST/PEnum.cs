using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class PEnum : IPDecl
    {
        private readonly HashSet<EnumElem> elements = new HashSet<EnumElem>();
        private readonly HashSet<int> values = new HashSet<int>();

        public PEnum(string name, PParser.EnumTypeDefDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public IEnumerable<EnumElem> Values => elements;
        public int Count => elements.Count;

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }

        public bool AddElement(EnumElem elem)
        {
            if (values.Contains(elem.Value))
            {
                return false;
            }

            var success = elem.ParentEnum?.RemoveElement(elem);
            Debug.Assert(success != false);
            elem.ParentEnum = this;
            elements.Add(elem);
            values.Add(elem.Value);
            return true;
        }

        public bool RemoveElement(EnumElem elem)
        {
            if (elem.ParentEnum != this)
            {
                return false;
            }

            bool success = elements.Remove(elem);
            Debug.Assert(success);
            values.Remove(elem.Value);
            elem.ParentEnum = null;
            return true;
        }
    }
}
