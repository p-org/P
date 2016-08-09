using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace P.PRuntime
{
    #region P state machines Implementation

    public abstract class Root
    {
        public abstract string Name
        {
            get;
        }

        public List<PrtValue> fields;
        public Event currentEvent;
        public PrtValue currentArg;
        public Continuation cont;

    }

    public abstract class BaseMachine : Root
    {
        public abstract bool IsHalted
        {
            get;
        }

        public abstract bool IsEnabled
        {
            get;
        }

        public enum PrtSMStatus
        {
            Enabled,        // The state machine is enabled
            Blocked,        // The state machine is blocked on a dequeue or receive
            Halted,         // The state machine has halted
        };

        public PrtSMStatus CurrentStatus
        {
            get
            {
                if (IsHalted)
                {
                    return PrtSMStatus.Halted;
                }
                else if (IsEnabled)
                {
                    return PrtSMStatus.Enabled;
                }
                else
                {
                    return PrtSMStatus.Blocked;
                }
            }

        }

        private bool doYield;
        
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

        private Stack<PrtSMMethod> methodStack;
        public PrtSMMethod lastFunctionCompleted;

        public BaseMachine()
        {
            methodStack = new Stack<PrtSMMethod>();

            doYield = true;
        }

        #region Method stack 
        public void RunNextBlock()
        {
            Debug.Assert(TopOfMethodStack != null);
            TopOfMethodStack.Dispatch(this);
        }


        public void MethodReturn()
        {
            PrtSMMethod returningMethod = PopMethod();

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

        public void CallMethod(PrtSMMethod method)
        {
            PushMethod(method);
        }

        public void PushMethod(PrtSMMethod method)
        {
            methodStack.Push(method);
        }

        public PrtSMMethod PopMethod()
        {
            Debug.Assert(TopOfMethodStack != null, "Pop on an empty method stack");
            return methodStack.Pop();
        }

        public PrtSMMethod TopOfMethodStack
        {
            get
            {
                if (methodStack.Count == 0)
                    return null;
                else
                    return methodStack.Peek();
            }
        }
        #endregion

    }

    public abstract class BaseMonitor : Root
    {
        public abstract bool IsHot
        {
            get;
        }
    }

    public abstract class Monitor<T> : BaseMonitor
    {

        public abstract State<T> StartState
        {
            get;
        }

        public abstract void Invoke();
    }

    public abstract class PrtSMMethod
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

        public abstract string ProgramCounter
        {
            get;
        }

        public abstract void Dispatch(BaseMachine m);
    }

    public abstract class Machine<T> : BaseMachine where T : Machine<T>
    {
        public StateStack<T> stateStack;
        public EventBuffer<T> buffer;
        public int maxBufferSize;
        public int instance;
        public HashSet<Event> receiveSet;

        private bool isHalted;
        private bool isEnabled;
        public override bool IsHalted
        {
            get
            {
                return isHalted;
            }
        }

        public override bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
        }
        public Machine(PStateImpl app, int instance, int maxBufferSize)
        {
            isHalted = false;
            isEnabled = true;
            stateStack = null;
            fields = new List<PrtValue>();
            cont = new Continuation();
            buffer = new EventBuffer<T>();
            this.maxBufferSize = maxBufferSize;
            this.instance = instance;
            currentEvent = null;
            currentArg = PrtValue.NullValue;
            receiveSet = new HashSet<Event>();
            PushMethod(new Start(app, this));
        }

        public void PushState(State<T> s)
        {
            StateStack<T> ss = new StateStack<T>();
            ss.next = this.stateStack;
            ss.state = s;
            this.stateStack = ss;
        }

        public void PopState()
        {
            this.stateStack = this.stateStack.next;
        }


        public abstract State<T> StartState
        {
            get;
        }

        public void EnqueueEvent(PStateImpl application, Event e, PrtValue arg, Machine<T> source)
        {
            PrtType prtType;

            if (e == null)
            {
                throw new PrtIllegalEnqueueException("Enqueued event must be non-null");
            }

            //assertion to check if argument passed inhabits the payload type.
            prtType = e.payload;

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
                        @"<EnqueueLog> Enqueued Event < {0} > in {1}-{2} by {3}-{4}",
                        e.name, this.Name, this.instance, source.Name, source.instance);
                }
                else
                {
                    application.Trace(
                        @"<EnqueueLog> Enqueued Event <{0}, {1}> in {2}-{3} by {4}-{5}",
                        e.name, arg.ToString(), this.Name, this.instance, source.Name, source.instance);
                }

                this.buffer.EnqueueEvent(e, arg);
                if (this.maxBufferSize != -1 && this.buffer.Size() > this.maxBufferSize)
                {
                    throw new PrtMaxBufferSizeExceededException(
                        String.Format(@"<EXCEPTION> Event Buffer Size Exceeded {0} in Machine {1}-{2}",
                        this.maxBufferSize, this.Name, this.instance));
                }
                if (!isEnabled && this.buffer.IsEnabled((T)this))
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
            buffer.DequeueEvent((T)this);
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
                receiveSet = new HashSet<Event>();
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
                receiveSet = new HashSet<Event>();
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

        internal sealed class Start : PrtSMMethod
        {
            private static readonly short typeId = 0;

            private PStateImpl application;
            private Machine<T> machine;

            // locals
            private Blocks nextBlock;

            public Start(PStateImpl app, Machine<T> machine)
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

            public override string ProgramCounter
            {
                get
                {
                    return nextBlock.ToString();
                }
            }

            public override void Dispatch(BaseMachine p)
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

            private void Enter(BaseMachine p)
            {
                Machine<T>.Run callee = new Machine<T>.Run(application, machine, machine.StartState);
                p.CallMethod(callee);
                StateImpl.IsCall = true;

                nextBlock = Blocks.B0;
            }

            private void B0(BaseMachine p)
            {
                p.lastFunctionCompleted = null;

                var currentEvent = machine.currentEvent;

                //Checking if currentEvent is halt:
                if (currentEvent == Event.HaltEvent)
                {
                    machine.stateStack = null;
                    machine.buffer = null;
                    machine.currentArg = null;
                    machine.isHalted = true;
                    machine.isEnabled = false;

                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                else
                {
                    application.Trace(

                        @"<StateLog> Unhandled event exception by machine Real1-{0}",
                        machine.instance);
                    this.StateImpl.Exception = new PrtUnhandledEventException("Unhandled event exception by machine <mach name>");
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
            }
        }

        internal sealed class Run : PrtSMMethod
        {
            private static readonly short typeId = 1;

            private PStateImpl application;
            private Machine<T> machine;

            // inputs
            private State<T> state;

            // locals
            private Blocks nextBlock;
            private bool doPop;

            public Run(PStateImpl app, Machine<T> machine)
            {
                application = app;
                nextBlock = Blocks.Enter;
                this.machine = machine;
                this.state = null;
            }

            public Run(PStateImpl app, Machine<T> machine, State<T> state)
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

            public override void Dispatch(BaseMachine p)
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

            public override string ProgramCounter
            {
                get
                {
                    return nextBlock.ToString();
                }
            }


            private void B5(BaseMachine p)
            {
                machine.PopState();
                p.MethodReturn();
                StateImpl.IsReturn = true;
            }

            private void B4(BaseMachine p)
            {
                doPop = ((Machine<T>.RunHelper)p.lastFunctionCompleted).ReturnValue;
                p.lastFunctionCompleted = null;

                //B1 is header of the "while" loop:
                nextBlock = Blocks.B1;
            }

            private void B3(BaseMachine p)
            {
                Machine<T>.RunHelper callee = new Machine<T>.RunHelper(application, machine, false);
                p.CallMethod(callee);
                StateImpl.IsCall = true;

                nextBlock = Blocks.B4;
            }

            private void B2(BaseMachine p)
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
                    StateImpl.IsReturn = true;
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

            private void B1(BaseMachine p)
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

            private void B0(BaseMachine p)
            {
                //Return from RunHelper:
                doPop = ((Machine<T>.RunHelper)p.lastFunctionCompleted).ReturnValue;
                p.lastFunctionCompleted = null;
                nextBlock = Blocks.B1;
            }

            private void Enter(BaseMachine p)
            {
                machine.PushState(state);

                Machine<T>.RunHelper callee = new Machine<T>.RunHelper(application, machine, true);
                p.CallMethod(callee);
                StateImpl.IsCall = true;

                nextBlock = Blocks.B0;
            }
        }

        internal sealed class RunHelper : PrtSMMethod
        {
            private static readonly short typeId = 2;

            private PStateImpl application;
            private Machine<T> machine;

            // inputs
            private bool start;

            // locals
            private Blocks nextBlock;
            private State<T> state;
            private Fun<T> fun;
            private Transition<T> transition;
            private PrtValue payload;

            // output
            private bool _ReturnValue;
            public bool ReturnValue
            {
                get
                {
                    return _ReturnValue;
                }
            }

            public RunHelper(PStateImpl app, Machine<T> machine, bool start)
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
                B0 = 2,
                B1 = 3,
                B2 = 4,
                B3 = 5,
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

            public override string ProgramCounter
            {
                get
                {
                    return nextBlock.ToString();
                }
            }

            public override void Dispatch(BaseMachine p)
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


            private void Enter(BaseMachine p)
            {
                var stateStack = machine.stateStack;
                this.state = stateStack.state;

                if (start)
                {
                    payload = machine.currentArg;
                    nextBlock = Blocks.B0;
                }
                else
                {
                    nextBlock = Blocks.B1;
                }
            }

            public void B0(BaseMachine p)
            {
                var stateStack = machine.stateStack;

                //enter:
                stateStack.CalculateDeferredAndActionSet();

                fun = state.entryFun;
                nextBlock = Blocks.B2;
            }

            private void B2(BaseMachine p)
            {
                var stateStack = machine.stateStack;

                Machine<T>.ReentrancyHelper callee = new Machine<T>.ReentrancyHelper(application, machine, fun, payload);
                p.CallMethod(callee);
                StateImpl.IsCall = true;
                nextBlock = Blocks.B3;
            }

            private void B3(BaseMachine p)
            {
                p.lastFunctionCompleted = null;

                var reason = machine.cont.reason;
                if (reason == ContinuationReason.Raise)
                {
                    //goto handle;
                    nextBlock = Blocks.B1;
                }
                else
                {
                    machine.currentEvent = null;
                    machine.currentArg = PrtValue.NullValue;
                    if (reason != ContinuationReason.Pop)
                    {
                        _ReturnValue = false;
                        p.MethodReturn();
                        StateImpl.IsReturn = true;
                    }
                    else
                    {
                        Machine<T>.ReentrancyHelper callee = new Machine<T>.ReentrancyHelper(application, machine, state.exitFun, null);
                        p.CallMethod(callee);
                        StateImpl.IsCall = true;

                        nextBlock = Blocks.B4;
                    }
                }
            }

            private void B4(BaseMachine p)
            {
                p.lastFunctionCompleted = null;

                _ReturnValue = true;
                p.MethodReturn();
                StateImpl.IsReturn = true;
            }

            private void B1(BaseMachine p)
            {
                var stateStack = machine.stateStack;
                var state = stateStack.state;
                var actionSet = stateStack.actionSet;

                //handle:
                payload = machine.currentArg;
                if (actionSet.Contains(machine.currentEvent))
                {
                    fun = stateStack.Find(machine.currentEvent);
                    //goto execute;
                    nextBlock = Blocks.B2;
                }
                else
                {
                    transition = state.FindPushTransition(machine.currentEvent);
                    if (transition != null)
                    {
                        Machine<T>.Run callee = new Machine<T>.Run(application, machine, transition.to);
                        p.CallMethod(callee);
                        StateImpl.IsCall = true;

                        nextBlock = Blocks.B5;
                    }
                    else
                    {
                        nextBlock = Blocks.B6;
                    }
                }
            }

            private void B5(BaseMachine p)
            {
                p.lastFunctionCompleted = null;

                if (machine.currentEvent == null)
                {
                    _ReturnValue = false;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                else
                {
                    //goto handle;
                    nextBlock = Blocks.B1;
                }
            }

            private void B6(BaseMachine p)
            {
                Machine<T>.ReentrancyHelper callee = new Machine<T>.ReentrancyHelper(application, machine, state.exitFun, null);
                p.CallMethod(callee);
                StateImpl.IsCall = true;

                nextBlock = Blocks.B7;
            }

            private void B7(BaseMachine p)
            {
                p.lastFunctionCompleted = null;

                transition = state.FindTransition(machine.currentEvent);
                if (transition == null)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                else
                {
                    Machine<T>.ReentrancyHelper callee = new Machine<T>.ReentrancyHelper(application, machine, transition.fun, payload);
                    p.CallMethod(callee);
                    StateImpl.IsCall = true;
                    nextBlock = Blocks.B8;
                }
            }

            private void B8(BaseMachine p)
            {
                payload = ((Machine<T>.ReentrancyHelper)p.lastFunctionCompleted).ReturnValue;
                p.lastFunctionCompleted = null;
                var stateStack = machine.stateStack;
                stateStack.state = transition.to;
                state = stateStack.state;

                //goto enter;
                nextBlock = Blocks.B0;
            }
        }

        internal sealed class ReentrancyHelper : PrtSMMethod
        {
            private static readonly short typeId = 3;

            private PStateImpl application;
            private Machine<T> machine;
            private Fun<T> fun;

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

            public ReentrancyHelper(PStateImpl app, Machine<T> machine, Fun<T> fun, PrtValue payload)
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

            public override string ProgramCounter
            {
                get
                {
                    return nextBlock.ToString();
                }
            }

            public override void Dispatch(BaseMachine p)
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


            private void Enter(BaseMachine p)
            {
                machine.cont.Reset();
                fun.PushFrame((T)machine, payload);

                nextBlock = Blocks.B0;
            }

            private void B0(BaseMachine p)
            {
                try
                {
                    fun.Execute(application, (T)machine);
                }
                catch (PrtException ex)
                {
                    application.Exception = ex;
                }
                Machine<T>.ProcessContinuation callee = new ProcessContinuation(application, machine);
                p.CallMethod(callee);
                StateImpl.IsCall = true;
                nextBlock = Blocks.B1;
            }

            private void B1(BaseMachine p)
            {
                var doPop = ((Machine<T>.ProcessContinuation)p.lastFunctionCompleted).ReturnValue;
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
                    StateImpl.IsReturn = true;
                }
                else
                {
                    nextBlock = Blocks.B0;
                }
            }
        }

        internal sealed class ProcessContinuation : PrtSMMethod
        {
            private static readonly short typeId = 4;

            private PStateImpl application;
            private Machine<T> machine;

            // locals
            private Blocks nextBlock;

            // output
            private bool _ReturnValue;

            public bool ReturnValue
            {
                get { return _ReturnValue; }
            }

            public ProcessContinuation(PStateImpl app, Machine<T> machine)
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

            public override string ProgramCounter
            {
                get
                {
                    return nextBlock.ToString();
                }
            }

            public override void Dispatch(BaseMachine p)
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


            private void Enter(BaseMachine p)
            {
                var cont = machine.cont;
                var reason = cont.reason;
                if (reason == ContinuationReason.Return)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                if (reason == ContinuationReason.Pop)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                if (reason == ContinuationReason.Raise)
                {
                    _ReturnValue = true;
                    p.MethodReturn();
                    StateImpl.IsReturn = true;
                }
                if (reason == ContinuationReason.Receive)
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
                if (reason == ContinuationReason.Nondet)
                {
                    application.SetPendingChoicesAsBoolean(p);
                    cont.nondet = ((Boolean)application.GetSelectedChoiceValue(p));
                    nextBlock = Blocks.B0;
                }
                if (reason == ContinuationReason.NewMachine)
                {
                    //yield;
                    p.DoYield = true;
                    nextBlock = Blocks.B0;
                }
                if (reason == ContinuationReason.Send)
                {
                    //yield;
                    p.DoYield = true;
                    nextBlock = Blocks.B0;
                }
            }

            private void B0(BaseMachine p)
            {
                // ContinuationReason.Receive
                _ReturnValue = false;
                p.MethodReturn();
                StateImpl.IsReturn = true;
            }
        }
    }
    #endregion
}
