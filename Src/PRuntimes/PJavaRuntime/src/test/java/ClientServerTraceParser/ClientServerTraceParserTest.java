package ClientServerTraceParser;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import tutorialmonitors.clientserver.ClientServer;

import java.util.Arrays;
import java.util.Iterator;
import java.util.List;
import java.util.stream.Stream;

import static java.util.stream.Collectors.toList;

import static org.junit.jupiter.api.Assertions.*;

public class ClientServerTraceParserTest {
    @Test
    @DisplayName("Can filter non-SendLog messages")
    public void testCanFilterNonSendlogMessages() {
        String lines[] = {
           "<MonitorLog> Monitor 'GuaranteedWithDrawProgress' raised event 'GotoStateEvent' in state 'PendingReqs'.",
           "<DequeueLog> 'Client(4)' dequeued event 'eWithDrawResp with payload (<status:0, accountId:0, balance:10, rId:7, >)' in state 'WithdrawMoney'."
        };

        List<PObserveEvent> evs = ClientServerTraceParser
                .eventsFrom(Arrays.stream(lines)).collect(toList());

        assertEquals(evs.size(), 0);
    }

    @Test
    @DisplayName("Can extract an eWithDrawReq from a SendLog message")
    public void testCanParseWithdrawReq() {
        String line = "<SendLog> 'Client(4)' in state 'WithdrawMoney' sent event " +
                "'eWithDrawReq with payload (<source:Client(4), accountId:0, amount:2, rId:1, >)' " +
                "to 'BankServer(3)'.";

        List<PObserveEvent> evs = ClientServerTraceParser
                .eventsFrom(Stream.of(line))
                .collect(toList());

        assertEquals(1, evs.size());
        assertEquals(new PObserveEvent(
                        new TimestampInterval(1L),
                        new ClientServer.eWithDrawReq(
                                    new ClientServer.PTuple_src_accnt_amnt_rId(4L, 0, 2, 1))),
                    evs.get(0));
    }

    @Test
    @DisplayName("Can extract an eReadQuery from a SendLog message")
    public void testCanParseReadQuery() {
        String line = "<SendLog> 'BankServer(3)' in state 'WaitForWithdrawRequests' " +
                "sent event 'eReadQuery with payload (<accountId:0, >)' to 'Database(5)'.";

        List<PObserveEvent> evs = ClientServerTraceParser
                .eventsFrom(Stream.of(line))
                .collect(toList());

        assertEquals(1, evs.size());
        assertEquals(
                new PObserveEvent(
                        new TimestampInterval(1L),
                        new ClientServer.eReadQuery(new ClientServer.PTuple_accnt(0))),
                evs.get(0));
    }

    @Test
    @DisplayName("Can extract an eReadQueryResp from a SendLog message")
    public void testCanParseReadQueryResp() {
        String line = "<SendLog> 'Database(5)' in state 'Init_1' sent event 'eReadQueryResp with payload (<accountId:0, balance:15, >)' to 'BankServer(3)'.";

        List<PObserveEvent> evs = ClientServerTraceParser
                .eventsFrom(Stream.of(line))
                        .collect(toList());

        assertEquals(1, evs.size());
        assertEquals(
                new PObserveEvent(
                        new TimestampInterval(1L),
                        new ClientServer.eReadQueryResp(new ClientServer.PTuple_accnt_blnc(0, 15))),
                evs.get(0));
    }

    @Test
    @DisplayName("Can extract multiple SendLog lines with increasing timestampe")
    public void testCanExtractMultipleSendLogLines() {
        Stream<String> lines = Stream.of(
            "<SendLog> 'Client(4)' in state 'WithdrawMoney' sent event 'eWithDrawReq with payload (<source:Client(4), accountId:0, amount:2, rId:1, >)' to 'BankServer(3)'.",
            "<SendLog> 'BankServer(3)' in state 'WaitForWithdrawRequests' sent event 'eReadQuery with payload (<accountId:0, >)' to 'Database(5)'.",
            "<SendLog> 'Database(5)' in state 'Init_1' sent event 'eReadQueryResp with payload (<accountId:0, balance:15, >)' to 'BankServer(3)'. ");

        Stream<PObserveEvent> evs = ClientServerTraceParser.eventsFrom(lines);
        Iterator<PObserveEvent> it = evs.iterator();

        assertEquals(
                new PObserveEvent(new TimestampInterval(1L),
                    new ClientServer.eWithDrawReq(
                            new ClientServer.PTuple_src_accnt_amnt_rId(4L, 0, 2, 1))),
                it.next());
        assertEquals(
                new PObserveEvent(new TimestampInterval(2L),
                        new ClientServer.eReadQuery(new ClientServer.PTuple_accnt(0))),
                it.next());
        assertEquals(
                new PObserveEvent(new TimestampInterval(3L),
                        new ClientServer.eReadQueryResp(
                            new ClientServer.PTuple_accnt_blnc(0, 15))),
                it.next());
    }
}
