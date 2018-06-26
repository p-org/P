namespace Microsoft.Pc {
    public interface ICompiler {
        bool Compile(ICompilerOutput output, CommandLineOptions options);
        bool Link(ICompilerOutput output, CommandLineOptions options);
    }
}