using System;
using System.Linq;
using PChecker.Generator.Object;

namespace PChecker.Generator.Mutator;

public class Utils
{
    public static int SampleGeometric(double p, double random) {
        var result = Math.Ceiling(Math.Log(1 - random) / Math.Log(1 - p));
        return (int)result;
    }
    public static RandomChoices<T> MutateRandomChoices<T> (RandomChoices<T> randomChoices, int meanMutationCount, int meanMutationSize, System.Random random)
        where T: IConvertible
    {
        meanMutationCount = Math.Max(Math.Min(randomChoices.Data.Count / 3, meanMutationCount), 1);
        meanMutationSize = Math.Max(Math.Min(randomChoices.Data.Count / 3, meanMutationSize), 1);
        RandomChoices<T> newChoices = new RandomChoices<T>(randomChoices);
        int mutations = Utils.SampleGeometric(1.0f / meanMutationCount, random.NextDouble());

        while (mutations-- > 0)
        {
            int offset = random.Next(newChoices.Data.Count);
            int mutationSize = Utils.SampleGeometric(1.0f / meanMutationSize, random.NextDouble());
            for (int i = offset; i < offset + mutationSize; i++)
            {
                if (i >= newChoices.Data.Count)
                {
                    break;
                }

                newChoices.Data[i] = newChoices.GenerateNew();
            }
        }

        return newChoices;
    }
}