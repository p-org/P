//------------------------------------------------------------------------------
// <copyright file="PClassifier.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace VSEditorExtensions
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "PClassifier" classification type.
    /// </summary>
    internal class PClassifier : IClassifier
    {
        ITextDocument _doc;

        /// <summary>
        /// Keyword type.
        /// </summary>
        private readonly IClassificationType keywordType;

        /// <summary>
        /// Identifier type.
        /// </summary>
        private readonly IClassificationType identifierType;

        /// <summary>
        /// Comment type.
        /// </summary>
        private readonly IClassificationType commentType;

        /// <summary>
        /// String type.
        /// </summary>
        private readonly IClassificationType stringType;

        /// <summary>
        /// Initializes a new instance of the <see cref="PClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal PClassifier(ITextDocument doc, IClassificationTypeRegistryService registry)
        {
            this.keywordType = registry.GetClassificationType(Constants.PKeyword);
            this.identifierType = registry.GetClassificationType(Constants.PIdentifier);
            this.commentType = registry.GetClassificationType(Constants.PComment);
            this.stringType = registry.GetClassificationType(Constants.PString);

            // With the document here we can now kick off a parser on a background thread, get everything figured out
            // so that when GetClassificationSpans we can just lookup the compiled info to get the right token info for
            // the given span.  Where it gets interesting is in optimizing how much we re-compile when the user edits text
            // we want the minimal incremental scan needed to compute new classification info.
            _doc = doc;
        }

        #region IClassifier

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
        /// </summary>
        /// <remarks>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </remarks>
        /// <param name="span">The span currently being classified.  Usually the span is a "line" of text</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            // TODO: rip all this out and replace it with the real Scanner generated from lexer.lex in the P compiler.
            ITextSnapshot snapshot = span.Snapshot;
            var result = new List<ClassificationSpan>();
            int start = 0;
            string line = span.GetText();
            for (int i = 0, n = line.Length; i < n; i++)
            {
                char ch = line[i];
                switch (ch)
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        // skip whitespace
                        break;
                    case '"':
                        // string literals.
                        start = i;
                        for (i = i + 1; i < n; i++)
                        {
                            ch = line[i];
                            if (ch == '"' || ch == '\r' || ch == '\n') // cannot span a newline.
                            {
                                result.Add(new ClassificationSpan(new SnapshotSpan(snapshot, new Span(span.Start + start, i - start + 1)), this.stringType));
                                break;
                            }
                        }
                        break;
                    case '.':
                    case ':':
                    case ',':
                    case ';':
                    case '=': // ==
                    case '+': // +=
                    case '-': // -=
                    case '!': // !=
                    case '<': // <=
                    case '>': // >=
                    case '*':
                    case '&': // &&
                    case '|': // ||
                    case '$': // $$
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                        // operators
                        break;
                    case '/': // divided by or a line comment
                        if (i + 1 < n && line[i + 1] == '/')
                        {
                            start = i;
                            // we found a line comment, so skip to the end of the line.
                            for (i = i + 1; i < n; i++)
                            {
                                ch = line[i];
                                if (ch == '\r' || ch == '\n')
                                {
                                    break;
                                }
                            }
                            result.Add(new ClassificationSpan(new SnapshotSpan(snapshot, new Span(span.Start + start, i - start)), this.commentType));
                        }
                        break;
                    default:
                        // must be an identifier or a constant integer.
                        bool startsWithDigit = Char.IsDigit(ch);
                        start = i;
                        for (i = i + 1; i < n; i++)
                        {
                            ch = line[i];
                            if (!Char.IsLetterOrDigit(ch))
                            {
                                break;
                            }
                        }
                        if (i > start)
                        {
                            string token = line.Substring(start, i - start);
                            if (keywords.Contains(token))
                            {
                                result.Add(new ClassificationSpan(new SnapshotSpan(snapshot, new Span(span.Start + start, i - start)), this.keywordType));
                            }
                            else
                            {
                                result.Add(new ClassificationSpan(new SnapshotSpan(snapshot, new Span(span.Start + start, i - start)), this.identifierType));
                            }
                        }
                        break;
                }
            }

            return result;
        }


        #endregion

        HashSet<string> keywords = new HashSet<string>(new string[]
        {
             "while",
             "if",
             "else",
             "return",
             "new",
             "this",
             "null",
             "pop",
             "true",
             "false",
             "sizeof",
             "keys",
             "values",

             "assert",
             "print",
             "send",
             "monitor",
             "spec",
             "monitors",
             "raise",
             "halt",

             "int",
             "bool",
             "any",
             "seq",
             "map",

             "type",
             "include",
             "main",
             "event",
             "machine",
             "assume",
             "default",
             "fresh",

             "var",
             "start",
             "hot",
             "cold",
             "model",
             "fun",
             "action",
             "state",
             "group",
             "static",

             "entry",
             "exit",
             "defer",
             "ignore",
             "goto",
             "push",
             "on",
             "do",
             "with",

             "receive",
             "case",

             "in",
             "as",
        });
    }
}
