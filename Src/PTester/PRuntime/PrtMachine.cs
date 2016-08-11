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

    public abstract class PrtMachine
    {
        #region Fields
        private PrtStateStack stateStack;
        public PrtEventBuffer buffer;
        public int maxBufferSize;
        public int instance;
        public HashSet<PrtEvent> receiveSet;

        private bool isHalted;
        private bool isEnabled;
        
        public List<PrtValue> fields;
        public PrtEvent currentEvent;
        public PrtValue currentArg;
        public PrtContinuation cont;
        private bool doYield;
        private Stack<PrtMethod> methodStack;
        public PrtMethod lastFunctionCompleted;
        #endregion

        public abstract PrtMachine Clone();

        #region Constructor
        public PrtMachine(PStateImpl app, int maxBuffSize, Type T)
        {
            isHalted = false;
            isEnabled = true;
            stateStack = null;
            fields = new List<PrtValue>();
            cont = new PrtContinuation();
            buffer = new PrtEventBuffer();
            this.maxBufferSize = maxBuffSize;
            this.instance = app.AllStateMachines.Where(mach => mach.GetType() == T).Count() + 1;
            currentEvent = null;
            currentArg = PrtValue.NullValue;
            receiveSet = new HashSet<PrtEvent>();
            PushMethod(new StartMethod(app, this));
        }
        #endregion

        #region getters and setters
        public abstract string Name
        {
            get;
        }
        public bool IsHalted
        {
            get
            {
                return isHalted;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
        }

        public bool DoYield
        {
            get
            {
                return doYield;
            }
            set
            {
                doYield = value;
            }
        }

        public PrtMethod TopOfMethodStack
        {
            get
            {
                if (methodStack.Count == 0)
                    return null;
                else
                    return methodStack.Peek();
            }
        }

        public PrtMachineStatus CurrentStatus
        {
            get
            {
                if (IsHalted)
                {
                    return PrtMachineStatus.Halted;
                }
                else if (IsEnabled)
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


        

        public void RunNextBlock()
        {
            Debug.Assert(TopOfMethodStack != null);
            TopOfMethodStack.Dispatch(this);
        }


        public void MethodReturn()
        {
            PrtMethod returningMethod = PopMethod();

            if (TopOfMethodStack != null)
                lastFunctionCompleted = returningMethod;
            else
            {
                lastFunctionCompleted = null;
                DoYield = true;
            }

            if (TopOfMethodStack == null)
            {
                //Process has terminated 
                //want to do something ???
            }
        }

        public void CallMethod(PrtMethod method)
        {
            PushMethod(method);
        }

        public void PushMethod(PrtMethod method)
        {
            methodStack.Push(method);
        }

        public PrtMethod PopMethod()
        {
            Debug.Assert(TopOfMethodStack != null, "Pop on an empty method stack");
            return methodStack.Pop();
        }

        public void PushState(PrtState s)
        {
            stateStack.PushStackFrame(s);
        }

        public void PopState()
        {
            stateStack.PopStackFrame();
        }


        public abstract PrtState StartState
        {
            get;
        }

        public void EnqueueEvent(PStateImpl application, PrtEvent e, PrtValue arg, PrtMachine source)
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
                    this.Name, this.instance, e.name);
            }
            else
            {
                if (arg != null)
                {
                    application.Trace(
                        @"<EnqueueLog> Enqueued Event <{0}, {1}> in {2}-{3} by {4}-{5}",
                        e.name, arg.ToString(), this.Name, this.instance, source.Name, source.instance);
                }
                else
                {
                    application.Trace(
                        @"<EnqueueLog> Enqueued Event < {0} > in {1}-{2} by {3}-{4}",
                        e.name, this.Name, this.instance, source.Name, source.instance);
                }

                this.buffer.EnqueueEvent(e, arg);
                if (this.maxBufferSize != -1 && this.buffer.Size() > this.maxBufferSize)
                {
                    throw new PrtMaxBufferSizeExceededException(
                        String.Format(@"<EXCEPTION> Event Buffer Size Exceeded {0} in Machine {1}-{2}",
                        this.maxBufferSize, this.Name, this.instance));
                }
                if (!isEnabled && this.buffer.IsEnabled(this))
                {
                    isEnabled = true;
                }
                if (isEnabled)
                {
                    //application.invokescheduler("enabled", machineId, source.machineId);
                }
            }
        }

        public enum DequeueEventReturnStatus { SUCCESS, NULL, BLOCKED };

        public DequeueEventReturnStatus DequeueEvent(PStateImpl application, bool hasNullTransition)
        {
            currentEvent = null;
            currentArg = null;
            buffer.DequeueEvent(this);
            if (currentEvent != null)
            {
                if (currentArg == null)
                {
                    throw new PrtInternalException("Internal error: currentArg is null");
                }
                if (!isEnabled)
                {
                    throw new PrtInternalException("Internal error: Tyring to execute blocked machine");
                }

                application.Trace(
                    "<DequeueLog> Dequeued Event < {0}, {1} > at Machine {2}-{3}\n",
                    currentEvent.name, currentArg.ToString(), Name, instance);
                receiveSet = new HashSet<PrtEvent>();
                return DequeueEventReturnStatus.SUCCESS;
            }
            else if (hasNullTransition || receiveSet.Contains(currentEvent))
            {
                if (!isEnabled)
                {
                    throw new PrtInternalException("Internal error: Tyring to execute blocked machine");
                }
                application.Trace(
                    "<NullTransLog> Null transition taken by Machine {0}-{1}\n",
                    Name, instance);
                currentArg = PrtValue.NullValue;
                receiveSet = new HashSet<PrtEvent>();
                return DequeueEventReturnStatus.NULL;
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
                return DequeueEventReturnStatus.BLOCKED;
            }
        }

        internal sealed class StartMethod : PrtMethod
        {
            private static readonly short typeId = 0;

            private PStateImpl application;
            private PrtMachine machine;

            // locals
            private Blocks nextBlock;

            public StartMethod(PStateImpl app, PrtMachine machine)
            {
                application = app;
                this.machine = machine;
                nextBlock = Blocks.Enter;
            }

            public override PStateImpl StateImpl
            {
                get
                {
                    return application;
                }
                set
                {
                    application = value;
                }
            }

            private enum Blocks : ushort
            {
                None = 0,
                Enter = 1,
                B0 = 2,
            };

            public override ushort NextBlock
            {
                get
                {
                    return ((ushort)nextBlock);
                }
                set
                {
                    nextBlock = ((Blocks)value);
                }
            }

            public override void Dispatch(PrtMachine p)
            {
                switch (nextBlock)
                {
                    case Blocks.Enter:
                        {
                            Enter(p);
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException();
                        }
                    case Blocks.B0:
                        {
                            B0(p);
                            break;
                        }
                }
            }

            private void Enter(PrtMachine p)
            {
                PrtMachine.RunMethod callee = new PrtMachine.RunMethod(application, machine, machine.StartState);
                p.CallMethod(callee);
                nextBlock = Blocks.B0;
            }

            private void B0(PrtMachine p)
            {
                p.lastFunctionCompleted = null;

                var currentEvent = machine.currentEvent;

                //Checking if currentEvent is halt:
                if (currentEvent == PrtEvent.HaltEvent)
                {
                    machine.stateStack = null;
                    machine.buffer = null;
                    machine.currentArg = null;
                    machine.isHalted = true;
                    machine.isEnabled = false;

                    p.MethodReturn();
                }
                else
                {
                    application.Trace(

                        @"<StateLog> Unhandled event exception by machine Real1-{0}",
                        machine.instance);
                    this.StateImpl.Exception = new PrtUnhandledEventException("Unhandled event exception by machine <mach name>");
                    p.MethodReturn();
                }
            }
        }

        internal sealed class RunMethod : PrtMethod
        {
            private static readonly short typeId = 1;

            private PStateImpl application;
            private PrtMachine machine;

            // inputs
            private PrtState state;

            // locals
            private Blocks nextBlock;
            private bool doPop;

            public RunMethod(PStateImpl app, PrtMachine machine)
            {
                application = app;
                nextBlock = Blocks.Enter;
                this.machine = machine;
                this.state = null;
            }

            public RunMethod(PStateImpl app, PrtMachine machine, PrtState state)
            {
                application = app;
                nextBlock = Blocks.Enter;
                this.machine = machine;
                this.state = state;
            }

            public override PStateImpl StateImpl
            {
                get
                {
                    return application;
                }
                set
                {
                    application = value;
                }
            }

            private enum Blocks : ushort
            {
                None = 0,
                Enter = 1,
                B0 = 2,
                B1 = 3,
                B2 = 4,
                B3 = 5,
                B4 = 6,
                B5 = 7,
            };

            public override void Dispatch(PrtMachine p)
            {
                switch (nextBlock)
                {
                    case Blocks.Enter:
                        {
                            Enter(p);
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException();
                        }
                    case Blocks.B0:
                        {
                            B0(p);
                            break;
                        }
                    case Blocks.B1:
                        {
                            B1(p);
                            break;
                        }
                    case Blocks.B2:
                        {
                            B2(p);
                            break;
                        }
                    case Blocks.B3:
                        {
                            B3(p);
                            break;
                        }
                    case Blocks.B4:
                        {
                            B4(p);
                            break;
                        }
                    case Blocks.B5:
                        {
                            B5(p);
                            break;
                        }
                }
            }

            public override ushort NextBlock
            {
                get
                {
                    return ((ushort)nextBlock);
                }
                set
                {
                    nextBlock = ((Blocks)value);
                }
            }


            private void B5(PrtMachine p)
            {
                machine.PopState();
                p.MethodReturn();
            }

            private void B4(PrtMachine p)
            {
                doPop = ((PrtMachine.RunHelperMethod)p.lastFunctionCompleted).ReturnValue;
                p.lastFunctionCompleted = null;

                //B1 is header of the "while" loop:
                nextBlock = Blocks.B1;
            }

            private void B3(PrtMachine p)
            {
                PrtMachine.RunHelperMethod callee = new PrtMachine.RunHelperMethod(application, machine, false);
                p.CallMethod(callee);
                nextBlock = Blocks.B4;
            }

            private void B2(PrtMachine p)
            {
                var stateStack = machine.stateStack;
                var hasNullTransitionOrAction = stateStack.HasNullTransitionOrAction();
                DequeueEventReturnStatus status;
                try
                {
                    status = machine.DequeueEvent(application, hasNullTransitionOrAction);
                }
                catch (PrtException ex)
                {
                    application.Exception = ex;
                    p.MethodReturn();
                    return;
                }

                if (status == DequeueEventReturnStatus.BLOCKED)
                {
                    p.DoYield = true;
                    nextBlock = Blocks.B2;
                }
                else if (status == DequeueEventReturnStatus.SUCCESS)
                {
                    nextBlock = Blocks.B3;
                }
                else
                {
                    p.DoYield = true;
                    nextBlock = Blocks.B3;
                }
            }

            private void B1(PrtMachine p)
            {
                if (!doPop)
                {
                    nextBlock = Blocks.B2;
                }
                else
                {
                    nextBlock = Blocks.B5;
                }
            }

            private void B0(PrtMachine p)
            {
                //Return from RunHelper:
                doPop = ((PrtMachine.RunHelperMethod)p.lastFunctionCompleted).ReturnValue;
                p.lastFunctionCompleted = null;
                nextBlock = Blocks.B1;
            }

            private void Enter(PrtMachine p)
            {
                machine.PushState(state);

                PrtMachine.RunHelperMethod callee = new PrtMachine.RunHelperMethod(application, machine, true);
                p.CallMethod(callee);
                nextBlock = Blocks.B0;
            }
        }

        internal sealed class RunHelperMethod : PrtMethod
        {
            private static readonly short typeId = 2;

            private PStateImpl application;
            private PrtMachine machine;

            // inputs
            private bool start;

            // locals
            private Blocks nextBlock;

            // output
            private bool _ReturnValue;
            public bool ReturnValue
            {
                get
                {
                    return _ReturnValue;
                }
            }

            public RunHelperMethod(PStateImpl app, PrtMachine machine, bool start)
            {
                application = app;
                nextBlock = Blocks.Enter;
                this.machine = machine;
                this.start = start;
            }

            public override PStateImpl StateImpl
            {
                get
                {
                    return application;
                }
                set
                {
                    application = value;
                }
            }

            public enum Blocks : ushort
            {
                None = 0,
                Enter = 1,
                EnterStart = 2,
                HandleEvent = 3,
                ExecuteEntry = 4,
                CheckCont = 5,
                B4 = 6,
                B5 = 7,
                B6 = 8,
                B7 = 9,
                B8 = 10,
            }

            public override ushort NextBlock
            {
                get
                {
                    return ((ushort)nextBlock);
                }
                set
                {
                    nextBlock = ((Blocks)value);
                }
            }


            public override void Dispatch(PrtMachine p)
            {
                switch (nextBlock)
                {
                    case Blocks.Enter:
                        {
                            Enter(p);
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException();
                        }
                    case Blocks.EnterStart:
                        {
                            EnterStart(p);
                            break;
                        }
                    case Blocks.HandleEvent:
                        {
                            HandleEvent(p);
                            break;
                        }
                    case Blocks.ExecuteEntry:
                        {
                            ExecuteEntry(p);
                            break;
                        }
                    case Blocks.CheckCont:
                        {
                            CheckCont(p);
                            break;
                        }
                    case Blocks.B4:
                        {
                            B4(p);
                            break;
                        }
                    case Blocks.B5:
                        {
                            B5(p);
                            break;
                        }
                    case Blocks.B6:
                        {
                            B6(p);
                            break;
                        }
                    case Blocks.B7:
                        {
                            B7(p);
                            break;
                        }
                    case Blocks.B8:
                        {
                            B8(p);
                            break;
                        }
                }
            }


            private void Enter(PrtMachine p)
            {
                if (start)
                {
                    nextBlock = Blocks.EnterStart;
                }
                else
                {
                    nextBlock = Blocks.HandleEvent;
                }
            }

            public void EnterStart(PrtMachine p)
            {
                p.PushState(p.StartState);
                nextBlock = Blocks.ExecuteEntry;
            }

            private void ExecuteEntry(PrtMachine p)
            {
                PrtMachine.ReentrancyHelperMethod callee = new PrtMachine.ReentrancyHelperMethod(application, machine, p.CurrentState.entryFun, p.currentArg);
                p.CallMethod(callee);
                nextBlock = Blocks.CheckCont;
            }

            private void CheckCont(PrtMachine p)
            {
                p.lastFunctionCompleted = null;

                var reason = machine.cont.reason;
                if (reason == PrtContinuationReason.Raise)
                {
                    nextBlock = Blocks.HandleEvent;
                }
                else
                {
                    machine.currentEvent = null;
                    machine.currentArg = PrtValue.NullValue;
                    if (reason != PrtContinuationReason.Pop)
                    {
                        _ReturnValue = false;
                        p.MethodReturn();
                    }
                    else
                    {
                        PrtMachine.ReentrancyHelperMethod callee = new PrtMachine.ReentrancyHelperMethod(application, machine, state.exitFun, null);
                        p.CallMethod(callee);

                        nextBlock = Blocks.B4;
                    }
                }
            }

            private void B4(PrtMachine p)
            {
                p.lastFunctionCompleted = null;
                _ReturnValue = true;
                p.MethodReturn();
            }

            private void HandleEvent(PrtMachine p)
            {
                if (p.CurrentState.dos.Contains(machine.currentEvent))
                {
                    fun = stateStack.Find(machine.currentEvent);
                    //goto execute;
                    nextBlock = Blocks.ExecuteEntry;
                }
                else
                {
                    transition = state.FindPushTransition(machine.currentEvent);
                    if (transition != null)
                    {
                        PrtMachine.RunMethod callee = new PrtMachine.RunMethod(application, machine, transition.to);
                        p.CallMethod(callee);
                        nextBlock = Blocks.B5;
                    }
                    else
                    {
                        nextBlock = Blocks.B6;
                    }
                }
            }

            private void B5(PrtMachine p)
            {
                p.lastFunctionCompleted = null;

                if (machine.currentEvent == null)
                {
                    _ReturnValue = false;
                    p.MethodReturn();
                }
                else
                {
                    //goto handle;
                    nextBlock = Blocks.HandleEvent;
                }
            }

            private void B6(PrtMachine p)
            {
                PrtMachine.ReentrancyHelperMethod callee = new PrtMachine.ReentrancyHelperMethod(application, machine, state.exitFun, null);
                p.CallMethod(callee);
                nextBlock = Blocks.B7;
            }

            private void B7(PrtMachine p)
            {
                p.lastFunctionCompleted = null;

                transition = state.FindTransition(machine.currentEvent);
                if (transition == null)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                }
                else
                {
                    PrtMachine.ReentrancyHelperMethod callee = new PrtMachine.ReentrancyHelperMethod(application, machine, transition.fun, payload);
                    p.CallMethod(callee);
                    nextBlock = Blocks.B8;
                }
            }

            private void B8(PrtMachine p)
            {
                payload = ((PrtMachine.ReentrancyHelperMethod)p.lastFunctionCompleted).ReturnValue;
                p.lastFunctionCompleted = null;
                var stateStack = machine.stateStack;
                stateStack.state = transition.to;
                state = stateStack.state;

                //goto enter;
                nextBlock = Blocks.EnterStart;
            }
        }

        internal sealed class ReentrancyHelperMethod : PrtMethod
        {
            private static readonly short typeId = 3;

            private PStateImpl application;
            private PrtMachine machine;
            private PrtFun fun;

            // inputs
            private PrtValue payload;

            // locals
            private Blocks nextBlock;

            // output
            private PrtValue _ReturnValue;

            public PrtValue ReturnValue
            {
                get
                {
                    return _ReturnValue;
                }
            }

            public ReentrancyHelperMethod(PStateImpl app, PrtMachine machine, PrtFun fun, PrtValue payload)
            {
                this.application = app;
                this.machine = machine;
                this.fun = fun;
                this.payload = payload;
            }

            public override PStateImpl StateImpl
            {
                get
                {
                    return application;
                }
                set
                {
                    application = value;
                }
            }

            public enum Blocks : ushort
            {
                None = 0,
                Enter = 1,
                B0 = 2,
                B1 = 3,
            }

            public override ushort NextBlock
            {
                get
                {
                    return ((ushort)nextBlock);
                }
                set
                {
                    nextBlock = ((Blocks)value);
                }
            }


            public override void Dispatch(PrtMachine p)
            {
                switch (nextBlock)
                {
                    case Blocks.Enter:
                        {
                            Enter(p);
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException();
                        }
                    case Blocks.B0:
                        {
                            B0(p);
                            break;
                        }
                    case Blocks.B1:
                        {
                            B1(p);
                            break;
                        }
                }
            }


            private void Enter(PrtMachine p)
            {
                machine.cont = new PrtContinuation();
                machine.cont.PushContFrame(0, fun.CreateLocals(payload));
                nextBlock = Blocks.B0;
            }

            private void B0(PrtMachine p)
            {
                try
                {
                    fun.Execute(application, machine);
                }
                catch (PrtException ex)
                {
                    application.Exception = ex;
                }
                PrtMachine.ProcessContinuationMethod callee = new ProcessContinuationMethod(application, machine);
                p.CallMethod(callee);
                nextBlock = Blocks.B1;
            }

            private void B1(PrtMachine p)
            {
                var doPop = ((PrtMachine.ProcessContinuationMethod)p.lastFunctionCompleted).ReturnValue;
                p.lastFunctionCompleted = null;

                if (doPop)
                {
                    if (machine.cont.retLocals == null)
                    {
                        _ReturnValue = payload;
                    }
                    else
                    {
                        _ReturnValue = machine.cont.retLocals[0];
                    }
                    p.MethodReturn();
                }
                else
                {
                    nextBlock = Blocks.B0;
                }
            }
        }

        internal sealed class ProcessContinuationMethod : PrtMethod
        {
            private static readonly short typeId = 4;

            private PStateImpl application;
            private PrtMachine machine;

            // locals
            private Blocks nextBlock;

            // output
            private bool _ReturnValue;

            public bool ReturnValue
            {
                get { return _ReturnValue; }
            }

            public ProcessContinuationMethod(PStateImpl app, PrtMachine machine)
            {
                application = app;
                this.machine = machine;
                nextBlock = Blocks.Enter;
            }

            public override PStateImpl StateImpl
            {
                get
                {
                    return application;
                }
                set
                {
                    application = value;
                }
            }

            public enum Blocks : ushort
            {
                None = 0,
                Enter = 1,
                B0 = 2
            };

            public override ushort NextBlock
            {
                get
                {
                    return ((ushort)nextBlock);
                }
                set
                {
                    nextBlock = ((Blocks)value);
                }
            }

            public override void Dispatch(PrtMachine p)
            {
                switch (nextBlock)
                {
                    case Blocks.Enter:
                        {
                            Enter(p);
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException();
                        }
                    case Blocks.B0:
                        {
                            B0(p);
                            break;
                        }
                }
            }


            private void Enter(PrtMachine p)
            {
                var cont = machine.cont;
                var reason = cont.reason;
                if (reason == PrtContinuationReason.Return)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                if (reason == PrtContinuationReason.Pop)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                if (reason == PrtContinuationReason.Raise)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                if (reason == PrtContinuationReason.Receive)
                {
                    DequeueEventReturnStatus status;
                    try
                    {
                        status = machine.DequeueEvent(application, false);
                    }
                    catch (PrtException ex)
                    {
                        application.Exception = ex;
                        p.MethodReturn();
                        StateImpl.IsReturn = true;
                        return;
                    }

                    if (status == DequeueEventReturnStatus.BLOCKED)
                    {
                        p.DoYield = true;
                        nextBlock = Blocks.Enter;
                    }
                    else if (status == DequeueEventReturnStatus.SUCCESS)
                    {
                        nextBlock = Blocks.B0;
                    }
                    else
                    {
                        p.DoYield = true;
                        nextBlock = Blocks.B0;
                    }
                }
                if (reason == PrtContinuationReason.Nondet)
                {
                    application.SetPendingChoicesAsBoolean(p);
                    cont.nondet = ((Boolean)application.GetSelectedChoiceValue(p));
                    nextBlock = Blocks.B0;
                }
                if (reason == PrtContinuationReason.NewMachine)
                {
                    //yield;
                    p.DoYield = true;
                    nextBlock = Blocks.B0;
                }
                if (reason == PrtContinuationReason.Send)
                {
                    //yield;
                    p.DoYield = true;
                    nextBlock = Blocks.B0;
                }
            }

            private void B0(PrtMachine p)
            {
                // ContinuationReason.Receive
                _ReturnValue = false;
                p.MethodReturn();
                StateImpl.IsReturn = true;
            }
        }


    }

    public abstract class PrtMonitor : ICloneable
    {
        public abstract bool IsHot
        {
            get;
        }

        public abstract PrtState StartState
        {
            get;
        }

        public abstract void Invoke();

        public object Clone()
        {

        }
    }

    public abstract class PrtMethod
    {
        public abstract PStateImpl StateImpl
        {
            get;
            set;
        }

        public abstract ushort NextBlock
        {
            get;
            set;
        }

        public abstract void Dispatch(PrtMachine m);
    }

}
