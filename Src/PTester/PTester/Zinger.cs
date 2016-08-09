using System;

namespace Microsoft.Zing
{
    /// <summary>
    /// Main entry point for Zinger.
    /// </summary>
    internal class Zinger
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            //Parse Command Line and Initialize the ZingerConfiguration
            if (!ZingerCommandLine.ParseCommandLine(args))
            {
                Environment.Exit((int)ZingerResult.InvalidParameters);
            }
            //capture the commandline arguments for dronaworker
            if(ZingerConfiguration.DronacharyaEnabled)
            {
                ZingDronacharya.ZingerCommandline = args;
            }

            //Infer the Zinger Configuration and make sure all flags are set correctly.
            ZingerConfiguration.InferConfiguration();
            //Print the Current Configuration of Zinger
            ZingerConfiguration.PrintConfiguration();

            try
            {
                ZingerResult result;

                ZingExplorer zingExplorer;

                //check if dronacharya is enabled and this is the main worker
                if(ZingerConfiguration.DronacharyaEnabled && ZingerConfiguration.IsDronaMain)
                {
                    ZingDronacharya.PerformIterativeSearch();
                    return;
                }


                //currently the only search allowed with dronacharya enabled is exhaustive
                if(ZingerConfiguration.DronacharyaEnabled)
                {
                    zingExplorer = new ZingExplorerExhaustiveSearch();
                }
                else
                {
                    if (ZingerConfiguration.DoNDFSLiveness)
                        zingExplorer = new ZingExplorerNDFSLiveness();
                    else if (ZingerConfiguration.DoRandomSampling && !ZingerConfiguration.DoDelayBounding)
                        zingExplorer = new ZingExplorerNaiveRandomWalk();
                    else if (ZingerConfiguration.DoRandomSampling && ZingerConfiguration.DoDelayBounding)
                        zingExplorer = new ZingExplorerDelayBoundedSampling();
                    else
                        zingExplorer = new ZingExplorerExhaustiveSearch();
                }
               
                //start periodic stats
                if (ZingerConfiguration.PrintStats)
                {
                    ZingerStats.StartPeriodicStats();
                }

                //start the time out timer 
                ZingerUtilities.StartTimeOut();

                //start the search
                if(ZingerConfiguration.DronacharyaEnabled && !ZingerConfiguration.IsDronaMain)
                {
                    result = zingExplorer.ExploreWithDronacharya();
                }
                else
                {
                    result = zingExplorer.Explore();
                }

                //Print Final Stats
                ZingerStats.PrintFinalStats();

                //Close operations
                if (ZingerConfiguration.PrintStats)
                {
                    ZingerStats.StopPeriodicStats();
                }

                Environment.Exit((int)result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception from Zing model checker:");
                Console.WriteLine(e);
                Environment.Exit((int)ZingerResult.ZingRuntimeError);
            }
        }
    }
}