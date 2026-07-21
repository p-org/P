// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PChecker.Feedback;

/// <summary>
/// Computes the scenario-coverage steering signal ("compliance") for the
/// feedback-guided search.
///
/// A naive signal — the maximum partial-progress fraction across ALL scenarios
/// this run — saturates at 1.0 whenever any easily-satisfied scenario is met
/// (which is essentially every run, since scenarios are auto-attached). A
/// constant signal applies a constant factor to <c>priority</c> and therefore
/// cannot re-order saved generators: it does not steer.
///
/// Instead we reward only <em>new coverage progress</em>: a run earns compliance
/// 1.0 iff it first-satisfies a scenario, or advances the furthest state reached
/// for a not-yet-satisfied scenario beyond the best seen so far in the suite.
/// This is sparse (most runs earn 0, so their priority stays == diversity) and
/// cannot saturate (once a scenario plateaus — including an unsatisfiable one at
/// its ceiling — it stops earning), so it genuinely biases the search toward
/// coverage gaps. The suite-so-far state is threaded by the caller and updated
/// in place, so the signal is deterministic under a fixed seed.
/// </summary>
public static class ScenarioSteering
{
    /// <summary>
    /// Returns 1.0 if this run made new coverage progress relative to the
    /// suite-so-far state (which is mutated to fold in this run), else 0.0.
    /// </summary>
    /// <param name="runReached">Distinct states reached this run, per scenario.</param>
    /// <param name="runSatisfied">Scenarios satisfied (reached a cold state) this run.</param>
    /// <param name="suiteBestReached">Best states-reached per scenario across the suite so far (mutated).</param>
    /// <param name="suiteSatisfied">Scenarios satisfied anywhere in the suite so far (mutated).</param>
    public static double NoveltyCompliance(
        IReadOnlyDictionary<string, int> runReached,
        IReadOnlyCollection<string> runSatisfied,
        IDictionary<string, int> suiteBestReached,
        ISet<string> suiteSatisfied)
    {
        double compliance = 0.0;

        // Strongest signal: the first run to satisfy a scenario anywhere in the suite.
        foreach (var scenario in runSatisfied)
        {
            if (suiteSatisfied.Add(scenario)) // Add returns true only if newly added.
            {
                compliance = 1.0;
            }
        }

        // Also reward advancing the furthest state of a not-yet-satisfied scenario.
        // Once a scenario is satisfied, diversity (not steering) drives finding more ways.
        foreach (var kv in runReached)
        {
            if (suiteSatisfied.Contains(kv.Key))
            {
                continue;
            }
            if (kv.Value > (suiteBestReached.TryGetValue(kv.Key, out var best) ? best : 0))
            {
                suiteBestReached[kv.Key] = kv.Value;
                compliance = 1.0;
            }
        }

        return compliance;
    }

    /// <summary>
    /// Whether entering a state marks its scenario satisfied. A scenario is satisfied by
    /// entering an accepting (cold) state (<paramref name="isInHotState"/> == false), EXCEPT
    /// on the monitor's very first (start) state entry — that entry is logged during
    /// RegisterMonitor, before any behavior is observed. Without this exception a
    /// <c>cold start state</c> scenario would be reported "covered" in every schedule with
    /// zero observed events (a coverage measurement that lies); with it, such a scenario is
    /// correctly a coverage gap until it re-enters a cold state through observed behavior.
    /// A hot (true) or unmarked/warm (null) state never satisfies.
    /// </summary>
    public static bool IsSatisfyingEntry(bool? isInHotState, bool isStartEntry)
        => isInHotState == false && !isStartEntry;
}
