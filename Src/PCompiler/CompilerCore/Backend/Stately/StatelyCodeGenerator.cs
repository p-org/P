using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;

namespace Plang.Compiler.Backend.Stately {
    public class StatelyCodeGenerator : ICodeGenerator
    {
        public bool HasCompilationStage => false;
        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            var statelySource = GenerateSource(context, globalScope);
            return new List<CompiledFile> { statelySource };
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);
            WriteSourcePrologue(context, source.Stream);
            // write the top level declarations
            foreach (var decl in globalScope.AllDecls)
            {
                WriteDecl(context, source.Stream, decl);
            }

            return source;
        }

        private void WriteDecl(CompilationContext context, StringWriter output, IPDecl decl)
        {
            //string declName;
            switch (decl)
            {
                case Machine machine:
                    if (!machine.IsSpec)
                    {
                        WriteMachine(context, output, machine);
                    }

                    break;
            }
        }

        private void WriteMachine(CompilationContext context, StringWriter output, Machine machine)
        {

            context.WriteLine(output, $"const {machine.Name} = createMachine<Context>({{");
            context.WriteLine(output, $"id: \"{machine.Name}\",");
            
            //Code start state of machine.
            context.WriteLine(output,$"initial: \"{machine.StartState.Name}\", ");
            
            //Code up the states in each machine.
            context.WriteLine(output, "states: {");
            foreach (State state in machine.States)
            {
                
                context.WriteLine(output,$"{state.Name}: {{");
                WriteState(context, output, state);
                context.WriteLine(output, state.Equals(machine.States.Last()) ? "}" : "},");
            }
            context.WriteLine(output, "}");
            context.WriteLine(output, "});");
        }

        private void WriteState(CompilationContext context, StringWriter output, State state)
        {
            //Entry function exists!
            if (state.Entry != null) {
                foreach (var s in state.Entry.Body.Statements) {
                    if (s.GetType() == typeof(GotoStmt)) {
                        var x = (GotoStmt)s;
                        context.WriteLine(output, "always: [");
                        context.WriteLine(output, $"{{target: '{x.State.Name}'}}");
                        context.WriteLine(output, "]");
                    }
                }
            }
            var gotoStmts = new List<(String, String)>();
            foreach (var pair in state.AllEventHandlers)
            {
                var handledEvent = pair.Key;
                
                //context.WriteLine(output, $"{pair.Value}");
                switch (pair.Value)
                {
                    case EventGotoState goAct:
                        gotoStmts.Add((goAct.Trigger.Name, goAct.Target.Name));
                        break;
                    case EventDoAction doAct:
                        foreach (var stmt in doAct.Target.Body.Statements)
                        {
                            if (stmt.GetType() == typeof(GotoStmt))
                            {
                                var gotoS = (GotoStmt)stmt;
                                gotoStmts.Add((doAct.Trigger.Name, gotoS.State.Name));
                            }
                        }
                        //TODO: If the statement is a Function Call, recursively add any goto statements inside too.
                        break;
                    
                }
            }
            if (gotoStmts.Any())
            {
                context.WriteLine(output, "on: {");
                foreach (var stmt in gotoStmts)
                {
                    context.WriteLine(output, $"{stmt.Item1} : {{ target: \"{stmt.Item2}\"}},");
                }
                context.WriteLine(output, "}");
            }

        }
        private void WriteSourcePrologue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "import { createMachine, assign } from 'xstate';");
            context.WriteLine(output, "interface Context {retries: number;}");
        }
    }
}