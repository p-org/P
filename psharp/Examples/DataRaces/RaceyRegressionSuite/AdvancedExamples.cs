using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace AdvancedExamples
{
    #region Events

    internal class eUnit : Event
    {
        public eUnit(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Structs and Classes

    internal struct Letter
    {
        public string Words;

        public Letter(string words)
        {
            this.Words = words;
        }
    }

    internal struct Envelope
    {
        public Letter Letter;
        public string Address;
        public int Id;

        public Envelope(string address, int id)
        {
            this.Letter = new Letter("");
            this.Address = address;
            this.Id = id;
        }
    }

    #endregion

    #region Machines

    internal class OtherClass1
    {
        private AnotherClass1 ac;

        internal OtherClass1(Envelope E)
        {
            ac = new AnotherClass1(E);
        }

        internal void foo()
        {
            ac.bar();
        }
    }

    internal class AnotherClass1
    {
        internal AnotherClass1(Envelope E)
        {
            this.E = E;
        }

        private Envelope E;

        internal void bar()
        {
            E.Id = 2; // ERROR
        }
    }

    internal class A3 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as A3;

                machine.Target = Machine.Factory.CreateMachine<A3>();
                Envelope envelope = new Envelope();
                OtherClass1 oc = new OtherClass1(envelope);

                this.Send(machine.Target, new eUnit(envelope));

                oc.foo();
            }
        }
    }

    internal class OtherClass2
    {
        private AnotherClass2 ac;

        internal OtherClass2()
        {

        }

        internal void foo(Envelope E)
        {
            ac = new AnotherClass2(E);
            ac.bar();
        }
    }

    internal class AnotherClass2
    {
        internal AnotherClass2(Envelope E)
        {
            this.E = E;
        }

        private Envelope E;

        internal void bar()
        {
            E.Id = 2; // ERROR
        }
    }

    internal class B3 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as B3;

                machine.Target = Machine.Factory.CreateMachine<B3>();
                Envelope envelope = new Envelope();
                OtherClass2 oc = new OtherClass2();

                this.Send(machine.Target, new eUnit(envelope));

                oc.foo(envelope);
            }
        }
    }

    internal class OtherClass3
    {
        private AnotherClass3 ac;

        internal OtherClass3(Envelope E)
        {
            ac = new AnotherClass3();
            ac.E = E;
        }

        internal void foo()
        {
            ac.bar();
        }
    }

    internal class AnotherClass3
    {
        internal AnotherClass3()
        {

        }

        internal Envelope E;

        internal void bar()
        {
            E.Id = 2; // ERROR
        }
    }

    internal class C3 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as C3;

                machine.Target = Machine.Factory.CreateMachine<C3>();
                Envelope envelope = new Envelope();
                OtherClass3 oc = new OtherClass3(envelope);

                this.Send(machine.Target, new eUnit(envelope));

                oc.foo();
            }
        }
    }

    internal class OtherClass4
    {
        private AnotherClass4 ac;

        internal OtherClass4()
        {

        }

        internal void foo(Envelope E)
        {
            ac = new AnotherClass4();
            ac.E = E;
            ac.bar();
        }
    }

    internal class AnotherClass4
    {
        internal AnotherClass4()
        {

        }

        internal Envelope E;

        internal void bar()
        {
            E.Id = 2; // ERROR
        }
    }

    internal class D3 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as D3;

                machine.Target = Machine.Factory.CreateMachine<D3>();
                Envelope envelope = new Envelope();
                OtherClass4 oc = new OtherClass4();

                this.Send(machine.Target, new eUnit(envelope));

                oc.foo(envelope);
            }
        }
    }

    internal class OtherClass5
    {
        internal Envelope E;

        internal OtherClass5(Envelope E)
        {
            this.E = E; // ERROR
        }

        internal void foo(Envelope E)
        {
            this.E = E; // ERROR
        }
    }

    internal class E3 : Machine
    {
        private Machine Target;
        private OtherClass5 OC;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as E3;

                machine.Target = Machine.Factory.CreateMachine<E3>();
                Envelope envelope = new Envelope();
                machine.OC = new OtherClass5(envelope);

                // ERROR
                this.Send(machine.Target, new eUnit(envelope));

                machine.OC = new OtherClass5(envelope);
                machine.OC.foo(envelope);
            }
        }
    }

    internal class OtherClass6
    {
        private AnotherClass6 ac;

        internal OtherClass6()
        {

        }

        internal void foo(Envelope E)
        {
            AnotherClass6 ac2 = new AnotherClass6(E);
            ac = ac2;
            ac.bar();
        }
    }

    internal class AnotherClass6
    {
        internal AnotherClass6(Envelope E)
        {
            this.E = E;
        }

        internal Envelope E;

        internal void bar()
        {
            E.Id = 2; // ERROR
        }
    }

    internal class F3 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as F3;

                machine.Target = Machine.Factory.CreateMachine<F3>();
                Envelope envelope = new Envelope();
                OtherClass6 oc = new OtherClass6();

                this.Send(machine.Target, new eUnit(envelope));

                oc.foo(envelope);
            }
        }
    }

    internal class OtherClass7
    {
        private AnotherClass7 ac;

        internal OtherClass7()
        {

        }

        internal void foo(Envelope E)
        {
            AnotherClass7 ac2 = new AnotherClass7(E);
            ac = ac2;
            //ac = new AnotherClass7(new Envelope());
            ac.E = new Envelope();
            ac.bar();
        }
    }

    internal class AnotherClass7
    {
        internal AnotherClass7(Envelope E)
        {
            this.E = E;
        }

        internal Envelope E;

        internal void bar()
        {
            E.Id = 2;
        }
    }

    internal class G3 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as G3;

                machine.Target = Machine.Factory.CreateMachine<G3>();
                Envelope envelope = new Envelope();
                OtherClass7 oc = new OtherClass7();

                this.Send(machine.Target, new eUnit(envelope));

                oc.foo(envelope);
            }
        }
    }

    #endregion
}
