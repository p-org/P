namespace Plang.PInfer
{
    public enum PInferMode
    {
        Compile, Interactive, Auto
    }

    public class PInferConfiguration
    {
        public int NumGuardPredicates;
        public int NumFilterPredicates;
        public int NumForallQuantifiers;
        public int InvArity;
        public int[] MustIncludeGuard;
        public int[] MustIncludeFilter;
        public bool SkipTrivialCombinations;
        public string[] TracePaths;
        public bool ListPredicates;
        public bool ListTerms;
        public string OutputDirectory;
        public string ProjectName;
        public bool Verbose;
        public int PruningLevel;
        public PInferMode Mode;

        public PInferConfiguration()
        {
            NumGuardPredicates = 1;
            NumFilterPredicates = 0;
            NumForallQuantifiers = -1;
            InvArity = 2;
            MustIncludeGuard = [];
            MustIncludeFilter = [];
            TracePaths = [];
            SkipTrivialCombinations = true;
            ListPredicates = false;
            ListTerms = false;
            OutputDirectory = "PGenerated";
            ProjectName = "generatedOutput";
            Verbose = false;
            PruningLevel = 3;
            Mode = PInferMode.Compile;
        }
    }
}
