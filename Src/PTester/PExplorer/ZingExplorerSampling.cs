using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Zing
{
    /// <summary>
    /// This class performs naive random walk
    /// </summary>

    public class ZingExplorerNaiveRandomWalk : ZingExplorer
    {
        /// <summary>
        /// Parallel worker threads for performing search.
        /// </summary>
        private Task[] searchWorkers;

        public ZingExplorerNaiveRandomWalk()
            : base()
        {
        }

        protected override ZingerResult IterativeSearchStateSpace()
        {
            //outer loop to search the state space iteratively
            do
            {
                //Increment the iterative bound
                ZingerConfiguration.zBoundedSearch.IncrementIterativeBound();

                try
                {
                    searchWorkers = new Task[ZingerConfiguration.DegreeOfParallelism];
                    //create parallel search threads
                    for (int i = 0; i < ZingerConfiguration.DegreeOfParallelism; i++)
                    {
                        searchWorkers[i] = Task.Factory.StartNew(SearchStateSpace, i);
                        System.Threading.Thread.Sleep(10);
                    }

                    //Wait for all search workers to finish
                    Task.WaitAll(searchWorkers);
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

                ZingerStats.NumOfFrontiers = -1;
                ZingerStats.PrintPeriodicStats();
            }
            while (!ZingerConfiguration.zBoundedSearch.checkIfFinalCutOffReached());

            return ZingerResult.Success;
        }

        protected override void SearchStateSpace(object obj)
        {
            int myThreadId = (int)obj;
            int numberOfSchedulesExplored = 0;

            //frontier
            FrontierNode startfN = new FrontierNode(StartStateTraversalInfo);
            TraversalInfo startState = startfN.GetTraversalInfo(StartStateStateImpl, myThreadId);

            while (numberOfSchedulesExplored < ZingerConfiguration.MaxSchedulesPerIteration)
            {
                //increment the schedule count
                numberOfSchedulesExplored++;
                ZingerStats.IncrementNumberOfSchedules();
                //random walk always starts from the start state ( no frontier ).
                TraversalInfo currentState = startState;

                while (currentState.CurrentDepth < ZingerConfiguration.zBoundedSearch.IterativeCutoff)
                {
                    //kil the exploration if bug found
                    //Check if cancelation token triggered
                    if (CancelTokenZingExplorer.IsCancellationRequested)
                    {
                        //some task found bug and hence cancelling this task
                        return;
                    }

                    ZingerStats.MaxDepth = Math.Max(ZingerStats.MaxDepth, currentState.CurrentDepth);

                    //Check if the DFS Stack Overflow has occured.
                    if (currentState.CurrentDepth > ZingerConfiguration.BoundDFSStackLength)
                    {
                        
                        //update the safety traces
                        SafetyErrors.Add(currentState.GenerateNonCompactTrace());
                        // return value
                        this.lastErrorFound = ZingerResult.DFSStackOverFlowError;

                        throw new ZingerDFSStackOverFlow();
                    }

                    TraversalInfo nextSuccessor = currentState.GetNextSuccessorUniformRandomly();
                    ZingerStats.IncrementTransitionsCount();
                    ZingerStats.IncrementStatesCount();
                    if (nextSuccessor == null)
                    {
                        break;
                    }

                    TerminalState terminalState = nextSuccessor as TerminalState;
                    if (terminalState != null)
                    {
                        if (terminalState.IsErroneousTI)
                        {
                            lock (SafetyErrors)
                            {
                                // bugs found
                                SafetyErrors.Add(nextSuccessor.GenerateNonCompactTrace());
                                this.lastErrorFound = nextSuccessor.ErrorCode;
                            }

                            if (ZingerConfiguration.StopOnError)
                            {
                                CancelTokenZingExplorer.Cancel(true);
                                throw nextSuccessor.Exception;
                            }
                        }

                        break;
                    }

                    currentState = nextSuccessor;
                }
            }
        }

        protected override bool MustExplore(TraversalInfo ti)
        {
            throw new NotImplementedException();
        }

        protected override void VisitState(TraversalInfo ti)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// The explorer will perform uniform random walk over the delay bounded executions.
    /// </summary>
    public class ZingExplorerDelayBoundedSampling : ZingExplorer
    {

        /// <summary>
        /// Explores a deterministic schedule from the peek of the stack to the terminal state.
        /// </summary>
        protected void RunToCompletionWithDelayZero(Stack<TraversalInfo> searchStack)
        {
            var currentState = searchStack.Peek();

            while (currentState.CurrentDepth < ZingerConfiguration.MaxDepthPerSchedule)
            {
                ZingerStats.MaxDepth = Math.Max(ZingerStats.MaxDepth, currentState.CurrentDepth);
                TraversalInfo nextState = currentState.GetNextSuccessorUnderDelayZeroForRW();
                ZingerStats.IncrementTransitionsCount();
                ZingerStats.IncrementStatesCount();
                if (nextState == null)
                {
                    return;
                }

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

                TerminalState terminal = nextState as TerminalState;
                if (terminal != null)
                {
                    if (terminal.IsErroneousTI)
                    {
                        lock (SafetyErrors)
                        {
                            SafetyErrors.Add(nextState.GenerateNonCompactTrace());
                            this.lastErrorFound = nextState.ErrorCode;
                        }

                        if (ZingerConfiguration.StopOnError)
                        {
                            //Stop all tasks
                            CancelTokenZingExplorer.Cancel(true);
                            throw nextState.Exception;
                        }
                    }
                    return;
                }

                searchStack.Push(nextState);
                currentState = searchStack.Peek();
            }
        }

        protected int RandomBackTrackAndDelay(Stack<TraversalInfo> searchStack, int startPoint)
        {
            //back track the stack randomly to some point
            var backtrackAt = ZingerUtilities.rand.Next(startPoint, searchStack.Count() + 1);
            while (searchStack.Count() > backtrackAt)
            {
                searchStack.Pop();
            }
            //try to get to an execution state
            while (searchStack.Count > 1 && !(searchStack.Peek() is ExecutionState))
            {
                searchStack.Pop();
            }
            //delay at
            var currentState = searchStack.Peek();
            //delay the schedule
            var delayedState = (currentState as ExecutionState).GetDelayedSuccessor();

            if (delayedState == null)
            {
                return -1;
            }

            TerminalState terminal = delayedState as TerminalState;
            if (terminal != null)
            {
                if (terminal.IsErroneousTI)
                {
                    lock (SafetyErrors)
                    {
                        SafetyErrors.Add(delayedState.GenerateNonCompactTrace());
                        this.lastErrorFound = delayedState.ErrorCode;
                    }

                    if (ZingerConfiguration.StopOnError)
                    {
                        //Stop all tasks
                        CancelTokenZingExplorer.Cancel(true);
                        throw delayedState.Exception;
                    }
                }
                return -1;
            }
            //push it on stack
            searchStack.Push(delayedState);

            return searchStack.Count();
        }

        /// <summary>
        /// Parallel worker threads for performing search.
        /// </summary>
        private Task[] searchWorkers;

        protected override ZingerResult IterativeSearchStateSpace()
        {
            //outer loop to search the state space iteratively
            do
            {
                //Increment the iterative bound
                ZingerConfiguration.zBoundedSearch.IncrementIterativeBound();

                try
                {
                    //set number of schedules explored to 0
                    ZingerConfiguration.numberOfSchedulesExplored = 0;

                    searchWorkers = new Task[ZingerConfiguration.DegreeOfParallelism];
                    //create parallel search threads
                    for (int i = 0; i < ZingerConfiguration.DegreeOfParallelism; i++)
                    {
                        searchWorkers[i] = Task.Factory.StartNew(SearchStateSpace, i);
                        System.Threading.Thread.Sleep(10);
                    }

                    //Wait for all search workers to finish
                    Task.WaitAll(searchWorkers);
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

                ZingerStats.NumOfFrontiers = -1;
                ZingerStats.PrintPeriodicStats();
            }
            while (!ZingerConfiguration.zBoundedSearch.checkIfFinalCutOffReached());

            return ZingerResult.Success;
        }

        protected override void SearchStateSpace(object obj)
        {
            int myThreadId = (int)obj;
            //maximum number of schedules per iteration = c1 + c2^d.
            // c1 = ZingerConfiguration.MaxSchedulesPerIteration.
            // c2 = 3 as we found that to work the best.
            int maxSchedulesPerIteration = ZingerConfiguration.MaxSchedulesPerIteration + (int)Math.Pow(1.3, ZingerConfiguration.zBoundedSearch.IterativeCutoff);
            int delayBudget = 0;
            Stack<TraversalInfo> searchStack = new Stack<TraversalInfo>();
            //frontier
            FrontierNode startfN = new FrontierNode(StartStateTraversalInfo);
            TraversalInfo startState = startfN.GetTraversalInfo(StartStateStateImpl, myThreadId);

            while (ZingerConfiguration.numberOfSchedulesExplored < maxSchedulesPerIteration)
            {
                //kil the exploration if bug found
                //Check if cancelation token triggered
                if (CancelTokenZingExplorer.IsCancellationRequested)
                {
                    //some task found bug and hence cancelling this task
                    return;
                }

                delayBudget = ZingerConfiguration.zBoundedSearch.IterativeCutoff;

                //increment the schedule count
                Interlocked.Increment(ref ZingerConfiguration.numberOfSchedulesExplored);

                ZingerStats.IncrementNumberOfSchedules();

                searchStack = new Stack<TraversalInfo>();
                searchStack.Push(startState);
                int lastStartPoint = 1;
                while (delayBudget > 0)
                {
                    RunToCompletionWithDelayZero(searchStack);
                    lastStartPoint = RandomBackTrackAndDelay(searchStack, lastStartPoint);
                    if (lastStartPoint == -1)
                    {
                        break;
                    }
                    delayBudget--;
                }
            }
        }

        protected override bool MustExplore(TraversalInfo ti)
        {
            throw new NotImplementedException();
        }

        protected override void VisitState(TraversalInfo ti)
        {
            throw new NotImplementedException();
        }
    }
}