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
            CreateSpecMachine("M");
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            Types.Types_NonConstantEventExprMonitor1();
            Events.Events_NonConstantEventExprMonitor1();
            (isSafeMap).Add("M", false);
            (isSafeMap).Add("Real2", false);
            (isSafeMap).Add("Main", false);
            (renameMap).Add("Real2", "Real2");
            (renameMap).Add("M", "M");
            (renameMap).Add("Main", "Main");
            (createMachineMap).Add("Real2", CreateMachine_Real2);
            (createSpecMap).Add("M", CreateSpecMachine_M);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (specMachineMap).Add("M", new List<string>()
            {"Main", "Real2", "M"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("M", "M");
            (_temp).Add("Real2", "Real2");
            (linkMap).Add("M", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("Real2", "Real2");
            (_temp).Add("M", "M");
            (linkMap).Add("Real2", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("M", "M");
            (_temp).Add("Real2", "Real2");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Main", _temp);
        }
    }
}
