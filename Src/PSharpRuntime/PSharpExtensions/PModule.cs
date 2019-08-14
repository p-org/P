using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Plang.PrtSharp
{
    public class PModule
    {
        public static Dictionary<string, Type> interfaceDefinitionMap = new Dictionary<string, Type>();
        public static Dictionary<string, List<Type>> monitorMap = new Dictionary<string, List<Type>>();
        public static Dictionary<string, List<string>> monitorObserves = new Dictionary<string, List<string>>();

        public static IDictionary<string, Dictionary<string, string>> linkMap =
            new Dictionary<string, Dictionary<string, string>>();

        public static IMachineRuntime runtime;
    }
}