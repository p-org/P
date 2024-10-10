using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Logging;

namespace PChecker.Feedback;

internal class TimelineObserver: ActorRuntimeLogBase
{

    private HashSet<(string, string, string)> _timelines = new();
    private Dictionary<string, HashSet<string>> _allEvents = new();
    private Dictionary<string, List<string>> _orderedEvents = new();

    public static readonly List<(int, int)> Coefficients = new();
    public static int NumOfCoefficients = 50;

    static TimelineObserver()
    {
        // Fix seed to generate same random numbers across runs.
        var rand = new System.Random(0);

        for (int i = 0; i < NumOfCoefficients; i++)
        {
            Coefficients.Add((rand.Next(), rand.Next()));
        }
    }

    public override void OnDequeueEvent(ActorId id, string stateName, Event e)
    {
        string actor = id.Type;
        
        _allEvents.TryAdd(actor, new());
        _orderedEvents.TryAdd(actor, new());

        string name = e.GetType().Name;
        foreach (var ev in _allEvents[actor])
        {
            _timelines.Add((actor, ev, name));
        }
        _allEvents[actor].Add(name);
        _orderedEvents[actor].Add(name);
    }

    public int GetTimelineHash()
    {
        return GetAbstractTimeline().GetHashCode();
    }

    public string GetAbstractTimeline()
    {
        var tls = _timelines.Select(it => $"<{it.Item1}, {it.Item2}, {it.Item3}>").ToList();
        tls.Sort();
        return string.Join(";", tls);
    }

    public string GetTimeline()
    {
        return string.Join(";", _orderedEvents.Select(it =>
        {
            var events = string.Join(",", it.Value);
            return $"{it.Key}: {events}";
        }));
    }

    public List<int> GetTimelineMinhash()
    {
        List<int> minHash = new();
        var timelineHash = _timelines.Select(it => it.GetHashCode());
        foreach (var (a, b) in Coefficients)
        {
            int minValue = Int32.MaxValue;
            foreach (var value in timelineHash)
            {
                int hash = a * value + b;
                minValue = Math.Min(minValue, hash);
            }
            minHash.Add(minValue);
        }
        return minHash;
    }
}