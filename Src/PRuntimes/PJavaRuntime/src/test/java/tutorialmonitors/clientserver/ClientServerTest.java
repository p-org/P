package tutorialmonitors.clientserver;

import java.util.HashMap;
import java.util.Map;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import prt.*;

import static org.junit.jupiter.api.Assertions.*;

import static prt.values.Equality.deepEquals;
import static tutorialmonitors.clientserver.ClientServer.*;

public class ClientServerTest {
    private BankBalanceIsAlwaysCorrect initedBankBalanceIsAlwaysCorrect() {
        HashMap<Integer, Integer> initialBalances =
                new HashMap<>(Map.of(100, 42, 101, 10));

        BankBalanceIsAlwaysCorrect m = new BankBalanceIsAlwaysCorrect();
        m.ready();
        m.process(new eSpec_BankBalanceIsAlwaysCorrect_Init(initialBalances));

        return m;
    }

    @Test
    @DisplayName("Can initialise a BankBalanceIsAlwaysCorrect monitor")
    public void testInitMonitor() {
        BankBalanceIsAlwaysCorrect m = initedBankBalanceIsAlwaysCorrect();
    }


    @Test
    @DisplayName("Asserts on invalid account ID")
    void testAssertsOnInvalidAccountID() {
        BankBalanceIsAlwaysCorrect m = initedBankBalanceIsAlwaysCorrect();

        assertThrows(prt.PAssertionFailureException.class,
                () -> m.process(new eWithDrawReq(
                        new PTuple_src_accnt_amnt_rId(0L, 31337, 10, 0))),
                "Assertion failure: Unknown accountId 102 in the withdraw request. Valid accountIds = [100, 101]");
    }


    @Test
    @DisplayName("Can consume some valid withdraw events")
    void testProcessValidWithdraws() {
        BankBalanceIsAlwaysCorrect m = initedBankBalanceIsAlwaysCorrect();

        m.process(new eWithDrawReq(
                new PTuple_src_accnt_amnt_rId(1L, 100, 10, 0)
        ));

        m.process(new eWithDrawResp(
                new PTuple_stts_accnt_blnc_rId(
                        tWithDrawRespStatus.WITHDRAW_SUCCESS, 100, 32, 0)));
    }

    @Test
    @DisplayName("Throws on invalid withdraw events")
    void testThrowsOnInvalidWithdraws() {
        BankBalanceIsAlwaysCorrect m = initedBankBalanceIsAlwaysCorrect();

        m.process(new eWithDrawReq(new PTuple_src_accnt_amnt_rId(1L, 100, 10, 0)));

        assertThrows(prt.PAssertionFailureException.class,
                () -> m.process(new eWithDrawResp(new PTuple_stts_accnt_blnc_rId(
                        tWithDrawRespStatus.WITHDRAW_SUCCESS,
                        100,
                        1000, /* Uh oh! The balance should be 42 - 10 = 32. */
                        0))));
    }

    private Monitor initedGuaranteWDProgress() {
        GuaranteedWithDrawProgress m = new GuaranteedWithDrawProgress();
        m.ready();

        return m;
    }

    @Test
    @DisplayName("Can process valid events")
    public void testWithDrawReqs() {
        Monitor m = initedGuaranteWDProgress();

        m.process(new eWithDrawReq(new PTuple_src_accnt_amnt_rId(1L, 100, 10, 0)));

        m.process(new eWithDrawResp(new PTuple_stts_accnt_blnc_rId(
                tWithDrawRespStatus.WITHDRAW_SUCCESS,
                100,
                90,
                0)));
    }

    @Test
    @DisplayName("Throws un invalid state transitions")
    public void testInvalidWithDrawReqs() {
        Monitor m = initedGuaranteWDProgress();

        // We begin in the NopendingRequests state, but that state has no handler
        // for a withDrawRewp.
        assertThrows(UnhandledEventException.class, () -> m.process(new eWithDrawResp(
                new PTuple_stts_accnt_blnc_rId(
                        tWithDrawRespStatus.WITHDRAW_ERROR,
                        100,
                        90,
                        0))));
    }

    @Test
    @DisplayName("Throws un invalid transactions")
    public void testInvalidWithDrawReqs2() {
        Monitor m = initedGuaranteWDProgress();

        m.process(new eWithDrawReq(new PTuple_src_accnt_amnt_rId(1L, 100, 10, 0)));

        // We begin in the NopendingRequests state, but that state has no handler
        // for a withDrawRewp.
        assertThrows(PAssertionFailureException.class, () -> m.process(new eWithDrawResp(
                new PTuple_stts_accnt_blnc_rId(
                        tWithDrawRespStatus.WITHDRAW_SUCCESS,
                        100,
                        90,
                        99999 /* We have never seen this rid before! */
                ))));
    }

    @Test
    @DisplayName("equality makes sense for generated events")
    public void testEqualityOnEventObjects() {
        ClientServer.PTuple_src_accnt_amnt_rId t1 =
                new ClientServer.PTuple_src_accnt_amnt_rId(4L, 0, 2, 1);
        ClientServer.PTuple_src_accnt_amnt_rId t2 = t1.deepClone();
        assertTrue(deepEquals(t1, t2));
    }

    @Test
    @DisplayName("can drive a ClientServer monitor from parsed events")
    public void testParseEventsAndMonitorExecution() {
        /*
        //  pmc POutput/netcoreapp3.1/ClientServer.dll \
        //      -m PImplementation.tcSingleClient.Execute \
        //      -i 100 -v | grep SendLog | head
        String logs = """
<SendLog> 'Client(4)' in state 'WithdrawMoney' sent event 'eWithDrawReq with payload (<source:Client(4), accountId:0, amount:29, rId:1, >)' to 'BankServer(3)'.
<SendLog> 'BankServer(3)' in state 'WaitForWithdrawRequests' sent event 'eWithDrawResp with payload (<status:0, accountId:0, balance:11, rId:1, >)' to 'Client(4)'.
<SendLog> 'Client(4)' in state 'WithdrawMoney' sent event 'eWithDrawReq with payload (<source:Client(4), accountId:0, amount:6, rId:2, >)' to 'BankServer(3)'.
<SendLog> 'BankServer(3)' in state 'WaitForWithdrawRequests' sent event 'eWithDrawResp with payload (<status:1, accountId:0, balance:11, rId:2, >)' to 'Client(4)'.
<SendLog> 'Client(4)' in state 'WithdrawMoney' sent event 'eWithDrawReq with payload (<source:Client(4), accountId:0, amount:7, rId:3, >)' to 'BankServer(3)'.
<SendLog> 'BankServer(3)' in state 'WaitForWithdrawRequests' sent event 'eWithDrawResp with payload (<status:1, accountId:0, balance:11, rId:3, >)' to 'Client(4)'.
<SendLog> 'Client(4)' in state 'WithdrawMoney' sent event 'eWithDrawReq with payload (<source:Client(4), accountId:0, amount:4, rId:4, >)' to 'BankServer(3)'.
<SendLog> 'BankServer(3)' in state 'WaitForWithdrawRequests' sent event 'eWithDrawResp with payload (<status:1, accountId:0, balance:11, rId:4, >)' to 'Client(4)'.
<SendLog> 'Client(4)' in state 'WithdrawMoney' sent event 'eWithDrawReq with payload (<source:Client(4), accountId:0, amount:3, rId:5, >)' to 'BankServer(3)'.
<SendLog> 'BankServer(3)' in state 'WaitForWithdrawRequests' sent event 'eWithDrawResp with payload (<status:1, accountId:0, balance:11, rId:5, >)' to 'Client(4)'.""";
        HashMap<Integer, Integer> initialBalances = new HashMap<>(Map.of(0, 40));

        BankBalanceIsAlwaysCorrect m = new BankBalanceIsAlwaysCorrect();
        m.ready();
        m.process(new eSpec_BankBalanceIsAlwaysCorrect_Init(initialBalances));

        ClientServerTraceParser.eventsFrom(logs.lines()).forEach(e -> m.process(e.getpEvent()));

         */
    }
}
