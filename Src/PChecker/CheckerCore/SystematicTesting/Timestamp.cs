using System;

namespace PChecker.SystematicTesting;

public class Timestamp : IComparable<Timestamp>
{
    private double Time;

    private object TimeLock;

    public static Timestamp DefaultTimestamp = new(-1);

    public Timestamp()
    {
        Time = 0;
        TimeLock = new object();
    }

    private Timestamp(double time)
    {
        Time = time;
        TimeLock = new object();
    }

    public double GetTime()
    {
        double time;
        lock (TimeLock)
        {
            time = Time;

        }
        return time;
    }

    public void SetTime(double time)
    {
        lock (TimeLock)
        {
            Time = time;

        }
    }

    public void IncrementTime(double delay)
    {
        lock (TimeLock)
        {
            Time += delay;

        }
    }

    public static bool operator>(Timestamp t1, Timestamp t2)
    {
        return t1.GetTime() > t2.GetTime();
    }

    public static bool operator>=(Timestamp t1, Timestamp t2)
    {
        return t1.GetTime() >= t2.GetTime();
    }

    public static bool operator<(Timestamp t1, Timestamp t2)
    {
        return t1.GetTime() < t2.GetTime();
    }

    public static bool operator<=(Timestamp t1, Timestamp t2)
    {
        return t1.GetTime() <= t2.GetTime();
    }

    public static bool operator==(Timestamp t1, Timestamp t2)
    {
        return t1.GetTime() == t2.GetTime();
    }

    public static bool operator!=(Timestamp t1, Timestamp t2)
    {
        return t1.GetTime() != t2.GetTime();
    }

    public int CompareTo(Timestamp other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Time.CompareTo(other.Time);
    }
}
