package psymbolic.runtime.logger;

import org.apache.log4j.*;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.valuesummary.solvers.sat.expr.Fraig;

import java.io.IOException;

public class StatLogger {
    /* Get actual class name to be printed on */
    static Logger log = Logger.getLogger(StatLogger.class.getName());

    public static void Initialize(String projectName)
    {
        // remove all the appenders
        log.removeAllAppenders();
        // setting up the logger
        //This is the root logger provided by log4j
        log.setLevel(Level.ALL);

        //Define log pattern layout
        PatternLayout layout = new PatternLayout("%m%n");

        //Add console appender to root logger
        log.addAppender(new ConsoleAppender(layout));

        try
        {
            // get new file name
//            SimpleDateFormat formatter = new SimpleDateFormat("dd:MM:yyyy HH:mm:ss");
//            Date date = new Date();
//            String fileName = "output/stats-"+date.toString() + ".log";
            String fileName = "output/stats-"+projectName + ".log";
            //Define file appender with layout and output log file name
            RollingFileAppender fileAppender = new RollingFileAppender(layout, fileName);
            //Add the appender to root logger
            log.addAppender(fileAppender);
        }
        catch (IOException e)
        {
            System.out.println("Failed to add appender to the SearchLogger!!");
        }
    }

    public static void disable() {
        log.setLevel(Level.OFF);
    }

    public static void enable() {
        log.setLevel(Level.ALL);
    }

    public static void log(String message)
    {
        log.info(message);
    }

    public static void logSolverStats() {
        log.info(String.format("#-vars:\t%d", SolverEngine.getVarCount()));
        log.info(String.format("#-guards:\t%d", SolverEngine.getGuardCount()));
        log.info(String.format("#-expr:\t%d", SolverEngine.getSolver().getExprCount()));
        log.info(String.format("#-ops:\t%d", SolverStats.andOperations+SolverStats.orOperations+SolverStats.notOperations));
        log.info(String.format("solver-#-nodes:\t%d", SolverEngine.getSolver().getNodeCount()));
        log.info(String.format("solver-#-sat-ops:\t%d", SolverStats.isSatOperations));
        log.info(String.format("solver-%%-sat-ops-sat:\t%.1f", SolverStats.isSatPercent(SolverStats.isSatOperations, SolverStats.isSatResult)));
//        log.info(String.format("aig-#-sat-ops:\t%d", Fraig.isSatOperations));
//        log.info(String.format("aig-%%-sat-ops-sat:\t%.1f", SolverStats.isSatPercent(Fraig.isSatOperations, Fraig.isSatResult)));
    }
    
}
