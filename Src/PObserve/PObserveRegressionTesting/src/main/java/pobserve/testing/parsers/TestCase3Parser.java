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
public class TestCase3Parser implements Parser<PEvent<?>> {
    private PEvents.eLockReq onLockReq(long clientID, long lockID, long rId) {
        return new PEvents.eLockReq(new PTypes.PTuple_clnt_lock_rId(clientID, lockID, rId));
    }

    private PEvents.eReleaseReq onReleaseReq(long clientID, long lockID, long rId) {
        return new PEvents.eReleaseReq(new PTypes.PTuple_clnt_lock_rId(clientID, lockID, rId));
    }

    private PEvents.eLockResp onLockRes(long clientID, long lockID, long rId, String result, PTypes.tLockStatus lockStatus) {
        PTypes.tLockRespStatus lockRespStatus = null;
        if (result.equals("SUCCESS")) {
            lockRespStatus = PTypes.tLockRespStatus.LOCK_SUCCESS;
        } else if (result.equals("FAIL")) {
            lockRespStatus = PTypes.tLockRespStatus.LOCK_ERROR;
        }
        return new PEvents.eLockResp(new PTypes.PTuple_stts_clnt_lock_lckst_rId(lockRespStatus, clientID, lockID,
                lockStatus, rId));
    }

    private PEvents.eReleaseResp onReleaseRes(long clientID, long lockID, long rId, String result, PTypes.tLockStatus lockStatus) {
        PTypes.tReleaseRespStatus releaseRespStatus = null;
        if (result.equals("SUCCESS")) {
            releaseRespStatus = PTypes.tReleaseRespStatus.RELEASE_SUCCESS;
        } else if (result.equals("FAIL")) {
            releaseRespStatus = PTypes.tReleaseRespStatus.RELEASE_ERROR;
        }
        return new PEvents.eReleaseResp(new PTypes.PTuple_stts_clnt_lock_lckst_rId_1(releaseRespStatus, clientID,
                lockID, lockStatus, rId));
    }
    @Override
    public Stream<PObserveEvent<PEvent<?>>> apply(Object obj) {
        long time;
        SimpleDateFormat df = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSSSSSSSSSSSSSS"); // $NON-NLS-L$ hardcoded locale
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
            System.out.println("Parsing error: " + e.toString());
            return Stream.empty();
        }
        String[] logInfoParts = logInfo.split(", ");
        HashMap<String, String> keyValueMap = new HashMap<>();
        for (String part : logInfoParts) {
            String[] keyValue = part.split("=");
            if (keyValue.length != 2) {
                return Stream.empty();
            }
            keyValueMap.put(keyValue[0], keyValue[1]);
        }
        return makeEvent(keyValueMap, line, time);
    }

    public Stream<PObserveEvent<PEvent<?>>> makeEvent(HashMap<String, String> keyValueMap, String line, long time) {
        PEvent<?> event = null;
        String transactionType = keyValueMap.getOrDefault("TransactionType", "");
        String messageType = keyValueMap.getOrDefault("MessageType", "");
        long clientID = parseStringValueToLong(keyValueMap.getOrDefault("ClientID", "null"));
        long lockID = parseStringValueToLong(keyValueMap.getOrDefault("LockID", "null"));
        long rId = parseStringValueToLong(keyValueMap.getOrDefault("TransactionID", "null"));
        switch (messageType) {
            case "REQUEST":
                if (transactionType.equals("LOCK")) {
                    event = onLockReq(clientID, lockID, rId);
                } else if (transactionType.equals("RELEASE")) {
                    event = onReleaseReq(clientID, lockID, rId);
                }
                break;
            case "RESPONSE":
                String result = keyValueMap.getOrDefault("Result", "");
                PTypes.tLockStatus lockStatus;
                String lockStatusString = keyValueMap.getOrDefault("LockStatus", "null");
                if (lockStatusString.equals("null")) {
                    lockStatus = null;
                } else {
                    lockStatus = PTypes.tLockStatus.valueOf(lockStatusString);
                }
                if (transactionType.equals("LOCK")) {
                    event = onLockRes(clientID, lockID, rId, result, lockStatus);
                } else if (transactionType.equals("RELEASE")) {
                    event = onReleaseRes(clientID, lockID, rId, result, lockStatus);
                }
                break;
            default:
                break;
        }
        return event == null ? Stream.empty() : Stream.of(new PObserveEvent(Long.toString(lockID), time, event, line));
    }

    private long parseStringValueToLong(String value) {
        return value.equals("null") ? 0 : Long.parseLong(value);
    }
}