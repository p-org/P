package pobserve.testing.parsers;

import pobserve.commons.PObserveEvent;

import pobserve.testing.spec.PGenerated.Java.PEvents;
import pobserve.testing.spec.PGenerated.Java.PTypes;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import pobserve.runtime.events.PEvent;

import static pobserve.testing.spec.PGenerated.Java.PTypes.tLockRespStatus.LOCK_ERROR;
import static pobserve.testing.spec.PGenerated.Java.PTypes.tLockRespStatus.LOCK_SUCCESS;
import static pobserve.testing.spec.PGenerated.Java.PTypes.tLockStatus.FREE;
import static pobserve.testing.spec.PGenerated.Java.PTypes.tLockStatus.LOCKED;
import static pobserve.testing.spec.PGenerated.Java.PTypes.tReleaseRespStatus.RELEASE_ERROR;
import static pobserve.testing.spec.PGenerated.Java.PTypes.tReleaseRespStatus.RELEASE_SUCCESS;

import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

class TestCase3ParserTest {
    @Test
    void testLockRequest() {
        TestCase3Parser parser = new TestCase3Parser();
        String logLine = "2023-11-06 14:12:54.000933 [Test worker] INFO  StructuredLogger - : MessageType=REQUEST, ClientID=12345, LockID=98765, TransactionType=LOCK, TransactionID=6789, Result=null, LockStatus=null, Message=This is a message";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        System.out.println(observeEvent);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eLockReq);
        PEvents.eLockReq event = (PEvents.eLockReq) observeEvent.getEvent();
        PTypes.PTuple_clnt_lock_rId payload = event.getPayload();
        Assertions.assertEquals(12345, payload.clientId);
        Assertions.assertEquals(6789, payload.rId);
        Assertions.assertEquals(98765, payload.lockId);
    }

    @Test
    void testLockResponseSuccess() {
        TestCase3Parser parser = new TestCase3Parser();
        String logLine = "2023-11-06 14:12:54.000933 [Test worker] INFO  StructuredLogger - : MessageType=RESPONSE, ClientID=12345, LockID=98765, TransactionType=LOCK, TransactionID=6789, Result=SUCCESS, LockStatus=LOCKED, Message=This is a message";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        System.out.println(observeEvent);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eLockResp);
        PEvents.eLockResp event = (PEvents.eLockResp) observeEvent.getEvent();
        PTypes.PTuple_stts_clnt_lock_lckst_rId payload = event.getPayload();
        Assertions.assertEquals(12345, payload.clientId);
        Assertions.assertEquals(6789, payload.rId);
        Assertions.assertEquals(98765, payload.lockId);
        Assertions.assertEquals(LOCKED, payload.lockStatus);
        Assertions.assertEquals(LOCK_SUCCESS, payload.status);
    }

    @Test
    void testLockResponseFail() {
        TestCase3Parser parser = new TestCase3Parser();
        String logLine = "2023-11-06 14:12:54.000933 [Test worker] INFO  StructuredLogger - : MessageType=RESPONSE, ClientID=12345, LockID=98765, TransactionType=LOCK, TransactionID=6789, Result=FAIL, LockStatus=FREE, Message=This is a message";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        System.out.println(observeEvent);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eLockResp);
        PEvents.eLockResp event = (PEvents.eLockResp) observeEvent.getEvent();
        PTypes.PTuple_stts_clnt_lock_lckst_rId payload = event.getPayload();
        Assertions.assertEquals(12345, payload.clientId);
        Assertions.assertEquals(6789, payload.rId);
        Assertions.assertEquals(98765, payload.lockId);
        Assertions.assertEquals(FREE, payload.lockStatus);
        Assertions.assertEquals(LOCK_ERROR, payload.status);
    }

    @Test
    void testReleaseRequest() {
        TestCase3Parser parser = new TestCase3Parser();
        String logLine = "2023-11-06 14:12:54.000933 [Test worker] INFO  StructuredLogger - : MessageType=REQUEST, ClientID=12345, LockID=98765, TransactionType=RELEASE, TransactionID=6789, Result=null, LockStatus=null, Message=This is a message";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        System.out.println(observeEvent);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eReleaseReq);
        PEvents.eReleaseReq event = (PEvents.eReleaseReq) observeEvent.getEvent();
        PTypes.PTuple_clnt_lock_rId payload = event.getPayload();
        Assertions.assertEquals(12345, payload.clientId);
        Assertions.assertEquals(6789, payload.rId);
        Assertions.assertEquals(98765, payload.lockId);
    }

    @Test
    void testReleaseResponseSuccess() {
        TestCase3Parser parser = new TestCase3Parser();
        String logLine = "2023-11-06 14:12:54.000933 [Test worker] INFO  StructuredLogger - : MessageType=RESPONSE, ClientID=12345, LockID=98765, TransactionType=RELEASE, TransactionID=6789, Result=SUCCESS, LockStatus=FREE, Message=This is a message";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        System.out.println(observeEvent);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eReleaseResp);
        PEvents.eReleaseResp event = (PEvents.eReleaseResp) observeEvent.getEvent();
        PTypes.PTuple_stts_clnt_lock_lckst_rId_1 payload = event.getPayload();
        Assertions.assertEquals(12345, payload.clientId);
        Assertions.assertEquals(6789, payload.rId);
        Assertions.assertEquals(98765, payload.lockId);
        Assertions.assertEquals(FREE, payload.lockStatus);
        Assertions.assertEquals(RELEASE_SUCCESS, payload.status);
    }

    @Test
    void testReleaseResponseFail() {
        TestCase3Parser parser = new TestCase3Parser();
        String logLine = "2023-11-06 14:12:54.000933 [Test worker] INFO  StructuredLogger - : MessageType=RESPONSE, ClientID=12345, LockID=98765, TransactionType=RELEASE, TransactionID=6789, Result=FAIL, LockStatus=LOCKED, Message=This is a message";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        System.out.println(observeEvent);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eReleaseResp);
        PEvents.eReleaseResp event = (PEvents.eReleaseResp) observeEvent.getEvent();
        PTypes.PTuple_stts_clnt_lock_lckst_rId_1 payload = event.getPayload();
        Assertions.assertEquals(12345, payload.clientId);
        Assertions.assertEquals(6789, payload.rId);
        Assertions.assertEquals(98765, payload.lockId);
        Assertions.assertEquals(LOCKED, payload.lockStatus);
        Assertions.assertEquals(RELEASE_ERROR, payload.status);
    }
}
