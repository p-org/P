package pobserve.logger;

import pobserve.PObserve;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class PObserveLogger {

    public static final Logger LOGGER = LogManager.getLogger(PObserve.class);

    public static void info(String message) {
        LOGGER.info(message);
    }

    public static void error(String message) {
        LOGGER.error(message);
    }

    public static void warn(String message) {
        LOGGER.warn(message);
    }

}
