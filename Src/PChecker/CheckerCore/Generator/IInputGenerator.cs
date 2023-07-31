namespace PChecker.Generator;

public interface IInputGenerator<T> : IGenerator<T>
{

    /// <summary>
    /// Returns a non-negative random number.
    /// </summary>
    int Next();

    /// <summary>
    /// Returns a non-negative random number less than maxValue.
    /// </summary>
    /// <param name="maxValue">Exclusive upper bound</param>
    int Next(int maxValue);

    /// <summary>
    /// Returns a random floating-point number that is greater
    /// than or equal to 0.0, and less than 1.0.
    /// </summary>
    double NextDouble();
}