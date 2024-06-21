using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{
    public class TraceReaderGenerator : MachineGenerator
    {
        List<PEvent> Events;
        public TraceReaderGenerator(ICompilerConfiguration job, string filename, List<PEvent> events) : base(job, filename)
        {
            Events = events;
        }

        private static string GenerateGetLong(string e, string fieldName)
        {
            return $"{e}.getLong(\"{fieldName}\")";
        }

        private static string GenerateGetMachine(string e, string fieldName)
        {
            return $"parseMachineId({e}.getString(\"${fieldName}\")";
        }

        private static string GenerateEventInit(string eventName, params string[] parameters)
        {
            return $"events.add(new {Constants.EventNamespaceName}.{eventName}({string.Join(", ", parameters.Concat(["i"]))}));";
        }

        private string GenerateEventPayloadInit(string e, string fieldName, PLanguageType type)
        {
            var t = type.Canonicalize();
            var javaType = Types.JavaTypeFor(t);
            if (t is EnumType enumType)
            {
                return $"{Constants.TypesNamespaceName}.{enumType.EnumDecl.Name}.from({GenerateGetLong(e, fieldName)})";
            }
            else if (t is PrimitiveType primitiveType || javaType.IsPrimitive)
            {
                return $"({e}.get{javaType.ReferenceTypeName}(\"{fieldName}\"))";
            }
            else if (t is NamedTupleType)
            {
                return $"({e}.getJSONObject(\"{fieldName}\"))";
            }
            else if (t is PermissionType)
            {
                return GenerateGetMachine(e, fieldName);
            }
            else
            {
                throw new Exception($"Unhandled initialization for type: {PInferPredicateGenerator.ShowType(t)}");
            }
        }

        private string CaseFor(PEvent e)
        {
            var result = $"case \"{e.Name}\":\n";
            var payloadType = e.PayloadType.Canonicalize();
            var javaType = Types.JavaTypeFor(payloadType);
            var hasPayload = javaType is not TypeManager.JType.JVoid;
            if (hasPayload)
            {
                var payloadName = $"{e.Name}Payload";
                result += $"var {payloadName} = {GenerateEventPayloadInit("e", "payload", payloadType)};\n";
                if (payloadType is NamedTupleType type)
                {
                    foreach (var field in type.Fields)
                    {
                        result += $"eventPayload.put(\"{field.Name}\", {GenerateEventPayloadInit(payloadName, field.Name, field.Type)});\n";
                    }
                }
                else
                {
                    result += $"eventPayload.put(\"payload\", {payloadName});\n";
                }
                result += GenerateEventInit(e.Name, "eventPayload") + "\n";
            }
            else
            {
                result += GenerateEventInit(e.Name, "new JSONObject()") + "\n";
            }
            result += "break;\n";
            return result;
        }

        protected override void GenerateCodeImpl()
        {
            var template = PreambleConstants.TraceReaderTemplate;
            string code = "";
            foreach (var e in Events)
            {
                code += CaseFor(e);
            }
            var generatedCode = template.Replace("%EVENT_INITIALIZATION%", code);
            WriteLine(generatedCode);
        }
    }
}