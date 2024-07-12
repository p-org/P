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

        private static string GenerateEventInit(string eventName, string payloadRefType, bool hasPayload = false)
        {
            if (!hasPayload)
            {
                return $"events.add(new {Constants.EventNamespaceName}.{eventName}(i, sender, target);";
            }
            return $"events.add(new {Constants.EventNamespaceName}.{eventName}(i, sender, target, new {payloadRefType}(eventPayload)));";
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
                result += $"var {payloadName} = {GenerateJSONObjectGet("details", "payload", payloadType)};\n";
                if (payloadType is NamedTupleType type)
                {
                    foreach (var field in type.Fields)
                    {
                        result += $"eventPayload.put(\"{field.Name}\", {GenerateJSONObjectGet(payloadName, field.Name, field.Type)});\n";
                    }
                }
                else
                {
                    result += $"eventPayload.put(\"payload\", {payloadName});\n";
                }
                result += GenerateEventInit(e.Name, javaType.ReferenceTypeName, true) + "\n";
            }
            else
            {
                result += GenerateEventInit(e.Name, null, false) + "\n";
            }
            result += "break;\n";
            return result;
        }

        protected override void GenerateCodeImpl()
        {
            var template = PreambleConstants.TraceReaderTemplate;
            string code = "";
            foreach (var e in Events.DistinctBy(x => x.Name))
            {
                code += CaseFor(e);
            }
            var generatedCode = template.Replace("%EVENT_INITIALIZATION%", code);
            WriteLine(generatedCode);
        }
    }
}