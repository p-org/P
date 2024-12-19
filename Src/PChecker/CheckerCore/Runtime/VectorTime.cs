using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.Runtime.StateMachines;
using PChecker.IO.Logging;

public class VectorTime
{
    // Dictionary that uses String as the key and stores the logical clock as the value
    public Dictionary<String, int> Clock { get; private set; }

    // The ID of the current state machine
    
    private String stateMachineId;

    public VectorTime(String stateMachineId)
    {
        this.stateMachineId = stateMachineId;
        Clock = new Dictionary<String, int>();
        Clock[stateMachineId] = 0;  // Initialize the clock for this state machine
    }
    
    // Clone constructor (creates a snapshot of the vector clock)
    public VectorTime(VectorTime other)
    {
        Clock = new Dictionary<String, int>(other.Clock);
    }

    // Increment the logical clock for this state machine
    public void Increment()
    {
        Clock[stateMachineId]++;
    }

    // Merge another vector clock into this one
    public void Merge(VectorTime otherTime)
    {
        foreach (var entry in otherTime.Clock)
        {
            String otherMachineId = entry.Key;
            int otherTimestamp = entry.Value;

            if (Clock.ContainsKey(otherMachineId))
            {
                // Take the maximum timestamp for each state machine
                Clock[otherMachineId] = Math.Max(Clock[otherMachineId], otherTimestamp);
            }
            else
            {
                // Add the state machine's timestamp if it doesn't exist in this time
                Clock[otherMachineId] = otherTimestamp;
            }
        }
    }
    
    // Compare this vector clock to another for sorting purposes
    // Return value: 1 = This vector clock happens after the other (greater), -1 = This vector clock happens before the other (less),
    // 0 = Clocks are equal or concurrent
    public static int CompareTwo(Dictionary<String, int> self, Dictionary<String, int> other)
    {
        bool atLeastOneLess = false;
        bool atLeastOneGreater = false;

        foreach (var machineId in self.Keys)
        {
            int thisTime = self[machineId];
            int otherTime = other.ContainsKey(machineId) ? other[machineId] : 0;

            if (thisTime < otherTime)
            {
                atLeastOneLess = true;
            }
            else if (thisTime > otherTime)
            {
                atLeastOneGreater = true;
            }
            if (atLeastOneLess && atLeastOneGreater)
            {
                return 0;
            }
        }
        if (atLeastOneLess && !atLeastOneGreater)
        {
            return 1;
        }
        if (atLeastOneGreater && !atLeastOneLess)
        {
            return -1;
        }
        return 0;
    }
    
    
    public int CompareTo(VectorTime other)
    {
        return CompareTwo(Clock, other.Clock);
    }
    
    public override string ToString()
    {
        var elements = new List<string>();
        foreach (var entry in Clock)
        {
            elements.Add($"StateMachine {entry.Key}: {entry.Value}");
        }
        return $"[{string.Join(", ", elements)}]";
    }
    
    public override bool Equals(object obj)
    {
        if (obj is VectorTime other)
        {
            return Clock.OrderBy(x => x.Key).SequenceEqual(other.Clock.OrderBy(x => x.Key));
        }
        return false;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var entry in Clock)
        {
            hash = hash * 31 + entry.Key.GetHashCode();
            hash = hash * 31 + entry.Value.GetHashCode();
        }
        return hash;
    }
}