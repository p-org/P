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
        [DataMember]
        public readonly ulong Value;

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
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// True if <see cref="NameValue"/> is used as the unique id, else false.
        /// </summary>
        public bool IsNameUsedForHashing => NameValue.Length > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineId"/> class.
        /// </summary>
        internal StateMachineId(Type type, string name, ControlledRuntime runtime, bool useNameForHashing = false)
        {

            if (useNameForHashing)
            {
                Value = 0;
                NameValue = name;
            }
            else
            {
                Value = runtime.GetNextOperationId();
                NameValue = string.Empty;
            }

            Type = type.FullName;
            if (IsNameUsedForHashing)
            {
                Name = NameValue;
            }
            else
            {
                Name = string.Format(CultureInfo.InvariantCulture, "{0}({1})",
                    string.IsNullOrEmpty(name) ? Type : name, Value.ToString());
            }
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