using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Symbolic
{
    internal class CompilationContext : CompilationContextBase
    {
        int nextPathConstraintScopeId;
        int nextLoopId;
        int nextBranchId;
        int nextTempVarId;

        internal readonly List<ValueSummaryOpsDef> PendingValueSummaryOpsDefs;
        private readonly Dictionary<ValueSummaryOpsDef, ValueSummaryOps> CachedValueSummaryOpsDefs;

        internal CompilationContext(ICompilationJob job)
            : base(job)
        {
            if (!IsSafeJavaIdentifier(job.ProjectName))
                throw new TranslationException(
                    $"Invalid project name '{ProjectName}'.  " +
                    "When generating code for the 'Symbolic' target, the project name should " +
                    "begin with an alphabetic character and contain only alphanumeric characters");

            MainClassName = ProjectName;


            PendingValueSummaryOpsDefs = new List<ValueSummaryOpsDef>();
            CachedValueSummaryOpsDefs = new Dictionary<ValueSummaryOpsDef, ValueSummaryOps>();
        }

        internal string MainClassName { get; }

        internal string FileName => $"{MainClassName}.java";

        internal static readonly string BddLib = "Bdd.getLib()";

        internal static readonly string ReturnValue = "retval";

        internal string GetNameForDecl(IPDecl decl)
        {
            // TODO: We will probably need to add more namespacing logic as we support more declaration types
            return $"decl_{decl.Name}";
        }

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

        internal ValueSummaryOps ValueSummaryOpsForDef(ValueSummaryOpsDef def)
        {
            if (CachedValueSummaryOpsDefs.ContainsKey(def))
            {
                return CachedValueSummaryOpsDefs[def];
            }
            else
            {
                ValueSummaryOps result = new ValueSummaryOps(CachedValueSummaryOpsDefs.Count);
                PendingValueSummaryOpsDefs.Add(def);
                CachedValueSummaryOpsDefs[def] = result;
                return result;
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
                System.Console.WriteLine(ident[0]);
                return false;
            }

            // We deliberately exclude underscores because we use underscores for internal namespacing

            for (var i = 1; i < ident.Length; i++)
            {
                if (!(IsAsciiAlphabetic(ident[i]) || IsAsciiNumeric(ident[i])))
                {
                    System.Console.WriteLine(ident[i]);
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

    internal struct ValueSummaryOpsDef
    {
        internal readonly string opsType;
        internal readonly string opsDef;

        internal ValueSummaryOpsDef(string opsType, string opsDef)
        {
            this.opsType = opsType;
            this.opsDef = opsDef;
        }

        public override bool Equals(object obj)
        {
            return obj is ValueSummaryOpsDef def &&
                   opsType == def.opsType &&
                   opsDef == def.opsDef;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(opsType, opsDef);
        }
    }

    internal struct ValueSummaryOps
    {
        internal readonly int id;

        internal ValueSummaryOps(int id)
        {
            this.id = id;
        }

        internal string GetName()
        {
            return $"ops_{id}";
        }
    }
}