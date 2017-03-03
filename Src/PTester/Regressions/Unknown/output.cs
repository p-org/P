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
        public partial class Events
        {
            public static PrtEventValue halt = PrtValue.halt;
            public static PrtEventValue @null = PrtValue.@null;
        }

        public Application(): base ()
        {
        }

        public Application(bool initialize): base ()
        {
            CreateSpecMachine("Spinlock");
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            Types.Types_SpinlockWithTypestate();
            Events.Events_SpinlockWithTypestate();
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("Spinlock", false);
            (renameMap).Add("Spinlock", "Spinlock");
            (renameMap).Add("Main", "Main");
            (createSpecMap).Add("Spinlock", CreateSpecMachine_Spinlock);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (specMachineMap).Add("Spinlock", new List<string>()
            {"Spinlock", "Main"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("Spinlock", "Spinlock");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Spinlock", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("Spinlock", "Spinlock");
            (linkMap).Add("Main", _temp);
        }
    }
}
