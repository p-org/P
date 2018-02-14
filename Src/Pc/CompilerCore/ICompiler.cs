namespace Microsoft.Pc {
    public interface ICompiler {
        bool Compile(ICompilerOutput log, CommandLineOptions options);
        bool Link(ICompilerOutput log, CommandLineOptions options);
    }
}