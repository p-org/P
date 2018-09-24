using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.IO;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    /// <summary>
    /// Logger that writes text to the console.
    /// </summary>
    public class PLogger : StateMachineLogger
    {
        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public override void Write(string value)
        {
            Console.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            Console.Write(format, args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public override void OnMachineExceptionHandled(MachineId machineId, string currentStateName, string actionName, Exception ex)
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

        public override void OnMachineExceptionThrown(MachineId machineId, string currentStateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                //ignore logging
            }
            else
            {
                base.OnMachineExceptionThrown(machineId, currentStateName,actionName,ex);
            }
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public override void Dispose() { }
    }
}
