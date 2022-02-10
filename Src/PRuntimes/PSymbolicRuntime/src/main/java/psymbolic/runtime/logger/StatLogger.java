package psymbolic.runtime.logger;

import org.apache.log4j.*;
import psymbolic.runtime.statistics.BDDStats;
import psymbolic.runtime.statistics.SearchStats;
import psymbolic.valuesummary.bdd.BDDEngine;

import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.Date;

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

    public static void logBDDStats() {
        log.info(String.format("bdd-#-vars:\t%d", BDDEngine.getInstance().getVarCount()));
        log.info(String.format("bdd-#-nodes:\t%d", BDDEngine.getInstance().getNodeCount()));
        log.info(String.format("bdd-#-and-ops:\t%d", BDDStats.andOperations));
        log.info(String.format("bdd-#-or-ops:\t%d", BDDStats.orOperations));
        log.info(String.format("bdd-#-not-ops:\t%d", BDDStats.notOperations));
        log.info(String.format("bdd-#-istrue-ops:\t%d", BDDStats.isTrueOperations));
        log.info(String.format("bdd-#-isfalse-ops:\t%d", BDDStats.isFalseOperations));
        log.info(String.format("bdd-%%-istrue-ops-yes:\t%.1f", BDDStats.isTruePercent()));
        log.info(String.format("bdd-%%-isfalse-ops-yes:\t%.1f", BDDStats.isFalsePercent()));
    }
    
}
