using System;
using System.Threading;
using P.Explorer;

namespace P.Tester
{
    public class PTesterUtil
    {
        public static Random rand = new Random(DateTime.Now.Millisecond);

        public static void PrintSuccessMessage(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = prevColor;
        }

        public static void PrintErrorMessage(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = prevColor;
        }

        public static void PrintMessage(string message)
        {
            if (PTConfig.PrintStats)
            {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(message);
                Console.ForegroundColor = prevColor;
            }
        }

    }
}
