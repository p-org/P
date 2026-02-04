package pobserve.commons.logging;

import java.util.Map;

import org.apache.logging.log4j.Logger;

// Container logging class
public class PObserveJsonLogger {
    private final Logger logger;

    public PObserveJsonLogger(Logger logger) {
        this.logger = logger;
    }

    // Info logs
    // In progress
    public void inProgressLog(String message, Map<String, String> additionalInfo) {
        logger.info(LogObject.getInProgressLog(message, additionalInfo));
    }

    public void inProgressLog(String message) {
        logger.info(LogObject.getInProgressLog(message, null));
    }

    // Success
    public void successLog(String message, Map<String, String> additionalInfo) {
        logger.info(LogObject.getSuccessLog(message, additionalInfo));
    }

    public void successLog(String message) {
        logger.info(LogObject.getSuccessLog(message, null));
    }

    // Error logs
    public void errorLog(String message, Exception e, Map<String, String> additionalInfo) {
        logger.error(LogObject.getErrorLog(message, e, additionalInfo));
    }

    public void errorLog(String message, Exception e) {
        logger.error(LogObject.getErrorLog(message, e, null));
    }

    public void errorLog(String message) {
        logger.error(LogObject.getErrorLog(message, null, null));
    }
}
