package symbolicp.runtime;

import symbolicp.vs.PrimVS;

import java.util.Arrays;
import java.util.logging.Level;
import java.util.logging.Logger;

public class RuntimeLogger {
    private final static Logger log = LoggingUtils.getLog("RUNTIME");

    public static void log(Object ... message) {
       log.info("<PrintLog> " + String.join(", ", Arrays.toString(message)));
    }

    public static void property(Object ... message) {
        log.info("<PropertyLog> " + String.join(", ", Arrays.toString(message)));
    }

    public static void disable() {
        log.setLevel(Level.OFF);
    }

    public static void enable() { log.setLevel(Level.ALL); }

}