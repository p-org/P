using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;

namespace Test
{
    internal class NamedTuple1 : IPMutableReadOnlyTuple<int,bool>
    {
        public NamedTuple1() { }

        public NamedTuple1(IPMutableReadOnlyTuple<int,bool> other)
        {
            // TODO: add deep copying code here
            this.Item1 = other.Item1;
            this.Item2 = other.Item2;
        }

        public NamedTuple1(Tuple<int,bool> other)
        {
            // TODO: add deep copying code here
            this.Item1 = other.Item1;
            this.Item2 = other.Item2;
        }

        public int a
        {
            get { return Item1; }
            set { Item1 = value; }
        }

        public bool b
        {
            get { return Item2; }
            set { Item2 = value; }
        }

        #region IPMutableReadOnlyTuple implementation
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Item1 { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool Item2 { get; set; }
        #endregion
    }

    internal class M : Machine
    {
        [Start]
        [OnEntry(nameof(AnonFun2))]
        [OnExit(nameof(AnonFun0))]
        class S : MachineState {}

    	internal NamedTuple1 foo(int x)
    	{
    	    // variable declarations
    	    int z;

    	    // function body
    	    /* TODO */
    	}

    	internal void AnonFun0()
    	{
    	    // variable declarations

    	    // function body
    	    /* TODO */
    	}

    	internal void AnonFun1()
    	{
    	    // variable declarations

    	    // function body
    	    /* TODO */
    	}

    	internal void AnonFun2()
    	{
    	    // variable declarations

    	    // function body
    	    /* TODO */
    	}
    }

    internal class halt : Event
    {
        public halt()
            : base(1, -1)
        {
        }
    }

    internal class null : Event
    {
        public null()
            : base(1, -1)
        {
        }
    }
}