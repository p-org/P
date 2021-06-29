package psymbolic.runtime.statistics;

public class BDDStats {
    public static int andOperations = 0;
    public static int orOperations = 0;
    public static int notOperations = 0;

    public static String prettyPrint() {
        return String.format("totalAnd = %d, totalOr = %d, totalNot = %d", andOperations, orOperations, notOperations);
    }
}
