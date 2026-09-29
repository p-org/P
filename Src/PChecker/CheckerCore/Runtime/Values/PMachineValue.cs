using System.Collections.Generic;
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
            Permissions = permissions.ToList();
        }

        public PMachineValue(PMachineValue mValue)
        {
            Id = mValue.Id;
            Permissions = mValue.Permissions.ToList();
        }

        public StateMachineId Id { get; }
        public List<string> Permissions { get; }

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