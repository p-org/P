package psymbolic.runtime.statistics;

public class BDDStats {
    public static int andOperations = 0;
    public static int orOperations = 0;
    public static int notOperations = 0;
    public static int isTrueOperations = 0;
    public static int isFalseOperations = 0;
    public static int isTrueResult = 0;
    public static int isFalseResult = 0;

    public static double isTruePercent() {
    	return (isTrueOperations == 0 ? 0.0 : (isTrueResult*100.0 / isTrueOperations));
    }
    
    public static double isFalsePercent() {
    	return (isFalseOperations == 0 ? 0.0 : (isFalseResult*100.0 / isFalseOperations));
    }
    
    public static String prettyPrint() {
        return 	  String.format(   "  totalAndOperations     = %d", andOperations)
        		+ String.format( "\n  totalOrOperations      = %d", orOperations)
        		+ String.format( "\n  totalNotOperations     = %d", notOperations)
        		+ String.format( "\n  totalIsTrueOperations  = %d\t(%.1f %% yes)", isTrueOperations, isTruePercent())
        		+ String.format( "\n  totalIsFalseOperations = %d\t(%.1f %% yes)", isFalseOperations, isFalsePercent());
    }
}
