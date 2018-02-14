namespace Microsoft.Pc
{
    public interface ICompilerOutput
    {
        void WriteMessage(string msg, SeverityKind severity);
    }

    public enum SeverityKind
    {
        Info,
        Warning,
        Error
    }
}
