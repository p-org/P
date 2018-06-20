using System;

namespace UnitTestsCore
{
    public static class ConsoleHelper
    {
        public static void WriteError(string format, params object[] args)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ForegroundColor = saved;
        }
    }
}
