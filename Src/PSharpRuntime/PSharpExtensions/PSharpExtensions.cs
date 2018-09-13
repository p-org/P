using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public class PEvent<T> : Event, IHasPayload<T>
    {
        public T Payload { get; set; }
    }

    public interface IHasPayload<out T>
    {
        T Payload { get; }
    }

    public class PMachine : Machine
    {
        public string interfaceName;
        public List<string> sends;
        public List<string> creates;
        public Permissions self;
        
    }

    public partial class PProgram
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
            Debug.Assert(machineRef != null, $"Failed to load class {createMachine}");
            machineRef.interfaceName = createdInterface;
            machineRef.self = new Permissions(machineRef, interfaces[createdInterface].ToList());
            return machineRef.self;
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
            var type = Type.GetType(strFullyQualifiedName);
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
