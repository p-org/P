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

        private static string GenerateEventInit(string eventName, TypeManager.JType payloadType, string payloadGet, bool hasPayload = false)
        {
            if (!hasPayload)
            {
                return $"events.add(new {Constants.EventNamespaceName}.{eventName}(i, sender, target);";
            }
            return $"events.add(new {Constants.EventNamespaceName}.{eventName}(i, sender, target, {payloadType.GenerateCastFromObject(payloadGet)}));";
        }

        private string CaseFor(PEvent e)
        {
            var result = $"case \"{e.Name}\":\n";
            var payloadType = e.PayloadType.Canonicalize();
            var javaType = Types.JavaTypeFor(payloadType);
            var hasPayload = javaType is not TypeManager.JType.JVoid;
            if (hasPayload)
            {
                result += GenerateEventInit(e.Name, javaType, GenerateJSONObjectGet("details", "payload", payloadType), true) + "\n";
            }
            else
            {
                result += GenerateEventInit(e.Name, null, null, false) + "\n";
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