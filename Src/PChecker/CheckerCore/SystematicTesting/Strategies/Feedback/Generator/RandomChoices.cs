using System;
using System.Collections.Generic;
using PChecker.Exceptions;

namespace PChecker.Generator.Object;

public class RandomChoices<T>
where T: IConvertible
{
    internal readonly System.Random Random;
    public int Pos;
    public List<T> Data = new();

    public RandomChoices(System.Random random)
    {
        Random = random;
    }

    public RandomChoices(RandomChoices<T> other)
    {
        Random = other.Random;
        Data = new List<T>(other.Data);
    }

    public T Next()
    {
        if (Pos == Data.Count)
        {
            Data.Add(GenerateNew());
        }
        return Data[Pos++];
    }

    public T GenerateNew()
    {
        if (typeof(T).IsAssignableFrom(typeof(int)))
        {
            return (T) Convert.ChangeType(Random.Next(), typeof(T));
        }

        if (typeof(T).IsAssignableFrom(typeof(double)))
        {
            return (T) Convert.ChangeType(Random.NextDouble(), typeof(T));
        }

        throw new RuntimeException("The random choices only supports int and double type.");
    }
}