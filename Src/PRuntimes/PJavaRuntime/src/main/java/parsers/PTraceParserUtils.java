package parsers;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Pattern;
import java.util.stream.Stream;

public class PTraceParserUtils {
    private static final String PREFIX = "<SendLog>";

    private static final String evtRegex = "sent event '(\\w+) with payload \\((.+)\\)' to";
    public static final Pattern evtPattern = Pattern.compile(evtRegex);

    private static boolean sendLogFilter(String msg) {
        return msg.startsWith(PREFIX);
    }

    /**
     * Produces the given stream of log lines, removing the ones that do
     * not originate from the SendLog source.
     * @param s
     * @return
     */
    public static Stream<String> FilterSendLogs(Stream<String> s){
        return s.filter(PTraceParserUtils::sendLogFilter);
    }


    /**
     * These routines help convert serialised data in the form emitted by the C# runtime,
     * which all inherit from `PLang.CSharpRuntime.PrtType`.  Should any PrtType's `.toString()`
     * method be changed, these conversion functions may break.
     */
    public static class Conversions {
        private static final String tupleDelimiter = ",";       /* Note the lack of space, vis a vis named tuples */
        private static final String namedTupleDelimiter = ", ";

        /**
         * Given a serialized named tuple of the form `<k1:v1, k2:v2, ... kn:vn, >`, return an array
         * of Strings containing all the key-value pairs.  (Note the trailing ", " after the final
         * key-value pair[1]; the suprious empty final element is truncated from the final result array.)
         *
         * [1]: https://github.com/p-org/P/blob/master/Src/PRuntimes/PCSharpRuntime/Values/PrtTuple.cs#L191-L191
         * @param token The string representation of a k-ary named tuple
         * @return An array of `k` elements, containing each of the colon-delimited key-value pairs.
         */
        public static List<String> namedTupleToKVPairs(String token) {
            if (token.charAt(0) != '<' || !token.endsWith(", >")) {
                throw new RuntimeException(String.format("Token \"%s\" does not appear to be a named tuple?", token));
            }
            token = token.substring(1, token.length() - 3); // Eat the open brace and trailing comma/space/closing brace.
            return splitCommasOutsideStrings(token, ", ");
        }

        /**
         * Given a serialised tuple of the form `<v1,v2,v3...vn,>`, return an array of Strings
         * containing all the values.  (Notice the trailing "," after the final value[1]; this spurious
         * element is truncated from the final result.)
         *
         * [1]: https://github.com/p-org/P/blob/master/Src/PRuntimes/PCSharpRuntime/Values/PrtTuple.cs#L95-L95
         * @param token
         * @return
         */
        public static List<String> tupleToValues(String token) {
            if (token.charAt(0) != '<' || !token.endsWith(",>")) {
                throw new RuntimeException(String.format("Token \"%s\" does not appear to be a tuple?", token));
            }
            token = token.substring(1, token.length() - 2); // Eat the open brace and trailing comma/closing brace
            return splitCommasOutsideStrings(token, ",");
        }

        /**
         * Given a key-value pair of the form "key:value", where value
         * is an Integer, extract the int.
         * @param kv
         * @return
         */
        public static int kvPairToInt(String kv) {
            return Integer.valueOf(kv.split(":")[1]);
        }

        /**
         * Given a key-value pair of the form "key:value", where value
         * is a long, extract the long.
         * @param kv
         * @return
         */
        public static long kvPairToLong(String kv) {
            return Long.valueOf(kv.split(":")[1]);
        }

        /**
         * Given a key-value pair of the form "key:MachineName(value)", where value
         * is a long and MachineName is an arbitrary identifier, extract the long.
         * @param kv
         * @return
         */
        public static long kvPairToMachineId(String kv) {
            int openParen = kv.indexOf("(");
            int closeParen = kv.indexOf(")");
            return Long.valueOf(kv.substring(openParen + 1, closeParen));
        }

        /**
         * Given a key-value pair of the form "key:value", where value
         * is a float, extract the long.
         * @param kv
         * @return
         */
        public static float kvPairToFloat(String kv) {
            return Float.valueOf(kv.split(":")[1]);
        }

        /**
         * Given a key-value pair of the form "key:value", where value
         * is an enumerated value, extract it.
         */
        public static int kvPairToEnumVal(String kv) {
            // Internally, the integer representation will already have been written out,
            // so this is equivalent to parsing an int out.
            return kvPairToInt(kv);
        }

        /**
         * Splits `s` according to its occurrences of `delimiter`, such that the delimiters
         * do not fall inside a quoted string.  This method assumes that there are no dangling
         * quotes (i.e. there are an even number of occurrences of /[^\\]"/ in s.
         *
         * @param s
         * @return
         */
        private static List<String> splitCommasOutsideStrings(String str, String delimiter)
        {
            ArrayList<String> ret = new ArrayList<>();
            boolean inQuotedString = (str.charAt(0) == '"');

            StringBuilder currentStr = new StringBuilder(str.substring(0, 1));

            // Note the slightly-pedestrian way of writing this.  The alternative was to write
            // a gnarly regex using negative lookahead assertions (which would be O(n^2) anyway)
            // or taking a major dependency on something like Apache Commons.
            for (int i = 1; i < str.length(); i++) {
                if (!inQuotedString && str.substring(i).startsWith(delimiter)) {
                    ret.add(currentStr.toString());
                    currentStr = new StringBuilder();
                    i += delimiter.length() - 1;
                    continue;
                }

                char prev = str.charAt(i - 1);
                char curr = str.charAt(i);

                if (curr == '"' && prev != '\\') {
                    inQuotedString = !inQuotedString;
                }
                currentStr.append(curr);
            }
            if (currentStr.length() > 0) {
                ret.add(currentStr.toString());
            }
            return ret;
        }

        /**
         * Given a key-value pair of the form "key:value", where value is a string, extract it.
         * Note: https://github.com/p-org/P/issues/447 is an issue here.
         */
        public static String kvPairToString(String kv) {
            return kv.split(":", 2)[1];
        }
    }
}
