package psymbolic.runtime.logger;

import lombok.Getter;
import lombok.Setter;
import org.apache.log4j.*;
import psymbolic.runtime.statistics.SearchStats;

import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.Date;

public class SearchLogger {
    /* Get actual class name to be printed on */
    static Logger log = Logger.getLogger(SearchLogger.class.getName());
    @Getter @Setter
    static int verbosity;

    public static void Initialize(int verb)
    {
        verbosity = verb;
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

    public static void logResumeExecution(int iter, int step)
    {
        log.info("--------------------");
        log.info("Resuming Iteration: " + iter + " from Step: " + step);
    }

    public static void logStartExecution(int iter, int step)
    {
        log.info("--------------------");
        log.info("Starting Iteration: " + iter + " from Step: " + step);
    }

    public static void logDepthStats(SearchStats.DepthStats depthStats)
    {
        log.info(String.format("Depth: %d: TotalTransitions = %d, ReducedTransitionsExplored = %d", depthStats.getDepth(), depthStats.getNumOfTransitions(), depthStats.getNumOfTransitionsExplored()));
    }

    public static void logIterationStats(SearchStats.IterationStats iterStats)
    {

        log.info(String.format("Finished Iteration: %d: Max Depth: %d, TotalStates = %d, TotalTransitions = %d, ReducedTransitionsExplored = %d",
                iterStats.getIteration(), iterStats.getIterationTotal().getDepth(), iterStats.getIterationTotal().getNumOfStates(), iterStats.getIterationTotal().getNumOfTransitions(), iterStats.getIterationTotal().getNumOfTransitionsExplored()));
    }
    
}
