using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSEditorExtensions
{
    static class ClassificationTypes
    {
        public const string PKeyword = Constants.PKeyword;
        public const string PIdentifier = Constants.PIdentifier;
        public const string PComment = Constants.PComment;
        public const string PString = Constants.PString;

        [Export]
        [Name(PKeyword)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition PKeywordClassificationType = null;

        [Export]
        [Name(PIdentifier)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition PIdentifierClassificationType = null;

        [Export]
        [Name(PComment)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition PCommentClassificationType = null;

        [Export]
        [Name(PString)]
        [BaseDefinition("literal")]
        internal static ClassificationTypeDefinition PStringClassificationType = null;

    }
}
