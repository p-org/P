using System;
using System.Collections.Generic;
using Microsoft.PSharp;

using GenericExamples;
using InheritanceExamples;
using AdvancedExamples;

namespace RaceyRegressionSuite
{
    /// <summary>
    /// This is the P# data race regression suite to be used for
    /// testing the P# static analyser.
    /// 
    /// Is not to be used for normal execution.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Registering events to the runtime.\n");
            //Runtime.RegisterNewEvent(typeof(eUnit));

            //Console.WriteLine("Registering state machines to the runtime.\n");
            //Runtime.RegisterNewMachine(typeof(A));
            //Runtime.RegisterNewMachine(typeof(B));
        }
    }
}
