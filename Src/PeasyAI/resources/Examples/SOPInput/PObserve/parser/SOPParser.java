package demo.pobserve;

import com.amazon.pobserve.commons.Parser;
import com.amazon.pobserve.runtime.events.PEvent;
import com.amazon.pobserve.commons.PObserveEvent;
import java.time.Instant;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Stream;
import spec.PEvents;
import spec.PTypes;

public class SOPParser implements Parser<PEvent<?>> {
    private static final Pattern LOG_PATTERN = Pattern.compile(
            "(\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z) - ([^:]+): (.*)");

    @Override
    public Stream<PObserveEvent<PEvent<?>>> apply(Object obj) {
        if (!(obj instanceof String)) {
            return Stream.empty();
        }

        String line = (String) obj;
        Matcher matcher = LOG_PATTERN.matcher(line);

        if (!matcher.matches()) {
            System.err.println("Invalid log line format: " + line);
            return Stream.empty();
        }

        try {
            String timestampStr = matcher.group(1);
            String eventName = matcher.group(2);
            String attributesStr = matcher.group(3);

            Instant timestamp = Instant.parse(timestampStr);

            return createEvent(eventName, attributesStr, timestamp, line);
        } catch (Exception e) {
            System.err.println("Error parsing log line: " + e.getMessage());
            return Stream.empty();
        }
    }

    private Stream<PObserveEvent<PEvent<?>>> createEvent(String eventName, String attributesStr, Instant timestamp,
            String line) {
        // Special case for finish which remains unchanged
        if (eventName.equals("finish")) {
            return parseFinishInvestigation(attributesStr, timestamp, line);
        }

        // Handle request events
        if (eventName.endsWith("Req")) {
            String baseEventName = eventName.substring(0, eventName.length() - 3);
            switch (baseEventName) {
                case "findStatus":
                    return parseFindStatusRequest(attributesStr, timestamp, line);
                case "findErrorMessage":
                    return parseFindErrorMessageRequest(attributesStr, timestamp, line);
                case "findExecutionTime":
                    return parseFindExecutionTimeRequest(attributesStr, timestamp, line);
                case "compareQueryPlan":
                    return parseCompareQueryPlanRequest(attributesStr, timestamp, line);
                case "findReturnedRows":
                    return parseFindReturnedRowsRequest(attributesStr, timestamp, line);
                case "getTotalSpilledBlocks":
                    return parseGetTotalSpilledBlocksRequest(attributesStr, timestamp, line);
                case "compareTotalSkewness":
                    return parseCompareTotalSkewnessRequest(attributesStr, timestamp, line);
                case "getQueueTimes":
                    return parseGetQueueTimesRequest(attributesStr, timestamp, line);
                case "getCompileTimes":
                    return parseGetCompileTimesRequest(attributesStr, timestamp, line);
                case "getPlanningTimes":
                    return parseGetPlanningTimesRequest(attributesStr, timestamp, line);
                case "getLockWaitTimes":
                    return parseGetLockWaitTimesRequest(attributesStr, timestamp, line);
                default:
                    System.err.println("Unknown request event: " + eventName);
                    return Stream.empty();
            }
        }
        // Handle response events
        else if (eventName.endsWith("Resp")) {
            String baseEventName = eventName.substring(0, eventName.length() - 4);
            switch (baseEventName) {
                case "findStatus":
                    return parseFindStatusResponse(attributesStr, timestamp, line);
                case "findErrorMessage":
                    return parseFindErrorMessageResponse(attributesStr, timestamp, line);
                case "findExecutionTime":
                    return parseFindExecutionTimeResponse(attributesStr, timestamp, line);
                case "compareQueryPlan":
                    return parseCompareQueryPlanResponse(attributesStr, timestamp, line);
                case "findReturnedRows":
                    return parseFindReturnedRowsResponse(attributesStr, timestamp, line);
                case "getTotalSpilledBlocks":
                    return parseGetTotalSpilledBlocksResponse(attributesStr, timestamp, line);
                case "compareTotalSkewness":
                    return parseCompareTotalSkewnessResponse(attributesStr, timestamp, line);
                case "getQueueTimes":
                    return parseGetQueueTimesResponse(attributesStr, timestamp, line);
                case "getCompileTimes":
                    return parseGetCompileTimesResponse(attributesStr, timestamp, line);
                case "getPlanningTimes":
                    return parseGetPlanningTimesResponse(attributesStr, timestamp, line);
                case "getLockWaitTimes":
                    return parseGetLockWaitTimesResponse(attributesStr, timestamp, line);
                default:
                    System.err.println("Unknown response event: " + eventName);
                    return Stream.empty();
            }
        } else {
            System.err.println("Invalid event name format: " + eventName);
            return Stream.empty();
        }
    }

    // Request parsing methods

    private Stream<PObserveEvent<PEvent<?>>> parseFindStatusRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            long queryId = Long.parseLong(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eFindStatusRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid findStatusReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFindStatusResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+),status=(.+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            long queryId = Long.parseLong(matcher.group(1));
            String statusStr = matcher.group(2);
            PEvent<?> responseEvent = new PEvents.eFindStatusResponse(new PTypes.PTuple_stts(statusStr));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid findStatusResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFindErrorMessageRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eFindErrorMessageRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid findErrorMessageReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFindErrorMessageResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+),error_message=(.+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            String errorMessage = matcher.group(2);
            PEvent<?> responseEvent = new PEvents.eFindErrorMessageResponse(new PTypes.PTuple_errr_(errorMessage));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid findErrorMessageResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFindExecutionTimeRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eFindExecutionTimeRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid findExecutionTimeReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFindExecutionTimeResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+),execution_time=(\\d+(?:\\.\\d+)?)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            float executionTime = Float.parseFloat(matcher.group(2));
            PEvent<?> responseEvent = new PEvents.eFindExecutionTimeResponse(new PTypes.PTuple_exctn(executionTime));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid findExecutionTimeResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseCompareQueryPlanRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eCheckIfSameQueryPlanRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid compareQueryPlanReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseCompareQueryPlanResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+),has_same_plan=(true|false)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            boolean hasSamePlan = Boolean.parseBoolean(matcher.group(2));
            PEvent<?> responseEvent = new PEvents.eCheckIfSameQueryPlanResponse(new PTypes.PTuple_hs_sm(hasSamePlan));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid compareQueryPlanResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFindReturnedRowsRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eGetReturnedRowsRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid findReturnedRowsReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFindReturnedRowsResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+),slow_query_row_count=(\\d+),fast_query_row_count=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            int slowQueryRowCount = Integer.parseInt(matcher.group(2));
            int fastQueryRowCount = Integer.parseInt(matcher.group(3));
            PEvent<?> responseEvent = new PEvents.eGetReturnedRowsResponse(
                    new PTypes.PTuple_slw_q_fst_q(slowQueryRowCount, fastQueryRowCount));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid findReturnedRowsResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetTotalSpilledBlocksRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eGetTotalSpilledBlocksRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid getTotalSpilledBlocksReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetTotalSpilledBlocksResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern
                .compile("query_id=(\\d+),fast_query_blocks_spilled=(\\d+),slow_query_blocks_spilled=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            int fastQueryBlocksSpilled = Integer.parseInt(matcher.group(2));
            int slowQueryBlocksSpilled = Integer.parseInt(matcher.group(3));
            PEvent<?> responseEvent = new PEvents.eGetTotalSpilledBlocksResponse(
                    new PTypes.PTuple_fst_q_slw_q(fastQueryBlocksSpilled, slowQueryBlocksSpilled));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid getTotalSpilledBlocksResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseCompareTotalSkewnessRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eCompareTotalSkewnessRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid compareTotalSkewnessReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseCompareTotalSkewnessResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+),has_similar_skewness=(true|false)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            boolean hasSimilarSkewness = Boolean.parseBoolean(matcher.group(2));
            PEvent<?> responseEvent = new PEvents.eCompareTotalSkewnessResponse(
                    new PTypes.PTuple_hs_sm_1(hasSimilarSkewness));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid compareTotalSkewnessResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetQueueTimesRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eGetQueueTimesRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid getQueueTimesReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetQueueTimesResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+),slow_query_queue_time=(\\d+),fast_query_queue_time=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            int slowQueryQueueTime = Integer.parseInt(matcher.group(2));
            int fastQueryQueueTime = Integer.parseInt(matcher.group(3));
            PEvent<?> responseEvent = new PEvents.eGetQueueTimesResponse(
                    new PTypes.PTuple_slw_q_fst_q_1(slowQueryQueueTime, fastQueryQueueTime));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid getQueueTimesResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetCompileTimesRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eGetCompileTimesRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid getCompileTimesReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetCompileTimesResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile(
                "query_id=(\\d+),slow_query_compile_time=(\\d+(?:\\.\\d+)?),fast_query_compile_time=(\\d+(?:\\.\\d+)?)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            float slowQueryCompileTime = Float.parseFloat(matcher.group(2));
            float fastQueryCompileTime = Float.parseFloat(matcher.group(3));
            PEvent<?> responseEvent = new PEvents.eGetCompileTimesResponse(
                    new PTypes.PTuple_slw_q_fst_q_2(slowQueryCompileTime, fastQueryCompileTime));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid getCompileTimesResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetPlanningTimesRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eGetPlanningTimesRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid getPlanningTimesReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetPlanningTimesResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile(
                "query_id=(\\d+),slow_query_planning_time=(\\d+(?:\\.\\d+)?),fast_query_planning_time=(\\d+(?:\\.\\d+)?)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            float slowQueryPlanningTime = Float.parseFloat(matcher.group(2));
            float fastQueryPlanningTime = Float.parseFloat(matcher.group(3));
            PEvent<?> responseEvent = new PEvents.eGetPlanningTimesResponse(
                    new PTypes.PTuple_slw_q_fst_q_3(slowQueryPlanningTime, fastQueryPlanningTime));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid getPlanningTimesResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetLockWaitTimesRequest(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            PEvent<?> requestEvent = new PEvents.eGetLockWaitTimesRequest(new PTypes.PTuple_qry_d(queryId));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
        }
        throw new IllegalArgumentException("Invalid getLockWaitTimesReq format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseGetLockWaitTimesResponse(String attributesStr, Instant timestamp,
            String line) {
        Pattern pattern = Pattern.compile(
                "query_id=(\\d+),slow_query_lock_wait_time=(\\d+(?:\\.\\d+)?),fast_query_lock_wait_time=(\\d+(?:\\.\\d+)?)");
        Matcher matcher = pattern.matcher(attributesStr);

        if (matcher.find()) {
            int queryId = Integer.parseInt(matcher.group(1));
            float slowQueryLockWaitTime = Float.parseFloat(matcher.group(2));
            float fastQueryLockWaitTime = Float.parseFloat(matcher.group(3));
            PEvent<?> responseEvent = new PEvents.eGetLockWaitTimesResponse(
                    new PTypes.PTuple_slw_q_fst_q_4(slowQueryLockWaitTime, fastQueryLockWaitTime));
            return Stream.of(new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, responseEvent, line));
        }
        throw new IllegalArgumentException("Invalid getLockWaitTimesResp format");
    }

    private Stream<PObserveEvent<PEvent<?>>> parseFinishInvestigation(String attributesStr, Instant timestamp,
            String line) {
        // Extract queryId if available
        Pattern pattern = Pattern.compile("query_id=(\\d+)");
        Matcher matcher = pattern.matcher(attributesStr);

        Long queryId = null;
        if (matcher.find()) {
            queryId = Long.parseLong(matcher.group(1));
        }

        PEvent<?> requestEvent = new PEvents.eFinishInvestigation();
        return Stream.of(
                new PObserveEvent<PEvent<?>>(Long.toString(queryId), timestamp, requestEvent, line));
    }
}
