using System;

namespace Plang;

public static class CommandLineOutput
{
    public static void WriteError(string msg)
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }

    public static void WriteInfo(string msg)
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }

    public static void WriteWarning(string msg)
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }
}