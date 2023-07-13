using System;
using PChecker.ExhaustiveSearch;
using PChecker.IO.Debugging;
using PChecker.IO.Logging;
using PChecker.Scheduling;
using PChecker.SystematicTesting;

namespace PChecker;

/// <summary>
/// Checker class that implements the run method which acts as the entry point into the P checker.
/// </summary>
public class Checker
{
    /// <summary>
    /// Run the P checker for the given configuration
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns>exit code for the p checker</returns>
    public static void Run(CheckerConfiguration configuration)
    {
        var logger = new ConsoleLogger();
        // if the replay option is passed then we ignore all the other options and replay the schedule
        if (configuration.SchedulingStrategy == "replay")
        {
            logger.WriteLine(
                $"Replay option is used, checker is ignoring all other parameters and using the {configuration.ScheduleFile} to replay the schedule");
            logger.WriteLine($"... Replaying {configuration.ScheduleFile}");

            switch (configuration.Mode)
            {
                case CheckerMode.BugFinding:
                {
                    var engine = TestingEngine.Create(configuration);
                    engine.Run();
                    Error.Write(logger, ConsoleColor.Yellow, engine.GetReport());
                }
                    break;
                case CheckerMode.Verification:
                case CheckerMode.Coverage:
                    ExhaustiveEngine.Create(configuration).Run();
                    break;
                default:
                    Error.Report($"[PTool] Checker with {configuration.Mode} mode is currently unsupported.");
                    break;
            }
        }
        else
        {
            logger.WriteLine(".. Checking " + configuration.AssemblyToBeAnalyzed);

            // Creates and runs the testing process scheduler.
            switch (configuration.Mode)
            {
                case CheckerMode.BugFinding:
                    TestingProcessScheduler.Create(configuration).Run();
                    break;
                case CheckerMode.Verification:
                case CheckerMode.Coverage:
                    ExhaustiveEngine.Create(configuration).Run();
                    break;
                default:
                    Error.Report($"[PTool] Checker with {configuration.Mode} mode is currently unsupported.");
                    break;
            }

            logger.WriteLine(". Done");
        }
    }
}