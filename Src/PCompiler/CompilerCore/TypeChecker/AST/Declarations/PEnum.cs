using Antlr4.Runtime;
using System.Collections.Generic;
using System.Diagnostics;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class PEnum : IPDecl
    {
        private readonly HashSet<EnumElem> elements = new HashSet<EnumElem>();
        private readonly HashSet<int> values = new HashSet<int>();

        public PEnum(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.EnumTypeDefDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public IEnumerable<EnumElem> Values => elements;

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }

        public bool AddElement(EnumElem elem)
        {
            if (values.Contains(elem.Value))
            {
                return false;
            }

            bool? success = elem.ParentEnum?.RemoveElement(elem);
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