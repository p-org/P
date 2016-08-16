using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace P.Runtime
{
    public enum PrtMachineStatus
    {
        Enabled,        // The state machine is enabled
        Blocked,        // The state machine is blocked on a dequeue or receive
        Halted,         // The state machine has halted
    };

    public enum PrtNextStatemachineOperation
    {
        EntryOperation,
        DequeueOperation,
        HandleEventOperation,
        ReceiveOperation
    };

    public enum PrtStateExitReason
    {
        NotExit,
        OnTransition,
        OnTransitionAfterExit,
        OnPopStatement,
        OnGotoStatement,
        OnUnhandledEvent
    };
    public enum PrtDequeueReturnStatus { SUCCESS, NULL, BLOCKED };

    public abstract class PrtMachine
    {
        #region Fields
        public int instanceNumber;
        public List<PrtValue> machineFields;
        private PrtEvent eventValue;
        private PrtStateStack stateStack;
        private PrtFunStack invertedFunStack;
        private PrtContinuation continuation;
        public PrtEvent currentTrigger;
        public PrtValue currentPayload;
        public PrtEventBuffer eventQueue;
        public HashSet<PrtEvent> receiveSet;
        public PrtMachineStatus currentStatus;
        private PrtNextStatemachineOperation nextSMOperation;
        private PrtStateExitReason stateExitReason;
        public int maxBufferSize;
        public PrtState destOfGoto;
        //just a reference to stateImpl
        private StateImpl StateImpl;
        #endregion

        #region Clone and Undo
        public PrtMachine Clone() { throw new NotImplementedException(); }
        #endregion

        #region Constructor
        public PrtMachine(StateImpl app, int maxBuff)
        {
            this.instanceNumber = this.NextInstanceNumber(app);
            this.machineFields = new List<PrtValue>();
            this.eventValue = null;
            this.stateStack = new PrtStateStack();
            this.invertedFunStack = new PrtFunStack();
            this.continuation = new PrtContinuation();
            this.currentTrigger = null;
            this.currentPayload = PrtValue.NullValue;
            this.eventQueue = new PrtEventBuffer();
            this.receiveSet = new HashSet<PrtEvent>();
            this.currentStatus = PrtMachineStatus.Enabled;
            this.nextSMOperation = PrtNextStatemachineOperation.EntryOperation;
            this.stateExitReason = PrtStateExitReason.NotExit;
            this.maxBufferSize = maxBuff;
            StateImpl = app;

            //Push the start state function on the funStack.
            stateStack.PushStackFrame(StartState);
        }
        #endregion

        #region getters and setters
        public abstract string Name
        {
            get;
        }

        public abstract PrtState StartState
        {
            get;
        }

        public abstract int NextInstanceNumber(StateImpl app);

        public PrtState CurrentState
        {
            get
            {
                return stateStack.TopOfStack.state;
            }
        }
        #endregion

        #region State machine helper functions
        public PrtFun PrtFindActionHandler(PrtEvent ev)
        {
            var tempStateStack = stateStack.Clone();
            while (tempStateStack.TopOfStack != null)
            {
                if (tempStateStack.TopOfStack.state.dos.ContainsKey(ev))
                {
                    return tempStateStack.TopOfStack.state.dos[ev];
                }
                else
                    tempStateStack.PopStackFrame();
            }
            Debug.Assert(false);
            return null;
        }

        public void PrtPushState(PrtState s)
        {
            stateStack.PushStackFrame(s);
        }

        public bool PrtPopState(bool isPopStatement)
        {
            Debug.Assert(stateStack.TopOfStack != null);
            //pop stack
            stateStack.PopStackFrame();
            
            if(stateStack.TopOfStack == null)
            {
                if(isPopStatement)
                {
                    throw new PrtInvalidPopStatement();
                }
                else if(eventValue != PrtEvent.HaltEvent)
                {
                    throw new PrtUnhandledEventException();
                }
                else
                {
                    currentStatus = PrtMachineStatus.Halted;
                }
            }

            return currentStatus == PrtMachineStatus.Halted;

        }

        public void PrtChangeState(PrtState s)
        {
            Debug.Assert(stateStack.TopOfStack != null);
            stateStack.PopStackFrame();
            stateStack.PushStackFrame(s);
        }

        public PrtFunStackFrame PrtPopFunStackFrame()
        {
            return invertedFunStack.PopFun();
        }

        public void PrtPushFunStackFrame(PrtFun fun, List<PrtValue> local)
        {
            invertedFunStack.PushFun(fun, local);
        }

        public void PrtPushFunStackFrame(PrtFun fun, List<PrtValue> local, int retTo)
        {
            invertedFunStack.PushFun(fun, local, retTo);
        }

        public void PrtResetTriggerAndPayload()
        {
            currentTrigger = null;
            currentPayload = null;
        }

        public void PrtEnqueueEvent(StateImpl application, PrtEvent e, PrtValue arg, PrtMachine source)
        {
            PrtType prtType;

            if (e == null)
            {
                throw new PrtIllegalEnqueueException("Enqueued event must be non-null");
            }

            //assertion to check if argument passed inhabits the payload type.
            prtType = e.payloadType;

            if ((arg.type.typeKind == PrtTypeKind.PRT_KIND_NULL)
                || (prtType.typeKind != PrtTypeKind.PRT_KIND_NULL && !PrtValue.PrtInhabitsType(arg, prtType)))
            {
                throw new PrtInhabitsTypeException(String.Format("Type of payload <{0}> does not match the expected type <{1}> with event <{2}>", arg.type.ToString(), prtType.ToString(), e.name));
            }

            if (currentStatus == PrtMachineStatus.Halted)
            {
                application.Trace(
                    @"<EnqueueLog> {0}-{1} Machine has been halted and Event {2} is dropped",
                    this.Name, this.instanceNumber, e.name);
            }
            else
            {
                if (arg != null)
                {
                    application.Trace(
                        @"<EnqueueLog> Enqueued Event <{0}, {1}> in {2}-{3} by {4}-{5}",
                        e.name, arg.ToString(), this.Name, this.instanceNumber, source.Name, source.instanceNumber);
                }
                else
                {
                    application.Trace(
                        @"<EnqueueLog> Enqueued Event < {0} > in {1}-{2} by {3}-{4}",
                        e.name, this.Name, this.instanceNumber, source.Name, source.instanceNumber);
                }

                this.eventQueue.EnqueueEvent(e, arg);
                if (this.maxBufferSize != -1 && this.eventQueue.Size() > this.maxBufferSize)
                {
                    throw new PrtMaxBufferSizeExceededException(
                        String.Format(@"<EXCEPTION> Event Buffer Size Exceeded {0} in Machine {1}-{2}",
                        this.maxBufferSize, this.Name, this.instanceNumber));
                }
                if (currentStatus == PrtMachineStatus.Blocked && this.eventQueue.IsEnabled(this))
                {
                    currentStatus = PrtMachineStatus.Enabled;
                }
            }
        }

        public PrtDequeueReturnStatus PrtDequeueEvent(StateImpl application, bool hasNullTransition)
        {
            currentTrigger = null;
            currentPayload = null;
            eventQueue.DequeueEvent(this);
            if (currentTrigger != null)
            {
                if (currentPayload == null)
                {
                    throw new PrtInternalException("Internal error: currentArg is null");
                }
                if (currentStatus == PrtMachineStatus.Blocked)
                {
                    throw new PrtInternalException("Internal error: Tyring to execute blocked machine");
                }

                application.Trace(
                    "<DequeueLog> Dequeued Event < {0}, {1} > at Machine {2}-{3}\n",
                    currentTrigger.name, currentPayload.ToString(), Name, instanceNumber);
                receiveSet = new HashSet<PrtEvent>();
                return PrtDequeueReturnStatus.SUCCESS;
            }
            else if (hasNullTransition || receiveSet.Contains(currentTrigger))
            {
                if (currentStatus == PrtMachineStatus.Blocked)
                {
                    throw new PrtInternalException("Internal error: Tyring to execute blocked machine");
                }
                application.Trace(
                    "<NullTransLog> Null transition taken by Machine {0}-{1}\n",
                    Name, instanceNumber);
                currentPayload = PrtValue.NullValue;
                receiveSet = new HashSet<PrtEvent>();
                return PrtDequeueReturnStatus.NULL;
            }
            else
            {
                if (currentStatus == PrtMachineStatus.Blocked)
                {
                    throw new PrtAssumeFailureException();
                }
                currentStatus = PrtMachineStatus.Blocked;
                if (application.Deadlock)
                {
                    throw new PrtDeadlockException("Deadlock detected");
                }
                return PrtDequeueReturnStatus.BLOCKED;
            }
        }

        public void PrtExecuteExitFunction()
        {
            //Shaz: exit functions do not take any arguments so is this current ??
            PrtPushFunStackFrame(CurrentState.exitFun, CurrentState.exitFun.CreateLocals());
            invertedFunStack.TopOfStack.fun.Execute(StateImpl, this);
        }

        public void PrtExecuteReceiveCase(PrtEvent ev)
        {
            var currRecIndex = continuation.receiveIndex;
            var currFun = invertedFunStack.TopOfStack.fun.receiveCases[currRecIndex][ev];
            PrtPushFunStackFrame(currFun, currFun.CreateLocals(currentPayload));
            currFun.Execute(StateImpl, this);
        }

        public bool PrtIsPushTransitionPresent(PrtEvent ev)
        {
            if (CurrentState.transitions.ContainsKey(ev))
                return CurrentState.transitions[ev].isPushTran;
            else
                return false;
        }

        public bool PrtIsTransitionPresent(PrtEvent ev)
        {
            return CurrentState.transitions.ContainsKey(ev);
        }

        public bool PrtIsActionInstalled(PrtEvent ev)
        {
            return CurrentState.dos.ContainsKey(ev);
        }

        public bool PrtHasNullReceiveCase()
        {
            return receiveSet.Contains(PrtEvent.NullEvent);
        }

        public void PrtExecuteTransitionFun(PrtEvent ev)
        {
            // Shaz: Figure out how to handle the transfer stuff for payload !!!
            PrtPushFunStackFrame(CurrentState.transitions[ev].transitionFun, CurrentState.transitions[ev].transitionFun.CreateLocals(currentPayload));
            invertedFunStack.TopOfStack.fun.Execute(StateImpl, this);
        }

        public void PrtFunContReturn(List<PrtValue> retLocals)
        {
            continuation.reason = PrtContinuationReason.Return;
            continuation.retVal = PrtValue.NullValue;
            continuation.retLocals = retLocals;
        }

        public void PrtFunContReturnVal(PrtValue val, List<PrtValue> retLocals)
        {
            continuation.reason = PrtContinuationReason.Return;
            continuation.retVal = val;
            continuation.retLocals = retLocals;
        }

        public void PrtFunContPop()
        {
            continuation.reason = PrtContinuationReason.Pop;
        }

        public void PrtFunContRaise()
        {
            continuation.reason = PrtContinuationReason.Raise;
        }

        public void PrtFunContSend(PrtFun fun, List<PrtValue> locals, int ret)
        {
            PrtPushFunStackFrame(fun, locals, ret);
            continuation.reason = PrtContinuationReason.Send;
        }

        void PrtFunContNewMachine(PrtFun fun, List<PrtValue> locals, PrtMachine o, int ret)
        {
            PrtPushFunStackFrame(fun, locals, ret);
            continuation.reason = PrtContinuationReason.NewMachine;
            continuation.createdMachine = o;
        }

        void PrtFunContReceive(PrtFun fun, List<PrtValue> locals, int ret)
        {
            PrtPushFunStackFrame(fun, locals, ret);
            continuation.reason = PrtContinuationReason.Receive;
            
        }

        void PrtFunContNondet(PrtFun fun, List<PrtValue> locals, int ret)
        {
            PrtPushFunStackFrame(fun, locals, ret);
            continuation.reason = PrtContinuationReason.Nondet;
        }
        #endregion


        public void PrtRunStateMachine()
        {
            int numOfStepsTaken = 0;
            Debug.Assert(currentStatus == PrtMachineStatus.Enabled, "Invoked PrtRunStateMachine on a blocked or a halted machine");

            try
            {
                while (PrtStepStateMachine())
                {
                    if (numOfStepsTaken > 100000)
                    {
                        throw new PrtInfiniteRaiseLoop();
                    }                   
                    numOfStepsTaken++;
                }
            }
            catch(PrtException ex)
            {
                StateImpl.Exception = ex;
            }
            
        }
        
        public bool PrtStepStateMachine()
        {
            PrtEvent currEventValue;
            PrtFun currAction;
            bool hasMoreWork = false;

            switch(nextSMOperation)
            {
                case PrtNextStatemachineOperation.EntryOperation:
                    goto DoEntry;
                case PrtNextStatemachineOperation.DequeueOperation:
                    goto DoDequeue;
                case PrtNextStatemachineOperation.HandleEventOperation:
                    goto DoHandleEvent;
                case PrtNextStatemachineOperation.ReceiveOperation:
                    goto DoReceive;
            }

            DoEntry:
            /*
             * Note that we have made an assumption that when a state is pushed on state stack or a transition is taken (update to a state)
             * the action set and deferred set is updated appropriately
            */
            if(invertedFunStack.TopOfStack == null)
            {
                //Trace: entered state
                //Shaz: Is the following this correct, how do we pass the payload to entry function.
                PrtPushFunStackFrame(CurrentState.entryFun, CurrentState.entryFun.CreateLocals(currentPayload));
            }
            //invoke the function
            invertedFunStack.TopOfStack.fun.Execute(StateImpl, this);
            goto CheckFunLastOperation;

            DoAction:
            currAction = PrtFindActionHandler(eventValue);
            if(currAction == PrtCommonFunctions.IgnoreFun)
            {
                //Trace: Performed ignore action for the event
                PrtResetTriggerAndPayload();
            }
            else
            {
                if(invertedFunStack.TopOfStack == null)
                {
                    //Trace: executed the action handler for event
                    PrtPushFunStackFrame(currAction, currAction.CreateLocals(currentPayload));
                }
                //invoke the action handler
                invertedFunStack.TopOfStack.fun.Execute(StateImpl, this);
            }
            goto CheckFunLastOperation;

            CheckFunLastOperation:
            if(receiveSet.Count != 0)
            {
                // We are at a blocking "receive"; so do receive operation
                nextSMOperation = PrtNextStatemachineOperation.ReceiveOperation;
                goto Finish;

            }

            switch(continuation.reason)
            {
                case PrtContinuationReason.Pop:
                    {
                        stateExitReason = PrtStateExitReason.OnPopStatement;
                        PrtExecuteExitFunction();
                        goto CheckFunLastOperation;
                    }
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
                case PrtContinuationReason.NewMachine:
                {
                    stateExitReason = PrtStateExitReason.NotExit;
                    hasMoreWork = false;
                    goto Finish;
                }
                case PrtContinuationReason.Nondet:
                    {
                        stateExitReason = PrtStateExitReason.NotExit;
                        StateImpl.SetPendingChoicesAsBoolean(this);
                        continuation.nondet = ((Boolean)StateImpl.GetSelectedChoiceValue(this));
                        hasMoreWork = false;
                        goto Finish;
                    }
                case PrtContinuationReason.Receive:
                    { 
                        stateExitReason = PrtStateExitReason.NotExit;
                        nextSMOperation = PrtNextStatemachineOperation.ReceiveOperation;
                        hasMoreWork = true;
                        goto Finish;
                    }
                case PrtContinuationReason.Send:
                    {
                        stateExitReason = PrtStateExitReason.NotExit;
                        hasMoreWork = false;
                        goto Finish;
                    }
                case PrtContinuationReason.Return:
                    {
                        switch (stateExitReason)
                        {
                            case PrtStateExitReason.NotExit:
                                {
                                    nextSMOperation = PrtNextStatemachineOperation.DequeueOperation;
                                    hasMoreWork = true;
                                    goto Finish;
                                }
                            case PrtStateExitReason.OnPopStatement:
                                {
                                    hasMoreWork = !PrtPopState(true);
                                    nextSMOperation = PrtNextStatemachineOperation.DequeueOperation;
                                    stateExitReason = PrtStateExitReason.NotExit;
                                    goto Finish;
                                }
                            case PrtStateExitReason.OnGotoStatement:
                                {
                                    PrtChangeState(destOfGoto);
                                    nextSMOperation = PrtNextStatemachineOperation.EntryOperation;
                                    stateExitReason = PrtStateExitReason.NotExit;
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
                                    stateExitReason = PrtStateExitReason.NotExit;
                                    goto DoEntry;
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

            DoDequeue:
            Debug.Assert(receiveSet.Count == 0, "Machine cannot be blocked at receive when here");
            var dequeueStatus = PrtDequeueEvent(StateImpl, CurrentState.hasNullTransition);
            if(dequeueStatus == PrtDequeueReturnStatus.BLOCKED)
            {
                nextSMOperation = PrtNextStatemachineOperation.DequeueOperation;
                hasMoreWork = false;
                goto Finish;

            }
            else if (dequeueStatus == PrtDequeueReturnStatus.SUCCESS)
            {
                nextSMOperation = PrtNextStatemachineOperation.HandleEventOperation;
                hasMoreWork = true;
                goto Finish;
            }
            else // NULL transition
            {
                nextSMOperation = PrtNextStatemachineOperation.HandleEventOperation;
                hasMoreWork = false;
                goto Finish;
            }


            DoHandleEvent:
            Debug.Assert(receiveSet.Count == 0, "The machine must not be blocked on a receive");
            if(currentTrigger != null)
            {
                currEventValue = currentTrigger;
                currentTrigger = null;
            }
            else
            {
                currEventValue = eventValue;
            }

            if(PrtIsPushTransitionPresent(currEventValue))
            {
                PrtPushState(CurrentState.transitions[currEventValue].gotoState);
                goto DoEntry;
            }
            else if(PrtIsTransitionPresent(currEventValue))
            {
                stateExitReason = PrtStateExitReason.OnTransition;
                eventValue = currEventValue;
                PrtExecuteExitFunction();
                goto CheckFunLastOperation;
            }
            else if(PrtIsActionInstalled(currEventValue))
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

            DoReceive:
            Debug.Assert(receiveSet.Count == 0 && PrtHasNullReceiveCase(), "Receive set empty and at receive !!");
            if(receiveSet.Count == 0)
            {
                stateExitReason = PrtStateExitReason.NotExit;
                PrtExecuteReceiveCase(PrtEvent.NullEvent);
                goto CheckFunLastOperation;
            }
            dequeueStatus = PrtDequeueEvent(StateImpl, false);
            if (dequeueStatus == PrtDequeueReturnStatus.BLOCKED)
            {
                nextSMOperation = PrtNextStatemachineOperation.ReceiveOperation;
                hasMoreWork = false;
                goto Finish;

            }
            else if (dequeueStatus == PrtDequeueReturnStatus.SUCCESS)
            {
                stateExitReason = PrtStateExitReason.NotExit;
                PrtExecuteReceiveCase(currentTrigger);
                goto CheckFunLastOperation;
            }
            else // NULL case
            {
                nextSMOperation = PrtNextStatemachineOperation.ReceiveOperation;
                hasMoreWork = false;
                goto Finish;
            }

            Finish:
            Debug.Assert(!hasMoreWork || currentStatus == PrtMachineStatus.Enabled, "hasMoreWork is true but the statemachine is blocked");
            return hasMoreWork;

            }

    }
}
