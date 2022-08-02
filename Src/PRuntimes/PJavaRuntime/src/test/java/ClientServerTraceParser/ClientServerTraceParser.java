package ClientServerTraceParser;

import prt.events.PEvent;
import parsers.PTraceParserUtils;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.regex.MatchResult;
import java.util.regex.Matcher;
import java.util.stream.Stream;

import static testcases.clientserver.PEvents.*;
import static testcases.clientserver.PTypes.*;

/**
 * Here is an example of a P parser that makes use of the PTraceParserUtils` helper class to
 * transform lines of P traces into PEvents to be processed by a Monitor.  In particular, this
 * can be used to drive a Monitor for the ClientServer example of the P tutorial.  For an
 * example of this in action, check out the unit tests such as the contents of
 * tutorialmonitors.clientserver.ClientServerTest#testParseEventsAndMonitorExecution().
 */
public class ClientServerTraceParser {
    /**
     * P traces are totally ordered and so do not have a timestamp as part of their payload, so the
     * parser will synthesise an increasing timestamp for each event.
     */
    private int ts;

    /**
     * Handlers for each payload type.  A handler consumes a string representation of a payload
     * for a given P event type, and parses out the corresponding PEvent and its
     * deserialised, typesafe payload.
     */
    private static final HashMap<String, Function<String, ? extends PEvent<?>>> handlers = new HashMap<>(Map.of(
            "eWithDrawReq", ClientServerTraceParser::payloadToWithdrawReq,
            "eWithDrawResp", ClientServerTraceParser::payloadToWithdrawResp
    ));

    /**
     * Constructs a new Parser.  Client code should use the `eventsFrom` factory function.
     */
    private ClientServerTraceParser() {
        this.ts = 0;
    }

    private static eWithDrawReq payloadToWithdrawReq(String payload) {
        List<String> params = PTraceParserUtils.Conversions.namedTupleToKVPairs(payload);

        return new eWithDrawReq(
                new PTuple_src_accnt_amnt_rId(
                        PTraceParserUtils.Conversions.kvPairToMachineId(params.get(0)),
                        PTraceParserUtils.Conversions.kvPairToInt(params.get(1)),
                        PTraceParserUtils.Conversions.kvPairToInt(params.get(2)),
                        PTraceParserUtils.Conversions.kvPairToInt(params.get(3))));
    }

    private static eWithDrawResp payloadToWithdrawResp(String payload) {
        List<String> params = PTraceParserUtils.Conversions.namedTupleToKVPairs(payload);

        return new eWithDrawResp(
                new PTuple_stts_accnt_blnc_rId(
                        tWithDrawRespStatus.values()[(PTraceParserUtils.Conversions.kvPairToInt(params.get(0)))],
                        PTraceParserUtils.Conversions.kvPairToInt(params.get(1)),
                        PTraceParserUtils.Conversions.kvPairToInt(params.get(2)),
                        PTraceParserUtils.Conversions.kvPairToInt(params.get(3))));
    }


    /**
     * msgToEvent is the workhorse function of the parser.  It consumes a log line
     * representing a SendLog event and, in this case, constructs zero or more
     * corresponding POBserve events.  In our situation, we produce exactly one Event
     * if the message corresponds to an event of interest, and zero otherwise (those
     * other messages are dropped on the floor).
     *
     * If you know you have a one-to-one relationship between messages and events in
     * your parser, you can change the type signature of your equivalent method to
     * return a PObserveEvent, and in `eventsFrom`, call `Stream#map` with the method
     * rather than `Stream#flatMap` as we do here.
     */
    private Stream<PObserveEvent> msgToEvent(String msg) {
        Matcher m = PTraceParserUtils.evtPattern.matcher(msg);
        m.find();
        MatchResult r = m.toMatchResult();

        String evtName = r.group(1);
        String payload = r.group(2);

        if (handlers.containsKey(evtName)) {
            ts++;
            Function<String, ? extends PEvent<?>> f = handlers.get(evtName);
            return Stream.of(new PObserveEvent(new TimestampInterval(ts), f.apply(payload)));
        }
        return Stream.empty();
    }

    /**
     * The factory method that consumes a stream of P trace log lines produces
     * the corresponding stream of PObserveEvent objects.
     * @param s
     * @return
     */
    public static Stream<PObserveEvent> eventsFrom(Stream<String> s) {
        ClientServerTraceParser p = new ClientServerTraceParser();
        return PTraceParserUtils
                .FilterSendLogs(s)       /* We are only interested in SendLog trace messages. */
                .flatMap(p::msgToEvent); /* For all other messages, do the transformation. */
    }
}
