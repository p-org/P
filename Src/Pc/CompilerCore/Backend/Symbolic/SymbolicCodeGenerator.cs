using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Symbolic
{
    class SymbolicCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            var javaSource = GenerateSource(context, globalScope);
            return new List<CompiledFile> { javaSource };
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            foreach (var decl in globalScope.AllDecls)
                WriteDecl(context, source.Stream, decl);

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteDecl(CompilationContext context, StringWriter output, IPDecl decl)
        {
            switch (decl)
            {
                case Function function:
                    if (function.IsForeign)
                        throw new NotImplementedException("Foreign functions are not yet supported");
                    
                    WriteFunction(context, output, function);
                    break;
                default:
                    context.WriteLine(output, $"// Skipping {decl.GetType().Name} '{decl.Name}'\n");
                    break;
            }
        }

        private void WriteFunction(CompilationContext context, StringWriter output, Function function)
        {
            if (function.Owner != null)
                throw new NotImplementedException("Non-static functions are not yet supported");

            if (function.CanReceive == true)
                throw new NotImplementedException("Async functions are not supported");

            var rootPCScope = context.FreshPathConstraintScope();

            var returnType = GetSymbolicType(function.Signature.ReturnType);
            var functionName = context.GetNameForDecl(function);

            context.WriteLine(output, $"static {returnType} ");
            context.Write(output, functionName);

            context.WriteLine(output, $"(");
            context.WriteLine(output, $"    psymbolic.Bdd {rootPCScope.PathConstraintVar},");

            for (int i = 0; i < function.Signature.Parameters.Count; i++)
            {
                var param = function.Signature.Parameters[i];
                context.Write(output, $"    {GetSymbolicType(param.Type, true)} {rootPCScope.GetVar(param.Name)}");
                if (i + 1 != function.Signature.Parameters.Count)
                    context.WriteLine(output, ",");
                else
                    context.WriteLine(output);
            }

            context.Write(output, ") ");

            context.WriteLine(output, "{");
            WriteFunctionBody(context, output, rootPCScope, function);
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteFunctionBody(CompilationContext context, StringWriter output, PathConstraintScope rootPCScope, Function function)
        {
            foreach (var local in function.LocalVariables)
            {
                context.WriteLine(output, $"{GetSymbolicType(local.Type)} {rootPCScope.GetVar(local.Name)} =");
                context.WriteLine(output, $"    {GetDefaultValue(context, rootPCScope, local.Type)};");
                context.WriteLine(output);
            }

            context.WriteLine(output, "throw new Exception();");
        }

        private string GetConcreteBoxedType(PLanguageType type)
        {
            switch (type)
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "Boolean";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "Integer";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "Float";
                default:
                    throw new NotImplementedException($"Concrete type '{type.OriginalRepresentation}' is not supported");
            }
        }

        private string GetSymbolicType(PLanguageType type, bool isVar = false)
        {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "psymbolic.PrimitiveValueSummary<psymbolic.Bdd, Boolean>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "psymbolic.PrimitiveValueSummary<psymbolic.Bdd, Integer>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "psymbolic.PrimitiveValueSummary<psymbolic.Bdd, Float>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    if (isVar)
                        throw new NotImplementedException("Variables of type 'null' not yet supported");
                    else
                        return "void";
                case SequenceType sequenceType:
                    return $"psymbolic.ListValueSummary<psymbolic.Bdd, {GetSymbolicType(sequenceType.ElementType, true)}>";
                case MapType mapType:
                    return $"psymbolic.MapValueSummary<" +
                        $"psymbolic.Bdd, " +
                        $"{GetConcreteBoxedType(mapType.KeyType)}, " +
                        $"{GetSymbolicType(mapType.ValueType, true)}>";
                default:
                    throw new NotImplementedException($"Symbolic type '{type.OriginalRepresentation}' not supported");
            }

            throw new NotImplementedException();
        }

        private string GetValueSummaryOpsType(PLanguageType type)
        {
            switch (type)
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "psymbolic.PrimitiveValueSummary.Ops<psymbolic.Bdd, Boolean>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "psymbolic.PrimitiveValueSummary.Ops<psymbolic.Bdd, Integer>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "psymbolic.PrimitiveValueSummary.Ops<psymbolic.Bdd, Float>";
                case SequenceType sequenceType:
                    return $"psymbolic.ListValueSummary.Ops<psymbolic.Bdd, {GetSymbolicType(sequenceType.ElementType, true)}>";
                case MapType mapType:
                    return $"psymbolic.MapValueSummary.Ops<" +
                        $"psymbolic.Bdd, " +
                        $"{GetConcreteBoxedType(mapType.KeyType)}, " +
                        $"{GetSymbolicType(mapType.ValueType, true)}>";
                default:
                    throw new NotImplementedException($"Symbolic type '{type.OriginalRepresentation}' ops type not supported");
            }
        }

        private ValueSummaryOps GetValueSummaryOps(CompilationContext context, PLanguageType type)
        {
            var opsType = GetValueSummaryOpsType(type);
            string defBody;
            switch (type)
            {
                case PrimitiveType primitiveType when
                    primitiveType.IsSameTypeAs(PrimitiveType.Bool) ||
                    primitiveType.IsSameTypeAs(PrimitiveType.Int) ||
                    primitiveType.IsSameTypeAs(PrimitiveType.Float):

                    defBody = $"new {opsType}({CompilationContext.BddLib})";
                    break;

                case SequenceType sequenceType:
                    var elemOps = GetValueSummaryOps(context, sequenceType.ElementType);
                    defBody = $"new {opsType}({CompilationContext.BddLib}, {elemOps.GetName()})";
                    break;
                case MapType mapType:
                    var valOps = GetValueSummaryOps(context, mapType.ValueType);
                    defBody = $"new {opsType}({CompilationContext.BddLib}, {valOps.GetName()})";
                    break;
                default:
                    throw new NotImplementedException($"Symbolic type '{type.OriginalRepresentation}' ops not supported");
            }

            return context.ValueSummaryOpsForDef(new ValueSummaryOpsDef(opsType, defBody));
        }

        private string GetDefaultValue(CompilationContext context, PathConstraintScope pcScope, PLanguageType type)
        {
            string unguarded;
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    unguarded = $"new {GetSymbolicType(type)}({CompilationContext.BddLib}, false)";
                    break;
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    unguarded = $"new {GetSymbolicType(type)}({CompilationContext.BddLib}, 0)";
                    break;
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    unguarded = $"new {GetSymbolicType(type)}({CompilationContext.BddLib}, 0.0f)";
                    break;
                case SequenceType sequenceType:
                    unguarded = $"new {GetSymbolicType(type)}({CompilationContext.BddLib})";
                    break;
                case MapType mapType:
                    unguarded = $"new {GetSymbolicType(type)}({CompilationContext.BddLib})";
                    break;
                default:
                    throw new NotImplementedException($"Default value for symbolic type '{type.OriginalRepresentation}' not supported");
            }

            var guarded = $"{GetValueSummaryOps(context, type).GetName()}.guard({unguarded}, {pcScope.PathConstraintVar})";
            return guarded;
        }

        private void WriteSourcePrologue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, $"public class {context.MainClassName} {{");
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter output)
        {
            for (int i = 0; i < context.PendingValueSummaryOpsDefs.Count; i++)
            {
                var def = context.PendingValueSummaryOpsDefs[i];
                var name = new ValueSummaryOps(i).GetName();
                context.WriteLine(output, $"private static final {def.opsType} {name} =");
                context.WriteLine(output, $"    {def.opsDef};");
                context.WriteLine(output);
            }

            context.WriteLine(output, "}");
        }
    }
}
