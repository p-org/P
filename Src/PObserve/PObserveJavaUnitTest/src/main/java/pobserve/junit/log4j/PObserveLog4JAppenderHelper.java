package pobserve.junit.log4j;

import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.Layout;
import org.apache.logging.log4j.core.layout.PatternLayout;
import org.junit.jupiter.api.TestInfo;

import pobserve.commons.Parser;
import pobserve.runtime.events.PEvent;

import java.io.Serializable;
import java.util.List;
import java.util.function.Supplier;

public class PObserveLog4JAppenderHelper {
    protected PObserveLog4JAppenderHelper() {
        throw new UnsupportedOperationException();
    }

    public static PObserveLog4JAppender installPObserveAppender(
            String layoutFormat,
            Parser<PEvent<?>> parser,
            List<Supplier<?>> monitorSuppliers
    ) {
        Layout<? extends Serializable> layout = PatternLayout.newBuilder()
                .withPattern(layoutFormat)
                .build();

        PObserveLog4JAppender pObserveAppender =
                new PObserveLog4JAppender("PObserveAppender", layout, parser, monitorSuppliers);
        pObserveAppender.start();

        Logger rootLogger = LogManager.getRootLogger();
        org.apache.logging.log4j.core.Logger coreLogger = (org.apache.logging.log4j.core.Logger) rootLogger;

        coreLogger.get().addAppender(pObserveAppender, Level.DEBUG, null);

        return pObserveAppender;
    }

    public static void teardownPObserveAppender(PObserveLog4JAppender pObserveAppender) {
        if (pObserveAppender != null) {
            Logger rootLogger = LogManager.getRootLogger();
            org.apache.logging.log4j.core.Logger coreLogger = (org.apache.logging.log4j.core.Logger) rootLogger;

            coreLogger.get().removeAppender("PObserveAppender");

            pObserveAppender.close();
        }
    }

    public static void teardownPObserveAppender(PObserveLog4JAppender pObserveAppender, TestInfo testInfo) {
        if (pObserveAppender != null) {
            Logger rootLogger = LogManager.getRootLogger();
            org.apache.logging.log4j.core.Logger coreLogger = (org.apache.logging.log4j.core.Logger) rootLogger;

            coreLogger.get().removeAppender("PObserveAppender");

            pObserveAppender.close(testInfo);
        }
    }
}