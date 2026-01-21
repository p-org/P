package lockserver.pobservejunit;

import java.util.HashMap;

import static lockserver.pobservejunit.constants.Result.FAIL;
import static lockserver.pobservejunit.constants.Result.SUCCESS;
import static lockserver.pobservejunit.logging.TransactionLogger.logLockRequest;
import static lockserver.pobservejunit.logging.TransactionLogger.logLockResponse;
import static lockserver.pobservejunit.logging.TransactionLogger.logReleaseRequest;
import static lockserver.pobservejunit.logging.TransactionLogger.logReleaseResponse;
import static java.lang.Thread.sleep;

public class LockServer {
    private HashMap<String, Lock> locks = new HashMap<>();

    public synchronized boolean acquireLock(String lockName, String clientID, String transactionID) {
        logLockRequest(clientID, lockName, String.valueOf(transactionID));
        try {
            sleep(1);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
        Lock lock = locks.get(lockName);
        if (lock == null) {
            lock = new Lock();
            locks.put(lockName, lock);
        }
        if (lock.acquire(clientID)) {
            logLockResponse(clientID, lockName, transactionID, SUCCESS, lock.getStatus());
            return true;
        }
        else {
            logLockResponse(clientID, lockName, transactionID, FAIL, lock.getStatus());
            return false;
        }
    }

    public synchronized boolean releaseLock(String lockName, String clientID, String transactionID) {
        logReleaseRequest(clientID, lockName, String.valueOf(transactionID));
        try {
            sleep(1);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
        Lock lock = locks.get(lockName);
        if (lock == null) {
            logReleaseResponse(clientID, lockName, transactionID, FAIL, null);
            return false; // Lock doesn't exist
        }
        if (lock.release(clientID)) {
            logReleaseResponse(clientID, lockName, transactionID, SUCCESS, lock.getStatus());
            return true;
        }
        else {
            logReleaseResponse(clientID, lockName, transactionID, FAIL, lock.getStatus());
            return false;
        }
    }
}
