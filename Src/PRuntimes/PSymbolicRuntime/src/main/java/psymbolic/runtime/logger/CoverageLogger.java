package psymbolic.runtime.logger;

import org.apache.log4j.Level;
import org.apache.log4j.Logger;
import org.apache.log4j.PatternLayout;
import org.apache.log4j.RollingFileAppender;
import psymbolic.runtime.scheduler.Schedule;
import psymbolic.runtime.statistics.CoverageStats;

import java.io.IOException;
import java.util.List;

public class CoverageLogger {
    /* Get actual class name to be printed on */
    static Logger log = Logger.getLogger(CoverageLogger.class.getName());

    public static void Initialize(String projectName, String outputFolder) {
        // remove all the appenders
        log.removeAllAppenders();
        // setting up the logger
        //This is the root logger provided by log4j
        log.setLevel(Level.ALL);

        //Define log pattern layout
        PatternLayout layout = new PatternLayout("%m%n");

        try {
            // get new file name
            String fileName = outputFolder + "/coverage-"+projectName + ".log";
            //Define file appender with layout and output log file name
            RollingFileAppender fileAppender = new RollingFileAppender(layout, fileName);
            //Add the appender to root logger
            log.addAppender(fileAppender);
            log.info(String.format("%15s%15s%15s%15s%15s", "Step", "CoveredSch", "CoveredData", "RemainingSch", "RemainingData"));
        }
        catch (IOException e) {
            System.out.println("Failed to add appender to the CoverageLogger!!");
        }
    }

    public static void disable() {
        log.setLevel(Level.OFF);
    }

    public static void enable() {
        log.setLevel(Level.ALL);
    }

    public static void log(int step, CoverageStats.CoverageDepthStats val) {
        log.info(String.format("%15s%15s%15s%15s%15s",
                step,
                val.getNumScheduleExplored(),
                val.getNumDataExplored(),
                val.getNumScheduleRemaining(),
                val.getNumDataRemaining()));
    }
}
