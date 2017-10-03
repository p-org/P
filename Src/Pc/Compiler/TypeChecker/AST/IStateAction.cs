namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IStateAction
    {
        PEvent Trigger { get; }
    }
}
