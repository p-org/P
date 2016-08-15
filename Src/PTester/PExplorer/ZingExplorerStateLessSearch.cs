using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Zing
{
    public class ZingExplorerStateLessSearch : ZingExplorer
    {
        /// <summary>
        /// Stores the global frontier after each iteration.
        /// </summary>
        private FrontierSet GlobalFrontierSet;

        private int maxSearchDepth;

        /// <summary>
        /// Parallel worker threads for performing the search
        /// </summary>
        private Task[] searchWorkers;

        public ZingExplorerStateLessSearch()
            : base()
        {
            GlobalFrontierSet = new FrontierSet(StartStateTraversalInfo);
            maxSearchDepth = ZingerConfiguration.MaxSchedulesPerIteration;
        }

        protected override ZingerResult IterativeSearchStateSpace()
        {
            //outer loop to search the state space Iteratively
            do
            {
                //Increment the iterative bound
                ZingerConfiguration.zBoundedSearch.IncrementIterativeBound();

                //call the frontier reset function
                GlobalFrontierSet.StartOfIterationReset();

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
                    GlobalFrontierSet.WaitForAllReaders(CancelTokenZingExplorer.Token);
                    // Wait for all search workers to Finish
                    Task.WaitAll(searchWorkers);
                    // Wait for all writer to Finish
                    GlobalFrontierSet.WaitForAllWriters(CancelTokenZingExplorer.Token);
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

                ZingerStats.NumOfFrontiers = GlobalFrontierSet.Count();
                ZingerStats.PrintPeriodicStats();
            }
            while (GlobalFrontierSet.Count() > 0 && !ZingerConfiguration.zBoundedSearch.checkIfFinalCutOffReached());

            return ZingerResult.Success;
        }

        private bool SearchStackContains(Stack<TraversalInfo> stack, TraversalInfo ti)
        {
            if (!ti.IsFingerPrinted)
            {
                return false;
            }

            Fingerprint fp = ti.Fingerprint;

            var contains = stack.Where(t => (t.IsFingerPrinted && t.Fingerprint == fp)).Count() > 0;
            return contains;
        }

        protected override bool MustExplore(TraversalInfo ti)
        {
            //Increment the number of transitions executed
            ZingerStats.IncrementTransitionsCount();

            if (!ti.IsFingerPrinted)
            {
                return true;
            }

            Fingerprint fp = ti.Fingerprint;
            if (GlobalFrontierSet.Contains(fp) || ti.CurrentDepth > maxSearchDepth)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        protected override void VisitState(TraversalInfo ti)
        {
            throw new NotImplementedException();
        }

        protected override void SearchStateSpace(object obj)
        {
            int myThreadId = (int)obj;
            while (!GlobalFrontierSet.IsCompleted())
            {
                FrontierNode fNode = GlobalFrontierSet.GetNextFrontier();
                if (fNode == null)
                {
                    //Taking item from the global frontier failed
                    Contract.Assert(GlobalFrontierSet.IsCompleted());
                    continue;
                }
                TraversalInfo startState = fNode.GetTraversalInfo(StartStateStateImpl, myThreadId);

                //Check if we need to explore the current frontier state
                if (!MustExplore(startState) && !ZingerConfiguration.DoDelayBounding)
                {
                    continue;
                }

                if (ZingerConfiguration.zBoundedSearch.checkIfIterativeCutOffReached(fNode.Bounds))
                {
                    GlobalFrontierSet.Add(startState);
                    continue;
                }

                //create search stack
                Stack<TraversalInfo> LocalSearchStack = new Stack<TraversalInfo>();
                LocalSearchStack.Push(startState);

                //dp local bounded dfs using the local search stack
                while (LocalSearchStack.Count() > 0)
                {
                    //check if cancellation token is triggered
                    if (CancelTokenZingExplorer.IsCancellationRequested)
                    {
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
                        GlobalFrontierSet.Add(currentState);
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

                    if (MustExplore(nextState) && !SearchStackContains(LocalSearchStack, nextState))
                    {
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
    }
}