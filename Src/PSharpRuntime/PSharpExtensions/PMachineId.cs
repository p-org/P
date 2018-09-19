using System.Collections.Generic;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    public class PMachineId
    {
        public MachineId Id { get; }
        public List<string> Permissions { get; }

        public PMachineId(MachineId machine, List<string> permissions)
        {
            this.Id = machine;
            this.Permissions = permissions;
        }
    }
}
