// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Runtime.Serialization;
using PChecker.SystematicTesting;

namespace PChecker.Runtime.StateMachines
{
    /// <summary>
    /// Unique state machine id.
    /// </summary>
    [DataContract]
    public sealed class StateMachineId : IEquatable<StateMachineId>, IComparable<StateMachineId>
    {
        /// <summary>
        /// Unique id, when <see cref="NameValue"/> is empty.
        /// </summary>
        public ulong Value
        {
            get
            {
                if (PendingRuntime != null)
                {
                    Bind();
                }

                return value;
            }
        }

        [DataMember(Name = nameof(Value))]
        private ulong value;

        /// <summary>
        /// Unique id, when non-empty.
        /// </summary>
        [DataMember]
        public readonly string NameValue;

        /// <summary>
        /// The type of the state machine associated with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Name used for logging.
        /// </summary>
        public string Name
        {
            get
            {
                if (PendingRuntime != null)
                {
                    Bind();
                }

                return name;
            }
        }

        [DataMember(Name = nameof(Name))]
        private string name;

        /// <summary>
        /// The name passed when the id was created, until <see cref="Bind"/> allocates its value.
        /// </summary>
        private string PendingName;

        /// <summary>
        /// The runtime that allocates the value of an id created by <see cref="CreateUnbound"/>,
        /// or null once it is allocated.
        /// </summary>
        private ControlledRuntime PendingRuntime;

        /// <summary>
        /// True if <see cref="NameValue"/> is used as the unique id, else false.
        /// </summary>
        public bool IsNameUsedForHashing => NameValue.Length > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineId"/> class.
        /// </summary>
        internal StateMachineId(Type type, string name, ControlledRuntime runtime, bool useNameForHashing = false)
        {
            Type = type.FullName;
            if (useNameForHashing)
            {
                value = 0;
                NameValue = name;
                this.name = NameValue;
            }
            else
            {
                NameValue = string.Empty;
                SetValue(runtime.GetNextOperationId(), name);
            }
        }

        private StateMachineId(Type type, string name, ControlledRuntime runtime)
        {
            Type = type.FullName;
            NameValue = string.Empty;
            PendingName = name;
            PendingRuntime = runtime;
        }

        /// <summary>
        /// Creates an id whose value is allocated only by <see cref="Bind"/>, or on first use.
        /// </summary>
        /// <remarks>
        /// Foreign code gets the id of the state machine it creates right away, but the machine is
        /// created only at the Create scheduling point once the foreign call returns. Binding the
        /// value there allocates it in the same order as a P <c>new</c>, which allocates it right
        /// after its scheduling point, so ids and schedules are the same. If foreign code hashes,
        /// compares or prints the id before that, the value is allocated then.
        /// </remarks>
        internal static StateMachineId CreateUnbound(Type type, string name, ControlledRuntime runtime) =>
            new StateMachineId(type, name, runtime);

        /// <summary>
        /// Allocates the value of an id created by <see cref="CreateUnbound"/>, if not yet allocated.
        /// </summary>
        internal void Bind()
        {
            var runtime = PendingRuntime;
            if (runtime is null)
            {
                return;
            }

            PendingRuntime = null;
            SetValue(runtime.GetNextOperationId(), PendingName);
            PendingName = null;
        }

        private void SetValue(ulong id, string machineName)
        {
            value = id;
            name = string.Format(CultureInfo.InvariantCulture, "{0}({1})",
                string.IsNullOrEmpty(machineName) ? Type : machineName, id.ToString());
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is StateMachineId id)
            {
                // Use same machanism for hashing.
                if (IsNameUsedForHashing != id.IsNameUsedForHashing)
                {
                    return false;
                }

                return IsNameUsedForHashing ?
                    NameValue.Equals(id.NameValue) :
                    Value == id.Value;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            var hash = 17;
            hash = (hash * 23) + (IsNameUsedForHashing ? NameValue.GetHashCode() : Value.GetHashCode());
            return hash;
        }

        /// <summary>
        /// Returns a string that represents the current state machine id.
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// Indicates whether the specified <see cref="StateMachineId"/> is equal
        /// to the current <see cref="StateMachineId"/>.
        /// </summary>
        public bool Equals(StateMachineId other) => Equals((object)other);

        /// <summary>
        /// Compares the specified <see cref="StateMachineId"/> with the current
        /// <see cref="StateMachineId"/> for ordering or sorting purposes.
        /// </summary>
        public int CompareTo(StateMachineId other) => string.Compare(Name, other?.Name);

        bool IEquatable<StateMachineId>.Equals(StateMachineId other) => Equals(other);

        int IComparable<StateMachineId>.CompareTo(StateMachineId other) => string.Compare(Name, other?.Name);
    }
}