using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.Backend
{
    public partial class PrtCodeGenerator
    {
        private class ValueInternmentManager<T>
        {
            private readonly NameManager nameManager;
            private readonly IDictionary<Function, IDictionary<T, string>> valueInternmentTable;
            private readonly string typeName = typeof(T).Name.ToUpper();

            public ValueInternmentManager(NameManager nameManager)
            {
                this.nameManager = nameManager;
                this.valueInternmentTable = new Dictionary<Function, IDictionary<T, string>>();
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
}
