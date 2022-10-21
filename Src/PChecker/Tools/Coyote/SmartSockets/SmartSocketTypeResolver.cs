// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.Coyote.SmartSockets
{
    public class SmartSocketTypeResolver : DataContractResolver
    {
        private readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

        public SmartSocketTypeResolver()
        {
            this.AddBaseTypes();
        }

        public SmartSocketTypeResolver(params Type[] knownTypes)
        {
            this.AddTypes(knownTypes);
        }

        public SmartSocketTypeResolver(IEnumerable<Type> knownTypes)
        {
            this.AddTypes(knownTypes);
        }

        private void AddTypes(IEnumerable<Type> knownTypes)
        {
            this.AddBaseTypes();
            foreach (var t in knownTypes)
            {
                this.TypeMap[t.FullName] = t;
            }
        }

        private void AddBaseTypes()
        {
            foreach (var t in new Type[] { typeof(SocketMessage) })
            {
                this.TypeMap[t.FullName] = t;
            }
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            string fullName = typeName;
            if (!string.IsNullOrEmpty(typeNamespace))
            {
                Uri uri = new Uri(typeNamespace);
                string clrNamespace = uri.Segments.Last();
                fullName = clrNamespace + "." + typeName;
            }

            if (!this.TypeMap.TryGetValue(fullName, out Type t))
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
