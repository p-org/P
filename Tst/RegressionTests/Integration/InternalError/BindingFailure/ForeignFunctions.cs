using System;
using PChecker.Runtime.StateMachines;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void CallStaleRuntimeMethod(StateMachine machine)
        {
            throw new MissingMethodException(
                "Method not found: 'PMachineValue PChecker.Runtime.StateMachines.StateMachine.Stale()'.");
        }
    }
}
