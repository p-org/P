package lockserver.pobservejunit.logging;

import lockserver.pobservejunit.LockServer;
import lockserver.pobservejunit.constants.LockStatus;
import lockserver.pobservejunit.constants.Result;

import edu.umd.cs.findbugs.annotations.Nullable;
import lombok.Builder;
import lombok.extern.slf4j.Slf4j;

@Builder
@Slf4j
public class StructuredLogger {
    private final String logMessageType; //true if message is request, false if message is response
    @Nullable
    private final String clientID;
    private final String transactionID;
    private final String lockID;
    private final Result logResult;
    private final String message;
    private final String transactionType;
    private final LockStatus lockStatus;


    public void logInfo() {
        String logMessage = String.format("MessageType=%s, ClientID=%s, LockID=%s, TransactionType=%s, "
                        + "TransactionID=%s, Result=%s, LockStatus=%s, Message=%s", logMessageType, clientID, lockID,
                transactionType, transactionID, logResult, lockStatus, message);
        log.info(logMessage);
    }

    public void logError() {
        String logMessage = String.format("MessageType=%s, ClientID=%s, LockID=%s, TransactionType=%s, "
                        + "TransactionID=%s, Result=%s, LockStatus=%s, Message=%s", logMessageType, clientID, lockID,
                transactionType, transactionID, logResult, lockStatus, message);
        log.error(logMessage);
    }


}
