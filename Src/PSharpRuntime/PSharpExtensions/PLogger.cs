using System;
using Microsoft.PSharp;
using Microsoft.PSharp.IO;
using Plang.PrtSharp.Exceptions;

namespace Plang.PrtSharp
{
    /// <summary>
    ///     Logger that writes text to the console.
    /// </summary>
    public class PLogger : RuntimeLogWriter
    {
        public PLogger() : base()
        {

        }

        public override void OnMachineExceptionHandled(MachineId machineId, string currentStateName, string actionName,
            Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                //ignore logging
            }
            else
            {
                base.OnMachineExceptionThrown(machineId, currentStateName, actionName, ex);
            }
        }

        public override void OnMachineExceptionThrown(MachineId machineId, string currentStateName, string actionName,
            Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                //ignore logging
            }
            else
            {
                base.OnMachineExceptionThrown(machineId, currentStateName, actionName, ex);
            }
        }
    }
}