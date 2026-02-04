package pobserve.junit.log4j;

import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.core.Appender;
import org.apache.logging.log4j.core.Layout;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.config.Configuration;
import org.apache.logging.log4j.core.config.LoggerConfig;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.TestInfo;

import pobserve.commons.Parser;
import pobserve.junit.PObserveJUnitSpecConfig;
import pobserve.junit.utils.ParserAndMonitorProvider;

import java.io.Serializable;
import java.util.List;
import java.util.function.Supplier;

/**
 * This an extendable class for PObserve log4J users who doesn't have a base test in their testing class
 */
public class PObserveLog4JBaseTest {

    LoggerConfig loggerConfig;
    private PObserveLog4JAppender pobserveLog4JAppender;

    /**
     * Gets spec configs from annotation, makes a new log4JAppender, append to log4J config
     */
    @BeforeEach
    public void setupLogging() {
        PObserveJUnitSpecConfig annotation = this.getClass().getAnnotation(PObserveJUnitSpecConfig.class);
        Parser parser = ParserAndMonitorProvider.getParser(annotation.parser());
        List<Supplier<?>> monitorSuppliers = ParserAndMonitorProvider.getMonitorSuppliers(annotation.monitors());

        LoggerContext ctx = (LoggerContext) LogManager.getContext(false);
        Configuration config = ctx.getConfiguration();
        loggerConfig = config.getLoggerConfig(annotation.appenderName());
        Appender appender = config.getAppender(annotation.appenderName());

        if (appender != null) {
            Layout<? extends Serializable> layout = appender.getLayout();
            pobserveLog4JAppender = new PObserveLog4JAppender("PAppender", layout, parser, monitorSuppliers);
            pobserveLog4JAppender.start();
        } else {
            //TODO initialize your own log4j.xml if not provided

        }
        loggerConfig.addAppender(pobserveLog4JAppender, Level.INFO, null);
        ctx.updateLoggers();
    }

    /**
     * Removes/ shuts down PObserveLogAppender
     *
     * @param testInfo test info from each JUnit test case
     */
    @AfterEach
    public void shutdown(TestInfo testInfo) {
        loggerConfig.removeAppender("PAppender");
        this.pobserveLog4JAppender.close(testInfo);
    }
}
