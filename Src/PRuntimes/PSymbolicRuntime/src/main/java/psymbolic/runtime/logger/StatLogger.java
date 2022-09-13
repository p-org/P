package psymbolic.runtime.logger;

import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.SolverEngine;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;

public class StatLogger {
    static PrintWriter log = null;

    public static void Initialize(String projectName, String outputFolder)
    {
        try
        {
            // get new file name
            String fileName = outputFolder + "/stats-"+projectName + ".log";
            //Define new file printer
            File statFile = new File(fileName);
            statFile.getParentFile().mkdirs();
            statFile.createNewFile();
            log = new PrintWriter(statFile);
        }
        catch (IOException e)
        {
            System.out.println("Failed to set printer to the StatLogger!!");
        }
    }

    public static void log(String key, String value) {
        log(key, value, true);
    }

    public static void log(String key, String value, boolean printToConsole) {
        log.println(String.format("%-40s%s", key+":", value));
        log.flush();
        if (printToConsole) {
            SearchLogger.log(String.format("%-40s%s", key+":", value));
        }
    }

    public static void logSolverStats() {
        log("#-vars", String.format("%d", SolverEngine.getVarCount()));
        log("#-guards", String.format("%d", SolverEngine.getGuardCount()));
        log("#-expr", String.format("%d", SolverEngine.getSolver().getExprCount()));
        log("#-op", String.format("%d", SolverStats.andOperations+SolverStats.orOperations+SolverStats.notOperations));
        log("solver-#-nodes", String.format("%d", SolverEngine.getSolver().getNodeCount()));
        log("solver-#-sat-ops", String.format("%d", SolverStats.isSatOperations));
        log("solver-#-sat-ops-sat", String.format("%d", SolverStats.isSatResult));
        log("solver-%-sat-ops-sat", String.format("%.1f", SolverStats.isSatPercent(SolverStats.isSatOperations, SolverStats.isSatResult)));
//        log("aig-#-sat-ops", String.format("%d", Fraig.isSatOperations));
//        log("aig-%-sat-ops-sat", String.format("%.1f", SolverStats.isSatPercent(Fraig.isSatOperations, Fraig.isSatResult)));
    }
    
}
