package pobserve.testing.parsers;

import pobserve.commons.PObserveEvent;

import pobserve.testing.spec.PGenerated.Java.PEvents;
import pobserve.testing.spec.PGenerated.Java.PTypes;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import pobserve.runtime.events.PEvent;
import static pobserve.testing.spec.PGenerated.Java.PTypes.tWithDrawRespStatus.WITHDRAW_ERROR;
import static pobserve.testing.spec.PGenerated.Java.PTypes.tWithDrawRespStatus.WITHDRAW_SUCCESS;
import java.util.HashMap;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

class TestCase2ParserTest {
    @Test
    void testWithdrawResponseSuccess() {
        TestCase2Parser parser = new TestCase2Parser();
        String logLine = "2023-11-06 15:35:04.930 [main] INFO  MyClass - : MessageType=RESPONSE; "
                + "AccountID=456; RecipientAccountID=null; TransactionType=WITHDRAW; Amount=1.00; "
                + "TransactionID=324235435; Result=SUCCESS; Message=null";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eWithDrawResp);
        PEvents.eWithDrawResp event = (PEvents.eWithDrawResp) observeEvent.getEvent();
        PTypes.PTuple_stts_accnt_blnc_rId payload = event.getPayload();
        Assertions.assertEquals(payload.accountId, 456);
        Assertions.assertEquals(payload.rId, 324235435);
        Assertions.assertEquals(payload.balance, 1);
        Assertions.assertEquals(payload.status, WITHDRAW_SUCCESS);
    }

    @Test
    void testWithdrawResponseFail() {
        TestCase2Parser parser = new TestCase2Parser();
        String logLine = "2023-11-06 12:25:44.939 [pool-4-thread-5] INFO  StructuredLogger - : MessageType=RESPONSE; "
                + "AccountID=456; RecipientAccountID=null; TransactionType=WITHDRAW; Amount=1.00; "
                + "TransactionID=324235435; Result=FAIL; Message=null";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);

        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());

        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eWithDrawResp);
        PEvents.eWithDrawResp event = (PEvents.eWithDrawResp) observeEvent.getEvent();
        PTypes.PTuple_stts_accnt_blnc_rId payload = event.getPayload();
        Assertions.assertEquals(payload.accountId, 456);
        Assertions.assertEquals(payload.rId, 324235435);
        Assertions.assertEquals(payload.balance, 1);
        Assertions.assertEquals(payload.status, WITHDRAW_ERROR);
    }

    @Test
    void testWithdrawRequest() {
        TestCase2Parser parser = new TestCase2Parser();

        String logLine = "2023-11-06 15:35:04.930 [main] INFO  MyClass - : MessageType=REQUEST; "
                + "AccountID=456; RecipientAccountID=null; TransactionType=WITHDRAW; Amount=1.00; "
                + "TransactionID=324235435; Result=null; Message=null";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);

        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());

        Assertions.assertEquals(1, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eWithDrawReq);
        PEvents.eWithDrawReq event = (PEvents.eWithDrawReq) observeEvent.getEvent();
        PTypes.PTuple_accnt_amnt_rId payload = event.getPayload();
        Assertions.assertEquals(payload.accountId, 456);
        Assertions.assertEquals(payload.rId, 324235435);
        Assertions.assertEquals(payload.amount, 1);
    }

    @Test
    void testCreateAccount() {
        TestCase2Parser parser = new TestCase2Parser();

        String logLine = "2023-11-06 15:35:04.930 [main] INFO  MyClass - : MessageType=RESPONSE; AccountID=0; " +
                "RecipientAccountID=null; TransactionType=CREATE_ACCOUNT; Amount=0.00; TransactionID=789; " +
                "Result=SUCCESS; Message=[[123, 22.5], [456, 100.0]]";
        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);

        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        Assertions.assertEquals(2, resultList.size());
        PObserveEvent<PEvent<?>> observeEvent = resultList.get(0);
        Assertions.assertEquals("456", observeEvent.getPartitionKey());
        Assertions.assertEquals("123", resultList.get(1).getPartitionKey());
        Assertions.assertTrue(observeEvent.getEvent() instanceof PEvents.eSpec_BankBalanceIsAlwaysCorrect_Init);
        PEvents.eSpec_BankBalanceIsAlwaysCorrect_Init event = (PEvents.eSpec_BankBalanceIsAlwaysCorrect_Init) observeEvent.getEvent();
        HashMap<Long, Long> payload = event.getPayload();
        HashMap<Long, Long> bank = new HashMap<>();
        bank.put((long) 456, (long) 100.00);
        bank.put((long) 123, (long) 23);
        Assertions.assertEquals(payload, bank);
    }
}