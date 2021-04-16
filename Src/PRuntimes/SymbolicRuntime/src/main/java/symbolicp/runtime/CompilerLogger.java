package symbolicp.runtime;

import java.util.Arrays;
import java.util.logging.Level;
import java.util.logging.Logger;

public class CompilerLogger {
    private final static Logger log = LoggingUtils.getLog("COMPILER");

    public static void log(Object ... message) {
       log.info("<Compiler> " + String.join(", ", Arrays.toString(message)));
    }

    public static void disable() {
        log.setLevel(Level.OFF);
    }

    public static void enable() { log.setLevel(Level.ALL); }
}