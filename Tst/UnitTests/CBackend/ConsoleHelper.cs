using System;

namespace UnitTests.CBackend
{
    public static class ConsoleHelper
    {
        public static void WriteError(string format, params object[] args)
        {
            ConsoleColor saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ForegroundColor = saved;
        }
    }
}
