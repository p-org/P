package pobserve.metrics;

import java.util.List;

public class MetricConstants {
    public static final String TOTAL_PARTITION_KEYS = "TotalPartitionKeys";
    public static final String TOTAL_VERIFIED_KEYS = "TotalVerifiedKeys";
    public static final String TOTAL_PARSER_ERRORS = "TotalParserErrors";
    public static final String TOTAL_SPEC_ERRORS = "TotalSpecErrors";
    public static final String TOTAL_EVENT_OUT_OF_ORDER_ERRORS = "TotalEventOutOfOrderErrors";
    public static final String TOTAL_UNKNOWN_ERRORS = "TotalUnknownErrors";
    public static final String TOTAL_EVENTS_READ = "TotalEventsRead";

    public static List<String> getMetricsList() {
        return List.of(
                TOTAL_VERIFIED_KEYS,
                TOTAL_PARTITION_KEYS,
                TOTAL_PARSER_ERRORS,
                TOTAL_SPEC_ERRORS,
                TOTAL_EVENT_OUT_OF_ORDER_ERRORS,
                TOTAL_UNKNOWN_ERRORS,
                TOTAL_EVENTS_READ);
    }
}
