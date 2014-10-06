//-----------------------------------------------------------------------
// <copyright file="PSharpToPLang.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;

namespace Microsoft.PSharp.Compilation
{
    internal sealed class PSharpToPLang
    {
        /// <summary>
        /// String containing the compiled program.
        /// </summary>
        private List<string> CompiledProgram;

        /// <summary>
        /// Set of state machine types in the runtime.
        /// </summary>
        private HashSet<Type> MachineTypes;

        /// <summary>
        /// Set of monitor types in the runtime.
        /// </summary>
        private HashSet<Type> MonitorTypes;

        /// <summary>
        /// Set of event types in the runtime.
        /// </summary>
        private HashSet<Type> EventTypes;

        /// <summary>
        /// Constructor of the PCompiler class.
        /// </summary>
        /// <param name="machines">MachineTypes</param>
        /// <param name="monitors">MonitorTypes</param>
        /// <param name="events">EventTypes</param>
        public PSharpToPLang(HashSet<Type> machines,
            HashSet<Type> monitors, HashSet<Type> events)
        {
            this.CompiledProgram = new List<string>();
            this.MachineTypes = machines;
            this.MonitorTypes = monitors;
            this.EventTypes = events;
        }

        /// <summary>
        /// Compiles to P.
        /// </summary>
        public void DoIt()
        {
            this.ParseEvents();
            this.ParseMachines();
            this.ParseMonitors();
            this.WriteToFile();
        }

        /// <summary>
        /// Parses all events.
        /// </summary>
        private void ParseEvents()
        {
            foreach (var e in this.EventTypes)
                this.ParseEvent(e);
            this.CompiledProgram.Add("");
        }

        /// <summary>
        /// Parses all machines.
        /// </summary>
        private void ParseMachines()
        {
            foreach (var m in this.MachineTypes)
                this.ParseMachine(m);
        }

        /// <summary>
        /// Parses all monitors.
        /// </summary>
        private void ParseMonitors()
        {
            foreach (var m in this.MonitorTypes)
                this.ParseMonitor(m);
        }

        /// <summary>
        /// Parses the given event.
        /// </summary>
        /// <param name="e">Event</param>
        private void ParseEvent(Type e)
        {
            string str = "event ";
            str += e.Name;
            str += ";";
            this.CompiledProgram.Add(str);
        }

        /// <summary>
        /// Parses the given machine.
        /// </summary>
        /// <param name="m">Machine</param>
        private void ParseMachine(Type m)
        {
            string str = "";

            if (m.IsDefined(typeof(Main), false))
                str += "main ";
            if (m.IsDefined(typeof(Ghost), false))
                str += "model ";

            str += "machine " + m.Name + " {";
            this.CompiledProgram.Add(str);

            this.ParseStates(m);
            this.ParseActions(m);

            this.CompiledProgram.Add("}");
            this.CompiledProgram.Add("");
        }

        /// <summary>
        /// Parses the given monitor.
        /// </summary>
        /// <param name="m">Monitor</param>
        private void ParseMonitor(Type m)
        {
            string str = "";

            str += "monitor " + m.Name + " {";
            this.CompiledProgram.Add(str);

            this.ParseStates(m);
            this.ParseActions(m);

            this.CompiledProgram.Add("}");
            this.CompiledProgram.Add("");
        }

        /// <summary>
        /// Parses the actions of the given machine.
        /// </summary>
        /// <param name="m">Machine</param>
        private void ParseActions(Type m)
        {
            var machineActions = this.ParseActionBindings(m);

            if (machineActions == null)
                return;

            HashSet<MethodInfo> actions = new HashSet<MethodInfo>();
            foreach (var state in machineActions)
            {
                foreach (var pair in state.Value)
                {
                    if (actions.Any(val => val.Name.Equals(pair.Value.Method.Name)))
                        continue;
                    actions.Add(pair.Value.Method);
                }
            }

            string str = "";
            foreach (var action in actions)
            {
                str = "  action " + action.Name + " {";
                this.CompiledProgram.Add(str);
                this.CompiledProgram.Add("  }");
                this.CompiledProgram.Add("");
            }
        }

        /// <summary>
        /// Parses the states of the given machine.
        /// </summary>
        /// <param name="m">Machine</param>
        private void ParseStates(Type m)
        {
            var machineActions = this.ParseActionBindings(m);
            var machineSteps = this.ParseStepTransitions(m);
            var machineCalls = this.ParseCallTransitions(m);

            Type machineType = m;
            while (machineType != typeof(Machine))
            {
                foreach (var s in machineType.GetNestedTypes(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (s.IsClass && s.IsSubclassOf(typeof(State)))
                    {
                        string str = "";

                        if (s.IsDefined(typeof(Initial), false))
                            str += "  start ";
                        else
                            str += "  ";

                        str += "state " + s.Name + " {";
                        this.CompiledProgram.Add(str);

                        if (machineActions != null && machineActions.Keys.Any(val =>
                            val.Name.Equals(s.Name)))
                        {
                            var actionsKey = machineActions.Keys.First(val =>
                                val.Name.Equals(s.Name));

                            foreach (var action in machineActions[actionsKey])
                            {
                                str = "    on " + action.Key.Name +
                                    " do " + action.Value.Method.Name + ";";
                                this.CompiledProgram.Add(str);
                            }
                        }

                        if (machineSteps != null && machineSteps.Keys.Any(val =>
                            val.Name.Equals(s.Name)))
                        {
                            var stepsKey = machineSteps.Keys.First(val =>
                                val.Name.Equals(s.Name));

                            foreach (var step in machineSteps[stepsKey])
                            {
                                str = "    on " + step.Key.Name +
                                    " goto " + step.Value.Item1.Name + ";";
                                this.CompiledProgram.Add(str);
                            }
                        }

                        if (machineCalls != null && machineCalls.Keys.Any(val =>
                            val.Name.Equals(s.Name)))
                        {
                            var callsKey = machineCalls.Keys.First(val =>
                                val.Name.Equals(s.Name));

                            foreach (var call in machineCalls[callsKey])
                            {
                                str = "    on " + call.Key.Name +
                                    " push " + call.Value.Name + ";";
                                this.CompiledProgram.Add(str);
                            }
                        }

                        this.CompiledProgram.Add("  }");
                        this.CompiledProgram.Add("");
                    }
                }

                machineType = machineType.BaseType;
            }
        }

        /// <summary>
        /// Parses and returns the action bindings of the given machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <returns>Dictionary<Type, ActionBindings></returns>
        private Dictionary<Type, ActionBindings> ParseActionBindings(Type m)
        {
            MethodInfo actions = null;
            Type machineType = m;

            while (machineType != typeof(Machine))
            {
                actions = machineType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic
                    | BindingFlags.Public | BindingFlags.DeclaredOnly).FirstOrDefault(val =>
                        val.Name.Equals("DefineActionBindings"));

                if (actions != null)
                    break;

                machineType = machineType.BaseType;
            }

            if (actions == null)
                return null;

            object classInstance = Activator.CreateInstance(m);
            return (Dictionary<Type, ActionBindings>)actions.Invoke(classInstance, null);
        }

        /// <summary>
        /// Parses and returns the step state transitions of the given machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        private Dictionary<Type, StepStateTransitions> ParseStepTransitions(Type m)
        {
            MethodInfo steps = null;
            Type machineType = m;

            while (machineType != typeof(Machine))
            {
                steps = machineType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic
                    | BindingFlags.Public | BindingFlags.DeclaredOnly).FirstOrDefault(val =>
                        val.Name.Equals("DefineStepTransitions"));

                if (steps != null)
                    break;

                machineType = machineType.BaseType;
            }

            if (steps == null)
                return null;

            object classInstance = Activator.CreateInstance(m);
            return (Dictionary<Type, StepStateTransitions>)steps.Invoke(classInstance, null);
        }

        /// <summary>
        /// Parses and returns the call state transitions of the given machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        private Dictionary<Type, CallStateTransitions> ParseCallTransitions(Type m)
        {
            MethodInfo calls = null;
            Type machineType = m;

            while (machineType != typeof(Machine))
            {
                calls = machineType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic
                    | BindingFlags.Public | BindingFlags.DeclaredOnly).FirstOrDefault(val =>
                        val.Name.Equals("DefineCallTransitions"));

                if (calls != null)
                    break;

                machineType = machineType.BaseType;
            }

            if (calls == null)
                return null;

            object classInstance = Activator.CreateInstance(m);
            return (Dictionary<Type, CallStateTransitions>)calls.Invoke(classInstance, null);
        }

        /// <summary>
        /// Writes the compiled program to a file.
        /// </summary>
        private void WriteToFile()
        {
            File.WriteAllLines(Directory.GetCurrentDirectory() +
                Path.DirectorySeparatorChar + "machines.p", this.CompiledProgram);
        }
    }
}
