package lockserver.pobservejunit.logging;

import lockserver.pobservejunit.constants.LockStatus;
import lockserver.pobservejunit.constants.Result;
import lockserver.pobservejunit.constants.TransactionType;

public class TransactionLogger {
    private static final String MESSAGE_TYPE_REQUEST = "REQUEST";
    private static final String MESSAGE_TYPE_RESPONSE = "RESPONSE";
    private static final String ERROR = "ERROR";
    private static StructuredLogger logger;

    public static void logLockRequest(String clientId, String lockId, String transactionID) {
        logger = StructuredLogger.builder()
                .transactionType(String.valueOf(TransactionType.LOCK))
                .clientID(clientId)
                .lockID(lockId)
                .transactionID(transactionID)
                .logMessageType(MESSAGE_TYPE_REQUEST)
                .build();
        logger.logInfo();
    }

    public static void logLockResponse(String clientId, String lockId, String transactionID, Result result, LockStatus lockStatus) {
        logger = StructuredLogger.builder()
                .transactionType(String.valueOf(TransactionType.LOCK))
                .clientID(clientId)
                .lockID(lockId)
                .transactionID(transactionID)
                .logResult(result)
                .lockStatus(lockStatus)
                .logMessageType(MESSAGE_TYPE_RESPONSE)
                .build();
        logger.logInfo();
    }

    public static void logReleaseRequest(String clientId, String lockId, String transactionID) {
        logger = StructuredLogger.builder()
                .transactionType(String.valueOf(TransactionType.RELEASE))
                .clientID(clientId)
                .lockID(lockId)
                .transactionID(transactionID)
                .logMessageType(MESSAGE_TYPE_REQUEST)
                .build();
        logger.logInfo();
    }

    public static void logReleaseResponse(String clientId, String lockId, String transactionID, Result result, LockStatus lockStatus) {
        logger = StructuredLogger.builder()
                .transactionType(String.valueOf(TransactionType.RELEASE))
                .clientID(clientId)
                .lockID(lockId)
                .transactionID(transactionID)
                .logResult(result)
                .lockStatus(lockStatus)
                .logMessageType(MESSAGE_TYPE_RESPONSE)
                .build();
        logger.logInfo();
    }

    public static void logError(String message) {
        logger = StructuredLogger.builder()
                .logMessageType(ERROR)
                .message(message)
                .build();
        logger.logError();
    }

}
