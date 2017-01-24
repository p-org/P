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
            CreateSpecMachine("Blah");
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            (isSafeMap).Add("Blah", false);
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("PONG", false);
            (renameMap).Add("Blah", "Blah");
            (renameMap).Add("Main", "Main");
            (renameMap).Add("PONG", "PONG");
            (createSpecMap).Add("Blah", CreateSpec_Blah);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createMachineMap).Add("PONG", CreateMachine_PONG);
            (specMachineMap).Add("Blah", new List<string>()
            {"PONG", "Blah", "Main"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("Blah", "Blah");
            (_temp).Add("Main", "Main");
            (_temp).Add("PONG", "PONG");
            (linkMap).Add("Main", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Blah", "Blah");
            (_temp).Add("Main", "Main");
            (_temp).Add("PONG", "PONG");
            (linkMap).Add("PONG", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("PONG", "PONG");
            (_temp).Add("Blah", "Blah");
            (linkMap).Add("Blah", _temp);
        }
    }
}
