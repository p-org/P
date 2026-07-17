using System;
using System.Collections.Generic;
using PChecker.SystematicTesting;

namespace PChecker.Runtime
{
    public class PModule
    {
        public static Dictionary<string, Type> interfaceDefinitionMap = new Dictionary<string, Type>();
        public static Dictionary<string, List<Type>> monitorMap = new Dictionary<string, List<Type>>();
        public static Dictionary<string, List<string>> monitorObserves = new Dictionary<string, List<string>>();

        /// <summary>
        /// Monitor types declared with the <c>scenario</c> keyword. These are "coverage"
        /// monitors: their satisfaction (reaching an accepting/cold state) is counted for
        /// scenario coverage, and they are exempt from the end-of-run liveness check
        /// (an unsatisfied scenario is not a bug, just uncovered).
        /// </summary>
        public static HashSet<Type> coverageMonitors = new HashSet<Type>();

        /// <summary>
        /// Total number of states in each scenario (coverage) monitor, used to report
        /// partial coverage (distinct states reached / total states).
        /// </summary>
        public static Dictionary<Type, int> scenarioStateCounts = new Dictionary<Type, int>();

        public static IDictionary<string, Dictionary<string, string>> linkMap =
            new Dictionary<string, Dictionary<string, string>>();

        public static ControlledRuntime runtime;
    }
}