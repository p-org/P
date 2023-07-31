namespace PChecker.Generator.Mutator;

public interface IMutator<T>
{
    T Mutate(T prev);
}