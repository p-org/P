using System.Diagnostics;
using System.IO;
using System.Reflection;
using Antlr4.StringTemplate;

namespace Microsoft.Pc.Backend
{
    public class CodeGen
    {
        private const int LineWidth = 100;
        private const string TemplatesNamespace = "Templates";
        private const string BaseNamespace = nameof(Microsoft) + "." + nameof(Pc) + "." + TemplatesNamespace;

        public static string GenerateCode(TargetLanguage language, PProgramModel program)
        {
            TemplateGroup templateGroup = GetTemplateGroup(language);
            Template t = templateGroup.GetInstanceOf("topLevel");
            t.Add("pgm", program);
            return t.Render(LineWidth);
        }

        private static TemplateGroup GetTemplateGroup(TargetLanguage language)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream(BaseNamespace + "." + language.LanguageName + ".stg");
            Debug.Assert(stream != null);
            using (var reader = new StreamReader(stream))
            {
                return new TemplateGroupString(reader.ReadToEnd());
            }
        }
    }
}
