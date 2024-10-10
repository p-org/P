namespace PChecker.Generator;

public interface IGenerator<T>
{
    /// <summary>
    /// Mutate the current generator and create a new one.
    /// </summary>
    /// <returns>A new generator.</returns>
    T Mutate();

    /// <summary>
    /// Copy the current generator and create a new one.
    /// </summary>
    /// <returns>A new generator.</returns>
    T Copy();

    T New();
}