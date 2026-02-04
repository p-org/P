package lockserver.pobservejunit.PObserve;

import lockserver.pobservejunit.LockServer;
import pobserve.junit.PObserveJUnitSpecConfig;
import pobserve.junit.log4j.PObserveLog4JBaseTest;

import java.util.Random;

import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import lockserver.pobserve.parser.LockServerParser;

import static lockserver.pobservejunit.constants.LockStatus.LOCKED;
import static lockserver.pobservejunit.constants.Result.SUCCESS;
import static lockserver.pobservejunit.logging.TransactionLogger.logLockResponse;

@PObserveJUnitSpecConfig(
        parser = LockServerParser.class,
        monitors = {
                lockserver.pobserve.spec.PMachines.MutualExclusion.Supplier.class,
                lockserver.pobserve.spec.PMachines.ResponseOnlyOnRequest.Supplier.class
        },
        appenderName = "LogFile"
)
public class LockServerExtendPObserveBaseTest extends PObserveLog4JBaseTest {
    @Test
    public void basicTest() {
        // Client 1 attempts to acquire and release a lock
        LockServer lockServer = new LockServer();
        String client1Id = "123";
        String client2Id = "456";
        String lockId = "1";
        int transactionId = 0;

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

    @Tag("ExpectFail")
    @Test
    public void expectFail() {
        String client1Id = "123";
        String lockId = "1";
        int transactionId = 0;
        // Send fake message
        logLockResponse(client1Id, lockId, String.valueOf(transactionId), SUCCESS, LOCKED);
    }

    /** single client performs random actions on 5 locks 1000 times **/
    @Test
    public void randomClient() throws InterruptedException {

        Random random = new Random();
        LockServer lockServer = new LockServer();
        int action;
        String lockId;
        String clientId = "123";
        int transactionId = 0;

        for (int i = 0; i < 1000; i++) {
            lockId = String.valueOf(random.nextInt(5));
            action = random.nextInt(2);
            if (action == 0) {
                lockServer.acquireLock(lockId, clientId, String.valueOf(transactionId++));
            }
            else {
                lockServer.releaseLock(lockId, clientId, String.valueOf(transactionId++));
            }
        }
    }

    // 5 client performs random actions on 5 locks 1000 times
    @Test
    public void multipleRandomClients() {
        Random random = new Random();
        LockServer lockServer = new LockServer();
        int action;
        String lockId;
        String clientId;
        int transactionId = 0;

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
