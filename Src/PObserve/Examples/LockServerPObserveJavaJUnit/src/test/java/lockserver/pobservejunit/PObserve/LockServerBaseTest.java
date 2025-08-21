package lockserver.pobservejunit.PObserve;

import lockserver.pobservejunit.LockServer;
import pobserve.junit.log4j.PObserveLog4JAppender;
import pobserve.junit.log4j.PObserveLog4JAppenderHelper;

import java.util.List;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.TestInfo;

import lockserver.pobserve.parser.LockServerParser;

public class LockServerBaseTest {
    PObserveLog4JAppender appender;
    LockServer lockServer = new LockServer();
    String client1Id = "123";
    String client2Id = "456";
    String lockId = "1";
    int transactionId = 0;
    @BeforeEach
    public void init() {
        /* Some other stuff needed for init tests */
        lockServer = new LockServer();

        // Initialize PObserveLog4JAppender
        appender = PObserveLog4JAppenderHelper.installPObserveAppender(
                "%d{yyyy-MM-dd HH:mm:ss.SSSSSS} [%t] %-5level %logger{1} - %marker: %msg%n",
                new LockServerParser(),
                List.of(new lockserver.pobserve.spec.PMachines.MutualExclusion.Supplier(),
                        new lockserver.pobserve.spec.PMachines.ResponseOnlyOnRequest.Supplier()));

    }

    @AfterEach
    public void close(TestInfo testInfo) {
        /* Some other stuff needed for terminate tests */

        // Shutdown PObserveLog4JAppender
        PObserveLog4JAppenderHelper.teardownPObserveAppender(appender, testInfo);
    }
}
