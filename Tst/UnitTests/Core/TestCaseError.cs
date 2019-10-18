namespace UnitTests.Core
{
    /// <summary>
    ///     Possible reasons for failure in a <see cref="CompilerTestException" />
    /// </summary>
    public enum TestCaseError
    {
        /// <summary>
        ///     The P compiler failed to produce code in the target language
        /// </summary>
        TranslationFailed,

        /// <summary>
        ///     The target language's compiler (eg. MSVC, Clang) failed to compile the generated sources
        /// </summary>
        GeneratedSourceCompileFailed,

        /// <summary>
        ///     The test case type is unrecognized
        /// </summary>
        UnrecognizedTestCaseType
    }
}