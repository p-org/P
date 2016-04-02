//------------------------------------------------------------------------------
// <copyright file="PClassifierProvider.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.PlatformUI;

namespace VSEditorExtensions
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType("P")] // This classifier applies to 'P' language text files.
    internal class PClassifierProvider : IClassifierProvider
    {
        private readonly ClassificationColorManager _classificationColorManager;
        private readonly IClassificationTypeRegistryService _classificationRegistry;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly IServiceProvider _serviceProvider;


        [ImportingConstructor]
        public PClassifierProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ClassificationColorManager classificationColorManager,
            IClassificationTypeRegistryService classificationRegistry,
            ITextDocumentFactoryService textDocumentFactoryService)
        {
            _serviceProvider = serviceProvider;
            _classificationColorManager = classificationColorManager;
            _classificationRegistry = classificationRegistry;
            _textDocumentFactoryService = textDocumentFactoryService;

            // Receive notification for Visual Studio theme change 
            VSColorTheme.ThemeChanged += UpdateTheme;
        }

        private void UpdateTheme(EventArgs e)
        {
            _classificationColorManager.UpdateColors();
        }


        // Disable "Field is never assigned to..." compiler's warning. Justification: the field is assigned by MEF.
#pragma warning disable 649

        /// <summary>
        /// Classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import]
        internal IEditorFormatMapService FormatMapService { get; set; }

        [Export]
        [Name("P")]
        [BaseDefinition("code")]
        [BaseDefinition("projection")]
        internal static ContentTypeDefinition PContentTypeDefinition;

        [Export]
        [FileExtension(".p")]
        [ContentType("P")]
        internal static FileExtensionToContentTypeDefinition TestFileExtensionDefinition;

#pragma warning restore 649

        #region IClassifierProvider

        /// <summary>
        /// Gets a classifier for the given text buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
        /// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            var map = FormatMapService.GetEditorFormatMap("text");

            ITextDocument doc;
            if (_textDocumentFactoryService.TryGetTextDocument(buffer, out doc))
            {
                return buffer.Properties.GetOrCreateSingletonProperty<PClassifier>(creator: () => new PClassifier(doc, this.classificationRegistry));
            }
            return null;
        }

        #endregion
    }
}
