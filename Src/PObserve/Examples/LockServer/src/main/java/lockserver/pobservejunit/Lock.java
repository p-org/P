package lockserver.pobservejunit;

import lockserver.pobservejunit.constants.LockStatus;

import static lockserver.pobservejunit.constants.LockStatus.FREE;
import static lockserver.pobservejunit.constants.LockStatus.LOCKED;

public class Lock {
    private LockStatus status = FREE;
    private String lockedBy = null;

    public synchronized boolean acquire(String clientID) {
        if (status == FREE) {
            status = LOCKED;
            lockedBy = clientID;
            return true; // Lock acquired successfully
        } else {
            return false; // Lock is already held by another client
        }
    }

    public synchronized boolean release(String clientID) {
        if (status == LOCKED && lockedBy.equals(clientID)) {
            status = FREE;
            lockedBy = null;
            return true; // Lock released successfully
        } else {
            return false; // Client doesn't hold the lock
        }
    }

    public synchronized LockStatus getStatus() {
        return status;
    }
}
