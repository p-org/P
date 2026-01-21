package lockserver.pobservejunit.lockserver;

import lockserver.pobservejunit.LockServer;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;


public class LockServerTest {
    @Test
    public void testAcquireAndReleaseLock() {
        LockServer lockServer = new LockServer();
        String client1ID = "123";
        String lockName = "1";
        int transactionId = 0;
        // Acquire a lock
        assertTrue(lockServer.acquireLock(lockName, client1ID, String.valueOf(transactionId)));
        // Release the lock
        assertTrue(lockServer.releaseLock(lockName, client1ID, String.valueOf(transactionId++)));
    }

    @Test
    public void testAcquireLockFail() {
        LockServer lockServer = new LockServer();
        String client1ID = "123";
        String lockName = "1";
        int transactionId = 0;
        // Attempt to acquire a lock that doesn't exist
        assertFalse(lockServer.releaseLock(lockName, client1ID, String.valueOf(transactionId)));

    }

    @Test
    public void testReleaseLockFail() {
        LockServer lockServer = new LockServer();
        String client1ID = "123";
        String lockName = "1";
        int transactionId = 0;
        // Attempt to release a lock that doesn't exist
        assertFalse(lockServer.releaseLock(lockName, client1ID, String.valueOf(transactionId)));
    }

    @Test
    public void testReleaseLockNotHeldByClient() {
        LockServer lockServer = new LockServer();
        String client1ID = "123";
        String client2ID = "456";
        String lockName = "1";
        int transactionId = 0;
        // Acquire a lock with one client
        lockServer.acquireLock(lockName, client1ID, String.valueOf(transactionId));

        // Another client attempts to release the lock
        assertFalse(lockServer.releaseLock(lockName, client2ID, String.valueOf(transactionId++)));
    }
}
