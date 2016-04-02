//------------------------------------------------------------------------------
// <copyright file="PClassifierFormat.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace VSEditorExtensions
{
    /// <summary>
    /// Defines an editor format for the P keywords
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.PKeyword)]
    [Name(Constants.PKeyword)]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class PKeywordFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PKeywordFormat"/> class.
        /// </summary>
        [ImportingConstructor]
        public PKeywordFormat(ClassificationColorManager colorManager)
        {
            this.DisplayName = Constants.PKeyword; // Human readable version of the name
            var colors = colorManager.GetDefaultColors(Constants.PKeyword);
            this.ForegroundColor = colors.Foreground;
            this.BackgroundColor = colors.Background;
            this.TextDecorations = colors.Decorations;
        }
    }

    /// <summary>
    /// Defines an editor format for the P comments
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.PComment)]
    [Name(Constants.PComment)]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class PCommentFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PCommentFormat"/> class.
        /// </summary>
        [ImportingConstructor]
        public PCommentFormat(ClassificationColorManager colorManager)
        {
            this.DisplayName = Constants.PComment; // Human readable version of the name
            var colors = colorManager.GetDefaultColors(Constants.PComment);
            this.ForegroundColor = colors.Foreground;
            this.BackgroundColor = colors.Background;
            this.TextDecorations = colors.Decorations;
        }
    }

    /// <summary>
    /// Defines an editor format for the P identifiers
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.PIdentifier)]
    [Name(Constants.PIdentifier)]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class PIdentifierFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PIdentifierFormat"/> class.
        /// </summary>
        [ImportingConstructor]
        public PIdentifierFormat(ClassificationColorManager colorManager)
        {
            this.DisplayName = Constants.PIdentifier; // Human readable version of the name
            var colors = colorManager.GetDefaultColors(Constants.PIdentifier);
            this.ForegroundColor = colors.Foreground;
            this.BackgroundColor = colors.Background;
            this.TextDecorations = colors.Decorations;
        }
    }


    /// <summary>
    /// Defines an editor format for the P keywords
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.PString)]
    [Name(Constants.PString)]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class PStringFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PKeywordFormat"/> class.
        /// </summary>
        [ImportingConstructor]
        public PStringFormat(ClassificationColorManager colorManager)
        {
            this.DisplayName = Constants.PString; // Human readable version of the name
            var colors = colorManager.GetDefaultColors(Constants.PString);
            this.ForegroundColor = colors.Foreground;
            this.BackgroundColor = colors.Background;
            this.TextDecorations = colors.Decorations;
        }
    }
}
