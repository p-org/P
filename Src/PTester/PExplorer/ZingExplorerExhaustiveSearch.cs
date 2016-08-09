using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Microsoft.Zing
{
    public class ZingExplorerExhaustiveSearch : ZingExplorer
    {
        /// <summary>
        /// Global hash table for storing the system state space during exploration.
        /// </summary>
        private ZingerStateTable GlobalStateTable;

        /// <summary>
        /// Store frontiers after each iteration.
        /// </summary>
        private FrontierSet GLobalFrontierSet;

        /// <summary>
        /// Parallel Worker threads for Performing search
        /// </summary>
        private Task[] searchWorkers;

        public ZingExplorerExhaustiveSearch()
            : base()
        {
            GlobalStateTable = new ZingerStateTable();
            GLobalFrontierSet = new FrontierSet(StartStateTraversalInfo);
        }

        protected override ZingerResult IterativeSearchStateSpace()
        {
            //outer loop to search the state space Iteratively
            do
            {
                //Increment the iterative bound
                ZingerConfiguration.zBoundedSearch.IncrementIterativeBound();

                //call the frontier reset function
                GLobalFrontierSet.StartOfIterationReset();

                try
                {
                    searchWorkers = new Task[ZingerConfiguration.DegreeOfParallelism];
                    //create parallel search threads
                    for (int i = 0; i < ZingerConfiguration.DegreeOfParallelism; i++)
                    {
                        searchWorkers[i] = Task.Factory.StartNew(SearchStateSpace, i);
                        System.Threading.Thread.Sleep(10);
                    }

                    // Wait for all readers to Finish
                    GLobalFrontierSet.WaitForAllReaders(CancelTokenZingExplorer.Token);
                    // Wait for all search workers to Finish
                    Task.WaitAll(searchWorkers);
                    // Wait for all writer to Finish
                    GLobalFrontierSet.WaitForAllWriters(CancelTokenZingExplorer.Token);
                    //For Debug
                    //GLobalFrontierSet.PrintAll();
                }
                catch (AggregateException ex)
                {
                    foreach (var inner in ex.InnerExceptions)
                    {
                        if ((inner is ZingException))
                        {
                            return lastErrorFound;
                        }
                        else
                        {
                            ZingerUtilities.PrintErrorMessage("Unknown Exception in Zing:");
                            ZingerUtilities.PrintErrorMessage(inner.ToString());
                            return ZingerResult.ZingRuntimeError;
                        }
                    }
                }

                ZingerStats.NumOfFrontiers = GLobalFrontierSet.Count();
                ZingerStats.PrintPeriodicStats();
            }
            while (GLobalFrontierSet.Count() > 0 && !ZingerConfiguration.zBoundedSearch.checkIfFinalCutOffReached());

            
            /*
            if (lastErrorFound != ZingerResult.Success)
                return ZingerResult.Assertion;
            else
                return ZingerResult.Success;
            */
            return lastErrorFound;
        }

        protected override void SearchStateSpace(object obj)
        {
            int myThreadId = (int)obj;

            while (!GLobalFrontierSet.IsCompleted())
            {
                FrontierNode fNode = GLobalFrontierSet.GetNextFrontier();
                if (fNode == null)
                {
                    //Taking item from the global frontier failed
                    Contract.Assert(GLobalFrontierSet.IsCompleted());
                    continue;
                }
                TraversalInfo startState = fNode.GetTraversalInfo(StartStateStateImpl, myThreadId);

                //Visit the current state (add it to state table in the case of state ful search)
                VisitState(startState);

                //create search stack
                Stack<TraversalInfo> LocalSearchStack = new Stack<TraversalInfo>();
                LocalSearchStack.Push(startState);

                //do bounded dfs using the local search stack
                while (LocalSearchStack.Count > 0)
                {
                    //Check if cancelation token triggered
                    if (CancelTokenZingExplorer.IsCancellationRequested)
                    {
                        //some task found bug and hence cancelling this task
                        return;
                    }

                    //start exploring the top of stack
                    TraversalInfo currentState = LocalSearchStack.Peek();

                    //update the maximum depth
                    ZingerStats.MaxDepth = Math.Max(ZingerStats.MaxDepth, currentState.CurrentDepth);
                    //Check if the DFS Stack Overflow has occured.
                    if (currentState.CurrentDepth > ZingerConfiguration.BoundDFSStackLength)
                    {
                        //BUG FOUND
                        //update the safety traces
                        SafetyErrors.Add(currentState.GenerateNonCompactTrace());
                        // return value
                        this.lastErrorFound = ZingerResult.DFSStackOverFlowError;

                        throw new ZingerDFSStackOverFlow();
                    }

                    //Add current state to frontier if the bound is greater than the cutoff and we have fingerprinted the state (its not a single successor state)
                    if (ZingerConfiguration.zBoundedSearch.checkIfIterativeCutOffReached(currentState.zBounds) && currentState.IsFingerPrinted)
                    {
                        GLobalFrontierSet.Add(currentState);
                        //since current state is add to the frontier; pop it
                        LocalSearchStack.Pop();

                        continue;
                    }

                    // OK. Current state is not at the frontier cutoff so lets explore further
                    TraversalInfo nextState = currentState.GetNextSuccessor();

                    //All successors explored already
                    if (nextState == null)
                    {
                        //since all successors explored pop the stack
                        LocalSearchStack.Pop();
                        continue;
                    }

                    //Check if its a terminal state
                    TerminalState terminalState = nextState as TerminalState;
                    if (terminalState != null)
                    {
                        if (terminalState.IsErroneousTI)
                        {
                            // If the exception is ZingInvokeMotionPlanning then handle it
                            // by passing it to ZingDronacharya
                            if(nextState.ErrorCode == ZingerResult.ZingerMotionPlanningInvocation)
                            {
                                if(ZingerConfiguration.DronacharyaEnabled)
                                {
                                    // return value
                                    this.lastErrorFound = nextState.ErrorCode;
                                    ZingerConfiguration.ZDronacharya.GenerateMotionPlanFor(nextState);
                                }
                                continue;
                            }

                            lock (SafetyErrors)
                            {
                                //BUG FOUND
                                //update the safety traces
                                SafetyErrors.Add(nextState.GenerateNonCompactTrace());
                                // return value
                                this.lastErrorFound = nextState.ErrorCode;
                            }

                            //find all errors ??
                            if (ZingerConfiguration.StopOnError)
                            {
                                //Stop all tasks
                                CancelTokenZingExplorer.Cancel(true);
                                throw nextState.Exception;
                            }
                        }

                        //else continue
                        continue;
                    }

                    if (MustExplore(nextState))
                    {
                        // Ensure that states that are at cutoff are not added to the state table
                        // Since they will be added to the Frontier for the next iteration.
                        if (!ZingerConfiguration.zBoundedSearch.checkIfIterativeCutOffReached(nextState.zBounds))
                        {
                            VisitState(nextState);
                        }

                        LocalSearchStack.Push(nextState);
                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        protected override bool MustExplore(TraversalInfo ti)
        {
            //Increment the number of transitions executed
            ZingerStats.IncrementTransitionsCount();

            if (!ti.IsFingerPrinted)
            {
                return true;
            }
            //else

            Fingerprint fp = ti.Fingerprint;

            //check if this is in the frontier

            //no need to explore frontier state if already explored
            if (GLobalFrontierSet.Contains(fp))
            {
                return false;
            }
            else
            {
                if (!GlobalStateTable.Contains(fp))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        protected override void VisitState(TraversalInfo ti)
        {
            if (!ti.IsFingerPrinted)
                return;

            Fingerprint fp = ti.Fingerprint;
            if (GLobalFrontierSet.Contains(fp) && !ZingerConfiguration.DoDelayBounding)
            {
                GLobalFrontierSet.Remove(fp);
            }
            if (!GlobalStateTable.Contains(fp))
            {
                GlobalStateTable.AddOrUpdate(fp, null);
            }
        }
    }
}