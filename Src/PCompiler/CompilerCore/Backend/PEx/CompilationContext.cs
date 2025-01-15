﻿using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PEx;

internal class CompilationContext : CompilationContextBase
{
    internal static readonly string ReturnValue = "retval";
    private readonly IDictionary<Continuation, int> continuationNames;

    internal Dictionary<Function, int> anonFuncIds;
    private int nextTempVarId;

    internal CompilationContext(ICompilerConfiguration job)
        : base(job)
    {
        if (!IsSafeJavaIdentifier(job.ProjectName))
            throw new TranslationException(
                $"Invalid project name '{ProjectName}'.  " +
                "When generating code for the 'PEx' target, the project name should " +
                "begin with an alphabetic character and contain only alphanumeric characters");

        MainClassName = ProjectName + "PModel";
        anonFuncIds = new Dictionary<Function, int>();
        continuationNames = new Dictionary<Continuation, int>();
    }

    internal IEnumerable<Continuation> Continuations => continuationNames.Keys;

    internal PLanguageType ReturnType { get; set; }

    internal string MainClassName { get; }

    internal string FileName => $"{MainClassName}.java";

    internal static string SchedulerVar => "PExGlobal.getScheduler()";

    internal static string CurrentMachine => "currentMachine";

    internal string GetContinuationName(Continuation c)
    {
        if (!continuationNames.ContainsKey(c))
            continuationNames.Add(c, continuationNames.Count);
        return "continuation_" + continuationNames[c];
    }

    internal string GetNameForDecl(IPDecl decl)
    {
        switch (decl)
        {
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

                return $"{func.Name}";
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
                return $"_{pEvent.Name}";
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

    internal static string GetVar(string rawName)
    {
        if (rawName.Contains("getLoopObject("))
            return rawName;
        else
            return $"var_{rawName}";
    }

    internal string FreshTempVar()
    {
        var id = nextTempVarId;
        nextTempVarId++;
        return $"temp_var_{id}";
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
            if (!(IsAsciiAlphabetic(ident[i]) || IsAsciiNumeric(ident[i])))
            {
                Console.WriteLine(ident[i]);
                return false;
            }

        return true;
    }
}