using System;
using System.Collections.Generic;
using System.Data.Common;
using PChecker.Exceptions;

namespace PChecker.Generator.Object;

public class RandomChoices<T>
where T: IConvertible
{
    private readonly System.Random _random;
    public int Pos;
    public List<T> Data = new();

    public RandomChoices(System.Random random)
    {
        _random = random;
    }

    public RandomChoices(RandomChoices<T> other)
    {
        _random = other._random;
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
            return (T) Convert.ChangeType(_random.Next(), typeof(T));
        }
        else if (typeof(T).IsAssignableFrom(typeof(double)))
        {
            return (T) Convert.ChangeType(_random.NextDouble(), typeof(T));
        }
        else
        {
            throw new RuntimeException("The random choices only supports int and double type.");
        }
    }
}