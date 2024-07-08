using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    internal class EventGenerator : JavaSourceGenerator {

        internal EventGenerator(ICompilerConfiguration job, string filename) : base(job, filename)
        {
        }

        /// <summary>
        /// Generates Java code for a given compilation job's used events.
        ///
        /// We only emit code for events that are actually used in Monitors (i.e. they
        /// appear in at least one Monitor's `observes` set.)
        /// </summary>
        protected override void GenerateCodeImpl()
        {
            WriteLine($"public class {Constants.EventNamespaceName} {{");
            foreach (var e in monitoredEvents(GlobalScope.Machines))
            {
                WriteEventDecl(e);
                WriteLine();
            }
            WriteLine("}");
        }


        private IEnumerable<PEvent> monitoredEvents(IEnumerable<Machine> machines)
        {
            var events = new HashSet<PEvent>();

            foreach (var m in machines.Where(m => m.IsSpec))
            {
                foreach (var e in m.Observes.Events)
                {
                    events.Add(e);
                }
            }

            return events;
        }

        private string JsonObjectGet(string objName, string field, PLanguageType type = null)
        {
            if (type != null)
            {
                if (type is EnumType enumType)
                {
                    return $"(({Constants.TypesNamespaceName}.{enumType.EnumDecl.Name}) (payload.get(\"{field}\")))";
                    // return $"{Constants.TypesNamespaceName}.{enumType.EnumDecl.Name}.from(" + JsonObjectGet(objName, field, PrimitiveType.Int) + ".intValue())";
                }
                else
                {
                    var javaType = Types.JavaTypeFor(type).ReferenceTypeName;
                    if (!javaType.Equals("Object"))
                    {
                        return $"({objName}.get{javaType}(\"{field}\"))";
                    }
                }
            }
            return $"({objName}.get(\"{field}\"))";
        }

        internal void WriteEventDecl(PEvent e, bool pinfer = false)
        {
            var eventName = Names.GetNameForDecl(e);
            var argType = Types.JavaTypeFor(e.PayloadType);

            var payloadType = argType.TypeName;
            var payloadRefType = argType.ReferenceTypeName;

            if (pinfer)
            {
                WriteLine($"public static class {eventName} extends EventBase implements Serializable {{");
            }
            else
            {
                WriteLine($"public static class {eventName} extends {Constants.PEventsClass}<{payloadRefType}> implements Serializable {{");
            }

            var hasPayload = !(argType is TypeManager.JType.JVoid);
            if (pinfer)
            {
                var payloadName = "payload";
                WriteLine($"private final int index;");
                WriteLine($"private final JSONObject {payloadName}; ");
                WriteLine($"public {eventName}(JSONObject p, int index) {{ this.{payloadName} = p; this.index = index; }}");
                WriteLine($"public int index() {{ return index; }}");
                if (!hasPayload)
                {
                    WriteLine($"public {eventName} payload() {{ return this; }}");
                }
                else if (argType.IsPrimitive)
                {
                    WriteLine($"public {payloadType} payload() {{ return {JsonObjectGet(payloadName, "payload", e.PayloadType.Canonicalize())}; }}");
                }
                else
                {
                    WriteLine($"public {eventName} payload() {{ return this; }}");
                    switch (e.PayloadType.Canonicalize())
                    {
                        case NamedTupleType tupleType:
                            foreach (var field in tupleType.Fields)
                            {
                                var fieldType = field.Type.Canonicalize();
                                var jType = Types.JavaTypeFor(fieldType);
                                if (jType.IsPrimitive || fieldType is PrimitiveType)
                                {
                                    WriteLine($"public {jType.TypeName} {field.Name}() {{ return {JsonObjectGet(payloadName, field.Name, fieldType)}; }}");
                                }
                                else if (fieldType is NamedTupleType || fieldType is MapType)
                                {
                                    WriteLine($"public JSONObject {field.Name}() {{ return {payloadName}.getJSONObject(\"{field.Name}\"); }}");
                                }
                                else if (fieldType is SequenceType || fieldType is SetType)
                                {
                                    WriteLine($"public JSONArray {field.Name}() {{ return {payloadName}.getJSONArray(\"{field.Name}\"); }}");
                                }
                                else
                                {
                                    throw new Exception($"Unsupported type for field {field.Name} of {e.PayloadType}: {fieldType}");
                                }
                            }
                            break;
                        default:
                            throw new Exception($"Unsupported type: {e.PayloadType} ({payloadType})");

                    }
                }
            }
            else
            {
                if (hasPayload)
                {
                    WriteLine($"public {eventName}({payloadType} p) {{ this.payload = p; }}");
                    WriteLine($"private {payloadType} payload; ");
                    WriteLine($"public {payloadRefType} getPayload() {{ return payload; }}");
                }
                else
                {
                    WriteLine($"public {eventName}() {{ }}");
                    WriteLine($"public Void getPayload() {{ return null; }}");
                }
            }

            WriteLine();
            WriteLine("@Override");
            Write("public String toString() {");
            if (hasPayload)
            {
                WriteLine($" return \"{eventName}[\" + payload + \"]\"; }}");
            }
            else
            {
                WriteLine($" return \"{eventName}\"; }}");
            }

            WriteLine($"}} // {eventName}");
        }
    }
}