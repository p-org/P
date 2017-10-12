using System.Collections.Generic;

namespace Microsoft.Pc.Backend
{
    public class TargetLanguage
    {
        private static readonly List<TargetLanguage> AllLanguagesList = new List<TargetLanguage>();

        public static TargetLanguage PSharp = new TargetLanguage("PSharp");
        public static TargetLanguage P3 = new TargetLanguage("P3");

        private TargetLanguage(string languageName)
        {
            LanguageName = languageName;
            AllLanguagesList.Add(this);
        }

        public string LanguageName { get; }

        public static IEnumerable<TargetLanguage> AllLanguages => AllLanguagesList;
    }
}
