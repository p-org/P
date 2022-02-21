package psymbolic.runtime.statistics;

public class SolverStats {
    public static int andOperations = 0;
    public static int orOperations = 0;
    public static int notOperations = 0;
    public static int isSatOperations = 0;
    public static int isSatResult = 0;

    public static double isSatPercent() {
    	return (isSatOperations == 0 ? 0.0 : (isSatResult * 100.0 / isSatOperations));
    }
    
    public static String prettyPrint() {
        return 	  String.format(   "  totalAndOperations     = %d", andOperations)
        		+ String.format( "\n  totalOrOperations      = %d", orOperations)
        		+ String.format( "\n  totalNotOperations     = %d", notOperations)
        		+ String.format( "\n  totalIsSatOperations  = %d\t(%.1f %% sat)", isSatOperations, isSatPercent());
    }
}
