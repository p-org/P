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
            (isSafeMap).Add("PONG", false);
            (renameMap).Add("Main", "Main");
            (renameMap).Add("PONG", "PONG");
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createMachineMap).Add("PONG", CreateMachine_PONG);
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("PONG", "PONG");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Main", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("PONG", "PONG");
            (_temp).Add("Main", "Main");
            (linkMap).Add("PONG", _temp);
        }
    }
}
