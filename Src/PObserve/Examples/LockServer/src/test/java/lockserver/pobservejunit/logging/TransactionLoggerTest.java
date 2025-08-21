package lockserver.pobservejunit.logging;

import lockserver.pobservejunit.constants.LockStatus;
import lockserver.pobservejunit.constants.Result;
import lockserver.pobservejunit.logging.TransactionLogger;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

public class TransactionLoggerTest {
    String logFilePath = "all.log";

    @Test
    public void testLogLockRequest() throws IOException {
        String clientId = "123456";
        String lockId = "123456";
        String transactionID = "789";
        TransactionLogger.logLockRequest(clientId, lockId, transactionID);
        // Read the log file
        String logContent = Files.readString(Path.of(logFilePath));

        // Verify the log message contains the expected values
        System.out.println("logcontent: " + logContent);
        Assertions.assertTrue(logContent.contains("MessageType=REQUEST, ClientID=123456, LockID=123456, " +
                "TransactionType=LOCK, TransactionID=789, Result=null, LockStatus=null, Message=null"));
    }

    @Test
    public void testLogLockResp() throws IOException {
        String clientId = "123456";
        String lockId = "123456";
        String transactionID = "789";
        TransactionLogger.logLockResponse(clientId, lockId, transactionID, Result.SUCCESS, LockStatus.LOCKED);
        // Read the log file
        String logContent = Files.readString(Path.of(logFilePath));

        // Verify the log message contains the expected values
        Assertions.assertTrue(logContent.contains("MessageType=RESPONSE, ClientID=123456, LockID=123456, " +
                "TransactionType=LOCK, TransactionID=789, Result=SUCCESS, LockStatus=LOCKED, Message=null"));
    }

    @Test
    public void testLogReleaseRequest() throws IOException {
        String clientId = "123456";
        String lockId = "123456";
        String transactionID = "789";
        TransactionLogger.logReleaseRequest(clientId, lockId, transactionID);
        // Read the log file
        String logContent = Files.readString(Path.of(logFilePath));

        // Verify the log message contains the expected values
        Assertions.assertTrue(logContent.contains("MessageType=REQUEST, ClientID=123456, LockID=123456, " +
                "TransactionType=RELEASE, TransactionID=789, Result=null, LockStatus=null, Message=null"));
    }

    @Test
    public void testLogReleaseResp() throws IOException {
        String clientId = "123456";
        String lockId = "123456";
        String transactionID = "789";
        TransactionLogger.logReleaseResponse(clientId, lockId, transactionID, Result.SUCCESS, LockStatus.FREE);
        // Read the log file
        String logContent = Files.readString(Path.of(logFilePath));

        // Verify the log message contains the expected values
        Assertions.assertTrue(logContent.contains("MessageType=RESPONSE, ClientID=123456, LockID=123456, " +
                "TransactionType=RELEASE, TransactionID=789, Result=SUCCESS, LockStatus=FREE, Message=null"));
    }

}
