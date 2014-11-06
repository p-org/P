using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace GenericExamples
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

    [Main]
    internal class A : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as A;

                machine.Target = Machine.Factory.CreateMachine<A>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));
            }
        }

        private class Second : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as A;

                // ERROR
                machine.Target = Machine.Factory.CreateMachine<A>(machine.Envelope);
            }
        }

        private class Third : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as A;

                // ERROR
                machine.Target = Machine.Factory.CreateMachine<A>(machine.Envelope as Object);
            }
        }
    }

    internal class B : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as B;

                machine.Target = Machine.Factory.CreateMachine<B>();
                machine.Envelope = new Envelope("London", 0);

                // ERROR
                this.Send(machine.Target, new eUnit(machine.Envelope));
            }
        }
    }

    internal class C : Machine
    {
        private Machine Target;
        private int Num;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as C;

                machine.Target = Machine.Factory.CreateMachine<C>();
                machine.Num = 10;

                this.Send(machine.Target, new eUnit(machine.Num));
            }
        }
    }

    internal class D : Machine
    {
        private Machine Target;
        private int Num;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as D;

                machine.Target = Machine.Factory.CreateMachine<D>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                envelope.Id = 10;            // ERROR
                int k = envelope.Id;         // ERROR
                machine.Num = k;
                machine.Num = envelope.Id;   // ERROR
                envelope.Id = machine.Num;   // ERROR
            }
        }
    }

    internal class E : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as E;

                machine.Target = Machine.Factory.CreateMachine<E>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                machine.Envelope = envelope;  // ERROR
            }
        }
    }

    internal class F : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as F;

                machine.Target = Machine.Factory.CreateMachine<F>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                envelope = machine.Envelope;
                machine.Envelope = envelope;
            }
        }
    }

    internal class G : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as G;

                machine.Target = Machine.Factory.CreateMachine<G>();
                machine.Envelope = new Envelope("London", 0);

                // ERROR
                this.Send(machine.Target, new eUnit(machine.Envelope));

                int k = machine.Envelope.Id;             // ERROR
                machine.Envelope.Address = "Hong-Kong";  // ERROR
            }
        }

        private class Second : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as G;

                machine.Target = Machine.Factory.CreateMachine<G>();
                machine.Envelope = new Envelope("London", 0);

                // ERROR
                this.Send(machine.Target, new eUnit(new Tuple<Machine, Envelope>(
                    machine, machine.Envelope)));

                int k = machine.Envelope.Id;             // ERROR
                machine.Envelope.Address = "Hong-Kong";  // ERROR
            }
        }
    }

    internal class H : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as H;

                machine.Target = Machine.Factory.CreateMachine<H>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                int k = envelope.Id;             // ERROR
                envelope.Address = "Hong-Kong";  // ERROR
            }
        }
    }

    internal class I : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as I;

                machine.Target = Machine.Factory.CreateMachine<I>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope = envelope;

                this.Send(machine.Target, new eUnit(envelope));

                int k = otherEnvelope.Id;             // ERROR
                otherEnvelope.Address = "Hong-Kong";  // ERROR
            }
        }
    }

    internal class J : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as J;

                machine.Target = Machine.Factory.CreateMachine<J>();
                machine.Envelope = new Envelope("London", 0);
                Envelope otherEnvelope = machine.Envelope;

                // ERROR
                this.Send(machine.Target, new eUnit(machine.Envelope));

                int k = otherEnvelope.Id;             // ERROR
                otherEnvelope.Address = "Hong-Kong";  // ERROR
            }
        }
    }

    internal class K : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as K;

                machine.Target = Machine.Factory.CreateMachine<K>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                envelope = new Envelope("Tokyo", 1);
                int k = envelope.Id;
                envelope.Address = "Hong-Kong";
            }
        }
    }

    internal class L : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as L;

                machine.Target = Machine.Factory.CreateMachine<L>();
                Envelope envelope = new Envelope("London", 0);

                machine.foo(envelope);

                this.Send(machine.Target, new eUnit(envelope));

                machine.foo(envelope);
            }
        }

        private void foo(Envelope envelope)
        {
            int k = envelope.Id;             // ERROR
            envelope.Address = "Hong-Kong";  // ERROR
        }
    }

    internal class M : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as M;

                machine.Target = Machine.Factory.CreateMachine<M>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                machine.foo(envelope);
            }
        }

        private void foo(Envelope envelope)
        {
            envelope = new Envelope();
            int k = envelope.Id;
            envelope.Address = "Hong-Kong";
        }
    }

    internal class N : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as N;

                machine.Target = Machine.Factory.CreateMachine<N>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                machine.foo(envelope);
            }
        }

        private void foo(Envelope envelope)
        {
            this.Envelope = envelope;           // ERROR
            envelope = new Envelope();
            this.Envelope.Id = 10;              // ERROR
            int k = this.Envelope.Id;           // ERROR
            envelope.Address = "Hong-Kong";
            this.Envelope.Address = "Tokyo";    // ERROR
        }
    }

    internal class O : Machine
    {
        private Machine Target;
        private Letter Letter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as O;

                machine.Target = Machine.Factory.CreateMachine<O>();
                Envelope envelope = new Envelope("London", 0);

                machine.foo(envelope);

                this.Send(machine.Target, new eUnit(envelope));
            }
        }

        private void foo(Envelope envelope)
        {
            this.Letter = envelope.Letter;         // ERROR
        }
    }

    internal class P : Machine
    {
        private Machine Target;
        private Letter Letter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as P;

                machine.Target = Machine.Factory.CreateMachine<P>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope = envelope;

                machine.foo(otherEnvelope);

                this.Send(machine.Target, new eUnit(envelope));

                machine.bar(otherEnvelope.Letter);
                otherEnvelope.Letter.Words = "???";     // ERROR

                envelope = new Envelope();
                machine.foobar(envelope, otherEnvelope.Letter);
            }
        }

        private void foo(Envelope envelope)
        {
            this.Letter = envelope.Letter;         // ERROR
        }

        private void bar(Letter letter)
        {
            letter.Words = "???";         // ERROR
        }

        private void foobar(Envelope envelope, Letter letter)
        {
            string str = letter.Words;         // ERROR
            envelope.Id = 5;
        }
    }

    internal class Q : Machine
    {
        private Machine Target;
        private Envelope Envelope;
        private int Num;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Q;

                int num = (int)this.Payload;

                machine.Target = Machine.Factory.CreateMachine<Q>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope;
                otherEnvelope = new Envelope();

                if (num == 0)
                {
                    otherEnvelope = envelope;
                }
                else
                {
                    otherEnvelope = new Envelope();
                }

                this.Send(machine.Target, new eUnit(otherEnvelope));

                otherEnvelope.Id = 10; // ERROR
                envelope.Id = 10; // ERROR
            }
        }

        private class Second : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Q;

                int num = (int)this.Payload;

                machine.Target = Machine.Factory.CreateMachine<Q>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope = new Envelope();

                if (num == 0)
                {
                    otherEnvelope = envelope;
                }
                else
                {
                    otherEnvelope = new Envelope();
                }

                otherEnvelope = new Envelope();

                this.Send(machine.Target, new eUnit(otherEnvelope));

                otherEnvelope.Id = 10; // ERROR
                envelope.Id = 10;
            }
        }

        private class Third : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Q;

                int num = (int)this.Payload;

                machine.Target = Machine.Factory.CreateMachine<Q>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope = new Envelope();

                if (num == 0)
                {
                    machine.Envelope = envelope;
                }
                else
                {
                    if (num == 0)
                    {
                        otherEnvelope = envelope;
                    }
                    else
                    {
                        otherEnvelope = new Envelope();
                    }

                    this.Send(machine.Target, new eUnit(otherEnvelope));
                }

                otherEnvelope.Id = 10; // ERROR
                envelope.Id = 10; // ERROR
            }
        }

        private class Fourth : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Q;

                int num = (int)this.Payload;

                machine.Target = Machine.Factory.CreateMachine<Q>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope = new Envelope();

                machine.Envelope = envelope; // ERROR

                if (num == 0)
                {
                    otherEnvelope = envelope;
                }
                else
                {
                    otherEnvelope = new Envelope();
                }

                this.Send(machine.Target, new eUnit(otherEnvelope));

                machine.Envelope.Letter.Words = "???"; // ERROR
                machine.foo(envelope.Id);
            }
        }

        private void foo(int num)
        {
            this.Num = num;
        }
    }

    internal class R : Machine
    {
        private Machine Target;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as R;

                machine.Target = Machine.Factory.CreateMachine<R>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                this.Send(machine.Target, new eUnit(envelope)); // ERROR
            }
        }

        private class Second : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as R;

                machine.Target = Machine.Factory.CreateMachine<R>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                machine.foo(envelope); // ERROR
            }
        }

        private class Third : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as R;

                machine.Target = Machine.Factory.CreateMachine<R>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                // ERROR
                machine.Target = Machine.Factory.CreateMachine<R>(envelope);

                machine.bar(envelope); // ERROR
            }
        }

        private class Fourth : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as R;

                machine.Target = Machine.Factory.CreateMachine<R>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                // ERROR
                Console.WriteLine(envelope.Id);
            }
        }

        private void foo(Envelope envelope)
        {
            this.Send(this.Target, new eUnit(envelope));
        }

        private void bar(Envelope envelope)
        {
            this.Send(this.Target, new eUnit(envelope));
        }
    }

    internal class S : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as S;

                machine.Target = Machine.Factory.CreateMachine<S>();
                Envelope envelope = new Envelope("London", 0);
                envelope = machine.Envelope; // ERROR

                machine.foo(envelope);
            }
        }

        private class Second : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as S;

                machine.Target = Machine.Factory.CreateMachine<S>();
                Envelope envelope = machine.Envelope; // ERROR

                machine.foo(envelope);
            }
        }

        private void foo(Envelope envelope)
        {
            this.Send(this.Target, new eUnit(envelope));
        }
    }

    internal class T : Machine
    {
        private Machine Target;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as T;

                machine.Target = Machine.Factory.CreateMachine<T>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope;

                this.Send(machine.Target, new eUnit(envelope));

                otherEnvelope = machine.foo(envelope);
                otherEnvelope.Id = 100; // ERROR
            }
        }

        private class Second : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as T;

                machine.Target = Machine.Factory.CreateMachine<T>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                envelope = machine.foo(envelope);
                envelope.Id = 100; // ERROR
            }
        }

        private class Third : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as T;

                machine.Target = Machine.Factory.CreateMachine<T>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                envelope = machine.bar(envelope);
                envelope.Id = 100;
            }
        }

        private class Fourth : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as T;

                machine.Target = Machine.Factory.CreateMachine<T>();
                Envelope envelope = new Envelope("London", 0);

                this.Send(machine.Target, new eUnit(envelope));

                envelope = machine.foobar(envelope);
                envelope.Id = 100; // ERROR
            }
        }

        private Envelope foo(Envelope envelope)
        {
            if (envelope.Id == 10)
            {
                return new Envelope();
            }
            else
            {
                return envelope;
            }
        }

        private Envelope bar(Envelope envelope)
        {
            return new Envelope();
        }

        private Envelope foobar(Envelope envelope)
        {
            envelope = foo(envelope);
            return envelope;
        }
    }

    internal class U : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as U;

                machine.Target = Machine.Factory.CreateMachine<U>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope = machine.foo(envelope);

                this.Send(machine.Target, new eUnit(otherEnvelope));
            }
        }

        private class Second : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as U;

                machine.Target = Machine.Factory.CreateMachine<U>();
                Envelope envelope = new Envelope("London", 0);
                Envelope otherEnvelope = envelope;
                envelope = machine.bar(envelope);

                this.Send(machine.Target, new eUnit(otherEnvelope));
            }
        }

        private Envelope foo(Envelope envelope)
        {
            if (envelope.Id == 100)
            {
                Envelope otherEnvelope = new Envelope();
                otherEnvelope = this.Envelope; // ERROR
                return otherEnvelope;
            }
            else
            {
                return this.Envelope; // ERROR
            }
        }

        private Envelope bar(Envelope envelope)
        {
            return foobar(envelope);
        }

        private Envelope foobar(Envelope envelope)
        {
            Envelope otherEnvelope = new Envelope();
            otherEnvelope = this.Envelope; // ERROR
            return otherEnvelope;
        }
    }

    internal class V : Machine
    {
        private Machine Target;
        private Envelope Envelope;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as V;

                machine.Target = Machine.Factory.CreateMachine<V>();
                Envelope envelope = new Envelope("London", 0);

                machine.foo(0, envelope, 1);

                envelope.Letter.Words = "???"; // ERROR
            }
        }

        private void foo(int k, Envelope envelope, int l)
        {
            this.bar(2, envelope);
        }

        private void bar(int z, Envelope envelope)
        {
            this.Send(this.Target, new eUnit(envelope));
        }
    }

    internal class W : Machine
    {
        private Machine Target;
        private Object Obj;

        [Initial]
        private class First : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as W;

                machine.Target = Machine.Factory.CreateMachine<W>();
                machine.Obj = this.Payload; // ERROR

                this.Send(machine.Target, new eUnit(this.Payload));
            }
        }
    }

    internal class X : Machine
    {
        private Machine Target;
        private int Id;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as X;

                machine.Target = Machine.Factory.CreateMachine<X>();

                machine.foo();
            }
        }

        private void foo()
        {
            Envelope envelope = new Envelope();
            envelope.Id = this.Id;
            this.Id = envelope.Id;
            envelope.Id = this.bar();

            this.Send(this.Target, new eUnit(envelope));
        }

        private int bar()
        {
            return this.Id;
        }
    }

    internal class Y : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Y;

                machine.Target = Machine.Factory.CreateMachine<Y>();

                this.Send(machine.Target, new eUnit(this.Machine));
            }
        }
    }

    #endregion
}
