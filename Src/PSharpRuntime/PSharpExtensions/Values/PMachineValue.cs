using System.Collections.Generic;
using Microsoft.PSharp;

namespace PrtSharp.Values
{
    public class PMachineValue
    {
        public MachineId Id { get; }
        public List<string> Permissions { get; }

        public PMachineValue(MachineId machine, List<string> permissions)
        {
            this.Id = machine;
            this.Permissions = permissions;
        }
    }
}
