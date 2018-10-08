using System.Collections.Generic;
using Microsoft.PSharp;

namespace PrtSharp.Values
{
    public class PMachineValue : IPrtValue
    {
        public MachineId Id { get; }
        public List<string> Permissions { get; }

        public PMachineValue(MachineId machine, List<string> permissions)
        {
            this.Id = machine;
            this.Permissions = permissions;
        }

        public bool Equals(IPrtValue other)
        {
            return other is PMachineValue machine && Equals(Id, machine.Id);
        }

        public IPrtValue Clone()
        {
            return new PMachineValue(Id, new List<string>(Permissions));
        }
    }
}
