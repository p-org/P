using System.Globalization;

namespace Plang.Compiler.TypeChecker
{
    /// <summary>
    /// Culture-invariant parsing of numeric literals taken verbatim from P source
    /// lexemes. Centralizes the parse so that:
    ///   * overflow / malformed input is reported as a located P diagnostic
    ///     (callers surface <see cref="ITranslationErrorHandler.ValueOutOfRange"/>)
    ///     instead of the raw <see cref="System.OverflowException"/> /
    ///     <see cref="System.FormatException"/> escaping the diagnostic machinery and
    ///     aborting the whole compilation with a stack trace; and
    ///   * float parsing never depends on the host machine's current culture (a
    ///     comma-decimal locale such as de-DE / fr-FR would otherwise misparse or
    ///     reject a valid P float literal).
    /// </summary>
    internal static class LiteralParsingUtils
    {
        /// <summary>
        /// Parses an integer literal lexeme (unsigned digits; the grammar handles the
        /// sign separately). Returns <c>false</c> on overflow or malformed input rather
        /// than throwing, so callers can emit a located diagnostic and recover.
        /// </summary>
        public static bool TryParseIntLiteral(string text, out int value)
        {
            // NumberStyles.None matches the grammar's IntLiteral ([0-9]+): digits only,
            // no leading sign or whitespace (the sign is handled by the grammar).
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Parses a decimal float literal assembled from grammar tokens (always
        /// '.'-separated, e.g. "3.14"). Uses invariant culture so the host locale
        /// cannot affect the result. Returns <c>false</c> on malformed input.
        /// </summary>
        public static bool TryParseFloatLiteral(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
