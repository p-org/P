// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace PChecker.SmartSockets
{
    internal class SmartSocketTypeResolver : DataContractResolver
    {
        private readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

        public SmartSocketTypeResolver()
        {
            AddBaseTypes();
        }

        public SmartSocketTypeResolver(params Type[] knownTypes)
        {
            AddTypes(knownTypes);
        }

        public SmartSocketTypeResolver(IEnumerable<Type> knownTypes)
        {
            AddTypes(knownTypes);
        }

        private void AddTypes(IEnumerable<Type> knownTypes)
        {
            AddBaseTypes();
            foreach (var t in knownTypes)
            {
                TypeMap[t.FullName] = t;
            }
        }

        private void AddBaseTypes()
        {
            foreach (var t in new Type[] { typeof(SocketMessage) })
            {
                TypeMap[t.FullName] = t;
            }
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            var fullName = typeName;
            if (!string.IsNullOrEmpty(typeNamespace))
            {
                var uri = new Uri(typeNamespace);
                var clrNamespace = uri.Segments.Last();
                fullName = clrNamespace + "." + typeName;
            }

            if (!TypeMap.TryGetValue(fullName, out var t))
            {
                t = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
            }

            return t;
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            return knownTypeResolver.TryResolveType(type, declaredType, knownTypeResolver, out typeName, out typeNamespace);
        }
    }
}
