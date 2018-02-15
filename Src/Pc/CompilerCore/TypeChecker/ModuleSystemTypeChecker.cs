using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker
{
    public class ModuleSystemTypeChecker
    {
        private readonly ITranslationErrorHandler handler;

        private ModuleSystemTypeChecker(ITranslationErrorHandler handler)
        {
            this.handler = handler;
        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, IPModuleExpr moduleExpr)
        {
            moduleExpr.CheckAndPopulateAttributes(handler);
        }
    }
}