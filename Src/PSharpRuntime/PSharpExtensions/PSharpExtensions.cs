using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    public class Permissions
    {
        private PMachine mRef;
        private List<string> permissions;

        public Permissions(PMachine machine, List<string> permissions)
        {
            this.mRef = machine;
            this.permissions = permissions;
        }
    }

    public class PMachine : Machine
    {
        public string interfaceName;
        public List<string> sends;
        public List<string> creates;
    }

    public abstract class PProgram
    {
        public static Dictionary<string, List<string>> interfaces;
        public static Dictionary<string, string> interfaceDefinitionMap;
        public static Dictionary<string, IEnumerable<string>> monitorMap;
        public static IDictionary<string, IDictionary<string, string>> linkMap = new Dictionary<string, IDictionary<string, string>>();

        public static Permissions CreateInterface(PMachine creator, string createInterface, object payload)
        {
            var createdInterface = linkMap[creator.interfaceName][createInterface];
            var createMachine = interfaceDefinitionMap[createdInterface];
            var machineRef = GetInstance(createMachine, payload) as PMachine;
            return new Permissions(machineRef, interfaces[createdInterface].ToList());
        }

        public static void SendEvent(PMachine source, Permissions target, Event ev, object payload)
        {

        }
        public static void AddInterface(string interfaceName, params string[] permissions)
        {
            interfaces.Add(interfaceName, permissions.ToList());
        }


        private static object GetInstance(string strFullyQualifiedName, object arg)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type, arg);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type, arg);
            }
            throw new InvalidOperationException($"Failed to find class {strFullyQualifiedName}");
        }

    }
}
