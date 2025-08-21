package lockserver.pobservejunit.logging;

import lockserver.pobservejunit.constants.LockStatus;
import lockserver.pobservejunit.constants.Result;
import lockserver.pobservejunit.constants.TransactionType;
import lockserver.pobservejunit.logging.StructuredLogger;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import org.mockito.MockitoAnnotations;

import static lockserver.pobservejunit.constants.LockStatus.LOCKED;
import static lockserver.pobservejunit.constants.Result.SUCCESS;
import static lockserver.pobservejunit.constants.TransactionType.LOCK;


public class StructuredLoggerTest {
    String logFilePath = "all.log";
    private StructuredLogger logger;

    @BeforeEach
    void setUp() {
        MockitoAnnotations.openMocks(this);
    }

    @Test
    public void testAllValuesFilled() throws IOException {
        logger = StructuredLogger.builder()
                .transactionType(String.valueOf(TransactionType.LOCK))
                .clientID("12345")
                .lockID("98765")
                .transactionID("6789")
                .logResult(SUCCESS)
                .logMessageType("RESPONSE")
                .lockStatus(LOCKED)
                .message("This is a message")
                .build();
        logger.logInfo();

        // Read the log file
        String logContent = Files.readString(Path.of(logFilePath));

        // Verify the log message contains the expected values
        Assertions.assertTrue(logContent.contains("MessageType=RESPONSE, ClientID=12345, LockID=98765, " +
                "TransactionType=LOCK, TransactionID=6789, Result=SUCCESS, LockStatus=LOCKED, " +
                "Message=This is a message"));
    }

    @Test
    public void testValuesNull() throws IOException {
        logger = StructuredLogger.builder()
                .transactionType(String.valueOf(TransactionType.LOCK))
                .clientID("12345")
                .lockID("98765")
                .transactionID("6789")
                .logMessageType("RESPONSE")
                .build();
        logger.logInfo();

        // Read the log file
        String logContent = Files.readString(Path.of(logFilePath));

        // Verify the log message contains the expected values
        Assertions.assertTrue(logContent.contains("MessageType=RESPONSE, ClientID=12345, LockID=98765, " +
                "TransactionType=LOCK, TransactionID=6789, Result=SUCCESS, LockStatus=LOCKED, " +
                "Message=This is a message"));
    }

}
