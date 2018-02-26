using System.Collections.Generic;

namespace Microsoft.Pc.Backend
{
    public class TargetLanguage
    {
        private static readonly IDictionary<CompilerOutput, ICodeGenerator> _backendMap = new Dictionary<CompilerOutput, ICodeGenerator>();

        public static void RegisterCodeGenerator(CompilerOutput name, ICodeGenerator generator)
        {
            _backendMap[name] = generator;
        }

        public static ICodeGenerator GetCodeGenerator(CompilerOutput languageName)
        {
            return _backendMap[languageName];
        }
    }
}
