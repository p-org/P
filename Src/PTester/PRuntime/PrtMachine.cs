using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace P.PRuntime
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
        public PrtEvent currentTrigger;
        public PrtValue currentPayload;
        public PrtEventBuffer eventQueue;
        public HashSet<PrtEvent> receiveSet;
        public bool isHalted;
        public bool isEnabled;
        private PrtNextStatemachineOperation nextSMOperation;
        private PrtStateExitReason stateExitReason;
        public int maxBufferSize;
        //just a reference to stateImpl
        private PStateImpl StateImpl;
        #endregion

        #region Clone and Undo
        public PrtMachine Clone() { throw new NotImplementedException(); }
        #endregion

        #region Constructor
        public PrtMachine(PStateImpl app, int maxBuff)
        {
            this.instanceNumber = this.NextInstanceNumber(app);
            this.machineFields = new List<PrtValue>();
            this.eventValue = null;
            this.stateStack = new PrtStateStack();
            this.invertedFunStack = new PrtFunStack();
            this.currentTrigger = null;
            this.currentPayload = PrtValue.NullValue;
            this.eventQueue = new PrtEventBuffer();
            this.receiveSet = new HashSet<PrtEvent>();
            this.isHalted = false;
            this.isEnabled = true;
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

        public abstract int NextInstanceNumber(PStateImpl app);
        public PrtMachineStatus CurrentStatus
        {
            get
            {
                if (isHalted)
                {
                    return PrtMachineStatus.Halted;
                }
                else if (isEnabled)
                {
                    return PrtMachineStatus.Enabled;
                }
                else
                {
                    return PrtMachineStatus.Blocked;
                }
            }

        }

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

        public void PrtPopState()
        {
            stateStack.PopStackFrame();
        }

        public void PrtChangeState(PrtState s)
        {
            if(stateStack.TopOfStack != null)
            {
                stateStack.PopStackFrame();
            }
            stateStack.PushStackFrame(s);
        }

        public PrtFunStackFrame PrtPopFunStack()
        {
            return invertedFunStack.PopFun();
        }

        public void PrtPushFunStack(PrtFun fun, List<PrtValue> local)
        {
            invertedFunStack.PushFun(fun, local);
        }

        public void PrtResetTriggerAndPayload()
        {
            currentTrigger = null;
            currentPayload = null;
        }

        public void PrtEnqueueEvent(PStateImpl application, PrtEvent e, PrtValue arg, PrtMachine source)
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

            if (isHalted)
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
                if (!isEnabled && this.eventQueue.IsEnabled(this))
                {
                    isEnabled = true;
                }
            }
        }

        public PrtDequeueReturnStatus PrtDequeueEvent(PStateImpl application, bool hasNullTransition)
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
                if (!isEnabled)
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
                if (!isEnabled)
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
                //invokescheduler("blocked", machineId);
                if (!isEnabled)
                {
                    throw new PrtAssumeFailureException();
                }
                isEnabled = false;
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
            PrtPushFunStack(CurrentState.exitFun, CurrentState.exitFun.CreateLocals());
            invertedFunStack.TopOfStack.fun.Execute(StateImpl, this);
        }
        #endregion


        public void PrtRunStateMachine()
        {
            int numOfStepsTaken = 0;
            Debug.Assert(isEnabled && !isHalted, "Invoked PrtRunStateMachine or a blocked or a halted machine");

            while (PrtStepStateMachine())
            {
                if(numOfStepsTaken > 100000)
                {
                    StateImpl.Exception = new PrtInfiniteRaiseLoop();
                    return;
                }
                numOfStepsTaken++;
            }
        }
        

        public bool PrtStepStateMachine()
        {
            PrtEvent currEventValue;
            PrtFun currAction;
            bool hasMoreWork = false;

            Debug.Assert(isEnabled, "PrtStepStateMachine is invoked when the state machine is blocked, STRANGE !!");

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
                PrtPushFunStack(CurrentState.entryFun, CurrentState.entryFun.CreateLocals(currentPayload));
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
                    PrtPushFunStack(currAction, currAction.CreateLocals(currentPayload));
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

            if(invertedFunStack.TopOfStack == null)
            {
                // This means that I just did an ignore and hence should go back to dequeue
                hasMoreWork = true;
                nextSMOperation = PrtNextStatemachineOperation.DequeueOperation;
                stateExitReason = PrtStateExitReason.NotExit;
            }

            switch(invertedFunStack.TopOfStack.cont.reason)
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
                case PrtContinuationReason.Return:
                    {
                        switch(stateExitReason)
                        {
                            case PrtStateExitReason.NotExit:
                                {
                                    nextSMOperation = PrtNextStatemachineOperation.DequeueOperation;
                                    hasMoreWork = true;
                                    goto Finish;
                                }
                            case PrtStateExitReason.OnPopStatement:
                                {
                                    hasMoreWork != PrtCheckPopState();


                                }
                        }
                    }


            }

            Finish:
            return hasMoreWork;

        }













    }
}
