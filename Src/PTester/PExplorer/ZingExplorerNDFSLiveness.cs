using System.Collections.Generic;

namespace Microsoft.Zing
{
    public class ZingExplorerNDFSLiveness : ZingExplorer
    {
        /// <summary>
        /// Global hash table for storing the system state space during exploration.
        /// </summary>
        private ZingerStateTable GlobalStateTable;

        /// <summary>
        /// Current Accepting State
        /// </summary>
        private Fingerprint CurrAcceptingState;

        public ZingExplorerNDFSLiveness()
        {
            GlobalStateTable = new ZingerStateTable();
        }

        protected override ZingerResult IterativeSearchStateSpace()
        {
            //NDF does not work for Iterative DFS
            try
            {
                SearchStateSpace(0);
            }
            catch
            {
                return lastErrorFound;
            }

            return ZingerResult.Success;
        }

        protected override void SearchStateSpace(object obj)
        {
            var myThreadId = (int)obj;

            TraversalInfo startState = StartStateTraversalInfo.Clone();

            //Visit the current state and add it to the state table
            VisitState(startState);

            //create the search stack
            Stack<TraversalInfo> LocalSearchStack = new Stack<TraversalInfo>();
            LocalSearchStack.Push(startState);

            while (LocalSearchStack.Count > 0)
            {
                //start exploring the top of stack
                TraversalInfo currentState = LocalSearchStack.Peek();

                //Check if the DFS Stack Overflow has occured.
                if (LocalSearchStack.Count > ZingerConfiguration.BoundDFSStackLength)
                {
                    //BUG FOUND
                    //update the safety traces
                    SafetyErrors.Add(currentState.GenerateNonCompactTrace());
                    // return value
                    this.lastErrorFound = ZingerResult.DFSStackOverFlowError;

                    throw new ZingerDFSStackOverFlow();
                }

                //Explore Successors
                TraversalInfo nextState = currentState.GetNextSuccessor();
                //All successors explored already
                if (nextState == null)
                {
                    //since we are going to pop the stack, lets start the red search
                    if (!currentState.MagicBit && currentState.IsAcceptingState)
                    {
                        LocalSearchStack.Pop();
                        var redCurrentState = SetMagicBit(currentState);
                        LocalSearchStack.Push(redCurrentState);
                        CurrAcceptingState = currentState.Fingerprint;
                        continue;
                    }

                    LocalSearchStack.Pop();
                    continue;
                }

                if (nextState.MagicBit && nextState.Fingerprint.Equals(CurrAcceptingState) && nextState.IsAcceptingState)
                {
                    AcceptingCycles.Add(nextState.GenerateNonCompactTrace());
                    lastErrorFound = ZingerResult.AcceptanceCyleFound;
                    if (ZingerConfiguration.StopOnError)
                        throw new ZingerAcceptingCycleFound();
                }

                //Check if its a terminal state
                TerminalState terminalState = nextState as TerminalState;
                if (terminalState != null)
                {
                    if (terminalState.IsErroneousTI)
                    {
                        //BUG FOUND
                        //update the safety traces
                        SafetyErrors.Add(nextState.GenerateNonCompactTrace());
                        // return value
                        this.lastErrorFound = nextState.ErrorCode;

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

                    VisitState(nextState);
                    LocalSearchStack.Push(nextState);
                    continue;
                }
                else
                {
                    continue;
                }
            }
        }

        private TraversalInfo SetMagicBit(TraversalInfo ti)
        {
            var fp = ti.Fingerprint;
            StateData sD = GlobalStateTable.GetStateData(fp);
            sD.MagicBit = true;
            GlobalStateTable.AddOrUpdate(fp, sD);
            return ti.SetMagicbit();
        }

        protected override bool MustExplore(TraversalInfo ti)
        {
            ZingerStats.IncrementTransitionsCount();

            if (!ti.IsFingerPrinted)
                return true;

            Fingerprint fp = ti.Fingerprint;
            if (GlobalStateTable.Contains(fp))
            {
                var stateD = GlobalStateTable.GetStateData(fp);
                if (stateD.MagicBit == ti.MagicBit)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        protected override void VisitState(TraversalInfo ti)
        {
            if (!ti.IsFingerPrinted)
                return;

            Fingerprint fp = ti.Fingerprint;

            StateData newD = new StateData(ti.MagicBit);
            GlobalStateTable.AddOrUpdate(fp, newD);
        }
    }
}