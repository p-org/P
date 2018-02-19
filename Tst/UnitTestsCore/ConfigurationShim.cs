using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace UnitTestsCore
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
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    using (Stream stream = assembly.GetManifestResourceStream(settingsResourceName))
                    using (var reader = new StreamReader(stream))
                    {
                        XDocument document = XDocument.Load(reader);
                        return document.Element(ns + "SettingsFile")
                                       .Element(ns + "Settings")
                                       .Elements(ns + "Setting")
                                       .Select(ParseSetting)
                                       .ToDictionary(kv => kv.Item1, kv => kv.Item2);
                    }
                });
        }

        public object this[string property] => configuration.Value[property];

        private static (string, object) ParseSetting(XElement setting)
        {
            string name = setting.Attribute("Name").Value;
            string typeName = setting.Attribute("Type").Value;
            string value = setting.Element(ns + "Value").Value;

            Type type = Type.GetType(typeName);
            IEnumerable<ConstructorInfo> ctors = GetSuitableConstructors(type);
            IEnumerable<MethodInfo> staticMethods = GetSuitableStaticMethods(type);

            object obj = null;
            foreach (MethodBase method in ctors.Cast<MethodBase>().Concat(staticMethods))
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
                ParameterInfo[] parameters = method.GetParameters();
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
                ParameterInfo[] parameters = ctor.GetParameters();
                return !ctor.ContainsGenericParameters &&
                       parameters.Length == 1 &&
                       parameters[0].ParameterType.IsAssignableFrom(typeof(string));
            });
        }
    }
}
