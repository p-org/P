using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrtDistDeployer
{
    class PrtDistDeployer
    {
        public static void PrintRed(string log)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(log);
            Console.ForegroundColor = oldColor;
        }

        public static void PrintGreen(string log)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(log);
            Console.ForegroundColor = oldColor;
        }

        public static void PrintOptions()
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("1 : Deploy on Network Share");
            Console.WriteLine("2 : Start NodeManager on all Machines");
            Console.WriteLine("3 : Ping NodeManager on all Machines");
            Console.WriteLine("4 : Start Main Machine");
            Console.WriteLine("5 : Kill Deployed Service");
            Console.WriteLine("6 : Kill NodeManager on all Machines");
            Console.WriteLine("7 : Exit");
            Console.WriteLine();
            Console.Write("Enter the Option: ");
            Console.ForegroundColor = oldColor;
        }
        public static void Main(string[] args)
        {
            while (true)
            {
                PrintRed("Pick the operation to be Performed");
                PrintOptions();
                var pressedOption = Console.ReadLine();
                int option = int.Parse(pressedOption);
                switch(option)
                {
                    case 1:
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    case 4:
                        break;
                    case 5:
                        break;
                    case 6:
                        break;
                    case 7:
                        Environment.Exit(0);
                        break;
                    default:
                        PrintRed("Invalid Option");
                        break;
                }
            }
        }

        public static void DeployOnNetworkShare()
        {

        }
    }
}
