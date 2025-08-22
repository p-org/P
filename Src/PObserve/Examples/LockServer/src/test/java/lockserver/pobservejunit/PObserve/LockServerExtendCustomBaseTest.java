package lockserver.pobservejunit.PObserve;

import lockserver.pobservejunit.LockServer;

import java.util.Random;

import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import static lockserver.pobservejunit.constants.LockStatus.LOCKED;
import static lockserver.pobservejunit.constants.Result.SUCCESS;
import static lockserver.pobservejunit.logging.TransactionLogger.logLockResponse;

public class LockServerExtendCustomBaseTest extends LockServerBaseTest{
    /**
     * Testing basic actions performed by users
     */
    @Test
    public void basicTest() {
        // Client 1 locks a lock
        lockServer.acquireLock(lockId, client1Id, String.valueOf(transactionId));
        // Client 2 attempts to release a lock that Client 1 holds
        lockServer.releaseLock(lockId, client2Id, String.valueOf(transactionId++));
        // Client 1 attempts to acquire lock again
        lockServer.acquireLock(lockId, client1Id, String.valueOf(transactionId++));
        // Client 1 releases the lock
        lockServer.releaseLock(lockId, client1Id, String.valueOf(transactionId++));
        // Client 2 attempts to acquire lock
        lockServer.acquireLock(lockId, client2Id, String.valueOf(transactionId));
    }

    /**
     *  Test if PObserve is working by sending fake message and expect PObserve to catch it
     */
    @Tag("ExpectFail")
    @Test
    public void expectFail() {
        logLockResponse(client1Id, lockId, String.valueOf(transactionId), SUCCESS, LOCKED);
    }

    /**
     *  A single client performs random actions on 5 locks 1000 times
     **/
    @Test
    public void randomClient() throws InterruptedException {
        Random random = new Random();
        int action;

        for (int i = 0; i < 1000; i++) {
            lockId = String.valueOf(random.nextInt(5));
            action = random.nextInt(2);
            if (action == 0) {
                lockServer.acquireLock(lockId, client1Id, String.valueOf(transactionId++));
            }
            else {
                lockServer.releaseLock(lockId, client1Id, String.valueOf(transactionId++));
            }
        }
    }

    /**
     *   5 clients performs random actions on 5 locks 1000 times
     */
    @Test
    public void multipleRandomClients() {
        Random random = new Random();
        int action;
        String clientId;

        for (int i = 0; i < 1000; i++) {
            lockId = String.valueOf(random.nextInt(5));
            clientId = String.valueOf(random.nextInt(5));
            action = random.nextInt(2);
            if (action == 0) {
                lockServer.acquireLock(lockId, clientId, String.valueOf(transactionId++));
            } else {
                lockServer.releaseLock(lockId, clientId, String.valueOf(transactionId++));
            }
        }
    }
}
