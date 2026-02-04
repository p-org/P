package lockserver.pobservejunit.lockserver;

import lockserver.pobservejunit.constants.LockStatus;
import lockserver.pobservejunit.Lock;
import lockserver.pobservejunit.LockServer;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

public class LockTest {

    private Lock lock;

    @BeforeEach
    public void setUp() {
        lock = new Lock();
    }

    @Test
    public void testAcquireLock() {
        // Acquire the lock
        boolean result = lock.acquire("1");

        assertTrue(result);
        assertEquals(LockStatus.LOCKED, lock.getStatus());
    }

    @Test
    public void testAcquireLockAlreadyHeld() {
        // Acquire the lock with one client
        lock.acquire("1");

        // Another client attempts to acquire the lock
        boolean result = lock.acquire("2");

        assertFalse(result);
        assertEquals(LockStatus.LOCKED, lock.getStatus());
    }

    @Test
    public void testReleaseLock() {
        // Acquire the lock
        lock.acquire("1");

        // Release the lock
        boolean result = lock.release("1");

        assertTrue(result);
        assertEquals(LockStatus.FREE, lock.getStatus());
    }

    @Test
    public void testReleaseLockNotHeldByClient() {
        // Acquire the lock with one client
        lock.acquire("1");

        // Another client attempts to release the lock
        boolean result = lock.release("2");

        assertFalse(result);
        assertEquals(LockStatus.LOCKED, lock.getStatus());
    }

    @Test
    public void testReleaseLockWhenNotLocked() {
        // Attempt to release the lock when it's not locked
        boolean result = lock.release("1");

        assertFalse(result);
        assertEquals(LockStatus.FREE, lock.getStatus());
    }
}
