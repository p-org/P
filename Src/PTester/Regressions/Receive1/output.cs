#pragma warning disable CS0162, CS0164, CS0168
using P.Runtime;
using System;
using System.Collections.Generic;

namespace P.Program
{
    using P.Runtime;
    using System;
    using System.Collections.Generic;

    public partial class Application : StateImpl
    {
        private partial class Events
        {
            public static PrtEventValue halt = new PrtEventValue(new PrtEvent("halt", new PrtNullType(), 1, false));
            public static PrtEventValue @null = new PrtEventValue(new PrtEvent("null", new PrtNullType(), 1, false));
        }

        public Application(): base ()
        {
        }

        public Application(bool initialize): base ()
        {
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("B", false);
            (renameMap).Add("B", "B");
            (renameMap).Add("Main", "Main");
            (createMachineMap).Add("B", CreateMachine_B);
            (createMachineMap).Add("Main", CreateMachine_Main);
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("B", "B");
            (_temp).Add("Main", "Main");
            (linkMap).Add("B", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("B", "B");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Main", _temp);
            Types.Types_receive1();
            Events.Events_receive1();
        }
    }
}
