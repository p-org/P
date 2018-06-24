using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace UnitTests.Core
{
    internal class ConfigurationShim
    {
        private static readonly XNamespace ns = "http://schemas.microsoft.com/VisualStudio/2004/01/settings";

        private readonly Lazy<IDictionary<string, object>> configuration;

        public ConfigurationShim(string settingsResourceName)
        {
            configuration = new Lazy<IDictionary<string, object>>(
                () =>
                {
                    // .NET core has some issues with .settings files, so we load them directly here.
                    // Tested to work with System.Boolean properties, but should work with other 
                    // string-constructible properties
                    var assembly = Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream(settingsResourceName))
                        if (stream != null)
                            using (var reader = new StreamReader(stream))
                            {
                                var document = XDocument.Load(reader);
                                return document.Element(ns + "SettingsFile")
                                    ?.Element(ns + "Settings")
                                    ?.Elements(ns + "Setting")
                                    .Select(ParseSetting)
                                    .ToDictionary(kv => kv.Item1, kv => kv.Item2);
                            }

                    return null;
                });
        }

        public object this[string property] => configuration.Value[property];

        private static (string, object) ParseSetting(XElement setting)
        {
            var name = setting.Attribute("Name")?.Value;
            var typeName = setting.Attribute("Type")?.Value;
            var value = setting.Element(ns + "Value")?.Value;

            Debug.Assert(typeName != null, nameof(typeName) + " != null");
            var type = Type.GetType(typeName);
            var ctors = GetSuitableConstructors(type);
            var staticMethods = GetSuitableStaticMethods(type);

            object obj = null;
            foreach (var method in ctors.Cast<MethodBase>().Concat(staticMethods))
            {
                try
                {
                    obj = method.Invoke(null, new object[] {value});
                    break;
                }
                catch (TargetInvocationException)
                {
                    // ignore and try next alternative
                }
            }

            return (name, obj);
        }

        private static IEnumerable<MethodInfo> GetSuitableStaticMethods(Type type)
        {
            // To use a static method to construct a type, it must provide a method that
            // returns a subtype of itself and that method must take a single string as
            // an argument. It cannot be generic.
            return type.GetMethods().Where(method =>
            {
                var parameters = method.GetParameters();
                return !method.ContainsGenericParameters &&
                       method.IsStatic &&
                       parameters.Length == 1 &&
                       parameters[0].ParameterType.IsAssignableFrom(typeof(string)) &&
                       type.IsAssignableFrom(method.ReturnType);
            });
        }

        private static IEnumerable<ConstructorInfo> GetSuitableConstructors(Type type)
        {
            // We need a constructor of a single string parameter with no generics.
            return type.GetConstructors().Where(ctor =>
            {
                var parameters = ctor.GetParameters();
                return !ctor.ContainsGenericParameters &&
                       parameters.Length == 1 &&
                       parameters[0].ParameterType.IsAssignableFrom(typeof(string));
            });
        }
    }
}
