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

        public static void ZingerTimeOut(object obj)
        {
            PTesterUtil.PrintMessage("");
            PTesterUtil.PrintMessage(String.Format("--Zinger Timed Out --"));
            PTesterUtil.PrintMessage(String.Format("--Final Stats --"));
            //ZingerStats.PrintPeriodicStats();
            //Environment.Exit((int)ZingerResult.ZingerTimeOut);
        }

        //private static System.Threading.Timer TimeOutTimer;
        public static void StartTimeOut()
        {
            TimerCallback tcb = ZingerTimeOut;
            //TimeOutTimer = new Timer(tcb, null, ZingerConfiguration.Timeout * 1000, ZingerConfiguration.Timeout * 1000);
        }
    }
}
