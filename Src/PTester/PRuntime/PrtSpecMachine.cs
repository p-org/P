using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace P.Runtime
{
    public abstract class PrtSpecMachine : PrtMachine
    {
        #region Fields
        public List<PrtEventValue> observes;
        public bool IsHot;
        #endregion

        public abstract PrtSpecMachine MakeSkeleton();

        public PrtSpecMachine() : base()
        {
            observes = new List<PrtEventValue>();
            IsHot = false;
        }

        public PrtSpecMachine(StateImpl app) : base()
        {
            observes = new List<PrtEventValue>();
            IsHot = false;
            stateImpl = app;
            //Push the start state function on the funStack.
            PrtPushState(StartState);
            //Execute the entry function
            PrtEnqueueEvent(PrtValue.@null, PrtValue.@null, null);
        }
        public object Clone()
        {
            var clonedMonitor = MakeSkeleton();
            foreach (var fd in fields)
            {
                clonedMonitor.fields.Add(fd.Clone());
            }
            clonedMonitor.stateStack = this.stateStack.Clone();
            clonedMonitor.nextSMOperation = this.nextSMOperation;
            clonedMonitor.stateExitReason = this.stateExitReason;
            clonedMonitor.stateImpl = this.stateImpl;
            return clonedMonitor;
        }

        public override void PrtEnqueueEvent(PrtValue e, PrtValue arg, PrtMachine source, PrtMachineValue target = null)
        {
            int numOfStepsTaken = 0;
            // set the currentTrigger and currentPayload fields
            // so that we can reuse the common functions
            currentPayload = arg.Clone();
            currentTrigger = e;

            PrtValue currEventValue;
            PrtFun currAction;
            bool hasMoreWork = false;
            try
            {
                Start:
                switch (nextSMOperation)
                {
                    case PrtNextStatemachineOperation.EntryOperation:
                        goto DoEntry;
                    case PrtNextStatemachineOperation.HandleEventOperation:
                        goto DoHandleEvent;
                    
                }

                DoEntry:
                if (invertedFunStack.TopOfStack == null)
                {
                    //Trace: entered state
                    if (CurrentState.entryFun.IsAnonFun)
                        PrtPushFunStackFrame(CurrentState.entryFun, CurrentState.entryFun.CreateLocals(currentPayload));
                    else
                        PrtPushFunStackFrame(CurrentState.entryFun, CurrentState.entryFun.CreateLocals());
                }
                //invoke the function
                invertedFunStack.TopOfStack.fun.Execute(stateImpl, this);
                goto CheckFunLastOperation;

                DoAction:
                currAction = PrtFindActionHandler(eventValue);
                if (currAction == PrtFun.IgnoreFun)
                {
                    //Trace: Performed ignore action for the event
                    currentTrigger = PrtValue.@null;
                    currentPayload = PrtValue.@null;
                }
                else
                {
                    if (invertedFunStack.TopOfStack == null)
                    {
                        //Trace: executed the action handler for event
                        PrtPushFunStackFrame(currAction, currAction.CreateLocals(currentPayload));
                    }
                    //invoke the action handler
                    invertedFunStack.TopOfStack.fun.Execute(stateImpl, this);
                }
                goto CheckFunLastOperation;

                CheckFunLastOperation:
                switch (continuation.reason)
                {
                    case PrtContinuationReason.Goto:
                        {
                            stateExitReason = PrtStateExitReason.OnGotoStatement;
                            PrtExecuteExitFunction();
                            goto CheckFunLastOperation;
                        }
                    case PrtContinuationReason.Raise:
                        {
                            nextSMOperation = PrtNextStatemachineOperation.HandleEventOperation;
                            hasMoreWork = true;
                            goto Finish;
                        }
                    case PrtContinuationReason.Return:
                        {
                            switch (stateExitReason)
                            {
                                case PrtStateExitReason.NotExit:
                                    {
                                        nextSMOperation = PrtNextStatemachineOperation.HandleEventOperation;
                                        hasMoreWork = false;
                                        goto Finish;
                                    }
                                case PrtStateExitReason.OnGotoStatement:
                                    {
                                        PrtChangeState(destOfGoto);
                                        nextSMOperation = PrtNextStatemachineOperation.EntryOperation;
                                        stateExitReason = PrtStateExitReason.NotExit;
                                        hasMoreWork = true;
                                        goto Finish;
                                    }
                                case PrtStateExitReason.OnUnhandledEvent:
                                    {
                                        hasMoreWork = !PrtPopState(false);
                                        nextSMOperation = PrtNextStatemachineOperation.HandleEventOperation;
                                        stateExitReason = PrtStateExitReason.NotExit;
                                        goto Finish;
                                    }
                                case PrtStateExitReason.OnTransition:
                                    {
                                        stateExitReason = PrtStateExitReason.OnTransitionAfterExit;
                                        PrtExecuteTransitionFun(eventValue);
                                        goto CheckFunLastOperation;
                                    }
                                case PrtStateExitReason.OnTransitionAfterExit:
                                    {
                                        PrtChangeState(CurrentState.transitions[eventValue].gotoState);
                                        hasMoreWork = true;
                                        nextSMOperation = PrtNextStatemachineOperation.EntryOperation;
                                        stateExitReason = PrtStateExitReason.NotExit;
                                        goto Finish;
                                    }
                                default:
                                    {
                                        Debug.Assert(false, "Unexpected value for exit reason");
                                        goto Finish;
                                    }
                            }

                        }
                    default:
                        {
                            Debug.Assert(false, "Unexpected value for continuation reason");
                            goto Finish;
                        }
                }

                DoHandleEvent:
                if (!currentTrigger.Equals(PrtValue.@null))
                {
                    currEventValue = currentTrigger;
                    currentTrigger = PrtValue.@null;
                }
                else
                {
                    currEventValue = eventValue;
                }

                if (PrtIsTransitionPresent(currEventValue))
                {
                    stateExitReason = PrtStateExitReason.OnTransition;
                    eventValue = currEventValue;
                    PrtExecuteExitFunction();
                    goto CheckFunLastOperation;
                }
                else if (PrtIsActionInstalled(currEventValue))
                {
                    goto DoAction;
                }
                else
                {
                    stateExitReason = PrtStateExitReason.OnUnhandledEvent;
                    eventValue = currEventValue;
                    PrtExecuteExitFunction();
                    goto CheckFunLastOperation;
                }

                Finish:
                if (hasMoreWork)
                {
                    if (numOfStepsTaken > 100000)
                    {
                        throw new PrtInfiniteRaiseLoop("Infinite loop in monitor");
                    }
                    else
                    {
                        numOfStepsTaken++;
                        goto Start;
                    }
                }
                else
                {
                    return;
                }
            }
            catch (PrtException ex)
            {
                stateImpl.Exception = ex;
            }
        }
    }
}
