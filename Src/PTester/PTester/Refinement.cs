using System;
using System.Collections.Generic;
using System.IO;
using P.Runtime;

namespace P.Tester
{
    

    public class RefinementChecking
    {
        string LHSModel;
        string RHSModel;

        List<VisibleTrace> allTracesRHS;
        public RefinementChecking(string lhs, string rhs)
        {
            LHSModel = lhs;
            if(!File.Exists(LHSModel))
            {
                Console.WriteLine("LHSModel file: {0} does not exist", LHSModel);
                Environment.Exit(-1);
            }
            RHSModel = rhs;
            if (!File.Exists(RHSModel))
            {
                Console.WriteLine("LHSModel file: {0} does not exist", RHSModel);
                Environment.Exit(-1);
            }
            allTracesRHS = new List<VisibleTrace>();
        }

        public void RunChecker()
        {

        }
    }
}
