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
        #endregion


        public void PrtRunStateMachine()
        {

        }
        
        

        


        

        

        

       
    }
}
