// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace PChecker.Actors
{
    /// <summary>
    /// Unique actor id.
    /// </summary>
    [DataContract]
    public sealed class ActorId : IEquatable<ActorId>, IComparable<ActorId>
    {
        /// <summary>
        /// The runtime that executes the actor with this id.
        /// </summary>
        public IActorRuntime Runtime { get; private set; }

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
        /// The type of the actor associated with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Name used for logging.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Generation of the runtime that created this actor id.
        /// </summary>
        [DataMember]
        public readonly ulong Generation;

        /// <summary>
        /// Endpoint.
        /// </summary>
        [DataMember]
        public readonly string Endpoint;

        /// <summary>
        /// True if <see cref="NameValue"/> is used as the unique id, else false.
        /// </summary>
        public bool IsNameUsedForHashing => NameValue.Length > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorId"/> class.
        /// </summary>
        internal ActorId(Type type, string name, ActorRuntime runtime, bool useNameForHashing = false)
        {
            Runtime = runtime;
            Endpoint = string.Empty;

            if (useNameForHashing)
            {
                Value = 0;
                NameValue = name;
                Runtime.Assert(!string.IsNullOrEmpty(NameValue), "The actor name cannot be null when used as id.");
            }
            else
            {
                Value = runtime.GetNextOperationId();
                NameValue = string.Empty;

                // Checks for overflow.
                Runtime.Assert(Value != ulong.MaxValue, "Detected actor id overflow.");
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
        /// Bind the actor id.
        /// </summary>
        internal void Bind(ActorRuntime runtime)
        {
            Runtime = runtime;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ActorId id)
            {
                // Use same machanism for hashing.
                if (IsNameUsedForHashing != id.IsNameUsedForHashing)
                {
                    return false;
                }

                return IsNameUsedForHashing ?
                    NameValue.Equals(id.NameValue) && Generation == id.Generation :
                    Value == id.Value && Generation == id.Generation;
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
            hash = (hash * 23) + Generation.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string that represents the current actor id.
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// Indicates whether the specified <see cref="ActorId"/> is equal
        /// to the current <see cref="ActorId"/>.
        /// </summary>
        public bool Equals(ActorId other) => Equals((object)other);

        /// <summary>
        /// Compares the specified <see cref="ActorId"/> with the current
        /// <see cref="ActorId"/> for ordering or sorting purposes.
        /// </summary>
        public int CompareTo(ActorId other) => string.Compare(Name, other?.Name);

        bool IEquatable<ActorId>.Equals(ActorId other) => Equals(other);

        int IComparable<ActorId>.CompareTo(ActorId other) => string.Compare(Name, other?.Name);
    }
}