package psymbolic.runtime.logger;

import org.apache.log4j.*;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.runtime.statistics.SearchStats;
import psymbolic.valuesummary.solvers.SolverEngine;

import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.Date;

public class SearchLogger {
    /* Get actual class name to be printed on */
    static Logger log = Logger.getLogger(SearchLogger.class.getName());

    public static void Initialize()
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
            SimpleDateFormat formatter = new SimpleDateFormat("dd:MM:yyyy HH:mm:ss");
            Date date = new Date();
            String fileName = "output/searchStats-"+date.toString() + ".log";
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

    public static void logDepthStats(SearchStats.DepthStats depthStats)
    {
        log.info(String.format("Depth: %d: TotalTransitions = %d, ReducedTransitionsExplored = %d", depthStats.getDepth(), depthStats.getNumOfTransitions(), depthStats.getNumOfTransitionsExplored()));
        log.info("BDD States:\n" + SolverEngine.getSolver().getStats());
        log.info(SolverStats.prettyPrint());
    }

    public static void logIterationStats(SearchStats.IterationStats iterStats)
    {

        log.info(String.format("Finished Iteration: %d: Max Depth: %dTotalTransitions = %d, ReducedTransitionsExplored = %d",
                iterStats.getIteration(), iterStats.getIterationTotal().getDepth(), iterStats.getIterationTotal().getNumOfTransitions(), iterStats.getIterationTotal().getNumOfTransitionsExplored()));
    }
    
}
