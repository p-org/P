package pobserve.testing.parsers;

import pobserve.commons.Parser;
import pobserve.commons.PObserveEvent;
import pobserve.testing.spec.PGenerated.Java.PEvents;
import pobserve.testing.spec.PGenerated.Java.PTypes;
import pobserve.runtime.events.PEvent;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.HashMap;
import java.util.TimeZone;
import java.util.stream.Stream;

public class TestCase2Parser implements Parser<PEvent<?>> {
    /**
     * Creates a bank init pevent
     */
    private static PEvents.eSpec_BankBalanceIsAlwaysCorrect_Init onBankInit(String msg, HashMap<Long, Long> bank) {
        String[] accounts = msg.split("], ");
        for (String account : accounts) {
            String trimmedAccount = account.replace("[", "").replace("]", "");
            String[] pair =  trimmedAccount.split(", ");
            bank.put(Long.parseLong(pair[0]), Math.round(Double.parseDouble(pair[1])));
        }
        PEvents.eSpec_BankBalanceIsAlwaysCorrect_Init banklocal = new PEvents.eSpec_BankBalanceIsAlwaysCorrect_Init(bank);
        return banklocal;
    }

    /**
     * Creates a with draw request PEvent
     */
    private static PEvents.eWithDrawReq onWithdrawReq(long accountID, long amount, long rId) {
        return new PEvents.eWithDrawReq(new PTypes.PTuple_accnt_amnt_rId(accountID, amount, rId));
    }

    /**
     * Creates a with draw response PEvent
     */
    private static PEvents.eWithDrawResp onWithdrawRes(PTypes.tWithDrawRespStatus status, long accountID, long amount, long rId) {
        return new PEvents.eWithDrawResp(new PTypes.PTuple_stts_accnt_blnc_rId(status, accountID, amount, rId));
    }

    /**
     *
     * @param obj Accepts one log line
     * @return Returns stream of PEvents parsed from input log line
     */
    @Override
    public Stream<PObserveEvent<PEvent<?>>> apply(Object obj) {
        Long time;
        String transactionType = null;
        String messageType = null;
        String result = null;
        String msg = null;
        HashMap<Long, Long> bank = new HashMap<>();
        PTypes.tWithDrawRespStatus status = null;
        long accountID = 0;
        long amount = 0;
        long rId = 0;
        long recipientAccountID = 0;
        long source = -1L;
        SimpleDateFormat df = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSSSSSSSSSSSSSS");
        String line = (String) obj;
        String[] parts = line.split(" - : ");
        if (parts.length != 2) {
            return Stream.empty();
        }
        String timestampAndThread = parts[0];
        String logInfo = parts[1];
        String[] timestampAndThreadParts = timestampAndThread.split(" ");
        try {
            df.setTimeZone(TimeZone.getTimeZone("UTC")); // $NON-NLS-L$ hardcoded time zone ID
            time = df.parse(timestampAndThreadParts[0] + " " + timestampAndThreadParts[1]).getTime();
        } catch (ParseException e) {
            return Stream.empty();
        }
        String[] logInfoParts = logInfo.split("; ");
        for (String part : logInfoParts) {
            String[] keyValue = part.split("=");
            if (keyValue.length != 2) {
                return Stream.empty();
            }
            String key = keyValue[0];
            String value = keyValue[1];
            switch (key) {
                case "MessageType":
                    messageType = value;
                    break;
                case "AccountID":
                    if (!value.equals("null")) {
                        accountID = Long.parseLong(value);
                    }
                    break;
                case "RecipientAccountID":
                    if (!value.equals("null")) {
                        recipientAccountID = Long.parseLong(value);
                    }
                    break;
                case "TransactionType":
                    transactionType = value;
                    break;
                case "Amount":
                    if (!value.equals("null")) {
                        amount = Math.round(Double.parseDouble(value));
                    }
                    break;
                case "TransactionID":
                    if (!value.equals("null")) {
                        rId = Long.parseLong(value);
                    }
                    break;
                case "Result":
                    result = value;
                    break;
                case "Message":
                    msg = value;
                    break;
                default:
            }
        }
        PEvent<?> event;
        if (transactionType.equals("WITHDRAW")) {
            if (result.equals("SUCCESS")) {
                status = PTypes.tWithDrawRespStatus.WITHDRAW_SUCCESS;
            } else if (result.equals("FAIL")) {
                status = PTypes.tWithDrawRespStatus.WITHDRAW_ERROR;
            }
            if (messageType.equals("REQUEST")) {
                event = onWithdrawReq(accountID, amount, rId);
            } else if (messageType.equals("RESPONSE")) {
                event = onWithdrawRes(status, accountID, amount, rId);
            } else {
                return Stream.empty();
            }
        } else if (transactionType.equals("CREATE_ACCOUNT") && messageType.equals("RESPONSE") && result.equals("SUCCESS")) {
            event = onBankInit(msg, bank);
            Stream.Builder<PObserveEvent<PEvent<?>>> eventStreamBuilder = Stream.builder();
            bank.forEach((key, value) -> eventStreamBuilder.accept(new PObserveEvent(Long.toString(key), time, event, line)));
            return eventStreamBuilder.build();
        } else {
            return Stream.empty();
        }
        return Stream.of(new PObserveEvent(Long.toString(accountID), time, event, line));
    }
}