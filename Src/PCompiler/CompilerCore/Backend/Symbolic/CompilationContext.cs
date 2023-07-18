using System;
using System.Collections.Generic;
using System.IO;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Symbolic
{
    internal class CompilationContext : CompilationContextBase
    {
        int nextPathConstraintScopeId;
        int nextLoopId;
        int nextBranchId;
        int nextTempVarId;
        IDictionary<Continuation, int> continuationNames;

        internal string GetContinuationName(Continuation c)
        {
            if (!continuationNames.ContainsKey(c))
                continuationNames.Add(c, continuationNames.Count);
            return "continuation_" + continuationNames[c];
        }

        internal IEnumerable<Continuation> Continuations => continuationNames.Keys;

        internal PLanguageType ReturnType { get; set; }

        internal Dictionary<Function, int> anonFuncIds;

        internal CompilationContext(ICompilerConfiguration job)
            : base(job)
        {
            if (!IsSafeJavaIdentifier(job.ProjectName))
                throw new TranslationException(
                    $"Invalid project name '{ProjectName}'.  " +
                    "When generating code for the 'Symbolic' target, the project name should " +
                    "begin with an alphabetic character and contain only alphanumeric characters");

            MainClassName = ProjectName+"Program";
            anonFuncIds = new Dictionary<Function, int>();
            continuationNames = new Dictionary<Continuation, int>();
        }

        internal string MainClassName { get; }

        internal string FileName => $"{MainClassName}.java";

        internal static readonly string ReturnValue = "retval";

        internal string GetNameForDecl(IPDecl decl)
        {
            switch (decl) {
                case Function func:
                    if (string.IsNullOrEmpty(func.Name))
                    {
                        if (!anonFuncIds.ContainsKey(func))
                        {
                            var newId = anonFuncIds.Count;
                            anonFuncIds.Add(func, newId);
                        }
                        return $"anonfun_{anonFuncIds[func]}";
                    }
                    else
                    {
                        return $"{func.Name}";
                    }
                case Machine machine:
                    return $"{machine.Name}";
                case Interface @interface:
                    // TODO: Is it safe to take an interface's name and treat it as if it were a machine's name?
                    return $"{@interface.Name}";
                case State state:
                    return $"{state.Name}";
                case PEvent pEvent:
                    if (!pEvent.IsBuiltIn)
                        return $"{pEvent.Name}";
                    else return $"_{pEvent.Name}";
                case SafetyTest safety:
                    return $"{safety.Name}";
                default:
                    throw new NotImplementedException($"decl type {decl.GetType().Name} not supported");
            }
        }

        internal object GetMachineName(Machine machine)
        {
            return $"{machine.Name}";
        }

        internal static string SchedulerVar => "PSymGlobal.getScheduler()";

        internal static string EffectCollectionVar => "effects";

        internal static string GetVar(string rawName)
        {
            return $"var_{rawName}";
        }

        internal PathConstraintScope FreshPathConstraintScope()
        {
            return new PathConstraintScope(nextPathConstraintScopeId++);
        }

        internal LoopScope FreshLoopScope()
        {
            return new LoopScope(nextLoopId++);
        }

        internal BranchScope FreshBranchScope()
        {
            return new BranchScope(nextBranchId++);
        }

        internal string FreshTempVar()
        {
            var id = nextTempVarId;
            nextTempVarId++;
            return $"temp_var_{id}";
        }

        internal void WriteCommaSeparated<T>(TextWriter output, IEnumerable<T> items, Action<T> writeItem)
        {
            var needComma = false;
            foreach (var item in items)
            {
                if (needComma)
                {
                    Write(output, ", ");
                }
                writeItem(item);
                needComma = true;
            }
        }
        
        private static bool IsAsciiAlphabetic(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        private static bool IsAsciiNumeric(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool IsSafeJavaIdentifier(string ident)
        {
            if (ident.Length == 0)
                return false;

            if (!IsAsciiAlphabetic(ident[0]))
            {
                Console.WriteLine(ident[0]);
                return false;
            }

            // We deliberately exclude underscores because we use underscores for internal namespacing

            for (var i = 1; i < ident.Length; i++)
            {
                if (!(IsAsciiAlphabetic(ident[i]) || IsAsciiNumeric(ident[i])))
                {
                    Console.WriteLine(ident[i]);
                    return false;
                }
            }

            return true;
        }
    }

    internal struct PathConstraintScope
    {
        internal readonly int id;

        internal PathConstraintScope(int id)
        {
            this.id = id;
        }

        internal string PathConstraintVar => $"pc_{id}";
    }

    internal struct LoopScope
    {
        internal readonly int id;

        internal LoopScope(int id)
        {
            this.id = id;
        }

        internal string LoopExitsList => $"loop_exits_{id}";

        internal string LoopEarlyReturnFlag => $"loop_early_ret_{id}";
    }

    internal struct BranchScope
    {
        internal readonly int id;

        internal BranchScope(int id)
        {
            this.id = id;
        }

        internal string JumpedOutFlag => $"jumpedOut_{id}";
    }

}
