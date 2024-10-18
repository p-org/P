using System;
using System.Collections.Generic;
using System.IO;
using PChecker.IO.Logging;
using PChecker.Runtime.Events;

namespace PChecker.Runtime;

public class BehavioralObserver
{
    private static HashSet<(VectorTime, Event, EventType)> CurrTimeline = new HashSet<(VectorTime, Event, EventType)>();
    private static List<int[]> AllTimeline = new List<int[]>();
    private static TextWriter Logger = new ConsoleLogger();
    // MinHash object with 100 hash functions, check how many of these 100 values are identical between the two sets
    private static MinHash minHash = new MinHash(100);  

    
    public enum EventType
    {
        SEND, DEQUEUE
    }

    public BehavioralObserver()
    {
        CurrTimeline = new HashSet<(VectorTime, Event, EventType)>();
    }

    public static void AddToCurrentTimeline(Event e, EventType t, VectorTime vectorTime)
    {
        CurrTimeline.Add((new VectorTime(vectorTime), e, t));
    }
    
    public static int CalculateDiff(int[] currentSignature, int[] otherSignature)
    {
        double similarity = minHash.ComputeSimilarity(currentSignature, otherSignature);
        double differenceScore = 1 - similarity;
        return (int)(differenceScore * 100);  // Scale the difference score (e.g., multiply by 100)
    }

    public static int GetUniqueScore(int[] currentSignature)
    {
        int scoreSum = 0;
        foreach (var otherSignature in AllTimeline)
        {
            int score = CalculateDiff(currentSignature, otherSignature);
            if (score == 0)
            {
                return 0;
            }
            scoreSum += score;
        }
        return scoreSum/AllTimeline.Count;
    }
    
    public static void PrintTimeline(List<(VectorTime, Event, EventType)> timeline)
    {
        Logger.WriteLine("Global state of all machines:");
        foreach (var entry in timeline)
        {
            Logger.WriteLine($"Machine {entry}");
        }
    }
    
    public static List<(VectorTime, Event, EventType)> SortByVectorClock()
    {
        List<(VectorTime, Event, EventType)> sortedTimeline = new List<(VectorTime, Event, EventType)>(CurrTimeline);

        // Sort by event name then vector time
        sortedTimeline.Sort((x, y) => x.Item2.ToString().CompareTo(y.Item2.ToString()));
        sortedTimeline.Sort((x, y) => x.Item1.CompareTo(y.Item1));

        return sortedTimeline;
    }

    public static void NextIter()
    {
        if (CurrTimeline.Count == 0)
        {
            return;
        }
        List<(VectorTime, Event, EventType)> SortedCurrTimeline = SortByVectorClock();
        int[] currSignature = minHash.ComputeMinHashSignature(SortedCurrTimeline);
        if (AllTimeline.Count == 0)
        {
            AllTimeline.Add(currSignature);
            return;
        }
        int uniqueScore = GetUniqueScore(currSignature);
        Logger.WriteLine($"----**** UniquenessScore: {uniqueScore}");
        if (uniqueScore != 0)
        {
            AllTimeline.Add(currSignature);
        }
        CurrTimeline = new HashSet<(VectorTime, Event, EventType)>();
    }
}

public class MinHash
{
    private int numHashFunctions;  // Number of hash functions to use
    private List<Func<int, int>> hashFunctions;  // List of hash functions

    public MinHash(int numHashFunctions)
    {
        this.numHashFunctions = numHashFunctions;
        this.hashFunctions = new List<Func<int, int>>();

        System.Random rand = new System.Random();
        for (int i = 0; i < numHashFunctions; i++)
        {
            int a = rand.Next();
            int b = rand.Next();
            hashFunctions.Add(x => a * x + b); 
        }
    }

    // Create a MinHash signature for a given set
    public int[] ComputeMinHashSignature(List<(VectorTime, Event, BehavioralObserver.EventType)> set)
    {
        int[] signature = new int[numHashFunctions];

        for (int i = 0; i < numHashFunctions; i++)
        {
            signature[i] = int.MaxValue;
        }
        foreach (var element in set)
        {
            int elementHash = ComputeElementHash(element);

            for (int i = 0; i < numHashFunctions; i++)
            {
                int hashedValue = hashFunctions[i](elementHash);
                if (hashedValue < signature[i])
                {
                    signature[i] = hashedValue;
                }
            }
        }
        return signature;
    }

    // Compute a composite hash for the (VectorTime, Event, EventType) tuple
    private int ComputeElementHash((VectorTime, Event, BehavioralObserver.EventType) element)
    {
        int hash1 = element.Item1.GetHashCode();
        int hash2 = element.Item2.GetHashCode();
        int hash3 = element.Item3.GetHashCode();
        return hash1 ^ hash2 ^ hash3;
    }

    // Compute Jaccard similarity based on MinHash signatures
    public double ComputeSimilarity(int[] signature1, int[] signature2)
    {
        int identicalMinHashes = 0;

        for (int i = 0; i < numHashFunctions; i++)
        {
            if (signature1[i] == signature2[i])
            {
                identicalMinHashes++;
            }
        }
        return (double)identicalMinHashes / numHashFunctions;
    }
}

