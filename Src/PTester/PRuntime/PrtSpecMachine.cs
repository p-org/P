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
        public PrtSpecMachine Clone(StateImpl app)
        {
            var clonedMachine = MakeSkeleton();
            //base class fields
            clonedMachine.instanceNumber = this.instanceNumber;
            foreach (var fd in fields)
            {
                clonedMachine.fields.Add(fd.Clone());
            }
            clonedMachine.eventValue = this.eventValue;
            clonedMachine.stateStack = this.stateStack.Clone();
            clonedMachine.invertedFunStack = this.invertedFunStack.Clone();
            clonedMachine.continuation = this.continuation.Clone();
            clonedMachine.currentTrigger = this.currentTrigger;
            clonedMachine.currentPayload = this.currentPayload.Clone();

            clonedMachine.currentStatus = this.currentStatus;
            clonedMachine.nextSMOperation = this.nextSMOperation;
            clonedMachine.stateExitReason = this.stateExitReason;
            clonedMachine.sends = this.sends;
            clonedMachine.renamedName = this.renamedName;
            clonedMachine.isSafe = this.isSafe;
            clonedMachine.stateImpl = app;

            //spec class fields
            clonedMachine.observes = this.observes.ToList();
            clonedMachine.IsHot = this.IsHot;

            return clonedMachine;
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
                    case PrtNextStatemachineOperation.ExecuteFunctionOperation:
                        goto DoExecuteFunction;
                    case PrtNextStatemachineOperation.HandleEventOperation:
                        goto DoHandleEvent;
                    
                }

                DoExecuteFunction:
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
                            nextSMOperation = PrtNextStatemachineOperation.ExecuteFunctionOperation;
                            PrtPushExitFunction();
                            goto DoExecuteFunction;
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
                                        nextSMOperation = PrtNextStatemachineOperation.ExecuteFunctionOperation;
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
                                        nextSMOperation = PrtNextStatemachineOperation.ExecuteFunctionOperation;
                                        PrtPushTransitionFun(eventValue);
                                        goto DoExecuteFunction;
                                    }
                                case PrtStateExitReason.OnTransitionAfterExit:
                                    {
                                        PrtChangeState(CurrentState.transitions[eventValue].gotoState);
                                        hasMoreWork = true;
                                        nextSMOperation = PrtNextStatemachineOperation.ExecuteFunctionOperation;
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
                    nextSMOperation = PrtNextStatemachineOperation.ExecuteFunctionOperation;
                    eventValue = currEventValue;
                    PrtPushExitFunction();
                    goto DoExecuteFunction;
                }
                else if (PrtIsActionInstalled(currEventValue))
                {
                    goto DoAction;
                }
                else
                {
                    stateExitReason = PrtStateExitReason.OnUnhandledEvent;
                    nextSMOperation = PrtNextStatemachineOperation.ExecuteFunctionOperation;
                    eventValue = currEventValue;
                    PrtPushExitFunction();
                    goto DoExecuteFunction;
                }

                Finish:
                if (hasMoreWork)
                {
                    if (numOfStepsTaken > 100000)
                    {
                        throw new PrtInfiniteRaiseLoop("Infinite loop in spec machine");
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
