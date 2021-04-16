package symbolicp.runtime;

import java.util.logging.Logger;

public class LoggingUtils {

    private static String format = "[%3$s] %5$s %6$s%n";

    public static Logger getLog(String name) {
        // set logging format programmatically here
        System.setProperty("java.util.logging.SimpleFormatter.format", format);
        return Logger.getLogger(name);
    }
}
