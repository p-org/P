using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PChecker.Runtime.StateMachines;

namespace PChecker.Runtime.Values
{
    public class I_Main : PMachineValue
    {
        public I_Main(StateMachineId machine, List<string> permissions) : base(machine, permissions)
        {
        }
    }

    public class PMachineValue : IPValue
    {
        public PMachineValue(StateMachineId machine, List<string> permissions)
        {
            Id = machine;
            Permissions = permissions.ToList().AsReadOnly();
        }

        public PMachineValue(PMachineValue mValue)
        {
            Id = mValue.Id;
            Permissions = mValue.Permissions;
        }

        public StateMachineId Id { get; }

        /// <summary>
        /// The events that can be sent to the machine through this reference. They cannot be changed,
        /// since copies of the reference share them.
        /// </summary>
        public ReadOnlyCollection<string> Permissions { get; }

        public bool Equals(IPValue other)
        {
            return other is PMachineValue machine && Equals(Id, machine.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Machine references are immutable, so copies share the instance.
        /// </summary>
        public IPValue Clone()
        {
            return this;
        }

        public override string ToString()
        {
            return Id.Name.Split('.').Last();
        }

        public object ToDict()
        {
            return ToString();
        }
    }
}