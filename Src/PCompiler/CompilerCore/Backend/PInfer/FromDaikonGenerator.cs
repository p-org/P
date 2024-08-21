using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    internal class FromDaikonGenerator : MachineGenerator
    {
        private readonly List<PEventVariable> QuantifiedEvents;
        public FromDaikonGenerator(ICompilerConfiguration job, List<PEventVariable> quantifiedEvents) : base(job, PreambleConstants.FromDaikonFileName)
        {
            QuantifiedEvents = quantifiedEvents;
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine(PreambleConstants.FromDaikonProg
                            .Replace("%QUANTIFIERS%",
                                    string.Join(", ", QuantifiedEvents.Select(x => $"\"{x.EventName}\"")))
                            .Replace("%QUANTIFIED_VARS%",
                                    string.Join(", ", QuantifiedEvents.Select(x => $"\"{x.Name}\""))));
                            
        }
    }
}