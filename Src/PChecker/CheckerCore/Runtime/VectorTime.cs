using System;
using System.Collections.Generic;
using PChecker.Runtime.StateMachines;

/// <summary>
/// Vector clock of a state machine. It is copied on every send, so it is kept as a dense array
/// indexed by <see cref="StateMachineId.Value"/>, with missing entries meaning 0.
/// </summary>
public class VectorTime
{
    private int[] clock;

    // Index of the owning state machine.
    private readonly int ownIndex;

    public VectorTime(StateMachineId stateMachineId)
    {
        ownIndex = checked((int)stateMachineId.Value);
        clock = new int[ownIndex + 1];
    }

    // Clone constructor (creates a snapshot of the vector clock)
    public VectorTime(VectorTime other)
    {
        ownIndex = other.ownIndex;
        clock = (int[])other.clock.Clone();
    }

    // Increment the logical clock for this state machine
    public void Increment()
    {
        clock[ownIndex]++;
    }

    // Merge another vector clock into this one, taking the maximum timestamp per state machine.
    public void Merge(VectorTime otherTime)
    {
        var other = otherTime.clock;
        if (other.Length > clock.Length)
        {
            Array.Resize(ref clock, other.Length);
        }

        for (var i = 0; i < other.Length; i++)
        {
            if (other[i] > clock[i])
            {
                clock[i] = other[i];
            }
        }
    }

    // Compare this vector clock to another (partial order over vector clocks).
    // Follows the IComparable convention: negative => this happens-BEFORE other;
    // positive => this happens-AFTER other; 0 => clocks are equal or concurrent.
    // Missing entries are treated as 0.
    public int CompareTo(VectorTime other)
    {
        bool atLeastOneLess = false;
        bool atLeastOneGreater = false;

        var length = Math.Max(clock.Length, other.clock.Length);
        for (var i = 0; i < length; i++)
        {
            int thisTime = i < clock.Length ? clock[i] : 0;
            int otherTime = i < other.clock.Length ? other.clock[i] : 0;

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
            return -1;
        }
        if (atLeastOneGreater && !atLeastOneLess)
        {
            return 1;
        }
        return 0;
    }
    
    public override string ToString()
    {
        var elements = new List<string>();
        for (var i = 0; i < clock.Length; i++)
        {
            if (clock[i] != 0)
            {
                elements.Add($"StateMachine {i}: {clock[i]}");
            }
        }
        return $"[{string.Join(", ", elements)}]";
    }

    public override bool Equals(object obj) => obj is VectorTime other && IsEqualTo(other);

    private bool IsEqualTo(VectorTime other)
    {
        var length = Math.Max(clock.Length, other.clock.Length);
        for (var i = 0; i < length; i++)
        {
            if ((i < clock.Length ? clock[i] : 0) != (i < other.clock.Length ? other.clock[i] : 0))
            {
                return false;
            }
        }
        return true;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        for (var i = 0; i < clock.Length; i++)
        {
            if (clock[i] != 0)
            {
                hash = hash * 31 + i;
                hash = hash * 31 + clock[i];
            }
        }
        return hash;
    }
}
