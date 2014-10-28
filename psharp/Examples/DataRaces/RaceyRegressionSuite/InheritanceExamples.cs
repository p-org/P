using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace InheritanceExamples
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

    internal class Box
    {
        internal Envelope Envelope;

        internal virtual void foo(Envelope e)
        {

        }

        internal virtual void bar(Envelope e)
        {

        }
    }

    internal class AmazingBox : Box
    {
        internal override void foo(Envelope e)
        {
            e.Id = 10;
        }

        internal override void bar(Envelope e)
        {
            base.Envelope.Id = 10;
        }
    }

    #endregion

    #region Machines

    internal class A2 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as A2;

                machine.Target = Machine.Factory.CreateMachine<A2>();
                Envelope envelope = new Envelope();
                Box box = new AmazingBox();

                this.Send(machine.Target, new eUnit(envelope));

                box.foo(envelope); // ERROR
            }
        }
    }

    internal class B2 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as B2;

                machine.Target = Machine.Factory.CreateMachine<B2>();
                Envelope envelope = new Envelope();
                Box box;
                box = new Box();
                if (envelope.Id == 10)
                {
                    box = new Box();
                }
                else
                {
                    box = new AmazingBox();
                }

                this.Send(machine.Target, new eUnit(envelope));

                box.foo(envelope); // ERROR
            }
        }
    }

    internal class C2 : Machine
    {
        private Machine Target;
        private bool Check;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as C2;

                machine.Target = Machine.Factory.CreateMachine<C2>();
                Envelope envelope = new Envelope();
                Box box = machine.foo();

                this.Send(machine.Target, new eUnit(envelope));

                box.foo(envelope); // ERROR
            }
        }

        private Box foo()
        {
            if (this.Check)
            {
                return new Box();
            }
            else
            {
                return new AmazingBox();
            }
        }
    }

    internal class D2 : Machine
    {
        private Machine Target;
        private bool Check;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as D2;

                machine.Target = Machine.Factory.CreateMachine<D2>();
                Envelope envelope = new Envelope();
                Box box = machine.foo();

                this.Send(machine.Target, new eUnit(envelope));

                box.foo(envelope); // ERROR
            }
        }

        private Box foo()
        {
            if (this.Check)
            {
                return new Box();
            }
            else
            {
                return this.bar();
            }
        }

        private Box bar()
        {
            return new AmazingBox();
        }
    }

    internal class E2 : Machine
    {
        private Machine Target;
        private bool Check;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as E2;

                machine.Target = Machine.Factory.CreateMachine<E2>();
                Envelope envelope = new Envelope();
                Box box = machine.foo();

                this.Send(machine.Target, new eUnit(envelope));

                box.foo(envelope); // ERROR
            }
        }

        private Box foo()
        {
            if (this.Check)
            {
                return new Box();
            }
            else
            {
                return this.bar();
            }
        }

        private Box bar()
        {
            Box box = new AmazingBox();
            return box;
        }
    }

    internal class F2 : Machine
    {
        private Machine Target;
        private bool Check;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as F2;

                machine.Target = Machine.Factory.CreateMachine<F2>();
                Envelope envelope = new Envelope();
                Box box = machine.foo();

                this.Send(machine.Target, new eUnit(envelope));

                box.foo(envelope); // ERROR
            }
        }

        private Box foo()
        {
            Box someBox = new AmazingBox();
            Box anotherBox;
            anotherBox = new Box();
            anotherBox = someBox;

            if (this.Check)
            {
                return new Box();
            }
            else
            {
                return anotherBox;
            }
        }
    }

    internal class G2 : Machine
    {
        private Machine Target;
        private bool Check;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as G2;

                machine.Target = Machine.Factory.CreateMachine<G2>();
                Envelope envelope = new Envelope();
                Box box = machine.foo();

                this.Send(machine.Target, new eUnit(envelope));

                machine.bar(box, envelope);
            }
        }

        private Box foo()
        {
            Box someBox = new AmazingBox();
            Box anotherBox;
            anotherBox = new Box();
            anotherBox = someBox;

            if (this.Check)
            {
                return new Box();
            }
            else
            {
                return anotherBox;
            }
        }

        private void bar(Box box, Envelope envelope)
        {
            foobar(box, envelope);
        }

        private void foobar(Box box, Envelope envelope)
        {
            box.foo(envelope); // ERROR
        }
    }

    internal abstract class H2 : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as H2;

                machine.Target = Machine.Factory.CreateMachine<H2>();
                Envelope envelope = new Envelope();

                this.Send(machine.Target, new eUnit(envelope));

                machine.foo(envelope);
            }
        }

        protected abstract void foo(Envelope e);
    }

    internal class I2 : H2
    {
        protected override void foo(Envelope e)
        {
            e.Id = 2;
        }
    }

    #endregion
}
