using System;
using System.Collections.Generic;
using System.IO;
using PChecker.Exceptions;
using PChecker.IO.Logging;
using PChecker.Runtime.StateMachines;

namespace PChecker.Runtime;

public class VectorClockTimeline
{
    private static Dictionary<(String, EventType), (HashSet<Dictionary<String, int>> vcSet, HashSet<(String, EventType)> eSet)> CurrMap = new Dictionary<(String, EventType), (HashSet<Dictionary<String, int>>, HashSet<(String, EventType)>)>(); 
    private static TextWriter Logger = new ConsoleLogger();
    private static HashSet<int> UniqueTimelineSet = new HashSet<int>();

    public enum EventType
    {
        SEND, DEQUEUE
    }
    public static void AddToCurrentTimeline(String currE, EventType currType, Dictionary<String, int> vectorTime)
    {
        if (!CurrMap.ContainsKey((currE, currType)))
        {
            CurrMap[(currE, currType)] = (new HashSet<Dictionary<String, int>>(), new HashSet<(String, EventType)>());
            CurrMap[(currE, currType)].vcSet.Add(vectorTime);
        }
        else
        {
            foreach (var vc in CurrMap[(currE, currType)].vcSet)
            {
                int comparison = VectorTime.CompareTwo(vectorTime, vc);
                if (comparison == 1)
                {
                    CurrMap[(currE, currType)].vcSet.Remove(vc);
                    CurrMap[(currE, currType)].vcSet.Add(vectorTime);
                    break;
                }
                else if (comparison == -1)
                {
                    new AssertionFailureException("An event occured after another but has less vector clock");
                }
                else
                {
                    CurrMap[(currE, currType)].vcSet.Add(vectorTime);
                    break;
                }
            }
        }
        
        var (currVcSet, currESet) = CurrMap[(currE, currType)];

        foreach ((String e, EventType t) in CurrMap.Keys)
        {
            var (vcSet, eSet) = CurrMap[(e, t)];

            foreach (var vc in vcSet)
            {
                int comparison = VectorTime.CompareTwo(vectorTime, vc);
                if (comparison == 1)
                {
                    currESet.Add((e,t));
                    break;
                }
                if (comparison == -1)
                {
                    new AssertionFailureException("An event occured after another but has less vector clock");
                }
            }
        }

        
    }

    public static void PrintTimeline()
    {
        foreach (var entry in CurrMap)
        {
            Logger.WriteLine($"\nEvent: {entry.Key.ToString()}");
            Logger.WriteLine("  Dependencies:");
            foreach (var dep in entry.Value.eSet)
            {
                Logger.WriteLine($"   {dep}");
            }
        }
    }
    
    public static void RecordTimeline()
    {
        if (CurrMap.Keys.Count == 0)
        {
            return;
        }
        // PrintTimeline();
        int timelineHash = GenerateTimelineHash();
        UniqueTimelineSet.Add(timelineHash);
        // Console.WriteLine($"Recorded timeline with hash: {timelineHash}");
        // Console.WriteLine($"Unique timelines: {string.Join(", ", UniqueTimelineSet)}");
    }
    
    // Step 2: Generate Hash for the Current Timeline
    private static int GenerateTimelineHash()
    {
        unchecked // Allow overflow for hash calculation
        {
            int hash = 19;

            foreach (var entry in CurrMap)
            {
                var (vcSet, eSet) = entry.Value;

                // Hash the event name
                hash = (hash * 31) + entry.Key.GetHashCode();

              
                // Hash dependencies
                foreach (var e in eSet)
                {
                    hash = (hash * 31) + e.GetHashCode();
                }
            }

            return hash;
        }
        
    }

    // Step 3: Get the Number of Unique Timelines
    public static int GetUniqueTimelineCount()
    {
        return UniqueTimelineSet.Count;
    }
}