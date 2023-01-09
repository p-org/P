using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.C
{
    internal class ValueInternmentManager<T>
    {
        private readonly CNameManager nameManager;
        private readonly string typeName = typeof(T).Name.ToUpper();
        private readonly IDictionary<Function, IDictionary<T, string>> valueInternmentTable;

        public ValueInternmentManager(CNameManager nameManager)
        {
            this.nameManager = nameManager;
            valueInternmentTable = new Dictionary<Function, IDictionary<T, string>>();
        }

        public string RegisterValue(Function function, T value)
        {
            if (!valueInternmentTable.TryGetValue(function, out var funcTable))
            {
                funcTable = new Dictionary<T, string>();
                valueInternmentTable.Add(function, funcTable);
            }

            if (!funcTable.TryGetValue(value, out var literalName))
            {
                literalName = nameManager.GetTemporaryName($"LIT_{typeName}");
                funcTable.Add(value, literalName);
            }

            return literalName;
        }

        public IEnumerable<KeyValuePair<T, string>> GetValues(Function function)
        {
            if (valueInternmentTable.TryGetValue(function, out var table))
            {
                return table.AsEnumerable();
            }

            return Enumerable.Empty<KeyValuePair<T, string>>();
        }
    }
}