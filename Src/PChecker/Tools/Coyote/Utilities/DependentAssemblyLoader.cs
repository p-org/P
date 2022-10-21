// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Coyote.SystematicTesting.Utilities
{
    public sealed class DependentAssemblyLoader : MarshalByRefObject
    {
#pragma warning disable CA1822 // Mark members as static
        public string[] GetDependenciesToCoyote(string assemblyUnderTest)
        {
            // Get the case-normalized directory name
            var fullTestAssemblyName = Path.GetFullPath(assemblyUnderTest);
            var dir = Path.GetDirectoryName(Assembly.ReflectionOnlyLoadFrom(fullTestAssemblyName).Location);
            var allNames = new HashSet<string>(Directory.GetFiles(dir, "*.dll"));

            // Because Assembly.GetReferencedAssemblies does not yet have the path (assembly resolution is complex), we will assume that
            // any assembly that matches a name in the executing directory is the referenced assembly.
            var assemblyNameToFullPathMap = allNames.ToDictionary(name => Path.GetFileNameWithoutExtension(name), name => name);

            string getAssemblyFullPath(AssemblyName assemblyName) =>
                assemblyNameToFullPathMap.ContainsKey(assemblyName.Name) ? assemblyNameToFullPathMap[assemblyName.Name] : string.Empty;

            string[] getDependencies(string fullPath)
            {
                var assembly = Assembly.ReflectionOnlyLoadFrom(fullPath);
                return assembly.GetReferencedAssemblies().Select(getAssemblyFullPath).Where(x => x.Length > 0).ToArray();
            }

            bool isTarget(string fullPath) => Path.GetFileName(fullPath).StartsWith("Microsoft.Coyote.");

            return DependencyGraph.GetDependenciesToTarget(fullTestAssemblyName, allNames, name => getDependencies(name), name => isTarget(name));
        }
#pragma warning restore CA1822 // Mark members as static
    }
}
#endif
