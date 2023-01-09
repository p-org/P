using System;

namespace Plang;

public class CommandLineOutput
{
    public static void WriteError(string msg)
    {
        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }

    public static void WriteInfo(string msg)
    {
        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }

    public static void WriteWarning(string msg)
    {
        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }
}